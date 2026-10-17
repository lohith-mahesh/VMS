using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RRVMS.Application.Abstractions;
using RRVMS.Infrastructure.Persistence;

namespace RRVMS.Infrastructure.Background;

public sealed partial class OutboxWorker(IServiceScopeFactory scopeFactory, ILogger<OutboxWorker> logger) : BackgroundService
{
    [LoggerMessage(2001, LogLevel.Warning, "Notification {MessageId} failed.")]
    private static partial void LogNotificationFailure(ILogger logger, Guid messageId, Exception exception);

    [LoggerMessage(2002, LogLevel.Error, "The outbox worker failed.")]
    private static partial void LogWorkerFailure(ILogger logger, Exception exception);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        do
        {
            await ProcessAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ProcessAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<RrvmsDbContext>();
            var directory = scope.ServiceProvider.GetRequiredService<IEmployeeDirectory>();
            var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
            var messages = await dbContext.OutboxMessages
                .Where(message => message.ProcessedAt == null && message.AttemptCount < 10)
                .OrderBy(message => message.OccurredAt)
                .Take(20)
                .ToListAsync(cancellationToken);
            foreach (var message in messages)
            {
                try
                {
                    using var payload = JsonDocument.Parse(message.PayloadJson);
                    var root = payload.RootElement;
                    var requesterId = root.GetProperty("RequesterObjectId").GetString() ?? string.Empty;
                    var requester = await directory.FindByIdAsync(requesterId, cancellationToken);
                    if (requester is null || string.IsNullOrWhiteSpace(requester.Email))
                    {
                        throw new InvalidOperationException("The notification recipient could not be resolved.");
                    }

                    var requestNumber = HtmlEncoder.Default.Encode(root.GetProperty("RequestNumber").GetString() ?? string.Empty);
                    var status = HtmlEncoder.Default.Encode(root.GetProperty("Status").ToString());
                    await emailSender.SendAsync(
                        new EmailMessage(requester.Email, $"RRVMS {requestNumber}: {message.MessageType}", $"<p>Request <strong>{requestNumber}</strong> is now <strong>{status}</strong>.</p>"),
                        cancellationToken);
                    message.MarkProcessed(DateTimeOffset.UtcNow);
                }
                catch (Exception exception)
                {
                    message.MarkFailed(exception.Message[..Math.Min(exception.Message.Length, 4000)]);
                    LogNotificationFailure(logger, message.Id, exception);
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);
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
