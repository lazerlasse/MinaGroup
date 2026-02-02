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
        private const string ProviderName = "GoogleDrive";

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

        public async Task<DriveUploadResult> UploadPdfForOrganizationAsync(
            int organizationId,
            string citizenName,
            string fileName,
            Stream pdfStream,
            CancellationToken cancellationToken = default)
        {
            if (organizationId <= 0)
                throw new ArgumentOutOfRangeException(nameof(organizationId));

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("fileName må ikke være tom.", nameof(fileName));

            if (pdfStream == null || !pdfStream.CanRead)
                throw new ArgumentException("pdfStream skal være en læsbar stream.", nameof(pdfStream));

            // Provider settings (global clientId/secret)
            var provider = await _db.IntegrationProviderSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProviderName == ProviderName, cancellationToken);

            // Org integration (root folder + refresh token + toggles)
            var integration = await _db.OrganizationStorageIntegrations
                .FirstOrDefaultAsync(i => i.OrganizationId == organizationId &&
                                          i.ProviderName == ProviderName,
                                      cancellationToken);

            if (provider == null)
            {
                var msg = "Google Drive: Provider settings mangler (IntegrationProviderSettings).";
                _logger.LogWarning("{Msg} OrgId={OrgId}", msg, organizationId);
                return DriveUploadResult.Skipped(msg);
            }

            if (integration == null)
            {
                var msg = "Google Drive: Organisationens integration findes ikke.";
                _logger.LogInformation("{Msg} OrgId={OrgId}", msg, organizationId);
                return DriveUploadResult.Skipped(msg);
            }

            if (!integration.IsEnabled)
            {
                var msg = "Google Drive: Upload er slået fra i organisationens indstillinger.";
                _logger.LogInformation("{Msg} OrgId={OrgId}", msg, organizationId);
                return DriveUploadResult.Skipped(msg);
            }

            if (!integration.IsConnected)
            {
                var msg = "Google Drive: Organisationen er ikke forbundet.";
                _logger.LogInformation("{Msg} OrgId={OrgId}", msg, organizationId);
                return DriveUploadResult.Skipped(msg);
            }

            if (string.IsNullOrWhiteSpace(integration.RootFolderId))
            {
                var msg = "Google Drive: RootFolderId er ikke sat for organisationen.";
                _logger.LogWarning("{Msg} OrgId={OrgId}", msg, organizationId);
                return DriveUploadResult.Skipped(msg);
            }

            // Validate encrypted values before unprotect (avoids null warnings + clearer errors)
            if (string.IsNullOrWhiteSpace(provider.EncryptedClientId) ||
                string.IsNullOrWhiteSpace(provider.EncryptedClientSecret))
            {
                var msg = "Google Drive: Provider settings mangler krypteret ClientId/ClientSecret.";
                _logger.LogWarning("{Msg} OrgId={OrgId}", msg, organizationId);
                return DriveUploadResult.Failed(msg);
            }

            if (string.IsNullOrWhiteSpace(integration.EncryptedRefreshToken))
            {
                var msg = "Google Drive: Refresh token mangler.";
                _logger.LogWarning("{Msg} OrgId={OrgId}", msg, organizationId);
                return DriveUploadResult.Skipped(msg);
            }

            // Decrypt secrets
            string clientId;
            string clientSecret;
            string refreshToken;

            try
            {
                clientId = _cryptoService.Unprotect(provider.EncryptedClientId);
                clientSecret = _cryptoService.Unprotect(provider.EncryptedClientSecret);
                refreshToken = _cryptoService.Unprotect(integration.EncryptedRefreshToken);
            }
            catch (Exception ex)
            {
                var msg = "Google Drive: Fejl ved dekryptering af credentials.";
                _logger.LogError(ex, "{Msg} OrgId={OrgId}", msg, organizationId);
                return DriveUploadResult.Failed(msg);
            }

            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                var msg = "Google Drive: Refresh token er tom efter dekryptering.";
                _logger.LogWarning("{Msg} OrgId={OrgId}", msg, organizationId);
                return DriveUploadResult.Skipped(msg);
            }

            // Ensure stream position
            if (pdfStream.CanSeek)
                pdfStream.Position = 0;

            // Build OAuth flow + credential
            // ✅ Vi holder os til DriveFile scope => ingen reconnect nødvendig.
            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = clientId,
                    ClientSecret = clientSecret
                },
                Scopes = new[]
                {
                    DriveService.Scope.DriveFile
                }
            });

            var token = new TokenResponse { RefreshToken = refreshToken };
            var credential = new UserCredential(flow, $"org-{organizationId}", token);

            var drive = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "MinaGroup.Backend"
            });

            try
            {
                string? uploadedFileId = null;
                string? citizenFolderId = null;

                await ExecuteWithRetryAsync(async () =>
                {
                    var safeFolderName = SanitizeFolderName(citizenName);
                    var parentFolderId = integration.RootFolderId!.Trim();

                    // 1) Find/opret borgermappe
                    citizenFolderId = await GetOrCreateFolderAsync(
                        drive,
                        parentFolderId,
                        safeFolderName,
                        cancellationToken);

                    // 2) Find eksisterende fil med samme navn (for overskriv)
                    var trimmedFileName = fileName.Trim();

                    var existingFileId = await FindFileIdByNameAsync(
                        drive,
                        citizenFolderId,
                        trimmedFileName,
                        cancellationToken);

                    if (pdfStream.CanSeek)
                        pdfStream.Position = 0;

                    if (!string.IsNullOrWhiteSpace(existingFileId))
                    {
                        // Overskriv eksisterende fil via Update (media upload)
                        var metadata = new Google.Apis.Drive.v3.Data.File
                        {
                            Name = trimmedFileName
                        };

                        var update = drive.Files.Update(metadata, existingFileId, pdfStream, "application/pdf");
                        // ✅ Brug kun 'modifiedTime' i fields; vi læser via ModifiedTimeDateTimeOffset i response/list.
                        update.Fields = "id, name, parents, modifiedTime";

                        var progress = await update.UploadAsync(cancellationToken);

                        if (progress.Status != Google.Apis.Upload.UploadStatus.Completed)
                        {
                            throw new GoogleApiException("Drive", $"Update upload mislykkedes: {progress.Status} {progress.Exception?.Message}")
                            {
                                Error = progress.Exception is GoogleApiException gae ? gae.Error : null
                            };
                        }

                        var updated = update.ResponseBody;
                        uploadedFileId = updated?.Id ?? existingFileId;

                        _logger.LogInformation(
                            "GoogleDrive upload: Updated existing {FileName} in folder {FolderId}. DriveFileId={DriveFileId} OrgId={OrgId}",
                            trimmedFileName, citizenFolderId, uploadedFileId, organizationId);

                        return 0;
                    }

                    // Opret ny fil via Create
                    var driveFile = new Google.Apis.Drive.v3.Data.File
                    {
                        Name = trimmedFileName,
                        Parents = new List<string> { citizenFolderId }
                    };

                    var create = drive.Files.Create(driveFile, pdfStream, "application/pdf");
                    // ✅ Undgå createdTime/modifiedTime obsolete properties i vores kode. (feltet 'modifiedTime' er ok)
                    create.Fields = "id, name, parents, modifiedTime";

                    var uploadProgress = await create.UploadAsync(cancellationToken);

                    if (uploadProgress.Status != Google.Apis.Upload.UploadStatus.Completed)
                    {
                        throw new GoogleApiException("Drive", $"Create upload mislykkedes: {uploadProgress.Status} {uploadProgress.Exception?.Message}")
                        {
                            Error = uploadProgress.Exception is GoogleApiException gae ? gae.Error : null
                        };
                    }

                    var uploaded = create.ResponseBody;
                    uploadedFileId = uploaded?.Id;

                    _logger.LogInformation(
                        "GoogleDrive upload: Created {FileName} in folder {FolderId}. DriveFileId={DriveFileId} OrgId={OrgId}",
                        trimmedFileName, citizenFolderId, uploadedFileId, organizationId);

                    return 0;

                }, _logger, cancellationToken);

                return DriveUploadResult.Uploaded(
                    message: "PDF blev uploadet til Google Drive (oprettet/overskrevet).",
                    fileId: uploadedFileId,
                    folderId: citizenFolderId);
            }
            catch (Exception ex)
            {
                var msg = "Google Drive: Upload fejlede.";
                _logger.LogError(ex, "{Msg} OrgId={OrgId} File={File}", msg, organizationId, fileName);
                return DriveUploadResult.Failed($"{msg} {ex.Message}");
            }
        }

        private static async Task<string?> FindFileIdByNameAsync(
            DriveService drive,
            string parentFolderId,
            string fileName,
            CancellationToken ct)
        {
            // Finder kun i den konkrete mappe.
            // Med drive.file vil dette typisk kun “se” filer appen har adgang til (vores egne) – ønsket adfærd.
            var escapedName = fileName.Replace("'", "\\'");

            var q =
                $"name='{escapedName}' " +
                $"and '{parentFolderId}' in parents " +
                $"and mimeType='application/pdf' " +
                $"and trashed=false";

            var listReq = drive.Files.List();
            listReq.Q = q;

            // ✅ Fields er stadig 'modifiedTime'/'createdTime' på wire-niveau, men vi læser via DateTimeOffset properties.
            listReq.Fields = "files(id, name, modifiedTime, createdTime)";
            listReq.PageSize = 10;

            var list = await listReq.ExecuteAsync(ct);

            // Hvis der mod forventning ligger flere med samme navn, vælg nyeste ud fra DateTimeOffset properties.
            var best = list.Files?
                .OrderByDescending(GetBestTimestampUtc)
                .FirstOrDefault();

            return best?.Id;
        }

        private static DateTimeOffset GetBestTimestampUtc(Google.Apis.Drive.v3.Data.File f)
        {
            // ✅ Brug de ikke-obsolete DateTimeOffset properties.
            // (Hvis en af dem er null, fallback til den anden. Hvis begge null => MinValue)
            var mod = f.ModifiedTimeDateTimeOffset;
            if (mod.HasValue) return mod.Value;

            var created = f.CreatedTimeDateTimeOffset;
            if (created.HasValue) return created.Value;

            return DateTimeOffset.MinValue;
        }

        private static string SanitizeFolderName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "Ukendt borger";

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

        private static async Task<T> ExecuteWithRetryAsync<T>(
            Func<Task<T>> action,
            ILogger logger,
            CancellationToken ct)
        {
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
                    if (attempt >= delays.Length)
                        throw;

                    logger.LogWarning(ex, "GoogleDrive timeout (attempt {Attempt}). Retrying in {Delay}…", attempt + 1, delays[attempt]);
                    await Task.Delay(delays[attempt], ct);
                }
            }
        }

        private static bool IsTransientGoogleError(GoogleApiException ex)
        {
            var code = (int)ex.HttpStatusCode;
            return code == 429 || code == 500 || code == 502 || code == 503 || code == 504;
        }
    }
}