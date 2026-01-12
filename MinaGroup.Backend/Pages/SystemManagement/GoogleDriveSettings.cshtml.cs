using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MinaGroup.Backend.Data;
using MinaGroup.Backend.Models;
using MinaGroup.Backend.Services.Interfaces;

namespace MinaGroup.Backend.Pages.SystemManagement
{
    [Authorize(Roles = "SysAdmin")]
    public class GoogleDriveSettingsModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly ICryptoService _crypto;
        private readonly ILogger<GoogleDriveSettingsModel> _logger;

        public GoogleDriveSettingsModel(
            AppDbContext context,
            ICryptoService crypto,
            ILogger<GoogleDriveSettingsModel> logger)
        {
            _context = context;
            _crypto = crypto;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string OAuthRedirectUri { get; set; } = string.Empty;

        public bool IsConfigured { get; set; }

        public class InputModel
        {
            public string ClientId { get; set; } = string.Empty;

            // Vises/indtastes i klartekst, men gemmes krypteret
            public string? ClientSecret { get; set; }
        }

        private async Task<GoogleDriveSystemSetting> GetOrCreateSystemSettingsAsync(CancellationToken ct)
        {
            var setting = await _context.GoogleDriveSystemSettings.FirstOrDefaultAsync(ct);
            if (setting == null)
            {
                setting = new GoogleDriveSystemSetting
                {
                    ClientId = string.Empty,
                    EncryptedClientSecret = string.Empty
                };
                _context.GoogleDriveSystemSettings.Add(setting);
                await _context.SaveChangesAsync(ct);
            }

            return setting;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var ct = HttpContext.RequestAborted;

            var setting = await GetOrCreateSystemSettingsAsync(ct);

            Input = new InputModel
            {
                ClientId = setting.ClientId,
                ClientSecret = string.Empty // secret vises ikke igen
            };

            OAuthRedirectUri = Url.Page(
                "/Management/ServiceManagement/GoogleDriveService",
                pageHandler: "OAuthCallback",
                values: null,
                protocol: Request.Scheme) ?? string.Empty;

            IsConfigured = !string.IsNullOrWhiteSpace(setting.ClientId)
                           && !string.IsNullOrWhiteSpace(setting.EncryptedClientSecret);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var ct = HttpContext.RequestAborted;

            // Rebuild redirect uri on validation errors too
            OAuthRedirectUri = Url.Page(
                "/Management/ServiceManagement/GoogleDriveService",
                pageHandler: "OAuthCallback",
                values: null,
                protocol: Request.Scheme) ?? string.Empty;

            if (!ModelState.IsValid)
                return Page();

            try
            {
                var setting = await GetOrCreateSystemSettingsAsync(ct);

                setting.ClientId = Input.ClientId?.Trim() ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(Input.ClientSecret))
                {
                    try
                    {
                        setting.EncryptedClientSecret = _crypto.Protect(Input.ClientSecret.Trim());
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Fejl ved kryptering af Google Drive Client Secret.");
                        ModelState.AddModelError(nameof(Input.ClientSecret),
                            "Der opstod en fejl ved kryptering af Client Secret.");
                        return Page();
                    }
                }

                await _context.SaveChangesAsync(ct);

                TempData["SuccessMessage"] = "Google Drive klientindstillinger er gemt.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fejl ved gemning af Google Drive systemindstillinger.");
                ModelState.AddModelError(string.Empty, "Der opstod en uventet fejl. Prøv igen.");
                return Page();
            }
        }
    }
}