using Microsoft.AspNetCore.Http;
using Trap_Intel.Application.Abstractions.Auditing;
using Trap_Intel.Application.Abstractions.RealTime;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Auditing;
using System.Security.Claims;

namespace Trap_Intel.Infrastructure.Auditing.Services;

internal sealed class AuditService : IAuditService
{
    private readonly IAuditTrailRepository _auditRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IListRealtimeNotifier _listRealtimeNotifier;
    private readonly IUnitOfWork _unitOfWork;

    public AuditService(
        IAuditTrailRepository auditRepository,
        IHttpContextAccessor httpContextAccessor,
        IListRealtimeNotifier listRealtimeNotifier,
        IUnitOfWork unitOfWork)
    {
        _auditRepository = auditRepository;
        _httpContextAccessor = httpContextAccessor;
        _listRealtimeNotifier = listRealtimeNotifier;
        _unitOfWork = unitOfWork;
    }

    public async Task RecordAsync(
        Guid organizationId,
        AuditResourceType resourceType,
        Guid resourceId,
        AuditAction action,
        AuditSeverity severity,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var (userId, ipAddress, userAgent) = GetContextInfo();

        var auditLogResult = AuditTrail.Create(
            organizationId,
            userId,
            resourceType,
            resourceId,
            action,
            severity,
            reason,
            ipAddress,
            userAgent
        );

        if (auditLogResult.IsSuccess)
        {
            await PersistAsync(auditLogResult.Value, cancellationToken);
        }
    }

    public async Task RecordLoginAsync(
        Guid userId, 
        Guid organizationId, 
        bool isSuccess, 
        string? failureReason = null, 
        CancellationToken cancellationToken = default)
    {
        var (_, ipAddress, userAgent) = GetContextInfo();

        var severity = isSuccess ? AuditSeverity.Info : AuditSeverity.Warning;

        // Using existing enumerations in Domain or treating Login as an 'Activate' action functionally 
        // if Domain doesn't have Login explicitly, otherwise we should use Create logic or adjust domain.
        var action = AuditAction.View; // Workaround assuming we just view the portal on login. Or update Enum in domain.
        var reason = isSuccess ? "User logged in successfully" : $"Login failed: {failureReason}";

        var auditLogResult = AuditTrail.Create(
            organizationId,
            userId,
            AuditResourceType.User,
            userId,
            action,
            severity,
            reason,
            ipAddress,
            userAgent
        );

        if (auditLogResult.IsSuccess)
        {
            await PersistAsync(auditLogResult.Value, cancellationToken);
        }
    }

    public async Task RecordChangesAsync(
        Guid organizationId,
        AuditResourceType resourceType,
        Guid resourceId,
        AuditAction action,
        AuditSeverity severity,
        List<(string PropertyName, string? OldValue, string? NewValue)> changes,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var (userId, ipAddress, userAgent) = GetContextInfo();

        var auditLogResult = AuditTrail.Create(
            organizationId,
            userId,
            resourceType,
            resourceId,
            action,
            severity,
            reason,
            ipAddress,
            userAgent
        );

        if (auditLogResult.IsSuccess)
        {
            var auditTrail = auditLogResult.Value;

            foreach (var (propName, oldVal, newVal) in changes)
            {
                auditTrail.AddChange(propName, oldVal, newVal);
            }

            await PersistAsync(auditTrail, cancellationToken);
        }
    }

    private async Task PersistAsync(AuditTrail auditTrail, CancellationToken cancellationToken)
    {
        await _auditRepository.AddAsync(auditTrail);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var payload = new { auditTrailId = auditTrail.Id, severity = auditTrail.Severity.ToString() };
        await _listRealtimeNotifier.NotifyOrganizationListChangedAsync(
            "auditlogs",
            auditTrail.OrganizationId,
            action: "created",
            payload: payload,
            cancellationToken: cancellationToken);
    }

    private (Guid? UserId, string? IpAddress, string? UserAgent) GetContextInfo()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null) return (null, null, null);

        var ipAddress = context.Connection.RemoteIpAddress?.ToString();
        var userAgent = context.Request.Headers["User-Agent"].ToString();

        var userIdString = context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid? userId = Guid.TryParse(userIdString, out var parsedId) ? parsedId : null;

        return (userId, ipAddress, userAgent);
    }
}
