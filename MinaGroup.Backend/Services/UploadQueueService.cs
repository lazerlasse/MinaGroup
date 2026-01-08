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

        public async Task<int> EnqueueSelfEvaluationUploadAsync(
            int organizationId,
            int selfEvaluationId,
            string providerName = "GoogleDrive",
            CancellationToken ct = default)
        {
            var existing = await _db.SelfEvaluationUploadQueueItems
                .FirstOrDefaultAsync(x =>
                    x.OrganizationId == organizationId &&
                    x.SelfEvaluationId == selfEvaluationId &&
                    x.ProviderName == providerName, ct);

            if (existing != null)
            {
                if (existing.State == UploadJobState.Succeeded)
                    return existing.Id;

                existing.State = UploadJobState.Queued;
                existing.NextAttemptAtUtc = DateTime.UtcNow;
                existing.ProcessingStartedAtUtc = null;
                existing.LastMessage = "Job sat i kø igen.";
                await _db.SaveChangesAsync(ct);

                return existing.Id;
            }

            var item = new SelfEvaluationUploadQueueItem
            {
                OrganizationId = organizationId,
                SelfEvaluationId = selfEvaluationId,
                ProviderName = providerName,
                State = UploadJobState.Queued,
                NextAttemptAtUtc = DateTime.UtcNow,
                LastMessage = "Job oprettet."
            };

            _db.SelfEvaluationUploadQueueItems.Add(item);
            await _db.SaveChangesAsync(ct);

            return item.Id;
        }
    }
}
