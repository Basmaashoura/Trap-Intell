# ?? Identity & Access Management (IAM) Master Plan
## Trap-Intel Platform - Enterprise Security Implementation

> **Version**: 1.0  
> **Created**: 2024  
> **Status**: Planning Phase  
> **Estimated Total Duration**: 4-5 Weeks  
> **Architecture**: Hybrid Approach (Custom Domain + Microsoft Identity Services)

---

# ?? Table of Contents

1. [Executive Summary](#executive-summary)
2. [Current State Analysis](#current-state-analysis)
3. [Architecture Overview](#architecture-overview)
4. [Phase 1: Core Authentication Foundation](#phase-1-core-authentication-foundation)
5. [Phase 2: Token Management System](#phase-2-token-management-system)
6. [Phase 3: Security Hardening](#phase-3-security-hardening)
7. [Phase 4: Advanced Features](#phase-4-advanced-features)
8. [Phase 5: External Authentication (SSO)](#phase-5-external-authentication-sso)
9. [Phase 6: API Security & Rate Limiting](#phase-6-api-security--rate-limiting)
10. [Phase 7: Monitoring & Compliance](#phase-7-monitoring--compliance)
11. [Security Considerations](#security-considerations)
12. [Non-Functional Requirements](#non-functional-requirements)

---

# Executive Summary

## ?? Goal
Build a **production-grade, enterprise-level** Identity & Access Management system for Trap-Intel that is:
- **Secure**: Following OWASP, NIST, and industry best practices
- **Scalable**: Supports thousands of users and organizations
- **Compliant**: SOC2, GDPR, HIPAA ready
- **Performant**: Sub-100ms authentication response times

## ??? Architecture Decision

**Chosen Approach: Hybrid**

```
???????????????????????????????????????????????????????????????????????
?                         TRAP-INTEL IAM                              ?
???????????????????????????????????????????????????????????????????????
?                                                                     ?
?  ????????????????????????????????????????????????????????????????  ?
?  ?                    YOUR DOMAIN (Keep As-Is)                   ?  ?
?  ?  • User Entity (OrganizationId, Role, Preferences, etc.)     ?  ?
?  ?  • Organization Entity (Multi-tenant)                         ?  ?
?  ?  • OrganizationInvitation                                     ?  ?
?  ?  • UserPermissionPolicy (RBAC)                                ?  ?
?  ?  • Domain Events (UserCreated, UserSuspended, etc.)           ?  ?
?  ????????????????????????????????????????????????????????????????  ?
?                              +                                      ?
?  ????????????????????????????????????????????????????????????????  ?
?  ?              NEW AUTHENTICATION LAYER                         ?  ?
?  ?  • PasswordHash (BCrypt via IPasswordHasher)                  ?  ?
?  ?  • RefreshToken Entity                                        ?  ?
?  ?  • UserSession Entity                                         ?  ?
?  ?  • EmailVerificationToken                                     ?  ?
?  ?  • PasswordResetToken                                         ?  ?
?  ?  • TwoFactorSecret                                            ?  ?
?  ????????????????????????????????????????????????????????????????  ?
?                              +                                      ?
?  ????????????????????????????????????????????????????????????????  ?
?  ?              INFRASTRUCTURE SERVICES                          ?  ?
?  ?  • JwtTokenService                                            ?  ?
?  ?  • RefreshTokenService                                        ?  ?
?  ?  • AuthenticationService                                      ?  ?
?  ?  • TwoFactorService                                           ?  ?
?  ?  • SessionManagementService                                   ?  ?
?  ????????????????????????????????????????????????????????????????  ?
?                                                                     ?
???????????????????????????????????????????????????????????????????????
```

---

# Current State Analysis

## ? What Exists (Excellent Foundation)

| Component | Status | Quality | Notes |
|-----------|--------|---------|-------|
| `User` Entity | ? | ????? | Rich DDD model, multi-tenant |
| `Organization` | ? | ????? | Complete with approval workflow |
| `OrganizationInvitation` | ? | ????? | Token-based, secure |
| `UserRole` (6 roles) | ? | ????? | RBAC implemented |
| `UserPermissionPolicy` | ? | ???? | Permission matrix |
| `UserPreferences` | ? | ????? | Language, timezone, etc. |
| `UserNotificationSettings` | ? | ????? | Granular settings |
| Login Tracking | ? | ???? | `RecordFailedLogin()`, `RecordSuccessfulLogin()` |
| Auto-Lockout | ? | ???? | After 5 failed attempts |
| Domain Events | ? | ????? | All events defined |
| `AuditTrail` | ? | ????? | Compliance ready |

## ? What's Missing (Critical)

| Component | Priority | Impact |
|-----------|----------|--------|
| Password Storage | ?? Critical | Cannot authenticate |
| Password Hashing | ?? Critical | Security vulnerability |
| JWT Access Token | ?? Critical | No API authentication |
| Refresh Token | ?? Critical | Session management |
| Token Rotation | ?? High | Security best practice |
| Email Verification | ?? High | Account security |
| Password Reset | ?? High | User experience |
| 2FA/MFA | ?? High | Enterprise requirement |
| Session Management | ?? High | Device tracking |
| External Login (SSO) | ?? Medium | Enterprise feature |
| Rate Limiting | ?? High | Brute force protection |

---

# Architecture Overview

## ??? Layer Architecture

```
???????????????????????????????????????????????????????????????????
?                          API Layer                               ?
?  • AuthController                                                ?
?  • TokenController                                               ?
?  • AccountController                                             ?
?  • Middleware (JWT Validation, Rate Limiting)                    ?
???????????????????????????????????????????????????????????????????
                                ?
                                ?
???????????????????????????????????????????????????????????????????
?                      Application Layer                           ?
?  • Commands: Login, Register, RefreshToken, ResetPassword, etc. ?
?  • Queries: GetCurrentUser, GetUserSessions, etc.               ?
?  • Handlers with MediatR                                        ?
???????????????????????????????????????????????????????????????????
                                ?
                                ?
???????????????????????????????????????????????????????????????????
?                       Domain Layer                               ?
?  • User (+ PasswordHash, TwoFactorEnabled)                      ?
?  • RefreshToken Entity                                          ?
?  • UserSession Entity                                           ?
?  • VerificationToken Entity                                     ?
?  • Domain Events                                                ?
?  • Security Policies                                            ?
???????????????????????????????????????????????????????????????????
                                ?
                                ?
???????????????????????????????????????????????????????????????????
?                    Infrastructure Layer                          ?
?  • IPasswordHasher<User> (Microsoft.AspNetCore.Identity)        ?
?  • IJwtTokenService                                             ?
?  • IRefreshTokenService                                         ?
?  • IAuthenticationService                                       ?
?  • ITwoFactorService                                            ?
?  • Repositories                                                 ?
?  • EF Configurations                                            ?
???????????????????????????????????????????????????????????????????
```

## ?? Token Architecture (Stateless + Minimal DB)

```
???????????????????????????????????????????????????????????????????
?                      ACCESS TOKEN (JWT)                          ?
?  ?????????????????????????????????????????????????????????????  ?
?  ? Header: { alg: "RS256", typ: "JWT" }                      ?  ?
?  ?????????????????????????????????????????????????????????????  ?
?  ? Payload:                                                   ?  ?
?  ?   sub: "user-guid"                                        ?  ?
?  ?   org: "organization-guid"                                ?  ?
?  ?   role: "OrganizationAdmin"                               ?  ?
?  ?   permissions: ["ViewHoneypots", "CreateReport", ...]     ?  ?
?  ?   jti: "unique-token-id"                                  ?  ?
?  ?   iat: 1234567890                                         ?  ?
?  ?   exp: 1234568790 (15 minutes)                            ?  ?
?  ?   iss: "trap-intel"                                       ?  ?
?  ?   aud: "trap-intel-api"                                   ?  ?
?  ?????????????????????????????????????????????????????????????  ?
?  • Lifetime: 15 minutes (short-lived)                           ?
?  • Storage: Client-side only (NOT in database)                  ?
?  • Signing: RS256 (asymmetric) for production                   ?
???????????????????????????????????????????????????????????????????

???????????????????????????????????????????????????????????????????
?                     REFRESH TOKEN                                ?
?  ?????????????????????????????????????????????????????????????  ?
?  ? Token: Secure random string (256-bit)                     ?  ?
?  ? Stored: SHA-256 hash in database                          ?  ?
?  ? Lifetime: 7 days (configurable)                           ?  ?
?  ? Rotation: New token on each use (old one invalidated)     ?  ?
?  ? Family: Token family ID for rotation detection            ?  ?
?  ?????????????????????????????????????????????????????????????  ?
?  • One-time use (invalidated after refresh)                     ?
?  • Rotation detection (reuse = revoke family)                   ?
?  • Device binding optional                                      ?
???????????????????????????????????????????????????????????????????
```

### ?? Important: Role NOT in Token

```
? WRONG: Store role in JWT and trust it forever
   Problem: Role changes won't reflect until token expires

? CORRECT: 
   Option A: Short-lived tokens (15 min) + role in token
   Option B: Store role version in token, validate on sensitive operations
   
We choose: Option A with critical operation validation
```

---

# Phase 1: Core Authentication Foundation

## ?? Duration: 5-7 Days

## ?? Objectives
- Add password storage to User
- Implement secure password hashing
- Create basic login/register flow
- Generate JWT access tokens

---

## 1.1 Domain Layer Changes

### 1.1.1 Update User Entity

**File**: `Trap-Intel.Domain/Identity/User.cs`

```csharp
// ADD these properties to User entity:

/// <summary>
/// BCrypt hashed password. Never store plain text.
/// </summary>
public string PasswordHash { get; private set; } = string.Empty;

/// <summary>
/// Security stamp for invalidating tokens when security changes.
/// Changes when: password changed, 2FA enabled/disabled, email changed.
/// </summary>
public string SecurityStamp { get; private set; } = string.Empty;

/// <summary>
/// Whether email is confirmed.
/// </summary>
public bool EmailConfirmed { get; private set; }

/// <summary>
/// Whether two-factor authentication is enabled.
/// </summary>
public bool TwoFactorEnabled { get; private set; }

/// <summary>
/// Encrypted TOTP secret for 2FA.
/// </summary>
public string? TwoFactorSecret { get; private set; }

/// <summary>
/// Lockout end date (null = not locked out).
/// </summary>
public DateTime? LockoutEnd { get; private set; }

/// <summary>
/// Password last changed date.
/// </summary>
public DateTime? PasswordChangedAt { get; private set; }
```

**Add Methods**:

```csharp
#region Password Management

/// <summary>
/// Set password hash (called from Application layer after hashing).
/// </summary>
public Result SetPasswordHash(string passwordHash)
{
    if (string.IsNullOrWhiteSpace(passwordHash))
        return Result.Failure(IdentityErrors.InvalidPassword);
    
    PasswordHash = passwordHash;
    PasswordChangedAt = DateTime.UtcNow;
    SecurityStamp = GenerateSecurityStamp();
    UpdatedAt = DateTime.UtcNow;
    
    RaiseDomainEvent(new UserPasswordChangedEvent(Id, OrganizationId, DateTime.UtcNow));
    
    return Result.Success();
}

/// <summary>
/// Confirm email address.
/// </summary>
public Result ConfirmEmail()
{
    if (EmailConfirmed)
        return Result.Failure(IdentityErrors.EmailAlreadyConfirmed);
    
    EmailConfirmed = true;
    UpdatedAt = DateTime.UtcNow;
    
    // If pending activation and email confirmed, activate
    if (Status == UserStatus.PendingActivation)
        Activate();
    
    RaiseDomainEvent(new UserEmailConfirmedEvent(Id, OrganizationId, DateTime.UtcNow));
    
    return Result.Success();
}

/// <summary>
/// Regenerate security stamp (invalidates all tokens).
/// </summary>
public void RegenerateSecurityStamp()
{
    SecurityStamp = GenerateSecurityStamp();
    UpdatedAt = DateTime.UtcNow;
}

private static string GenerateSecurityStamp()
{
    return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
}

#endregion

#region Lockout Management

/// <summary>
/// Lock user account until specified date.
/// </summary>
public Result LockAccount(DateTime lockoutEnd, string reason)
{
    if (Role == UserRole.SuperAdmin)
        return Result.Failure(IdentityErrors.SuperAdminCannotBeLocked);
    
    LockoutEnd = lockoutEnd;
    UpdatedAt = DateTime.UtcNow;
    
    RaiseDomainEvent(new UserLockedOutEvent(Id, OrganizationId, _consecutiveFailedLogins, DateTime.UtcNow));
    
    return Result.Success();
}

/// <summary>
/// Unlock user account.
/// </summary>
public void UnlockAccount()
{
    LockoutEnd = null;
    _consecutiveFailedLogins = 0;
    UpdatedAt = DateTime.UtcNow;
    
    RaiseDomainEvent(new UserUnlockedEvent(Id, OrganizationId, DateTime.UtcNow));
}

/// <summary>
/// Check if user is currently locked out.
/// </summary>
public bool IsLockedOut => LockoutEnd.HasValue && LockoutEnd.Value > DateTime.UtcNow;

#endregion
```

### 1.1.2 New Domain Events

**File**: `Trap-Intel.Domain/Identity/Events/IdentityEvents.cs`

```csharp
// ADD these events:

/// <summary>
/// Raised when user password is changed.
/// </summary>
public record UserPasswordChangedEvent(
    Guid UserId,
    Guid OrganizationId,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Raised when user email is confirmed.
/// </summary>
public record UserEmailConfirmedEvent(
    Guid UserId,
    Guid OrganizationId,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Raised when user account is unlocked.
/// </summary>
public record UserUnlockedEvent(
    Guid UserId,
    Guid OrganizationId,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Raised when user successfully logs in.
/// </summary>
public record UserLoggedInEvent(
    Guid UserId,
    Guid OrganizationId,
    string IpAddress,
    string UserAgent,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Raised when user logs out.
/// </summary>
public record UserLoggedOutEvent(
    Guid UserId,
    Guid OrganizationId,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Raised when 2FA is enabled.
/// </summary>
public record UserTwoFactorEnabledEvent(
    Guid UserId,
    Guid OrganizationId,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Raised when 2FA is disabled.
/// </summary>
public record UserTwoFactorDisabledEvent(
    Guid UserId,
    Guid OrganizationId,
    DateTime OccurredOn) : IDomainEvent;
```

### 1.1.3 Update Identity Errors

**File**: `Trap-Intel.Domain/Identity/IdentityErrors.cs`

```csharp
// ADD these errors:

public static Error InvalidPassword =>
    Error.Custom("Identity.InvalidPassword", "Password is invalid.");

public static Error PasswordTooWeak =>
    Error.Custom("Identity.PasswordTooWeak", "Password does not meet complexity requirements.");

public static Error EmailAlreadyConfirmed =>
    Error.Custom("Identity.EmailAlreadyConfirmed", "Email is already confirmed.");

public static Error EmailNotConfirmed =>
    Error.Custom("Identity.EmailNotConfirmed", "Email must be confirmed before login.");

public static Error InvalidCredentials =>
    Error.Custom("Identity.InvalidCredentials", "Invalid email or password.");

public static Error AccountLocked =>
    Error.Custom("Identity.AccountLocked", "Account is locked. Try again later.");

public static Error SuperAdminCannotBeLocked =>
    Error.Custom("Identity.SuperAdminCannotBeLocked", "Super admin accounts cannot be locked.");

public static Error TwoFactorRequired =>
    Error.Custom("Identity.TwoFactorRequired", "Two-factor authentication code required.");

public static Error InvalidTwoFactorCode =>
    Error.Custom("Identity.InvalidTwoFactorCode", "Invalid two-factor authentication code.");

public static Error InvalidRefreshToken =>
    Error.Custom("Identity.InvalidRefreshToken", "Refresh token is invalid or expired.");

public static Error RefreshTokenReused =>
    Error.Custom("Identity.RefreshTokenReused", "Refresh token reuse detected. All sessions revoked.");

public static Error SessionExpired =>
    Error.Custom("Identity.SessionExpired", "Session has expired. Please login again.");

public static Error InvalidEmailVerificationToken =>
    Error.Custom("Identity.InvalidEmailVerificationToken", "Email verification token is invalid or expired.");

public static Error InvalidPasswordResetToken =>
    Error.Custom("Identity.InvalidPasswordResetToken", "Password reset token is invalid or expired.");
```

---

## 1.2 Infrastructure Layer

### 1.2.1 Add NuGet Packages

**File**: `Trap-Intel.Infrastructure/Trap-Intel.Infrastructure.csproj`

```xml
<ItemGroup>
  <!-- Existing packages... -->
  
  <!-- Identity - ONLY for password hashing and 2FA -->
  <PackageReference Include="Microsoft.AspNetCore.Identity" Version="2.3.0" />
  
  <!-- JWT Token Generation -->
  <PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="8.3.0" />
  <PackageReference Include="Microsoft.IdentityModel.Tokens" Version="8.3.0" />
  
  <!-- TOTP for 2FA -->
  <PackageReference Include="Otp.NET" Version="1.4.0" />
</ItemGroup>
```

### 1.2.2 Password Hashing Service

**File**: `Trap-Intel.Infrastructure/Authentication/Services/PasswordHashingService.cs`

```csharp
using Microsoft.AspNetCore.Identity;
using Trap_Intel.Domain.Identity;

namespace Trap_Intel.Infrastructure.Authentication.Services;

/// <summary>
/// Service for secure password hashing using BCrypt (via ASP.NET Identity).
/// </summary>
public interface IPasswordHashingService
{
    /// <summary>
    /// Hash a password securely.
    /// </summary>
    string HashPassword(string password);
    
    /// <summary>
    /// Verify a password against a hash.
    /// </summary>
    PasswordVerificationResult VerifyPassword(string hashedPassword, string providedPassword);
}

public sealed class PasswordHashingService : IPasswordHashingService
{
    private readonly IPasswordHasher<User> _passwordHasher;

    public PasswordHashingService()
    {
        _passwordHasher = new PasswordHasher<User>(new OptionsWrapper<PasswordHasherOptions>(
            new PasswordHasherOptions
            {
                CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3,
                IterationCount = 100_000 // OWASP recommended minimum
            }));
    }

    public string HashPassword(string password)
    {
        // PasswordHasher expects a user object but only uses it for auditing
        // We pass null since we don't need that feature
        return _passwordHasher.HashPassword(null!, password);
    }

    public PasswordVerificationResult VerifyPassword(string hashedPassword, string providedPassword)
    {
        return _passwordHasher.VerifyHashedPassword(null!, hashedPassword, providedPassword);
    }
}
```

### 1.2.3 JWT Token Service

**File**: `Trap-Intel.Infrastructure/Authentication/Services/JwtTokenService.cs`

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Trap_Intel.Domain.Identity;
using Trap_Intel.Infrastructure.Authentication.Configuration;

namespace Trap_Intel.Infrastructure.Authentication.Services;

/// <summary>
/// Service for generating and validating JWT access tokens.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generate a JWT access token for a user.
    /// </summary>
    string GenerateAccessToken(User user, IEnumerable<string> permissions);
    
    /// <summary>
    /// Validate a JWT access token and return claims principal.
    /// </summary>
    ClaimsPrincipal? ValidateAccessToken(string token);
    
    /// <summary>
    /// Get user ID from token without full validation (for refresh flow).
    /// </summary>
    Guid? GetUserIdFromExpiredToken(string token);
}

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _settings;
    private readonly TokenValidationParameters _validationParameters;
    private readonly SigningCredentials _signingCredentials;

    public JwtTokenService(IOptions<JwtSettings> settings)
    {
        _settings = settings.Value;
        
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
        _signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        
        _validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateIssuer = true,
            ValidIssuer = _settings.Issuer,
            ValidateAudience = true,
            ValidAudience = _settings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero // No tolerance for expiration
        };
    }

    public string GenerateAccessToken(User user, IEnumerable<string> permissions)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new("org", user.OrganizationId.ToString()),
            new("role", user.Role.ToString()),
            new("email", user.Email.Value),
            new("name", user.FullName),
            new("security_stamp", user.SecurityStamp) // For token invalidation
        };
        
        // Add permissions as claims (for authorization)
        foreach (var permission in permissions)
        {
            claims.Add(new Claim("permission", permission));
        }

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(_settings.AccessTokenExpirationMinutes),
            signingCredentials: _signingCredentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal? ValidateAccessToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, _validationParameters, out _);
            return principal;
        }
        catch
        {
            return null;
        }
    }

    public Guid? GetUserIdFromExpiredToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            
            // Validate without checking expiration
            var paramsWithoutLifetime = _validationParameters.Clone();
            paramsWithoutLifetime.ValidateLifetime = false;
            
            var principal = handler.ValidateToken(token, paramsWithoutLifetime, out _);
            var userIdClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub);
            
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
                return userId;
            
            return null;
        }
        catch
        {
            return null;
        }
    }
}
```

### 1.2.4 JWT Settings Configuration

**File**: `Trap-Intel.Infrastructure/Authentication/Configuration/JwtSettings.cs`

```csharp
namespace Trap_Intel.Infrastructure.Authentication.Configuration;

/// <summary>
/// JWT configuration settings.
/// </summary>
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";
    
    /// <summary>
    /// Secret key for signing tokens. In production, use Azure Key Vault or similar.
    /// Minimum 256 bits (32 characters) for HS256.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;
    
    /// <summary>
    /// Token issuer (your application identifier).
    /// </summary>
    public string Issuer { get; set; } = "trap-intel";
    
    /// <summary>
    /// Token audience (API identifier).
    /// </summary>
    public string Audience { get; set; } = "trap-intel-api";
    
    /// <summary>
    /// Access token lifetime in minutes.
    /// Recommended: 15-30 minutes.
    /// </summary>
    public int AccessTokenExpirationMinutes { get; set; } = 15;
    
    /// <summary>
    /// Refresh token lifetime in days.
    /// Recommended: 7-30 days.
    /// </summary>
    public int RefreshTokenExpirationDays { get; set; } = 7;
    
    /// <summary>
    /// Whether to allow refresh token rotation (recommended: true).
    /// </summary>
    public bool EnableRefreshTokenRotation { get; set; } = true;
}
```

### 1.2.5 Update appsettings.json

```json
{
  "Jwt": {
    "SecretKey": "YOUR-SUPER-SECRET-KEY-AT-LEAST-32-CHARACTERS-LONG-FOR-SECURITY",
    "Issuer": "trap-intel",
    "Audience": "trap-intel-api",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7,
    "EnableRefreshTokenRotation": true
  }
}
```

---

## 1.3 Database Changes

### 1.3.1 Update User Configuration

**File**: `Trap-Intel.Infrastructure/Configuration/EntityConfigurations/UserConfiguration.cs`

```csharp
// ADD to UserConfiguration.Configure():

// Password & Security
builder.Property(u => u.PasswordHash)
    .HasColumnName("password_hash")
    .HasMaxLength(256)
    .IsRequired();

builder.Property(u => u.SecurityStamp)
    .HasColumnName("security_stamp")
    .HasMaxLength(64)
    .IsRequired();

builder.Property(u => u.EmailConfirmed)
    .HasColumnName("email_confirmed")
    .HasDefaultValue(false);

builder.Property(u => u.TwoFactorEnabled)
    .HasColumnName("two_factor_enabled")
    .HasDefaultValue(false);

builder.Property(u => u.TwoFactorSecret)
    .HasColumnName("two_factor_secret")
    .HasMaxLength(256);

builder.Property(u => u.LockoutEnd)
    .HasColumnName("lockout_end");

builder.Property(u => u.PasswordChangedAt)
    .HasColumnName("password_changed_at");
```

---

## 1.4 API Layer

### 1.4.1 Auth Controller

**File**: `Trap-Intel.Api/Controllers/AuthController.cs`

```csharp
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Trap_Intel.Application.Authentication.Commands;
using Trap_Intel.Application.Authentication.Queries;

namespace Trap_Intel.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Login with email and password.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        command = command with 
        { 
            IpAddress = GetClientIpAddress(),
            UserAgent = GetUserAgent()
        };
        
        var result = await _sender.Send(command);
        
        if (result.IsFailure)
            return Unauthorized(new { error = result.Error.Code, message = result.Error.Message });
        
        return Ok(result.Value);
    }

    /// <summary>
    /// Register a new user and organization.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command)
    {
        var result = await _sender.Send(command);
        
        if (result.IsFailure)
            return BadRequest(new { error = result.Error.Code, message = result.Error.Message });
        
        return Ok(new { message = "Registration successful. Please check your email to verify your account." });
    }

    /// <summary>
    /// Refresh access token.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenCommand command)
    {
        var result = await _sender.Send(command);
        
        if (result.IsFailure)
            return Unauthorized(new { error = result.Error.Code, message = result.Error.Message });
        
        return Ok(result.Value);
    }

    /// <summary>
    /// Logout (revoke refresh token).
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] LogoutCommand command)
    {
        var result = await _sender.Send(command);
        return result.IsSuccess ? Ok() : BadRequest();
    }

    /// <summary>
    /// Verify email address.
    /// </summary>
    [HttpPost("verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailCommand command)
    {
        var result = await _sender.Send(command);
        
        if (result.IsFailure)
            return BadRequest(new { error = result.Error.Code, message = result.Error.Message });
        
        return Ok(new { message = "Email verified successfully." });
    }

    private string GetClientIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private string GetUserAgent()
    {
        return Request.Headers.UserAgent.ToString();
    }
}
```

---

## 1.5 Deliverables Checklist

### Domain Layer
- [ ] Update `User` entity with password and security properties
- [ ] Add new domain events (PasswordChanged, EmailConfirmed, etc.)
- [ ] Add new identity errors
- [ ] Create `RefreshToken` entity (Phase 2)

### Infrastructure Layer
- [ ] Add NuGet packages
- [ ] Implement `IPasswordHashingService`
- [ ] Implement `IJwtTokenService`
- [ ] Create `JwtSettings` configuration
- [ ] Update `UserConfiguration` for EF

### API Layer
- [ ] Create `AuthController`
- [ ] Configure JWT authentication middleware
- [ ] Add authorization policies

### Application Layer
- [ ] Create `LoginCommand` and handler
- [ ] Create `RegisterCommand` and handler
- [ ] Create authentication DTOs

### Testing
- [ ] Unit tests for password hashing
- [ ] Unit tests for JWT generation/validation
- [ ] Integration tests for login flow

---

# Phase 2: Token Management System

## ?? Duration: 5-7 Days

## ?? Objectives
- Implement Refresh Token entity with rotation
- Implement token family for reuse detection
- Create token revocation mechanism
- Implement "Remember Me" functionality

---

## 2.1 Domain Layer - Refresh Token Entity

### 2.1.1 RefreshToken Entity

**File**: `Trap-Intel.Domain/Identity/Entities/RefreshToken.cs`

```csharp
using System.Security.Cryptography;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Shared;

namespace Trap_Intel.Domain.Identity.Entities;

/// <summary>
/// Represents a refresh token for JWT token renewal.
/// Implements secure token rotation with reuse detection.
/// </summary>
public class RefreshToken : Entity<Guid>
{
    private RefreshToken() { }

    private RefreshToken(
        Guid id,
        Guid userId,
        string tokenHash,
        Guid familyId,
        DateTime expiresAt,
        string? deviceInfo,
        string? ipAddress)
        : base(id)
    {
        UserId = userId;
        TokenHash = tokenHash;
        FamilyId = familyId;
        ExpiresAt = expiresAt;
        DeviceInfo = deviceInfo;
        IpAddress = ipAddress;
        CreatedAt = DateTime.UtcNow;
        IsRevoked = false;
        IsUsed = false;
    }

    #region Properties

    /// <summary>
    /// User this token belongs to.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// SHA-256 hash of the actual token.
    /// The raw token is only returned to client once.
    /// </summary>
    public string TokenHash { get; private set; } = string.Empty;

    /// <summary>
    /// Token family ID for rotation tracking.
    /// All tokens in a rotation chain share the same family.
    /// </summary>
    public Guid FamilyId { get; private set; }

    /// <summary>
    /// When this token expires.
    /// </summary>
    public DateTime ExpiresAt { get; private set; }

    /// <summary>
    /// When this token was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When this token was used (for rotation tracking).
    /// </summary>
    public DateTime? UsedAt { get; private set; }

    /// <summary>
    /// Whether this token has been revoked.
    /// </summary>
    public bool IsRevoked { get; private set; }

    /// <summary>
    /// When this token was revoked.
    /// </summary>
    public DateTime? RevokedAt { get; private set; }

    /// <summary>
    /// Reason for revocation.
    /// </summary>
    public string? RevocationReason { get; private set; }

    /// <summary>
    /// Whether this token has been used (for one-time use).
    /// </summary>
    public bool IsUsed { get; private set; }

    /// <summary>
    /// ID of the replacement token (if rotated).
    /// </summary>
    public Guid? ReplacedByTokenId { get; private set; }

    /// <summary>
    /// Device information (browser, OS).
    /// </summary>
    public string? DeviceInfo { get; private set; }

    /// <summary>
    /// IP address where token was issued.
    /// </summary>
    public string? IpAddress { get; private set; }

    #endregion

    #region Computed Properties

    /// <summary>
    /// Check if token is expired.
    /// </summary>
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    /// <summary>
    /// Check if token is valid (not expired, not revoked, not used).
    /// </summary>
    public bool IsValid => !IsExpired && !IsRevoked && !IsUsed;

    #endregion

    #region Factory Methods

    /// <summary>
    /// Create a new refresh token.
    /// Returns the entity AND the raw token (only returned once).
    /// </summary>
    public static (RefreshToken Token, string RawToken) Create(
        Guid userId,
        int expirationDays,
        string? deviceInfo = null,
        string? ipAddress = null,
        Guid? familyId = null)
    {
        // Generate cryptographically secure random token
        var rawToken = GenerateSecureToken();
        var tokenHash = HashToken(rawToken);
        
        var token = new RefreshToken(
            Guid.NewGuid(),
            userId,
            tokenHash,
            familyId ?? Guid.NewGuid(), // New family if not specified
            DateTime.UtcNow.AddDays(expirationDays),
            deviceInfo,
            ipAddress);

        return (token, rawToken);
    }

    /// <summary>
    /// Create a rotated refresh token (new token in same family).
    /// </summary>
    public static (RefreshToken Token, string RawToken) CreateRotated(
        RefreshToken previousToken,
        int expirationDays)
    {
        var (newToken, rawToken) = Create(
            previousToken.UserId,
            expirationDays,
            previousToken.DeviceInfo,
            previousToken.IpAddress,
            previousToken.FamilyId); // Same family

        return (newToken, rawToken);
    }

    #endregion

    #region Domain Operations

    /// <summary>
    /// Mark token as used and link to replacement.
    /// </summary>
    public void MarkAsUsed(Guid replacementTokenId)
    {
        IsUsed = true;
        UsedAt = DateTime.UtcNow;
        ReplacedByTokenId = replacementTokenId;
    }

    /// <summary>
    /// Revoke this token.
    /// </summary>
    public void Revoke(string reason)
    {
        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
        RevocationReason = reason;
    }

    /// <summary>
    /// Verify a raw token against this token's hash.
    /// </summary>
    public bool VerifyToken(string rawToken)
    {
        var providedHash = HashToken(rawToken);
        return TokenHash == providedHash;
    }

    #endregion

    #region Private Methods

    private static string GenerateSecureToken()
    {
        var randomBytes = new byte[64]; // 512 bits
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    private static string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(token);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    #endregion
}
```

### 2.1.2 RefreshToken Repository Interface

**File**: `Trap-Intel.Domain/Identity/IRefreshTokenRepository.cs`

```csharp
namespace Trap_Intel.Domain.Identity;

/// <summary>
/// Repository interface for RefreshToken operations.
/// </summary>
public interface IRefreshTokenRepository
{
    /// <summary>
    /// Get a refresh token by its hash.
    /// </summary>
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash);

    /// <summary>
    /// Get all active tokens for a user.
    /// </summary>
    Task<IReadOnlyList<RefreshToken>> GetActiveByUserAsync(Guid userId);

    /// <summary>
    /// Get all tokens in a family.
    /// </summary>
    Task<IReadOnlyList<RefreshToken>> GetByFamilyAsync(Guid familyId);

    /// <summary>
    /// Add a new refresh token.
    /// </summary>
    Task AddAsync(RefreshToken token);

    /// <summary>
    /// Update a refresh token.
    /// </summary>
    Task UpdateAsync(RefreshToken token);

    /// <summary>
    /// Revoke all tokens for a user.
    /// </summary>
    Task RevokeAllForUserAsync(Guid userId, string reason);

    /// <summary>
    /// Revoke all tokens in a family.
    /// </summary>
    Task RevokeAllInFamilyAsync(Guid familyId, string reason);

    /// <summary>
    /// Delete expired tokens (cleanup job).
    /// </summary>
    Task DeleteExpiredAsync(DateTime olderThan);

    /// <summary>
    /// Count active sessions for a user.
    /// </summary>
    Task<int> CountActiveSessionsAsync(Guid userId);
}
```

---

## 2.2 Refresh Token Service

**File**: `Trap-Intel.Infrastructure/Authentication/Services/RefreshTokenService.cs`

```csharp
using Microsoft.Extensions.Options;
using Trap_Intel.Domain.Identity;
using Trap_Intel.Domain.Identity.Entities;
using Trap_Intel.Domain.Shared;
using Trap_Intel.Infrastructure.Authentication.Configuration;

namespace Trap_Intel.Infrastructure.Authentication.Services;

/// <summary>
/// Service for managing refresh tokens with secure rotation.
/// </summary>
public interface IRefreshTokenService
{
    /// <summary>
    /// Create a new refresh token for a user.
    /// </summary>
    Task<(RefreshToken Token, string RawToken)> CreateTokenAsync(
        Guid userId,
        string? deviceInfo = null,
        string? ipAddress = null);

    /// <summary>
    /// Rotate a refresh token (use old, create new).
    /// </summary>
    Task<Result<(RefreshToken NewToken, string RawToken)>> RotateTokenAsync(
        string rawToken);

    /// <summary>
    /// Validate a refresh token.
    /// </summary>
    Task<Result<RefreshToken>> ValidateTokenAsync(string rawToken);

    /// <summary>
    /// Revoke a specific refresh token.
    /// </summary>
    Task RevokeTokenAsync(string rawToken, string reason);

    /// <summary>
    /// Revoke all tokens for a user.
    /// </summary>
    Task RevokeAllUserTokensAsync(Guid userId, string reason);
}

public sealed class RefreshTokenService : IRefreshTokenService
{
    private readonly IRefreshTokenRepository _repository;
    private readonly JwtSettings _settings;

    public RefreshTokenService(
        IRefreshTokenRepository repository,
        IOptions<JwtSettings> settings)
    {
        _repository = repository;
        _settings = settings.Value;
    }

    public async Task<(RefreshToken Token, string RawToken)> CreateTokenAsync(
        Guid userId,
        string? deviceInfo = null,
        string? ipAddress = null)
    {
        var (token, rawToken) = RefreshToken.Create(
            userId,
            _settings.RefreshTokenExpirationDays,
            deviceInfo,
            ipAddress);

        await _repository.AddAsync(token);

        return (token, rawToken);
    }

    public async Task<Result<(RefreshToken NewToken, string RawToken)>> RotateTokenAsync(
        string rawToken)
    {
        var validationResult = await ValidateTokenAsync(rawToken);
        
        if (validationResult.IsFailure)
            return Result.Failure<(RefreshToken, string)>(validationResult.Error);

        var oldToken = validationResult.Value;

        // Check for token reuse (security breach)
        if (oldToken.IsUsed)
        {
            // Token reuse detected! Revoke entire family
            await _repository.RevokeAllInFamilyAsync(
                oldToken.FamilyId,
                "Token reuse detected - possible token theft");

            return Result.Failure<(RefreshToken, string)>(IdentityErrors.RefreshTokenReused);
        }

        // Create rotated token (same family)
        var (newToken, newRawToken) = RefreshToken.CreateRotated(
            oldToken,
            _settings.RefreshTokenExpirationDays);

        // Mark old token as used
        oldToken.MarkAsUsed(newToken.Id);

        // Save changes
        await _repository.UpdateAsync(oldToken);
        await _repository.AddAsync(newToken);

        return Result.Success((newToken, newRawToken));
    }

    public async Task<Result<RefreshToken>> ValidateTokenAsync(string rawToken)
    {
        // Hash the provided token to find it
        var tokenHash = HashToken(rawToken);
        var token = await _repository.GetByTokenHashAsync(tokenHash);

        if (token == null)
            return Result.Failure<RefreshToken>(IdentityErrors.InvalidRefreshToken);

        if (token.IsRevoked)
            return Result.Failure<RefreshToken>(IdentityErrors.InvalidRefreshToken);

        if (token.IsExpired)
            return Result.Failure<RefreshToken>(IdentityErrors.SessionExpired);

        // Note: We check IsUsed in RotateTokenAsync for reuse detection

        return Result.Success(token);
    }

    public async Task RevokeTokenAsync(string rawToken, string reason)
    {
        var tokenHash = HashToken(rawToken);
        var token = await _repository.GetByTokenHashAsync(tokenHash);

        if (token != null)
        {
            token.Revoke(reason);
            await _repository.UpdateAsync(token);
        }
    }

    public async Task RevokeAllUserTokensAsync(Guid userId, string reason)
    {
        await _repository.RevokeAllForUserAsync(userId, reason);
    }

    private static string HashToken(string token)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(token);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
```

---

## 2.3 Token Rotation Flow Diagram

```
???????????????????????????????????????????????????????????????????????
?                    REFRESH TOKEN ROTATION FLOW                       ?
???????????????????????????????????????????????????????????????????????

Normal Flow:
?????????????
1. Login ? AccessToken (15min) + RefreshToken_A (Family: X)
2. AccessToken expires
3. Client sends RefreshToken_A
4. Server validates RefreshToken_A ?
5. Server marks RefreshToken_A as "used"
6. Server creates RefreshToken_B (Family: X, ReplacedBy: B)
7. Returns new AccessToken + RefreshToken_B

Token Reuse Detection:
??????????????????????
1. Attacker steals RefreshToken_A
2. Real user uses RefreshToken_A ? Gets RefreshToken_B
3. Attacker tries to use RefreshToken_A
4. Server sees RefreshToken_A is already "used"
5. SERVER REVOKES ENTIRE FAMILY X (all tokens)
6. Both attacker AND user must re-login
7. Alert is raised for security investigation

???????????????????????????????????????????????????????????????????????
?                         TOKEN FAMILY                                 ?
?                                                                     ?
?   Login                                                             ?
?     ?                                                               ?
?     ?                                                               ?
?   Token_A (active) ??refresh??> Token_B (active) ??refresh??> ...  ?
?   Family: X                     Family: X                           ?
?   Used: false                   Used: false                         ?
?     ?                             ?                                 ?
?     ? (after refresh)            ? (after refresh)                 ?
?   Token_A (used)               Token_B (used)                       ?
?   ReplacedBy: B                ReplacedBy: C                        ?
?                                                                     ?
?   If Token_A is used again ? REVOKE ALL in Family X                 ?
?                                                                     ?
???????????????????????????????????????????????????????????????????????
```

---

## 2.4 Deliverables Checklist

### Domain Layer
- [ ] Create `RefreshToken` entity
- [ ] Create `IRefreshTokenRepository` interface
- [ ] Add refresh token related domain events

### Infrastructure Layer
- [ ] Implement `IRefreshTokenService`
- [ ] Implement `RefreshTokenRepository`
- [ ] Create EF configuration for `RefreshToken`

### Application Layer
- [ ] Create `RefreshTokenCommand` and handler
- [ ] Create `RevokeTokenCommand` and handler
- [ ] Create `RevokeAllUserTokensCommand` and handler

### Testing
- [ ] Unit tests for token rotation
- [ ] Unit tests for reuse detection
- [ ] Integration tests for refresh flow

---

# Phase 3: Security Hardening

## ?? Duration: 4-5 Days

## ?? Objectives
- Implement email verification flow
- Implement password reset flow
- Add password complexity validation
- Implement account lockout enhancement

---

## 3.1 Email Verification Token Entity

**File**: `Trap-Intel.Domain/Identity/Entities/EmailVerificationToken.cs`

```csharp
using System.Security.Cryptography;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Shared;

namespace Trap_Intel.Domain.Identity.Entities;

/// <summary>
/// Token for verifying user email addresses.
/// Single-use, time-limited token.
/// </summary>
public class EmailVerificationToken : Entity<Guid>
{
    private EmailVerificationToken() { }

    private EmailVerificationToken(
        Guid id,
        Guid userId,
        string tokenHash,
        DateTime expiresAt)
        : base(id)
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        CreatedAt = DateTime.UtcNow;
        IsUsed = false;
    }

    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsUsed { get; private set; }
    public DateTime? UsedAt { get; private set; }

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public bool IsValid => !IsExpired && !IsUsed;

    public static (EmailVerificationToken Token, string RawToken) Create(
        Guid userId,
        int expirationHours = 24)
    {
        var rawToken = GenerateSecureToken();
        var tokenHash = HashToken(rawToken);

        var token = new EmailVerificationToken(
            Guid.NewGuid(),
            userId,
            tokenHash,
            DateTime.UtcNow.AddHours(expirationHours));

        return (token, rawToken);
    }

    public Result MarkAsUsed()
    {
        if (IsUsed)
            return Result.Failure(IdentityErrors.InvalidEmailVerificationToken);
        
        if (IsExpired)
            return Result.Failure(IdentityErrors.InvalidEmailVerificationToken);

        IsUsed = true;
        UsedAt = DateTime.UtcNow;
        
        return Result.Success();
    }

    public bool VerifyToken(string rawToken)
    {
        var providedHash = HashToken(rawToken);
        return TokenHash == providedHash && IsValid;
    }

    private static string GenerateSecureToken()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes).Replace("+", "-").Replace("/", "_");
    }

    private static string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(token);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
```

---

## 3.2 Password Reset Token Entity

**File**: `Trap-Intel.Domain/Identity/Entities/PasswordResetToken.cs`

```csharp
using System.Security.Cryptography;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Shared;

namespace Trap_Intel.Domain.Identity.Entities;

/// <summary>
/// Token for password reset requests.
/// Single-use, short-lived token with IP tracking.
/// </summary>
public class PasswordResetToken : Entity<Guid>
{
    private PasswordResetToken() { }

    private PasswordResetToken(
        Guid id,
        Guid userId,
        string tokenHash,
        DateTime expiresAt,
        string? ipAddress)
        : base(id)
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        RequestedFromIP = ipAddress;
        CreatedAt = DateTime.UtcNow;
        IsUsed = false;
    }

    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsUsed { get; private set; }
    public DateTime? UsedAt { get; private set; }
    public string? RequestedFromIP { get; private set; }
    public string? UsedFromIP { get; private set; }

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public bool IsValid => !IsExpired && !IsUsed;

    public static (PasswordResetToken Token, string RawToken) Create(
        Guid userId,
        string? ipAddress = null,
        int expirationMinutes = 60) // 1 hour default
    {
        var rawToken = GenerateSecureToken();
        var tokenHash = HashToken(rawToken);

        var token = new PasswordResetToken(
            Guid.NewGuid(),
            userId,
            tokenHash,
            DateTime.UtcNow.AddMinutes(expirationMinutes),
            ipAddress);

        return (token, rawToken);
    }

    public Result MarkAsUsed(string? usedFromIP = null)
    {
        if (IsUsed)
            return Result.Failure(IdentityErrors.InvalidPasswordResetToken);
        
        if (IsExpired)
            return Result.Failure(IdentityErrors.InvalidPasswordResetToken);

        IsUsed = true;
        UsedAt = DateTime.UtcNow;
        UsedFromIP = usedFromIP;
        
        return Result.Success();
    }

    public bool VerifyToken(string rawToken)
    {
        var providedHash = HashToken(rawToken);
        return TokenHash == providedHash && IsValid;
    }

    private static string GenerateSecureToken()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes).Replace("+", "-").Replace("/", "_");
    }

    private static string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(token);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
```

---

## 3.3 Password Validation Policy

**File**: `Trap-Intel.Domain/Identity/Policies/PasswordPolicy.cs`

```csharp
using System.Text.RegularExpressions;
using Trap_Intel.Domain.Shared;

namespace Trap_Intel.Domain.Identity.Policies;

/// <summary>
/// Password complexity and validation policy.
/// OWASP compliant password requirements.
/// </summary>
public static class PasswordPolicy
{
    public const int MinimumLength = 8;
    public const int MaximumLength = 128;
    public const int RequiredUniqueCharacters = 4;

    /// <summary>
    /// Validate password complexity.
    /// </summary>
    public static Result ValidatePassword(string password)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(password))
        {
            return Result.Failure(IdentityErrors.InvalidPassword);
        }

        if (password.Length < MinimumLength)
        {
            errors.Add($"Password must be at least {MinimumLength} characters long.");
        }

        if (password.Length > MaximumLength)
        {
            errors.Add($"Password cannot exceed {MaximumLength} characters.");
        }

        if (!HasUpperCase(password))
        {
            errors.Add("Password must contain at least one uppercase letter.");
        }

        if (!HasLowerCase(password))
        {
            errors.Add("Password must contain at least one lowercase letter.");
        }

        if (!HasDigit(password))
        {
            errors.Add("Password must contain at least one digit.");
        }

        if (!HasSpecialCharacter(password))
        {
            errors.Add("Password must contain at least one special character.");
        }

        if (password.Distinct().Count() < RequiredUniqueCharacters)
        {
            errors.Add($"Password must contain at least {RequiredUniqueCharacters} unique characters.");
        }

        if (HasCommonPattern(password))
        {
            errors.Add("Password contains a common pattern and is easily guessable.");
        }

        if (errors.Count > 0)
        {
            return Result.Failure(
                Error.Custom("Identity.PasswordTooWeak", string.Join(" ", errors)));
        }

        return Result.Success();
    }

    /// <summary>
    /// Check if password is in common password list.
    /// </summary>
    public static bool IsCommonPassword(string password)
    {
        // Top 100 most common passwords (abbreviated list)
        var commonPasswords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "password", "123456", "12345678", "qwerty", "abc123",
            "monkey", "1234567", "letmein", "trustno1", "dragon",
            "baseball", "iloveyou", "master", "sunshine", "ashley",
            "bailey", "passw0rd", "shadow", "123123", "654321",
            "superman", "qazwsx", "michael", "football", "password1",
            "password123", "welcome", "welcome1", "admin", "login"
        };

        return commonPasswords.Contains(password);
    }

    private static bool HasUpperCase(string password) =>
        password.Any(char.IsUpper);

    private static bool HasLowerCase(string password) =>
        password.Any(char.IsLower);

    private static bool HasDigit(string password) =>
        password.Any(char.IsDigit);

    private static bool HasSpecialCharacter(string password) =>
        password.Any(c => !char.IsLetterOrDigit(c));

    private static bool HasCommonPattern(string password)
    {
        var patterns = new[]
        {
            @"(.)\1{2,}",           // Repeated characters (aaa, 111)
            @"(012|123|234|345|456|567|678|789|890)", // Sequential numbers
            @"(abc|bcd|cde|def|efg|fgh|ghi|hij|ijk|jkl|klm|lmn|mno|nop|opq|pqr|qrs|rst|stu|tuv|uvw|vwx|wxy|xyz)", // Sequential letters
            @"(qwerty|asdf|zxcv)"   // Keyboard patterns
        };

        return patterns.Any(pattern => 
            Regex.IsMatch(password, pattern, RegexOptions.IgnoreCase));
    }
}
```

---

## 3.4 Lockout Configuration

**File**: `Trap-Intel.Infrastructure/Authentication/Configuration/LockoutSettings.cs`

```csharp
namespace Trap_Intel.Infrastructure.Authentication.Configuration;

/// <summary>
/// Account lockout configuration settings.
/// </summary>
public sealed class LockoutSettings
{
    public const string SectionName = "Lockout";

    /// <summary>
    /// Maximum failed login attempts before lockout.
    /// </summary>
    public int MaxFailedAttempts { get; set; } = 5;

    /// <summary>
    /// Lockout duration in minutes.
    /// </summary>
    public int LockoutDurationMinutes { get; set; } = 15;

    /// <summary>
    /// Whether to enable lockout for new users.
    /// </summary>
    public bool EnableLockout { get; set; } = true;

    /// <summary>
    /// Whether to lockout SuperAdmin accounts.
    /// </summary>
    public bool LockoutSuperAdmin { get; set; } = false;

    /// <summary>
    /// Progressive lockout multiplier (each subsequent lockout is longer).
    /// </summary>
    public double ProgressiveLockoutMultiplier { get; set; } = 2.0;

    /// <summary>
    /// Maximum lockout duration in minutes.
    /// </summary>
    public int MaxLockoutDurationMinutes { get; set; } = 1440; // 24 hours
}
```

---

## 3.5 Deliverables Checklist

### Domain Layer
- [ ] Create `EmailVerificationToken` entity
- [ ] Create `PasswordResetToken` entity
- [ ] Create `PasswordPolicy` policy
- [ ] Add repository interfaces

### Infrastructure Layer
- [ ] Implement token repositories
- [ ] Create EF configurations
- [ ] Implement email service interface (for sending emails)
- [ ] Create `LockoutSettings` configuration

### Application Layer
- [ ] Create `VerifyEmailCommand` and handler
- [ ] Create `ForgotPasswordCommand` and handler
- [ ] Create `ResetPasswordCommand` and handler
- [ ] Create `ChangePasswordCommand` and handler

### Testing
- [ ] Unit tests for password validation
- [ ] Unit tests for token generation
- [ ] Integration tests for email verification flow
- [ ] Integration tests for password reset flow

---

# Phase 4: Advanced Features

## ?? Duration: 5-7 Days

## ?? Objectives
- Implement Two-Factor Authentication (2FA/TOTP)
- Implement User Sessions tracking
- Implement device management
- Add security alerts

---

## 4.1 Two-Factor Authentication (2FA)

### 4.1.1 TOTP Service

**File**: `Trap-Intel.Infrastructure/Authentication/Services/TwoFactorService.cs`

```csharp
using OtpNet;
using QRCoder;
using Trap_Intel.Domain.Identity;
using Trap_Intel.Domain.Shared;

namespace Trap_Intel.Infrastructure.Authentication.Services;

/// <summary>
/// Service for Two-Factor Authentication using TOTP.
/// </summary>
public interface ITwoFactorService
{
    /// <summary>
    /// Generate a new TOTP secret for a user.
    /// </summary>
    (string Secret, string QrCodeBase64) GenerateSecret(User user);

    /// <summary>
    /// Validate a TOTP code.
    /// </summary>
    bool ValidateCode(string secret, string code);

    /// <summary>
    /// Generate backup codes.
    /// </summary>
    IReadOnlyList<string> GenerateBackupCodes(int count = 10);
}

public sealed class TwoFactorService : ITwoFactorService
{
    private const string Issuer = "Trap-Intel";
    private const int SecretSize = 20; // 160 bits

    public (string Secret, string QrCodeBase64) GenerateSecret(User user)
    {
        // Generate random secret
        var secret = KeyGeneration.GenerateRandomKey(SecretSize);
        var base32Secret = Base32Encoding.ToString(secret);

        // Create OTP URI
        var otpUri = $"otpauth://totp/{Issuer}:{user.Email.Value}?secret={base32Secret}&issuer={Issuer}";

        // Generate QR code
        using var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(otpUri, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        var qrCodeBytes = qrCode.GetGraphic(5);
        var qrCodeBase64 = Convert.ToBase64String(qrCodeBytes);

        return (base32Secret, qrCodeBase64);
    }

    public bool ValidateCode(string secret, string code)
    {
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(code))
            return false;

        try
        {
            var secretBytes = Base32Encoding.ToBytes(secret);
            var totp = new Totp(secretBytes);
            
            // Allow 1 step tolerance (30 seconds before/after)
            return totp.VerifyTotp(code, out _, new VerificationWindow(1, 1));
        }
        catch
        {
            return false;
        }
    }

    public IReadOnlyList<string> GenerateBackupCodes(int count = 10)
    {
        var codes = new List<string>(count);
        
        for (int i = 0; i < count; i++)
        {
            var code = GenerateBackupCode();
            codes.Add(code);
        }

        return codes.AsReadOnly();
    }

    private static string GenerateBackupCode()
    {
        // Generate 8-character alphanumeric code
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Removed confusing chars
        var random = new byte[8];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(random);
        
        return new string(random.Select(b => chars[b % chars.Length]).ToArray());
    }
}
```

### 4.1.2 User 2FA Methods in Domain

**Add to `User.cs`**:

```csharp
#region Two-Factor Authentication

/// <summary>
/// Enable 2FA for this user.
/// </summary>
public Result EnableTwoFactor(string encryptedSecret)
{
    if (TwoFactorEnabled)
        return Result.Failure(Error.Custom("Identity.2FAAlreadyEnabled", "2FA is already enabled."));

    TwoFactorSecret = encryptedSecret;
    TwoFactorEnabled = true;
    SecurityStamp = GenerateSecurityStamp(); // Invalidate existing tokens
    UpdatedAt = DateTime.UtcNow;

    RaiseDomainEvent(new UserTwoFactorEnabledEvent(Id, OrganizationId, DateTime.UtcNow));

    return Result.Success();
}

/// <summary>
/// Disable 2FA for this user.
/// </summary>
public Result DisableTwoFactor()
{
    if (!TwoFactorEnabled)
        return Result.Failure(Error.Custom("Identity.2FANotEnabled", "2FA is not enabled."));

    TwoFactorSecret = null;
    TwoFactorEnabled = false;
    SecurityStamp = GenerateSecurityStamp();
    UpdatedAt = DateTime.UtcNow;

    RaiseDomainEvent(new UserTwoFactorDisabledEvent(Id, OrganizationId, DateTime.UtcNow));

    return Result.Success();
}

#endregion
```

---

## 4.2 User Session Entity

**File**: `Trap-Intel.Domain/Identity/Entities/UserSession.cs`

```csharp
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Identity.Entities;

/// <summary>
/// Represents an active user session for device tracking and management.
/// </summary>
public class UserSession : Entity<Guid>
{
    private UserSession() { }

    private UserSession(
        Guid id,
        Guid userId,
        Guid refreshTokenId,
        string deviceFingerprint,
        string? deviceName,
        string? browser,
        string? operatingSystem,
        string? ipAddress,
        string? location)
        : base(id)
    {
        UserId = userId;
        RefreshTokenId = refreshTokenId;
        DeviceFingerprint = deviceFingerprint;
        DeviceName = deviceName;
        Browser = browser;
        OperatingSystem = operatingSystem;
        IpAddress = ipAddress;
        Location = location;
        CreatedAt = DateTime.UtcNow;
        LastActivityAt = DateTime.UtcNow;
        IsActive = true;
    }

    public Guid UserId { get; private set; }
    public Guid RefreshTokenId { get; private set; }
    public string DeviceFingerprint { get; private set; } = string.Empty;
    public string? DeviceName { get; private set; }
    public string? Browser { get; private set; }
    public string? OperatingSystem { get; private set; }
    public string? IpAddress { get; private set; }
    public string? Location { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime LastActivityAt { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime? TerminatedAt { get; private set; }
    public string? TerminationReason { get; private set; }

    /// <summary>
    /// Check if this is the current session based on IP and fingerprint.
    /// </summary>
    public bool IsCurrentSession(string ipAddress, string fingerprint)
    {
        return IpAddress == ipAddress && DeviceFingerprint == fingerprint;
    }

    public static UserSession Create(
        Guid userId,
        Guid refreshTokenId,
        string deviceFingerprint,
        string? deviceName = null,
        string? browser = null,
        string? operatingSystem = null,
        string? ipAddress = null,
        string? location = null)
    {
        return new UserSession(
            Guid.NewGuid(),
            userId,
            refreshTokenId,
            deviceFingerprint,
            deviceName,
            browser,
            operatingSystem,
            ipAddress,
            location);
    }

    public void UpdateActivity()
    {
        LastActivityAt = DateTime.UtcNow;
    }

    public void Terminate(string reason)
    {
        IsActive = false;
        TerminatedAt = DateTime.UtcNow;
        TerminationReason = reason;
    }
}
```

---

## 4.3 Deliverables Checklist

### Domain Layer
- [ ] Add 2FA properties and methods to `User`
- [ ] Create `UserSession` entity
- [ ] Create `BackupCode` entity
- [ ] Add 2FA domain events

### Infrastructure Layer
- [ ] Implement `ITwoFactorService`
- [ ] Implement session repository
- [ ] Add QRCoder and Otp.NET packages
- [ ] Create EF configurations

### Application Layer
- [ ] Create `Enable2FACommand` and handler
- [ ] Create `Verify2FACommand` and handler
- [ ] Create `Disable2FACommand` and handler
- [ ] Create `GetUserSessionsQuery` and handler
- [ ] Create `TerminateSessionCommand` and handler

### API Layer
- [ ] Add 2FA endpoints to AccountController
- [ ] Add sessions endpoint

---

# Phase 5: External Authentication (SSO)

## ?? Duration: 5-7 Days

## ?? Objectives
- Implement Google OAuth2 login
- Implement Microsoft OAuth2 login
- Link external accounts to existing users
- Handle first-time external login

---

## 5.1 External Login Provider Entity

**File**: `Trap-Intel.Domain/Identity/Entities/ExternalLoginProvider.cs`

```csharp
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Identity.Entities;

/// <summary>
/// Represents a linked external login provider (Google, Microsoft, etc.).
/// </summary>
public class ExternalLoginProvider : Entity<Guid>
{
    private ExternalLoginProvider() { }

    private ExternalLoginProvider(
        Guid id,
        Guid userId,
        string providerName,
        string providerUserId,
        string? email)
        : base(id)
    {
        UserId = userId;
        ProviderName = providerName;
        ProviderUserId = providerUserId;
        ProviderEmail = email;
        LinkedAt = DateTime.UtcNow;
    }

    public Guid UserId { get; private set; }
    public string ProviderName { get; private set; } = string.Empty;
    public string ProviderUserId { get; private set; } = string.Empty;
    public string? ProviderEmail { get; private set; }
    public DateTime LinkedAt { get; private set; }
    public DateTime? LastUsedAt { get; private set; }

    public static ExternalLoginProvider Create(
        Guid userId,
        string providerName,
        string providerUserId,
        string? email = null)
    {
        return new ExternalLoginProvider(
            Guid.NewGuid(),
            userId,
            providerName,
            providerUserId,
            email);
    }

    public void RecordUsage()
    {
        LastUsedAt = DateTime.UtcNow;
    }
}
```

---

## 5.2 OAuth Configuration

**File**: `Trap-Intel.Infrastructure/Authentication/Configuration/OAuthSettings.cs`

```csharp
namespace Trap_Intel.Infrastructure.Authentication.Configuration;

/// <summary>
/// OAuth provider configuration.
/// </summary>
public sealed class OAuthSettings
{
    public const string SectionName = "OAuth";

    public GoogleOAuthSettings Google { get; set; } = new();
    public MicrosoftOAuthSettings Microsoft { get; set; } = new();
}

public sealed class GoogleOAuthSettings
{
    public bool Enabled { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}

public sealed class MicrosoftOAuthSettings
{
    public bool Enabled { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string TenantId { get; set; } = "common"; // "common" for multi-tenant
}
```

---

## 5.3 API Configuration

**Add to `Program.cs`**:

```csharp
// OAuth Configuration
var oauthSettings = builder.Configuration.GetSection(OAuthSettings.SectionName).Get<OAuthSettings>();

builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        if (oauthSettings?.Google.Enabled == true)
        {
            options.ClientId = oauthSettings.Google.ClientId;
            options.ClientSecret = oauthSettings.Google.ClientSecret;
        }
    })
    .AddMicrosoftAccount(options =>
    {
        if (oauthSettings?.Microsoft.Enabled == true)
        {
            options.ClientId = oauthSettings.Microsoft.ClientId;
            options.ClientSecret = oauthSettings.Microsoft.ClientSecret;
        }
    });
```

---

## 5.4 Deliverables Checklist

### Domain Layer
- [ ] Create `ExternalLoginProvider` entity
- [ ] Add repository interface
- [ ] Add domain events

### Infrastructure Layer
- [ ] Implement OAuth callback handlers
- [ ] Implement external login service
- [ ] Create EF configurations
- [ ] Add OAuth packages

### Application Layer
- [ ] Create `ExternalLoginCommand` and handler
- [ ] Create `LinkExternalAccountCommand` and handler
- [ ] Create `UnlinkExternalAccountCommand` and handler

### API Layer
- [ ] Add OAuth callback endpoints
- [ ] Add account linking endpoints

---

# Phase 6: API Security & Rate Limiting

## ?? Duration: 3-4 Days

## ?? Objectives
- Implement rate limiting for authentication endpoints
- Implement IP-based blocking
- Add request throttling
- Security headers

---

## 6.1 Rate Limiting Configuration

**File**: `Trap-Intel.Infrastructure/Authentication/Configuration/RateLimitSettings.cs`

```csharp
namespace Trap_Intel.Infrastructure.Authentication.Configuration;

/// <summary>
/// Rate limiting configuration for security.
/// </summary>
public sealed class RateLimitSettings
{
    public const string SectionName = "RateLimiting";

    /// <summary>
    /// Login endpoint rate limit.
    /// </summary>
    public EndpointRateLimit Login { get; set; } = new()
    {
        PermitLimit = 5,
        WindowSeconds = 60, // 5 attempts per minute
        QueueLimit = 0
    };

    /// <summary>
    /// Register endpoint rate limit.
    /// </summary>
    public EndpointRateLimit Register { get; set; } = new()
    {
        PermitLimit = 3,
        WindowSeconds = 300, // 3 registrations per 5 minutes
        QueueLimit = 0
    };

    /// <summary>
    /// Password reset endpoint rate limit.
    /// </summary>
    public EndpointRateLimit PasswordReset { get; set; } = new()
    {
        PermitLimit = 3,
        WindowSeconds = 300, // 3 reset requests per 5 minutes
        QueueLimit = 0
    };

    /// <summary>
    /// Token refresh endpoint rate limit.
    /// </summary>
    public EndpointRateLimit TokenRefresh { get; set; } = new()
    {
        PermitLimit = 10,
        WindowSeconds = 60, // 10 refreshes per minute
        QueueLimit = 2
    };

    /// <summary>
    /// Global API rate limit per user.
    /// </summary>
    public EndpointRateLimit GlobalPerUser { get; set; } = new()
    {
        PermitLimit = 100,
        WindowSeconds = 60, // 100 requests per minute
        QueueLimit = 10
    };
}

public sealed class EndpointRateLimit
{
    public int PermitLimit { get; set; }
    public int WindowSeconds { get; set; }
    public int QueueLimit { get; set; }
}
```

---

## 6.2 Rate Limiting Implementation

**Add to `Program.cs`**:

```csharp
using System.Threading.RateLimiting;

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    var rateLimitSettings = builder.Configuration
        .GetSection(RateLimitSettings.SectionName)
        .Get<RateLimitSettings>() ?? new RateLimitSettings();

    // Login rate limiter
    options.AddPolicy("login", context =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = rateLimitSettings.Login.PermitLimit,
                Window = TimeSpan.FromSeconds(rateLimitSettings.Login.WindowSeconds),
                SegmentsPerWindow = 4,
                QueueLimit = rateLimitSettings.Login.QueueLimit,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            }));

    // Registration rate limiter
    options.AddPolicy("register", context =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = rateLimitSettings.Register.PermitLimit,
                Window = TimeSpan.FromSeconds(rateLimitSettings.Register.WindowSeconds),
                SegmentsPerWindow = 4,
                QueueLimit = rateLimitSettings.Register.QueueLimit
            }));

    // Customize rejection response
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";
        
        var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterValue)
            ? retryAfterValue.TotalSeconds
            : 60;
        
        context.HttpContext.Response.Headers.RetryAfter = retryAfter.ToString();
        
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            error = "TooManyRequests",
            message = "Rate limit exceeded. Please try again later.",
            retryAfterSeconds = retryAfter
        }, token);
    };
});

// Use rate limiter
app.UseRateLimiter();
```

---

## 6.3 Security Headers Middleware

**File**: `Trap-Intel.Api/Middleware/SecurityHeadersMiddleware.cs`

```csharp
namespace Trap_Intel.Api.Middleware;

/// <summary>
/// Middleware to add security headers to all responses.
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Prevent clickjacking
        context.Response.Headers["X-Frame-Options"] = "DENY";
        
        // Prevent MIME type sniffing
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        
        // Enable XSS filtering
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
        
        // Content Security Policy
        context.Response.Headers["Content-Security-Policy"] = 
            "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline';";
        
        // Referrer Policy
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        
        // Permissions Policy (formerly Feature Policy)
        context.Response.Headers["Permissions-Policy"] = 
            "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()";
        
        // HSTS (only in production)
        if (!context.Request.Host.Host.Contains("localhost"))
        {
            context.Response.Headers["Strict-Transport-Security"] = 
                "max-age=31536000; includeSubDomains; preload";
        }

        await _next(context);
    }
}
```

---

## 6.4 Deliverables Checklist

### Infrastructure Layer
- [ ] Create `RateLimitSettings` configuration
- [ ] Configure rate limiting policies
- [ ] Implement IP blocking service (optional)

### API Layer
- [ ] Add rate limiting middleware
- [ ] Add security headers middleware
- [ ] Apply rate limit attributes to controllers

### Testing
- [ ] Test rate limiting under load
- [ ] Verify security headers

---

# Phase 7: Monitoring & Compliance

## ?? Duration: 3-4 Days

## ?? Objectives
- Implement comprehensive security audit logging
- Create security alerts
- Implement compliance reports
- Add monitoring dashboards integration

---

## 7.1 Security Audit Events

**File**: `Trap-Intel.Domain/Identity/Events/SecurityAuditEvents.cs`

```csharp
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Identity.Events;

/// <summary>
/// Security-specific audit events for compliance.
/// </summary>

public record SuspiciousLoginDetectedEvent(
    Guid UserId,
    Guid OrganizationId,
    string IpAddress,
    string Reason, // "New device", "New location", "Multiple failures"
    DateTime OccurredOn) : IDomainEvent;

public record TokenReuseDetectedEvent(
    Guid UserId,
    Guid OrganizationId,
    Guid TokenFamilyId,
    string IpAddress,
    DateTime OccurredOn) : IDomainEvent;

public record BruteForceAttemptDetectedEvent(
    string TargetEmail,
    string IpAddress,
    int AttemptCount,
    DateTime OccurredOn) : IDomainEvent;

public record PrivilegeEscalationEvent(
    Guid UserId,
    Guid OrganizationId,
    Guid PerformedByUserId,
    UserRole OldRole,
    UserRole NewRole,
    DateTime OccurredOn) : IDomainEvent;

public record SensitiveDataAccessEvent(
    Guid UserId,
    Guid OrganizationId,
    string ResourceType,
    Guid ResourceId,
    string AccessType, // "View", "Export", "Delete"
    DateTime OccurredOn) : IDomainEvent;
```

---

## 7.2 Security Alert Service

**File**: `Trap-Intel.Infrastructure/Security/SecurityAlertService.cs`

```csharp
using MediatR;
using Microsoft.Extensions.Logging;
using Trap_Intel.Domain.Identity.Events;

namespace Trap_Intel.Infrastructure.Security;

/// <summary>
/// Service for handling security alerts and notifications.
/// </summary>
public interface ISecurityAlertService
{
    Task RaiseAlertAsync(SecurityAlertType type, Guid? userId, string message, Dictionary<string, object>? metadata = null);
}

public enum SecurityAlertType
{
    SuspiciousLogin,
    BruteForceAttempt,
    TokenReuse,
    PrivilegeEscalation,
    AccountLockout,
    PasswordReset,
    TwoFactorDisabled,
    MassSessionTermination
}

public sealed class SecurityAlertService : ISecurityAlertService
{
    private readonly ILogger<SecurityAlertService> _logger;
    private readonly IPublisher _publisher;

    public SecurityAlertService(
        ILogger<SecurityAlertService> logger,
        IPublisher publisher)
    {
        _logger = logger;
        _publisher = publisher;
    }

    public async Task RaiseAlertAsync(
        SecurityAlertType type,
        Guid? userId,
        string message,
        Dictionary<string, object>? metadata = null)
    {
        // Log the alert
        _logger.LogWarning(
            "Security Alert [{AlertType}]: {Message} | UserId: {UserId} | Metadata: {@Metadata}",
            type,
            message,
            userId,
            metadata);

        // TODO: Publish to notification system
        // TODO: Send email to security team for critical alerts
        // TODO: Integrate with SIEM system

        await Task.CompletedTask;
    }
}
```

---

## 7.3 Compliance Data Export

**File**: `Trap-Intel.Infrastructure/Security/ComplianceExportService.cs`

```csharp
namespace Trap_Intel.Infrastructure.Security;

/// <summary>
/// Service for GDPR/compliance data exports.
/// </summary>
public interface IComplianceExportService
{
    /// <summary>
    /// Export all user data (GDPR Article 20 - Right to data portability).
    /// </summary>
    Task<UserDataExport> ExportUserDataAsync(Guid userId);

    /// <summary>
    /// Delete all user data (GDPR Article 17 - Right to erasure).
    /// </summary>
    Task<bool> DeleteUserDataAsync(Guid userId, string reason);
}

public record UserDataExport(
    Guid UserId,
    string Email,
    DateTime ExportedAt,
    PersonalData PersonalData,
    IReadOnlyList<LoginHistory> LoginHistory,
    IReadOnlyList<SessionInfo> Sessions,
    IReadOnlyList<AuditEntry> AuditTrail);

public record PersonalData(
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    DateTime CreatedAt,
    Dictionary<string, object> Preferences);

public record LoginHistory(
    DateTime Timestamp,
    string IpAddress,
    string? Location,
    bool Successful);

public record SessionInfo(
    DateTime CreatedAt,
    string DeviceName,
    string Browser,
    bool IsActive);

public record AuditEntry(
    DateTime Timestamp,
    string Action,
    string ResourceType,
    string Details);
```

---

## 7.4 Deliverables Checklist

### Domain Layer
- [ ] Create security audit events

### Infrastructure Layer
- [ ] Implement `ISecurityAlertService`
- [ ] Implement `IComplianceExportService`
- [ ] Create audit event handlers

### Application Layer
- [ ] Create security report queries
- [ ] Create compliance export commands

### Integration
- [ ] Integrate with existing `AuditTrail` entity
- [ ] Set up alert notifications (email, webhook)

---

# Security Considerations

## ?? Critical Security Requirements

### 1. Password Security
- ? BCrypt with 100,000+ iterations
- ? Minimum 8 characters, complexity requirements
- ? Common password list check
- ? Password history (prevent reuse)
- ?? Consider: Pwned Passwords API integration

### 2. Token Security
- ? Short-lived access tokens (15 min)
- ? Secure refresh token rotation
- ? Token family for reuse detection
- ? Tokens stored as hashes only
- ? HTTPS only (Secure cookie flag)

### 3. Authentication
- ? Rate limiting on login
- ? Account lockout after failures
- ? 2FA support (TOTP)
- ? Suspicious login detection
- ?? Consider: WebAuthn/FIDO2 support

### 4. Session Management
- ? Session tracking per device
- ? Remote session termination
- ? Session timeout
- ? Single active session option

### 5. Data Protection
- ? Sensitive data encrypted at rest
- ? Security stamp for token invalidation
- ? Audit logging
- ? GDPR compliance tools

---

# Non-Functional Requirements

## Performance
| Metric | Target |
|--------|--------|
| Login response time | < 200ms |
| Token validation | < 10ms |
| Token refresh | < 100ms |
| Concurrent users | 10,000+ |

## Scalability
- Stateless JWT (no session store needed)
- Refresh tokens in database (can shard by user)
- Rate limiting at edge (Redis-backed)

## Availability
- Token service: 99.9% uptime
- Graceful degradation on database issues
- Circuit breaker for external services

## Security Compliance
- SOC2 Type II ready
- GDPR compliant
- HIPAA considerations
- OWASP Top 10 mitigation

---

# Implementation Timeline

```
Week 1: Phase 1 (Core Authentication)
??? Day 1-2: Domain changes + Password hashing
??? Day 3-4: JWT service + Basic login
??? Day 5: Testing + Bug fixes

Week 2: Phase 2 (Token Management)
??? Day 1-2: RefreshToken entity + Rotation
??? Day 3-4: Reuse detection + Service
??? Day 5: Testing + Integration

Week 3: Phase 3 (Security Hardening)
??? Day 1-2: Email verification
??? Day 3: Password reset
??? Day 4-5: Password policy + Testing

Week 4: Phase 4 (Advanced Features)
??? Day 1-2: 2FA implementation
??? Day 3-4: User sessions
??? Day 5: Device management

Week 5: Phase 5-7 (SSO + Security + Monitoring)
??? Day 1-2: OAuth providers
??? Day 3: Rate limiting
??? Day 4: Security headers
??? Day 5: Audit logging + Final testing
```

---

# Appendix: File Structure

```
Trap-Intel.Domain/
??? Identity/
?   ??? User.cs (updated)
?   ??? Entities/
?   ?   ??? RefreshToken.cs
?   ?   ??? UserSession.cs
?   ?   ??? EmailVerificationToken.cs
?   ?   ??? PasswordResetToken.cs
?   ?   ??? ExternalLoginProvider.cs
?   ?   ??? BackupCode.cs
?   ??? Events/
?   ?   ??? IdentityEvents.cs (updated)
?   ?   ??? SecurityAuditEvents.cs
?   ??? Policies/
?   ?   ??? UserPermissionPolicy.cs (existing)
?   ?   ??? PasswordPolicy.cs
?   ?   ??? SessionPolicy.cs
?   ??? Repositories/
?       ??? IUserRepository.cs (existing)
?       ??? IRefreshTokenRepository.cs
?       ??? IUserSessionRepository.cs

Trap-Intel.Infrastructure/
??? Authentication/
?   ??? Configuration/
?   ?   ??? JwtSettings.cs
?   ?   ??? LockoutSettings.cs
?   ?   ??? OAuthSettings.cs
?   ?   ??? RateLimitSettings.cs
?   ??? Services/
?   ?   ??? PasswordHashingService.cs
?   ?   ??? JwtTokenService.cs
?   ?   ??? RefreshTokenService.cs
?   ?   ??? TwoFactorService.cs
?   ?   ??? AuthenticationService.cs
?   ??? Repositories/
?       ??? RefreshTokenRepository.cs
?       ??? UserSessionRepository.cs
??? Security/
?   ??? SecurityAlertService.cs
?   ??? ComplianceExportService.cs

Trap-Intel.Api/
??? Controllers/
?   ??? AuthController.cs
?   ??? AccountController.cs
??? Middleware/
?   ??? SecurityHeadersMiddleware.cs
?   ??? JwtValidationMiddleware.cs
```

---

**End of Master Plan Document**

*This document serves as the complete technical specification for implementing IAM in Trap-Intel. Each phase can be executed independently while building upon previous phases.*
