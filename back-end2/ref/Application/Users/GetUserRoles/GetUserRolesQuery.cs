using Application.Abstractions.Messaging;
using Application.Roles.Common;

namespace Application.Users.GetUserRoles;

/// <summary>
/// Query to get roles assigned to a user.
/// </summary>
/// <param name="UserId">The ID of the user.</param>
public sealed record GetUserRolesQuery(Guid UserId) : IQuery<IReadOnlyList<RoleListItemResponse>>;
