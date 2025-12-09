using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MinaGroup.Backend.Pages.SystemManagement.RoleManagement
{
    [Authorize(Roles = "SysAdmin")]
    public class CreateModel : PageModel
    {
        private readonly RoleManager<IdentityRole> _roleManager;

        public CreateModel(RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required]
            [Display(Name = "Rollenavn")]
            [StringLength(256, MinimumLength = 2)]
            public string Name { get; set; } = string.Empty;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var roleName = Input.Name.Trim();

            // Tjek om rollen allerede findes
            if (await _roleManager.RoleExistsAsync(roleName))
            {
                ModelState.AddModelError(string.Empty, "Der findes allerede en rolle med dette navn.");
                return Page();
            }

            var result = await _roleManager.CreateAsync(new IdentityRole(roleName));

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return Page();
            }

            return RedirectToPage("Index");
        }
    }
}