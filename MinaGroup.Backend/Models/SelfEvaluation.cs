using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MinaGroup.Backend.Models
{
    public class SelfEvaluation
    {
        // Evaluation id.
        [Key]
        public int Id { get; set; }
        

        // Borger som evalueringen tilhører.
        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey(nameof(UserId)), Display(Name = "Borger"), ValidateNever]
        public AppUser User { get; set; }


        // Hvem har godkendt evalueringen.
        public string? ApprovedByUserId { get; set; }

        [ForeignKey(nameof(ApprovedByUserId)), Display(Name = "Godkendt af"), ValidateNever]
        public AppUser? ApprovedByUser { get; set; }


        // Sygemeldt.
        [Required, Display(Name = "Sygemeldt")]
        public bool IsSick { get; set; } = false;

        [Display(Name = "Sygemeldings årsag")]
        public string? SickReason { get; set; } = string.Empty;


        // Udeblevet.
        [Required, Display(Name = "Udeblevet")]
        public bool IsNoShow { get; set; } = false;

        [DisplayName("Udeblivelses årsag")]
        public string? NoShowReason { get; set; } = string.Empty;


        // Fri og lovligt fravær
        [Display(Name = "Ferie og lovligt fravær")]
        public bool IsOffWork { get; set; } = false;

        [Display(Name = "Fraværs årsag")]
        public string? OffWorkReason { get; set; } = string.Empty;


        // Mødetid og arbejdstid.
        [DisplayName("Tid mødt")]
        public TimeSpan? ArrivalTime { get; set; }

        [DisplayName("Tid gået hjem")]
        public TimeSpan? DepartureTime { get; set; }

        [DisplayName("Arbejdstimer i alt")]
        public TimeSpan? TotalHours { get; set; }

        [DisplayName("Kom borgeren til tiden?")]
        public string? ArrivalStatus { get; set; }


        // Afholdt pause.
        [DisplayName("Har haft pause")]
        public bool HadBreak { get; set; } = false;

        [Display(Name = "Pause tid")]
        [DisplayFormat(DataFormatString = "{0:hh\\:mm}", ApplyFormatInEditMode = true)]
        public TimeSpan? BreakDuration { get; set; } = TimeSpan.Zero;

        
        // Aktiviteter, samarbejde mm.
        [DisplayName("Dagens aktiviteter")]
        public ICollection<TaskOption> SelectedTask { get; set; } = [];

        [DisplayName("Samarbejde")]
        public string? Collaboration { get; set; }


        // Hjælp og assistance.
        [DisplayName("Hjælp")]
        public string? Assistance { get; set; }

        [DisplayName("Hjælpemidler")]
        public string? Aid { get; set; }

        [DisplayName("Hvilke hjælpemidler")]
        public string? AidDescription { get; set; }


        // Smerter, skader mm.
        [DisplayName("Træthed/smerter mv.")]
        public bool HadDiscomfort { get; set; } = false;

        [DisplayName("Beskrivelse af ubehag")]
        public string? DiscomfortDescription { get; set; }


        // Kommentarer og bemærkninger.
        [DisplayName("Kommentar fra borger")]
        public string? CommentFromUser { get; set; }

        [DisplayName("Kommentar fra leder")]
        public string? CommentFromLeader { get; set; }
        

        // Aftaler.
        [DisplayName("Aftaler næste møde")]
        public string? NextMeetingNotes { get; set; }


        // Dato og tidsstempler.
        [Required, DisplayName("Evalueringsdato")]
        public DateTime EvaluationDate { get; set; } = DateTime.UtcNow;

        [Required, DisplayName("Seneste opdateret")]
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;


        // Marker at leder har godkendt/afsluttet evalueringsskemaet
        [Display(Name = "Godkendt af leder")]
        public bool IsApproved { get; set; } = false;
    }
}
