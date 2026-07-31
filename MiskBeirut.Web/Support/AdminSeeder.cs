using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using MiskBeirut.Core.Constants;
using MiskBeirut.Core.Entities;

namespace MiskBeirut.Web.Support;

/// <summary>
/// Ensures the fixed set of roles exists and at least one Admin account exists on startup.
/// There is no self-registration path for the Admin area (accounts are created by an existing
/// Admin), so without this the app would have no way to log in for the first time.
/// </summary>
public static class AdminSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<object>>();

        foreach (var role in RoleNames.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole<int>(role));
        }

        if (await userManager.GetUsersInRoleAsync(RoleNames.Admin) is { Count: > 0 })
            return;

        var password = GenerateTemporaryPassword();
        var admin = new User
        {
            UserName = "admin",
            Email = "admin@miskbeirut.com",
            CreatedAt = DateTime.UtcNow,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(admin, password);
        if (!result.Succeeded)
        {
            logger.LogError("Failed to seed initial admin account: {Errors}",
                string.Join("; ", result.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(admin, RoleNames.Admin);

        logger.LogWarning(
            "Seeded initial admin account. Username: {Username}  Password: {Password}  " +
            "Log in and change this password immediately.",
            admin.UserName, password);
    }

    private static string GenerateTemporaryPassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
        var bytes = RandomNumberGenerator.GetBytes(16);
        var result = new char[16];
        for (var i = 0; i < result.Length; i++)
            result[i] = chars[bytes[i] % chars.Length];
        return new string(result);
    }
}
