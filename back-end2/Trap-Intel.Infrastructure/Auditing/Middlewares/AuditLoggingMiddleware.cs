using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Trap_Intel.Application.Abstractions.Auditing;
using Trap_Intel.Domain.Auditing;

namespace Trap_Intel.Infrastructure.Auditing.Middlewares;

public sealed class AuditLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditLoggingMiddleware> _logger;

    public AuditLoggingMiddleware(RequestDelegate next, ILogger<AuditLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IAuditService auditService)
    {
        var method = context.Request.Method;

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception intercepted. Recording to AuditTrail.");

            // Defensive error logging
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var orgClaim = context.User.FindFirst("org")
                    ?? context.User.FindFirst("organizationId");

                if (orgClaim != null && Guid.TryParse(orgClaim.Value, out var orgId))
                {
                    var incidentId = Guid.NewGuid();

                    try
                    {
                        await auditService.RecordAsync(
                            orgId,
                            AuditResourceType.Settings,
                            incidentId,
                            AuditAction.View,
                            AuditSeverity.Critical,
                            $"Unhandled exception during {method} {context.Request.Path}. IncidentId={incidentId}. Error={ex.Message}",
                            context.RequestAborted);
                    }
                    catch (Exception auditException)
                    {
                        _logger.LogWarning(
                            auditException,
                            "Failed to persist audit record for unhandled exception. IncidentId={IncidentId}",
                            incidentId);
                    }
                }
            }

            throw; // Re-throw for generic API exception mapper
        }
    }
}
