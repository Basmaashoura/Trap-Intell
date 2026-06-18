using Application.Abstractions.Messaging;
using Application.Users.Common;

namespace Application.Users.GetUserSessions;

/// <summary>
/// Query to get user's active sessions.
/// </summary>
/// <param name="UserId">The ID of the user.</param>
/// <param name="CurrentSessionToken">The current session token hash (to mark current session).</param>
public sealed record GetUserSessionsQuery(
    Guid UserId,
    string? CurrentSessionToken = null) : IQuery<IReadOnlyList<UserSessionResponse>>;
