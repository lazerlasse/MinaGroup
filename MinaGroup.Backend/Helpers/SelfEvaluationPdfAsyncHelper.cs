using MinaGroup.Backend.Models;
using MinaGroup.Backend.Services;

namespace MinaGroup.Backend.Helpers
{
    public static class SelfEvaluationPdfAsyncHelper
    {
        public static Task<byte[]> GeneratePdfAsync(SelfEvaluationPdfService pdfService, SelfEvaluation eval, CancellationToken ct)
        {
            // QuestPDF er sync → kør på threadpool, men respekter cancellation før/efter.
            ct.ThrowIfCancellationRequested();

            return Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();
                return pdfService.GeneratePdf(eval);
            }, ct);
        }
    }
}
