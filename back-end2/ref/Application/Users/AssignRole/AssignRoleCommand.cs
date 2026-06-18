using Application.Abstractions.Messaging;

namespace Application.Users.AssignRole;

/// <summary>
/// Command to assign a role to a user.
/// </summary>
/// <param name="UserId">The ID of the user.</param>
/// <param name="RoleId">The ID of the role to assign.</param>
public sealed record AssignRoleCommand(Guid UserId, Guid RoleId) : ICommand;
