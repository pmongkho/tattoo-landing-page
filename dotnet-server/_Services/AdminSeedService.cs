using dotnet_server._Models;
using Microsoft.AspNetCore.Identity;

namespace dotnet_server._Services;

public static class AdminSeedService
{
    public static async Task SeedDevelopmentAdminAsync(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        using var scope = serviceProvider.CreateScope();
        var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

        if (!env.IsDevelopment())
        {
            return;
        }

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        const string adminRole = "Admin";
        if (!await roleManager.RoleExistsAsync(adminRole))
        {
            await roleManager.CreateAsync(new IdentityRole(adminRole));
        }

        var adminEmail = configuration["ADMIN_EMAIL"];
        var adminPassword = configuration["ADMIN_PASSWORD"];

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            return;
        }

        var existingUser = await userManager.FindByEmailAsync(adminEmail);
        if (existingUser is null)
        {
            var newAdmin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(newAdmin, adminPassword);
            if (!createResult.Succeeded)
            {
                return;
            }

            existingUser = newAdmin;
        }

        if (!await userManager.IsInRoleAsync(existingUser, adminRole))
        {
            await userManager.AddToRoleAsync(existingUser, adminRole);
        }
    }
}
