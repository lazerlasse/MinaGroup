using MinaGroup.Backend.Enums;
using System.ComponentModel.DataAnnotations;

namespace MinaGroup.Backend.Models
{
    public class SelfEvaluationUploadLog
    {
        [Key]
        public int Id { get; set; }

        public int OrganizationId { get; set; }

        public int SelfEvaluationId { get; set; }
        public SelfEvaluation SelfEvaluation { get; set; } = default!;

        public string ProviderName { get; set; } = "GoogleDrive";

        public DriveUploadStatus Status { get; set; }

        [MaxLength(1024)]
        public string Message { get; set; } = string.Empty;

        [MaxLength(256)]
        public string? DriveFileId { get; set; }

        [MaxLength(256)]
        public string? DriveFolderId { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public int AttemptNumber { get; set; } = 1;
    }
}
