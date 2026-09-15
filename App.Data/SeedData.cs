using App.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace App.Data;

/// <summary>
/// Seeds the two Identity roles ("Admin", "Consultant") and one initial Admin
/// user, so there's a way to log in on first run. Every user gets the
/// Consultant role — Admin is granted ADDITIONALLY on top of it (never
/// instead of it), so a promoted Admin can still be assigned to projects as
/// a Manager/Consultant like anyone else. Call SeedAsync from
/// Program.cs after the app is built, e.g.:
///
///   using (var scope = app.Services.CreateScope())
///   {
///       await SeedData.SeedAsync(scope.ServiceProvider);
///   }
///
/// The seeded Admin's password is read from configuration
/// (Seed:AdminEmail / Seed:AdminPassword — e.g. via user-secrets or an env
/// var) with hardcoded fallbacks for local dev only. Change the password
/// after first login in anything beyond local dev.
/// </summary>
public static class SeedData
{
    public const string AdminRole = "Admin";
    public const string ConsultantRole = "Consultant";

    private const string DefaultAdminEmail = "admin@timetracker.local";
    private const string DefaultAdminPassword = "ChangeMe123!";
    private const string DefaultAdminName = "System Administrator";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var config = services.GetRequiredService<IConfiguration>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(SeedData));

        await EnsureRoleAsync(roleManager, AdminRole);
        await EnsureRoleAsync(roleManager, ConsultantRole);

        var adminEmail = config["Seed:AdminEmail"] ?? DefaultAdminEmail;
        var adminPassword = config["Seed:AdminPassword"] ?? DefaultAdminPassword;

        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin is not null)
        {
            logger.LogInformation("Seed admin {Email} already exists — skipping.", adminEmail);
            return;
        }

        var admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            Name = DefaultAdminName
        };

        var createResult = await userManager.CreateAsync(admin, adminPassword);
        if (!createResult.Succeeded)
        {
            var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
            logger.LogError("Failed to seed admin user {Email}: {Errors}", adminEmail, errors);
            return;
        }

        // Every user is a Consultant by default, including Admins — Admin is an
        // additional role layered on top, not a replacement for it.
        await userManager.AddToRoleAsync(admin, ConsultantRole);
        await userManager.AddToRoleAsync(admin, AdminRole);
        logger.LogInformation("Seeded initial admin user {Email}.", adminEmail);
    }

    private static async Task EnsureRoleAsync(RoleManager<IdentityRole> roleManager, string roleName)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }
}
