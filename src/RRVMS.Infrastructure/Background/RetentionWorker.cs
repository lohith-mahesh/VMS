using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RRVMS.Application.Abstractions;
using RRVMS.Infrastructure.Persistence;

namespace RRVMS.Infrastructure.Background;

public sealed partial class RetentionWorker(IServiceScopeFactory scopeFactory, ILogger<RetentionWorker> logger) : BackgroundService
{
    [LoggerMessage(1001, LogLevel.Error, "The retention worker failed.")]
    private static partial void LogWorkerFailure(ILogger logger, Exception exception);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ProcessAsync(stoppingToken);
        using var timer = new PeriodicTimer(TimeSpan.FromHours(24));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ProcessAsync(stoppingToken);
        }
    }

    private async Task ProcessAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IVisitorRequestRepository>();
            var storage = scope.ServiceProvider.GetRequiredService<IDocumentStorage>();
            var clock = scope.ServiceProvider.GetRequiredService<IClock>();
            var expired = await repository.ExpiredAsync(clock.UtcNow, 100, cancellationToken);
            foreach (var request in expired)
            {
                foreach (var document in request.Visitors.SelectMany(visitor => visitor.DpsDocuments))
                {
                    await storage.DeleteAsync(document.BlobName, cancellationToken);
                }

                repository.Remove(request);
            }

            if (expired.Count > 0)
            {
                await repository.SaveChangesAsync(cancellationToken);
            }

            var dbContext = scope.ServiceProvider.GetRequiredService<RrvmsDbContext>();
            var auditCutoff = clock.UtcNow.AddYears(-6);
            await dbContext.ReportExportEvents.Where(item => item.ExportedAt <= auditCutoff).ExecuteDeleteAsync(cancellationToken);
            await dbContext.OutboxMessages.Where(item => item.ProcessedAt != null && item.ProcessedAt <= auditCutoff).ExecuteDeleteAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            LogWorkerFailure(logger, exception);
        }
    }
}
