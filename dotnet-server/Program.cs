using dotnet_server._Data;
using dotnet_server._Integrations;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

const string CorsPolicy = "TattooFrontend";

var rawConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

var connectionString = NormalizeConnectionString(rawConnectionString);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.Configure<QuoApiOptions>(builder.Configuration.GetSection(QuoApiOptions.SectionName));
builder.Services.Configure<SquareApiOptions>(builder.Configuration.GetSection(SquareApiOptions.SectionName));
builder.Services.AddHttpClient<IQuoLeadMessagingClient, QuoLeadMessagingClient>((sp, client) =>
{
    var quoOptions = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<QuoApiOptions>>().Value;
    if (!string.IsNullOrWhiteSpace(quoOptions.BaseUrl))
    {
        client.BaseAddress = new Uri(quoOptions.BaseUrl);
    }
});
builder.Services.AddHttpClient<ISquareBookingClient, SquareBookingPlaceholder>((sp, client) =>
{
    var squareOptions = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<SquareApiOptions>>().Value;
    if (!string.IsNullOrWhiteSpace(squareOptions.BaseUrl))
    {
        client.BaseAddress = new Uri(squareOptions.BaseUrl);
    }
});

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
    {
        var allowedOrigins = new List<string> { "http://localhost:4200","https://wohutattoo.vercel.app" };
        var frontendOrigin = builder.Configuration["FRONTEND_ORIGIN"];
        if (!string.IsNullOrWhiteSpace(frontendOrigin))
        {
            allowedOrigins.Add(frontendOrigin);
        }

        policy.WithOrigins(allowedOrigins.Distinct().ToArray())
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(CorsPolicy);

app.MapControllers();


app.MapGet("/health/db", async (AppDbContext dbContext) =>
{
    try
    {
        var canConnect = await dbContext.Database.CanConnectAsync();
        return canConnect
            ? Results.Ok(new { status = "healthy", database = "reachable", checkedAt = DateTimeOffset.UtcNow })
            : Results.Problem("Database is unreachable.", statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Database health check failed: {ex.Message}", statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    var allMigrations = (await dbContext.Database.GetMigrationsAsync()).ToList();
    var appliedMigrations = (await dbContext.Database.GetAppliedMigrationsAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);
    var firstMigration = allMigrations.FirstOrDefault();

    if (firstMigration is not null && appliedMigrations.Count == 0)
    {
        var consultationsTableExists = await dbContext.Database.SqlQueryRaw<int>(@"""
            SELECT CASE
                WHEN EXISTS (
                    SELECT 1
                    FROM information_schema.tables
                    WHERE table_schema = 'public'
                      AND table_name = 'Consultations'
                ) THEN 1 ELSE 0 END;
            """).SingleAsync() == 1;

        if (consultationsTableExists)
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync($@"""
                INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
                VALUES ({firstMigration}, {'10.0.0'})
                ON CONFLICT ("MigrationId") DO NOTHING;
                """);
        }
    }

    await dbContext.Database.MigrateAsync();

    await dbContext.Database.ExecuteSqlRawAsync(@"
        ALTER TABLE ""Consultations""
        ADD COLUMN IF NOT EXISTS ""Status"" character varying(40) NOT NULL DEFAULT 'New';
    ");
}

app.Run();

static string NormalizeConnectionString(string connectionString)
{
    if (!connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
        && !connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
    {
        return connectionString;
    }

    var uri = new Uri(connectionString);
    var userInfoParts = uri.UserInfo.Split(':', 2);
    var builder = new NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.Port > 0 ? uri.Port : 5432,
        Database = uri.AbsolutePath.TrimStart('/'),
        Username = userInfoParts[0],
        Password = userInfoParts.Length > 1 ? userInfoParts[1] : string.Empty,
        SslMode = SslMode.Require
    };

    return builder.ConnectionString;
}
