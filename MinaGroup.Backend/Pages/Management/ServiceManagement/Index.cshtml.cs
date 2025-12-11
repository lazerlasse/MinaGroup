using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MinaGroup.Backend.Data;
using MinaGroup.Backend.Models;
using MinaGroup.Backend.Services.Interfaces;

namespace MinaGroup.Backend.Pages.Management.ServiceManagement
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IOrganizationResolver _orgResolver;

        public IndexModel(
            AppDbContext context,
            IOrganizationResolver orgResolver)
        {
            _context = context;
            _orgResolver = orgResolver;
        }

        /// <summary>
        /// Den organisation, som den aktuelle admin/leder arbejder i.
        /// </summary>
        public Organization? CurrentOrganization { get; set; }

        // Google Drive status
        public bool GoogleDriveIsEnabled { get; set; }
        public bool GoogleDriveHasRefreshToken { get; set; }
        public string? GoogleDriveConnectedAccountEmail { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Find organisation for nuværende bruger
            CurrentOrganization = await _orgResolver.GetCurrentOrganizationAsync();

            if (CurrentOrganization == null)
            {
                TempData["ErrorMessage"] = "Der er ikke tilknyttet nogen virksomhed til din bruger.";
                return RedirectToPage("/Management/Index");
            }

            // Simpel status for Google Drive config (global/org-niveau afhængigt af din nuværende model)
            var cfg = await _context.GoogleDriveConfigs.FirstOrDefaultAsync();

            if (cfg != null)
            {
                GoogleDriveIsEnabled = cfg.IsEnabled;
                GoogleDriveHasRefreshToken = !string.IsNullOrWhiteSpace(cfg.EncryptedRefreshToken);
                GoogleDriveConnectedAccountEmail = cfg.ConnectedAccountEmail;
            }

            return Page();
        }
    }
}