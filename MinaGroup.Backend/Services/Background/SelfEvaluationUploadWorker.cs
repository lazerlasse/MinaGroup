using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MinaGroup.Backend.Data;
using MinaGroup.Backend.Enums;
using MinaGroup.Backend.Helpers;
using MinaGroup.Backend.Models;
using MinaGroup.Backend.Services.Interfaces;

namespace MinaGroup.Backend.Services.Background
{
    public class SelfEvaluationUploadWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SelfEvaluationUploadWorker> _logger;

        // Tuning
        private const int BatchSize = 5;
        private static readonly TimeSpan PollDelay = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan JobStaleAfter = TimeSpan.FromMinutes(10);
        private static readonly int MaxAttempts = 8;

        public SelfEvaluationUploadWorker(IServiceScopeFactory scopeFactory, ILogger<SelfEvaluationUploadWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SelfEvaluationUploadWorker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    // 1) Gen-queue “stuck” jobs (crash mid-processing)
                    var staleCutoff = DateTime.UtcNow - JobStaleAfter;

                    var stale = await db.SelfEvaluationUploadQueueItems
                        .Where(x => (x.State == UploadJobState.Processing || x.State == UploadJobState.Retrying)
                                    && x.ProcessingStartedAtUtc != null
                                    && x.ProcessingStartedAtUtc < staleCutoff)
                        .ToListAsync(stoppingToken);

                    foreach (var s in stale)
                    {
                        s.State = UploadJobState.Retrying;
                        s.ProcessingStartedAtUtc = null;
                        s.NextAttemptAtUtc = DateTime.UtcNow;
                        s.LastMessage = "Job var låst/forældet og blev sat til retry.";
                    }

                    if (stale.Count > 0)
                        await db.SaveChangesAsync(stoppingToken);

                    // 2) Find due jobs
                    var now = DateTime.UtcNow;

                    var due = await db.SelfEvaluationUploadQueueItems
                        .Where(x =>
                            (x.State == UploadJobState.Queued || x.State == UploadJobState.Retrying) &&
                            x.NextAttemptAtUtc <= now &&
                            x.AttemptCount < MaxAttempts)
                        .OrderBy(x => x.NextAttemptAtUtc)
                        .Take(BatchSize)
                        .ToListAsync(stoppingToken);

                    if (due.Count == 0)
                    {
                        await Task.Delay(PollDelay, stoppingToken);
                        continue;
                    }

                    // 3) Process each job
                    foreach (var job in due)
                    {
                        if (stoppingToken.IsCancellationRequested) break;

                        using var jobScope = _scopeFactory.CreateScope();
                        var jobDb = jobScope.ServiceProvider.GetRequiredService<AppDbContext>();
                        var drive = jobScope.ServiceProvider.GetRequiredService<IGoogleDriveService>();
                        var pdfService = jobScope.ServiceProvider.GetRequiredService<SelfEvaluationPdfService>();
                        var logger = jobScope.ServiceProvider.GetRequiredService<ILogger<SelfEvaluationUploadWorker>>();

                        await ProcessOneAsync(jobDb, drive, pdfService, logger, job.Id, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Worker loop fejl.");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }

            _logger.LogInformation("SelfEvaluationUploadWorker stopped.");
        }

        private static async Task ProcessOneAsync(
            AppDbContext db,
            IGoogleDriveService driveService,
            SelfEvaluationPdfService pdfService,
            ILogger logger,
            int queueItemId,
            CancellationToken ct)
        {
            // Hent job (fresh from DB)
            var job = await db.SelfEvaluationUploadQueueItems.FirstOrDefaultAsync(x => x.Id == queueItemId, ct);
            if (job == null) return;

            // Mark processing
            job.State = UploadJobState.Processing;
            job.ProcessingStartedAtUtc = DateTime.UtcNow;
            job.LastMessage = "Behandler upload…";
            await db.SaveChangesAsync(ct);

            job.AttemptCount += 1;
            await db.SaveChangesAsync(ct);

            // Load evaluation (inkl. nødvendige includes til PDF)
            var evaluation = await db.SelfEvaluations
                .Include(se => se.User)
                .Include(se => se.ApprovedByUser)
                .Include(se => se.SelectedTask)
                .FirstOrDefaultAsync(se => se.Id == job.SelfEvaluationId, ct);

            if (evaluation?.User == null)
            {
                await WriteLogAndFailAsync(db, job, DriveUploadStatus.Failed, "SelfEvaluation/User mangler.", null, null, ct);
                return;
            }

            // Krav: kun upload når godkendt
            if (!evaluation.IsApproved)
            {
                await WriteLogAndFinishAsync(db, job, DriveUploadStatus.Skipped, "Springer over: evalueringen er ikke godkendt.", null, null, succeeded: true, ct: ct);
                return;
            }

            // Sikkerhed: org-scope
            if (evaluation.User.OrganizationId != job.OrganizationId)
            {
                await WriteLogAndFailAsync(db, job, DriveUploadStatus.Failed, "Org mismatch: job org != user org.", null, null, ct);
                return;
            }

            // Build filename
            var citizenName = evaluation.User.FullName;
            var fileName = $"{evaluation.EvaluationDate:dd.MM.yy}-{citizenName}.pdf";

            try
            {
                // Generate PDF (async wrapper)
                job.LastMessage = "Genererer PDF…";
                await db.SaveChangesAsync(ct);

                var pdfBytes = await SelfEvaluationPdfAsyncHelper.GeneratePdfAsync(pdfService, evaluation, ct);

                // Upload to Drive
                job.LastMessage = "Uploader til Google Drive…";
                await db.SaveChangesAsync(ct);

                await using var ms = new MemoryStream(pdfBytes);

                var result = await driveService.UploadPdfForOrganizationAsync(
                    organizationId: job.OrganizationId,
                    citizenName: citizenName,
                    fileName: fileName,
                    pdfStream: ms,
                    cancellationToken: ct);

                // Log attempt
                db.SelfEvaluationUploadLogs.Add(new SelfEvaluationUploadLog
                {
                    OrganizationId = job.OrganizationId,
                    SelfEvaluationId = job.SelfEvaluationId,
                    ProviderName = job.ProviderName,
                    Status = result.Status,
                    Message = result.Message,
                    DriveFileId = result.DriveFileId,
                    DriveFolderId = result.DriveFolderId,
                    AttemptNumber = job.AttemptCount,
                    CreatedAtUtc = DateTime.UtcNow
                });

                if (result.Status == DriveUploadStatus.Uploaded)
                {
                    job.State = UploadJobState.Succeeded;
                    job.LastMessage = result.Message;
                    job.LastDriveFileId = result.DriveFileId;
                    job.LastDriveFolderId = result.DriveFolderId;
                    job.ProcessingStartedAtUtc = null;

                    await db.SaveChangesAsync(ct);
                    return;
                }

                if (result.Status == DriveUploadStatus.Skipped)
                {
                    // Skipped = “det er okay” → vi afslutter som success (ingen retry)
                    job.State = UploadJobState.Succeeded;
                    job.LastMessage = result.Message;
                    job.ProcessingStartedAtUtc = null;
                    await db.SaveChangesAsync(ct);
                    return;
                }

                // Failed → retry eller endelig fail
                await HandleFailureAsync(db, job, result.Message, result.DriveFileId, result.DriveFolderId, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Upload job fejlede. QueueItemId={QueueItemId} EvalId={EvalId}", job.Id, job.SelfEvaluationId);

                db.SelfEvaluationUploadLogs.Add(new SelfEvaluationUploadLog
                {
                    OrganizationId = job.OrganizationId,
                    SelfEvaluationId = job.SelfEvaluationId,
                    ProviderName = job.ProviderName,
                    Status = DriveUploadStatus.Failed,
                    Message = $"Exception: {ex.Message}",
                    AttemptNumber = job.AttemptCount,
                    CreatedAtUtc = DateTime.UtcNow
                });

                await HandleFailureAsync(db, job, $"Exception: {ex.Message}", null, null, ct);
            }
        }

        private static async Task HandleFailureAsync(AppDbContext db, SelfEvaluationUploadQueueItem job, string message, string? fileId, string? folderId, CancellationToken ct)
        {
            job.LastMessage = message;
            job.LastDriveFileId = fileId;
            job.LastDriveFolderId = folderId;
            job.ProcessingStartedAtUtc = null;

            if (job.AttemptCount >= MaxAttempts)
            {
                job.State = UploadJobState.Failed;
                job.NextAttemptAtUtc = DateTime.UtcNow;
            }
            else
            {
                job.State = UploadJobState.Retrying;
                job.NextAttemptAtUtc = DateTime.UtcNow + ComputeBackoff(job.AttemptCount);
            }

            await db.SaveChangesAsync(ct);
        }

        private static TimeSpan ComputeBackoff(int attemptCount)
        {
            // 1: 10s, 2: 30s, 3: 1m, 4: 2m, 5: 5m, 6+: 10m
            return attemptCount switch
            {
                1 => TimeSpan.FromSeconds(10),
                2 => TimeSpan.FromSeconds(30),
                3 => TimeSpan.FromMinutes(1),
                4 => TimeSpan.FromMinutes(2),
                5 => TimeSpan.FromMinutes(5),
                _ => TimeSpan.FromMinutes(10)
            };
        }

        private static async Task WriteLogAndFailAsync(AppDbContext db, SelfEvaluationUploadQueueItem job, DriveUploadStatus status, string message, string? fileId, string? folderId, CancellationToken ct)
        {
            db.SelfEvaluationUploadLogs.Add(new SelfEvaluationUploadLog
            {
                OrganizationId = job.OrganizationId,
                SelfEvaluationId = job.SelfEvaluationId,
                ProviderName = job.ProviderName,
                Status = status,
                Message = message,
                DriveFileId = fileId,
                DriveFolderId = folderId,
                AttemptNumber = job.AttemptCount,
                CreatedAtUtc = DateTime.UtcNow
            });

            job.State = UploadJobState.Failed;
            job.LastMessage = message;
            job.ProcessingStartedAtUtc = null;

            await db.SaveChangesAsync(ct);
        }

        private static async Task WriteLogAndFinishAsync(AppDbContext db, SelfEvaluationUploadQueueItem job, DriveUploadStatus status, string message, string? fileId, string? folderId, bool succeeded, CancellationToken ct)
        {
            db.SelfEvaluationUploadLogs.Add(new SelfEvaluationUploadLog
            {
                OrganizationId = job.OrganizationId,
                SelfEvaluationId = job.SelfEvaluationId,
                ProviderName = job.ProviderName,
                Status = status,
                Message = message,
                DriveFileId = fileId,
                DriveFolderId = folderId,
                AttemptNumber = job.AttemptCount,
                CreatedAtUtc = DateTime.UtcNow
            });

            job.State = succeeded ? UploadJobState.Succeeded : UploadJobState.Failed;
            job.LastMessage = message;
            job.LastDriveFileId = fileId;
            job.LastDriveFolderId = folderId;
            job.ProcessingStartedAtUtc = null;

            await db.SaveChangesAsync(ct);
        }
    }
}