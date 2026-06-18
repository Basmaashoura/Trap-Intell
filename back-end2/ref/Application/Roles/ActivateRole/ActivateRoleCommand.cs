using Application.Abstractions.Messaging;

namespace Application.Roles.ActivateRole;

/// <summary>
/// Command to activate a deactivated role.
/// </summary>
/// <param name="RoleId">The ID of the role to activate.</param>
public sealed record ActivateRoleCommand(Guid RoleId) : ICommand;
