using dotnet_server._Data;
using dotnet_server._HostedServices;
using dotnet_server._Integrations;
using dotnet_server._Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using System.Text;
using System.Security.Cryptography;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

const string CorsPolicy = "TattooFrontend";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentityCore<IdentityUser>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequiredLength = 12;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
}).AddRoles<IdentityRole>().AddEntityFrameworkStores<AppDbContext>().AddSignInManager();

var jwt = builder.Configuration.GetSection("JWT").Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwt.Key) && builder.Environment.IsDevelopment())
    jwt.Key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
if (!jwt.IsValid() && !string.IsNullOrWhiteSpace(jwt.Key))
    throw new InvalidOperationException("Configure JWT__Key (at least 32 bytes), JWT__Issuer, JWT__Audience and JWT__AccessTokenMinutes (1-1440) on the backend.");
builder.Services.AddSingleton<IOptions<JwtOptions>>(Options.Create(jwt));
builder.Services.AddScoped<AdminTokenService>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = jwt.IsValid() ? new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)) : null,
        ValidateIssuer = true, ValidIssuer = jwt.Issuer,
        ValidateAudience = true, ValidAudience = jwt.Audience,
        ValidateLifetime = true, ClockSkew = TimeSpan.FromSeconds(30)
    };
});
builder.Services.AddAuthorization();

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

builder.Services.Configure<InstagramOptions>(builder.Configuration.GetSection("Instagram"));
builder.Services.AddHttpClient<InstagramClient>(client =>
{
    client.BaseAddress = new Uri("https://graph.instagram.com/");
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddScoped<InstagramSyncService>();
builder.Services.AddHostedService<InstagramSyncWorker>();

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

// Explicit deployment switches; neither schema changes nor admin creation happen by default.
if (app.Configuration.GetValue<bool>("Database:ApplyMigrations"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
}
await AdminBootstrapper.SeedAsync(app.Services, app.Configuration);
if (!jwt.IsValid())
    app.Logger.LogWarning("Admin login is disabled until JWT__Key is configured on the backend. Public endpoints remain available.");
if (app.Environment.IsDevelopment() && string.IsNullOrWhiteSpace(app.Configuration["JWT:Key"]))
    app.Logger.LogWarning("JWT__Key is unset: using a temporary development key. Admins must log in again after a restart.");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(CorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
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

public partial class Program { }
