using System;
using System.Collections.Generic;
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
    public class EditModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly ILogger<EditModel> _logger;

        public EditModel(AppDbContext context, ILogger<EditModel> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [BindProperty]
        public SelfEvaluation Evaluation { get; set; } = new();

        public List<string> ArrivalOptions { get; set; } = ["Intet valgt", "Til tiden", "Forsent", "Aftalt forsinkelse"];
        public List<string> CollaborationOptions { get; set; } = ["Intet valgt", "Godt", "Okay", "Dårligt"];
        public List<string> AssistanceOptions { get; set; } = ["Intet valgt", "Klarer det selv", "Lidt hjælp", "Meget hjælp"];
        public List<string> AidOptions { get; set; } = ["Nej", "Ja – hvilke?", "Har brug for noget – hvad?"];

        public async Task<IActionResult> OnGetAsync(int id)
        {
            if (User == null || !User.IsInRole("Admin"))
                return Unauthorized();

            try
            {
                var evaluation = await _context.SelfEvaluations
                    .Include(e => e.User)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (evaluation == null)
                {
                    _logger.LogWarning("Edit attempt failed: No SelfEvaluation found with Id {Id}", id);
                    TempData["ErrorMessage"] = "Evalueringen kunne ikke findes.";
                    return RedirectToPage("./Index");
                }

                Evaluation = evaluation;
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching SelfEvaluation with Id {Id}", id);
                TempData["ErrorMessage"] = "Der opstod en fejl under indlæsning af selvevalueringen.";
                return RedirectToPage("./Index");
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (User == null || !User.IsInRole("Admin"))
                return Forbid();

            if (Evaluation == null)
            {
                _logger.LogWarning("OnPostAsync called with null Evaluation model.");
                TempData["ErrorMessage"] = "Ugyldig data blev sendt. Prøv igen.";
                return RedirectToPage("./Index");
            }

            try
            {
                var evalInDb = await _context.SelfEvaluations
                    .FirstOrDefaultAsync(e => e.Id == Evaluation.Id);

                if (evalInDb == null)
                {
                    _logger.LogWarning("Update attempt failed: No SelfEvaluation found with Id {Id}", Evaluation.Id);
                    TempData["ErrorMessage"] = "Evalueringen kunne ikke findes.";
                    return RedirectToPage("./Index");
                }

                // Opdater felter (kun Admin kan redigere her)
                evalInDb.IsSick = Evaluation.IsSick;
                evalInDb.IsNoShow = Evaluation.IsNoShow;
                evalInDb.NoShowReason = Evaluation.NoShowReason;
                evalInDb.ArrivalTime = Evaluation.ArrivalTime;
                evalInDb.DepartureTime = Evaluation.DepartureTime;
                evalInDb.TotalHours = Evaluation.TotalHours;
                evalInDb.HadBreak = Evaluation.HadBreak;
                evalInDb.BreakDuration = Evaluation.BreakDuration;
                evalInDb.ArrivalStatus = Evaluation.ArrivalStatus;
                evalInDb.Collaboration = Evaluation.Collaboration;
                evalInDb.Assistance = Evaluation.Assistance;
                evalInDb.Aid = Evaluation.Aid;
                evalInDb.AidDescription = Evaluation.AidDescription;
                evalInDb.HadDiscomfort = Evaluation.HadDiscomfort;
                evalInDb.DiscomfortDescription = Evaluation.DiscomfortDescription;
                evalInDb.NextMeetingNotes = Evaluation.NextMeetingNotes;
                evalInDb.CommentFromLeader = Evaluation.CommentFromLeader;

                await _context.SaveChangesAsync();

                _logger.LogInformation("SelfEvaluation with Id {Id} successfully updated by {User}", Evaluation.Id, User.Identity?.Name);
                TempData["SuccessMessage"] = "Evalueringen blev opdateret med succes.";
                return RedirectToPage("./Index");
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error while updating SelfEvaluation with Id {Id}", Evaluation.Id);
                TempData["ErrorMessage"] = "Der opstod en databasefejl. Prøv igen senere.";
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating SelfEvaluation with Id {Id}", Evaluation.Id);
                TempData["ErrorMessage"] = "Der opstod en fejl under opdatering af selvevalueringen.";
                return RedirectToPage("./Index");
            }
        }
    }
}