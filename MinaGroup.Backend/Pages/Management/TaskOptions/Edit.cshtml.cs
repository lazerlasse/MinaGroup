using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MinaGroup.Backend.Data;
using MinaGroup.Backend.Models;
using System.Linq;
using System.Threading.Tasks;

namespace MinaGroup.Backend.Pages.Management.TaskOptions
{
    [Authorize(Roles = "Admin,Leder")]
    public class EditModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public EditModel(AppDbContext context, UserManager<AppUser> userManager)
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

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return Unauthorized();

            if (currentUser.OrganizationId == null)
                return Forbid();

            var taskoption = await _context.TaskOptions
                .FirstOrDefaultAsync(t =>
                    t.TaskOptionId == TaskOption.TaskOptionId &&
                    t.OrganizationId == currentUser.OrganizationId);

            if (taskoption == null)
                return NotFound();

            // Kun navnet må rettes her – org styres centralt
            taskoption.TaskName = TaskOption.TaskName;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TaskOptionExists(TaskOption.TaskOptionId, currentUser.OrganizationId.Value))
                    return NotFound();

                throw;
            }

            return RedirectToPage("./Index");
        }

        private bool TaskOptionExists(int id, int organizationId)
        {
            return _context.TaskOptions.Any(e =>
                e.TaskOptionId == id &&
                e.OrganizationId == organizationId);
        }
    }
}