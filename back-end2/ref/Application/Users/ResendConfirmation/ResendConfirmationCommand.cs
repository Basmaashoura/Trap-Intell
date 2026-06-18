using Application.Abstractions.Messaging;

namespace Application.Users.ResendConfirmation;

/// <summary>
/// Command to resend email confirmation link.
/// </summary>
/// <param name="Email">The email address to send confirmation to.</param>
public sealed record ResendConfirmationCommand(string Email) : ICommand;
