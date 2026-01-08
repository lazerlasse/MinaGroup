using MinaGroup.Backend.Models;

namespace MinaGroup.Backend.Services.Interfaces
{
    public interface IGoogleDriveService
    {
        Task<DriveUploadResult> UploadPdfForOrganizationAsync(
            int organizationId,
            string citizenName,
            string fileName,
            Stream pdfStream,
            CancellationToken cancellationToken = default);
    }
}
