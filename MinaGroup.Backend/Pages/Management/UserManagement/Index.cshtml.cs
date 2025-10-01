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
        public string CurrentFilter { get; set; } = string.Empty;

        public class UserViewModel
        {
            public string Id { get; set; } = string.Empty;
            public string? Email { get; set; } = string.Empty;
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string? PhoneNumber { get; set; } = string.Empty;
            public IList<string> Roles { get; set; } = [];
        }

        public async Task OnGetAsync(string searchString)
        {
            CurrentFilter = searchString;

            var users = _userManager.Users.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                users = users.Where(u => u.Email.Contains(searchString)
                                       || u.FirstName.Contains(searchString)
                                       || u.LastName.Contains(searchString));
            }

            var userList = await users.ToListAsync();
            Users = [];

            foreach (var user in userList)
            {
                var roles = await _userManager.GetRolesAsync(user);

                // Filtrering baseret på hvem der er logget ind
                if (User.IsInRole("Admin"))
                {
                    // Admin må IKKE se SysAdmin
                    if (roles.Contains("SysAdmin"))
                        continue;
                }

                if (User.IsInRole("SysAdmin"))
                {
                    // SysAdmin må IKKE se Borger
                    if (roles.Contains("Borger"))
                        continue;
                }

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
    }
}