using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MinaGroup.Backend.Data;
using MinaGroup.Backend.Models;

namespace MinaGroup.Backend.Pages.Management.SelfEvaluations
{
    [Authorize(Roles = "Admin,SysAdmin")]
    public class DeleteModel : PageModel
    {
        private readonly AppDbContext _context;

        public DeleteModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public SelfEvaluation SelfEvaluation { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            var userIsInRole = User.IsInRole("Admin") || User.IsInRole("SysAdmin");

            if (!userIsInRole)
                return Unauthorized();

            if (id == null)
            {
                return NotFound();
            }

            var selfevaluation = await _context.SelfEvaluations
                .Include(e => e.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (selfevaluation == null)
            {
                return NotFound();
            }
            else
            {
                SelfEvaluation = selfevaluation;
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var selfevaluation = await _context.SelfEvaluations.FindAsync(id);
            if (selfevaluation != null)
            {
                SelfEvaluation = selfevaluation;
                _context.SelfEvaluations.Remove(SelfEvaluation);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
