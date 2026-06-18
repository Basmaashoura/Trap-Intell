# Phase 1: Authentication & Onboarding UI Screens

> **Target**: Frontend Team  
> **Backend Domain**: `Identity`, `Organizations`, `Invitations`  
> **Priority**: Critical - Required First  
> **Domain Aggregates**: `User`, `Organization`, `OrganizationInvitation`  
> **Key Services**: `OrganizationApprovalService`, `UserPasswordResetService`, `InvitationService`

---

## 1.1 Login Screen

### Purpose
Allow existing users to authenticate and access the platform.

### Screen Elements

| Element | Type | Description | Validation |
|---------|------|-------------|------------|
| Email | Input (email) | User's registered email | Required, valid email format |
| Password | Input (password) | User's password | Required, min 8 chars |
| Remember Me | Checkbox | Keep session alive | Optional |
| Login Button | Button (primary) | Submit credentials | Disabled until valid |
| Forgot Password | Link | Navigate to password reset | - |
| Sign Up | Link | Navigate to registration | - |
| SSO Options | Buttons | Google, Microsoft, SAML | Optional based on org settings |

### States
- **Default**: Empty form
- **Loading**: Show spinner on button
- **Error**: Display validation/auth errors
- **Success**: Redirect to dashboard
- **Locked Out**: Show after 5 failed attempts (auto-suspend feature)

### API Endpoints
```
POST /api/auth/login
Body: { email, password, rememberMe }
Response: { token, user, organization }
```

### Backend Reference
- `User.RecordSuccessfulLogin()` - Updates `LastLoginAt`
- `User.RecordFailedLogin()` - Tracks consecutive failures, auto-suspends after 5

### Error Messages
| Code | Message |
|------|---------|
| `Identity.InvalidCredentials` | Invalid email or password |
| `Identity.UserNotActive` | Account is not active |
| `Identity.UserSuspended` | Account has been suspended |
| `Identity.AccountLocked` | Too many failed attempts |

---

## 1.2 Registration Screen

### Purpose
Allow new users to create an account and organization.

### Screen Elements

| Element | Type | Description | Validation |
|---------|------|-------------|------------|
| **Personal Info** | | | |
| First Name | Input | User's first name | Required, max 50 chars |
| Last Name | Input | User's last name | Required, max 50 chars |
| Email | Input (email) | Work email | Required, valid email, unique |
| Password | Input (password) | Create password | Required, min 8 chars, complexity rules |
| Confirm Password | Input (password) | Confirm password | Must match password |
| **Organization Info** | | | |
| Organization Name | Input | Company name | Required, min 2 chars |
| Industry | Select | Industry sector | Required |
| Company Size | Select | Employee count range | Required |
| Website | Input (url) | Company website | Required, valid URL |
| **Contact Info** | | | |
| Phone Number | Input (tel) | Contact phone | Optional |
| **Legal** | | | |
| Terms Checkbox | Checkbox | Accept Terms of Service | Required |
| Privacy Checkbox | Checkbox | Accept Privacy Policy | Required |

### Industry Options (from domain)
- Technology
- Finance
- Healthcare
- Government
- Education
- Manufacturing
- Retail
- Other

### Company Size Options
- 1-10 employees
- 11-50 employees
- 51-200 employees
- 201-1000 employees
- 1000+ employees

### Registration Flow
```
1. User fills form ? 
2. Create Organization (status: PendingApproval) ?
3. Create User (status: PendingActivation) ?
4. Send verification email ?
5. Show "Check your email" screen
```

### API Endpoints
```
POST /api/auth/register
Body: {
  firstName, lastName, email, password,
  organization: { name, industry, size, website, domain }
}
Response: { message: "Verification email sent" }
```

### Backend Reference
- `Organization.Create()` - Creates org with `PendingApproval` status
- `User.Create()` - Creates user with `PendingActivation` status
- `UserCreatedEvent` - Triggers welcome email

---

## 1.3 Email Verification Screen

### Purpose
Verify user's email address after registration.

### Screen Elements
| Element | Type | Description |
|---------|------|-------------|
| Status Icon | Icon | Check/X based on verification |
| Status Message | Text | Verification result |
| Resend Link | Link | Resend verification email |
| Login Button | Button | Navigate to login |

### States
- **Verifying**: Show spinner
- **Success**: Show success message, redirect to login
- **Expired**: Link expired, offer resend
- **Invalid**: Invalid token

### API Endpoint
```
POST /api/auth/verify-email
Body: { token }
Response: { success: true }
```

### Backend Reference
- `User.Activate()` - Changes status from `PendingActivation` to `Active`
- `UserActivatedEvent` - Raised on success

---

## 1.4 Forgot Password Screen

### Purpose
Allow users to request password reset.

### Screen Elements
| Element | Type | Description | Validation |
|---------|------|-------------|------------|
| Email | Input (email) | Registered email | Required, valid email |
| Submit Button | Button | Send reset link | - |
| Back to Login | Link | Return to login | - |

### API Endpoint
```
POST /api/auth/forgot-password
Body: { email }
Response: { message: "If account exists, email sent" }
```

---

## 1.5 Reset Password Screen

### Purpose
Allow users to set a new password via email link.

### Screen Elements
| Element | Type | Description | Validation |
|---------|------|-------------|------------|
| New Password | Input (password) | New password | Required, complexity rules |
| Confirm Password | Input (password) | Confirm new password | Must match |
| Reset Button | Button | Submit new password | - |

### Password Complexity Rules
- Minimum 8 characters
- At least 1 uppercase letter
- At least 1 lowercase letter
- At least 1 number
- At least 1 special character

### API Endpoint
```
POST /api/auth/reset-password
Body: { token, newPassword }
Response: { success: true }
```

---

## 1.6 Organization Pending Approval Screen

### Purpose
Inform users their organization is awaiting admin approval.

### Screen Elements
| Element | Type | Description |
|---------|------|-------------|
| Status Icon | Icon | Hourglass/clock icon |
| Title | Heading | "Pending Approval" |
| Message | Text | Explanation of approval process |
| Organization Name | Text | Display org name |
| Submitted Date | Text | When registration was submitted |
| Contact Support | Link | Support contact |

### When to Show
- After login if `Organization.Status == PendingApproval`

### Backend Reference
- `OrganizationStatus.PendingApproval` - Default status after registration
- `Organization.Approve()` - Admin action to activate

---

## 1.7 Invitation Accept Screen

### Purpose
Allow invited users to join an existing organization.

### Screen Elements
| Element | Type | Description | Validation |
|---------|------|-------------|------------|
| Organization Name | Text (readonly) | Inviting organization | - |
| Role | Text (readonly) | Assigned role | - |
| Invited By | Text (readonly) | Who sent invitation | - |
| First Name | Input | User's first name | Required |
| Last Name | Input | User's last name | Required |
| Password | Input (password) | Create password | Required |
| Accept Button | Button | Accept invitation | - |
| Decline Link | Link | Decline invitation | - |

### Invitation Status Values (from InvitationEnums)
- `Pending` - Awaiting response
- `Accepted` - User accepted
- `Declined` - User declined
- `Expired` - Past expiration date
- `Revoked` - Admin cancelled

### API Endpoints
```
GET /api/invitations/{token}
Response: { organizationName, role, invitedBy, email, expiresAt }

POST /api/invitations/{token}/accept
Body: { firstName, lastName, password }
Response: { token, user }
```

### Backend Reference
- `OrganizationInvitation` aggregate
- `InvitationAcceptedEvent` - Raised on acceptance
- `User.Create()` with role from invitation

---

## 1.8 Account Suspended Screen

### Purpose
Inform users their account has been suspended.

### Screen Elements
| Element | Type | Description |
|---------|------|-------------|
| Status Icon | Icon | Warning/lock icon |
| Title | Heading | "Account Suspended" |
| Message | Text | Reason for suspension |
| Contact Support | Button | Contact admin/support |

### When to Show
- After login if `User.Status == Suspended`

### Suspension Reasons (auto-suspension)
- Too many failed login attempts (brute force protection)
- Admin action (manual suspension)

### Backend Reference
- `User.Suspend()` - Sets status to `Suspended`
- `UserSuspendedEvent` - Contains reason
- `User.Unsuspend()` - Admin can restore access

---

## 1.9 Two-Factor Authentication (2FA) Screens

### 1.9.1 2FA Setup Screen
| Element | Type | Description |
|---------|------|-------------|
| QR Code | Image | TOTP setup QR code |
| Secret Key | Text | Manual entry key |
| Verification Code | Input | 6-digit code | 
| Enable Button | Button | Confirm 2FA setup |

### 1.9.2 2FA Verification Screen (Login)
| Element | Type | Description |
|---------|------|-------------|
| Verification Code | Input | 6-digit TOTP code |
| Verify Button | Button | Complete login |
| Use Backup Code | Link | Alternative method |

---

## Navigation After Authentication

### Based on User Status & Role
```
if (user.status == PendingActivation) ? Email Verification Screen
if (organization.status == PendingApproval) ? Pending Approval Screen
if (user.status == Suspended) ? Account Suspended Screen
if (user.role == SuperAdmin) ? Admin Dashboard
else ? Main Dashboard
```

### User Roles (from IdentityEnums)
| Role | Description | Access Level | Domain Value |
|------|-------------|--------------|---------------|
| `SuperAdmin` | System administrator | Full system access | `UserRole.SuperAdmin` |
| `OrganizationAdmin` | Organization owner | Full org access | `UserRole.OrganizationAdmin` |
| `SecurityAnalyst` | Threat analyst | Analysis features | `UserRole.SecurityAnalyst` |
| `OperationsAnalyst` | Operations/monitoring | Monitoring features | `UserRole.OperationsAnalyst` |
| `Viewer` | Read-only access | View only | `UserRole.Viewer` |
| `Guest` | Temporary access | Limited view | `UserRole.Guest` |

---

## UI/UX Guidelines for Phase 1

### Design Principles
1. **Clean & Professional** - Enterprise security product aesthetic
2. **Trust Indicators** - SSL badge, security certifications
3. **Accessibility** - WCAG 2.1 AA compliance
4. **Responsive** - Mobile-friendly authentication

### Color Scheme Suggestions
- Primary: Professional blue (#1a365d)
- Secondary: Security green for success (#38a169)
- Error: Alert red (#e53e3e)
- Background: Light gray (#f7fafc)

### Loading States
- Use skeleton loaders for forms
- Show progress indicators for multi-step processes
- Disable buttons during API calls

### Error Handling
- Display inline validation errors
- Show toast notifications for API errors
- Provide clear recovery actions

---

## Testing Checklist

- [ ] Login with valid credentials
- [ ] Login with invalid credentials (show error)
- [ ] Login lockout after 5 failures
- [ ] Registration with new organization
- [ ] Registration with duplicate email (show error)
- [ ] Email verification flow
- [ ] Password reset flow
- [ ] Invitation acceptance flow
- [ ] 2FA setup and verification
- [ ] Suspended account handling
- [ ] Pending approval screen
- [ ] Responsive design on mobile
- [ ] Accessibility testing

---

## Domain Reference Summary

### Aggregates & Services

| Type | Purpose | Key Properties |
|------|---------|----------------|
| `User` | User identity management | `Id`, `Email`, `FirstName`, `LastName`, `Role`, `Status`, `OrganizationId` |
| `Organization` | Organization management | `Id`, `Name`, `Code`, `Status`, `Industry`, `Size` |
| `OrganizationInvitation` | Team invitation management | `Id`, `Email`, `Role`, `Status`, `InvitedBy`, `ExpiresAt` |
| `OrganizationApprovalService` | Organization approval workflow | Methods: `ApproveOrganization()`, `RejectOrganization()` |
| `UserPasswordResetService` | Password reset workflow | Methods: `SendResetEmail()`, `ValidateResetToken()` |
| `InvitationService` | Invitation management | Methods: `SendInvitation()`, `AcceptInvitation()`, `DeclineInvitation()` |

### Key Value Objects

| Value Object | Purpose | Properties |
|--------------|---------|------------|
| `EmailAddress` | Email validation | `Value`, validation methods |
| `Password` | Password with hashing | `HashedValue`, complexity validation |
| `OrganizationCode` | Unique org identifier | `Value`, generated format |
| `UserProfile` | User profile data | `FirstName`, `LastName`, `JobTitle`, `PhoneNumber` |
| `OrganizationProfile` | Org profile data | `Name`, `Industry`, `Size`, `Website`, `Address` |
| `InvitationToken` | Secure invitation token | `Value`, `ExpiresAt` |

### Domain Events

| Event | Trigger |
|-------|--------|
| `UserRegisteredEvent` | New user registration |
| `UserActivatedEvent` | Email verification complete |
| `UserLoginSuccessEvent` | Successful login |
| `UserLoginFailedEvent` | Failed login attempt |
| `UserPasswordResetRequestedEvent` | Password reset requested |
| `UserPasswordChangedEvent` | Password updated |
| `User2FAEnabledEvent` | 2FA configured |
| `UserSuspendedEvent` | Account suspended |
| `OrganizationCreatedEvent` | New organization registered |
| `OrganizationApprovedEvent` | Organization approved by admin |
| `OrganizationRejectedEvent` | Organization rejected |
| `OrganizationInvitationSentEvent` | Team member invited |
| `OrganizationInvitationAcceptedEvent` | Invitation accepted |
| `OrganizationInvitationDeclinedEvent` | Invitation declined |
| `OrganizationInvitationExpiredEvent` | Invitation expired |

### User Status (from IdentityEnums)

| Status | Description | Domain Value |
|--------|-------------|---------------|
| `PendingEmailVerification` | Email not verified | `UserStatus.PendingEmailVerification` |
| `Active` | Account active | `UserStatus.Active` |
| `Suspended` | Account suspended | `UserStatus.Suspended` |
| `Locked` | Account locked (failed logins) | `UserStatus.Locked` |
| `Deleted` | Account deleted | `UserStatus.Deleted` |

### Organization Status (from OrganizationEnums)

| Status | Description | Domain Value |
|--------|-------------|---------------|
| `PendingApproval` | Awaiting admin approval | `OrganizationStatus.PendingApproval` |
| `Active` | Organization active | `OrganizationStatus.Active` |
| `Suspended` | Organization suspended | `OrganizationStatus.Suspended` |
| `Deleted` | Organization deleted | `OrganizationStatus.Deleted` |

### Invitation Status (from InvitationEnums)

| Status | Description | Domain Value |
|--------|-------------|---------------|
| `Pending` | Invitation sent, awaiting response | `InvitationStatus.Pending` |
| `Accepted` | Invitation accepted | `InvitationStatus.Accepted` |
| `Declined` | Invitation declined | `InvitationStatus.Declined` |
| `Expired` | Invitation expired | `InvitationStatus.Expired` |
| `Cancelled` | Invitation cancelled | `InvitationStatus.Cancelled` |

### Industry Types (from OrganizationEnums)

| Industry | Domain Value |
|----------|---------------|
| `Technology` | `Industry.Technology` |
| `Finance` | `Industry.Finance` |
| `Healthcare` | `Industry.Healthcare` |
| `Government` | `Industry.Government` |
| `Education` | `Industry.Education` |
| `Manufacturing` | `Industry.Manufacturing` |
| `Retail` | `Industry.Retail` |
| `Other` | `Industry.Other` |

### Company Size (from OrganizationEnums)

| Size | Domain Value |
|------|---------------|
| `1-10 employees` | `CompanySize.Micro` |
| `11-50 employees` | `CompanySize.Small` |
| `51-200 employees` | `CompanySize.Medium` |
| `201-1000 employees` | `CompanySize.Large` |
| `1000+ employees` | `CompanySize.Enterprise` |

### Business Rules & Policies

| Rule | Description |
|------|-------------|
| `PasswordComplexity` | Min 8 chars, uppercase, lowercase, number, special char |
| `EmailUniqueness` | Email must be unique across all users |
| `InvitationExpiry` | Invitations expire after 7 days |
| `AutoSuspension` | Lock account after 5 failed login attempts |
| `OrganizationApproval` | New orgs require admin approval |
| `RoleAssignment` | First user becomes OrganizationAdmin |
| `2FAEnforcement` | Can be required org-wide |
| `SessionTimeout` | Configurable session timeout |

### Repository Interfaces

| Repository | Purpose |
|------------|--------|
| `IUserRepository` | User CRUD operations |
| `IOrganizationRepository` | Organization CRUD operations |
| `IOrganizationInvitationRepository` | Invitation management |

### Authentication Flow States

```
1. Registration ? PendingEmailVerification
2. Email Verification ? Active (if org approved) or PendingApproval
3. Login ? Check User.Status and Organization.Status
4. Route to appropriate screen based on status
```

### Error Codes & Messages

| Error Code | UI Message |
|------------|------------|
| `Identity.InvalidCredentials` | Invalid email or password |
| `Identity.UserNotActive` | Please verify your email address |
| `Identity.UserSuspended` | Account suspended. Contact support. |
| `Identity.AccountLocked` | Too many failed attempts. Try again later. |
| `Identity.EmailAlreadyExists` | Account with this email already exists |
| `Organizations.DuplicateName` | Organization name already exists |
| `Invitations.InvalidToken` | Invitation link is invalid or expired |
| `Invitations.AlreadyAccepted` | This invitation has already been used |
