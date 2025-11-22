using System.ComponentModel.DataAnnotations;

namespace MinaGroup.Backend.Models
{
    public class GoogleDriveConfig
    {
        public int Id { get; set; }

        [Display(Name = "Aktivér upload til Google Drive")]
        public bool IsEnabled { get; set; }

        [MaxLength(128)]
        [Display(Name = "Root folder ID i Google Drive")]
        public string RootFolderId { get; set; } = string.Empty;

        // Alt nedenfor lagres krypteret i DB via ICryptoService
        [MaxLength(4096)]
        public string? EncryptedClientId { get; set; }

        [MaxLength(4096)]
        public string? EncryptedClientSecret { get; set; }

        [MaxLength(4096)]
        public string? EncryptedRefreshToken { get; set; }

        // Kun til visning i UI (ikke krypteret, ikke hemmeligt)
        [MaxLength(256)]
        public string? ConnectedAccountEmail { get; set; }
    }
}
