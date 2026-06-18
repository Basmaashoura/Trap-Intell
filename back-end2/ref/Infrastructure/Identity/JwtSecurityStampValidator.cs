using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Identity;

/// <summary>
/// Validates SecurityStamp in JWT tokens to enable instant logout and permission revocation.
/// </summary>
internal sealed class JwtSecurityStampValidator
{
    private readonly UserManager<ApplicationIdentityUser> _userManager;
    private readonly ILogger<JwtSecurityStampValidator> _logger;

    public JwtSecurityStampValidator(
        UserManager<ApplicationIdentityUser> userManager,
        ILogger<JwtSecurityStampValidator> logger)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validates that the SecurityStamp in the JWT matches the current SecurityStamp in the database.
    /// This enables instant token invalidation on:
    /// - Logout
    /// - Password change
    /// - Role/permission changes
    /// - Account deactivation
    /// </summary>
    /// <param name="principal">The claims principal from the JWT.</param>
    /// <returns>True if the security stamp is valid, false otherwise.</returns>
    public async Task<bool> ValidateSecurityStampAsync(ClaimsPrincipal principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        // Get user ID from claims
        string? userIdClaim = principal.FindFirstValue(CustomClaimTypes.IdentityUserId)
                             ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdClaim, out Guid userId))
        {
            _logger.LogWarning("SecurityStamp validation failed: Invalid user ID in claims");
            return false;
        }

        // Get security stamp from claims
        string? securityStampClaim = principal.FindFirstValue("security_stamp");
        if (string.IsNullOrEmpty(securityStampClaim))
        {
            // Token was issued before security stamp was added to claims
            // For backward compatibility, allow it but log a warning
            _logger.LogWarning(
                "SecurityStamp not found in JWT for user {UserId}. Token may be old.",
                userId);
            return true;
        }

        // Get user from database
        ApplicationIdentityUser? user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            _logger.LogWarning(
                "SecurityStamp validation failed: User {UserId} not found",
                userId);
            return false;
        }

        // Check if user is still active
        if (!user.IsActive)
        {
            _logger.LogInformation(
                "SecurityStamp validation failed: User {UserId} is deactivated",
                userId);
            return false;
        }

        // Compare security stamps
        string? currentSecurityStamp = await _userManager.GetSecurityStampAsync(user);
        if (string.IsNullOrEmpty(currentSecurityStamp))
        {
            _logger.LogWarning(
                "SecurityStamp validation failed: No security stamp in database for user {UserId}",
                userId);
            return false;
        }

        bool isValid = string.Equals(securityStampClaim, currentSecurityStamp, StringComparison.Ordinal);

        if (!isValid)
        {
            _logger.LogInformation(
                "SecurityStamp validation failed: Stamp mismatch for user {UserId}. Token invalidated.",
                userId);
        }

        return isValid;
    }
}
