using Application.Abstractions.Messaging;

namespace Application.Users.ResetPassword;

/// <summary>
/// Command to reset a user's password using a reset token.
/// </summary>
/// <param name="Email">The email address of the user.</param>
/// <param name="Token">The password reset token.</param>
/// <param name="NewPassword">The new password to set.</param>
public sealed record ResetPasswordCommand(
    string Email,
    string Token,
    string NewPassword) : ICommand;
