using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MinaGroup.Backend.Data;
using MinaGroup.Backend.Models;
using System.Threading.Tasks;

namespace MinaGroup.Backend.Pages.Management.TaskOptions
{
    [Authorize(Roles = "Admin,Leder")]
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public CreateModel(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public TaskOption TaskOption { get; set; } = new TaskOption();

        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return Unauthorized();

            if (currentUser.OrganizationId == null)
                return Forbid();

            // 🔒 Force org-scope uanset hvad der kommer fra klienten
            TaskOption.OrganizationId = currentUser.OrganizationId.Value;

            _context.TaskOptions.Add(TaskOption);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}