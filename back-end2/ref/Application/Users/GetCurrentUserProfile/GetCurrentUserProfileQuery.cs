using Application.Abstractions.Messaging;
using Application.Users.Common;

namespace Application.Users.GetCurrentUserProfile;

/// <summary>
/// Query to get the current user's profile.
/// </summary>
public sealed record GetCurrentUserProfileQuery : IQuery<UserProfileResponse>;
