using System.ComponentModel.DataAnnotations;

namespace MinaGroup.Backend.Models
{
    public class Organization
    {
        [Key]
        public int Id { get; set; }

        // Firma cvr nummer.
        [Display(Name = "CVR Nr."), MinLength(8), MaxLength(8)]
        public int CVRNumber { get; set; }

        // Firmanavn – kan senere udbygges med CVR, adresse osv.
        [Required, Display(Name = "Virksomheds navn")]
        public string Name { get; set; } = string.Empty;

        // Adresse oplysninger.
        [Display(Name = "Adresse")]
        public string? OrganizationAdress { get; set; } = string.Empty;

        // Postnummer.
        [Display(Name = "Postnummer"), MinLength(4), MaxLength(4)]
        public int PostalCode { get; set; } = 0000;

        // By.
        [Display(Name = "By")]
        public string? Town { get; set; } = string.Empty;


        // Evt. til senere multi-domain / white-label
        public string? Slug { get; set; }

        // Navigation
        public ICollection<AppUser> Users { get; set; } = new List<AppUser>();
        public ICollection<OrganizationStorageIntegration> StorageIntegrations { get; set; }
            = [];
    }
}