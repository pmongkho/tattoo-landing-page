using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace dotnet_server._Services;

public class JwtOptions
{
    public string Key { get; set; } = "";
    public string Issuer { get; set; } = "tattoo-api";
    public string Audience { get; set; } = "tattoo-admin";
    public int AccessTokenMinutes { get; set; } = 120;

    public bool IsValid() => Encoding.UTF8.GetByteCount(Key) >= 32 &&
        !string.IsNullOrWhiteSpace(Issuer) && !string.IsNullOrWhiteSpace(Audience) &&
        AccessTokenMinutes is >= 1 and <= 1440;
}

public class AdminTokenService(IOptions<JwtOptions> options)
{
    public bool IsConfigured => options.Value.IsValid();

    public (string AccessToken, DateTimeOffset ExpiresAt) Create(IdentityUser user)
    {
        if (!IsConfigured) throw new InvalidOperationException("JWT signing is not configured.");
        var config = options.Value;
        var expires = DateTimeOffset.UtcNow.AddMinutes(config.AccessTokenMinutes);
        var token = new JwtSecurityToken(config.Issuer, config.Audience,
            [new Claim(JwtRegisteredClaimNames.Sub, user.Id),
             new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
             new Claim(ClaimTypes.Role, "Admin")],
            notBefore: DateTime.UtcNow, expires: expires.UtcDateTime,
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config.Key)), SecurityAlgorithms.HmacSha256));
        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}

public static class AdminBootstrapper
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration config)
    {
        if (!config.GetValue<bool>("AdminBootstrap:Enabled")) return;
        var email = config["ADMIN_EMAIL"]?.Trim();
        var password = config["ADMIN_PASSWORD"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Admin bootstrap requires ADMIN_EMAIL and ADMIN_PASSWORD in server environment settings.");

        using var scope = services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var existing = await users.FindByEmailAsync(email);
        if (existing is not null)
        {
            if (!await users.IsInRoleAsync(existing, "Admin"))
                throw new InvalidOperationException("Bootstrap email already belongs to a non-admin account. It will not be promoted automatically.");
            return; // Never reset an existing admin's password at startup.
        }
        if (!await roles.RoleExistsAsync("Admin")) Check(await roles.CreateAsync(new IdentityRole("Admin")));
        var user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
        Check(await users.CreateAsync(user, password));
        var assigned = await users.AddToRoleAsync(user, "Admin");
        if (!assigned.Succeeded)
        {
            await users.DeleteAsync(user);
            Check(assigned);
        }
    }

    private static void Check(IdentityResult result)
    {
        // Codes are sufficient to diagnose bootstrap without logging credentials or user input.
        if (!result.Succeeded) throw new InvalidOperationException("Admin bootstrap failed: " + string.Join(", ", result.Errors.Select(x => x.Code)));
    }
}
