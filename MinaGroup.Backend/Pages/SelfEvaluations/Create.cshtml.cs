using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MinaGroup.Backend.Data;
using MinaGroup.Backend.Helpers;
using MinaGroup.Backend.Models;
using MinaGroup.Backend.Services;
using MinaGroup.Backend.Services.Interfaces;
using MinaGroup.Backend.Enums;
using System;
using System.Collections.Generic;
using System.IO;
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

        // 👇 NYT
        private readonly SelfEvaluationPdfService _pdfService;
        private readonly IGoogleDriveService _googleDriveService;

        public CreateModel(
            AppDbContext context,
            UserManager<AppUser> userManager,
            ILogger<CreateModel> logger,
            ICryptoService cryptoService,
            SelfEvaluationPdfService pdfService,                 // 👈 NYT
            IGoogleDriveService googleDriveService)              // 👈 NYT
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _cryptoService = cryptoService;

            _pdfService = pdfService;
            _googleDriveService = googleDriveService;
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
        public List<BorgerUserDto> BorgerUsers { get; set; } = [];


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
                var currentUser = await _userManager.GetCurrentUserWithOrganizationAsync(User);
                if (currentUser == null)
                {
                    TempData["ErrorMessage"] =
                        "Den aktuelle bruger kunne ikke indlæses eller er ikke tilknyttet en organisation.";
                    return Unauthorized();
                }

                var orgId = currentUser.OrganizationId!.Value;

                // Hvis vi kommer via route med fast brugerId
                if (!string.IsNullOrEmpty(userId))
                {
                    RouteUserId = userId;
                    SelfEvaluation.UserId = userId;

                    // Hent borgeren, men kun hvis den tilhører samme organisation
                    var user = await _userManager.FindByIdAsync(userId);
                    if (user != null && user.OrganizationId == orgId)
                    {
                        UserFullName = user.GetType().GetProperty("FullName") != null
                            ? (string?)user.GetType().GetProperty("FullName")!.GetValue(user)
                            : $"{user.FirstName} {user.LastName}";

                        // Maskeret CPR (ddMMyy-xxxx)
                        UserCPRNumber = CprHelper.GetMaskedCpr(user, _cryptoService);
                    }
                    else
                    {
                        TempData["ErrorMessage"] =
                            "Den valgte borger tilhører ikke din organisation eller kunne ikke findes.";
                        return RedirectToPage("./Index");
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

                // Dagens opgaver (kun for denne organisation)
                TaskOptions = await _context.TaskOptions
                    .AsNoTracking()
                    .Where(t => t.OrganizationId == orgId)
                    .OrderBy(t => t.TaskName)
                    .ToListAsync();

                // Kun hvis vi IKKE har låst bruger via route, hentes liste til dropdown
                if (string.IsNullOrEmpty(RouteUserId))
                {
                    var borgerUsersAll = await _userManager.GetUsersInRoleAsync("Borger");

                    var borgerUsers = borgerUsersAll
                        .Where(u => u.OrganizationId == orgId)
                        .OrderBy(u => u.FirstName)
                        .ThenBy(u => u.LastName)
                        .ToList();

                    BorgerUsers = borgerUsers
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

            var currentUser = await _userManager.GetCurrentUserWithOrganizationAsync(User);
            if (currentUser == null)
            {
                _logger.LogWarning("Forsøg på at oprette evaluering uden gyldig bruger/organisation.");
                TempData["ErrorMessage"] = "Den aktuelle bruger kunne ikke indlæses eller mangler organisation.";
                return RedirectToPage("./Index");
            }

            var orgId = currentUser.OrganizationId!.Value;

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
                    TempData["ErrorMessage"] =
                        "Der opstod en uventet fejl, prøv venligst igen! Fortsætter problemet, så prøv at gå til oversigten og forsøg at oprette skema påny!";
                }

                // Genindlæs TaskOptions (kun for denne organisation)
                TaskOptions = await _context.TaskOptions
                    .AsNoTracking()
                    .Where(t => t.OrganizationId == orgId)
                    .OrderBy(t => t.TaskName)
                    .ToListAsync();

                // Genopbyg BorgerUsers til dropdown (hvis vi ikke har RouteUserId)
                if (string.IsNullOrEmpty(RouteUserId))
                {
                    var borgerUsersAll = await _userManager.GetUsersInRoleAsync("Borger");
                    var borgerUsers = borgerUsersAll
                        .Where(u => u.OrganizationId == orgId)
                        .OrderBy(u => u.FirstName)
                        .ThenBy(u => u.LastName)
                        .ToList();

                    BorgerUsers = borgerUsers
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

            try
            {
                // Verificér at den valgte borger tilhører samme organisation
                var evalUser = await _userManager.FindByIdAsync(SelfEvaluation.UserId);
                if (evalUser == null || evalUser.OrganizationId != orgId)
                {
                    _logger.LogWarning("Forsøg på at oprette evaluering for borger i anden organisation. UserId={UserId}", SelfEvaluation.UserId);
                    TempData["ErrorMessage"] = "Den valgte borger tilhører ikke din organisation eller kunne ikke findes.";
                    return RedirectToPage("./Index");
                }

                // Beregn total tid
                if (SelfEvaluation.ArrivalTime.HasValue && SelfEvaluation.DepartureTime.HasValue)
                {
                    SelfEvaluation.TotalHours = CalculateTotalWorkHours.CalculateTotalHours(
                        SelfEvaluation.ArrivalTime.Value,
                        SelfEvaluation.DepartureTime.Value,
                        SelfEvaluation.BreakDuration);
                }

                // Auto-godkend hvis udfyldt korrekt
                if ((SelfEvaluation.IsSick && !string.IsNullOrEmpty(SelfEvaluation.SickReason)) ||
                    (SelfEvaluation.IsNoShow && !string.IsNullOrEmpty(SelfEvaluation.NoShowReason)) ||
                    (SelfEvaluation.IsOffWork && !string.IsNullOrEmpty(SelfEvaluation.OffWorkReason)) ||
                    (
                        !SelfEvaluation.IsSick &&
                        !SelfEvaluation.IsNoShow &&
                        !SelfEvaluation.IsOffWork &&
                        SelfEvaluation.ArrivalTime.HasValue &&
                        SelfEvaluation.DepartureTime.HasValue &&
                        !string.IsNullOrEmpty(SelfEvaluation.CommentFromLeader)
                    ))
                {
                    SelfEvaluation.IsApproved = true;
                    SelfEvaluation.ApprovedByUserId = currentUser.Id;
                }

                SelfEvaluation.LastUpdated = DateTime.UtcNow;

                if (SelectedTaskIds is { Count: > 0 })
                {
                    SelfEvaluation.SelectedTask = await _context.TaskOptions
                        .Where(t =>
                            SelectedTaskIds.Contains(t.TaskOptionId) &&
                            t.OrganizationId == orgId)
                        .ToListAsync();
                }

                _context.SelfEvaluations.Add(SelfEvaluation);
                await _context.SaveChangesAsync();

                // Upload hvis auto-godkendt
                if (SelfEvaluation.IsApproved)
                {
                    try
                    {
                        var queue = HttpContext.RequestServices.GetRequiredService<UploadQueueService>();

                        var queueItemId = await queue.EnqueueOrRequeueSelfEvaluationUploadAsync(
                            organizationId: orgId,
                            selfEvaluationId: SelfEvaluation.Id,
                            providerName: "GoogleDrive",
                            reason: null,
                            ct: HttpContext.RequestAborted);

                        TempData["UploadQueueItemId"] = queueItemId;
                        TempData["InfoMessage"] = "Evalueringen blev oprettet og godkendt. PDF upload er sat i kø.";
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Kunne ikke enqueue upload job efter Create. EvalId={EvalId} OrgId={OrgId}", SelfEvaluation.Id, orgId);
                        TempData["WarningMessage"] = "Evalueringen blev oprettet og godkendt, men upload-job kunne ikke oprettes. Du kan downloade PDF manuelt.";
                    }
                }
                else
                {
                    TempData["SuccessMessage"] = "Evalueringen blev oprettet med succes.";
                }



                if (!SelfEvaluation.IsApproved)
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