using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
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
    public class GoogleDriveServiceModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly ICryptoService _crypto;
        private readonly ILogger<GoogleDriveServiceModel> _logger;

        public GoogleDriveServiceModel(
            AppDbContext context,
            ICryptoService crypto,
            ILogger<GoogleDriveServiceModel> logger)
        {
            _context = context;
            _crypto = crypto;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        /// <summary>
        /// Om der findes globale Google Drive-systemindstillinger (ClientId etc.)
        /// sat af SysAdmin.
        /// </summary>
        public bool HasGlobalDriveSettings { get; set; }

        public class InputModel
        {
            public bool IsEnabled { get; set; }

            public string RootFolderId { get; set; } = string.Empty;

            // Kun info / status
            public string? ConnectedAccountEmail { get; set; }

            public bool HasRefreshToken { get; set; }
        }

        /// <summary>
        /// Henter eller opretter en enkelt GoogleDriveConfig-række.
        /// (På sigt kan den blive koblet til en OrganizationId.)
        /// </summary>
        private async Task<GoogleDriveConfig> GetOrCreateConfigAsync(CancellationToken ct)
        {
            var cfg = await _context.GoogleDriveConfigs.FirstOrDefaultAsync(ct);
            if (cfg == null)
            {
                cfg = new GoogleDriveConfig
                {
                    IsEnabled = false,
                    RootFolderId = string.Empty
                };
                _context.GoogleDriveConfigs.Add(cfg);
                await _context.SaveChangesAsync(ct);
            }

            return cfg;
        }

        /// <summary>
        /// Henter globale Google Drive-systemindstillinger (sat af SysAdmin).
        /// Returnerer null hvis der ikke er nogen.
        /// </summary>
        private async Task<GoogleDriveSystemSetting?> GetSystemSettingsAsync(CancellationToken ct)
        {
            return await _context.GoogleDriveSystemSettings.FirstOrDefaultAsync(ct);
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var ct = HttpContext.RequestAborted;

            var cfg = await GetOrCreateConfigAsync(ct);
            var systemSettings = await GetSystemSettingsAsync(ct);

            HasGlobalDriveSettings = systemSettings != null &&
                                     !string.IsNullOrWhiteSpace(systemSettings.ClientId) &&
                                     !string.IsNullOrWhiteSpace(systemSettings.EncryptedClientSecret);

            Input = new InputModel
            {
                IsEnabled = cfg.IsEnabled,
                RootFolderId = cfg.RootFolderId,
                ConnectedAccountEmail = cfg.ConnectedAccountEmail,
                HasRefreshToken = !string.IsNullOrWhiteSpace(cfg.EncryptedRefreshToken)
            };

            return Page();
        }

        /// <summary>
        /// Gemmer lokale (org/system) Drive-indstillinger:
        /// - IsEnabled
        /// - RootFolderId
        /// </summary>
        public async Task<IActionResult> OnPostSaveAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var ct = HttpContext.RequestAborted;

            var cfg = await GetOrCreateConfigAsync(ct);

            cfg.IsEnabled = Input.IsEnabled;
            cfg.RootFolderId = Input.RootFolderId?.Trim() ?? string.Empty;

            await _context.SaveChangesAsync(ct);

            TempData["SuccessMessage"] = "Google Drive opsætning gemt.";
            return RedirectToPage();
        }

        /// <summary>
        /// Starter OAuth-flowet for at forbinde organisationens Google Drive.
        /// Bruger globale ClientId/Secret fra GoogleDriveSystemSetting.
        /// </summary>
        public async Task<IActionResult> OnPostConnectAsync()
        {
            var ct = HttpContext.RequestAborted;

            var cfg = await GetOrCreateConfigAsync(ct);
            var systemSettings = await GetSystemSettingsAsync(ct);

            if (systemSettings == null ||
                string.IsNullOrWhiteSpace(systemSettings.ClientId) ||
                string.IsNullOrWhiteSpace(systemSettings.EncryptedClientSecret))
            {
                TempData["ErrorMessage"] =
                    "Google Drive klienten er ikke konfigureret af systemadministrator. Kontakt SysAdmin.";
                return RedirectToPage();
            }

            string clientSecret;
            try
            {
                clientSecret = _crypto.Unprotect(systemSettings.EncryptedClientSecret);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fejl ved dekryptering af globalt Google Drive client secret.");
                TempData["ErrorMessage"] = "Fejl ved dekryptering af global Google Drive konfiguration.";
                return RedirectToPage();
            }

            var redirectUri = Url.Page(
                "/Management/ServiceManagement/GoogleDriveService",
                pageHandler: "OAuthCallback",
                values: null,
                protocol: Request.Scheme);

            var flow = new GoogleAuthorizationCodeFlow(
                new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = new ClientSecrets
                    {
                        ClientId = systemSettings.ClientId,
                        ClientSecret = clientSecret
                    },
                    Scopes = new[]
                    {
                        DriveService.Scope.DriveFile,
                        DriveService.Scope.DriveMetadataReadonly
                    }
                });

            var authUrl = flow.CreateAuthorizationCodeRequest(redirectUri).Build();

            return Redirect(authUrl.ToString());
        }

        /// <summary>
        /// Callback fra Google efter OAuth. Her får vi authorization code,
        /// bytter den til tokens og gemmer refresh-token i GoogleDriveConfig.
        /// </summary>
        public async Task<IActionResult> OnGetOAuthCallbackAsync(string? code, string? error)
        {
            var ct = HttpContext.RequestAborted;

            if (!string.IsNullOrEmpty(error))
            {
                TempData["ErrorMessage"] = $"Google OAuth fejl: {error}";
                return RedirectToPage();
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                TempData["ErrorMessage"] = "Manglende authorization code fra Google.";
                return RedirectToPage();
            }

            var cfg = await GetOrCreateConfigAsync(ct);
            var systemSettings = await GetSystemSettingsAsync(ct);

            if (systemSettings == null ||
                string.IsNullOrWhiteSpace(systemSettings.ClientId) ||
                string.IsNullOrWhiteSpace(systemSettings.EncryptedClientSecret))
            {
                TempData["ErrorMessage"] = "Global Google Drive klientkonfiguration mangler.";
                return RedirectToPage();
            }

            string clientSecret;
            try
            {
                clientSecret = _crypto.Unprotect(systemSettings.EncryptedClientSecret);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fejl ved dekryptering af globalt Google Drive client secret (callback).");
                TempData["ErrorMessage"] = "Fejl ved dekryptering af global Google Drive konfiguration.";
                return RedirectToPage();
            }

            var redirectUri = Url.Page(
                "/Management/ServiceManagement/GoogleDriveService",
                pageHandler: "OAuthCallback",
                values: null,
                protocol: Request.Scheme);

            var flow = new GoogleAuthorizationCodeFlow(
                new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = new ClientSecrets
                    {
                        ClientId = systemSettings.ClientId,
                        ClientSecret = clientSecret
                    },
                    Scopes = new[]
                    {
                        DriveService.Scope.DriveFile,
                        DriveService.Scope.DriveMetadataReadonly
                    }
                });

            // "org" her er bare et internt id/label – kunne senere være OrganizationId.
            TokenResponse token;
            try
            {
                token = await flow.ExchangeCodeForTokenAsync(
                    userId: "org",
                    code: code,
                    redirectUri: redirectUri,
                    taskCancellationToken: ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fejl ved ExchangeCodeForTokenAsync i Google OAuth callback.");
                TempData["ErrorMessage"] = "Kunne ikke udveksle authorization code til tokens.";
                return RedirectToPage();
            }

            if (string.IsNullOrWhiteSpace(token.RefreshToken))
            {
                TempData["ErrorMessage"] =
                    "Google returnerede ikke noget refresh token. Sørg for at appen er konfigureret med offline access, " +
                    "og at du godkender med 'Consent' igen, hvis du tidligere har accepteret.";
                return RedirectToPage();
            }

            // Gem refresh token i org/system config
            cfg.EncryptedRefreshToken = _crypto.Protect(token.RefreshToken);

            // Hent email-adresse på den konto, som blev brugt til at forbinde
            var cred = new UserCredential(flow, "org", token);

            var drive = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = cred,
                ApplicationName = "MinaGroup.Backend"
            });

            try
            {
                var aboutReq = drive.About.Get();
                aboutReq.Fields = "user(emailAddress)";
                var about = await aboutReq.ExecuteAsync(ct);

                cfg.ConnectedAccountEmail = about.User?.EmailAddress;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fejl ved hentning af Google Drive brugerinfo efter OAuth.");
                // Hvis mailen fejler, vil vi stadig gerne gemme refresh token.
            }

            await _context.SaveChangesAsync(ct);

            TempData["SuccessMessage"] =
                $"Google Drive er nu forbundet til {cfg.ConnectedAccountEmail ?? "kontoen"}.";

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDisconnectAsync()
        {
            var ct = HttpContext.RequestAborted;

            var cfg = await GetOrCreateConfigAsync(ct);

            cfg.EncryptedRefreshToken = null;
            cfg.ConnectedAccountEmail = null;

            await _context.SaveChangesAsync(ct);

            TempData["SuccessMessage"] = "Forbindelsen til Google Drive er fjernet.";
            return RedirectToPage();
        }
    }
}