using System.Net.Http.Json;
using System.Text.Json.Serialization;
using RRVMS.Application.Abstractions;
using RRVMS.Infrastructure.Options;

namespace RRVMS.Infrastructure.Graph;

public sealed class GraphEmployeeDirectory(HttpClient httpClient, GraphOptions options) : IEmployeeDirectory
{
    public async Task<IReadOnlyList<EmployeeDirectoryResult>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        var trimmed = query.Trim();
        if (trimmed.Length < 2)
        {
            return [];
        }

        var escapedFilter = trimmed.Replace("'", "''", StringComparison.Ordinal);
        var relative = $"{options.BaseUrl.TrimEnd('/')}/users?$select=id,displayName,mail,userPrincipalName,department&$top=10&$filter=accountEnabled eq true and startswith(displayName,'{Uri.EscapeDataString(escapedFilter)}')";
        using var response = await httpClient.GetAsync(relative, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GraphUserResponse>(cancellationToken: cancellationToken);
        return result?.Value.Select(user => new EmployeeDirectoryResult(
            user.Id,
            user.DisplayName,
            string.IsNullOrWhiteSpace(user.Mail) ? user.UserPrincipalName : user.Mail,
            user.Department ?? string.Empty)).ToArray() ?? [];
    }

    public async Task<EmployeeDirectoryResult?> FindByIdAsync(string objectId, CancellationToken cancellationToken)
    {
        var endpoint = $"{options.BaseUrl.TrimEnd('/')}/users/{Uri.EscapeDataString(objectId)}?$select=id,displayName,mail,userPrincipalName,department";
        using var response = await httpClient.GetAsync(endpoint, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        var user = await response.Content.ReadFromJsonAsync<GraphUser>(cancellationToken: cancellationToken);
        return user is null
            ? null
            : new EmployeeDirectoryResult(user.Id, user.DisplayName, string.IsNullOrWhiteSpace(user.Mail) ? user.UserPrincipalName : user.Mail, user.Department ?? string.Empty);
    }

    private sealed record GraphUserResponse([property: JsonPropertyName("value")] IReadOnlyList<GraphUser> Value);
    private sealed record GraphUser(string Id, string DisplayName, string? Mail, string UserPrincipalName, string? Department);
}
