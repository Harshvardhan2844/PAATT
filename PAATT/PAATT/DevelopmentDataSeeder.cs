using Microsoft.AspNetCore.Identity;
using PAATT.Data.Entities;
using PAATT.Shared.Enums;

namespace PAATT;

public static class DevelopmentDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration, bool isDevelopment)
    {
        if (!isDevelopment) return;
        var password = configuration["DevelopmentSeed:Password"];
        if (string.IsNullOrWhiteSpace(password)) return;
        var users = services.GetRequiredService<UserManager<ApplicationUser>>();
        const string email = "admin@paatt.local";
        var user = await users.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser { Name = "PAATT Administrator", Email = email, UserName = email, EmailConfirmed = true };
            var result = await users.CreateAsync(user, password);
            if (!result.Succeeded) throw new InvalidOperationException(string.Join(" ", result.Errors.Select(error => error.Description)));
        }
        else if (!await users.CheckPasswordAsync(user, password))
        {
            var resetToken = await users.GeneratePasswordResetTokenAsync(user);
            var result = await users.ResetPasswordAsync(user, resetToken, password);
            if (!result.Succeeded) throw new InvalidOperationException(string.Join(" ", result.Errors.Select(error => error.Description)));
        }
        var roles = Enum.GetNames<AppRole>();
        var existingRoles = await users.GetRolesAsync(user);
        var missingRoles = roles.Except(existingRoles, StringComparer.OrdinalIgnoreCase);
        await users.AddToRolesAsync(user, missingRoles);
    }
}
