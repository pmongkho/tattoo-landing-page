using dotnet_server._Data;
using dotnet_server._HostedServices;
using dotnet_server._Integrations;
using dotnet_server._Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

const string CorsPolicy = "TattooFrontend";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<QuoApiOptions>(builder.Configuration.GetSection(QuoApiOptions.SectionName));
builder.Services.Configure<SquareOptions>(builder.Configuration.GetSection(SquareOptions.SectionName));
builder.Services.AddHttpClient<IQuoLeadMessagingClient, QuoLeadMessagingClient>((sp, client) =>
{
    var quoOptions = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<QuoApiOptions>>().Value;
    if (!string.IsNullOrWhiteSpace(quoOptions.BaseUrl))
    {
        client.BaseAddress = new Uri($"{quoOptions.BaseUrl.TrimEnd('/')}/");
    }
});
builder.Services.AddHttpClient("SquareApi", (sp, client) =>
{
    var squareOptions = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<SquareOptions>>().Value;
    client.BaseAddress = new Uri(squareOptions.BaseUrl);
});
builder.Services.AddHttpClient<ISquareCustomerClient, SquareCustomerClient>((sp, client) =>
{
    var squareOptions = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<SquareOptions>>().Value;
    client.BaseAddress = new Uri(squareOptions.BaseUrl);
});

builder.Services.AddScoped<ISquareDepositService, SquareDepositService>();
builder.Services.AddHostedService<SquareDepositBackgroundService>();

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
