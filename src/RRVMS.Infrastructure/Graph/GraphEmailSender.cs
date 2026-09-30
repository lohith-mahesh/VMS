using System.Net.Http.Json;
using RRVMS.Application.Abstractions;
using RRVMS.Infrastructure.Options;

namespace RRVMS.Infrastructure.Graph;

public sealed class GraphEmailSender(HttpClient httpClient, GraphOptions options) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        if (!options.Enabled || string.IsNullOrWhiteSpace(options.SenderMailbox))
        {
            throw new InvalidOperationException("Microsoft Graph email is not configured.");
        }

        var payload = new
        {
            message = new
            {
                subject = message.Subject,
                body = new { contentType = "HTML", content = message.HtmlBody },
                toRecipients = new[] { new { emailAddress = new { address = message.To } } }
            },
            saveToSentItems = true
        };
        var endpoint = $"{options.BaseUrl.TrimEnd('/')}/users/{Uri.EscapeDataString(options.SenderMailbox)}/sendMail";
        using var response = await httpClient.PostAsJsonAsync(endpoint, payload, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
