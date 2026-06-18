using Application.Abstractions.Messaging;

namespace Application.Users.UpdateCurrentUser;

/// <summary>
/// Command to update the current user's profile information.
/// </summary>
/// <param name="FirstName">Updated first name.</param>
/// <param name="LastName">Updated last name.</param>
public sealed record UpdateCurrentUserCommand(
    string FirstName,
    string LastName) : ICommand;
