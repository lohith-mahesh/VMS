using System.Security.Claims;
using RRVMS.Application.Abstractions;
using RRVMS.Application.Common;
using RRVMS.Domain.Enums;

namespace RRVMS.Api.Auth;

public sealed class HttpCurrentActor(IHttpContextAccessor accessor) : ICurrentActor
{
    private ClaimsPrincipal User => accessor.HttpContext?.User ?? new ClaimsPrincipal();

    public string ObjectId => User.FindFirstValue("oid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    public string DisplayName => User.FindFirstValue("name") ?? User.Identity?.Name ?? string.Empty;
    public string Email => User.FindFirstValue("preferred_username") ?? User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
    public UserRole Role
    {
        get
        {
            var roles = User.FindAll(ClaimTypes.Role)
                .Concat(User.FindAll("roles"))
                .Select(claim => claim.Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(value => Enum.TryParse<UserRole>(value, true, out var role) ? role : (UserRole?)null)
                .Where(role => role is not null)
                .Select(role => role!.Value)
                .Distinct()
                .ToArray();
            return roles.Length == 1
                ? roles[0]
                : throw new ForbiddenException("Exactly one RRVMS application role must be assigned to the signed-in user.");
        }
    }

    public string CorrelationId => accessor.HttpContext?.TraceIdentifier ?? Guid.NewGuid().ToString("N");
    public bool IsAuthenticated => User.Identity?.IsAuthenticated == true;
}
