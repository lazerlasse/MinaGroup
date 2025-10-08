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
    [Authorize(Roles = "Admin")]
    public class DeleteModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DeleteModel> _logger;

        public DeleteModel(AppDbContext context, ILogger<DeleteModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public SelfEvaluation SelfEvaluation { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            try
            {
                var userIsInRole = User.IsInRole("Admin") || User.IsInRole("SysAdmin");
                if (!userIsInRole)
                {
                    _logger.LogWarning("Uautoriseret forsøg på at tilgå Delete SelfEvaluation siden.");
                    return Unauthorized();
                }

                if (id == null)
                {
                    return NotFound("Ingen evaluering valgt til sletning.");
                }

                var selfevaluation = await _context.SelfEvaluations
                    .Include(e => e.User)
                    .Include(e => e.ApprovedByUser)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (selfevaluation == null)
                {
                    return NotFound("Evalueringen kunne ikke findes.");
                }

                SelfEvaluation = selfevaluation;
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fejl ved indlæsning af Delete SelfEvaluation siden for id {Id}", id);
                TempData["ErrorMessage"] = "Der opstod en fejl ved indlæsning af siden.";
                return RedirectToPage("./Index");
            }
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "Ingen evaluering valgt til sletning.";
                return RedirectToPage("./Index");
            }

            try
            {
                var selfevaluation = await _context.SelfEvaluations.FindAsync(id);
                if (selfevaluation != null)
                {
                    SelfEvaluation = selfevaluation;
                    _context.SelfEvaluations.Remove(SelfEvaluation);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Evalueringen blev slettet med succes.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Evalueringen kunne ikke findes. Den kan allerede være slettet.";
                }

                return RedirectToPage("./Index");
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Databasefejl ved sletning af SelfEvaluation med id {Id}", id);
                TempData["ErrorMessage"] = "Der opstod en databasefejl under sletning. Prøv igen.";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Uventet fejl ved sletning af SelfEvaluation med id {Id}", id);
                TempData["ErrorMessage"] = "Der opstod en uventet fejl under sletning.";
                return RedirectToPage("./Index");
            }
        }
    }
}