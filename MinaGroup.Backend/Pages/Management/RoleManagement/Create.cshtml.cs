using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace MinaGroup.Backend.Pages.Management.RoleManagement
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
            [Required(ErrorMessage = "Rollens navn er påkrævet")]
            [Display(Name = "Rolles navn")]
            public string Name { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            if (await _roleManager.RoleExistsAsync(Input.Name))
            {
                ModelState.AddModelError(string.Empty, "Denne rolle findes allerede.");
                return Page();
            }

            var result = await _roleManager.CreateAsync(new IdentityRole(Input.Name));
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Rollen blev oprettet.";
                return RedirectToPage("./Index");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return Page();
        }
    }
}