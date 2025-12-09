using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MinaGroup.Backend.Data;
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
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // ---------- 1) Roller ----------
            string[] roles = { "SysAdmin", "Admin", "Leder", "Borger" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    var result = await roleManager.CreateAsync(new IdentityRole(role));
                    if (!result.Succeeded)
                    {
                        throw new Exception(
                            $"Kunne ikke oprette rollen '{role}': {string.Join(", ", result.Errors)}");
                    }
                }
            }

            // ---------- 2) Standard-organisation: Mina Group ----------
            // Hentes fra config med fallback til dine ønskede værdier
            var orgName = config["ORG:NAME"] ?? "Mina Group ApS";
            var orgAddress = config["ORG:ADDRESS"] ?? "Bag Stadion 10";
            var orgTown = config["ORG:TOWN"] ?? "Korsør";

            int cvrNumber = 0;
            int postalCode = 0;

            int.TryParse(config["ORG:CVR"], out cvrNumber);
            int.TryParse(config["ORG:POSTALCODE"], out postalCode);

            if (cvrNumber == 0)
                cvrNumber = 39405474; // din default CVR
            if (postalCode == 0)
                postalCode = 4220;    // din default postnr.

            var organization = await db.Organizations
                .FirstOrDefaultAsync(o =>
                    o.Name == orgName &&
                    o.CVRNumber == cvrNumber);

            if (organization == null)
            {
                organization = new Organization
                {
                    Name = orgName,
                    CVRNumber = cvrNumber,
                    OrganizationAdress = orgAddress,
                    PostalCode = postalCode,
                    Town = orgTown,
                    Slug = "mina-group-aps"
                };

                db.Organizations.Add(organization);
                await db.SaveChangesAsync();
            }

            // ---------- 3) SysAdmin-bruger ----------
            // SysAdmin skal typisk IKKE være bundet til en organisation
            await CreateUserWithRoleAsync(
                userManager: userManager,
                email: config["SYSADM:EMAIL"],
                password: config["SYSADM:PASSWORD"],
                firstName: config["SYSADM:FIRSTNAME"],
                lastName: config["SYSADM:LASTNAME"],
                phoneNumber: config["SYSADM:PHONE"],
                role: "SysAdmin",
                organizationId: null);

            // ---------- 4) Admin-bruger i Mina Group ----------
            // Denne bruger får OrganizationId = organization.Id
            await CreateUserWithRoleAsync(
                userManager: userManager,
                email: config["ADMIN:EMAIL"],
                password: config["ADMIN:PASSWORD"],
                firstName: config["ADMIN:FIRSTNAME"],
                lastName: config["ADMIN:LASTNAME"],
                phoneNumber: config["ADMIN:PHONE"],
                role: "Admin",
                organizationId: organization.Id);
        }

        /// <summary>
        /// Opretter (eller opdaterer) en bruger og sikrer, at vedkommende har den angivne rolle.
        /// Hvis organizationId er sat, knyttes brugeren til den organisation.
        /// Idempotent: kan kaldes flere gange uden at duplikere.
        /// </summary>
        private static async Task<AppUser?> CreateUserWithRoleAsync(
            UserManager<AppUser> userManager,
            string? email,
            string? password,
            string? firstName,
            string? lastName,
            string? phoneNumber,
            string role,
            int? organizationId)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return null;

            var user = await userManager.FindByEmailAsync(email);

            if (user == null)
            {
                user = new AppUser
                {
                    UserName = email,
                    Email = email,
                    FirstName = firstName ?? string.Empty,
                    LastName = lastName ?? string.Empty,
                    PhoneNumber = phoneNumber,
                    EmailConfirmed = true,
                    OrganizationId = organizationId
                };

                var createResult = await userManager.CreateAsync(user, password);
                if (!createResult.Succeeded)
                {
                    throw new Exception(
                        $"Kunne ikke oprette bruger {email}: {string.Join(", ", createResult.Errors)}");
                }
            }
            else
            {
                // Opdater basisdata hvis de er sat i config
                bool changed = false;

                if (!string.IsNullOrWhiteSpace(firstName) && user.FirstName != firstName)
                {
                    user.FirstName = firstName;
                    changed = true;
                }

                if (!string.IsNullOrWhiteSpace(lastName) && user.LastName != lastName)
                {
                    user.LastName = lastName;
                    changed = true;
                }

                if (!string.IsNullOrWhiteSpace(phoneNumber) && user.PhoneNumber != phoneNumber)
                {
                    user.PhoneNumber = phoneNumber;
                    changed = true;
                }

                if (organizationId.HasValue && user.OrganizationId != organizationId)
                {
                    user.OrganizationId = organizationId;
                    changed = true;
                }

                if (changed)
                {
                    var updateResult = await userManager.UpdateAsync(user);
                    if (!updateResult.Succeeded)
                    {
                        throw new Exception(
                            $"Kunne ikke opdatere bruger {email}: {string.Join(", ", updateResult.Errors)}");
                    }
                }
            }

            // Sørg for at brugeren har den ønskede rolle
            if (!await userManager.IsInRoleAsync(user, role))
            {
                var roleResult = await userManager.AddToRoleAsync(user, role);
                if (!roleResult.Succeeded)
                {
                    throw new Exception(
                        $"Kunne ikke tilføje bruger {email} til rollen {role}: {string.Join(", ", roleResult.Errors)}");
                }
            }

            return user;
        }
    }
}