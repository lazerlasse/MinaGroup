using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MinaGroup.Backend.Pages.SystemManagement.RoleManagement
{
    [Authorize(Roles = "SysAdmin")]
    public class EditModel : PageModel
    {
        private readonly RoleManager<IdentityRole> _roleManager;

        public EditModel(RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required]
            public string Id { get; set; } = string.Empty;

            [Required]
            [Display(Name = "Rollenavn")]
            [StringLength(256, MinimumLength = 2)]
            public string Name { get; set; } = string.Empty;

            public bool IsCoreRole { get; set; }
        }

        private static bool IsCoreRoleName(string? roleName)
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

            Input = new InputModel
            {
                Id = role.Id,
                Name = role.Name ?? string.Empty,
                IsCoreRole = IsCoreRoleName(role.Name)
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var role = await _roleManager.FindByIdAsync(Input.Id);
            if (role == null)
                return NotFound();

            var isCore = IsCoreRoleName(role.Name);

            // Vi tillader ikke at omdøbe core-rollerne, da de bruges i [Authorize(Roles = "...")].
            if (isCore && !string.Equals(role.Name, Input.Name.Trim()))
            {
                ModelState.AddModelError(string.Empty,
                    "Systemroller kan ikke omdøbes, da de anvendes i systemets sikkerhedskonfiguration.");
                Input.IsCoreRole = true;
                Input.Name = role.Name ?? Input.Name;
                return Page();
            }

            var newName = Input.Name.Trim();

            if (!string.Equals(role.Name, newName))
            {
                // Tjek om en anden rolle allerede har det navn
                var existingRole = await _roleManager.FindByNameAsync(newName);
                if (existingRole != null && existingRole.Id != role.Id)
                {
                    ModelState.AddModelError(string.Empty, "Der findes allerede en rolle med dette navn.");
                    Input.IsCoreRole = isCore;
                    return Page();
                }

                role.Name = newName;
                var result = await _roleManager.UpdateAsync(role);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(string.Empty, error.Description);

                    Input.IsCoreRole = isCore;
                    return Page();
                }
            }

            return RedirectToPage("Index");
        }
    }
}