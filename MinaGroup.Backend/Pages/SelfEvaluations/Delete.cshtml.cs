using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MinaGroup.Backend.Data;
using MinaGroup.Backend.Helpers;
using MinaGroup.Backend.Models;

namespace MinaGroup.Backend.Pages.SelfEvaluations
{
    [Authorize(Roles = "Admin")]
    public class DeleteModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DeleteModel> _logger;
        private readonly UserManager<AppUser> _userManager;

        public DeleteModel(
            AppDbContext context,
            ILogger<DeleteModel> logger,
            UserManager<AppUser> userManager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
        }

        [BindProperty]
        public SelfEvaluation SelfEvaluation { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "Ingen evaluering valgt til sletning.";
                return RedirectToPage("./Index");
            }

            try
            {
                var currentUser = await _userManager.GetCurrentUserWithOrganizationAsync(User);
                if (currentUser == null)
                {
                    TempData["ErrorMessage"] =
                        "Den aktuelle bruger kunne ikke indlæses eller er ikke tilknyttet en organisation.";
                    return Unauthorized();
                }

                var orgId = currentUser.OrganizationId!.Value;

                var selfevaluation = await _context.SelfEvaluations
                    .Include(e => e.User)
                    .Include(e => e.ApprovedByUser)
                    .FirstOrDefaultAsync(m =>
                        m.Id == id.Value &&
                        m.User != null &&
                        m.User.OrganizationId == orgId);

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
                var currentUser = await _userManager.GetCurrentUserWithOrganizationAsync(User);
                if (currentUser == null)
                {
                    TempData["ErrorMessage"] =
                        "Den aktuelle bruger kunne ikke indlæses eller er ikke tilknyttet en organisation.";
                    return Unauthorized();
                }

                var orgId = currentUser.OrganizationId!.Value;

                var selfevaluation = await _context.SelfEvaluations
                    .Include(e => e.User)
                    .FirstOrDefaultAsync(e =>
                        e.Id == id.Value &&
                        e.User != null &&
                        e.User.OrganizationId == orgId);

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