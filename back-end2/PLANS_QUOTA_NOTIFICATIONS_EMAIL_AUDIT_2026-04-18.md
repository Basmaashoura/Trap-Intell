# Plans, Quota, Notifications, and Email Audit (2026-04-18)

## Scope
- Plan endpoints and feature necessity
- Quota endpoint necessity and operational usage
- Notification integration coverage
- Email integration and template quality
- Validation by automated tests

## 1) Plan + Quota Endpoint Necessity Matrix

### Core Required (keep)
- `GET /api/plans/`
- `POST /api/plans/{planId}/activate`
- `POST /api/plans/{planId}/deactivate`
- `GET /api/organizations/{organizationId}/subscriptions/current/quota`
- `GET /api/organizations/{organizationId}/subscriptions/{subscriptionId}/quota/check`
- `POST /api/organizations/{organizationId}/subscriptions/{subscriptionId}/usage/snapshots`
- `POST /api/organizations/{organizationId}/subscriptions/{subscriptionId}/usage/monthly/{year}/{month}/finalize`
- `POST /api/organizations/{organizationId}/subscriptions/{subscriptionId}/usage/monthly/{year}/{month}/mark-billed`

### Operationally Useful (optional, but strongly recommended)
- `GET /api/plans/all` (system admin visibility/control)
- `GET /api/plans/{planId}/pricing` (billing UX and transparency)
- `GET /api/plans/{planId}/quota-template` (pre-change impact checks)

## 2) Evidence of Real Usage
- Scenario automation uses the endpoint surface heavily:
  - `scripts/plans-subscriptions-quota-200-scenarios.ps1`
- Integration tests cover happy/security/negative paths for plans/subscriptions/quota:
  - `Trap-Intel.Tests/Integration/Plans/*`
  - `Trap-Intel.Tests/Integration/Subscriptions/*`

## 3) Notification Integration Status

### Covered
- Quota exceeded and quota enforcement blocked events create alerts and dispatch notifications.
- Quota warning now also creates alerts and dispatches notifications (newly wired).
- Plan lifecycle events (`PlanCreatedEvent`, `PlanActivatedEvent`, `PlanDeactivatedEvent`) now create and dispatch system notifications to resolved plan admins (SuperAdmin + OrganizationAdmin, with recipient deduplication).
- OrganizationAdmin recipients are now gated by a feature flag (`Notifications:PlanLifecycle:IncludeOrganizationAdmins`) with safe default enabled.

### Current flow
1. Quota event handled in `SubscriptionQuotaAlertDomainEventHandler`.
2. Alert persisted.
3. `AlertNotificationPublisher` creates `Notification` entries with `NotificationCategory.Alert`.
4. Dispatcher sends through channels (including email when policy allows).

## 4) Email Integration Status

### Improvements applied
- Notification email channel now chooses template by category:
  - Alert/Security -> security alert template
  - Non-security notifications -> new platform notification template
- Added a dedicated service method for non-security notification emails.
- Added a new styled `BuildPlatformNotificationTemplate` for polished non-security email rendering.

### Result
- Billing/system notifications no longer appear as security incidents in email.
- Security alerts keep high-visibility incident styling.

## 5) Tests and Validation

Executed:
- `Trap-Intel.Tests/Subscriptions/SubscriptionQuotaAlertDomainEventHandlerTests.cs` -> passed
- `Trap-Intel.Tests/Plans/PlanLifecycleDomainEventNotificationHandlerTests.cs` -> passed
- Targeted plan/subscription integration tests -> passed
- Full test suite -> 187 passed, 0 failed

## 6) Remaining Gaps

- No blocking gaps found in Plan/Quota -> Notification -> Email wiring for audited scope.
- Optional enhancement: if product policy changes, expand recipients for plan lifecycle events beyond admin scopes (for example, Operations analysts) with explicit opt-in policy.
- Operator control is now available to disable OrganizationAdmin plan lifecycle recipients without code changes.

## 7) Final Verdict
- Plans/Quota endpoint surface is valid for this project and actively used.
- Notifications + emails are now correctly wired for quota warning/exceeded/enforcement paths.
- Email quality is improved with a dedicated non-security styled template.
- Suite is stable after changes (all tests green).
