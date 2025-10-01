using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MinaGroup.Backend.Models;

namespace MinaGroup.Backend.Pages.Management.RoleManagement
{
    [Authorize(Roles = "SysAdmin")]
    public class IndexModel : PageModel
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<AppUser> _userManager;

        public IndexModel(RoleManager<IdentityRole> roleManager, UserManager<AppUser> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }

        public List<RoleViewModel> Roles { get; set; } = [];

        public async Task OnGetAsync()
        {
            var roles = _roleManager.Roles.ToList();

            foreach (var role in roles)
            {
                if (!string.IsNullOrEmpty(role.Name))
                {
                    var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name);
                    Roles.Add(new RoleViewModel
                    {
                        Id = role.Id,
                        Name = role.Name!,
                        UserCount = usersInRole.Count
                    });
                }
            }
        }

        public class RoleViewModel
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public int UserCount { get; set; }
        }
    }
}