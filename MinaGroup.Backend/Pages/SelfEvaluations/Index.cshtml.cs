using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MinaGroup.Backend.Data;
using MinaGroup.Backend.Enums;
using MinaGroup.Backend.Helpers;
using MinaGroup.Backend.Models;
using MinaGroup.Backend.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MinaGroup.Backend.Pages.SelfEvaluations
{
    [Authorize(Roles = "Admin,Leder")]
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly SelfEvaluationPdfService _pdfService;
        private readonly ILogger<EditModel> _logger;

        public IndexModel(AppDbContext context, UserManager<AppUser> userManager, SelfEvaluationPdfService pdfService, ILogger<EditModel> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _pdfService = pdfService ?? throw new ArgumentNullException(nameof(pdfService));
            _logger = logger;
        }

        // View model for missing evals
        public class MissingEvalView
        {
            public string UserId { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public DateTime Date { get; set; }
        }

        // Outputs to view
        public List<MissingEvalView> MissingEvaluations { get; set; } = [];
        public List<SelfEvaluation> PendingLeaderComments { get; set; } = [];
        public List<SelfEvaluation> ApprovedEvaluations { get; set; } = [];
        public List<SelectListItem> BorgerSelectList { get; set; } = [];

        // Filters / paging (bound from query string)
        [BindProperty(SupportsGet = true)]
        public string? UserFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public new int Page { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 25;

        public int CurrentPage => Math.Max(Page, 1);
        public int TotalPages { get; set; } = 1;

        public async Task<IActionResult> OnGetAsync(string? userFilter, int page = 1, int pageSize = 25)
        {
            try
            {
                // normalize & bind
                UserFilter = userFilter;
                Page = page <= 0 ? 1 : page;
                PageSize = (pageSize == 25 || pageSize == 50) ? pageSize : 25;

                // 1) Build list of borger users for select list
                var borgerUsers = await _userManager.GetUsersInRoleAsync("Borger");
                BorgerSelectList = borgerUsers
                    .Select(u => new SelectListItem { Value = u.Id, Text = string.IsNullOrWhiteSpace(u.FullName) ? u.Email : u.FullName })
                    .OrderBy(x => x.Text)
                    .ToList();

                // 2) Compute missing evaluations
                var today = DateTime.UtcNow.Date;
                var borgerIds = borgerUsers.Select(u => u.Id).ToList();

                if (borgerIds.Any())
                {
                    var earliestStart = borgerUsers.Min(u => u.JobStartDate) ?? today.AddMonths(-6);
                    var existingEvals = await _context.SelfEvaluations
                        .Where(se => borgerIds.Contains(se.UserId) && se.EvaluationDate.Date >= earliestStart.Date && se.EvaluationDate.Date <= today)
                        .AsNoTracking()
                        .ToListAsync();

                    foreach (var user in borgerUsers)
                    {
                        if (!user.JobStartDate.HasValue) continue;

                        var start = user.JobStartDate.Value.Date;
                        var end = user.JobEndDate.HasValue ? user.JobEndDate.Value.Date : today;
                        if (start > today) continue;
                        if (end > today) end = today;

                        var scheduled = user.ScheduledDays ?? (WeekDays.Mandag | WeekDays.Tirsdag | WeekDays.Onsdag | WeekDays.Torsdag | WeekDays.Fredag);

                        var userExistingDates = existingEvals
                            .Where(e => e.UserId == user.Id)
                            .Select(e => e.EvaluationDate.Date)
                            .ToHashSet();

                        for (var d = start; d <= end; d = d.AddDays(1))
                        {
                            if (!IsScheduledOn(scheduled, d.DayOfWeek)) continue;
                            if (userExistingDates.Contains(d)) continue;

                            MissingEvaluations.Add(new MissingEvalView
                            {
                                UserId = user.Id,
                                FullName = user.FullName,
                                Date = d
                            });
                        }
                    }

                    MissingEvaluations = MissingEvaluations.OrderBy(m => m.FullName).ThenBy(m => m.Date).ToList();
                }

                // 3) Pending leader comments
                PendingLeaderComments = await _context.SelfEvaluations
                    .Include(se => se.User)
                    .Where(se =>
                        se.IsApproved == false &&
                        (
                            string.IsNullOrEmpty(se.CommentFromLeader)
                            || (se.IsSick && string.IsNullOrEmpty(se.SickReason))
                            || (se.IsNoShow && string.IsNullOrEmpty(se.NoShowReason))
                            || (se.IsOffWork && string.IsNullOrEmpty(se.OffWorkReason))
                        )
                     )
                    .OrderByDescending(se => se.EvaluationDate)
                    .AsNoTracking()
                    .ToListAsync();

                // 4) Approved evaluations (with optional filter and paging)
                var approvedQuery = _context.SelfEvaluations
                    .Include(se => se.User)
                    .Include(se => se.ApprovedByUser)
                    .Where(se => se.IsApproved);

                if (!string.IsNullOrEmpty(UserFilter))
                {
                    approvedQuery = approvedQuery.Where(se => se.UserId == UserFilter);
                }

                var totalApproved = await approvedQuery.CountAsync();
                TotalPages = Math.Max(1, (int)Math.Ceiling(totalApproved / (double)PageSize));

                if (Page > TotalPages) Page = TotalPages;

                ApprovedEvaluations = await approvedQuery
                    .OrderByDescending(se => se.EvaluationDate)
                    .Skip((Page - 1) * PageSize)
                    .Take(PageSize)
                    .AsNoTracking()
                    .ToListAsync();

                return Page();
            }
            catch (Exception ex)
            {
                // Generic error handling with TempData + redirect to dashboard
                _logger.LogError(ex, "Der opstod en fejl ved indlæsning af selvevalueringer.");
                TempData["ErrorMessage"] = "Der opstod en fejl ved indlæsning af selvevalueringer. Prøv igen senere.";
                return RedirectToPage("/Management/Index");
            }
        }

        // Helper
        private static bool IsScheduledOn(WeekDays scheduled, DayOfWeek day)
        {
            return day switch
            {
                DayOfWeek.Monday => scheduled.HasFlag(WeekDays.Mandag),
                DayOfWeek.Tuesday => scheduled.HasFlag(WeekDays.Tirsdag),
                DayOfWeek.Wednesday => scheduled.HasFlag(WeekDays.Onsdag),
                DayOfWeek.Thursday => scheduled.HasFlag(WeekDays.Torsdag),
                DayOfWeek.Friday => scheduled.HasFlag(WeekDays.Fredag),
                DayOfWeek.Saturday => scheduled.HasFlag(WeekDays.Lørdag),
                DayOfWeek.Sunday => scheduled.HasFlag(WeekDays.Søndag),
                _ => false
            };
        }

        public async Task<IActionResult> OnGetDownloadAsync(int id)
        {
            try
            {
                var evaluation = await _context.SelfEvaluations
                    .Include(se => se.User)
                    .FirstOrDefaultAsync(se => se.Id == id);

                if (evaluation == null)
                {
                    TempData["ErrorMessage"] = "Kunne ikke finde den valgte evaluering til download.";
                    return RedirectToPage("/Management/Index");
                }

                var pdfBytes = _pdfService.GeneratePdf(evaluation);
                var fileName = $"{evaluation.EvaluationDate:dd.MM.yy}-{evaluation.User.FullName}.pdf";

                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Der opstod en fejl under generering af PDF fil.");
                TempData["ErrorMessage"] = "Der opstod en fejl under generering af PDF. Prøv igen senere.";
                return RedirectToPage("/Management/Index");
            }
        }
    }
}