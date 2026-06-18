using Application.Abstractions.Messaging;

namespace Application.Users.RemoveRole;

/// <summary>
/// Command to remove a role from a user.
/// </summary>
/// <param name="UserId">The ID of the user.</param>
/// <param name="RoleId">The ID of the role to remove.</param>
public sealed record RemoveRoleCommand(Guid UserId, Guid RoleId) : ICommand;
