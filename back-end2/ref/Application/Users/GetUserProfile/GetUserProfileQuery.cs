using Application.Abstractions.Messaging;
using Application.Users.Common;

namespace Application.Users.GetUserProfile;

/// <summary>
/// Query to get a user's profile.
/// </summary>
/// <param name="UserId">The ID of the user.</param>
/// <param name="IncludeSensitiveData">Whether to include sensitive data (admin only).</param>
public sealed record GetUserProfileQuery(
    Guid UserId,
    bool IncludeSensitiveData = false) : IQuery<UserProfileResponse>;
