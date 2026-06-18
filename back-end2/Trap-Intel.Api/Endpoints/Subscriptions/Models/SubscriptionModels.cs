namespace Trap_Intel.Api.Endpoints.Subscriptions.Models;

public sealed record CreateSubscriptionRequest(
    Guid PlanId,
    string BillingCycle = "Monthly",
    bool IsTrial = false,
    int TrialDays = 14,
    bool ActivateImmediately = true);

public sealed record SetSubscriptionPaymentMethodRequest(
    Guid PaymentMethodId);

public sealed record CancelSubscriptionRequest(
    string Reason);

public sealed record ChangeSubscriptionPlanRequest(
    Guid PlanId);

public sealed record RenewSubscriptionRequest(
    DateTime? RenewalEndDate = null);

public sealed record ScheduleSubscriptionCancellationRequest(
    string Reason);

public sealed record RecordSubscriptionUsageSnapshotRequest(
    int HoneypotsActive,
    decimal StorageUsedGb,
    int ApiCallsCount = 0,
    int ActiveUsers = 0,
    int EventsCaptured = 0,
    string PeriodType = "Daily");

public sealed record MarkMonthlyUsageBilledRequest(
    Guid InvoiceId);
