# Audit and Logging Gap Report (2026-04-03)

## Scope
- Domain model review for auditing and logging.
- API endpoint exposure and runtime behavior review.
- Application/Infrastructure wiring review.
- Existing execution evidence review from endpoint sweep and logs.

## What Exists (Implemented)
- Domain auditing model exists with aggregate, events, enums, and repository contract:
  - Trap-Intel.Domain/Auditing/AuditTrail.cs
  - Trap-Intel.Domain/Auditing/Events/AuditingEvents.cs
  - Trap-Intel.Domain/Auditing/IAuditTrailRepository.cs
- Application auditing layer exists (commands, queries, handlers, DTOs):
  - Trap-intell.Application/Auditing/**
- Infrastructure auditing exists:
  - Repository: Trap-Intel.Infrastructure/Auditing/Repositories/AuditTrailRepository.cs
  - Services: Trap-Intel.Infrastructure/Auditing/Services/*
  - Middleware: Trap-Intel.Infrastructure/Auditing/Middlewares/AuditLoggingMiddleware.cs
  - Save interceptor: Trap-Intel.Infrastructure/Auditing/Interceptors/AuditingSaveInterceptor.cs
  - Cleanup background service: Trap-Intel.Infrastructure/Auditing/BackgroundServices/AuditCleanupBackgroundService.cs
- API audit endpoints are exposed and protected by auth:
  - 8 endpoints under Trap-Intel.Api/Endpoints/AuditLogs/*
- Logging stack is present:
  - Serilog host integration in Program.cs
  - Request logging middleware enabled
  - Seq configured in Docker + appsettings.Docker.json

## Runtime Evidence
- Endpoint sweep shows all audit endpoints reachable with expected status codes (401 unauth, 200/204 auth):
  - SCENARIOS/results/endpoint_sweep_results.json
- Endpoint sweep payload shows audit export returns rows including auto-audited records.
- Endpoint sweep verify endpoint shows tamper report result with high tampered count:
  - totalChecked=18, tamperedCount=12 in SCENARIOS/results/endpoint_sweep_results.json
- Historical API logs show migration and seeding of audit_trails table and indexes.

## Findings (Ordered by Severity)

### Critical 1: Domain events are collected but never published
- File: Trap-Intel.Infrastructure/Persistence/ApplicationDbContext.cs
- Observation:
  - SaveChangesAsync calls DispatchDomainEventsAsync.
  - DispatchDomainEventsAsync collects and clears events, but does not publish them to MediatR.
- Impact:
  - All domain event handlers are effectively dead code at runtime.
  - Critical flows such as:
    - CriticalAuditLogRecordedDomainEventHandler
    - AuditLogAcknowledgedDomainEventHandler
    - AuditRecordedDomainEventHandler
    will not run.

### Critical 2: Acknowledge/Tag command handlers do not persist changes
- Files:
  - Trap-intell.Application/Auditing/Commands/AcknowledgeAuditLog/AcknowledgeAuditLogCommandHandler.cs
  - Trap-intell.Application/Auditing/Commands/TagAuditLog/TagAuditLogCommandHandler.cs
- Observation:
  - Handlers call repository UpdateAsync and return success, but no IUnitOfWork.SaveChangesAsync call.
- Impact:
  - API can return 200/204 while DB state is not guaranteed to be updated.
  - This creates false-positive success behavior.

### Critical 3: Exception audit logging path is invalid by design
- Files:
  - Trap-Intel.Infrastructure/Auditing/Middlewares/AuditLoggingMiddleware.cs
  - Trap-Intel.Domain/Auditing/AuditTrail.cs
- Observation:
  - Middleware calls RecordAsync with resourceId = Guid.Empty.
  - Domain Create() rejects empty resourceId.
- Impact:
  - Unhandled exceptions intercepted by middleware are likely not recorded into audit_trails.

### High 1: Integrity verification currently reports many records as tampered
- Evidence:
  - SCENARIOS/results/endpoint_sweep_results.json verify endpoint result: tamperedCount=12/18.
- Files:
  - Trap-Intel.Domain/Auditing/AuditTrail.cs (ComputeHash)
- Likely causes:
  - Hash payload relies on DateTime string representation that may not round-trip exactly after DB persistence precision normalization.
  - Hash strategy is not canonicalized against DB serialization precision.
- Impact:
  - Integrity check yields noisy/false-positive tamper alarms.

### High 2: Save interceptor cannot attribute user identity
- File: Trap-Intel.Infrastructure/Auditing/Interceptors/AuditingSaveInterceptor.cs
- Observation:
  - GetPotentialUserId() returns null always.
- Impact:
  - Auto-generated audit logs lose actor attribution (UserId null), weakening forensic value.

### High 3: Event handler condition logic appears inverted for critical notifications
- File: Trap-intell.Application/Auditing/Events/AuditRecordedDomainEventHandler.cs
- Observation:
  - Condition uses: severity != Critical && action == Delete, then logs as "CRITICAL AUDIT EVENT DETECTED".
- Impact:
  - Notification behavior is inconsistent with message intent and expected criticality semantics.

### Medium 1: Audit hash does not include important mutable fields
- File: Trap-Intel.Domain/Auditing/AuditTrail.cs
- Observation:
  - ComputeHash includes core scalar fields, but excludes changes list, compliance tags, acknowledgement fields.
- Impact:
  - Tampering in excluded fields is not covered by integrity verification.

### Medium 2: Duplicate query models for change history increase maintenance risk
- Files:
  - Trap-intell.Application/Auditing/Queries/GetAuditLogChanges/*
  - Trap-intell.Application/Auditing/Queries/GetAuditTrailChanges/*
- Observation:
  - Two parallel query paths exist for similar behavior.
- Impact:
  - Increases drift risk and confusion.

### Medium 3: No dedicated validators for auditing commands/queries
- Observation:
  - No audit-specific FluentValidation validators found.
- Impact:
  - Guardrails for bounds/date/page/filter combinations are weaker than other modules.

## Coverage and Confidence
- Audit endpoints are exposed and respond.
- Basic endpoint-level E2E coverage exists.
- Deep behavior-level guarantees are incomplete for:
  - domain-event-driven flows,
  - persistence confirmation for acknowledge/tag,
  - integrity-check reliability.

## Recommended Priority Fix Plan
1. Fix domain event publishing in ApplicationDbContext (or dedicated dispatcher) before clearing events.
2. Add IUnitOfWork to AcknowledgeAuditLog and TagAuditLog handlers and persist updates explicitly.
3. Fix middleware exception audit record creation:
   - avoid Guid.Empty resourceId,
   - use a stable synthetic resource identity strategy.
4. Canonicalize integrity hash strategy (timestamp normalization + deterministic payload) and re-baseline existing hashes.
5. Wire current user context into AuditingSaveInterceptor for non-null actor attribution.
6. Correct AuditRecordedDomainEventHandler condition semantics.
7. Add audit validators and remove duplicate changes query model.

## Operational Note
- Endpoint-level success in sweeps does not prove data persistence unless state is validated after each mutating operation.
- Add post-condition checks for acknowledge/tag and integrity verification in the E2E suite.

## Addendum: Business Completion Enhancements Implemented (2026-04-03)

### 1) Advanced Audit Search Filters in `GetAuditLogs`
- Added support for these query parameters:
  - `ipAddress`
  - `startDate`
  - `endDate`
  - `standard` (compliance standard)
  - `includeArchived`
  - `isAcknowledged`
- `GetAuditLogsQueryHandler` now uses `IAuditTrailRepository.SearchAsync(...)` for unified filter behavior.
- Added date range guard:
  - returns validation failure if `endDate < startDate`.

### 2) Richer Audit DTO Payload
- `AuditTrailDto` now includes business-relevant fields:
  - `Reason`
  - `IsAcknowledged`
  - `IsArchived`
  - `AcknowledgedBy`
  - `AcknowledgedAt`
  - `ComplianceStandards`
- This enables SOC dashboards and admin UIs to render full audit context without extra round-trips.

### 3) Improved Acknowledge Endpoint Error Semantics
- Endpoint now maps domain errors to clearer HTTP behavior:
  - `Auditing.InvalidResourceId` -> `404 Not Found`
  - `Auditing.AlreadyAcknowledged` -> `409 Conflict`
- This prevents ambiguous `400` responses and improves client UX/workflow handling.

### 4) New Forensic Endpoint for Operational Monitoring
- Added:
  - `GET /api/organizations/{organizationId}/auditlogs/critical/unacknowledged-count`
- Backed by new application query:
  - `GetUnacknowledgedCriticalAuditCountQuery`
  - `GetUnacknowledgedCriticalAuditCountQueryHandler`
- Purpose: direct KPI feed for SOC dashboards, alerts, and runbooks.

### 5) Enhanced CSV Export for Compliance Teams
- Export now includes additional columns:
  - `AcknowledgedBy`
  - `AcknowledgedAt`
  - `AcknowledgeNotes`
  - `ComplianceStandards`
  - `ChangesCount`
- Added robust CSV escaping for string fields.

### 6) Search UX and Analytics Expansion
- `GetAuditLogs` now supports additional business filters and sorting:
  - `reasonContains`
  - `sortBy` (`Timestamp`, `Severity`, `Action`, `ResourceType`)
  - `sortDirection` (`Desc`, `Asc`)
- `isAcknowledged` filtering is now applied at repository query level before pagination.
- Added new analytics endpoint:
  - `GET /api/organizations/{organizationId}/auditlogs/summary`
- New summary endpoint supports:
  - optional `startDate` / `endDate`
  - optional `includeArchived`
  - configurable `top` buckets
- Summary response includes:
  - total, acknowledged, unacknowledged, archived counts
  - distribution by severity
  - top actions
  - top resource types

### Runtime Verification Summary
- Verified endpoints after rebuild:
  - `GET /health` -> `200`
  - Advanced filter query -> `200` with enriched payload fields
  - Unack critical count endpoint -> `200` with `{ "count": ... }`
  - Export endpoint returns CSV header containing new columns
- Verified acknowledge conflict handling:
  - re-acknowledging an already acknowledged record -> `409` with clear message.
