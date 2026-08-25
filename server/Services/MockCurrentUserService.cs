using Microsoft.AspNetCore.Http;

namespace RRVMS.Api.Services;

public sealed class MockCurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private static readonly Dictionary<string, (string Name, string Email, string Role)> Users = new(StringComparer.OrdinalIgnoreCase)
    {
        ["prototype-host-requester"] = ("Alex Morgan", "alex.morgan@rolls-royce.com", "HOST_REQUESTER"),
        ["prototype-export-control"] = ("Priya Shah", "priya.shah@rolls-royce.com", "EXPORT_CONTROL"),
        ["prototype-reception"] = ("Michael Brown", "michael.brown@rolls-royce.com", "RECEPTION"),
    };

    private (string Id, string Name, string Email, string Role)? Current
    {
        get
        {
            var id = httpContextAccessor.HttpContext?.Request.Headers["X-RRVMS-Prototype-User"].FirstOrDefault();
            return id is not null && Users.TryGetValue(id, out var user) ? (id, user.Name, user.Email, user.Role) : null;
        }
    }

    public string UserId => Current?.Id ?? string.Empty;
    public string Name => Current?.Name ?? string.Empty;
    public string Email => Current?.Email ?? string.Empty;
    public string Role => Current?.Role ?? string.Empty;
    public bool IsAuthenticated => Current is not null;
}
