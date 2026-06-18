namespace Trap_Intel.Application.Abstractions.RealTime;

public interface IListRealtimeNotifier
{
    Task NotifyOrganizationListChangedAsync(
        string entity,
        Guid organizationId,
        string? filterKey = null,
        string action = "updated",
        object? payload = null,
        CancellationToken cancellationToken = default);

    Task NotifyUserListChangedAsync(
        string entity,
        Guid userId,
        string? filterKey = null,
        string action = "updated",
        object? payload = null,
        CancellationToken cancellationToken = default);
}
