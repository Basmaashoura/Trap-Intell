using Application.Abstractions.Messaging;

namespace Application.Users.ForgotPassword;

/// <summary>
/// Command to request a password reset.
/// </summary>
/// <param name="Email">The email address of the user requesting password reset.</param>
public sealed record ForgotPasswordCommand(string Email) : ICommand;
