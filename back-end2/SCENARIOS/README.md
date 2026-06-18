# Scenarios Organization

This folder is the organized entrypoint for scenario execution.

## Structure

- `audit/` - audit-focused checks and future audit scenarios.
- `notifications/` - notifications, push token, and notification settings scenarios.
- `organizations/` - invitation, onboarding, and organization member lifecycle scenarios.
- `sweeps/` - broad API endpoint sweep and smoke coverage scripts.
- `results/` - recommended output location for scenario reports and artifacts.

## Run Examples

From repository root:

```powershell
# Full endpoint sweep
./SCENARIOS/sweeps/run-endpoint-sweep.ps1

# Admin + guest + mailpit E2E
./SCENARIOS/organizations/run-admin-guest-mailpit.ps1

# Invitation (new flow) + 2FA E2E
./SCENARIOS/organizations/run-invitation-new-2fa.ps1

# Notification mega scenarios
./SCENARIOS/notifications/run-notification-mega.ps1

# Notification settings smoke
./SCENARIOS/notifications/run-notification-settings-smoke.ps1

# Alert notification smoke probe
./SCENARIOS/notifications/run-alert-notification-smoke.ps1

# Push token register/delete roundtrip
./SCENARIOS/notifications/run-push-token-roundtrip.ps1
```

## Notes

- Wrapper scripts are used to centralize scenario entrypoints while keeping existing root scripts backward-compatible.
- Some notification probes are fully local scripts in this folder (`alert_notification_smoke.ps1`, `push_token_roundtrip.ps1`).
- New scenario outputs should be placed under `SCENARIOS/results/`.
