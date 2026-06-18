using Application.Abstractions.Identity;
using Application.Abstractions.Messaging;

namespace Application.Users.Login2FA;

/// <summary>
/// Command for completing login with 2FA code.
/// Used when initial login returns TwoFactorRequired.
/// </summary>
/// <param name="Email">The user's email address.</param>
/// <param name="Code">The 6-digit 2FA code from authenticator app.</param>
public sealed record Login2FACommand(
    string Email,
    string Code) : ICommand<TokenResponse>;
