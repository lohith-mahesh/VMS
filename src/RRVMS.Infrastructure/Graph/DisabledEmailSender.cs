using RRVMS.Application.Abstractions;

namespace RRVMS.Infrastructure.Graph;

public sealed class DisabledEmailSender : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken) => Task.CompletedTask;
}
