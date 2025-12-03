using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MinaGroup.Backend.Data;
using MinaGroup.Backend.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MinaGroup.Backend.Pages.Management.TaskOptions
{
    // Kun Admin og Leder arbejder med org-specifikke opgaver
    [Authorize(Roles = "Admin,Leder")]
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public IndexModel(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IList<TaskOption> TaskOption { get; set; } = new List<TaskOption>();

        public async Task<IActionResult> OnGetAsync()
        {
            // Hent nuværende bruger
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return Unauthorized();

            if (currentUser.OrganizationId == null)
                return Forbid();

            var orgId = currentUser.OrganizationId.Value;

            TaskOption = await _context.TaskOptions
                .Where(t => t.OrganizationId == orgId)
                .OrderBy(t => t.TaskName)
                .ToListAsync();

            return Page();
        }
    }
}