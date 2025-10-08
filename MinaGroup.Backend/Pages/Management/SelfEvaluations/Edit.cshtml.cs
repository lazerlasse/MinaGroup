using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MinaGroup.Backend.Data;
using MinaGroup.Backend.Helpers;
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

        [BindProperty]
        public List<int> SelectedTaskIds { get; set; } = [];

        public List<TaskOption> TaskOptions { get; set; } = [];

        public List<string> ArrivalOptions { get; set; } = ["Intet valgt", "Til tiden", "Forsent", "Aftalt forsinkelse"];
        public List<string> CollaborationOptions { get; set; } = ["Intet valgt", "Godt", "Okay", "Dårligt"];
        public List<string> AssistanceOptions { get; set; } = ["Intet valgt", "Klarer det selv", "Lidt hjælp", "Meget hjælp"];
        public List<string> AidOptions { get; set; } = ["Nej", "Ja – hvilke?", "Har brug for noget – hvad?"];

        public async Task<IActionResult> OnGetAsync(int id)
        {
            if (!User.IsInRole("Admin"))
                return Unauthorized();

            try
            {
                var evaluation = await _context.SelfEvaluations
                    .Include(e => e.User)
                    .Include(e => e.ApprovedByUser)
                    .Include(e => e.SelectedTask)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (evaluation == null)
                {
                    TempData["ErrorMessage"] = "Evalueringen kunne ikke findes.";
                    return RedirectToPage("./Index");
                }

                // Hent alle task options
                TaskOptions = await _context.TaskOptions.AsNoTracking().OrderBy(t => t.TaskName).ToListAsync();

                // Sæt valgte IDs
                SelectedTaskIds = evaluation.SelectedTask.Select(t => t.TaskOptionId).ToList();

                Evaluation = evaluation;
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fejl under hentning af selvevaluering {Id}", id);
                TempData["ErrorMessage"] = "Der opstod en fejl under indlæsning af selvevalueringen.";
                return RedirectToPage("./Index");
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!User.IsInRole("Admin"))
                return Forbid();

            if (!ModelState.IsValid)
            {
                await ReloadPageDataAsync(Evaluation.Id);

                TempData["ErrorMessage"] = "Der opstod en uventet fejl, forsøg venligst igen!.";

                return Page();
            }

            try
            {
                var evalInDb = await _context.SelfEvaluations
                    .Include(e => e.SelectedTask)
                    .FirstOrDefaultAsync(e => e.Id == Evaluation.Id);

                if (evalInDb == null)
                {
                    TempData["ErrorMessage"] = "Evalueringen kunne ikke findes.";
                    return RedirectToPage("./Index");
                }

                // Opdater felter
                evalInDb.IsSick = Evaluation.IsSick;
                evalInDb.SickReason = Evaluation.SickReason;
                evalInDb.IsNoShow = Evaluation.IsNoShow;
                evalInDb.NoShowReason = Evaluation.NoShowReason;
                evalInDb.IsOffWork = Evaluation.IsOffWork;
                evalInDb.OffWorkReason = Evaluation.OffWorkReason;
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
                evalInDb.LastUpdated = DateTime.Now;

                // --- Opdater mange-til-mange relationer ---
                evalInDb.SelectedTask.Clear();

                if (SelectedTaskIds is { Count: > 0 })
                {
                    var selectedTasks = await _context.TaskOptions
                        .Where(t => SelectedTaskIds.Contains(t.TaskOptionId))
                        .ToListAsync();

                    foreach (var task in selectedTasks)
                    {
                        if (_context.Entry(task).State == EntityState.Detached)
                            _context.TaskOptions.Attach(task);

                        evalInDb.SelectedTask.Add(task);
                    }
                }

                // Beregn total tid igen ved ændringer
                if (Evaluation.ArrivalTime.HasValue && Evaluation.DepartureTime.HasValue)
                    evalInDb.TotalHours = CalculateTotalWorkHours.CalculateTotalHours(Evaluation.ArrivalTime.Value, Evaluation.DepartureTime.Value, Evaluation.BreakDuration);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Evalueringen blev opdateret.";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fejl under opdatering af selvevaluering {Id}", Evaluation.Id);
                TempData["ErrorMessage"] = "Der opstod en fejl under opdatering af selvevalueringen.";
                await ReloadPageDataAsync(Evaluation.Id);
                return Page();
            }
        }

        private async Task ReloadPageDataAsync(int evalId)
        {
            Evaluation = await _context.SelfEvaluations
                .Include(e => e.User)
                .Include(e => e.ApprovedByUser)
                .Include(e => e.SelectedTask)
                .FirstOrDefaultAsync(e => e.Id == evalId) ?? new SelfEvaluation();

            TaskOptions = await _context.TaskOptions.AsNoTracking().OrderBy(t => t.TaskName).ToListAsync();
            SelectedTaskIds = Evaluation.SelectedTask.Select(t => t.TaskOptionId).ToList();
        }
    }
}