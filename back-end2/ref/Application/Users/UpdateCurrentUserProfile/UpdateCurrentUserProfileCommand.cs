using Application.Abstractions.Messaging;

namespace Application.Users.UpdateCurrentUserProfile;

/// <summary>
/// Command to update the current user's profile.
/// </summary>
#pragma warning disable CA1054 // URI parameters should not be strings
public sealed record UpdateCurrentUserProfileCommand(
    string? PhoneNumber,
    string? AvatarUrl,
    string? Bio,
    IReadOnlyList<string>? Skills) : ICommand;
#pragma warning restore CA1054
