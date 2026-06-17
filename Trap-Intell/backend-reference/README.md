# Backend Endpoints Reference

This directory contains C# .NET endpoint definitions cloned from the Trap-Intel backend API repository. These files serve as API contracts and integration reference for your React frontend.

## Location

```
Trap-Intell/
└── backend-reference/
    └── Endpoints/
```

## Files Downloaded

### Core Interface

- **IEndpoint.cs** - Base interface all endpoints implement

### Endpoints by Module

#### Authentication (`Auth/`)

- `LoginEndpoint.cs` - User login with 2FA support
- `RegisterEndpoint.cs` - New user registration
- `TokenEndpoints.cs` - Token refresh, logout, session management
- `AuthHelpers.cs` - Auth utility functions
- Additional files in source: EmailVerificationEndpoints, TwoFactorEndpoints, ProfileEndpoints, PasswordEndpoints

#### Alerts (`Alerts/`)

- `AlertQueryEndpoints.cs` - List, filter, and get alert details
- `AlertActionEndpoints.cs` - Acknowledge and resolve alerts
- `AssignAlertEndpoint.cs` - Assign alerts to analysts
- `SnoozeAlertEndpoints.cs` - Snooze and unsnooze alerts

#### Attacks (`Attacks/`)

- `AttackEventIngestionEndpoints.cs` - Ingest honeypot attack events

#### Audit Logs (`AuditLogs/`)

- `GetAuditLogsEndpoint.cs` - Query audit logs with advanced filtering
- `VerifyAuditLogIntegrityEndpoint.cs` - Verify audit log integrity
- Additional files in source: ExportAuditLogs, TagAuditLogs, AcknowledgeAuditLog

#### Users (`Users/`)

- `GetUsersEndpoint.cs` - List and retrieve user details
- `UserManagementEndpoints.cs` - Change role, deactivate, suspend users

#### Plans (`Plans/`)

- `PlanManagementEndpoints.cs` - CRUD operations for billing plans

#### Subscriptions (`Subscriptions/`)

- `SubscriptionManagementEndpoints.cs` - Create, manage, and cancel subscriptions

#### Invoices (`Invoices/`)

- `InvoiceManagementEndpoints.cs` - List, issue, pay, cancel, and export invoices

#### Roles (`Roles/`)

- `RoleManagementEndpoints.cs` - Create roles and manage permissions
- `GetPermissionsEndpoint.cs` - List all available permissions

#### Honeypots (`Honeypots/`)

- `HoneypotLifecycleEndpoints.cs` - Deploy, pause, resume, terminate honeypots

#### Additional Categories (Not yet downloaded but available in source)

- Organizations/
- Notifications/
- PaymentMethods/
- Profiles/
- Admin/

## API Base URL

All endpoints use `/api/` prefix:

```
POST   /api/auth/login
GET    /api/users
GET    /api/organizations/{organizationId}/alerts
POST   /api/organizations/{organizationId}/honeypots
```

## Authentication

Most endpoints require:

- Bearer token in `Authorization` header
- Organization ID in URL path
- Role-based permissions

Example:

```javascript
fetch("https://api.example.com/api/users", {
  headers: {
    Authorization: "Bearer <access_token>",
    "Content-Type": "application/json",
  },
});
```

## Integration with Frontend

These endpoint files document:

1. **Request format** - Required/optional fields, query parameters
2. **Response format** - DTO structures, HTTP status codes
3. **Authorization** - Permission requirements
4. **Rate limiting** - Applied to auth endpoints

Use these as reference when:

- Building API client services
- Implementing authentication flows
- Handling errors and validation
- Planning data fetching logic

## Next Steps

1. Create API service layer (`src/services/api/`)
2. Implement axios or fetch client
3. Build type definitions matching endpoint DTOs
4. Handle authentication flow (login → token storage → request headers)
5. Implement error handling for different status codes

## Notes

- These are reference files (C# code, not executable in React)
- The actual backend API must be running to interact with these endpoints
- Some file implementations show only method signatures and comments
- Check GitHub repo for complete implementation details if needed
