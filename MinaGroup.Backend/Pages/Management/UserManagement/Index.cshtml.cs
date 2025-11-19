using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MinaGroup.Backend.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MinaGroup.Backend.Pages.Management.UserManagement
{
    [Authorize(Roles = "Admin,SysAdmin")]
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

        public async Task OnGetAsync(string? searchString)
        {
            CurrentFilter = searchString;

            var usersQuery = _userManager.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                usersQuery = usersQuery.Where(u =>
                    u.Email!.Contains(searchString) ||
                    u.FirstName!.Contains(searchString) ||
                    u.LastName!.Contains(searchString));
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
        }

        private bool CanCurrentUserSeeUser(IList<string> candidateRoles)
        {
            var r = new HashSet<string>(candidateRoles, StringComparer.OrdinalIgnoreCase);

            bool isSysAdmin = r.Contains("SysAdmin");
            bool isAdmin = r.Contains("Admin");
            bool isBorger = r.Contains("Borger");
            bool isLeder = r.Contains("Leder");

            // -------- SYSADMIN LOGIK --------
            // SysAdmin må se:
            //  - alle med SysAdmin
            //  - alle med Admin
            //  - alle med Leder
            //  - borgere, hvis de OGSÅ er Admin/SysAdmin/Leder
            if (User.IsInRole("SysAdmin"))
            {
                if (isSysAdmin || isAdmin || isLeder)
                    return true;

                // Ren borger (kun Borger) -> skjules
                if (isBorger && !isSysAdmin && !isAdmin && !isLeder)
                    return false;

                // Brugere uden nogen af rollerne ovenfor (meget edge-case) -> skjules
                return false;
            }

            // -------- ADMIN LOGIK --------
            // Admin må se:
            //  - Admin
            //  - Borger
            //  - Leder
            //  (og kombinationer med SysAdmin)
            if (User.IsInRole("Admin"))
            {
                if (isAdmin || isBorger || isLeder)
                    return true;

                // Ren SysAdmin (kun SysAdmin) -> skjules
                if (isSysAdmin && !isAdmin && !isBorger && !isLeder)
                    return false;

                return false;
            }

            // Andre roller har ikke adgang til brugerlisten
            return false;
        }
    }
}