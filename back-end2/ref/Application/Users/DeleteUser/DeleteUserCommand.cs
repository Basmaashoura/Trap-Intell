using Application.Abstractions.Messaging;

namespace Application.Users.DeleteUser;

/// <summary>
/// Command to permanently delete a user.
/// </summary>
/// <param name="UserId">The ID of the user to delete.</param>
public sealed record DeleteUserCommand(Guid UserId) : ICommand;
