using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace MinaGroup.Backend.Models
{
    public class AppUser : IdentityUser
    {
        [Display(Name = "Fornavn")]
        public string FirstName { get; set; } = string.Empty;

        [Display(Name = "Efternavn")]
        public string LastName { get; set; } = string.Empty;

        [Display(Name = "CPR Nummer")]
        [MaxLength(8), MinLength(8)]
        public int? PersonNumberCPR { get; set; }
    }
}
