using Application.Abstractions.Messaging;

namespace Application.Users.RevokeAllSessions;

/// <summary>
/// Command to revoke all sessions for a user.
/// </summary>
/// <param name="UserId">The ID of the user.</param>
/// <param name="ExcludeCurrentSession">Whether to exclude the current session from revocation.</param>
public sealed record RevokeAllSessionsCommand(
    Guid UserId,
    bool ExcludeCurrentSession = false) : ICommand;
