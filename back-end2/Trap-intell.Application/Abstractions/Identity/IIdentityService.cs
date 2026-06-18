using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Identity;

namespace Trap_Intel.Application.Abstractions.Identity;

/// <summary>
/// A port (adapter interface) in the Application layer that bridges 
/// our Domain User with the specific Identity store mechanism 
/// (ASP.NET Core Identity in Infrastructure).
/// </summary>
public interface IIdentityService
{
    /// <summary>
    /// Validates the given email and password and returns the Domain User if valid.
    /// Manages lockout logic and hashing upgrades internally.
    /// </summary>
    Task<Result<User>> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new user securely.
    /// </summary>
    Task<Result> RegisterUserAsync(User user, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes the user's password.
    /// </summary>
    Task<Result> ChangePasswordAsync(User user, string currentPassword, string newPassword, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a password reset token for a user securely.
    /// </summary>
    Task<Result<string>> GeneratePasswordResetTokenAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to securely reset a user's password using a valid token.
    /// </summary>
    Task<Result> ResetPasswordAsync(string email, string token, string newPassword, CancellationToken cancellationToken = default);
}
