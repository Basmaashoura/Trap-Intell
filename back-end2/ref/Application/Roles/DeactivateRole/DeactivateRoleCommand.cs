using Application.Abstractions.Messaging;

namespace Application.Roles.DeactivateRole;

/// <summary>
/// Command to deactivate a role.
/// </summary>
/// <param name="RoleId">The ID of the role to deactivate.</param>
public sealed record DeactivateRoleCommand(Guid RoleId) : ICommand;
