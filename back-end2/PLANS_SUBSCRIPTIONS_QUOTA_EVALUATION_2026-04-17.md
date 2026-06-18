# Plans / Subscriptions / Quota Comprehensive Evaluation

Date: 2026-04-17  
Scope: Domain, Domain Boundaries, Infrastructure, Endpoints, Validation, Code Quality, Business Rules, Tests

## 1) Executive Verdict

Overall status: **Partially Ready**  
Estimated readiness for stable production behavior in this module: **7/10**

Strengths:
- Clear aggregate-centric modeling for `Plan` and `Subscription`.
- Good API permission and org-boundary checks on subscription endpoints.
- Good baseline of integration security tests and endpoint behavior tests.
- Quota audit/alert event handlers are implemented and operational.

Key blockers before calling this module fully robust:
- Plan-change/downgrade path does not enforce usage-fit rules.
- Quota summary API reports API-call usage as zero (data accuracy gap).
- Subscription creation path does not explicitly enforce plan active-state.
- Domain/service layer contains significant unused/legacy paths that duplicate and conflict with live logic.

## 2) Findings (Ordered by Severity)

### CRITICAL-1: Plan change flow can apply downgraded quotas without usage-fit enforcement

Evidence:
- `ManageSubscriptionLifecycleCommandHandler.ChangePlanAsync` changes plan and directly updates quota template without checking current usage vs new quota limits.
- Existing conflict codes (`Subscription.CannotDowngradeWithHighUsage`) are mapped by API, but not emitted in this path.

Files:
- `Trap-intell.Application/Subscriptions/Commands/ManageSubscriptionLifecycle/ManageSubscriptionLifecycleCommandHandler.cs`
- `Trap-Intel.Api/Endpoints/Subscriptions/SubscriptionManagementEndpoints.cs`
- `Trap-Intel.Domain/Subscriptions/Rules/SubscriptionBusinessRules.cs`

Why this matters:
- A downgrade can succeed even if tenant current usage exceeds target plan limits, causing immediate over-limit inconsistencies and business-policy violation.

Recommendation:
- In `ChangePlanAsync`, validate projected/current usage against target quota before `subscription.ChangePlan(...)` and `UpdateQuota(...)`.
- Return explicit domain errors (`Subscription.CannotDowngradeWithHighUsage` / `Subscription.PlanChangeNotAllowed`) on violation.

---

### HIGH-1: Quota summary returns API-call usage as constant zero

Evidence:
- `Subscription.GetQuotaUsageSummary()` hardcodes API-call values to 0.
- `MonthlyUsageSummary.UpdateFromSnapshot(...)` correctly accumulates API calls.
- Query handlers and dashboards rely on `GetQuotaUsageSummary()`, propagating inaccurate output.

Files:
- `Trap-Intel.Domain/Subscriptions/Subscription.cs`
- `Trap-Intel.Domain/Subscriptions/Entities/MonthlyUsageSummary.cs`
- `Trap-intell.Application/Subscriptions/Queries/GetSubscriptionById/GetSubscriptionByIdQueryHandler.cs`
- `Trap-intell.Application/Subscriptions/Queries/GetSubscriptionUsageInsights/GetSubscriptionUsageInsightsQueryHandler.cs`

Why this matters:
- Users see incorrect quota telemetry and may make wrong upgrade/operational decisions.
- Monitoring/billing analytics lose trustworthiness.

Recommendation:
- Compute `CurrentApiCalls` from current month summary (or a dedicated usage counter) and map correct API usage percentage in `GetQuotaUsageSummary()`.

---

### HIGH-2: Subscription creation flow lacks explicit active-plan gate

Evidence:
- Creation flow uses `PlanActivationRule`, but activation rule checks configuration completeness, not active-state.
- No explicit `plan.IsActive` check in create service path.

Files:
- `Trap-Intel.Domain/Subscriptions/Services/CreateSubscriptionService.cs`
- `Trap-Intel.Domain/Plans/Rules/PlanBusinessRules.cs`

Why this matters:
- Inactive plans can still be subscribed if they are otherwise configured.

Recommendation:
- Add explicit guard: reject create/trial create when `!plan.IsActive`.
- Optionally include this in `PlanActivationRule` or create dedicated subscription-eligibility rule.

---

### HIGH-3: `CanAddHoneypot()` has off-by-one behavior at hard limit boundary

Evidence:
- Limit check uses `IsHoneypotLimitExceeded(current)` where exceeded means `current > max`.
- At `current == max`, method still returns allowed.

Files:
- `Trap-Intel.Domain/Subscriptions/Subscription.cs`
- `Trap-Intel.Domain/Subscriptions/Entities/SubscriptionQuotaEntity.cs`

Why this matters:
- Boundary logic can allow one extra operation before flagging quota exceed.

Recommendation:
- For admission checks, compare projected value (`current + requested`) or use `>=` when semantics are "can add".

---

### MEDIUM-1: Seed data inconsistency between subscription plan and quota source/limits

Evidence (HealthGuard seed):
- Subscription row references enterprise plan.
- Quota seed comment/source references professional plan and smaller limits.

Files:
- `Trap-Intel.Infrastructure/Persistence/SeedData/Seeders/SubscriptionSeeder.cs`
- `Trap-Intel.Infrastructure/Persistence/SeedData/Seeders/SubscriptionQuotaSeeder.cs`

Why this matters:
- Test/local data can mislead QA and scenario automation.
- Operational assumptions from seeded env become unreliable.

Recommendation:
- Align seeded quota source and values with seeded subscription plan.

---

### MEDIUM-2: Large legacy/unreferenced quota/subscription service surface increases ambiguity

Evidence:
- Multiple validator/services exist but are not referenced by application flow.
- Some legacy rules include hardcoded fallback limits and TODO-based logic.

Files (examples):
- `Trap-Intel.Domain/Subscriptions/Services/SubscriptionUsageService.cs`
- `Trap-Intel.Domain/Subscriptions/Services/SubscriptionEligibilityValidator.cs`
- `Trap-Intel.Domain/Subscriptions/Services/PlanChangeValidator.cs`
- `Trap-Intel.Domain/Subscriptions/Services/QuotaValidationService.cs`
- `Trap-Intel.Domain/Subscriptions/Rules/SubscriptionBusinessRules.cs`

Why this matters:
- Maintenance risk: developers may edit inactive code paths assuming they are live.
- Business rules become fragmented across live and dead logic.

Recommendation:
- Decide canonical path and archive/remove unreferenced services.
- Keep one active rule implementation per business invariant.

---

### MEDIUM-3: Renew command has validator/handler behavior mismatch

Evidence:
- Validator requires non-null `RenewalEndDate` for renew action.
- Handler still includes defaulting logic when date is null.

Files:
- `Trap-intell.Application/Subscriptions/Commands/ManageSubscriptionLifecycle/ManageSubscriptionLifecycleCommand.cs`
- `Trap-intell.Application/Subscriptions/Commands/ManageSubscriptionLifecycle/ManageSubscriptionLifecycleCommandHandler.cs`

Why this matters:
- Dead branch and policy ambiguity (is date optional or required?).

Recommendation:
- Choose one behavior: either keep required date and remove fallback, or make date optional and relax validator.

## 3) Domain Boundary Assessment

Verdict: **Good direction, mixed execution**

What is good:
- Aggregates (`Plan`, `Subscription`) hold meaningful behavior and emit domain events.
- Application handlers mostly orchestrate repositories + aggregate calls.

What needs tightening:
- Some invariants are defined in rules/services but not enforced in active command paths.
- Parallel legacy service layer duplicates live behavior using older models.

## 4) Infrastructure Assessment

Verdict: **Solid baseline with data-consistency caveat**

What is good:
- Repositories include required details (`Quota`, history, snapshots, summaries).
- EF configurations cover owned/value and precision-sensitive fields.

Gaps:
- Seed consistency issue (plan/quota mismatch for at least one subscription).
- No visible optimistic-concurrency strategy for lifecycle-critical updates.

## 5) Endpoint + Validation Assessment

Verdict: **Strong security posture, decent validation, some business gaps**

What is good:
- Endpoints protected by permissions and org checks.
- Error mapping is reasonably explicit for domain errors.

Gaps:
- Validation/data-model does not cover all business constraints; key checks still depend on handler logic.
- Some mapped conflict codes appear unreachable from active handler logic.

## 6) Test Assessment

Executed now:
- Targeted plans/subscriptions/quota tests: **36 passed, 0 failed**.

Coverage snapshot for critical files (from targeted run):
- `Subscription.cs`: ~15.7%
- `ManageSubscriptionLifecycleCommandHandler.cs`: ~0%
- `CheckSubscriptionQuotaOperationQueryHandler.cs`: ~0%
- `Plan.cs`: ~32.6%
- `CreatePlanCommandHandler.cs`: ~0%

Interpretation:
- Current tests prove endpoint/security basics and selected happy/negative paths.
- Core lifecycle and quota-business internals remain under-tested.

## 7) Prioritized Action Plan

P0 (Immediate)
1. Enforce downgrade usage-fit validation in plan-change handler.
2. Fix API-call quota summary computation (remove hardcoded zero path).
3. Add explicit active-plan check in subscription creation flows.
4. Fix boundary check in `CanAddHoneypot()`.

P1 (Short-term)
1. Clean up or deprecate unreferenced legacy services/rules.
2. Align renew validator/handler contract.
3. Fix seed plan/quota inconsistency.

P2 (Hardening)
1. Add targeted unit tests for lifecycle transitions and downgrade conflicts.
2. Add tests for quota summary API-calls correctness.
3. Add optimistic-concurrency handling tests for lifecycle updates.

## 8) Final Readiness Statement

Can this module run? **Yes**.  
Can it be considered fully reliable for business-critical billing/quota governance without fixes? **Not yet**.

The module is close, but the highlighted P0 items should be resolved before calling the Plans/Subscriptions/Quota area fully hardened.
