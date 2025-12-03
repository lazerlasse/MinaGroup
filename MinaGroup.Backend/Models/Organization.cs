using System.ComponentModel.DataAnnotations;

namespace MinaGroup.Backend.Models
{
    public class Organization
    {
        [Key]
        public int Id { get; set; }

        [Display(Name = "CVR Nr.")]
        public int CVRNumber { get; set; }

        [Required, Display(Name = "Virksomheds navn")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Adresse")]
        public string OrganizationAdress { get; set; } = string.Empty;

        [Display(Name = "Postnummer")]
        public int PostalCode { get; set; } = 0000;

        [Display(Name = "By")]
        public string Town { get; set; } = string.Empty;

        public string? Slug { get; set; }

        public ICollection<AppUser> Users { get; set; } = [];
        public ICollection<OrganizationStorageIntegration> StorageIntegrations { get; set; } = [];
    }
}