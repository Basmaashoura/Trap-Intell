using Application.Abstractions.Messaging;

namespace Application.Users.Verify2FA;

/// <summary>
/// Command to verify two-factor authentication code.
/// This finalizes the 2FA setup after scanning the QR code.
/// </summary>
/// <param name="UserId">The ID of the user verifying 2FA.</param>
/// <param name="Code">The 6-digit verification code from authenticator app.</param>
public sealed record Verify2FACommand(
    Guid UserId,
    string Code) : ICommand;
