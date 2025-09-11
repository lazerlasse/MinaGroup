using System.ComponentModel.DataAnnotations;

namespace MinaGroup.Backend.Models
{
    public class TaskOption
    {
        [Key]
        public int TaskOptionId { get; set; }

        [Required, Display(Name = "Opgave/værksted")]
        public string TaskName { get; set; } = string.Empty;
    }
}
