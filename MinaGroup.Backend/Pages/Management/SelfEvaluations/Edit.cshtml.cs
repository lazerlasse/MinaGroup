using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MinaGroup.Backend.Data;
using MinaGroup.Backend.Models;

namespace MinaGroup.Backend.Pages.Management.SelfEvaluations
{
    public class EditModel : PageModel
    {
        private readonly AppDbContext _context;

        public EditModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public SelfEvaluation SelfEvaluation { get; set; } = default!;

        // Dropdown options (samme som i appen)
        public List<string> ArrivalOptions { get; } = ["Intet valgt", "Til tiden", "Forsent", "Aftalt forsinkelse"];
        public List<string> CollaborationOptions { get; } = ["Intet valgt", "Godt", "Okay", "Dårligt"];
        public List<string> AssistanceOptions { get; } = ["Intet valgt", "Klarer det selv", "Lidt hjælp", "Meget hjælp"];
        public List<string> AidOptions { get; } = ["Nej", "Ja – hvilke?", "Har brug for noget – hvad?"];

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var selfEvaluation = await _context.SelfEvaluations
                .Include(s => s.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (selfEvaluation == null)
            {
                return NotFound();
            }

            SelfEvaluation = selfEvaluation;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                var evaluation = await _context.SelfEvaluations.FirstOrDefaultAsync(e => e.Id == SelfEvaluation.Id);
                if (evaluation == null)
                    return NotFound("Kunne ikke indlæse data! Forsøg venligst igen.");

                evaluation.IsSick = SelfEvaluation.IsSick;
                evaluation.ArrivalTime = SelfEvaluation.ArrivalTime;
                evaluation.DepartureTime = SelfEvaluation.DepartureTime;
                evaluation.TotalHours = SelfEvaluation.TotalHours;
                evaluation.HadBreak = SelfEvaluation.HadBreak;
                evaluation.BreakDuration = SelfEvaluation.BreakDuration;
                evaluation.ArrivalStatus = SelfEvaluation.ArrivalStatus;
                evaluation.Collaboration = SelfEvaluation.Collaboration;
                evaluation.Assistance = SelfEvaluation.Assistance;
                evaluation.Aid = SelfEvaluation.Aid;
                evaluation.AidDescription = SelfEvaluation.AidDescription;
                evaluation.HadDiscomfort = SelfEvaluation.HadDiscomfort;
                evaluation.DiscomfortDescription = SelfEvaluation.DiscomfortDescription;
                evaluation.NextMeetingNotes = SelfEvaluation.NextMeetingNotes;
                evaluation.CommentFromLeader = SelfEvaluation.CommentFromLeader;
                evaluation.LastUpdated = DateTime.UtcNow;

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                // Log fx via ILogger<EditModel>
                if (!_context.SelfEvaluations.Any(e => e.Id == SelfEvaluation.Id))
                    return NotFound("Evalueringen findes ikke længere.");

                ModelState.AddModelError(string.Empty, "Der opstod en konflikt – en anden har måske rettet i denne evaluering samtidig.");
                return Page();
            }
            catch (Exception ex)
            {
                // Log exception
                ModelState.AddModelError(string.Empty, "Der opstod en uventet fejl. Kontakt support, hvis problemet fortsætter.");
                return Page();
            }

            return RedirectToPage("./Index");
        }
    }
}