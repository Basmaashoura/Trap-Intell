using System;
using Trap_Intel.Domain.Shared;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Plans;
using Trap_Intel.Domain.Subscriptions.Entities;
using Trap_Intel.Domain.Subscriptions.Events;

namespace Trap_Intel.Domain.Subscriptions
{
    /// <summary>
    /// Represents an organization's subscription to a plan.
    /// Enterprise-grade design with factory methods, validation, and domain events.
    /// Owns quota and usage tracking entities.
    /// </summary>
    public class Subscription : AggregateRoot<Guid>
    {
        private List<SubscriptionQuotaEntity> _quotaHistory = new();
        private List<UsageSnapshot> _usageSnapshots = new();
        private List<MonthlyUsageSummary> _monthlySummaries = new();

        private Subscription() { }

        private Subscription(
            Guid id,
            Guid organizationId,
            Guid planId,
            SubscriptionPeriod period,
            BillingCycle billingCycle,
            BillingInfo billingInfo)
            : base(id)
        {
            OrganizationId = organizationId;
            PlanId = planId;
            Period = period;
            BillingCycle = billingCycle;
            BillingInfo = billingInfo;
            Status = SubscriptionStatus.Trial;
            CurrentUsage = new UsageStatistics(0, 0);
            IsAutoRenew = true;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        #region Properties

        public Guid OrganizationId { get; private set; }
        public Guid PlanId { get; private set; }
        public SubscriptionStatus Status { get; private set; }
        public SubscriptionPeriod Period { get; private set; } = null!;
        public BillingCycle BillingCycle { get; private set; }
        public BillingInfo BillingInfo { get; private set; } = null!;
        public Guid? PaymentMethodId { get; private set; }
        public bool IsAutoRenew { get; private set; }
        public CancellationInfo? CancellationInfo { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        /// <summary>
        /// Current active quota for this subscription.
        /// </summary>
        public SubscriptionQuotaEntity? Quota { get; private set; }

        /// <summary>
        /// Current usage statistics (quick access).
        /// </summary>
        public UsageStatistics CurrentUsage { get; private set; } = null!;

        /// <summary>
        /// History of quota changes (for auditing).
        /// </summary>
        public IReadOnlyList<SubscriptionQuotaEntity> QuotaHistory => _quotaHistory.AsReadOnly();

        /// <summary>
        /// Recent usage snapshots (for trend analysis).
        /// </summary>
        public IReadOnlyList<UsageSnapshot> UsageSnapshots => _usageSnapshots.AsReadOnly();

        /// <summary>
        /// Monthly usage summaries (for billing).
        /// </summary>
        public IReadOnlyList<MonthlyUsageSummary> MonthlySummaries => _monthlySummaries.AsReadOnly();

        // Legacy property for backward compatibility
        [Obsolete("Use CurrentUsage instead")]
        public UsageStatistics Usage => CurrentUsage;

        #endregion

        #region Factory Methods

        /// <summary>
        /// Factory method to create a new subscription.
        /// </summary>
        public static Result<Subscription> Create(
            Guid organizationId,
            Guid planId,
            SubscriptionPeriod period,
            BillingCycle billingCycle,
            BillingInfo billingInfo)
        {
            // Validation
            if (organizationId == Guid.Empty)
                return Result.Failure<Subscription>(Error.Custom("Subscription.InvalidOrganization", "Organization ID cannot be empty."));

            if (planId == Guid.Empty)
                return Result.Failure<Subscription>(Error.Custom("Subscription.InvalidPlan", "Plan ID cannot be empty."));

            if (period is null)
                return Result.Failure<Subscription>(Error.Custom("Subscription.InvalidPeriod", "Subscription period cannot be null."));

            if (period.StartDate >= period.EndDate && period.EndDate.HasValue)
                return Result.Failure<Subscription>(Error.Custom("Subscription.InvalidDates", "Start date must be before end date."));

            if (billingInfo is null)
                return Result.Failure<Subscription>(Error.Custom("Subscription.InvalidBilling", "Billing info cannot be null."));

            var subscription = new Subscription(
                Guid.NewGuid(),
                organizationId,
                planId,
                period,
                billingCycle,
                billingInfo);

            subscription.RaiseDomainEvent(new SubscriptionCreatedEvent(
                subscription.Id,
                organizationId,
                planId,
                DateTime.UtcNow));

            return Result.Success(subscription);
        }

        /// <summary>
        /// Factory method to reconstruct subscription from database.
        /// </summary>
        public static Subscription Reconstruct(
            Guid id,
            Guid organizationId,
            Guid planId,
            SubscriptionStatus status,
            SubscriptionPeriod period,
            BillingCycle billingCycle,
            BillingInfo billingInfo,
            UsageStatistics usage,
            Guid? paymentMethodId,
            bool isAutoRenew,
            CancellationInfo? cancellationInfo,
            DateTime createdAt,
            DateTime updatedAt,
            SubscriptionQuotaEntity? quota = null,
            List<SubscriptionQuotaEntity>? quotaHistory = null,
            List<UsageSnapshot>? usageSnapshots = null,
            List<MonthlyUsageSummary>? monthlySummaries = null)
        {
            var subscription = new Subscription(
                id,
                organizationId,
                planId,
                period,
                billingCycle,
                billingInfo)
            {
                Status = status,
                CurrentUsage = usage,
                PaymentMethodId = paymentMethodId,
                IsAutoRenew = isAutoRenew,
                CancellationInfo = cancellationInfo,
                CreatedAt = createdAt,
                UpdatedAt = updatedAt,
                Quota = quota,
                _quotaHistory = quotaHistory ?? new(),
                _usageSnapshots = usageSnapshots ?? new(),
                _monthlySummaries = monthlySummaries ?? new()
            };

            return subscription;
        }

        #endregion

        #region Domain Operations

        /// <summary>
        /// Activate the subscription.
        /// </summary>
        public Result Activate()
        {
            if (Status == SubscriptionStatus.Active)
                return Result.Failure(Error.Custom("Subscription.AlreadyActive", "Subscription is already active."));

            Status = SubscriptionStatus.Active;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new SubscriptionActivatedEvent(Id, DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>
        /// Suspend the subscription.
        /// </summary>
        public Result Suspend()
        {
            if (Status == SubscriptionStatus.Suspended)
                return Result.Failure(Error.Custom("Subscription.AlreadySuspended", "Subscription is already suspended."));

            Status = SubscriptionStatus.Suspended;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new SubscriptionSuspendedEvent(Id, DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>
        /// Cancel the subscription.
        /// </summary>
        public Result Cancel(string reason)
        {
            if (Status == SubscriptionStatus.Cancelled)
                return Result.Failure(Error.Custom("Subscription.AlreadyCancelled", "Subscription is already cancelled."));

            if (string.IsNullOrWhiteSpace(reason))
                return Result.Failure(Error.Custom("Subscription.InvalidReason", "Cancellation reason cannot be empty."));

            Status = SubscriptionStatus.Cancelled;
            CancellationInfo = new CancellationInfo(DateTime.UtcNow, reason);
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new SubscriptionCancelledEvent(Id, reason, DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>
        /// Update usage statistics (legacy - prefer RecordUsageSnapshot).
        /// </summary>
        [Obsolete("Use RecordUsageSnapshot for proper historical tracking")]
        public void UpdateUsage(int honeypotsUsed, decimal storageUsedGb, decimal overageCharges = 0)
        {
            CurrentUsage = new UsageStatistics(honeypotsUsed, storageUsedGb, overageCharges);
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new SubscriptionUsageUpdatedEvent(
                Id,
                honeypotsUsed,
                storageUsedGb,
                DateTime.UtcNow));
        }

        /// <summary>
        /// Set payment method for this subscription.
        /// </summary>
        public void SetPaymentMethod(Guid paymentMethodId)
        {
            if (paymentMethodId == Guid.Empty)
                throw new ArgumentException("Payment method ID cannot be empty.", nameof(paymentMethodId));

            PaymentMethodId = paymentMethodId;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Enable auto-renewal.
        /// </summary>
        public void EnableAutoRenewal()
        {
            IsAutoRenew = true;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Disable auto-renewal.
        /// </summary>
        public void DisableAutoRenewal()
        {
            IsAutoRenew = false;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Change the subscription plan to a different plan.
        /// Used for upgrades and downgrades.
        /// </summary>
        public Result ChangePlan(Guid newPlanId, decimal newPrice)
        {
            if (newPlanId == Guid.Empty)
                return Result.Failure(Error.Custom("Subscription.InvalidNewPlan", "New plan ID cannot be empty."));

            if (newPrice < 0)
                return Result.Failure(Error.Custom("Subscription.InvalidPrice", "Price cannot be negative."));

            var oldPlanId = PlanId;
            PlanId = newPlanId;
            BillingInfo = new BillingInfo(BillingCycle, newPrice, BillingInfo.DiscountApplied);
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new SubscriptionPlanChangedEvent(
                Id,
                oldPlanId,
                newPlanId,
                newPrice,
                DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>
        /// Renew the subscription (extend period).
        /// </summary>
        public Result Renew(SubscriptionPeriod newPeriod)
        {
            if (newPeriod is null)
                return Result.Failure(Error.Custom("Subscription.InvalidPeriod", "New period cannot be null."));

            if (IsCancellationScheduled)
                return Result.Failure(SubscriptionErrors.CancellationAlreadyScheduled);

            // Validate period is contiguous
            var endDate = Period.EndDate ?? DateTime.UtcNow.AddYears(1);
            var periodRule = new SubscriptionPeriodValidityRule(
                new SubscriptionPeriod(Period.StartDate, endDate),
                newPeriod);
            if (!periodRule.IsSatisfied())
                return Result.Failure(periodRule.Error);

            // Update period and status
            Period = newPeriod;
            Status = SubscriptionStatus.Active;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new SubscriptionRenewedEvent(
                Id,
                newPeriod.StartDate,
                newPeriod.EndDate ?? DateTime.UtcNow.AddYears(1),
                DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>
        /// Schedule cancellation at the end of current period.
        /// </summary>
        public void ScheduleCancellationAtPeriodEnd(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Cancellation reason cannot be empty.", nameof(reason));

            var scheduleDate = Period.EndDate ?? DateTime.UtcNow.AddYears(1);
            DisableAutoRenewal();
            CancellationInfo = new CancellationInfo(scheduleDate, reason);
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Clear an existing scheduled cancellation.
        /// </summary>
        public void ClearScheduledCancellation()
        {
            if (CancellationInfo is null)
            {
                return;
            }

            CancellationInfo = null;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Get cancellation status.
        /// </summary>
        public bool IsCancellationScheduled => CancellationInfo is not null;

        /// <summary>
        /// Update usage with validation result (legacy - prefer RecordUsageSnapshot).
        /// </summary>
        [Obsolete("Use RecordUsageSnapshot for proper historical tracking")]
        public Result UpdateUsage(int honeypotsUsed, decimal storageUsedGb)
        {
            if (honeypotsUsed < 0)
                return Result.Failure(SubscriptionErrors.SubscriptionHoneypotLimitExceeded);

            if (storageUsedGb < 0)
                return Result.Failure(SubscriptionErrors.SubscriptionStorageLimitExceeded);

            CurrentUsage = new UsageStatistics(honeypotsUsed, storageUsedGb);
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new SubscriptionUsageUpdatedEvent(
                Id,
                honeypotsUsed,
                storageUsedGb,
                DateTime.UtcNow));

            return Result.Success();
        }

        #endregion

        #region Trial Management

        /// <summary>
        /// Check if trial subscription has expired.
        /// </summary>
        public bool IsTrialExpired()
        {
            if (Status != SubscriptionStatus.Trial)
                return false;

            if (!Period.EndDate.HasValue)
                return false;

            return DateTime.UtcNow > Period.EndDate.Value;
        }

        /// <summary>
        /// Expire trial subscription (called by background job).
        /// </summary>
        public Result ExpireTrial()
        {
            if (Status != SubscriptionStatus.Trial)
                return Result.Failure(
                    Error.Custom("Subscription.NotInTrial", "Subscription is not in trial status."));

            if (!IsTrialExpired())
                return Result.Failure(
                    Error.Custom("Subscription.TrialNotExpired", "Trial period has not expired yet."));

            Status = SubscriptionStatus.Expired;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new SubscriptionExpiredEvent(
                Id,
                Period.EndDate ?? DateTime.UtcNow,
                DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>
        /// Get days remaining in trial period.
        /// </summary>
        public int GetTrialDaysRemaining()
        {
            if (Status != SubscriptionStatus.Trial || !Period.EndDate.HasValue)
                return 0;

            var remaining = (Period.EndDate.Value - DateTime.UtcNow).Days;
            return remaining > 0 ? remaining : 0;
        }

        #endregion

        #region Overage Management

        /// <summary>
        /// Calculate overage charges based on current usage against quota.
        /// Uses internal quota.
        /// </summary>
        public decimal CalculateOverageCharges()
        {
            if (Quota == null)
                return 0;

            return Quota.CalculateOverageCharges(CurrentUsage.HoneypotsUsed, CurrentUsage.StorageUsedGb);
        }

        /// <summary>
        /// Calculate overage charges based on usage exceeding quota.
        /// Legacy method for backward compatibility.
        /// </summary>
        [Obsolete("Use CalculateOverageCharges() without parameters")]
        public decimal CalculateOverageCharges(
            SubscriptionQuota quota,
            decimal pricePerExtraHoneypot,
            decimal pricePerExtraGb)
        {
            if (quota is null)
                return 0;

            var honeypotsOverage = Math.Max(0, CurrentUsage.HoneypotsUsed - quota.MaxHoneypots);
            var storageOverage = Math.Max(0, CurrentUsage.StorageUsedGb - quota.MaxStorageGb);

            var overageCharge =
                (honeypotsOverage * pricePerExtraHoneypot) +
                (storageOverage * pricePerExtraGb);

            return Math.Round(overageCharge, 2);
        }

        /// <summary>
        /// Check if subscription has usage overages.
        /// Uses internal quota.
        /// </summary>
        public bool HasOverages()
        {
            if (Quota == null)
                return false;

            return Quota.IsHoneypotLimitExceeded(CurrentUsage.HoneypotsUsed) ||
                   Quota.IsStorageLimitExceeded(CurrentUsage.StorageUsedGb);
        }

        /// <summary>
        /// Check if subscription has usage overages.
        /// Legacy method for backward compatibility.
        /// </summary>
        [Obsolete("Use HasOverages() without parameters")]
        public bool HasOverages(SubscriptionQuota quota)
        {
            if (quota is null)
                return false;

            return CurrentUsage.HoneypotsUsed > quota.MaxHoneypots ||
                   CurrentUsage.StorageUsedGb > quota.MaxStorageGb;
        }

        #endregion

        #region Quota Management

        /// <summary>
        /// Initialize quota for subscription (typically from plan).
        /// </summary>
        public Result InitializeQuota(
            int maxHoneypots,
            decimal maxStorageGb,
            int maxMonthlyApiCalls = 1000000,
            int maxUsers = 100,
            bool hardLimitEnforced = false,
            decimal overageHoneypotRate = 10m,
            decimal overageStorageRatePerGb = 0.50m)
        {
            if (Quota != null)
                return Result.Failure(Error.Custom("Subscription.QuotaAlreadyExists", "Quota already initialized. Use UpdateQuota instead."));

            var quotaResult = SubscriptionQuotaEntity.Create(
                Id,
                maxHoneypots,
                maxStorageGb,
                maxMonthlyApiCalls,
                maxUsers,
                hardLimitEnforced,
                overageHoneypotRate,
                overageStorageRatePerGb,
                PlanId);

            if (quotaResult.IsFailure)
                return Result.Failure(quotaResult.Errors[0]);

            Quota = quotaResult.Value;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new QuotaCreatedEvent(
                Quota.Id,
                Id,
                maxHoneypots,
                maxStorageGb,
                PlanId,
                DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>
        /// Update quota (typically when plan changes).
        /// </summary>
        public Result UpdateQuota(
            int maxHoneypots,
            decimal maxStorageGb,
            int maxMonthlyApiCalls = 1000000,
            int maxUsers = 100,
            bool hardLimitEnforced = false,
            decimal overageHoneypotRate = 10m,
            decimal overageStorageRatePerGb = 0.50m,
            Guid? newPlanId = null)
        {
            var oldQuota = Quota;
            
            var quotaResult = SubscriptionQuotaEntity.Create(
                Id,
                maxHoneypots,
                maxStorageGb,
                maxMonthlyApiCalls,
                maxUsers,
                hardLimitEnforced,
                overageHoneypotRate,
                overageStorageRatePerGb,
                newPlanId ?? PlanId);

            if (quotaResult.IsFailure)
                return Result.Failure(quotaResult.Errors[0]);

            // Archive old quota
            if (oldQuota != null)
            {
                oldQuota.CloseEffectivePeriod();
                _quotaHistory.Add(oldQuota);
            }

            Quota = quotaResult.Value;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new QuotaChangedEvent(
                Id,
                oldQuota?.Id ?? Guid.Empty,
                Quota.Id,
                oldQuota?.MaxHoneypots ?? 0,
                maxHoneypots,
                oldQuota?.MaxStorageGb ?? 0,
                maxStorageGb,
                newPlanId ?? PlanId,
                DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>
        /// Check if operation is allowed based on quota.
        /// </summary>
        public Result<bool> CanAddHoneypot()
        {
            if (Quota == null)
                return Result.Failure<bool>(QuotaErrors.NoActiveQuota);

            var projectedHoneypots = CurrentUsage.HoneypotsUsed + 1;

            if (Quota.IsHoneypotLimitExceeded(projectedHoneypots))
            {
                if (Quota.HardLimitEnforced)
                {
                    RaiseDomainEvent(new QuotaEnforcementBlockedEvent(
                        Id,
                        QuotaResourceType.Honeypots,
                        "AddHoneypot",
                        projectedHoneypots,
                        Quota.MaxHoneypots,
                        DateTime.UtcNow));

                    return Result.Failure<bool>(QuotaErrors.HoneypotLimitExceeded);
                }
                
                // Soft limit - allow with warning
                RaiseDomainEvent(new QuotaExceededEvent(
                    Id,
                    QuotaResourceType.Honeypots,
                    projectedHoneypots,
                    Quota.MaxHoneypots,
                    false,
                    DateTime.UtcNow));
            }

            return Result.Success(true);
        }

        /// <summary>
        /// Get quota usage summary.
        /// </summary>
        public QuotaUsageSummary GetQuotaUsageSummary()
        {
            if (Quota == null)
                return new QuotaUsageSummary(0, 0, 0, 0, 0, 0, 0, 0, 0);

            var currentApiCalls = GetCurrentMonthApiCalls();

            return new QuotaUsageSummary(
                CurrentUsage.HoneypotsUsed,
                Quota.MaxHoneypots,
                Quota.GetHoneypotUsagePercent(CurrentUsage.HoneypotsUsed),
                CurrentUsage.StorageUsedGb,
                Quota.MaxStorageGb,
                Quota.GetStorageUsagePercent(CurrentUsage.StorageUsedGb),
                currentApiCalls,
                Quota.MaxMonthlyApiCalls,
                Quota.GetApiCallsUsagePercent(currentApiCalls));
        }

        private int GetCurrentMonthApiCalls()
        {
            var now = DateTime.UtcNow;

            var currentMonthSummary = _monthlySummaries
                .LastOrDefault(summary => summary.Year == now.Year && summary.Month == now.Month);

            if (currentMonthSummary != null)
            {
                return currentMonthSummary.TotalApiCalls;
            }

            return _usageSnapshots
                .Where(snapshot => snapshot.RecordedAt.Year == now.Year && snapshot.RecordedAt.Month == now.Month)
                .Sum(snapshot => snapshot.ApiCallsCount);
        }

        #endregion

        #region Usage Tracking

        /// <summary>
        /// Record a usage snapshot.
        /// </summary>
        public Result RecordUsageSnapshot(
            int honeypotsActive,
            decimal storageUsedGb,
            int apiCallsCount = 0,
            int activeUsers = 0,
            int eventsCaptured = 0,
            UsagePeriodType periodType = UsagePeriodType.Daily)
        {
            var previousSnapshot = _usageSnapshots.LastOrDefault();

            var snapshotResult = UsageSnapshot.CreateWithDelta(
                Id,
                periodType,
                honeypotsActive,
                storageUsedGb,
                apiCallsCount,
                activeUsers,
                eventsCaptured,
                previousSnapshot);

            if (snapshotResult.IsFailure)
                return Result.Failure(snapshotResult.Errors[0]);

            var snapshot = snapshotResult.Value;
            _usageSnapshots.Add(snapshot);

            // Update current usage
            CurrentUsage = new UsageStatistics(honeypotsActive, storageUsedGb);
            UpdatedAt = DateTime.UtcNow;

            // Update monthly summary
            var currentMonthSummary = GetOrCreateCurrentMonthSummary();
            if (currentMonthSummary.IsSuccess)
            {
                currentMonthSummary.Value.UpdateFromSnapshot(snapshot);
            }

            RaiseDomainEvent(new UsageSnapshotRecordedEvent(
                snapshot.Id,
                Id,
                periodType,
                honeypotsActive,
                storageUsedGb,
                apiCallsCount,
                DateTime.UtcNow));

            // Check quota warnings
            CheckQuotaWarnings(honeypotsActive, storageUsedGb);

            return Result.Success();
        }

        /// <summary>
        /// Get or create the current month's usage summary.
        /// </summary>
        public Result<MonthlyUsageSummary> GetOrCreateCurrentMonthSummary()
        {
            var now = DateTime.UtcNow;
            var existing = _monthlySummaries.FirstOrDefault(s => s.Year == now.Year && s.Month == now.Month);
            
            if (existing != null)
                return Result.Success(existing);

            var summaryResult = MonthlyUsageSummary.CreateForCurrentMonth(Id);
            if (summaryResult.IsFailure)
                return summaryResult;

            _monthlySummaries.Add(summaryResult.Value);
            return Result.Success(summaryResult.Value);
        }

        /// <summary>
        /// Finalize a monthly usage summary.
        /// </summary>
        public Result FinalizeMonthlyUsage(int year, int month)
        {
            var summary = _monthlySummaries.FirstOrDefault(s => s.Year == year && s.Month == month);
            if (summary == null)
                return Result.Failure(QuotaErrors.SummaryNotFound);

            // Get snapshots for this month
            var monthSnapshots = _usageSnapshots
                .Where(s => s.RecordedAt.Year == year && s.RecordedAt.Month == month)
                .ToList();

            // Finalize averages
            var avgResult = summary.FinalizeAverages(monthSnapshots);
            if (avgResult.IsFailure)
                return avgResult;

            // Calculate overages
            if (Quota != null)
            {
                var overageResult = summary.CalculateOverages(Quota);
                if (overageResult.IsFailure)
                    return overageResult;
            }

            // Finalize
            var finalizeResult = summary.Finalize();
            if (finalizeResult.IsFailure)
                return finalizeResult;

            RaiseDomainEvent(new MonthlyUsageFinalizedEvent(
                summary.Id,
                Id,
                year,
                month,
                summary.PeakHoneypots,
                summary.PeakStorageGb,
                summary.TotalApiCalls,
                summary.OverageCharges,
                DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>
        /// Mark monthly usage as billed.
        /// </summary>
        public Result MarkMonthlyUsageAsBilled(int year, int month, Guid invoiceId)
        {
            var summary = _monthlySummaries.FirstOrDefault(s => s.Year == year && s.Month == month);
            if (summary == null)
                return Result.Failure(QuotaErrors.SummaryNotFound);

            var result = summary.MarkAsBilled(invoiceId);
            if (result.IsFailure)
                return result;

            RaiseDomainEvent(new MonthlyUsageBilledEvent(
                summary.Id,
                Id,
                invoiceId,
                summary.OverageCharges,
                DateTime.UtcNow));

            return Result.Success();
        }

        private void CheckQuotaWarnings(int honeypots, decimal storageGb)
        {
            if (Quota == null) return;

            const decimal warningThreshold = 80m;

            var honeypotPercent = Quota.GetHoneypotUsagePercent(honeypots);
            var storagePercent = Quota.GetStorageUsagePercent(storageGb);

            if (honeypotPercent >= warningThreshold && honeypotPercent < 100)
            {
                RaiseDomainEvent(new QuotaWarningEvent(
                    Id,
                    QuotaResourceType.Honeypots,
                    honeypotPercent,
                    warningThreshold,
                    DateTime.UtcNow));
            }

            if (storagePercent >= warningThreshold && storagePercent < 100)
            {
                RaiseDomainEvent(new QuotaWarningEvent(
                    Id,
                    QuotaResourceType.Storage,
                    storagePercent,
                    warningThreshold,
                    DateTime.UtcNow));
            }
        }

        #endregion
    }
}
