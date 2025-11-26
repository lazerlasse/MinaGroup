namespace MinaGroup.Backend.Models
{
    public class OrganizationStorageIntegration
    {
        public int Id { get; set; }

        public int OrganizationId { get; set; }
        public Organization Organization { get; set; } = default!;

        // "GoogleDrive" i første omgang
        public string ProviderName { get; set; } = "GoogleDrive";

        // Root folder i den pågældende organisations Drive
        public string? RootFolderId { get; set; }

        // Tokens – ALDRIG i klartekst i DB, altid krypteret
        public string? EncryptedRefreshToken { get; set; }
        public string? EncryptedAccessToken { get; set; }
        public DateTime? AccessTokenExpiresAt { get; set; }

        public bool IsConnected { get; set; }
        public DateTime? ConnectedAt { get; set; }

        // Evt. til debugging/log:
        public string? ConnectedAccountEmail { get; set; }
    }
}
