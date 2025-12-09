using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace MinaGroup.Backend.Models
{
    public class TaskOption
    {
        [Key]
        public int TaskOptionId { get; set; }

        [Required, Display(Name = "Opgave/værksted")]
        public string TaskName { get; set; } = string.Empty;

        // 🔗 Hør til én organisation.
        public int OrganizationId { get; set; }

        [Display(Name = "Virksomhed"), ValidateNever]
        public Organization Organization { get; set; } = null!;
    }
}
