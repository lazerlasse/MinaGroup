using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using MinaGroup.Backend.Models;
using System;
using System.Threading.Tasks;

namespace MinaGroup.Backend.Infrastructure.Identity
{
    public static class DataSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            // Definer rollerne
            string[] roles = { "SysAdmin", "Admin", "Leder", "Borger" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // SysAdmin
            await CreateUserWithRole(userManager,
                email: config["SYSADM:EMAIL"],
                password: config["SYSADM:PASSWORD"],
                firstName: config["SYSADM:FIRSTNAME"],
                lastName: config["SYSADM:LASTNAME"],
                role: "SysAdmin");

            // Admin
            await CreateUserWithRole(userManager,
                email: config["ADMIN:EMAIL"],
                password: config["ADMIN:PASSWORD"],
                firstName: config["ADMIN:FIRSTNAME"],
                lastName: config["ADMIN:LASTNAME"],
                role: "Admin");

            // Leder
            await CreateUserWithRole(userManager,
                email: config["LEDER:EMAIL"],
                password: config["LEDER:PASSWORD"],
                firstName: config["LEDER:FIRSTNAME"],
                lastName: config["LEDER:LASTNAME"],
                role: "Leder");

            // Borger
            await CreateUserWithRole(userManager,
                email: config["BORGER:EMAIL"],
                password: config["BORGER:PASSWORD"],
                firstName: config["BORGER:FIRSTNAME"],
                lastName: config["BORGER:LASTNAME"],
                role: "Borger");
        }

        private static async Task CreateUserWithRole(UserManager<AppUser> userManager, string email, string password, string firstName, string lastName, string role)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password)) return;

            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new AppUser
                {
                    UserName = email,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(user, password);

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, role);
                }
                else
                {
                    throw new Exception($"Kunne ikke oprette bruger {email}: {string.Join(", ", result.Errors)}");
                }
            }
        }
    }
}