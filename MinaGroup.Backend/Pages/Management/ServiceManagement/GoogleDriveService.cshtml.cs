using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MinaGroup.Backend.Data;
using MinaGroup.Backend.Models;
using MinaGroup.Backend.Services.Interfaces;
using System.Security.Cryptography;

namespace MinaGroup.Backend.Pages.Management.ServiceManagement
{
    [Authorize(Roles = "Admin")]
    public class GoogleDriveServiceModel : PageModel
    {
        private const string ProviderName = "GoogleDrive";
        private const string TempDataOAuthStateKey = "GoogleDriveOAuthState";

        private readonly AppDbContext _context;
        private readonly ICryptoService _crypto;
        private readonly ILogger<GoogleDriveServiceModel> _logger;
        private readonly UserManager<AppUser> _userManager;

        public GoogleDriveServiceModel(
            AppDbContext context,
            ICryptoService crypto,
            ILogger<GoogleDriveServiceModel> logger,
            UserManager<AppUser> userManager)
        {
            _context = context;
            _crypto = crypto;
            _logger = logger;
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public bool HasGlobalDriveSettings { get; set; }

        public class InputModel
        {
            public bool IsEnabled { get; set; }
            public string RootFolderId { get; set; } = string.Empty;

            public string? ConnectedAccountEmail { get; set; }
            public bool HasRefreshToken { get; set; }
        }

        private async Task<GoogleDriveSystemSetting?> GetSystemSettingsAsync(CancellationToken ct)
            => await _context.GoogleDriveSystemSettings.FirstOrDefaultAsync(ct);

        private async Task<OrganizationStorageIntegration> GetOrCreateOrgIntegrationAsync(int orgId, CancellationToken ct)
        {
            var integration = await _context.OrganizationStorageIntegrations
                .FirstOrDefaultAsync(x => x.OrganizationId == orgId && x.ProviderName == ProviderName, ct);

            if (integration == null)
            {
                integration = new OrganizationStorageIntegration
                {
                    OrganizationId = orgId,
                    ProviderName = ProviderName,
                    IsConnected = false,
                    ConnectedAt = null,
                    RootFolderId = string.Empty,
                    EncryptedRefreshToken = null,
                    ConnectedAccountEmail = null,
                    IsEnabled = true
                };

                _context.OrganizationStorageIntegrations.Add(integration);
                await _context.SaveChangesAsync(ct);
            }

            return integration;
        }

        private async Task EnsureProviderSettingsRowAsync(GoogleDriveSystemSetting systemSettings, CancellationToken ct)
        {
            // Upload-service forventer IntegrationProviderSettings med krypteret clientId/secret.
            var provider = await _context.IntegrationProviderSettings
                .FirstOrDefaultAsync(p => p.ProviderName == ProviderName, ct);

            var clientId = systemSettings.ClientId?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(systemSettings.EncryptedClientSecret))
            {
                _logger.LogError("GoogleDrive: Globale system settings mangler ClientId og/eller EncryptedClientSecret.");
                throw new InvalidOperationException("Global Google Drive klientkonfiguration mangler.");
            }

            string clientSecret;
            try
            {
                clientSecret = _crypto.Unprotect(systemSettings.EncryptedClientSecret);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GoogleDrive: Fejl ved dekryptering af global client secret (EnsureProviderSettingsRow).");
                throw;
            }

            // Scopes gemmes kun som info/metadata – selve flowet bruger scopes-arrayet.
            const string scopes = "drive.file drive.metadata.readonly";

            if (provider == null)
            {
                provider = new IntegrationProviderSettings
                {
                    ProviderName = ProviderName,
                    EncryptedClientId = _crypto.Protect(clientId),
                    EncryptedClientSecret = _crypto.Protect(clientSecret),
                    AuthorizationEndpoint = null,
                    TokenEndpoint = null,
                    Scopes = scopes
                };
                _context.IntegrationProviderSettings.Add(provider);
            }
            else
            {
                provider.EncryptedClientId = _crypto.Protect(clientId);
                provider.EncryptedClientSecret = _crypto.Protect(clientSecret);
                provider.Scopes = scopes;
            }

            await _context.SaveChangesAsync(ct);
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var ct = HttpContext.RequestAborted;

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.OrganizationId == null)
                return Unauthorized();

            var orgId = currentUser.OrganizationId.Value;

            var systemSettings = await GetSystemSettingsAsync(ct);

            HasGlobalDriveSettings = systemSettings != null
                                    && !string.IsNullOrWhiteSpace(systemSettings.ClientId)
                                    && !string.IsNullOrWhiteSpace(systemSettings.EncryptedClientSecret);

            var integration = await GetOrCreateOrgIntegrationAsync(orgId, ct);

            Input = new InputModel
            {
                IsEnabled = integration.IsEnabled,
                RootFolderId = integration.RootFolderId ?? string.Empty,
                ConnectedAccountEmail = integration.ConnectedAccountEmail,
                HasRefreshToken = !string.IsNullOrWhiteSpace(integration.EncryptedRefreshToken)
            };

            return Page();
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            var ct = HttpContext.RequestAborted;

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.OrganizationId == null)
                return Unauthorized();

            var orgId = currentUser.OrganizationId.Value;

            var integration = await GetOrCreateOrgIntegrationAsync(orgId, ct);

            // ✅ Gem både toggle + root folder
            integration.IsEnabled = Input.IsEnabled;
            integration.RootFolderId = Input.RootFolderId?.Trim() ?? string.Empty;

            await _context.SaveChangesAsync(ct);

            TempData["SuccessMessage"] = "Google Drive opsætning gemt.";
            return RedirectToPage();
        }

        public Task<IActionResult> OnPostReconnectAsync()
        {
            // Force samme flow som connect (prompt=consent + access_type=offline)
            return OnPostConnectAsync();
        }

        public async Task<IActionResult> OnPostConnectAsync()
        {
            var ct = HttpContext.RequestAborted;

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.OrganizationId == null)
                return Unauthorized();

            var orgId = currentUser.OrganizationId.Value;

            var systemSettings = await GetSystemSettingsAsync(ct);
            if (systemSettings == null ||
                string.IsNullOrWhiteSpace(systemSettings.ClientId) ||
                string.IsNullOrWhiteSpace(systemSettings.EncryptedClientSecret))
            {
                TempData["ErrorMessage"] = "Google Drive klienten er ikke konfigureret af SysAdmin.";
                return RedirectToPage();
            }

            // Sørg for at provider settings findes, så upload-service kan finde dem
            try
            {
                await EnsureProviderSettingsRowAsync(systemSettings, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GoogleDrive: Could not ensure provider settings row.");
                TempData["ErrorMessage"] = "Kunne ikke klargøre ProviderSettings. Tjek logs.";
                return RedirectToPage();
            }

            string clientSecret;
            try
            {
                clientSecret = _crypto.Unprotect(systemSettings.EncryptedClientSecret);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fejl ved dekryptering af globalt Google Drive client secret (Connect).");
                TempData["ErrorMessage"] = "Fejl ved dekryptering af global Google Drive konfiguration.";
                return RedirectToPage();
            }

            var redirectUri = Url.Page(
                "/Management/ServiceManagement/GoogleDriveService",
                pageHandler: "OAuthCallback",
                values: null,
                protocol: Request.Scheme);

            if (string.IsNullOrWhiteSpace(redirectUri))
            {
                TempData["ErrorMessage"] = "Kunne ikke generere redirect URI.";
                return RedirectToPage();
            }

            var state = CreateStateToken();
            TempData[TempDataOAuthStateKey] = state;

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

            // Base URL
            var authUrl = flow.CreateAuthorizationCodeRequest(redirectUri).Build().ToString();

            // ✅ Offline + consent (kræves for at få refresh token) - uden at bruge properties der ikke findes i lib
            authUrl = SetOrReplaceQuery(authUrl, "access_type", "offline");
            authUrl = SetOrReplaceQuery(authUrl, "prompt", "consent");
            authUrl = SetOrReplaceQuery(authUrl, "include_granted_scopes", "true");
            authUrl = SetOrReplaceQuery(authUrl, "state", state);

            return Redirect(authUrl);
        }

        public async Task<IActionResult> OnGetOAuthCallbackAsync(string? code, string? error, string? state)
        {
            var ct = HttpContext.RequestAborted;

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.OrganizationId == null)
                return Unauthorized();

            var orgId = currentUser.OrganizationId.Value;

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

            var expectedState = TempData[TempDataOAuthStateKey] as string;
            if (string.IsNullOrWhiteSpace(expectedState) || !string.Equals(expectedState, state, StringComparison.Ordinal))
            {
                TempData["ErrorMessage"] = "Ugyldigt OAuth state. Prøv at forbinde igen.";
                return RedirectToPage();
            }

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
                _logger.LogError(ex, "Fejl ved dekryptering af globalt Google Drive client secret (Callback).");
                TempData["ErrorMessage"] = "Fejl ved dekryptering af global Google Drive konfiguration.";
                return RedirectToPage();
            }

            var redirectUri = Url.Page(
                "/Management/ServiceManagement/GoogleDriveService",
                pageHandler: "OAuthCallback",
                values: null,
                protocol: Request.Scheme);

            if (string.IsNullOrWhiteSpace(redirectUri))
            {
                TempData["ErrorMessage"] = "Kunne ikke generere redirect URI (callback).";
                return RedirectToPage();
            }

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

            TokenResponse token;
            try
            {
                token = await flow.ExchangeCodeForTokenAsync(
                    userId: $"org-{orgId}",
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
                    "Google returnerede ikke noget refresh token. Fjern app-adgang under Google-kontoens Connected apps og prøv igen (brug også 'Forbind igen (force)').";
                return RedirectToPage();
            }

            var integration = await GetOrCreateOrgIntegrationAsync(orgId, ct);

            integration.EncryptedRefreshToken = _crypto.Protect(token.RefreshToken);
            integration.IsConnected = true;
            integration.ConnectedAt = DateTime.UtcNow;

            // Nice-to-have: email for den konto, der forbandt
            try
            {
                var cred = new UserCredential(flow, $"org-{orgId}", token);

                var drive = new DriveService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = cred,
                    ApplicationName = "MinaGroup.Backend"
                });

                var aboutReq = drive.About.Get();
                aboutReq.Fields = "user(emailAddress)";
                var about = await aboutReq.ExecuteAsync(ct);

                integration.ConnectedAccountEmail = about.User?.EmailAddress;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fejl ved hentning af Google Drive brugerinfo efter OAuth.");
            }

            await _context.SaveChangesAsync(ct);

            TempData["SuccessMessage"] =
                $"Google Drive er nu forbundet{(string.IsNullOrWhiteSpace(integration.ConnectedAccountEmail) ? "." : $" til {integration.ConnectedAccountEmail}.")}";

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDisconnectAsync()
        {
            var ct = HttpContext.RequestAborted;

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.OrganizationId == null)
                return Unauthorized();

            var orgId = currentUser.OrganizationId.Value;

            var integration = await GetOrCreateOrgIntegrationAsync(orgId, ct);

            integration.EncryptedRefreshToken = null;
            integration.ConnectedAccountEmail = null;
            integration.IsConnected = false;
            integration.ConnectedAt = null;

            await _context.SaveChangesAsync(ct);

            TempData["SuccessMessage"] = "Forbindelsen til Google Drive er fjernet.";
            return RedirectToPage();
        }

        private static string CreateStateToken()
        {
            Span<byte> bytes = stackalloc byte[16];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToHexString(bytes);
        }

        private static string SetOrReplaceQuery(string url, string key, string value)
        {
            var uri = new Uri(url);
            var pairs = new List<KeyValuePair<string, string>>();

            if (!string.IsNullOrWhiteSpace(uri.Query))
            {
                foreach (var part in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
                {
                    var kv = part.Split('=', 2);
                    var k = Uri.UnescapeDataString(kv[0]);
                    var v = kv.Length > 1 ? Uri.UnescapeDataString(kv[1]) : string.Empty;

                    if (!string.Equals(k, key, StringComparison.Ordinal))
                        pairs.Add(new KeyValuePair<string, string>(k, v));
                }
            }

            pairs.Add(new KeyValuePair<string, string>(key, value));

            // ✅ FIX: korrekt key=value (ikke key=value=value)
            var newQuery = string.Join("&",
                pairs.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));

            return new UriBuilder(uri) { Query = newQuery }.Uri.ToString();
        }
    }
}