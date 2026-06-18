namespace Application.Abstractions.Identity;

/// <summary>
/// Information returned when setting up two-factor authentication.
/// </summary>
#pragma warning disable CA1054 // QR code URI is intentionally a string for otpauth:// format
public sealed record TwoFactorSetupInfo(
    string SharedSecret,
    string QrCodeUri);
#pragma warning restore CA1054
