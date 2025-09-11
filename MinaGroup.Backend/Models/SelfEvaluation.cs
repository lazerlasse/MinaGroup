using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MinaGroup.Backend.Models
{
    public class SelfEvaluation
    {
        [Key]
        public int Id { get; set; }

        
        [Required]
        public string UserId { get; set; }

        [ForeignKey(nameof(UserId)), Display(Name = "Bruger"), ValidateNever]
        public AppUser User { get; set; }


        [Required, Display(Name = "Sygemeldt")]
        public bool IsSick { get; set; }
        
        
        [DisplayName("Tid mødt")]
        public TimeSpan? ArrivalTime { get; set; }

        [DisplayName("Tid gået hjem")]
        public TimeSpan? DepartureTime { get; set; }

        [DisplayName("Arbejdstimer i alt")]
        public TimeSpan? TotalHours { get; set; }


        [DisplayName("Har haft pause")]
        public bool HadBreak { get; set; } = false;

        [Display(Name = "Pause tid")]
        public TimeSpan? BreakDuration { get; set; }

        
        [DisplayName("Kom borgeren til tiden?")]
        public string? ArrivalStatus { get; set; }

        
        [DisplayName("Dagens aktiviteter")]
        public ICollection<TaskOption> SelectedTask { get; set; } = [];

        [DisplayName("Samarbejde")]
        public string? Collaboration { get; set; }

        [DisplayName("Hjælp")]
        public string? Assistance { get; set; }

        [DisplayName("Hjælpemidler")]
        public string? Aid { get; set; }

        [DisplayName("Hvilke hjælpemidler")]
        public string? AidDescription { get; set; }

        [DisplayName("Træthed/smerter mv.")]
        public bool HadDiscomfort { get; set; } = false;

        [DisplayName("Beskrivelse af ubehag")]
        public string? DiscomfortDescription { get; set; }

        [DisplayName("Kommentar fra borger")]
        public string? CommentFromUser { get; set; }

        [DisplayName("Aftaler næste møde")]
        public string? NextMeetingNotes { get; set; }

        [DisplayName("Kommentar fra leder")]
        public string? CommentFromLeader { get; set; }

        [Required, DisplayName("Evalueringsdato")]
        public DateTime EvaluationDate { get; set; } = DateTime.UtcNow;

        [Required, DisplayName("Seneste opdateret")]
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
