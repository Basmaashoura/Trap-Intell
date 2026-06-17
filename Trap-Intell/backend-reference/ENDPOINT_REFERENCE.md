# Trap-Intel Backend API Endpoint Reference

This directory contains C# .NET endpoint definitions from the Trap-Intel backend API. These are for reference only and document the API contracts for frontend integration.

## Directory Structure

```
Endpoints/
├── IEndpoint.cs                 # Base endpoint interface
├── Auth/                        # Authentication endpoints
│   ├── LoginEndpoint.cs
│   ├── RegisterEndpoint.cs
│   ├── TokenEndpoints.cs
│   ├── EmailVerificationEndpoints.cs
│   ├── TwoFactorEndpoints.cs
│   ├── PasswordEndpoints.cs
│   ├── ProfileEndpoints.cs
│   └── AuthHelpers.cs
├── Alerts/                      # Alert management endpoints
│   ├── AlertQueryEndpoints.cs
│   ├── AlertActionEndpoints.cs
│   ├── AssignAlertEndpoint.cs
│   └── SnoozeAlertEndpoints.cs
├── Attacks/                     # Attack event endpoints
│   └── AttackEventIngestionEndpoints.cs
├── Organizations/              # Organization management
├── Users/                       # User management
├── AuditLogs/                   # Audit and compliance logging
├── Notifications/               # Notification system
├── Plans/                       # Billing plans
├── PaymentMethods/              # Payment processing
├── Subscriptions/               # Subscription management
├── Invoices/                    # Invoice generation
├── Profiles/                    # User profiles
├── Roles/                       # Role-based access control
├── Honeypots/                   # Honeypot management
└── Admin/                       # Admin operations
```

## Key Endpoint Groups

### Authentication (/api/auth)

- `POST /login` - User authentication
- `POST /register` - New user registration
- `POST /refresh` - Refresh access token
- `POST /logout` - Logout user
- `GET /sessions` - Get active sessions

### Alerts (/api/organizations/{organizationId}/alerts)

- `GET /` - List and filter alerts
- `GET /{alertId}` - Get alert details
- `GET /dashboard` - Get dashboard statistics
- `PUT /{alertId}/acknowledge` - Acknowledge alert
- `PUT /{alertId}/resolve` - Resolve alert
- `PUT /{alertId}/assign` - Assign alert to user
- `POST /{alertId}/snooze` - Snooze alert

### Attacks (/api/organizations/{organizationId}/honeypots/{honeypotId}/attacks)

- `POST /ingest` - Ingest attack event from honeypot

### Organizations (/api/organizations)

- Tenant/organization management
- User invitations
- Organization status and settings

### Users (/api/users)

- User management
- Role assignments
- User status and permissions

### Audit Logs (/api/organizations/{organizationId}/auditlogs)

- Query audit trails
- Export audit logs
- Verify audit integrity
- Tag audit logs for compliance

### Notifications (/api/notifications)

- Get notifications
- Mark as read
- Settings management
- Push token registration

### Plans (/api/plans)

- List billing plans
- Get plan details
- Manage plan lifecycle

### Subscriptions (/api/organizations/{organizationId}/subscriptions)

- Create subscriptions
- Manage subscription status
- Quota management

### Invoices (/api/organizations/{organizationId}/invoices)

- List and retrieve invoices
- Issue invoices
- Export PDFs
- Process payments

### Profiles (/api/profile)

- User profile management
- Avatar/cover uploads
- Organization branding

### Honeypots (/api/organizations/{organizationId}/honeypots)

- Deploy honeypots
- Manage honeypot lifecycle (pause, resume, terminate)
- Monitor activity

## Authorization

Most endpoints require:

- Bearer token authentication
- Organization membership
- Appropriate role-based permissions
- Rate limiting on auth endpoints

## Response Format

All endpoints follow RFC 7807 Problem Details for error responses:

```json
{
  "type": "about:blank",
  "title": "Bad Request",
  "status": 400,
  "detail": "Validation failed",
  "instance": "/path/to/request"
}
```

Successful responses vary by endpoint but typically include:

- Paged results with `totalCount`, `pageNumber`, `pageSize`
- DTOs with relevant entity data
- Timestamps in ISO 8601 format

## Integration Notes for Frontend

1. **Authentication Flow**: Login → Receive tokens → Use Bearer token for subsequent requests
2. **Token Refresh**: Implement automatic token refresh using refresh endpoint
3. **Organization Scoping**: Include `organizationId` in URL params for tenant-isolated endpoints
4. **Rate Limiting**: Implement exponential backoff for 429 responses
5. **Pagination**: Use `pageNumber` and `pageSize` query params for list endpoints
6. **Error Handling**: Check HTTP status codes and ProblemDetails response body

## Testing

The backend includes comprehensive integration tests in `Trap-Intel.Tests/Integration/` for each endpoint group.
