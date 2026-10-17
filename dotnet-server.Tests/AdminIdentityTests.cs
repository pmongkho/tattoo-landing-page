using System.Net;
using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using dotnet_server._Data;
using dotnet_server._HostedServices;
using dotnet_server._Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Xunit;

public class AdminIdentityTests : IAsyncLifetime
{
    private readonly IdentityApp app = new();
    private HttpClient client = null!;
    private const string Password = "Test-only-password-123!";
    private const string Email = "admin@example.test";

    public async Task InitializeAsync()
    {
        client = app.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        using var scope = app.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreatedAsync();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        Assert.True((await roles.CreateAsync(new IdentityRole("Admin"))).Succeeded);
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var admin = new IdentityUser { UserName = Email, Email = Email };
        Assert.True((await users.CreateAsync(admin, Password)).Succeeded);
        Assert.True((await users.AddToRoleAsync(admin, "Admin")).Succeeded);
        Assert.True((await users.CreateAsync(new IdentityUser { UserName = "reader@example.test", Email = "reader@example.test" }, Password)).Succeeded);
    }

    [Fact]
    public async Task AdminLoginIssuesUsableTokenAndStoresOnlyPasswordHash()
    {
        var response = await Login(" ADMIN@EXAMPLE.TEST ", Password);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("no-store", response.Headers.CacheControl!.ToString());
        var body = await response.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(body);
        client.DefaultRequestHeaders.Authorization = new("Bearer", body.AccessToken);
        var result = await client.PatchAsJsonAsync($"/api/admin/consultations/{Guid.Empty}/status", new { status = "New" });
        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode); // Authorized, then no such consultation.
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.PostAsJsonAsync("/api/portfolio/sync", new { })).StatusCode);
        using var scope = app.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var user = await users.FindByEmailAsync(Email);
        Assert.NotEqual(Password, user!.PasswordHash);
        Assert.True(await users.CheckPasswordAsync(user, Password));
    }

    [Fact]
    public async Task MissingCredentialsCannotReadOrChangeAdminData()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/admin/consultations")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/portfolio/manage")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/portfolio/sync", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/quo-messaging/test", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/auth/register", new { })).StatusCode);
    }

    [Theory]
    [InlineData("unknown@example.test", Password)]
    [InlineData(Email, "Wrong-password-123!")]
    [InlineData("reader@example.test", Password)]
    public async Task InvalidOrNonAdminAccountsReceiveSameFailure(string email, string password)
    {
        var response = await Login(email, password);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("{\"message\":\"Invalid email or password.\"}", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task FiveBadPasswordsLockAccountEvenWhenCorrectPasswordFollows()
    {
        for (var i = 0; i < 5; i++) Assert.Equal(HttpStatusCode.Unauthorized, (await Login(Email, "Wrong-password-123!")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Login(Email, Password)).StatusCode);
        using var scope = app.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        Assert.True(await users.IsLockedOutAsync((await users.FindByEmailAsync(Email))!));
    }

    [Fact]
    public async Task ExpiredWrongAudienceAndNonAdminTokensAreRejected()
    {
        foreach (var (role, audience, expires, status) in new[] {
            ("Admin", "tattoo-admin", DateTime.UtcNow.AddMinutes(-5), HttpStatusCode.Unauthorized),
            ("Admin", "other-app", DateTime.UtcNow.AddMinutes(5), HttpStatusCode.Unauthorized),
            ("Reader", "tattoo-admin", DateTime.UtcNow.AddMinutes(5), HttpStatusCode.Forbidden) })
        {
            var token = new JwtSecurityToken("tattoo-api", audience, [new Claim(ClaimTypes.Role, role)], expires: expires,
                signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(IdentityApp.Key)), SecurityAlgorithms.HmacSha256));
            client.DefaultRequestHeaders.Authorization = new("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
            Assert.Equal(status, (await client.GetAsync("/api/admin/consultations")).StatusCode);
        }
    }

    [Fact]
    public async Task BootstrapIsOptInAndDoesNotResetExistingAdminPassword()
    {
        IConfiguration Config(bool enabled, string password) => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["AdminBootstrap:Enabled"] = enabled.ToString(), ["ADMIN_EMAIL"] = "bootstrap@example.test", ["ADMIN_PASSWORD"] = password
        }).Build();
        await AdminBootstrapper.SeedAsync(app.Services, Config(false, Password));
        using var scope = app.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        Assert.Null(await users.FindByEmailAsync("bootstrap@example.test"));
        await AdminBootstrapper.SeedAsync(app.Services, Config(true, Password));
        await AdminBootstrapper.SeedAsync(app.Services, Config(true, "Different-password-456!"));
        var user = await users.FindByEmailAsync("bootstrap@example.test");
        Assert.True(await users.IsInRoleAsync(user!, "Admin"));
        Assert.True(await users.CheckPasswordAsync(user!, Password));
        Assert.False(await users.CheckPasswordAsync(user!, "Different-password-456!"));
    }

    [Fact]
    public async Task TamperedTokenCannotAccessAdminEndpoints()
    {
        var response = await Login(Email, Password);
        var token = (await response.Content.ReadFromJsonAsync<TokenResponse>())!.AccessToken.Split('.');
        token[2] = (token[2][0] == 'a' ? "b" : "a") + token[2][1..];
        client.DefaultRequestHeaders.Authorization = new("Bearer", string.Join('.', token));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/admin/consultations")).StatusCode);
    }

    [Fact]
    public async Task BootstrapDoesNotPromoteExistingNonAdmin()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["AdminBootstrap:Enabled"] = "true", ["ADMIN_EMAIL"] = "reader@example.test", ["ADMIN_PASSWORD"] = Password
        }).Build();
        await Assert.ThrowsAsync<InvalidOperationException>(() => AdminBootstrapper.SeedAsync(app.Services, config));
        using var scope = app.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        Assert.False(await users.IsInRoleAsync((await users.FindByEmailAsync("reader@example.test"))!, "Admin"));
    }

    [Fact]
    public async Task MissingProductionSigningKeyDisablesLoginWithoutBreakingPublicService()
    {
        await using var unconfigured = new IdentityApp(noKey: true);
        using var http = unconfigured.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await http.PostAsJsonAsync("/api/auth/login", new { email = Email, password = Password })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await http.GetAsync("/health/db")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await http.GetAsync("/api/admin/consultations")).StatusCode);
    }

    private Task<HttpResponseMessage> Login(string email, string password) => client.PostAsJsonAsync("/api/auth/login", new { email, password });
    public async Task DisposeAsync() { client.Dispose(); await app.DisposeAsync(); }
    private record TokenResponse(string AccessToken, DateTimeOffset ExpiresAt);

    private class IdentityApp(bool noKey = false) : WebApplicationFactory<Program>
    {
        public const string Key = "Only-for-tests-signing-key-never-use-in-production-123456789";
        private readonly SqliteConnection connection = new("Data Source=:memory:");
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            connection.Open();
            builder.UseEnvironment(noKey ? "Production" : "Development").UseSetting("JWT:Key", noKey ? "" : Key)
                .UseSetting("JWT:Issuer", "tattoo-api").UseSetting("JWT:Audience", "tattoo-admin")
                .UseSetting("AdminBootstrap:Enabled", "false").UseSetting("Database:ApplyMigrations", "false")
                .UseSetting("Instagram:AccessToken", "");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                services.RemoveAll<AppDbContext>();
                services.AddDbContext<AppDbContext>(o => o.UseSqlite(connection));
                foreach (var service in services.Where(x => x.ServiceType == typeof(IHostedService) &&
                    (x.ImplementationType == typeof(InstagramSyncWorker) || x.ImplementationType == typeof(SquareDepositBackgroundService))).ToArray())
                    services.Remove(service);
            });
        }
        public override async ValueTask DisposeAsync() { await base.DisposeAsync(); await connection.DisposeAsync(); }
    }
}
