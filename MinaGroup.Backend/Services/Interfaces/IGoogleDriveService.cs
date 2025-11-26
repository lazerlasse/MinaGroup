using MinaGroup.Backend.Models;

namespace MinaGroup.Backend.Services.Interfaces
{
    public interface IGoogleDriveService
    {
        Task UploadPdfForOrganizationAsync(int organizationId, string citizenName, string fileName, Stream pdfStream, CancellationToken cancellationToken = default);
    }
}
