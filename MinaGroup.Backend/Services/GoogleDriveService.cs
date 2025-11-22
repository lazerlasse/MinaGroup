using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Microsoft.EntityFrameworkCore;
using MinaGroup.Backend.Data;
using MinaGroup.Backend.Models;
using MinaGroup.Backend.Services.Interfaces;

namespace MinaGroup.Backend.Services
{
    public class GoogleDriveService : IGoogleDriveService
    {
        private readonly AppDbContext _db;
        private readonly ICryptoService _crypto;
        private readonly ILogger<GoogleDriveService> _logger;
        private DriveService? _driveService;
        private GoogleDriveConfig? _config;

        public GoogleDriveService(
            AppDbContext db,
            ICryptoService crypto,
            ILogger<GoogleDriveService> logger)
        {
            _db = db;
            _crypto = crypto;
            _logger = logger;
        }

        private async Task<bool> EnsureInitializedAsync(CancellationToken ct)
        {
            if (_driveService != null)
                return true;

            _config = await _db.GoogleDriveConfigs.FirstOrDefaultAsync(ct);
            if (_config == null || !_config.IsEnabled)
            {
                _logger.LogInformation("Google Drive er ikke konfigureret eller er deaktiveret.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(_config.EncryptedClientId) ||
                string.IsNullOrWhiteSpace(_config.EncryptedClientSecret) ||
                string.IsNullOrWhiteSpace(_config.EncryptedRefreshToken))
            {
                _logger.LogWarning("Google Drive OAuth credentials mangler (ClientId/Secret/RefreshToken).");
                return false;
            }

            string clientId, clientSecret, refreshToken;
            try
            {
                clientId = _crypto.Unprotect(_config.EncryptedClientId);
                clientSecret = _crypto.Unprotect(_config.EncryptedClientSecret);
                refreshToken = _crypto.Unprotect(_config.EncryptedRefreshToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fejl ved dekryptering af Google Drive OAuth credentials.");
                return false;
            }

            var token = new TokenResponse
            {
                RefreshToken = refreshToken
                // AccessToken lader vi være tomt; UserCredential henter selv nye access tokens
            };

            var flow = new GoogleAuthorizationCodeFlow(
                new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = new ClientSecrets
                    {
                        ClientId = clientId,
                        ClientSecret = clientSecret
                    },
                    Scopes = new[] { DriveService.Scope.DriveFile }
                });

            // "admin" er bare en key til intern caching – ikke en rigtig bruger
            var credential = new UserCredential(flow, "admin", token);

            _driveService = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "MinaGroup.Backend"
            });

            return true;
        }

        public async Task UploadSelfEvaluationPdfAsync(SelfEvaluation evaluation, byte[] pdfBytes, CancellationToken ct = default)
        {
            if (!await EnsureInitializedAsync(ct))
            {
                _logger.LogInformation("Springer Google Drive upload over (ikke initialiseret).");
                return;
            }

            try
            {
                var cfg = _config ?? await _db.GoogleDriveConfigs.FirstAsync(ct);
                if (string.IsNullOrWhiteSpace(cfg.RootFolderId))
                {
                    _logger.LogWarning("GoogleDrive RootFolderId er ikke sat. Upload afbrydes.");
                    return;
                }

                // Borgerens navn
                var name = evaluation.User?.FullName;
                if (string.IsNullOrWhiteSpace(name))
                    name = $"{evaluation.User?.FirstName} {evaluation.User?.LastName}".Trim();
                if (string.IsNullOrWhiteSpace(name))
                    name = "Ukendt_borger";

                var safeFolderName = name.Replace("/", "_").Trim();

                var folderId = await EnsureFolderAsync(safeFolderName, cfg.RootFolderId, ct);

                var fileName = $"Selvevaluering_{safeFolderName}_{evaluation.EvaluationDate:yyyyMMdd}.pdf";

                using var stream = new MemoryStream(pdfBytes);

                var fileMetadata = new Google.Apis.Drive.v3.Data.File
                {
                    Name = fileName,
                    Parents = new[] { folderId },
                    MimeType = "application/pdf"
                };

                var uploadRequest = _driveService!.Files.Create(fileMetadata, stream, "application/pdf");
                uploadRequest.Fields = "id, name, parents";

                await uploadRequest.UploadAsync(ct);

                var uploadedFile = uploadRequest.ResponseBody;
                _logger.LogInformation(
                    "PDF uploadet til Google Drive. FileId={FileId}, Name={Name}",
                    uploadedFile.Id, uploadedFile.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fejl under upload af selvevaluering {Id} til Google Drive", evaluation.Id);
            }
        }

        private async Task<string> EnsureFolderAsync(string folderName, string parentFolderId, CancellationToken ct)
        {
            var escaped = folderName.Replace("'", "\\'");
            var listReq = _driveService!.Files.List();
            listReq.Q = $"mimeType = 'application/vnd.google-apps.folder' and " +
                        $"name = '{escaped}' and '{parentFolderId}' in parents and trashed = false";
            listReq.Spaces = "drive";
            listReq.Fields = "files(id, name)";
            listReq.PageSize = 10;

            var list = await listReq.ExecuteAsync(ct);
            if (list.Files is { Count: > 0 })
                return list.Files[0].Id;

            var folderMetadata = new Google.Apis.Drive.v3.Data.File
            {
                Name = folderName,
                MimeType = "application/vnd.google-apps.folder",
                Parents = new[] { parentFolderId }
            };

            var createReq = _driveService.Files.Create(folderMetadata);
            createReq.Fields = "id, name";
            var folder = await createReq.ExecuteAsync(ct);

            _logger.LogInformation("Oprettede ny borger-mappe i Google Drive. Name={Name}, Id={Id}", folder.Name, folder.Id);

            return folder.Id;
        }
    }
}
