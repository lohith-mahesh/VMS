using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using RRVMS.Domain.Enums;

namespace RRVMS.Api.Auth;

public sealed class DevelopmentAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Development";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var roleValue = Request.Headers["X-Dev-Role"].FirstOrDefault() ?? UserRole.HostRequester.ToString();
        if (!Enum.TryParse<UserRole>(roleValue, true, out var role))
        {
            return Task.FromResult(AuthenticateResult.Fail("X-Dev-Role is invalid."));
        }

        var objectId = Request.Headers["X-Dev-User-Id"].FirstOrDefault() ?? "00000000-0000-0000-0000-000000000001";
        var displayName = Request.Headers["X-Dev-User"].FirstOrDefault() ?? "Development User";
        var email = Request.Headers["X-Dev-Email"].FirstOrDefault() ?? "developer@example.test";
        var claims = new[]
        {
            new Claim("oid", objectId),
            new Claim("name", displayName),
            new Claim("preferred_username", email),
            new Claim(ClaimTypes.Name, displayName),
            new Claim(ClaimTypes.Role, role.ToString())
        };
        var identity = new ClaimsIdentity(claims, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}
