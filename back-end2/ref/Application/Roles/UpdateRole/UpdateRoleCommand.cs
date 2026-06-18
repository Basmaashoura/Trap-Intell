using Application.Abstractions.Messaging;

namespace Application.Roles.UpdateRole;

/// <summary>
/// Command to update an existing role.
/// </summary>
/// <param name="RoleId">The ID of the role to update.</param>
/// <param name="Name">Updated role name.</param>
/// <param name="Description">Updated role description.</param>
/// <param name="CanViewSensitiveData">Updated sensitive data access.</param>
public sealed record UpdateRoleCommand(
    Guid RoleId,
    string Name,
    string? Description,
    bool CanViewSensitiveData) : ICommand;
