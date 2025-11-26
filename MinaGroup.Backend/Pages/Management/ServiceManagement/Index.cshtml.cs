using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MinaGroup.Backend.Data;
using MinaGroup.Backend.Models;
using MinaGroup.Backend.Services;
using MinaGroup.Backend.Services.Interfaces;

namespace MinaGroup.Backend.Pages.Management.ServiceManagement
{
    [Authorize(Roles = "Admin,SysAdmin")]
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IOrganizationResolver _orgResolver;
        private readonly ICryptoService _cryptoService;

        public IndexModel(
            AppDbContext context,
            IOrganizationResolver orgResolver,
            ICryptoService cryptoService)
        {
            _context = context;
            _orgResolver = orgResolver;
            _cryptoService = cryptoService;
        }

        public Organization? CurrentOrganization { get; set; }
        public OrganizationStorageIntegration? GoogleDriveIntegration { get; set; }

        [BindProperty]
        public string? RootFolderId { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            CurrentOrganization = await _orgResolver.GetCurrentOrganizationAsync();
            if (CurrentOrganization == null)
            {
                TempData["ErrorMessage"] = "Der er ikke tilknyttet nogen virksomhed til din bruger.";
                return RedirectToPage("/Management/Index");
            }

            GoogleDriveIntegration = await _context.OrganizationStorageIntegrations
                .FirstOrDefaultAsync(x => x.OrganizationId == CurrentOrganization.Id &&
                                          x.ProviderName == "GoogleDrive");

            RootFolderId = GoogleDriveIntegration?.RootFolderId;

            return Page();
        }

        // Gemmer kun RootFolderId – selve Connect-flowet håndteres af en separat handler (OAuth callback)
        public async Task<IActionResult> OnPostSaveRootFolderAsync()
        {
            CurrentOrganization = await _orgResolver.GetCurrentOrganizationAsync();
            if (CurrentOrganization == null)
            {
                TempData["ErrorMessage"] = "Der er ikke tilknyttet nogen virksomhed til din bruger.";
                return RedirectToPage("/Management/Index");
            }

            GoogleDriveIntegration = await _context.OrganizationStorageIntegrations
                .FirstOrDefaultAsync(x => x.OrganizationId == CurrentOrganization.Id &&
                                          x.ProviderName == "GoogleDrive");

            if (GoogleDriveIntegration == null)
            {
                GoogleDriveIntegration = new OrganizationStorageIntegration
                {
                    OrganizationId = CurrentOrganization.Id,
                    ProviderName = "GoogleDrive",
                    RootFolderId = RootFolderId
                };
                _context.OrganizationStorageIntegrations.Add(GoogleDriveIntegration);
            }
            else
            {
                GoogleDriveIntegration.RootFolderId = RootFolderId;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Google Drive mappe-indstilling blev gemt.";
            return RedirectToPage();
        }

        // Her laver du evt. en handler der kalder din Google OAuth "Connect"-flow:
        public async Task<IActionResult> OnPostConnectGoogleDriveAsync()
        {
            CurrentOrganization = await _orgResolver.GetCurrentOrganizationAsync();
            if (CurrentOrganization == null)
            {
                TempData["ErrorMessage"] = "Der er ikke tilknyttet nogen virksomhed til din bruger.";
                return RedirectToPage("/Management/Index");
            }

            // TODO: Redirect til Google OAuth-autorisations-URL med state = orgId
            // For nu kan vi bare smide en TODO besked:
            TempData["InfoMessage"] = "Connect-flow til Google Drive er endnu ikke fuldt implementeret.";
            return RedirectToPage();
        }
    }
}