using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MinaGroup.Backend.Data;
using MinaGroup.Backend.Models;
using System.Threading.Tasks;

namespace MinaGroup.Backend.Pages.Management.TaskOptions
{
    [Authorize(Roles = "Admin,Leder")]
    public class DeleteModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public DeleteModel(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public TaskOption TaskOption { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
                return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return Unauthorized();

            if (currentUser.OrganizationId == null)
                return Forbid();

            var taskoption = await _context.TaskOptions
                .FirstOrDefaultAsync(m =>
                    m.TaskOptionId == id &&
                    m.OrganizationId == currentUser.OrganizationId);

            if (taskoption == null)
                return NotFound();

            TaskOption = taskoption;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
                return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return Unauthorized();

            if (currentUser.OrganizationId == null)
                return Forbid();

            var taskoption = await _context.TaskOptions
                .FirstOrDefaultAsync(t =>
                    t.TaskOptionId == id &&
                    t.OrganizationId == currentUser.OrganizationId);

            if (taskoption != null)
            {
                _context.TaskOptions.Remove(taskoption);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}