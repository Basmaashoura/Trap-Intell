using Application.Abstractions.Data;
using Application.Abstractions.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Infrastructure.Identity;

/// <summary>
/// Implementation of IIdentityService using ASP.NET Identity.
/// </summary>
internal sealed class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationIdentityUser> _userManager;
    private readonly SignInManager<ApplicationIdentityUser> _signInManager;
    private readonly RoleManager<ApplicationIdentityRole> _roleManager;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly IdentityDbContext _context;
    private readonly IApplicationDbContext _appContext;

    public IdentityService(
        UserManager<ApplicationIdentityUser> userManager,
        SignInManager<ApplicationIdentityUser> signInManager,
        RoleManager<ApplicationIdentityRole> roleManager,
        IJwtTokenGenerator tokenGenerator,
        IdentityDbContext context,
        IApplicationDbContext appContext)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _tokenGenerator = tokenGenerator;
        _context = context;
        _appContext = appContext;
    }

    #region User Management

    public async Task<Result<Guid>> CreateUserAsync(
        Guid domainUserId,
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        // Check if user already exists
        if (await _userManager.FindByEmailAsync(email) is not null)
        {
            return Result.Failure<Guid>(IdentityErrors.EmailAlreadyExists);
        }

        var user = ApplicationIdentityUser.Create(domainUserId, email);
        IdentityResult result = await _userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            IdentityError? error = result.Errors.FirstOrDefault();
            return Result.Failure<Guid>(
                IdentityErrors.FromIdentityError(error?.Code ?? "Unknown", error?.Description ?? "Failed to create user"));
        }

        return user.Id;
    }

    public async Task<Result> UpdateEmailAsync(
        Guid domainUserId,
        string newEmail,
        CancellationToken cancellationToken = default)
    {
        ApplicationIdentityUser? user = await FindByDomainUserIdAsync(domainUserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure(IdentityErrors.UserNotFound);
        }

        // Check if new email already exists
        ApplicationIdentityUser? existingUser = await _userManager.FindByEmailAsync(newEmail);
        if (existingUser is not null && existingUser.Id != user.Id)
        {
            return Result.Failure(IdentityErrors.EmailAlreadyExists);
        }

        string token = await _userManager.GenerateChangeEmailTokenAsync(user, newEmail);
        IdentityResult result = await _userManager.ChangeEmailAsync(user, newEmail, token);

        if (!result.Succeeded)
        {
            IdentityError? error = result.Errors.FirstOrDefault();
            return Result.Failure(
                IdentityErrors.FromIdentityError(error?.Code ?? "Unknown", error?.Description ?? "Failed to update email"));
        }

        // Update username to match email
        user.UserName = newEmail;
        user.NormalizedUserName = newEmail.ToUpperInvariant();
        await _userManager.UpdateAsync(user);

        return Result.Success();
    }

    public async Task<Result> DeleteUserAsync(
        Guid domainUserId,
        CancellationToken cancellationToken = default)
    {
        ApplicationIdentityUser? user = await FindByDomainUserIdAsync(domainUserId, cancellationToken);
        if (user is null)
        {
            return Result.Success(); // Already deleted
        }

        IdentityResult result = await _userManager.DeleteAsync(user);

        return result.Succeeded
            ? Result.Success()
            : Result.Failure(IdentityErrors.FromIdentityError("DeleteFailed", "Failed to delete user"));
    }

    public async Task<bool> UserExistsAsync(
        Guid domainUserId,
        CancellationToken cancellationToken = default)
    {
        return await FindByDomainUserIdAsync(domainUserId, cancellationToken) is not null;
    }

    #endregion

    #region Authentication

    public async Task<Result<TokenResponse>> LoginAsync(
        string email,
        string password,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        ApplicationIdentityUser? user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            // SECURITY: Add constant delay to prevent email enumeration via timing attacks
            // This makes the response time similar to a valid user's failed login attempt
            await SecurityHelper.ConstantTimeDelayAsync(cancellationToken);
            return Result.Failure<TokenResponse>(IdentityErrors.InvalidCredentials);
        }

        if (!user.IsActive)
        {
            // User exists but deactivated - still add delay for consistency
            await SecurityHelper.ConstantTimeDelayAsync(cancellationToken);
            return Result.Failure<TokenResponse>(IdentityErrors.UserDeactivated);
        }

        SignInResult result = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            return Result.Failure<TokenResponse>(IdentityErrors.UserLockedOut);
        }

        if (result.RequiresTwoFactor)
        {
            return Result.Failure<TokenResponse>(IdentityErrors.TwoFactorRequired);
        }

        if (!result.Succeeded)
        {
            return Result.Failure<TokenResponse>(IdentityErrors.InvalidCredentials);
        }

        return await GenerateTokensForUserAsync(user, cancellationToken);
    }

    public async Task<Result> LogoutAsync(
        Guid domainUserId,
        CancellationToken cancellationToken = default)
    {
        ApplicationIdentityUser? user = await FindByDomainUserIdAsync(domainUserId, cancellationToken);
        if (user is null)
        {
            return Result.Success();
        }

        user.RevokeRefreshToken();
        await _userManager.UpdateAsync(user);
        await _userManager.UpdateSecurityStampAsync(user);

        return Result.Success();
    }


    public async Task<Result<TokenResponse>> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Result.Failure<TokenResponse>(IdentityErrors.InvalidRefreshToken);
        }

        // OPTIMIZED: O(1) lookup using RefreshTokenId (first 16 chars of token)
        // This avoids loading all users with active tokens into memory
        string tokenId = ApplicationIdentityUser.GetTokenId(refreshToken);

        ApplicationIdentityUser? user = await _context.Users
            .Where(u => u.RefreshTokenId == tokenId)
            .Where(u => u.RefreshTokenExpiresAt != null && u.RefreshTokenExpiresAt > DateTime.UtcNow)
            .FirstOrDefaultAsync(cancellationToken);

        // Verify the full token hash (the tokenId alone is not sufficient for security)
        if (user is null || !user.IsRefreshTokenValid(refreshToken))
        {
            return Result.Failure<TokenResponse>(IdentityErrors.InvalidRefreshToken);
        }

        if (!user.IsActive)
        {
            return Result.Failure<TokenResponse>(IdentityErrors.UserDeactivated);
        }

        // TOKEN ROTATION: Old refresh token is invalidated, new one is generated
        // This prevents refresh token reuse attacks
        return await GenerateTokensForUserAsync(user, cancellationToken);
    }

    public async Task<Result> RevokeAllTokensAsync(
        Guid domainUserId,
        CancellationToken cancellationToken = default)
    {
        ApplicationIdentityUser? user = await FindByDomainUserIdAsync(domainUserId, cancellationToken);
        if (user is null)
        {
            return Result.Success();
        }

        // Revoke refresh token
        user.RevokeRefreshToken();
        
        // Update security stamp to invalidate all existing tokens
        await _userManager.UpdateSecurityStampAsync(user);
        await _userManager.UpdateAsync(user);

        return Result.Success();
    }

    public async Task<Result> RevokeRefreshTokenAsync(
        Guid domainUserId,
        string refreshTokenHash,
        CancellationToken cancellationToken = default)
    {
        ApplicationIdentityUser? user = await FindByDomainUserIdAsync(domainUserId, cancellationToken);
        if (user is null)
        {
            return Result.Success();
        }

        // Only revoke if the hash matches (single session revocation)
        if (user.RefreshTokenHash == refreshTokenHash)
        {
            user.RevokeRefreshToken();
            await _userManager.UpdateAsync(user);
        }

        return Result.Success();
    }

    #endregion

    #region Password Management

    public async Task<Result> ChangePasswordAsync(
        Guid domainUserId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        ApplicationIdentityUser? user = await FindByDomainUserIdAsync(domainUserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure(IdentityErrors.UserNotFound);
        }

        IdentityResult result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

        if (!result.Succeeded)
        {
            IdentityError? error = result.Errors.FirstOrDefault();

            if (error?.Code == "PasswordMismatch")
            {
                return Result.Failure(IdentityErrors.PasswordMismatch);
            }

            return Result.Failure(
                IdentityErrors.FromIdentityError(error?.Code ?? "Unknown", error?.Description ?? "Failed to change password"));
        }

        // Revoke all tokens on password change
        user.RevokeRefreshToken();
        await _userManager.UpdateAsync(user);

        return Result.Success();
    }

    public async Task<Result<string>> GeneratePasswordResetTokenAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        ApplicationIdentityUser? user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            // Don't reveal that user doesn't exist - return empty token
            return string.Empty;
        }

        string token = await _userManager.GeneratePasswordResetTokenAsync(user);
        return token;
    }

    public async Task<Result> ResetPasswordAsync(
        string email,
        string token,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        ApplicationIdentityUser? user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return Result.Failure(IdentityErrors.InvalidPasswordResetToken);
        }

        IdentityResult result = await _userManager.ResetPasswordAsync(user, token, newPassword);

        if (!result.Succeeded)
        {
            return Result.Failure(IdentityErrors.InvalidPasswordResetToken);
        }

        // Revoke all tokens on password reset
        user.RevokeRefreshToken();
        await _userManager.UpdateAsync(user);

        return Result.Success();
    }

    #endregion

    #region Two-Factor Authentication

    public async Task<Result<TwoFactorSetupInfo>> EnableTwoFactorAsync(
        Guid domainUserId,
        CancellationToken cancellationToken = default)
    {
        ApplicationIdentityUser? user = await FindByDomainUserIdAsync(domainUserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure<TwoFactorSetupInfo>(IdentityErrors.UserNotFound);
        }

        if (user.TwoFactorEnabled)
        {
            return Result.Failure<TwoFactorSetupInfo>(IdentityErrors.TwoFactorAlreadyEnabled);
        }

        // Reset authenticator key
        await _userManager.ResetAuthenticatorKeyAsync(user);
        string? key = await _userManager.GetAuthenticatorKeyAsync(user);

        if (string.IsNullOrEmpty(key))
        {
            return Result.Failure<TwoFactorSetupInfo>(
                IdentityErrors.FromIdentityError("2FASetupFailed", "Failed to generate authenticator key"));
        }

        string authenticatorUri = GenerateQrCodeUri(user.Email!, key);

        return new TwoFactorSetupInfo(
            SharedSecret: key,
            QrCodeUri: authenticatorUri);
    }

    public async Task<Result> ConfirmTwoFactorAsync(
        Guid domainUserId,
        string code,
        CancellationToken cancellationToken = default)
    {
        ApplicationIdentityUser? user = await FindByDomainUserIdAsync(domainUserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure(IdentityErrors.UserNotFound);
        }

        bool isValid = await _userManager.VerifyTwoFactorTokenAsync(
            user,
            _userManager.Options.Tokens.AuthenticatorTokenProvider,
            code);

        if (!isValid)
        {
            return Result.Failure(IdentityErrors.InvalidTwoFactorCode);
        }

        await _userManager.SetTwoFactorEnabledAsync(user, true);
        return Result.Success();
    }

    public async Task<Result> DisableTwoFactorAsync(
        Guid domainUserId,
        string code,
        CancellationToken cancellationToken = default)
    {
        ApplicationIdentityUser? user = await FindByDomainUserIdAsync(domainUserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure(IdentityErrors.UserNotFound);
        }

        if (!user.TwoFactorEnabled)
        {
            return Result.Failure(IdentityErrors.TwoFactorNotEnabled);
        }

        bool isValid = await _userManager.VerifyTwoFactorTokenAsync(
            user,
            _userManager.Options.Tokens.AuthenticatorTokenProvider,
            code);

        if (!isValid)
        {
            return Result.Failure(IdentityErrors.InvalidTwoFactorCode);
        }

        await _userManager.SetTwoFactorEnabledAsync(user, false);
        await _userManager.ResetAuthenticatorKeyAsync(user);

        return Result.Success();
    }

    public async Task<Result<TokenResponse>> VerifyTwoFactorAsync(
        string email,
        string code,
        CancellationToken cancellationToken = default)
    {
        ApplicationIdentityUser? user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return Result.Failure<TokenResponse>(IdentityErrors.InvalidCredentials);
        }

        if (!user.IsActive)
        {
            return Result.Failure<TokenResponse>(IdentityErrors.UserDeactivated);
        }

        bool isValid = await _userManager.VerifyTwoFactorTokenAsync(
            user,
            _userManager.Options.Tokens.AuthenticatorTokenProvider,
            code);

        if (!isValid)
        {
            return Result.Failure<TokenResponse>(IdentityErrors.InvalidTwoFactorCode);
        }

        return await GenerateTokensForUserAsync(user, cancellationToken);
    }

    public async Task<bool> IsTwoFactorEnabledAsync(
        Guid domainUserId,
        CancellationToken cancellationToken = default)
    {
        ApplicationIdentityUser? user = await FindByDomainUserIdAsync(domainUserId, cancellationToken);
        return user?.TwoFactorEnabled ?? false;
    }

    #endregion

    #region Account Status

    public async Task<Result> DeactivateUserAsync(
        Guid domainUserId,
        CancellationToken cancellationToken = default)
    {
        ApplicationIdentityUser? user = await FindByDomainUserIdAsync(domainUserId, cancellationToken);
        if (user is null)
        {
            return Result.Success();
        }

        user.Deactivate();
        await _userManager.UpdateAsync(user);

        return Result.Success();
    }

    public async Task<Result> ActivateUserAsync(
        Guid domainUserId,
        CancellationToken cancellationToken = default)
    {
        ApplicationIdentityUser? user = await FindByDomainUserIdAsync(domainUserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure(IdentityErrors.UserNotFound);
        }

        user.Activate();
        await _userManager.UpdateAsync(user);

        return Result.Success();
    }

    public async Task<bool> IsLockedOutAsync(
        Guid domainUserId,
        CancellationToken cancellationToken = default)
    {
        ApplicationIdentityUser? user = await FindByDomainUserIdAsync(domainUserId, cancellationToken);
        if (user is null)
        {
            return false;
        }

        return await _userManager.IsLockedOutAsync(user);
    }

    #endregion

    #region Email Confirmation

    public async Task<Result<string>> GenerateEmailConfirmationTokenAsync(
        Guid domainUserId,
        CancellationToken cancellationToken = default)
    {
        ApplicationIdentityUser? user = await FindByDomainUserIdAsync(domainUserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure<string>(IdentityErrors.UserNotFound);
        }

        string token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        return token;
    }

    public async Task<Result> ConfirmEmailAsync(
        Guid domainUserId,
        string token,
        CancellationToken cancellationToken = default)
    {
        ApplicationIdentityUser? user = await FindByDomainUserIdAsync(domainUserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure(IdentityErrors.UserNotFound);
        }

        IdentityResult result = await _userManager.ConfirmEmailAsync(user, token);

        return result.Succeeded
            ? Result.Success()
            : Result.Failure(IdentityErrors.InvalidEmailConfirmationToken);
    }

    public async Task<bool> IsEmailConfirmedAsync(
        Guid domainUserId,
        CancellationToken cancellationToken = default)
    {
        ApplicationIdentityUser? user = await FindByDomainUserIdAsync(domainUserId, cancellationToken);
        return user?.EmailConfirmed ?? false;
    }

    #endregion

    #region Role Management

    public async Task<Result> CreateRoleAsync(
        Guid domainRoleId,
        string roleName,
        CancellationToken cancellationToken = default)
    {
        if (await _roleManager.RoleExistsAsync(roleName))
        {
            return Result.Success(); // Already exists
        }

        var role = ApplicationIdentityRole.Create(domainRoleId, roleName);
        IdentityResult result = await _roleManager.CreateAsync(role);

        return result.Succeeded
            ? Result.Success()
            : Result.Failure(IdentityErrors.FromIdentityError("CreateRoleFailed", "Failed to create role"));
    }

    public async Task<Result> DeleteRoleAsync(
        string roleName,
        CancellationToken cancellationToken = default)
    {
        ApplicationIdentityRole? role = await _roleManager.FindByNameAsync(roleName);
        if (role is null)
        {
            return Result.Success(); // Already deleted
        }

        IdentityResult result = await _roleManager.DeleteAsync(role);

        return result.Succeeded
            ? Result.Success()
            : Result.Failure(IdentityErrors.FromIdentityError("DeleteRoleFailed", "Failed to delete role"));
    }

    public async Task<Result> AssignRoleAsync(
        Guid domainUserId,
        string roleName,
        CancellationToken cancellationToken = default)
    {
        ApplicationIdentityUser? user = await FindByDomainUserIdAsync(domainUserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure(IdentityErrors.UserNotFound);
        }

        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            return Result.Failure(IdentityErrors.RoleNotFound);
        }

        if (await _userManager.IsInRoleAsync(user, roleName))
        {
            return Result.Success(); // Already in role
        }

        IdentityResult result = await _userManager.AddToRoleAsync(user, roleName);

        return result.Succeeded
            ? Result.Success()
            : Result.Failure(IdentityErrors.FromIdentityError("AssignRoleFailed", "Failed to assign role"));
    }

    public async Task<Result> RemoveRoleAsync(
        Guid domainUserId,
        string roleName,
        CancellationToken cancellationToken = default)
    {
        ApplicationIdentityUser? user = await FindByDomainUserIdAsync(domainUserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure(IdentityErrors.UserNotFound);
        }

        if (!await _userManager.IsInRoleAsync(user, roleName))
        {
            return Result.Success(); // Not in role
        }

        IdentityResult result = await _userManager.RemoveFromRoleAsync(user, roleName);

        return result.Succeeded
            ? Result.Success()
            : Result.Failure(IdentityErrors.FromIdentityError("RemoveRoleFailed", "Failed to remove role"));
    }

    public async Task<IReadOnlyList<string>> GetUserRolesAsync(
        Guid domainUserId,
        CancellationToken cancellationToken = default)
    {
        ApplicationIdentityUser? user = await FindByDomainUserIdAsync(domainUserId, cancellationToken);
        if (user is null)
        {
            return [];
        }

        IList<string> roles = await _userManager.GetRolesAsync(user);
        return roles.ToList();
    }

    public async Task<bool> IsInRoleAsync(
        Guid domainUserId,
        string roleName,
        CancellationToken cancellationToken = default)
    {
        ApplicationIdentityUser? user = await FindByDomainUserIdAsync(domainUserId, cancellationToken);
        if (user is null)
        {
            return false;
        }

        return await _userManager.IsInRoleAsync(user, roleName);
    }

    #endregion

    #region Private Methods

    private async Task<ApplicationIdentityUser?> FindByDomainUserIdAsync(
        Guid domainUserId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.DomainUserId == domainUserId, cancellationToken);
    }

    private async Task<Result<TokenResponse>> GenerateTokensForUserAsync(
        ApplicationIdentityUser user,
        CancellationToken cancellationToken = default)
    {
        // Get roles from Identity
        IList<string> roles = await _userManager.GetRolesAsync(user);

        // Get permissions from Domain layer
        List<string> permissions = await _appContext.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == user.DomainUserId)
            .Where(ur => ur.Role != null && ur.Role.IsActive)
            .SelectMany(ur => ur.Role!.RolePermissions)
            .Where(rp => rp.Permission != null)
            .Select(rp => rp.Permission!.Code)
            .Distinct()
            .ToListAsync(cancellationToken);

        // Get canViewSensitiveData from Domain layer
        bool canViewSensitiveData = await _appContext.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == user.DomainUserId)
            .Where(ur => ur.Role != null && ur.Role.IsActive)
            .AnyAsync(ur => ur.Role!.CanViewSensitiveData, cancellationToken);

        // Get security stamp for instant token invalidation
        string? securityStamp = await _userManager.GetSecurityStampAsync(user);

        // Generate tokens with all claims
        TokenResponse tokens = _tokenGenerator.GenerateTokens(
            user.DomainUserId,
            user.Id,
            user.Email!,
            roles.ToList(),
            permissions,
            canViewSensitiveData,
            securityStamp);

        // Store refresh token (hashed)
        user.SetRefreshToken(tokens.RefreshToken, tokens.RefreshTokenExpiresAt);
        await _userManager.UpdateAsync(user);

        return tokens;
    }

    private static string GenerateQrCodeUri(string email, string key)
    {
        const string appName = "ONEX";
        return $"otpauth://totp/{Uri.EscapeDataString(appName)}:{Uri.EscapeDataString(email)}?secret={key}&issuer={Uri.EscapeDataString(appName)}&digits=6";
    }

    #endregion
}
