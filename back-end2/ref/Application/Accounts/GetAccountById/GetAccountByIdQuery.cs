using Application.Abstractions.Messaging;
using Application.Accounts.Common;

namespace Application.Accounts.GetAccountById;

/// <summary>
/// Query to get an account by its ID.
/// </summary>
/// <param name="AccountId">The ID of the account to retrieve.</param>
public sealed record GetAccountByIdQuery(Guid AccountId) : IQuery<AccountResponse>;
