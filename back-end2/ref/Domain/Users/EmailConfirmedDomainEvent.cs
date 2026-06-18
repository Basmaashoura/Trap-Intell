using SharedKernel;

namespace Domain.Users;

/// <summary>
/// Domain event raised when a user's email is confirmed.
/// </summary>
public sealed record EmailConfirmedDomainEvent(Guid UserId, string Email) : IDomainEvent;
