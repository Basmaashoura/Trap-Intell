using Application.Abstractions.Messaging;

namespace Application.Users.RevokeSession;

/// <summary>
/// Command to revoke a specific user session.
/// </summary>
/// <param name="UserId">The ID of the user.</param>
/// <param name="SessionId">The ID of the session to revoke.</param>
public sealed record RevokeSessionCommand(Guid UserId, Guid SessionId) : ICommand;
