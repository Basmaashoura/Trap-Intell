using Application.Abstractions.Messaging;

namespace Application.Roles.CreateRole;

/// <summary>
/// Command to create a new role.
/// </summary>
/// <param name="Name">Role name (must be unique).</param>
/// <param name="Description">Optional role description.</param>
/// <param name="CanViewSensitiveData">Whether users with this role can view sensitive data.</param>
public sealed record CreateRoleCommand(
    string Name,
    string? Description,
    bool CanViewSensitiveData = false) : ICommand<Guid>;
