using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MinaGroup.Backend.Data;
using MinaGroup.Backend.Models;
using MinaGroup.Backend.Helpers;
using MinaGroup.Backend.Services.Interfaces;

namespace MinaGroup.Backend.Pages.SelfEvaluations
{
    [Authorize(Roles = "Admin,Leder")]
    public class DetailsModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DetailsModel> _logger;
        private readonly ICryptoService _cryptoService;
        private readonly UserManager<AppUser> _userManager;

        public DetailsModel(
            AppDbContext context,
            ILogger<DetailsModel> logger,
            ICryptoService cryptoService,
            UserManager<AppUser> userManager)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cryptoService = cryptoService ?? throw new ArgumentNullException(nameof(cryptoService));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        public SelfEvaluation SelfEvaluation { get; set; } = default!;

        public List<TaskOption> TaskOptions { get; set; } = new();

        [BindProperty]
        public List<int> SelectedTaskIds { get; set; } = new();

        // Maskeret CPR til visning (ddMMyy-xxxx)
        public string? MaskedCpr { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "Ingen evaluering valgt til visning.";
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
                    .Include(u => u.User)
                    .Include(u => u.ApprovedByUser)
                    .Include(u => u.SelectedTask)
                    .Where(se => se.Id == id.Value &&
                                 se.User != null &&
                                 se.User.OrganizationId == orgId)
                    .FirstOrDefaultAsync();

                if (selfevaluation == null)
                {
                    TempData["ErrorMessage"] = "Evalueringen kunne ikke findes.";
                    return RedirectToPage("./Index");
                }

                // Hent TaskOptions for DENNE organisation (readonly på view)
                TaskOptions = await _context.TaskOptions
                    .AsNoTracking()
                    .Where(t => t.OrganizationId == orgId)
                    .OrderBy(t => t.TaskName)
                    .ToListAsync();

                // Sæt valgte IDs
                SelectedTaskIds = selfevaluation.SelectedTask
                    .Select(t => t.TaskOptionId)
                    .ToList();

                SelfEvaluation = selfevaluation;

                // Maskeret CPR via helper (ddMMyy-xxxx)
                if (SelfEvaluation.User != null)
                {
                    MaskedCpr = CprHelper.GetMaskedCpr(SelfEvaluation.User, _cryptoService);
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fejl ved hentning af detaljer for SelfEvaluation med id {Id}", id);
                TempData["ErrorMessage"] = "Der opstod en fejl ved hentning af evalueringen.";
                return RedirectToPage("./Index");
            }
        }
    }
}