using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MinaGroup.Backend.Data;
using MinaGroup.Backend.Models;

namespace MinaGroup.Backend.Pages.Management.SelfEvaluations
{
    [Authorize(Roles = "Admin,Leder")]
    public class DetailsModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DetailsModel> _logger;

        public DetailsModel(AppDbContext context, ILogger<DetailsModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public SelfEvaluation SelfEvaluation { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "Ingen evaluering valgt til visning.";
                return RedirectToPage("./Index");
            }

            try
            {
                var selfevaluation = await _context.SelfEvaluations
                    .Include(u => u.User)
                    .Include(u => u.ApprovedByUser)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (selfevaluation == null)
                {
                    TempData["ErrorMessage"] = "Evalueringen kunne ikke findes.";
                    return RedirectToPage("./Index");
                }

                SelfEvaluation = selfevaluation;
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fejl ved hentning af detaljer for SelfEvaluation med id {Id}", id);
                TempData["ErrorMessage"] = "Der opstod en fejl ved hentning af evalueringen.";
                return RedirectToPage("./Index");
            }
        }
    }
}