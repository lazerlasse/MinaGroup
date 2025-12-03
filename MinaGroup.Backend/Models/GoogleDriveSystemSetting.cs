using System.ComponentModel.DataAnnotations;

namespace MinaGroup.Backend.Models
{
    /// <summary>
    /// Globale Google Drive OAuth-indstillinger, styret af SysAdmin.
    /// Der forventes kun én række i tabellen.
    /// </summary>

    public class GoogleDriveSystemSetting
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(256)]
        [Display(Name = "Google OAuth Client ID")]
        public string ClientId { get; set; } = string.Empty;


        [MaxLength(4096)]
        [Display(Name = "Krypteret Client Secret")]
        public string? EncryptedClientSecret { get; set; } = string.Empty;

        [Required]
        [MaxLength(512)]
        [Display(Name = "Redirect URI")]
        public string RedirectUri { get; set; } = string.Empty;

        // Evt. plads til flere globale Drive-relaterede settings senere
        // fx: scopes, consent screen info, osv.
    }
}
