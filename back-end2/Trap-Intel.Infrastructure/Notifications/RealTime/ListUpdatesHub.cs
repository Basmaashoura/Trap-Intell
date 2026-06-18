using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Trap_Intel.Infrastructure.Notifications.RealTime;

[Authorize]
public sealed class ListUpdatesHub : Hub<IListUpdatesHubClient>
{
    private readonly ILogger<ListUpdatesHub> _logger;

    public ListUpdatesHub(ILogger<ListUpdatesHub> logger)
    {
        _logger = logger;
    }

    public async Task SubscribeOrganizationList(string entity, string? filterKey = null)
    {
        var organizationId = GetOrganizationId();
        var normalizedFilter = ListUpdateGroups.NormalizeFilter(filterKey);

        var specificGroup = ListUpdateGroups.Organization(entity, organizationId, normalizedFilter);
        await Groups.AddToGroupAsync(Context.ConnectionId, specificGroup);

        // Always subscribe to the wildcard channel to receive fallback invalidation events.
        var wildcardGroup = ListUpdateGroups.Organization(entity, organizationId, ListUpdateGroups.AllFilter);
        if (!string.Equals(specificGroup, wildcardGroup, StringComparison.Ordinal))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, wildcardGroup);
        }

        _logger.LogInformation(
            "Connection {ConnectionId} subscribed to organization list {Entity} ({FilterKey})",
            Context.ConnectionId,
            entity,
            normalizedFilter);
    }

    public async Task UnsubscribeOrganizationList(string entity, string? filterKey = null)
    {
        var organizationId = GetOrganizationId();
        var normalizedFilter = ListUpdateGroups.NormalizeFilter(filterKey);

        var specificGroup = ListUpdateGroups.Organization(entity, organizationId, normalizedFilter);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, specificGroup);

        var wildcardGroup = ListUpdateGroups.Organization(entity, organizationId, ListUpdateGroups.AllFilter);
        if (!string.Equals(specificGroup, wildcardGroup, StringComparison.Ordinal))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, wildcardGroup);
        }

        _logger.LogInformation(
            "Connection {ConnectionId} unsubscribed from organization list {Entity} ({FilterKey})",
            Context.ConnectionId,
            entity,
            normalizedFilter);
    }

    public async Task SubscribeUserList(string entity, string? filterKey = null)
    {
        var userId = GetUserId();
        var normalizedFilter = ListUpdateGroups.NormalizeFilter(filterKey);

        var specificGroup = ListUpdateGroups.User(entity, userId, normalizedFilter);
        await Groups.AddToGroupAsync(Context.ConnectionId, specificGroup);

        var wildcardGroup = ListUpdateGroups.User(entity, userId, ListUpdateGroups.AllFilter);
        if (!string.Equals(specificGroup, wildcardGroup, StringComparison.Ordinal))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, wildcardGroup);
        }

        _logger.LogInformation(
            "Connection {ConnectionId} subscribed to user list {Entity} ({FilterKey})",
            Context.ConnectionId,
            entity,
            normalizedFilter);
    }

    public async Task UnsubscribeUserList(string entity, string? filterKey = null)
    {
        var userId = GetUserId();
        var normalizedFilter = ListUpdateGroups.NormalizeFilter(filterKey);

        var specificGroup = ListUpdateGroups.User(entity, userId, normalizedFilter);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, specificGroup);

        var wildcardGroup = ListUpdateGroups.User(entity, userId, ListUpdateGroups.AllFilter);
        if (!string.Equals(specificGroup, wildcardGroup, StringComparison.Ordinal))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, wildcardGroup);
        }

        _logger.LogInformation(
            "Connection {ConnectionId} unsubscribed from user list {Entity} ({FilterKey})",
            Context.ConnectionId,
            entity,
            normalizedFilter);
    }

    private Guid GetOrganizationId()
    {
        var organizationIdClaim =
            Context.User?.FindFirst("org")?.Value ??
            Context.User?.FindFirst("organizationId")?.Value;

        if (!Guid.TryParse(organizationIdClaim, out var organizationId))
        {
            throw new HubException("Organization claim is required for this subscription.");
        }

        return organizationId;
    }

    private Guid GetUserId()
    {
        var userIdClaim = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            throw new HubException("User identity claim is required for this subscription.");
        }

        return userId;
    }
}
