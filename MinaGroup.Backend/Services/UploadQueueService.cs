using Microsoft.EntityFrameworkCore;
using MinaGroup.Backend.Data;
using MinaGroup.Backend.Enums;
using MinaGroup.Backend.Models;

namespace MinaGroup.Backend.Services
{
    public class UploadQueueService
    {
        private readonly AppDbContext _db;

        public UploadQueueService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<int> EnqueueOrRequeueSelfEvaluationUploadAsync(
            int organizationId,
            int selfEvaluationId,
            string providerName,
            string? reason,
            CancellationToken ct)
        {
            var existing = await _db.SelfEvaluationUploadQueueItems
                .FirstOrDefaultAsync(x =>
                    x.OrganizationId == organizationId &&
                    x.SelfEvaluationId == selfEvaluationId &&
                    x.ProviderName == providerName, ct);

            if (existing != null)
            {
                // Requeue: reset state så worker tager den igen
                existing.State = UploadJobState.Queued;
                existing.NextAttemptAtUtc = DateTime.UtcNow;
                existing.ProcessingStartedAtUtc = null;

                // VIGTIGT: nulstil attempts ellers kan den være stuck på MaxAttempts
                existing.AttemptCount = 0;

                // Optional: behold LastDriveFileId så vi kan vælge Update-by-Id senere
                existing.LastMessage = string.IsNullOrWhiteSpace(reason)
                    ? "Requeued"
                    : reason;

                await _db.SaveChangesAsync(ct);
                return existing.Id;
            }

            var job = new SelfEvaluationUploadQueueItem
            {
                OrganizationId = organizationId,
                SelfEvaluationId = selfEvaluationId,
                ProviderName = providerName,
                State = UploadJobState.Queued,
                NextAttemptAtUtc = DateTime.UtcNow,
                AttemptCount = 0,
                LastMessage = reason ?? "Queued"
            };

            _db.SelfEvaluationUploadQueueItems.Add(job);
            await _db.SaveChangesAsync(ct);
            return job.Id;
        }
    }
}
