using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MinaGroup.Backend.Data;
using MinaGroup.Backend.Helpers;
using MinaGroup.Backend.Models;
using MinaGroup.Backend.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MinaGroup.Backend.Pages.SelfEvaluations
{
    [Authorize(Roles = "Admin,Leder")]
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<CreateModel> _logger;
        private readonly ICryptoService _cryptoService;

        public CreateModel(
            AppDbContext context,
            UserManager<AppUser> userManager,
            ILogger<CreateModel> logger,
            ICryptoService cryptoService)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _cryptoService = cryptoService;
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
        public string? UserCPRNumber { get; set; } = string.Empty;
        public List<TaskOption>? TaskOptions { get; set; } = [];

        // Liste af borgere til dropdown (inkl. maskeret CPR)
        public List<BorgerUserDto> BorgerUsers { get; set; } = new();

        public class BorgerUserDto
        {
            public string Id { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string MaskedCPR { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnGet(string? userId, string? evaluationDate)
        {
            try
            {
                // Hvis vi kommer via route med fast brugerId
                if (!string.IsNullOrEmpty(userId))
                {
                    RouteUserId = userId;
                    SelfEvaluation.UserId = userId;

                    var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                    if (user != null)
                    {
                        // Navn
                        UserFullName = user.GetType().GetProperty("FullName") != null
                            ? (string?)user.GetType().GetProperty("FullName")!.GetValue(user)
                            : $"{user.FirstName} {user.LastName}";

                        // Maskeret CPR (ddMMyy-xxxx)
                        UserCPRNumber = CprHelper.GetMaskedCpr(user, _cryptoService);
                    }
                }

                // Dato fra route
                if (!string.IsNullOrEmpty(evaluationDate) && DateTime.TryParse(evaluationDate, out var d))
                {
                    RouteEvaluationDate = d.Date;
                    SelfEvaluation.EvaluationDate = d.Date;
                }

                // Hvis ingen dato fra route → default til i dag
                if (RouteEvaluationDate == null)
                {
                    SelfEvaluation.EvaluationDate = DateTime.UtcNow.Date;
                }

                // Dagens opgaver
                TaskOptions = await _context.TaskOptions
                    .AsNoTracking()
                    .OrderBy(t => t.TaskName)
                    .ToListAsync();

                // Kun hvis vi IKKE har låst bruger via route, hentes liste til dropdown
                if (string.IsNullOrEmpty(RouteUserId))
                {
                    var borgerUsers = await _userManager.GetUsersInRoleAsync("Borger");

                    BorgerUsers = borgerUsers
                        .OrderBy(u => u.FirstName)
                        .ThenBy(u => u.LastName)
                        .Select(u => new BorgerUserDto
                        {
                            Id = u.Id,
                            FullName = u.GetType().GetProperty("FullName") != null
                                ? (string?)u.GetType().GetProperty("FullName")!.GetValue(u) ?? $"{u.FirstName} {u.LastName}"
                                : $"{u.FirstName} {u.LastName}",
                            MaskedCPR = CprHelper.GetMaskedCpr(u, _cryptoService)
                        })
                        .ToList();
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fejl ved indlæsning af CreatePage.");
                TempData["ErrorMessage"] = "Der opstod en uventet fejl ved indlæsning af siden.";
                return RedirectToPage("./Index");
            }
        }

        public async Task<IActionResult> OnPostAsync([FromForm] List<int>? SelectedTaskIds)
        {
            // SelfEvaluation.User navigation skal ikke valideres
            ModelState.Remove(nameof(SelfEvaluation) + ".User");

            // Check om der allerede findes en evaluering for samme borger og dato
            var alreadyExist = await _context.SelfEvaluations.AnyAsync(
                e => e.UserId == SelfEvaluation.UserId &&
                     e.EvaluationDate.Date == SelfEvaluation.EvaluationDate.Date);

            if (!ModelState.IsValid || alreadyExist)
            {
                if (alreadyExist)
                {
                    TempData["ErrorMessage"] = "Der findes allerede en evaluering for denne borger på den valgte dato.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Der opstod en uventet fejl, prøv venligst igen! Fortsætter problemet, så prøv at gå til oversigten og forsøg at oprette skema påny!";
                }

                // Genindlæs TaskOptions
                TaskOptions = await _context.TaskOptions
                    .AsNoTracking()
                    .OrderBy(t => t.TaskName)
                    .ToListAsync();

                // Genopbyg BorgerUsers til dropdown (hvis vi ikke har RouteUserId)
                if (string.IsNullOrEmpty(RouteUserId))
                {
                    var borgerUsers = await _userManager.GetUsersInRoleAsync("Borger");
                    BorgerUsers = borgerUsers
                        .OrderBy(u => u.FirstName)
                        .ThenBy(u => u.LastName)
                        .Select(u => new BorgerUserDto
                        {
                            Id = u.Id,
                            FullName = u.GetType().GetProperty("FullName") != null
                                ? (string?)u.GetType().GetProperty("FullName")!.GetValue(u) ?? $"{u.FirstName} {u.LastName}"
                                : $"{u.FirstName} {u.LastName}",
                            MaskedCPR = CprHelper.GetMaskedCpr(u, _cryptoService)
                        })
                        .ToList();
                }

                return Page();
            }

            // Load current user og check
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                _logger.LogWarning("Forsøg på at oprette evaluering uden gyldig bruger.");
                TempData["ErrorMessage"] = "Den aktuelle bruger kunne ikke indlæses.";
                return RedirectToPage("./Index");
            }

            try
            {
                // Beregn total tid (bruger din helper)
                if (SelfEvaluation.ArrivalTime.HasValue && SelfEvaluation.DepartureTime.HasValue)
                    SelfEvaluation.TotalHours = CalculateTotalWorkHours.CalculateTotalHours(
                        SelfEvaluation.ArrivalTime.Value,
                        SelfEvaluation.DepartureTime.Value,
                        SelfEvaluation.BreakDuration);

                // Auto-godkend hvis udfyldt korrekt
                if ((SelfEvaluation.IsSick && !string.IsNullOrEmpty(SelfEvaluation.SickReason)) ||
                    (SelfEvaluation.IsNoShow && !string.IsNullOrEmpty(SelfEvaluation.NoShowReason)) ||
                    (SelfEvaluation.IsOffWork && !string.IsNullOrEmpty(SelfEvaluation.OffWorkReason)))
                {
                    SelfEvaluation.IsApproved = true;
                    SelfEvaluation.ApprovedByUserId = currentUser.Id;
                }

                SelfEvaluation.LastUpdated = DateTime.UtcNow;

                if (SelectedTaskIds is { Count: > 0 })
                {
                    SelfEvaluation.SelectedTask = await _context.TaskOptions
                        .Where(t => SelectedTaskIds.Contains(t.TaskOptionId))
                        .ToListAsync();
                }

                _context.SelfEvaluations.Add(SelfEvaluation);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Evalueringen blev oprettet med succes.";
                return RedirectToPage("./Index");
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx,
                    "Databasefejl ved oprettelse af evalueringsskemaet for borger: {UserId}",
                    SelfEvaluation.UserId);
                TempData["ErrorMessage"] = "Der opstod en databasefejl. Prøv igen.";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Uventet fejl ved oprettelse af evalueringsskemaet for borger: {UserId}",
                    SelfEvaluation.UserId);
                TempData["ErrorMessage"] = "Der opstod en uventet fejl. Kontakt support hvis problemet fortsætter.";
                return RedirectToPage("./Index");
            }
        }
    }
}