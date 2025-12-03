using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MinaGroup.Backend.Models;

namespace MinaGroup.Backend.Pages.Management.UserManagement
{
    // 🔐 Kun Admin & Leder – SysAdmin bruger senere SystemManagement
    [Authorize(Roles = "Admin,Leder")]
    public class DeleteModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;

        public DeleteModel(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        // Vises i UI
        public AppUser? SelectedUser { get; set; }

        private async Task<(AppUser currentUser, AppUser targetUser, IList<string> targetRoles)?> LoadAndValidateAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            // Hent nuværende bruger (Admin/Leder)
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                throw new InvalidOperationException("Den aktuelle bruger kunne ikke indlæses.");

            if (currentUser.OrganizationId == null)
                throw new UnauthorizedAccessException("Du er ikke tilknyttet en organisation.");

            // Hent brugeren der skal slettes
            var targetUser = await _userManager.FindByIdAsync(id);
            if (targetUser == null)
                return null;

            // Org-scope: kun samme organisation
            if (targetUser.OrganizationId != currentUser.OrganizationId)
                throw new UnauthorizedAccessException("Du har ikke adgang til denne bruger.");

            var targetRoles = await _userManager.GetRolesAsync(targetUser);

            // Leder må kun håndtere borgere – ikke Admin/Leder/SysAdmin
            if (User.IsInRole("Leder") &&
                (targetRoles.Contains("Admin") || targetRoles.Contains("Leder") || targetRoles.Contains("SysAdmin")))
            {
                throw new UnauthorizedAccessException("Leder må kun slette borgere i egen organisation.");
            }

            return (currentUser, targetUser, targetRoles);
        }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            try
            {
                var result = await LoadAndValidateAsync(id);
                if (result == null)
                    return NotFound();

                SelectedUser = result.Value.targetUser;
                return Page();
            }
            catch (UnauthorizedAccessException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return Unauthorized();
            }
        }

        public async Task<IActionResult> OnPostAsync(string id)
        {
            try
            {
                var result = await LoadAndValidateAsync(id);
                if (result == null)
                    return NotFound();

                var user = result.Value.targetUser;

                var deleteResult = await _userManager.DeleteAsync(user);
                if (!deleteResult.Succeeded)
                {
                    foreach (var error in deleteResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }

                    SelectedUser = user;
                    return Page();
                }

                return RedirectToPage("./Index");
            }
            catch (UnauthorizedAccessException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return Unauthorized();
            }
        }
    }
}