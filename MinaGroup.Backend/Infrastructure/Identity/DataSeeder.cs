using Microsoft.AspNetCore.Identity;
using MinaGroup.Backend.Models;

namespace MinaGroup.Backend.Infrastructure.Identity
{
    public static class DataSeeder
    {
        public static async Task SeedAdminUserAsync(IServiceProvider serviceProvider)
        {
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DataSeeder");

            using var scope = serviceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            string adminEmail = config["Admin:Email"];
            string adminPassword = config["Admin:Password"];
            string adminRole = "Admin";

            // Opret rolle hvis ikke findes
            if (!await roleManager.RoleExistsAsync(adminRole))
            {
                logger.LogInformation("Opretter rolle: {role}", adminRole);
                await roleManager.CreateAsync(new IdentityRole(adminRole));
            }

            // Find bruger
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                logger.LogInformation("Adminbruger findes ikke – opretter ny.");

                adminUser = new AppUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (!result.Succeeded)
                {
                    throw new Exception("Kunne ikke oprette adminbruger: " +
                        string.Join(", ", result.Errors));
                }
            }

            // Tildel rolle
            if (!await userManager.IsInRoleAsync(adminUser, adminRole))
            {
                logger.LogInformation("Tildeler rolle '{role}' til adminbruger.", adminRole);
                await userManager.AddToRoleAsync(adminUser, adminRole);
            }

            logger.LogInformation("Adminbruger klar.");
        }
    }
}
