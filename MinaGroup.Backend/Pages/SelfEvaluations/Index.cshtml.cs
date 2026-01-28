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
using MinaGroup.Backend.ViewModels;
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
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(
            AppDbContext context,
            UserManager<AppUser> userManager,
            SelfEvaluationPdfService pdfService,
            ILogger<IndexModel> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _pdfService = pdfService ?? throw new ArgumentNullException(nameof(pdfService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Outputs to view
        public List<MissingEvalView> MissingEvaluations { get; set; } = [];
        public List<SelfEvaluation> PendingLeaderComments { get; set; } = [];
        public List<SelfEvaluation> ApprovedEvaluations { get; set; } = [];
        public List<SelectListItem> BorgerSelectList { get; set; } = [];

        // Filters / paging (bound from query string)
        [BindProperty(SupportsGet = true)]
        public string? UserFilter { get; set; }

        // Brug pageIndex som querystring-navn for at undgå konflikter med Razor Pages' "page"
        [BindProperty(SupportsGet = true)]
        public int PageIndex { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 25;

        public int CurrentPage => Math.Max(PageIndex, 1);
        public int TotalPages { get; set; } = 1;

        public async Task<IActionResult> OnGetAsync()
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

                // normalize
                PageIndex = PageIndex <= 0 ? 1 : PageIndex;
                PageSize = (PageSize == 25 || PageSize == 50) ? PageSize : 25;

                // 1) Liste over borger-brugere (kun i denne organisation)
                var borgerUsersAll = await _userManager.GetUsersInRoleAsync("Borger");
                var borgerUsers = borgerUsersAll
                    .Where(u => u.OrganizationId == orgId)
                    .ToList();

                BorgerSelectList = borgerUsers
                    .Select(u => new SelectListItem
                    {
                        Value = u.Id,
                        Text = string.IsNullOrWhiteSpace(u.FullName) ? u.Email : u.FullName
                    })
                    .OrderBy(x => x.Text)
                    .ToList();

                // 2) Beregn manglende evalueringer (per borger i org)
                var today = DateTime.UtcNow.Date;
                var borgerIds = borgerUsers.Select(u => u.Id).ToList();

                MissingEvaluations = [];

                if (borgerIds.Any())
                {
                    var earliestStart = borgerUsers
                        .Where(u => u.JobStartDate.HasValue)
                        .Select(u => u.JobStartDate!.Value)
                        .DefaultIfEmpty(today.AddMonths(-6))
                        .Min()
                        .Date;

                    var existingEvals = await _context.SelfEvaluations
                        .Include(se => se.User)
                        .Where(se => borgerIds.Contains(se.UserId)
                                     && se.User.OrganizationId == orgId
                                     && se.EvaluationDate.Date >= earliestStart
                                     && se.EvaluationDate.Date <= today)
                        .AsNoTracking()
                        .ToListAsync();

                    foreach (var user in borgerUsers)
                    {
                        if (!user.JobStartDate.HasValue)
                            continue;

                        var start = user.JobStartDate.Value.Date;
                        var end = user.JobEndDate.HasValue ? user.JobEndDate.Value.Date : today;

                        if (start > today)
                            continue;
                        if (end > today)
                            end = today;

                        var scheduled = user.ScheduledDays ??
                                        (WeekDays.Mandag | WeekDays.Tirsdag | WeekDays.Onsdag |
                                         WeekDays.Torsdag | WeekDays.Fredag);

                        var userExistingDates = existingEvals
                            .Where(e => e.UserId == user.Id)
                            .Select(e => e.EvaluationDate.Date)
                            .ToHashSet();

                        for (var d = start; d <= end; d = d.AddDays(1))
                        {
                            if (!IsScheduledOn(scheduled, d.DayOfWeek))
                                continue;
                            if (userExistingDates.Contains(d))
                                continue;

                            MissingEvaluations.Add(new MissingEvalView
                            {
                                UserId = user.Id,
                                FullName = user.FullName,
                                Date = d
                            });
                        }
                    }

                    MissingEvaluations = MissingEvaluations
                        .OrderBy(m => m.FullName)
                        .ThenBy(m => m.Date)
                        .ToList();
                }

                // 3) Skemaer der mangler lederkommentar (kun i denne org)
                PendingLeaderComments = await _context.SelfEvaluations
                    .Include(se => se.User)
                    .Where(se => se.User.OrganizationId == orgId &&
                                 se.IsApproved == false)
                    .OrderByDescending(se => se.EvaluationDate)
                    .AsNoTracking()
                    .ToListAsync();

                // 4) Godkendte evalueringer med paging (kun i denne org)
                var approvedQuery = _context.SelfEvaluations
                    .Include(se => se.User)
                    .Include(se => se.ApprovedByUser)
                    .Where(se => se.IsApproved && se.User.OrganizationId == orgId);

                if (!string.IsNullOrEmpty(UserFilter))
                {
                    approvedQuery = approvedQuery.Where(se => se.UserId == UserFilter);
                }

                var totalApproved = await approvedQuery.CountAsync();
                TotalPages = Math.Max(1, (int)Math.Ceiling(totalApproved / (double)PageSize));

                if (PageIndex > TotalPages)
                    PageIndex = TotalPages;

                ApprovedEvaluations = await approvedQuery
                    .OrderByDescending(se => se.EvaluationDate)
                    .Skip((PageIndex - 1) * PageSize)
                    .Take(PageSize)
                    .AsNoTracking()
                    .ToListAsync();

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Der opstod en fejl ved indlæsning af selvevalueringer.");
                TempData["ErrorMessage"] = "Der opstod en fejl ved indlæsning af selvevalueringer. Prøv igen senere.";
                return RedirectToPage("/Management/Index");
            }
        }

        private static bool IsScheduledOn(WeekDays scheduled, DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
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
                var currentUser = await _userManager.GetCurrentUserWithOrganizationAsync(User);
                if (currentUser == null)
                {
                    TempData["ErrorMessage"] = "Den aktuelle bruger kunne ikke indlæses eller mangler organisation.";
                    return Unauthorized();
                }

                var orgId = currentUser.OrganizationId!.Value;

                var evaluation = await _context.SelfEvaluations
                    .Include(se => se.User)
                    .Where(se => se.Id == id && se.User.OrganizationId == orgId)
                    .FirstOrDefaultAsync();

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

        // GET: Upload status for selvevalueringer.
        public async Task<IActionResult> OnGetUploadStatusesAsync([FromQuery] int[] ids)
        {
            var currentUser = await _userManager.GetCurrentUserWithOrganizationAsync(User);
            if (currentUser?.OrganizationId == null)
                return Unauthorized();

            var orgId = currentUser.OrganizationId.Value;

            // Queue items (seneste state)
            var queueItems = await _context.SelfEvaluationUploadQueueItems
                .AsNoTracking()
                .Where(x => x.OrganizationId == orgId && ids.Contains(x.SelfEvaluationId))
                .ToListAsync();

            // Seneste log (hvis du vil vise fejltekst)
            var lastLogs = await _context.SelfEvaluationUploadLogs
                .AsNoTracking()
                .Where(x => x.OrganizationId == orgId && ids.Contains(x.SelfEvaluationId))
                .GroupBy(x => x.SelfEvaluationId)
                .Select(g => g.OrderByDescending(x => x.CreatedAtUtc).First())
                .ToListAsync();

            var result = ids.Select(id =>
            {
                var q = queueItems.FirstOrDefault(x => x.SelfEvaluationId == id);
                var log = lastLogs.FirstOrDefault(x => x.SelfEvaluationId == id);

                // Hvis intet queue item: så enten ikke sat op, eller før feature blev lavet
                var state = q?.State.ToString() ?? "None";
                var msg = q?.LastMessage ?? log?.Message ?? "";

                return new
                {
                    selfEvaluationId = id,
                    state,
                    message = msg
                };
            });

            return new JsonResult(result);
        }
    }
}