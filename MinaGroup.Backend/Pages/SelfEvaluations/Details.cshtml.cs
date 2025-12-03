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

        public DetailsModel(
            AppDbContext context,
            ILogger<DetailsModel> logger,
            ICryptoService cryptoService)
        {
            _context = context;
            _logger = logger;
            _cryptoService = cryptoService;
        }

        public SelfEvaluation SelfEvaluation { get; set; } = default!;

        public List<TaskOption> TaskOptions { get; set; } = [];

        [BindProperty]
        public List<int> SelectedTaskIds { get; set; } = [];

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
                var selfevaluation = await _context.SelfEvaluations
                    .Include(u => u.User)
                    .Include(u => u.ApprovedByUser)
                    .Include(u => u.SelectedTask)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (selfevaluation == null)
                {
                    TempData["ErrorMessage"] = "Evalueringen kunne ikke findes.";
                    return RedirectToPage("./Index");
                }

                // Hent alle TaskOptions til visning (checkboxes readonly)
                TaskOptions = await _context.TaskOptions
                    .AsNoTracking()
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