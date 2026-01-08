using MinaGroup.Backend.Enums;

namespace MinaGroup.Backend.Models
{
    public class DriveUploadResult
    {
        public DriveUploadStatus Status { get; private set; }
        public string Message { get; private set; } = string.Empty;
        public string? DriveFileId { get; private set; }
        public string? DriveFolderId { get; private set; }

        private DriveUploadResult() { }

        public static DriveUploadResult Skipped(string message) => new()
        {
            Status = DriveUploadStatus.Skipped,
            Message = message
        };

        public static DriveUploadResult Failed(string message) => new()
        {
            Status = DriveUploadStatus.Failed,
            Message = message
        };

        public static DriveUploadResult Uploaded(string message, string? fileId, string? folderId) => new()
        {
            Status = DriveUploadStatus.Uploaded,
            Message = message,
            DriveFileId = fileId,
            DriveFolderId = folderId
        };
    }
}
