using Application.Abstractions.Messaging;

namespace Application.Users.Enable2FA;

/// <summary>
/// Command to enable two-factor authentication for a user.
/// Returns a QR code URI and shared secret for the authenticator app.
/// </summary>
/// <param name="UserId">The ID of the user enabling 2FA.</param>
public sealed record Enable2FACommand(Guid UserId) : ICommand<Enable2FAResponse>;

/// <summary>
/// Response containing 2FA setup information.
/// </summary>
/// <param name="SharedSecret">The shared secret for the authenticator app.</param>
/// <param name="QrCodeUri">The URI for generating a QR code (otpauth:// format).</param>
#pragma warning disable CA1054 // QR code URI is intentionally a string for otpauth:// format
public sealed record Enable2FAResponse(
    string SharedSecret,
    string QrCodeUri);
#pragma warning restore CA1054
