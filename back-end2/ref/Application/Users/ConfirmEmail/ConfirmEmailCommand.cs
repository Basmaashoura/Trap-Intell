using Application.Abstractions.Messaging;

namespace Application.Users.ConfirmEmail;

/// <summary>
/// Command to confirm a user's email address.
/// </summary>
/// <param name="Email">The email address to confirm.</param>
/// <param name="Token">The confirmation token sent via email.</param>
public sealed record ConfirmEmailCommand(
    string Email,
    string Token) : ICommand;
