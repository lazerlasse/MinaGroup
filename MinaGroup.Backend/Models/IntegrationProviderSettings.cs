namespace MinaGroup.Backend.Models
{
    public class IntegrationProviderSettings
    {
        public int Id { get; set; }

        // fx "GoogleDrive", "OneDrive", "Dropbox"
        public string ProviderName { get; set; } = string.Empty;

        // Krypterede via ICryptoService
        public string EncryptedClientId { get; set; } = string.Empty;
        public string EncryptedClientSecret { get; set; } = string.Empty;

        // Evt. endpoints (du kan også hardcode dem, hvis du vil være simpel)
        public string? AuthorizationEndpoint { get; set; }
        public string? TokenEndpoint { get; set; }

        // fx "https://www.googleapis.com/auth/drive.file"
        public string? Scopes { get; set; }
    }
}
