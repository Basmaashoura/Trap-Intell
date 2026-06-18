using Application.Abstractions.Messaging;
using Domain.Common;

namespace Application.Users.UpdateUserProfile;

/// <summary>
/// Command to update a user's profile (Admin).
/// Includes sensitive fields like HourlyCost and InternalJobTitle.
/// </summary>
#pragma warning disable CA1054 // URI parameters should not be strings
public sealed record UpdateUserProfileCommand(
    Guid UserId,
    Department Department,
    string? DisplayJobTitle,
    string? InternalJobTitle,
    decimal? HourlyCost,
    string? PhoneNumber,
    DateTime? HireDate,
    string? AvatarUrl,
    string? Bio,
    IReadOnlyList<string>? Skills) : ICommand;
#pragma warning restore CA1054
