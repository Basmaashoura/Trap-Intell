using Application.Abstractions.Messaging;
using Application.Users.Common;

namespace Application.Users.GetCurrentUserSessions;

/// <summary>
/// Query to get the current user's active sessions.
/// </summary>
/// <param name="CurrentSessionToken">The current session token hash (to mark current session).</param>
public sealed record GetCurrentUserSessionsQuery(
    string? CurrentSessionToken = null) : IQuery<IReadOnlyList<UserSessionResponse>>;
