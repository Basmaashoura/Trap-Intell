using System.Text.Json;
using Application.Abstractions.Audit;
using Application.Abstractions.Data;
using Application.Abstractions.Identity;
using Domain.AuditLogs;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Audit;

/// <summary>
/// Implementation of IAuditService that records audit logs to the database.
/// </summary>
internal sealed class AuditService : IAuditService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AuditService(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _currentUser = currentUser;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(
        AuditAction action,
        string entityType,
        string? entityId,
        string? entityName = null,
        object? oldValues = null,
        object? newValues = null,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        string? oldValuesJson = oldValues is not null
            ? JsonSerializer.Serialize(oldValues, JsonOptions)
            : null;

        string? newValuesJson = newValues is not null
            ? JsonSerializer.Serialize(newValues, JsonOptions)
            : null;

        var auditLog = AuditLog.Create(
            userId: _currentUser.UserId != Guid.Empty ? _currentUser.UserId : null,
            userEmail: _currentUser.Email,
            action: action,
            entityType: entityType,
            entityId: entityId,
            entityName: entityName,
            oldValues: oldValuesJson,
            newValues: newValuesJson,
            description: description,
            ipAddress: GetClientIpAddress(),
            userAgent: GetUserAgent(),
            correlationId: GetCorrelationId());

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task LogAuthenticationAsync(
        AuditAction action,
        Guid? userId,
        string? userEmail,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        var auditLog = AuditLog.Create(
            userId: userId,
            userEmail: userEmail,
            action: action,
            entityType: "User",
            entityId: userId?.ToString(),
            entityName: userEmail,
            oldValues: null,
            newValues: null,
            description: description,
            ipAddress: GetClientIpAddress(),
            userAgent: GetUserAgent(),
            correlationId: GetCorrelationId());

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private string? GetClientIpAddress()
    {
        HttpContext? context = _httpContextAccessor.HttpContext;
        if (context is null)
        {
            return null;
        }

        // Check for forwarded IP (behind proxy/load balancer)
        string? forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',').FirstOrDefault()?.Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString();
    }

    private string? GetUserAgent()
    {
        return _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.FirstOrDefault();
    }

    private string? GetCorrelationId()
    {
        return _httpContextAccessor.HttpContext?.TraceIdentifier;
    }
}
