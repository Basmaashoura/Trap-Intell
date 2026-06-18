using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Trap_Intel.Domain.Roles;

namespace Trap_Intel.Infrastructure.Authorization;

/// <summary>
/// Handles SameOrganizationRequirement by comparing the user's organization claim
/// against the organization ID in the request route or query.
/// Ensures multi-tenant data isolation.
/// </summary>
public sealed class SameOrganizationAuthorizationHandler : AuthorizationHandler<SameOrganizationRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<SameOrganizationAuthorizationHandler> _logger;

    public SameOrganizationAuthorizationHandler(
        IHttpContextAccessor httpContextAccessor,
        ILogger<SameOrganizationAuthorizationHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        SameOrganizationRequirement requirement)
    {
        var userOrgClaim = context.User.FindFirst("org")?.Value
            ?? context.User.FindFirst("organizationId")?.Value;

        if (string.IsNullOrEmpty(userOrgClaim))
        {
            _logger.LogDebug("Organization check failed: no org claim");
            return Task.CompletedTask;
        }

        // SuperAdmin can access any organization
        var roleClaim = GetRoleClaimValue(context.User);
        var isSuperAdmin = SystemRoles.IsSuperAdmin(roleClaim);

        if (isSuperAdmin)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Try to find organization ID from the request
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            _logger.LogDebug("Organization check: no HTTP context available");
            return Task.CompletedTask;
        }

        var requestOrgId = GetOrganizationIdFromRequest(httpContext);

        // If no org ID in request, allow (the service layer will filter by user's org)
        if (string.IsNullOrEmpty(requestOrgId))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (string.Equals(userOrgClaim, requestOrgId, StringComparison.OrdinalIgnoreCase))
        {
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning(
                "Organization mismatch: user org {UserOrg}, requested org {RequestOrg}",
                userOrgClaim, requestOrgId);
        }

        return Task.CompletedTask;
    }

    private static string? GetRoleClaimValue(ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Role)?.Value
            ?? user.FindFirst("role")?.Value;
    }

    private static string? GetOrganizationIdFromRequest(HttpContext httpContext)
    {
        // Check route values
        if (httpContext.Request.RouteValues.TryGetValue("organizationId", out var routeValue))
        {
            return routeValue?.ToString();
        }

        // Check query string
        if (httpContext.Request.Query.TryGetValue("organizationId", out var queryValue))
        {
            return queryValue.FirstOrDefault();
        }

        return null;
    }
}
