using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Trap_Intel.Application.Abstractions.RealTime;

namespace Trap_Intel.Infrastructure.Notifications.RealTime;

internal sealed class SignalRListRealtimeNotifier : IListRealtimeNotifier
{
    private readonly IHubContext<ListUpdatesHub, IListUpdatesHubClient> _hubContext;
    private readonly ILogger<SignalRListRealtimeNotifier> _logger;

    public SignalRListRealtimeNotifier(
        IHubContext<ListUpdatesHub, IListUpdatesHubClient> hubContext,
        ILogger<SignalRListRealtimeNotifier> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyOrganizationListChangedAsync(
        string entity,
        Guid organizationId,
        string? filterKey = null,
        string action = "updated",
        object? payload = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedEntity = ListUpdateGroups.NormalizeEntity(entity);
        var normalizedFilter = ListUpdateGroups.NormalizeFilter(filterKey);

        var message = new ListUpdateMessage(
            normalizedEntity,
            "organization",
            organizationId,
            normalizedFilter,
            action,
            DateTime.UtcNow,
            payload);

        var targetGroup = ListUpdateGroups.Organization(normalizedEntity, organizationId, normalizedFilter);
        await _hubContext.Clients.Group(targetGroup).ListChanged(message);

        if (!string.Equals(normalizedFilter, ListUpdateGroups.AllFilter, StringComparison.Ordinal))
        {
            var wildcardGroup = ListUpdateGroups.Organization(normalizedEntity, organizationId, ListUpdateGroups.AllFilter);
            await _hubContext.Clients.Group(wildcardGroup).ListChanged(message with { FilterKey = ListUpdateGroups.AllFilter });
        }

        _logger.LogDebug(
            "Published organization list update: Entity={Entity}, OrganizationId={OrganizationId}, FilterKey={FilterKey}, Action={Action}",
            normalizedEntity,
            organizationId,
            normalizedFilter,
            action);
    }

    public async Task NotifyUserListChangedAsync(
        string entity,
        Guid userId,
        string? filterKey = null,
        string action = "updated",
        object? payload = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedEntity = ListUpdateGroups.NormalizeEntity(entity);
        var normalizedFilter = ListUpdateGroups.NormalizeFilter(filterKey);

        var message = new ListUpdateMessage(
            normalizedEntity,
            "user",
            userId,
            normalizedFilter,
            action,
            DateTime.UtcNow,
            payload);

        var targetGroup = ListUpdateGroups.User(normalizedEntity, userId, normalizedFilter);
        await _hubContext.Clients.Group(targetGroup).ListChanged(message);

        if (!string.Equals(normalizedFilter, ListUpdateGroups.AllFilter, StringComparison.Ordinal))
        {
            var wildcardGroup = ListUpdateGroups.User(normalizedEntity, userId, ListUpdateGroups.AllFilter);
            await _hubContext.Clients.Group(wildcardGroup).ListChanged(message with { FilterKey = ListUpdateGroups.AllFilter });
        }

        _logger.LogDebug(
            "Published user list update: Entity={Entity}, UserId={UserId}, FilterKey={FilterKey}, Action={Action}",
            normalizedEntity,
            userId,
            normalizedFilter,
            action);
    }
}
