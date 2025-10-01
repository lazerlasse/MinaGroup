using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MinaGroup.Backend.Data;
using MinaGroup.Backend.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MinaGroup.Backend.Pages.Management.SelfEvaluations
{
    [Authorize(Roles = "Admin,Leder")]
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(AppDbContext context, UserManager<AppUser> userManager, ILogger<CreateModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // Route values (bruges til visning og readonly)
        [BindProperty(SupportsGet = true)]
        public string? RouteUserId { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? RouteEvaluationDate { get; set; }

        [BindProperty]
        public SelfEvaluation SelfEvaluation { get; set; } = new SelfEvaluation();

        // Dropdown/muligheder
        public List<string> ArrivalOptions { get; } = ["Intet valgt", "Til tiden", "Forsent", "Aftalt forsinkelse"];
        public List<string> CollaborationOptions { get; } = ["Intet valgt", "Godt", "Okay", "Dårligt"];
        public List<string> AssistanceOptions { get; } = ["Intet valgt", "Klarer det selv", "Lidt hjælp", "Meget hjælp"];
        public List<string> AidOptions { get; } = ["Nej", "Ja – hvilke?", "Har brug for noget – hvad?"];

        // Til visning
        public string? UserFullName { get; set; } = string.Empty;
        public List<TaskOption>? TaskOptions { get; set; } = [];

        public IActionResult OnGet(string? userId, string? evaluationDate)
        {
            try
            {
                if (!string.IsNullOrEmpty(userId))
                {
                    RouteUserId = userId;
                    SelfEvaluation.UserId = userId;

                    var user = _context.Users.FirstOrDefault(u => u.Id == userId);
                    if (user != null)
                    {
                        UserFullName = user.GetType().GetProperty("FullName") != null
                            ? (string?)user.GetType().GetProperty("FullName")!.GetValue(user)
                            : $"{user.FirstName} {user.LastName}";
                    }
                }

                if (!string.IsNullOrEmpty(evaluationDate) && DateTime.TryParse(evaluationDate, out var d))
                {
                    RouteEvaluationDate = d.Date;
                    SelfEvaluation.EvaluationDate = d.Date;
                }

                if (RouteEvaluationDate == null)
                {
                    SelfEvaluation.EvaluationDate = DateTime.UtcNow.Date;
                }

                TaskOptions = _context.TaskOptions.AsNoTracking().OrderBy(t => t.TaskName).ToList();

                if (string.IsNullOrEmpty(RouteUserId))
                {
                    ViewData["UserId"] = new SelectList(
                        _context.Users.OrderBy(u => u.FirstName).ThenBy(u => u.LastName).ToList(),
                        "Id",
                        "FullName"
                    );
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fejl ved indlæsning af Create SelfEvaluation siden.");
                TempData["ErrorMessage"] = "Der opstod en uventet fejl ved indlæsning af siden.";
                return RedirectToPage("./Index");
            }
        }

        public async Task<IActionResult> OnPostAsync([FromForm] List<int>? SelectedTaskIds)
        {
            ModelState.Remove(nameof(SelfEvaluation) + ".User");

            if (!ModelState.IsValid)
            {
                TaskOptions = _context.TaskOptions.AsNoTracking().OrderBy(t => t.TaskName).ToList();
                if (string.IsNullOrEmpty(SelfEvaluation.UserId))
                {
                    ViewData["UserId"] = new SelectList(
                        _context.Users.OrderBy(u => u.FirstName).ThenBy(u => u.LastName).ToList(),
                        "Id",
                        "FullName"
                    );
                }
                return Page();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                _logger.LogWarning("Forsøg på at oprette SelfEvaluation uden gyldig bruger.");
                TempData["ErrorMessage"] = "Den aktuelle bruger kunne ikke indlæses.";
                return RedirectToPage("./Index");
            }

            try
            {
                // Beregn total tid
                if (SelfEvaluation.ArrivalTime.HasValue && SelfEvaluation.DepartureTime.HasValue)
                {
                    var total = SelfEvaluation.DepartureTime.Value - SelfEvaluation.ArrivalTime.Value;

                    if (SelfEvaluation.HadBreak && SelfEvaluation.BreakDuration.HasValue)
                        total -= SelfEvaluation.BreakDuration.Value;

                    if (total < TimeSpan.Zero)
                        total = TimeSpan.Zero;

                    SelfEvaluation.TotalHours = total;
                }

                // Godkend automatisk hvis udfyldt korrekt
                if (SelfEvaluation.IsSick && !string.IsNullOrEmpty(SelfEvaluation.SickReason))
                {
                    SelfEvaluation.IsApproved = true;
                    SelfEvaluation.ApprovedByUserId = currentUser.Id;
                }

                if (SelfEvaluation.IsNoShow && !string.IsNullOrEmpty(SelfEvaluation.NoShowReason))
                {
                    SelfEvaluation.IsApproved = true;
                    SelfEvaluation.ApprovedByUserId = currentUser.Id;
                }

                SelfEvaluation.LastUpdated = DateTime.UtcNow;

                if (SelectedTaskIds is { Count: > 0 })
                {
                    SelfEvaluation.SelectedTask = _context.TaskOptions
                        .Where(t => SelectedTaskIds.Contains(t.TaskOptionId))
                        .ToList();
                }

                _context.SelfEvaluations.Add(SelfEvaluation);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Evalueringen blev oprettet med succes.";
                return RedirectToPage("./Index");
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Databasefejl ved oprettelse af SelfEvaluation for bruger {UserId}", SelfEvaluation.UserId);
                TempData["ErrorMessage"] = "Der opstod en databasefejl. Prøv igen.";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Uventet fejl ved oprettelse af SelfEvaluation for bruger {UserId}", SelfEvaluation.UserId);
                TempData["ErrorMessage"] = "Der opstod en uventet fejl. Kontakt support hvis problemet fortsætter.";
                return RedirectToPage("./Index");
            }
        }
    }
}