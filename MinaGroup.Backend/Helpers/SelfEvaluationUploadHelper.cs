using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MinaGroup.Backend.Data;
using MinaGroup.Backend.Enums;
using MinaGroup.Backend.Models;
using MinaGroup.Backend.Services;
using MinaGroup.Backend.Services.Interfaces;

namespace MinaGroup.Backend.Helpers
{
    public static class SelfEvaluationUploadHelper
    {
        public static async Task<DriveUploadResult> TryUploadApprovedSelfEvaluationPdfAsync(
            AppDbContext db,
            SelfEvaluationPdfService pdfService,
            IGoogleDriveService googleDriveService,
            ILogger logger,
            int organizationId,
            int selfEvaluationId,
            CancellationToken ct)
        {
            var eval = await db.SelfEvaluations
                .Include(e => e.User)
                .Include(e => e.SelectedTask)
                .FirstOrDefaultAsync(e => e.Id == selfEvaluationId, ct);

            if (eval == null)
                return DriveUploadResult.Skipped("Evalueringen kunne ikke findes.");

            if (!eval.IsApproved)
                return DriveUploadResult.Skipped("Evalueringen er ikke godkendt endnu.");

            if (eval.User == null)
                return DriveUploadResult.Failed("Borger-data (User) mangler på evalueringen.");

            if (eval.User.OrganizationId != organizationId)
                return DriveUploadResult.Failed("Sikkerhedsblok: Evalueringen tilhører ikke din organisation.");

            var citizenName = eval.User.FullName;
            var fileName = $"Selvevaluering_{citizenName}_{eval.EvaluationDate:yyyy-MM-dd}.pdf";

            var attempt = await db.SelfEvaluationUploadLogs
                .Where(x => x.SelfEvaluationId == selfEvaluationId && x.ProviderName == "GoogleDrive")
                .CountAsync(ct) + 1;

            try
            {
                // Din PDF-service er sync → vi genererer bytes og uploader async
                var pdfBytes = pdfService.GeneratePdf(eval);
                await using var stream = new MemoryStream(pdfBytes);

                var result = await googleDriveService.UploadPdfForOrganizationAsync(
                    organizationId: organizationId,
                    citizenName: citizenName,
                    fileName: fileName,
                    pdfStream: stream,
                    cancellationToken: ct);

                db.SelfEvaluationUploadLogs.Add(new SelfEvaluationUploadLog
                {
                    OrganizationId = organizationId,
                    SelfEvaluationId = selfEvaluationId,
                    ProviderName = "GoogleDrive",
                    Status = result.Status,
                    Message = result.Message,
                    DriveFileId = result.DriveFileId,
                    DriveFolderId = result.DriveFolderId,
                    AttemptNumber = attempt,
                    CreatedAtUtc = DateTime.UtcNow
                });

                await db.SaveChangesAsync(ct);

                logger.LogInformation(
                    "SelfEval upload attempt {Attempt} done. SelfEvalId={Id} Status={Status} OrgId={OrgId}",
                    attempt, selfEvaluationId, result.Status, organizationId);

                return result;
            }
            catch (Exception ex)
            {
                var msg = $"PDF-upload flow fejlede: {ex.Message}";
                logger.LogError(ex, "SelfEval upload flow exception. SelfEvalId={Id} OrgId={OrgId}",
                    selfEvaluationId, organizationId);

                db.SelfEvaluationUploadLogs.Add(new SelfEvaluationUploadLog
                {
                    OrganizationId = organizationId,
                    SelfEvaluationId = selfEvaluationId,
                    ProviderName = "GoogleDrive",
                    Status = DriveUploadStatus.Failed,
                    Message = msg,
                    AttemptNumber = attempt,
                    CreatedAtUtc = DateTime.UtcNow
                });

                await db.SaveChangesAsync(ct);

                return DriveUploadResult.Failed(msg);
            }
        }
    }
}