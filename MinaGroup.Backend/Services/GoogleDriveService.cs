using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MinaGroup.Backend.Data;
using MinaGroup.Backend.Models;
using MinaGroup.Backend.Services.Interfaces;

namespace MinaGroup.Backend.Services
{
    public class GoogleDriveService : IGoogleDriveService
    {
        private readonly AppDbContext _db;
        private readonly ICryptoService _cryptoService;
        private readonly ILogger<GoogleDriveService> _logger;

        public GoogleDriveService(
            AppDbContext db,
            ICryptoService cryptoService,
            ILogger<GoogleDriveService> logger)
        {
            _db = db;
            _cryptoService = cryptoService;
            _logger = logger;
        }

        public async Task UploadPdfForOrganizationAsync(
            int organizationId,
            string citizenName,
            string fileName,
            Stream pdfStream,
            CancellationToken cancellationToken = default)
        {
            // Basic validation
            if (organizationId <= 0)
                throw new ArgumentOutOfRangeException(nameof(organizationId));

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("fileName må ikke være tom.", nameof(fileName));

            if (pdfStream == null || !pdfStream.CanRead)
                throw new ArgumentException("pdfStream skal være en læsbar stream.", nameof(pdfStream));

            // Load integration data
            var provider = await _db.IntegrationProviderSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProviderName == "GoogleDrive", cancellationToken);

            var integration = await _db.OrganizationStorageIntegrations
                .FirstOrDefaultAsync(i => i.OrganizationId == organizationId &&
                                          i.ProviderName == "GoogleDrive",
                                      cancellationToken);

            if (provider == null)
            {
                _logger.LogWarning("GoogleDrive upload: Provider settings mangler (IntegrationProviderSettings). OrgId={OrgId}", organizationId);
                return;
            }

            if (integration == null || !integration.IsConnected)
            {
                _logger.LogInformation("GoogleDrive upload: Organization integration mangler/ikke forbundet. OrgId={OrgId}", organizationId);
                return;
            }

            if (string.IsNullOrWhiteSpace(integration.RootFolderId))
            {
                _logger.LogWarning("GoogleDrive upload: RootFolderId er ikke sat for org. OrgId={OrgId}", organizationId);
                return;
            }

            // Decrypt secrets
            string clientId;
            string clientSecret;
            string refreshToken;

            try
            {
                clientId = _cryptoService.Unprotect(provider.EncryptedClientId);
                clientSecret = _cryptoService.Unprotect(provider.EncryptedClientSecret);

                refreshToken = string.IsNullOrWhiteSpace(integration.EncryptedRefreshToken)
                    ? string.Empty
                    : _cryptoService.Unprotect(integration.EncryptedRefreshToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GoogleDrive upload: Fejl ved dekryptering af credentials. OrgId={OrgId}", organizationId);
                return;
            }

            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                _logger.LogWarning("GoogleDrive upload: Refresh token mangler. OrgId={OrgId}", organizationId);
                return;
            }

            // Ensure stream position
            if (pdfStream.CanSeek)
                pdfStream.Position = 0;

            // Build OAuth flow + credential
            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = clientId,
                    ClientSecret = clientSecret
                },
                Scopes = new[]
                {
                    DriveService.Scope.DriveFile,
                    DriveService.Scope.DriveMetadataReadonly
                }
            });

            // userId kan være orgId, så tokens caches per org i memory hos google lib (ikke DB)
            var token = new TokenResponse { RefreshToken = refreshToken };

            var credential = new UserCredential(flow, $"org-{organizationId}", token);

            // Google lib udsteder access token on-demand via refresh token
            var drive = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "MinaGroup.Backend"
            });

            // Retry wrapper for transient issues
            await ExecuteWithRetryAsync(async () =>
            {
                // 1) Find/opret undermappe til borger (valgfri)
                var safeFolderName = SanitizeFolderName(citizenName);
                var parentFolderId = integration.RootFolderId.Trim();

                var citizenFolderId = await GetOrCreateFolderAsync(
                    drive,
                    parentFolderId,
                    safeFolderName,
                    cancellationToken);

                // 2) Upload file into citizen folder
                var driveFile = new Google.Apis.Drive.v3.Data.File
                {
                    Name = fileName.Trim(),
                    Parents = new List<string> { citizenFolderId }
                };

                var create = drive.Files.Create(driveFile, pdfStream, "application/pdf");
                create.Fields = "id, name, parents, createdTime, modifiedTime";

                var uploadProgress = await create.UploadAsync(cancellationToken);

                if (uploadProgress.Status != Google.Apis.Upload.UploadStatus.Completed)
                {
                    throw new GoogleApiException("Drive", $"Upload mislykkedes: {uploadProgress.Status} {uploadProgress.Exception?.Message}")
                    {
                        Error = uploadProgress.Exception is GoogleApiException gae ? gae.Error : null
                    };
                }

                var uploaded = create.ResponseBody;

                _logger.LogInformation(
                    "GoogleDrive upload: Uploaded {FileName} to folder {FolderId}. DriveFileId={DriveFileId} OrgId={OrgId}",
                    fileName, citizenFolderId, uploaded?.Id, organizationId);

                return 0;
            }, _logger, cancellationToken);
        }

        private static string SanitizeFolderName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "Ukendt borger";

            // Drive tillader mange tegn, men vi fjerner de mest problematiske / kosmetiske
            var invalid = Path.GetInvalidFileNameChars();
            var cleaned = new string(name.Trim().Where(c => !invalid.Contains(c)).ToArray());
            return string.IsNullOrWhiteSpace(cleaned) ? "Ukendt borger" : cleaned;
        }

        private static async Task<string> GetOrCreateFolderAsync(
            DriveService drive,
            string parentFolderId,
            string folderName,
            CancellationToken ct)
        {
            // Query for folder with same name under parent
            // NB: drive query requires escaping single quotes
            var escapedName = folderName.Replace("'", "\\'");
            var q =
                $"mimeType='application/vnd.google-apps.folder' " +
                $"and name='{escapedName}' " +
                $"and '{parentFolderId}' in parents " +
                $"and trashed=false";

            var listReq = drive.Files.List();
            listReq.Q = q;
            listReq.Fields = "files(id, name)";
            listReq.PageSize = 1;

            var list = await listReq.ExecuteAsync(ct);
            var existing = list.Files?.FirstOrDefault();
            if (existing != null && !string.IsNullOrWhiteSpace(existing.Id))
                return existing.Id;

            // Create folder
            var folder = new Google.Apis.Drive.v3.Data.File
            {
                Name = folderName,
                MimeType = "application/vnd.google-apps.folder",
                Parents = new List<string> { parentFolderId }
            };

            var createReq = drive.Files.Create(folder);
            createReq.Fields = "id";

            var created = await createReq.ExecuteAsync(ct);
            return created.Id;
        }

        /// <summary>
        /// Simple retry helper for transient Google/HTTP failures.
        /// </summary>
        private static async Task<T> ExecuteWithRetryAsync<T>(
            Func<Task<T>> action,
            ILogger logger,
            CancellationToken ct)
        {
            // Exponential-ish backoff
            var delays = new[]
            {
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(3),
                TimeSpan.FromSeconds(8)
            };

            for (int attempt = 0; ; attempt++)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    return await action();
                }
                catch (GoogleApiException ex) when (IsTransientGoogleError(ex))
                {
                    if (attempt >= delays.Length)
                        throw;

                    logger.LogWarning(ex, "GoogleDrive transient fejl (attempt {Attempt}). Retrying in {Delay}…", attempt + 1, delays[attempt]);
                    await Task.Delay(delays[attempt], ct);
                }
                catch (HttpRequestException ex)
                {
                    if (attempt >= delays.Length)
                        throw;

                    logger.LogWarning(ex, "GoogleDrive netværksfejl (attempt {Attempt}). Retrying in {Delay}…", attempt + 1, delays[attempt]);
                    await Task.Delay(delays[attempt], ct);
                }
                catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
                {
                    // Timeout
                    if (attempt >= delays.Length)
                        throw;

                    logger.LogWarning(ex, "GoogleDrive timeout (attempt {Attempt}). Retrying in {Delay}…", attempt + 1, delays[attempt]);
                    await Task.Delay(delays[attempt], ct);
                }
            }
        }

        private static bool IsTransientGoogleError(GoogleApiException ex)
        {
            // Typical transient statuses: 429 rate limit, 500/502/503/504
            var code = (int)ex.HttpStatusCode;
            return code == 429 || code == 500 || code == 502 || code == 503 || code == 504;
        }
    }
}