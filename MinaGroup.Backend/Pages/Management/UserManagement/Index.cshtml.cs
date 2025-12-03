using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MinaGroup.Backend.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MinaGroup.Backend.Pages.Management.UserManagement
{
    [Authorize(Roles = "Admin,Leder")] // 🔁 Kun Admin + Leder
    public class IndexModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;

        public IndexModel(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        public IList<UserViewModel> Users { get; set; } = [];
        public string? CurrentFilter { get; set; } = string.Empty;

        public class UserViewModel
        {
            public string Id { get; set; } = string.Empty;
            public string? Email { get; set; } = string.Empty;
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string? PhoneNumber { get; set; } = string.Empty;
            public IList<string> Roles { get; set; } = [];
        }

        public async Task<IActionResult> OnGetAsync(string? searchString)
        {
            CurrentFilter = searchString;

            // 🔹 Find den aktuelle bruger inkl. OrganizationId
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            if (currentUser.OrganizationId == null)
            {
                // SysAdmin bør håndtere at knytte admin/leder til en org
                TempData["ErrorMessage"] = "Din bruger er ikke tilknyttet en organisation. Kontakt systemadministrator.";
                Users = [];
                return Page();
            }

            // 🔹 Start med kun brugere i samme organisation
            var usersQuery = _userManager.Users
                .Where(u => u.OrganizationId == currentUser.OrganizationId);

            // 🔹 Søgning
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                usersQuery = usersQuery.Where(u =>
                    (u.Email ?? "").Contains(searchString) ||
                    (u.FirstName ?? "").Contains(searchString) ||
                    (u.LastName ?? "").Contains(searchString));
            }

            var userList = await usersQuery.ToListAsync();
            Users = [];

            foreach (var user in userList)
            {
                var roles = await _userManager.GetRolesAsync(user);

                if (!CanCurrentUserSeeUser(roles))
                    continue;

                Users.Add(new UserViewModel
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    PhoneNumber = user.PhoneNumber,
                    Roles = roles
                });
            }

            return Page();
        }

        private bool CanCurrentUserSeeUser(IList<string> candidateRoles)
        {
            var r = new HashSet<string>(candidateRoles, StringComparer.OrdinalIgnoreCase);

            bool isSysAdmin = r.Contains("SysAdmin");
            bool isAdmin = r.Contains("Admin");
            bool isLeder = r.Contains("Leder");
            bool isBorger = r.Contains("Borger");

            // 🔒 Ingen skal se SysAdmin her
            if (isSysAdmin)
                return false;

            // -------- ADMIN LOGIK --------
            // Admin må se:
            //  - Admin
            //  - Leder
            //  - Borger
            //  (kun inden for egen organisation – det er håndteret i query'en)
            if (User.IsInRole("Admin"))
            {
                return isAdmin || isLeder || isBorger;
            }

            // -------- LEDER LOGIK --------
            // Leder må kun se borgere i egen organisation
            if (User.IsInRole("Leder"))
            {
                return isBorger;
            }

            // Andre roller har ikke adgang til brugerlisten
            return false;
        }
    }
}