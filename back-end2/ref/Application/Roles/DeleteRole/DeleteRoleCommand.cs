using Application.Abstractions.Messaging;

namespace Application.Roles.DeleteRole;

/// <summary>
/// Command to delete a role.
/// </summary>
/// <param name="RoleId">The ID of the role to delete.</param>
public sealed record DeleteRoleCommand(Guid RoleId) : ICommand;
