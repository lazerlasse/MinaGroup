using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MinaGroup.Backend.Data;
using MinaGroup.Backend.Models;

namespace MinaGroup.Backend.Pages.SystemManagement.Organizations
{
    [Authorize(Roles = "SysAdmin")]
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(AppDbContext context, ILogger<IndexModel> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IList<Organization> Organizations { get; set; } = [];

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            public int? Id { get; set; }

            [Required(ErrorMessage = "Virksomhedsnavn er påkrævet")]
            [Display(Name = "Virksomheds navn")]
            public string Name { get; set; } = string.Empty;

            [Display(Name = "CVR Nr.")]
            [Range(10000000, 99999999, ErrorMessage = "CVR skal være 8 cifre")]
            public int CVRNumber { get; set; }

            [Display(Name = "Adresse")]
            public string OrganizationAdress { get; set; } = string.Empty;

            [Display(Name = "Postnummer")]
            [Range(0, 9999, ErrorMessage = "Postnummer skal være 4 cifre")]
            public int PostalCode { get; set; }

            [Display(Name = "By")]
            public string Town { get; set; } = string.Empty;

            [Display(Name = "Slug / nøgle (valgfrit)")]
            public string? Slug { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            Organizations = await _context.Organizations
                .Include(o => o.Users)
                .Include(o => o.StorageIntegrations)
                .OrderBy(o => o.Name)
                .ToListAsync();

            if (id.HasValue)
            {
                var org = await _context.Organizations.FindAsync(id.Value);
                if (org != null)
                {
                    Input = new InputModel
                    {
                        Id = org.Id,
                        Name = org.Name,
                        CVRNumber = org.CVRNumber,
                        OrganizationAdress = org.OrganizationAdress,
                        PostalCode = org.PostalCode,
                        Town = org.Town,
                        Slug = org.Slug
                    };
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!ModelState.IsValid)
            {
                Organizations = await _context.Organizations
                    .Include(o => o.Users)
                    .Include(o => o.StorageIntegrations)
                    .OrderBy(o => o.Name)
                    .ToListAsync();

                return Page();
            }

            try
            {
                var org = new Organization
                {
                    Name = Input.Name.Trim(),
                    CVRNumber = Input.CVRNumber,
                    OrganizationAdress = Input.OrganizationAdress,
                    PostalCode = Input.PostalCode,
                    Town = Input.Town,
                    Slug = string.IsNullOrWhiteSpace(Input.Slug)
                        ? null
                        : Input.Slug.Trim()
                };

                _context.Organizations.Add(org);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Organisation oprettet.";
                return RedirectToPage(new { id = (int?)null });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fejl ved oprettelse af organisation.");
                TempData["ErrorMessage"] = "Der opstod en fejl ved oprettelse af organisationen.";
                Organizations = await _context.Organizations
                    .Include(o => o.Users)
                    .Include(o => o.StorageIntegrations)
                    .OrderBy(o => o.Name)
                    .ToListAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostUpdateAsync()
        {
            if (!ModelState.IsValid || Input.Id == null)
            {
                Organizations = await _context.Organizations
                    .Include(o => o.Users)
                    .Include(o => o.StorageIntegrations)
                    .OrderBy(o => o.Name)
                    .ToListAsync();

                return Page();
            }

            try
            {
                var org = await _context.Organizations.FindAsync(Input.Id.Value);
                if (org == null)
                {
                    TempData["ErrorMessage"] = "Organisationen blev ikke fundet.";
                    return RedirectToPage();
                }

                org.Name = Input.Name.Trim();
                org.CVRNumber = Input.CVRNumber;
                org.OrganizationAdress = Input.OrganizationAdress;
                org.PostalCode = Input.PostalCode;
                org.Town = Input.Town;
                org.Slug = string.IsNullOrWhiteSpace(Input.Slug)
                    ? null
                    : Input.Slug.Trim();

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Organisation opdateret.";
                return RedirectToPage(new { id = org.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fejl ved opdatering af organisation {Id}", Input.Id);
                TempData["ErrorMessage"] = "Der opstod en fejl ved opdatering af organisationen.";
                Organizations = await _context.Organizations
                    .Include(o => o.Users)
                    .Include(o => o.StorageIntegrations)
                    .OrderBy(o => o.Name)
                    .ToListAsync();
                return Page();
            }
        }
    }
}