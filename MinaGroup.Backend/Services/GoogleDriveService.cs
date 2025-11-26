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
        private readonly ICryptoService _cryptoService;

        public GoogleDriveService(AppDbContext db, ICryptoService cryptoService)
        {
            _db = db;
            _cryptoService = cryptoService;
        }

        public async Task UploadPdfForOrganizationAsync(
            int organizationId,
            string citizenName,
            string fileName,
            Stream pdfStream,
            CancellationToken cancellationToken = default)
        {
            var provider = await _db.IntegrationProviderSettings
                .FirstOrDefaultAsync(p => p.ProviderName == "GoogleDrive", cancellationToken);

            var integration = await _db.OrganizationStorageIntegrations
                .FirstOrDefaultAsync(i => i.OrganizationId == organizationId &&
                                          i.ProviderName == "GoogleDrive",
                                      cancellationToken);

            if (provider == null || integration == null || !integration.IsConnected)
            {
                // log + evt. læg i en "retry queue" eller skriv notifikation
                return;
            }

            var clientId = _cryptoService.Unprotect(provider.EncryptedClientId);
            var clientSecret = _cryptoService.Unprotect(provider.EncryptedClientSecret);
            var refreshToken = integration.EncryptedRefreshToken != null
                ? _cryptoService.Unprotect(integration.EncryptedRefreshToken)
                : null;

            if (string.IsNullOrEmpty(refreshToken))
                return;

            // TODO: brug Google OAuth endpoints til at hente access_token fra refresh_token
            // TODO: upload filen til integration.RootFolderId / citizenName

            // Når du får nyt refresh_token / access_token, skal du opdatere integration og SaveChanges().
        }
    }
}
