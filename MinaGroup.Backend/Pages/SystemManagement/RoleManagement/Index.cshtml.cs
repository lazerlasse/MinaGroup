using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace MinaGroup.Backend.Pages.SystemManagement.RoleManagement
{
    [Authorize(Roles = "SysAdmin")]
    public class IndexModel : PageModel
    {
        private readonly RoleManager<IdentityRole> _roleManager;

        public IndexModel(RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
        }

        public IList<IdentityRole> Roles { get; set; } = new List<IdentityRole>();

        public async Task OnGetAsync()
        {
            Roles = await _roleManager.Roles
                .OrderBy(r => r.Name)
                .ToListAsync();
        }

        /// <summary>
        /// Hjælper til at markere system-roller, som ikke må slettes/ændres.
        /// </summary>
        public static bool IsCoreRole(string? roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                return false;

            return roleName is "SysAdmin" or "Admin" or "Leder" or "Borger";
        }
    }
}