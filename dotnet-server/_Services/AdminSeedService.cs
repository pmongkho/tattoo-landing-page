using dotnet_server._Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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
        var dbContext = scope.ServiceProvider.GetRequiredService<_Data.AppDbContext>();

        if (!await IdentitySchemaExistsAsync(dbContext))
        {
            return;
        }

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

    private static async Task<bool> IdentitySchemaExistsAsync(_Data.AppDbContext dbContext)
    {
        try
        {
            await using var connection = dbContext.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT to_regclass('public.\"AspNetRoles\"') IS NOT NULL;";
            var result = await command.ExecuteScalarAsync();

            return result is true;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable)
        {
            return false;
        }
    }
}
