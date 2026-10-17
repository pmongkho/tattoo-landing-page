using System.ComponentModel.DataAnnotations;
using dotnet_server._Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace dotnet_server._Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(UserManager<IdentityUser> users, SignInManager<IdentityUser> signIn, AdminTokenService tokens) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        Response.Headers.CacheControl = "no-store";
        if (!tokens.IsConfigured) return Problem("Admin login is not configured. Set JWT__Key in backend environment settings.", statusCode: 503);
        var user = await users.FindByEmailAsync(request.Email.Trim());
        if (user is null) return Unauthorized(new { message = "Invalid email or password." });
        var result = await signIn.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded || !await users.IsInRoleAsync(user, "Admin"))
            return Unauthorized(new { message = "Invalid email or password." });
        var token = tokens.Create(user);
        return Ok(new { accessToken = token.AccessToken, expiresAt = token.ExpiresAt });
    }

    public record LoginRequest([Required, EmailAddress, StringLength(254)] string Email,
        [Required, StringLength(1024)] string Password);
}
