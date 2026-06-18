using Application.Abstractions.Messaging;

namespace Application.Users.ActivateUser;

/// <summary>
/// Command to activate a deactivated user.
/// </summary>
/// <param name="UserId">The ID of the user to activate.</param>
public sealed record ActivateUserCommand(Guid UserId) : ICommand;
