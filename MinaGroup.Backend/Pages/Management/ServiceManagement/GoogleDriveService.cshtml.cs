using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
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

    [Authorize(Roles = "Admin,SysAdmin")]
    public class GoogleDriveServiceModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly ICryptoService _crypto;
        private readonly ILogger<GoogleDriveServiceModel> _logger;

        public GoogleDriveServiceModel(AppDbContext context, ICryptoService crypto, ILogger<GoogleDriveServiceModel> logger)
        {
            _context = context;
            _crypto = crypto;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            public bool IsEnabled { get; set; }

            public string RootFolderId { get; set; } = string.Empty;

            // Vises/indtastes i klar tekst, men gemmes krypteret
            public string? ClientId { get; set; }

            public string? ClientSecret { get; set; }

            public string? ConnectedAccountEmail { get; set; }

            public bool HasRefreshToken { get; set; }
        }

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

        public async Task<IActionResult> OnGetAsync()
        {
            var cfg = await GetOrCreateConfigAsync(HttpContext.RequestAborted);

            string? clientId = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(cfg.EncryptedClientId))
                    clientId = _crypto.Unprotect(cfg.EncryptedClientId);
            }
            catch
            {
                clientId = null;
            }

            Input = new InputModel
            {
                IsEnabled = cfg.IsEnabled,
                RootFolderId = cfg.RootFolderId,
                ClientId = clientId,
                // Vi viser aldrig secret eller refresh token i UI
                ClientSecret = string.Empty,
                ConnectedAccountEmail = cfg.ConnectedAccountEmail,
                HasRefreshToken = !string.IsNullOrWhiteSpace(cfg.EncryptedRefreshToken)
            };

            return Page();
        }

        // Gemmer generelle settings + ClientId/Secret (men ikke refresh token)
        public async Task<IActionResult> OnPostSaveAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var cfg = await GetOrCreateConfigAsync(HttpContext.RequestAborted);

            cfg.IsEnabled = Input.IsEnabled;
            cfg.RootFolderId = Input.RootFolderId?.Trim() ?? string.Empty;

            // ClientId/Secret opdateres kun hvis der er skrevet noget
            if (!string.IsNullOrWhiteSpace(Input.ClientId))
                cfg.EncryptedClientId = _crypto.Protect(Input.ClientId.Trim());

            if (!string.IsNullOrWhiteSpace(Input.ClientSecret))
                cfg.EncryptedClientSecret = _crypto.Protect(Input.ClientSecret.Trim());

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Google Drive opsætning gemt.";
            return RedirectToPage();
        }

        // Starter OAuth flow (redirecter til Google)
        public async Task<IActionResult> OnPostConnectAsync()
        {
            var cfg = await GetOrCreateConfigAsync(HttpContext.RequestAborted);

            if (string.IsNullOrWhiteSpace(cfg.EncryptedClientId) ||
                string.IsNullOrWhiteSpace(cfg.EncryptedClientSecret))
            {
                TempData["ErrorMessage"] = "Angiv først Client ID og Client Secret og gem.";
                return RedirectToPage();
            }

            string clientId = _crypto.Unprotect(cfg.EncryptedClientId);
            string clientSecret = _crypto.Unprotect(cfg.EncryptedClientSecret);

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
                        ClientId = clientId,
                        ClientSecret = clientSecret
                    },
                    Scopes = [DriveService.Scope.DriveFile, DriveService.Scope.DriveMetadataReadonly]
                });

            var authUrl = flow.CreateAuthorizationCodeRequest(redirectUri).Build();

            return Redirect(authUrl.ToString());
        }

        // Callback fra Google: vi får ?code=...
        public async Task<IActionResult> OnGetOAuthCallbackAsync(string? code, string? error)
        {
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

            var cfg = await GetOrCreateConfigAsync(HttpContext.RequestAborted);

            if (string.IsNullOrWhiteSpace(cfg.EncryptedClientId) ||
                string.IsNullOrWhiteSpace(cfg.EncryptedClientSecret))
            {
                TempData["ErrorMessage"] = "Client ID/Secret mangler i config.";
                return RedirectToPage();
            }

            string clientId = _crypto.Unprotect(cfg.EncryptedClientId);
            string clientSecret = _crypto.Unprotect(cfg.EncryptedClientSecret);

            var redirectUri = Url.Page(
                "/Management/Settings/GoogleDrive",
                pageHandler: "OAuthCallback",
                values: null,
                protocol: Request.Scheme);

            var flow = new GoogleAuthorizationCodeFlow(
                new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = new ClientSecrets
                    {
                        ClientId = clientId,
                        ClientSecret = clientSecret
                    },
                    Scopes = new[] { DriveService.Scope.DriveFile, DriveService.Scope.DriveMetadataReadonly }
                });

            // "admin" her er bare et key/subject-id til intern brug
            var token = await flow.ExchangeCodeForTokenAsync(
                userId: "admin",
                code: code,
                redirectUri: redirectUri,
                taskCancellationToken: HttpContext.RequestAborted);

            if (string.IsNullOrWhiteSpace(token.RefreshToken))
            {
                TempData["ErrorMessage"] = "Google returnerede ikke noget refresh token. Sørg for at du ikke allerede har godkendt appen uden at slå 'offline access' til.";
                return RedirectToPage();
            }

            cfg.EncryptedRefreshToken = _crypto.Protect(token.RefreshToken);

            // Hent email-adresse på den konto, der blev brugt
            var cred = new UserCredential(flow, "admin", token);

            var drive = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = cred,
                ApplicationName = "MinaGroup.Backend"
            });

            var aboutReq = drive.About.Get();
            aboutReq.Fields = "user(emailAddress)";
            var about = await aboutReq.ExecuteAsync();

            cfg.ConnectedAccountEmail = about.User?.EmailAddress;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Google Drive er nu forbundet til {cfg.ConnectedAccountEmail}.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDisconnectAsync()
        {
            var cfg = await GetOrCreateConfigAsync(HttpContext.RequestAborted);

            cfg.EncryptedRefreshToken = null;
            cfg.ConnectedAccountEmail = null;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Forbindelsen til Google Drive er fjernet.";
            return RedirectToPage();
        }
    }
}
