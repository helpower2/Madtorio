using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Madtorio.Data.Seed;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        // Apply any pending migrations
        await context.Database.MigrateAsync();

        // Create Admin role if it doesn't exist
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
            logger.LogInformation("Admin role created.");
        }

        // Create default admin user if it doesn't exist
        var adminEmail = "admin@madtorio.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            var defaultPassword = "Madtorio2026!";
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(adminUser, defaultPassword);

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
                logger.LogWarning("========================================");
                logger.LogWarning("DEFAULT ADMIN USER CREATED");
                logger.LogWarning("Email: {Email}", adminEmail);
                logger.LogWarning("Password: {Password}", defaultPassword);
                logger.LogWarning("PLEASE CHANGE THIS PASSWORD IMMEDIATELY!");
                logger.LogWarning("========================================");
            }
            else
            {
                logger.LogError("Failed to create admin user: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }
}
