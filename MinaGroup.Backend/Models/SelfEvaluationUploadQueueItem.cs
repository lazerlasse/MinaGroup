using MinaGroup.Backend.Enums;
using System.ComponentModel.DataAnnotations;

namespace MinaGroup.Backend.Models
{
    public class SelfEvaluationUploadQueueItem
    {
        [Key]
        public int Id { get; set; }

        public int OrganizationId { get; set; }

        public int SelfEvaluationId { get; set; }

        [MaxLength(64)]
        public string ProviderName { get; set; } = "GoogleDrive";

        public UploadJobState State { get; set; } = UploadJobState.Queued;

        public int AttemptCount { get; set; } = 0;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime NextAttemptAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? ProcessingStartedAtUtc { get; set; }

        [MaxLength(1024)]
        public string? LastMessage { get; set; }

        [MaxLength(256)]
        public string? LastDriveFileId { get; set; }

        [MaxLength(256)]
        public string? LastDriveFolderId { get; set; }
    }
}
