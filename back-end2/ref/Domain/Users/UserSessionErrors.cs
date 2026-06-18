using SharedKernel;

namespace Domain.Users;

public static class UserSessionErrors
{
    public static Error NotFound(Guid sessionId) => Error.NotFound(
        "UserSessions.NotFound",
        $"The session with the Id = '{sessionId}' was not found");

    public static readonly Error SessionExpired = Error.Failure(
        "UserSessions.Expired",
        "The session has expired");

    public static readonly Error SessionRevoked = Error.Failure(
        "UserSessions.Revoked",
        "The session has been revoked");

    public static readonly Error InvalidToken = Error.Failure(
        "UserSessions.InvalidToken",
        "Invalid session token");

    public static readonly Error InvalidRefreshToken = Error.Failure(
        "UserSessions.InvalidRefreshToken",
        "Invalid refresh token");

    public static readonly Error MaxSessionsReached = Error.Failure(
        "UserSessions.MaxSessionsReached",
        "Maximum number of active sessions reached");
}
