namespace Trap_Intel.Infrastructure.Notifications.RealTime;

public sealed record ListUpdateMessage(
    string Entity,
    string Scope,
    Guid ScopeId,
    string FilterKey,
    string Action,
    DateTime OccurredAtUtc,
    object? Payload = null);
