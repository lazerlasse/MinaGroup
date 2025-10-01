using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MinaGroup.Backend.Models;

namespace MinaGroup.Backend.Pages.Management.UserManagement
{
    [Authorize(Roles = "Admin,SysAdmin")]
    public class DeleteModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;

        public DeleteModel(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        // ✅ Undgå "new" – kald den fx SelectedUser for klarhed
        public AppUser? SelectedUser { get; set; }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            SelectedUser = user;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                SelectedUser = user;
                return Page();
            }

            return RedirectToPage("./Index");
        }
    }
}