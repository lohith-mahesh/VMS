using System.Net.Http.Headers;
using Azure.Core;

namespace RRVMS.Infrastructure.Graph;

public sealed class GraphAccessTokenHandler(TokenCredential credential) : DelegatingHandler
{
    private static readonly TokenRequestContext TokenRequest = new(["https://graph.microsoft.com/.default"]);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await credential.GetTokenAsync(TokenRequest, cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        return await base.SendAsync(request, cancellationToken);
    }
}
