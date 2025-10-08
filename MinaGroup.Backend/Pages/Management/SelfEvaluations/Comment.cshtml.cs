using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MinaGroup.Backend.Data;
using MinaGroup.Backend.Models;
using System;
using System.Threading.Tasks;

namespace MinaGroup.Backend.Pages.Management.SelfEvaluations
{
    [Authorize(Roles = "Admin,Leder")]
    public class CommentModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<CommentModel> _logger;

        public CommentModel(AppDbContext context, UserManager<AppUser> userManager, ILogger<CommentModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public SelfEvaluation SelfEvaluation { get; set; } = new();

        public List<TaskOption>? TaskOptions { get; set; } = [];

        [BindProperty]
        public List<int> SelectedTaskIds { get; set; } = [];

        public async Task<IActionResult> OnGetAsync(int id)
        {
            try
            {
                // Brug korrekt OR-logik til rollecheck
                if (!(User.IsInRole("Leder") || User.IsInRole("Admin")))
                {
                    _logger.LogWarning("Unauthorized adgangsforsøg til SelfEvaluation {Id}", id);
                    TempData["ErrorMessage"] = "Du har ikke adgang til denne side.";
                    return Unauthorized();
                }

                var evaluation = await _context.SelfEvaluations
                    .Include(s => s.User)
                    .Include(s => s.ApprovedByUser)
                    .Include(u => u.SelectedTask)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (evaluation == null)
                {
                    TempData["ErrorMessage"] = "Skemaet kunne ikke indlæses, prøv venligst igen!";
                    return NotFound();
                }

                TaskOptions = _context.TaskOptions.AsNoTracking().OrderBy(t => t.TaskName).ToList();

                // Sæt valgte IDs
                SelectedTaskIds = evaluation.SelectedTask.Select(t => t.TaskOptionId).ToList();

                SelfEvaluation = evaluation;

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fejl ved indlæsning af evaluering med ID: {Id}", id);
                TempData["ErrorMessage"] = "Der opstod en uventet fejl ved indlæsning af skemaet.";
                return RedirectToPage("Index");
            }
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            try
            {
                var evaluation = await _context.SelfEvaluations.FindAsync(id);
                if (evaluation == null)
                {
                    TempData["ErrorMessage"] = "Evalueringsskemaet kunne ikke indlæses!";
                    return NotFound();
                }

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    TempData["ErrorMessage"] = "Den aktuelle bruger kunne ikke indlæses!";
                    return Unauthorized();
                }

                // Opdater felter afhængig af status
                if (SelfEvaluation.IsSick)
                {
                    evaluation.SickReason = SelfEvaluation.SickReason;
                }
                else if (SelfEvaluation.IsNoShow)
                {
                    evaluation.NoShowReason = SelfEvaluation.NoShowReason;
                }
                else if (SelfEvaluation.IsOffWork)
                {
                    evaluation.OffWorkReason = SelfEvaluation.OffWorkReason;
                }
                else
                {
                    evaluation.CommentFromLeader = SelfEvaluation.CommentFromLeader;
                }

                evaluation.IsApproved = true;
                evaluation.ApprovedByUserId = currentUser.Id;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Evalueringen blev opdateret og godkendt med succes.";

                return RedirectToPage("Index");
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Databasefejl ved opdatering af SelfEvaluation {Id}", id);
                TempData["ErrorMessage"] = "Der opstod en fejl ved gemning af ændringerne. Prøv igen.";
                return RedirectToPage("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Uventet fejl ved opdatering af SelfEvaluation {Id}", id);
                TempData["ErrorMessage"] = "Der opstod en uventet fejl. Kontakt support hvis problemet fortsætter.";
                return RedirectToPage("Index");
            }
        }
    }
}