using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Madtorio.Data.Models;

namespace Madtorio.Data.Seed;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider, string adminEmail, string adminPassword)
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        // Validate admin email
        if (string.IsNullOrWhiteSpace(adminEmail) || !adminEmail.Contains('@'))
        {
            throw new ArgumentException("Invalid admin email address.", nameof(adminEmail));
        }

        // Validate admin password (Identity requirements)
        if (string.IsNullOrWhiteSpace(adminPassword) || adminPassword.Length < 6)
        {
            throw new ArgumentException("Admin password must be at least 6 characters long.", nameof(adminPassword));
        }

        // Apply any pending migrations
        await context.Database.MigrateAsync();

        // Seed rules and server config
        await SeedRulesAndServerConfig(context, logger);

        // Create Admin role if it doesn't exist
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
            logger.LogInformation("Admin role created.");
        }

        // Create default admin user if it doesn't exist
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
                logger.LogWarning("========================================");
                logger.LogWarning("ADMIN USER CREATED");
                logger.LogWarning("Email: {Email}", adminEmail);

                // Warn if using default credentials
                if (adminEmail == "admin@madtorio.com" && adminPassword == "Madtorio2026!")
                {
                    logger.LogWarning("Using DEFAULT credentials - PLEASE CHANGE IMMEDIATELY!");
                }

                logger.LogWarning("CHANGE THE PASSWORD ON FIRST LOGIN!");
                logger.LogWarning("========================================");
            }
            else
            {
                logger.LogError("Failed to create admin user: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }

    private static async Task SeedRulesAndServerConfig(ApplicationDbContext context, ILogger logger)
    {
        // Seed Server IP if not exists
        if (!await context.ServerConfigs.AnyAsync(sc => sc.Key == "ServerIP"))
        {
            context.ServerConfigs.Add(new ServerConfig
            {
                Key = "ServerIP",
                Value = "192.67.197.11",
                Description = "Factorio dedicated server IP address"
            });
            logger.LogInformation("Server IP configuration seeded.");
        }

        // Seed categories if not exist
        if (!await context.RuleCategories.AnyAsync())
        {
            var generalRules = new RuleCategory
            {
                Name = "General Rules",
                Description = null,
                DisplayOrder = 1,
                IsEnabled = true
            };

            var serverRules = new RuleCategory
            {
                Name = "Factorio Server Rules",
                Description = "All general rules still apply.",
                DisplayOrder = 2,
                IsEnabled = true
            };

            context.RuleCategories.Add(generalRules);
            context.RuleCategories.Add(serverRules);
            await context.SaveChangesAsync();

            // Seed General Rules (6 rules)
            var generalRulesList = new[]
            {
                "Keep it in english",
                "No racism, sexism, bigotry, etc.",
                "No NSFW posts; this includes images, links and other content",
                "No charged conversations, or being toxic, treat everyone with respect",
                "No drama. Leave private matters private, and avoid starting conflict with other members of the server",
                "No abusing reactions. This includes spelling hateful things and stirring up drama through them"
            };

            for (int i = 0; i < generalRulesList.Length; i++)
            {
                context.Rules.Add(new Rule
                {
                    CategoryId = generalRules.Id,
                    Content = generalRulesList[i],
                    DisplayOrder = i + 1,
                    IsEnabled = true
                });
            }

            // Seed Factorio Server Rules (9 rules)
            var serverRulesList = new[]
            {
                new { Content = "All general rules still apply", DetailedDescription = (string?)null },
                new { Content = "No griefing; which includes killing players out of malice", DetailedDescription = (string?)null },
                new { Content = "No game breaking bug abuse", DetailedDescription = (string?)null },
                new { Content = "Keep the arguments civil and respectful, don't get personal", DetailedDescription = (string?)null },
                new { Content = "Avoid excessive or selfish use of shared resources", DetailedDescription = "This includes making constant, large requests for items you don't use productively; Building massive, unnecessary structures that harm server progression or remain unused; Hoarding materials or creating personal projects that drain resources without benefiting the factory." },
                new { Content = "Respect server limits; avoid creating unnecessarily resource-intensive or lag-inducing systems. Keep the server performance in mind", DetailedDescription = (string?)null },
                new { Content = "Each planet should be a unified system, since you can only have 1 cargo landing pad. Avoid creating isolated, disconnected, or redundant setups unless they serve a clear purpose for the team", DetailedDescription = (string?)null },
                new { Content = "Complete pre-made factory blueprints from external sources are not allowed. Using individual blueprints (miners, smelters, etc.) is fine. Build the overall base collaboratively", DetailedDescription = (string?)null },
                new { Content = "Make an effort to complete your projects that fall under the category of factory critical or at least ensure they don't negatively impact other players. Leave things in a manageable state", DetailedDescription = (string?)null }
            };

            for (int i = 0; i < serverRulesList.Length; i++)
            {
                context.Rules.Add(new Rule
                {
                    CategoryId = serverRules.Id,
                    Content = serverRulesList[i].Content,
                    DetailedDescription = serverRulesList[i].DetailedDescription,
                    DisplayOrder = i + 1,
                    IsEnabled = true
                });
            }

            await context.SaveChangesAsync();
            logger.LogInformation("Rules system seeded with default data.");
        }
    }
}
