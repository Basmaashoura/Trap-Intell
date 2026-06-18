using SharedKernel;

namespace Domain.Accounts;

public sealed record AccountCreatedDomainEvent(Guid AccountId) : IDomainEvent;
