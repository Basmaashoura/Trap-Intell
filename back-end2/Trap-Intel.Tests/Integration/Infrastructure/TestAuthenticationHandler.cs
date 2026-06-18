using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Trap_Intel.Tests.Integration.Infrastructure;

internal sealed class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "TestScheme";

    public const string AuthHeader = "X-Test-Auth";
    public const string UserIdHeader = "X-Test-UserId";
    public const string OrganizationIdHeader = "X-Test-OrgId";
    public const string RoleHeader = "X-Test-Role";
    public const string PermissionsHeader = "X-Test-Permissions";

    public TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(AuthHeader, out var authValue) ||
            !string.Equals(authValue.ToString(), "true", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing test authentication header."));
        }

        var userId = Request.Headers.TryGetValue(UserIdHeader, out var userIdHeader)
            ? userIdHeader.ToString()
            : Guid.NewGuid().ToString();

        var organizationId = Request.Headers.TryGetValue(OrganizationIdHeader, out var organizationIdHeader)
            ? organizationIdHeader.ToString()
            : Guid.Empty.ToString();

        var role = Request.Headers.TryGetValue(RoleHeader, out var roleHeader)
            ? roleHeader.ToString()
            : "OrganizationAdmin";

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new("sub", userId),
            new("org", organizationId),
            new(ClaimTypes.Role, role)
        };

        if (Request.Headers.TryGetValue(PermissionsHeader, out var permissionsHeader))
        {
            var permissions = permissionsHeader
                .ToString()
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var permission in permissions)
            {
                claims.Add(new Claim("permission", permission));
            }
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
