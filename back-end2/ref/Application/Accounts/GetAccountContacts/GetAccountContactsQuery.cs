using Application.Abstractions.Messaging;
using Application.Accounts.Common;

namespace Application.Accounts.GetAccountContacts;

/// <summary>
/// Query to get contacts for an account.
/// </summary>
/// <param name="AccountId">The ID of the account.</param>
public sealed record GetAccountContactsQuery(Guid AccountId) : IQuery<IReadOnlyList<AccountContactResponse>>;
