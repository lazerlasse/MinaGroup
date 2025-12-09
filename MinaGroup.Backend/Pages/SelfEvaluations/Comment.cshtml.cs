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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MinaGroup.Backend.Pages.SelfEvaluations
{
    [Authorize(Roles = "Admin,Leder")]
    public class CommentModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<CommentModel> _logger;
        private readonly ICryptoService _cryptoService;
        private readonly IGoogleDriveService _googleDriveService;
        private readonly SelfEvaluationPdfService _pdfService;

        public CommentModel(
            AppDbContext context,
            UserManager<AppUser> userManager,
            ILogger<CommentModel> logger,
            ICryptoService cryptoService,
            IGoogleDriveService googleDriveService,
            SelfEvaluationPdfService pdfService)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _cryptoService = cryptoService;
            _googleDriveService = googleDriveService;
            _pdfService = pdfService;
        }

        [BindProperty]
        public SelfEvaluation SelfEvaluation { get; set; } = new();

        public List<TaskOption>? TaskOptions { get; set; } = [];

        [BindProperty]
        public List<int> SelectedTaskIds { get; set; } = [];

        // Maskeret CPR til visning på siden
        public string? MaskedCpr { get; set; }


        public async Task<IActionResult> OnGetAsync(int id)
        {
            try
            {
                // Rollecheck (Admin eller Leder)
                if (!(User.IsInRole("Leder") || User.IsInRole("Admin")))
                {
                    _logger.LogWarning("Unauthorized adgangsforsøg til SelfEvaluation {Id}", id);
                    TempData["ErrorMessage"] = "Du har ikke adgang til denne side.";
                    return Unauthorized();
                }

                var currentUser = await _userManager.GetCurrentUserWithOrganizationAsync(User);
                if (currentUser == null)
                {
                    TempData["ErrorMessage"] = "Den aktuelle bruger kunne ikke indlæses eller mangler organisation.";
                    return Unauthorized();
                }

                var orgId = currentUser.OrganizationId!.Value;

                var evaluation = await _context.SelfEvaluations
                    .Include(s => s.User)
                    .Include(s => s.ApprovedByUser)
                    .Include(u => u.SelectedTask)
                    .FirstOrDefaultAsync(m => m.Id == id &&
                                              m.User != null &&
                                              m.User.OrganizationId == orgId);

                if (evaluation == null)
                {
                    TempData["ErrorMessage"] = "Skemaet kunne ikke indlæses, prøv venligst igen!";
                    return NotFound();
                }

                // TaskOptions for DENNE organisation
                TaskOptions = await _context.TaskOptions
                    .AsNoTracking()
                    .Where(t => t.OrganizationId == orgId)
                    .OrderBy(t => t.TaskName)
                    .ToListAsync();

                // Sæt valgte IDs
                SelectedTaskIds = evaluation.SelectedTask
                    .Select(t => t.TaskOptionId)
                    .ToList();

                SelfEvaluation = evaluation;

                // Sæt maskeret CPR vha. CprHelper
                if (SelfEvaluation.User != null)
                {
                    MaskedCpr = CprHelper.GetMaskedCpr(SelfEvaluation.User, _cryptoService);
                }

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
                var currentUser = await _userManager.GetCurrentUserWithOrganizationAsync(User);
                if (currentUser == null)
                {
                    TempData["ErrorMessage"] = "Den aktuelle bruger kunne ikke indlæses eller mangler organisation.";
                    return Unauthorized();
                }

                var orgId = currentUser.OrganizationId!.Value;

                // Hent evaluering og sørg for at den tilhører samme organisation
                var evaluation = await _context.SelfEvaluations
                    .Include(se => se.User)
                    .FirstOrDefaultAsync(se => se.Id == id &&
                                               se.User != null &&
                                               se.User.OrganizationId == orgId);

                if (evaluation == null)
                {
                    TempData["ErrorMessage"] = "Evalueringsskemaet kunne ikke indlæses!";
                    return NotFound();
                }

                // Opdater felter afhængig af status
                if (SelfEvaluation.IsSick)
                {
                    evaluation.IsSick = true;
                    evaluation.IsNoShow = false;
                    evaluation.IsOffWork = false;

                    evaluation.SickReason = SelfEvaluation.SickReason;
                    evaluation.NoShowReason = null;
                    evaluation.OffWorkReason = null;
                    evaluation.CommentFromLeader = null;
                }
                else if (SelfEvaluation.IsNoShow)
                {
                    evaluation.IsSick = false;
                    evaluation.IsNoShow = true;
                    evaluation.IsOffWork = false;

                    evaluation.SickReason = null;
                    evaluation.NoShowReason = SelfEvaluation.NoShowReason;
                    evaluation.OffWorkReason = null;
                    evaluation.CommentFromLeader = null;
                }
                else if (SelfEvaluation.IsOffWork)
                {
                    evaluation.IsSick = false;
                    evaluation.IsNoShow = false;
                    evaluation.IsOffWork = true;

                    evaluation.SickReason = null;
                    evaluation.NoShowReason = null;
                    evaluation.OffWorkReason = SelfEvaluation.OffWorkReason;
                    evaluation.CommentFromLeader = null;
                }
                else
                {
                    evaluation.IsSick = false;
                    evaluation.IsNoShow = false;
                    evaluation.IsOffWork = false;

                    evaluation.SickReason = null;
                    evaluation.NoShowReason = null;
                    evaluation.OffWorkReason = null;
                    evaluation.CommentFromLeader = SelfEvaluation.CommentFromLeader;
                }

                evaluation.IsApproved = true;
                evaluation.ApprovedByUserId = currentUser.Id;
                evaluation.LastUpdated = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // --- Forsøg automatisk upload til Google Drive ---
                try
                {
                    if (evaluation.User?.OrganizationId != null)
                    {
                        var evalOrgId = evaluation.User.OrganizationId.Value;
                        var citizenName = evaluation.User.FullName;
                        var fileName = $"{evaluation.EvaluationDate:dd.MM.yy}-{citizenName}.pdf";

                        // Sørg for at vi har de nødvendige data til PDF
                        var evalForPdf = await _context.SelfEvaluations
                            .Include(se => se.User)
                            .Include(se => se.ApprovedByUser)
                            .Include(se => se.SelectedTask)
                            .FirstOrDefaultAsync(se => se.Id == evaluation.Id);

                        if (evalForPdf != null)
                        {
                            var pdfBytes = _pdfService.GeneratePdf(evalForPdf);

                            using var ms = new MemoryStream(pdfBytes);
                            await _googleDriveService.UploadPdfForOrganizationAsync(
                                evalOrgId,
                                citizenName,
                                fileName,
                                ms);
                        }
                    }
                }
                catch (Exception driveEx)
                {
                    // Vi vil ikke blokere brugeren, hvis Drive fejler – fallback er manuel download
                    _logger.LogError(driveEx,
                        "Fejl ved upload af selvevaluering {Id} til Google Drive for org {OrgId}.",
                        evaluation.Id,
                        evaluation.User?.OrganizationId);
                }

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