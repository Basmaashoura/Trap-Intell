# Frontend API Endpoint Reference

## Purpose

This document is the frontend-oriented reference for all currently mapped HTTP endpoints in Trap-Intel API.

It is designed for:
- frontend engineers implementing data access and page flows,
- QA building test scenarios,
- product teams validating expected request and response behavior.

## Scope and Source of Truth

- API surface source: `Trap-Intel.Api/Endpoints/**/*.cs`.
- Style: Minimal APIs with route groups.
- Authorization style: mostly bearer-protected, with explicit anonymous endpoints for auth bootstrap flows.
- Organization isolation: endpoints with `{organizationId:guid}` are tenant-scoped and must match caller claims.

## Transport and Standards

- HTTP semantics: RFC 9110.
- Bearer token usage: RFC 6750.
- JSON format and UTF-8 defaults: RFC 8259.
- Recommended problem details model for errors: RFC 7807-compatible pattern.
- API security posture guidance: OWASP API Security Top 10.

## Base Conventions

### Base URL

- Local docker/dev example: `http://localhost:5000`
- Health endpoint: `GET /health`

### Required Headers

- `Authorization: Bearer <access-token>` for protected routes.
- `Content-Type: application/json` for JSON request bodies.
- `Accept: application/json` recommended on all API calls.

### Date/Time Format

- Use ISO 8601 UTC timestamps when sending date filters (for example: `2026-04-03T00:00:00Z`).

### Pagination

Common list endpoints support:
- `pageNumber` (1-based)
- `pageSize`

### Error Handling Contract (Frontend Recommendation)

Handle these categories consistently:
- `400`: validation/business rule failure, show actionable message.
- `401`: session missing/expired, trigger re-auth flow.
- `403`: authenticated but not allowed, show access denied view.
- `404`: resource not found or not visible in tenant context.
- `429`: rate-limited, retry with backoff and user feedback.
- `5xx`: server/transient failure, retry + observability logging.

## Authentication and Identity Endpoints

| Method | Route | Auth | Common Statuses | Notes |
|---|---|---|---|---|
| POST | /api/auth/register | Anonymous | 200, 400 | User registration |
| POST | /api/auth/login | Anonymous | 200, 400, 401, 429 | Login and potential 2FA challenge |
| POST | /api/auth/refresh | Anonymous | 200, 401, 403 | Refresh access token |
| POST | /api/auth/logout | Required | 200 | Revoke one refresh token/session |
| POST | /api/auth/logout-all | Required | 200 | Revoke all sessions |
| GET | /api/auth/sessions | Required | 200 | Active session insight |
| GET | /api/auth/me | Required | 200, 401 | Current user profile |
| PUT | /api/auth/me/profile | Required | 200, 400, 401 | Update profile |
| POST | /api/auth/validate-password | Anonymous | 200, 400 | Password policy pre-check |
| POST | /api/auth/forgot-password | Anonymous | 200 | Start reset flow |
| POST | /api/auth/validate-reset-token | Anonymous | 200, 400 | Validate reset token |
| POST | /api/auth/reset-password | Anonymous | 200, 400 | Complete password reset |
| POST | /api/auth/verify-email | Anonymous | 200, 400 | Verify email token |
| POST | /api/auth/resend-verification | Anonymous | 200 | Resend verification |

### Two-Factor Authentication

| Method | Route | Auth | Common Statuses | Notes |
|---|---|---|---|---|
| POST | /api/auth/2fa/setup | Required | 200, 400 | Begin TOTP setup |
| POST | /api/auth/2fa/confirm | Required | 200, 400 | Confirm setup using code |
| POST | /api/auth/2fa/verify | Anonymous | 200, 400 | Complete login challenge |
| POST | /api/auth/2fa/disable | Required | 200, 400 | Disable 2FA |
| POST | /api/auth/2fa/backup-codes/regenerate | Required | 200, 400 | Rotate backup codes |
| GET | /api/auth/2fa/status | Required | 200 | Read 2FA status |

## Admin Endpoints

| Method | Route | Auth | Common Statuses | Notes |
|---|---|---|---|---|
| GET | /api/admin/users | Required (permission gated) | 200, 403 | Admin users list |
| GET | /api/admin/users/{userId:guid} | Required (permission gated) | 200, 404 | User detail |
| POST | /api/admin/users/{userId:guid}/change-role | Required (permission gated) | 200, 400, 403 | Role reassignment |
| POST | /api/admin/users/{userId:guid}/deactivate | Required (permission gated) | 200, 400 | Deactivate user |
| POST | /api/admin/users/{userId:guid}/activate | Required (permission gated) | 200, 400 | Activate user |
| POST | /api/admin/users/{userId:guid}/unlock | Required (permission gated) | 200, 400 | Unlock account |
| GET | /api/admin/permissions/me | Required | 200 | Current effective permissions |
| GET | /api/admin/permissions/roles | Required (super-admin style) | 200 | Role-permission matrix |

## Organization and Invitations Endpoints

| Method | Route | Auth | Common Statuses | Notes |
|---|---|---|---|---|
| POST | /api/organizations/ | Required | 201, 400 | Create tenant/organization |
| GET | /api/organizations/{organizationId:guid}/status | Required | 200, 401, 403, 404 | Read org status |
| PUT | /api/organizations/{organizationId:guid}/status | Required | 200, 400, 401, 403, 404 | Update org status |
| POST | /api/organizations/{organizationId:guid}/invitations | Required | 200, 400 | Invite user |
| GET | /api/organizations/{organizationId:guid}/invitations | Required | 200, 400 | Query param: `status` |
| POST | /api/organizations/{organizationId:guid}/invitations/{invitationId:guid}/resend | Required | 200, 400, 404 | Resend invite |
| DELETE | /api/organizations/{organizationId:guid}/invitations/{invitationId:guid} | Required | 200, 400, 404 | Revoke invite |
| POST | /api/organizations/invitations/accept | Anonymous | 200, 400 | Accept invitation token |

## Roles and Permissions Endpoints

| Method | Route | Auth | Common Statuses | Notes |
|---|---|---|---|---|
| GET | /api/roles/ | Required | 200, 401 | System roles |
| GET | /api/roles/permissions | Required | 200, 401 | Permission catalog |

## Users Endpoints

| Method | Route | Auth | Common Statuses | Notes |
|---|---|---|---|---|
| GET | /api/organizations/{organizationId:guid}/users/ | Required | 200, 401, 403 | Organization users |
| GET | /api/users/{userId:guid} | Required | 200, 401, 404 | User by id |
| POST | /api/users/{userId:guid}/deactivate | Required | 200, 400, 404 | Deactivate |
| PUT | /api/users/{userId:guid}/role | Required | 200, 400, 404 | Change role |
| POST | /api/users/{userId:guid}/suspend | Required | 200, 400, 404 | Suspend |
| POST | /api/users/{userId:guid}/unsuspend | Required | 200, 400, 404 | Unsuspend |

## Alerts Endpoints

| Method | Route | Auth | Common Statuses | Query Parameters |
|---|---|---|---|---|
| GET | /api/organizations/{organizationId:guid}/alerts/ | Required | 200, 401, 403 | `status`, `severity`, `type`, `assignedUserId`, `pageNumber`, `pageSize` |
| GET | /api/organizations/{organizationId:guid}/alerts/{id:guid} | Required | 200, 401, 403, 404 | — |
| GET | /api/organizations/{organizationId:guid}/alerts/dashboard | Required | 200, 401, 403 | `lastNDays` |
| PUT | /api/organizations/{organizationId:guid}/alerts/{alertId:guid}/acknowledge | Required | 200, 400, 404 | — |
| PUT | /api/organizations/{organizationId:guid}/alerts/{alertId:guid}/resolve | Required | 200, 400, 404 | — |
| PUT | /api/organizations/{organizationId:guid}/alerts/{alertId:guid}/assign | Required | 200, 400, 404 | `targetUserId` |
| PUT | /api/organizations/{organizationId:guid}/alerts/{alertId:guid}/snooze | Required | 200, 400, 404 | `minutes`, `reason` |
| PUT | /api/organizations/{organizationId:guid}/alerts/{alertId:guid}/unsnooze | Required | 200, 400, 404 | — |

## Audit Logs Endpoints

| Method | Route | Auth | Common Statuses | Query Parameters |
|---|---|---|---|---|
| GET | /api/organizations/{organizationId:guid}/auditlogs/ | Required | 200, 400, 401, 403 | `pageNumber`, `pageSize`, `action`, `resourceType`, `severity`, `userId`, `ipAddress`, `startDate`, `endDate`, `standard`, `includeArchived`, `isAcknowledged`, `reasonContains`, `sortBy`, `sortDirection` |
| GET | /api/organizations/{organizationId:guid}/auditlogs/export/ | Required | 200, 400, 401, 403 | `userId`, `action`, `resourceType`, `severity`, `ipAddress`, `startDate`, `endDate`, `standard`, `includeArchived` |
| POST | /api/organizations/{organizationId:guid}/auditlogs/{id:guid}/acknowledge/ | Required | 204, 400, 401, 403, 404 | request body supports notes |
| GET | /api/organizations/{organizationId:guid}/auditlogs/verify/ | Required | 200, 400, 401, 403 | `startDate`, `endDate` |
| GET | /api/organizations/{organizationId:guid}/auditlogs/dashboard/ | Required | 200, 401, 403 | `lastNDays` |
| POST | /api/organizations/{organizationId:guid}/auditlogs/{auditTrailId:guid}/tags | Required | 200, 400, 404 | `standard` |
| GET | /api/organizations/{organizationId:guid}/auditlogs/critical | Required | 200, 401, 403 | `pageNumber`, `pageSize` |
| GET | /api/organizations/{organizationId:guid}/auditlogs/critical/unacknowledged-count | Required | 200, 401, 403 | — |
| GET | /api/organizations/{organizationId:guid}/auditlogs/summary | Required | 200, 400, 401, 403 | `startDate`, `endDate`, `includeArchived`, `top` |
| GET | /api/organizations/{organizationId:guid}/auditlogs/{auditTrailId:guid}/changes | Required | 200, 401, 403, 404 | — |

## Notifications Endpoints

| Method | Route | Auth | Common Statuses | Query Parameters |
|---|---|---|---|---|
| GET | /api/notifications/ | Required | 200 | `pageNumber`, `pageSize`, `unreadOnly` |
| GET | /api/notifications/unread-count | Required | 200 | — |
| PUT | /api/notifications/{notificationId:guid}/read | Required | 200, 400, 404 | — |
| PUT | /api/notifications/read-all | Required | 200, 400 | — |
| PUT | /api/notifications/settings/ | Required | 200, 400 | large settings payload |
| POST | /api/notifications/push-tokens/ | Required | 200, 400 | — |
| DELETE | /api/notifications/push-tokens/{token} | Required | 200, 404 | token in path |
| DELETE | /api/notifications/push-tokens/ | Required | 200, 400, 404 | `token` |
| POST | /api/notifications/debug/send-self | Required | 200, 400, 401, 404 | development/debug use |
| POST | /api/notifications/debug/send-self-standard | Required | 200, 400, 401, 404 | debug with `type`, `title`, `message`, `category`, `priority` |

## Honeypot Lifecycle Endpoints

| Method | Route | Auth | Common Statuses | Query Parameters |
|---|---|---|---|---|
| POST | /api/organizations/{organizationId:guid}/honeypots/ | Required | 200, 400, 403 | request body deployment data |
| PUT | /api/organizations/{organizationId:guid}/honeypots/{honeypotId:guid}/pause | Required | 200, 400, 404 | `reason` |
| PUT | /api/organizations/{organizationId:guid}/honeypots/{honeypotId:guid}/resume | Required | 200, 400, 404 | `reason` |
| PUT | /api/organizations/{organizationId:guid}/honeypots/{honeypotId:guid}/terminate | Required | 200, 400, 404 | `reason` |

## Frontend Integration Patterns

### Auth-aware Fetch Wrapper

- Attach bearer token automatically.
- Detect `401` and route to refresh/login workflow.
- Detect `403` and show permission-aware empty state.
- Include request correlation id in client logs when available.

### List Page Defaults

- Default `pageNumber=1`.
- Default `pageSize` per module (for example 20 for logs, 10 for cards).
- Preserve filters in URL query state for shareable deep links.

### Mutations

- Optimistic UI only for idempotent, low-risk actions (for example mark notification read).
- Use pessimistic update for security-critical actions (role change, deactivate user, terminate honeypot).

### Retry Strategy

- Retry transient failures (`429`, occasional `5xx`) with capped exponential backoff.
- Do not retry deterministic validation failures (`400`) without user edits.

## Known Module Notes

- Auth endpoints include rate limiting behavior; UI should gracefully handle `429`.
- Invitation acceptance and 2FA verify are intentionally anonymous because they are token/challenge-based transitions.
- Notification debug endpoints are not production UX endpoints; gate these in non-production builds.

## Change Management

When backend endpoint contracts change:
- update this file,
- update frontend API client typings,
- update e2e scenarios under `SCENARIOS/`.
