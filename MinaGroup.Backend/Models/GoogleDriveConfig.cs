using System.ComponentModel.DataAnnotations;

public class GoogleDriveConfig
{
    public int Id { get; set; }

    [Display(Name = "Aktivér upload til Google Drive")]
    public bool IsEnabled { get; set; }

    [MaxLength(128)]
    [Display(Name = "Root folder ID i Google Drive")]
    public string RootFolderId { get; set; } = string.Empty;

    // Refresh-token er unikt pr. organisation
    [MaxLength(4096)]
    public string? EncryptedRefreshToken { get; set; }

    // Hvem har forbundet?
    [MaxLength(256)]
    public string? ConnectedAccountEmail { get; set; }
}