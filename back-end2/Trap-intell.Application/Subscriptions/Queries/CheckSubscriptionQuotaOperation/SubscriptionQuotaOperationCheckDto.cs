namespace Trap_Intel.Application.Subscriptions.Queries.CheckSubscriptionQuotaOperation;

public sealed record SubscriptionQuotaOperationCheckDto(
    Guid SubscriptionId,
    int CurrentHoneypots,
    int MaxHoneypots,
    decimal CurrentStorageGb,
    decimal MaxStorageGb,
    int AdditionalHoneypotsRequested,
    decimal AdditionalStorageGbRequested,
    int ProjectedHoneypots,
    decimal ProjectedStorageGb,
    bool HardLimitEnforced,
    bool WouldExceedQuota,
    bool IsAllowed,
    string Message);
