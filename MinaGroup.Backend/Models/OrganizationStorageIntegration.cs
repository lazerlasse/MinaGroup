namespace MinaGroup.Backend.Models
{
    public class OrganizationStorageIntegration
    {
        public int Id { get; set; }
        public int OrganizationId { get; set; }
        public Organization Organization { get; set; } = default!;
        public string ProviderName { get; set; } = "GoogleDrive";

        public string? RootFolderId { get; set; }

        public string? EncryptedRefreshToken { get; set; }
        public string? EncryptedAccessToken { get; set; }
        public DateTime? AccessTokenExpiresAt { get; set; }

        public bool IsConnected { get; set; }
        public DateTime? ConnectedAt { get; set; }
        public string? ConnectedAccountEmail { get; set; }

        // ✅ NYT: brugerens toggle i admin UI
        public bool IsEnabled { get; set; } = true;
    }
}
