using dotnet_server._Dtos;
using dotnet_server._Models;
using dotnet_server._Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace dotnet_server._Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(UserManager<ApplicationUser> userManager, IJwtTokenService jwtTokenService) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] AdminLoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return Unauthorized();
        }

        if (!await userManager.IsInRoleAsync(user, "Admin"))
        {
            return Forbid();
        }

        var validPassword = await userManager.CheckPasswordAsync(user, request.Password);
        if (!validPassword)
        {
            return Unauthorized();
        }

        var (token, expiresAt) = await jwtTokenService.CreateAdminTokenAsync(user);
        return Ok(new AuthResponse { AccessToken = token, ExpiresAt = expiresAt });
    }
}
