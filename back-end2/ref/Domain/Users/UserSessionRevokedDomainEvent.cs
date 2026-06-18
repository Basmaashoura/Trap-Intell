using SharedKernel;

namespace Domain.Users;

/// <summary>
/// Raised when a user session is revoked (logout or force logout).
/// </summary>
public sealed record UserSessionRevokedDomainEvent(
    Guid UserId,
    Guid SessionId,
    string Reason) : IDomainEvent;
