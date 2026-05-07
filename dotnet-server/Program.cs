using dotnet_server._Data;
using dotnet_server._Integrations;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

const string CorsPolicy = "TattooFrontend";

builder.Services.AddDbContext<AppDbContext>(options => 
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

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
        var allowedOrigins = new List<string>
        {
            "http://localhost:4200",
            "https://wohutattoo.vercel.app",
            "https://www.wohutattoo.vercel.app"
        };

        var frontendOrigin = builder.Configuration["FRONTEND_ORIGIN"];
        if (!string.IsNullOrWhiteSpace(frontendOrigin))
        {
            allowedOrigins.Add(frontendOrigin);
        }

        policy
            .WithOrigins(allowedOrigins.Distinct(StringComparer.OrdinalIgnoreCase).ToArray())
            .SetIsOriginAllowedToAllowWildcardSubdomains()
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



app.Run();

