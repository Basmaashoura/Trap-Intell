using Application.Abstractions.Messaging;

namespace Application.Users.UpdateUser;

/// <summary>
/// Command to update a user's information (admin operation).
/// </summary>
/// <param name="UserId">The ID of the user to update.</param>
/// <param name="FirstName">Updated first name.</param>
/// <param name="LastName">Updated last name.</param>
/// <param name="Email">Updated email address.</param>
public sealed record UpdateUserCommand(
    Guid UserId,
    string FirstName,
    string LastName,
    string Email) : ICommand;
