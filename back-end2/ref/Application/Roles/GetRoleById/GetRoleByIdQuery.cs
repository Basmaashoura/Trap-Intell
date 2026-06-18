using Application.Abstractions.Messaging;
using Application.Roles.Common;

namespace Application.Roles.GetRoleById;

/// <summary>
/// Query to get a role by its ID.
/// </summary>
/// <param name="RoleId">The ID of the role to retrieve.</param>
public sealed record GetRoleByIdQuery(Guid RoleId) : IQuery<RoleResponse>;
