using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace RRVMS.Api.IntegrationTests;

public sealed class RequestCreationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public RequestCreationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Development"))
            .CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    [Fact]
    public async Task HostCanCreateAndReadRequest()
    {
        var start = DateTimeOffset.UtcNow.AddDays(2).ToOffset(TimeSpan.FromMinutes(330));
        var payload = new
        {
            visitorType = "External",
            contractorType = "NormalVisitor",
            siteCode = "Bengaluru",
            purposeType = "Technical",
            purpose = $"Integration test {Guid.NewGuid():N}",
            areasToVisit = "Reception",
            mainHostObjectId = "00000000-0000-0000-0000-000000000001",
            mainHostName = "Development User",
            hostDepartment = "Engineering",
            escortingHostObjectId = "",
            escortingHostName = "",
            numberOfVisitors = 1,
            visitStart = start,
            visitEnd = start.AddHours(2)
        };

        var response = await _client.PostAsJsonAsync("/api/requests", payload, TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.True(response.StatusCode == HttpStatusCode.Created, $"{response.StatusCode}: {content}");

        using var result = JsonDocument.Parse(content);
        var id = result.RootElement.GetProperty("id").GetGuid();
        var persisted = await _client.GetAsync($"/api/requests/{id}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, persisted.StatusCode);
    }
}
