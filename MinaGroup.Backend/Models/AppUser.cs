using Microsoft.AspNetCore.Identity;
using MinaGroup.Backend.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MinaGroup.Backend.Models
{
    public class AppUser : IdentityUser
    {
        [Display(Name = "Fornavn"), PersonalData]
        public string FirstName { get; set; } = string.Empty;

        [Display(Name = "Efternavn"), PersonalData]
        public string LastName { get; set; } = string.Empty;

        // DB-felter
        [MaxLength(512)]
        public string? EncryptedPersonNumber { get; set; }    // gem krypteret CPR
        [MaxLength(512)]
        public string? PersonNumberHash { get; set; }        // HMAC hash til lookup

        // Ikke-mapped property for nem brug i kode (valgfrit)
        [NotMapped]
        [StringLength(11)]
        [RegularExpression(@"^\d{6}-\d{4}$", ErrorMessage = "CPR-nummer skal være 10 cifre i formatet xxxxxx-xxxx")]
        public string? PersonNumberCPR { get; set; } = null;

        [Display(Name = "Start dato"), PersonalData]
        public DateTime? JobStartDate { get; set; }

        [Display(Name = "Slut dato"), PersonalData]
        public DateTime? JobEndDate { get; set; }


        // Refresh Token properties
        public string? RefreshToken { get; set; }
        public DateTime RefreshTokenExpiryTime { get; set; }

        public void SetRefreshToken(string token, DateTime expires)
        {
            RefreshToken = token;
            RefreshTokenExpiryTime = expires;
        }

        public string FullName => $"{FirstName} {LastName}";

        // Hvilke dage borgeren forventes at møde (nullable => kan lade være tomt)
        [Display(Name = "Ungetlige Arbejdsdage")]
        public WeekDays? ScheduledDays { get; set; }

        public ICollection<SelfEvaluation> SelfEvaluations { get; set; } = [];
    }
}
