using Microsoft.AspNetCore.Identity;
using MinaGroup.Backend.Models;
using System.Security.Claims;

namespace MinaGroup.Backend.Helpers
{
    public static class UserContextHelper
    {
        /// <summary>
        /// Henter den nuværende AppUser og sikrer,
        /// at brugeren også er tilknyttet en Organization.
        /// Returnerer null hvis noget mangler.
        /// </summary>
        public static async Task<AppUser?> GetCurrentUserWithOrganizationAsync(
            this UserManager<AppUser> userManager,
            ClaimsPrincipal principal)
        {
            var user = await userManager.GetUserAsync(principal);
            if (user == null)
                return null;

            if (!user.OrganizationId.HasValue)
                return null;

            return user;
        }

        /// <summary>
        /// Hjælper til at tjekke om en bruger er Admin/Leder/SysAdmin.
        /// (Bruges hvor det giver mening – fx på SelfEvaluations Index-siden).
        /// </summary>
        public static async Task<bool> IsAdminOrLeaderAsync(
            this UserManager<AppUser> userManager,
            AppUser user)
        {
            return await userManager.IsInRoleAsync(user, "Admin")
                   || await userManager.IsInRoleAsync(user, "Leder")
                   || await userManager.IsInRoleAsync(user, "SysAdmin");
        }
    }
}
