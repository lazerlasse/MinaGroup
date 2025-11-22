using MinaGroup.Backend.Models;

namespace MinaGroup.Backend.Services.Interfaces
{
    public interface IGoogleDriveService
    {
        Task UploadSelfEvaluationPdfAsync(SelfEvaluation evaluation, byte[] pdfBytes, CancellationToken ct = default);
    }
}
