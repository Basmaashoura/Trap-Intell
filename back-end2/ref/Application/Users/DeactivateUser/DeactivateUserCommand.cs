using Application.Abstractions.Messaging;

namespace Application.Users.DeactivateUser;

/// <summary>
/// Command to deactivate a user (Offboarding Protocol).
/// </summary>
/// <param name="UserId">The ID of the user to deactivate.</param>
public sealed record DeactivateUserCommand(Guid UserId) : ICommand;
