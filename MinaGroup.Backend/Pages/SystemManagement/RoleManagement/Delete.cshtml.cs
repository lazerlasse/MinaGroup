using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MinaGroup.Backend.Pages.SystemManagement.RoleManagement
{
    [Authorize(Roles = "SysAdmin")]
    public class DeleteModel : PageModel
    {
        private readonly RoleManager<IdentityRole> _roleManager;

        public DeleteModel(RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
        }

        public IdentityRole? Role { get; set; }

        [BindProperty]
        public string Id { get; set; } = string.Empty;

        private static bool IsCoreRole(string? roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                return false;

            return roleName is "SysAdmin" or "Admin" or "Leder" or "Borger";
        }

        public async Task<IActionResult> OnGetAsync(string? id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
                return NotFound();

            Role = role;
            Id = role.Id;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Id))
                return NotFound();

            var role = await _roleManager.FindByIdAsync(Id);
            if (role == null)
                return NotFound();

            if (IsCoreRole(role.Name))
            {
                ModelState.AddModelError(string.Empty, "Systemroller kan ikke slettes.");
                Role = role;
                return Page();
            }

            var result = await _roleManager.DeleteAsync(role);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                Role = role;
                return Page();
            }

            return RedirectToPage("Index");
        }
    }
}