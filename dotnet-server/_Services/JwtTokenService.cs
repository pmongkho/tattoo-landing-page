using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using dotnet_server._Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace dotnet_server._Services;

public interface IJwtTokenService
{
    Task<(string token, DateTimeOffset expiresAt)> CreateAdminTokenAsync(ApplicationUser user);
}

public class JwtTokenService(IConfiguration configuration, UserManager<ApplicationUser> userManager) : IJwtTokenService
{
    public async Task<(string token, DateTimeOffset expiresAt)> CreateAdminTokenAsync(ApplicationUser user)
    {
        var jwtKey = configuration["JWT__Key"] ?? throw new InvalidOperationException("JWT__Key not configured.");
        var jwtIssuer = configuration["JWT__Issuer"] ?? "dotnet-server";
        var jwtAudience = configuration["JWT__Audience"] ?? "tattoo-frontend";
        var jwtExpiryMinutes = int.TryParse(configuration["JWT__AccessTokenMinutes"], out var minutes) ? minutes : 120;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var roles = await userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? user.Email ?? string.Empty)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(jwtExpiryMinutes);
        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
