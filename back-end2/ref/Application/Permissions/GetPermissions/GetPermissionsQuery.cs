using Application.Abstractions.Messaging;
using Application.Roles.Common;

namespace Application.Permissions.GetPermissions;

/// <summary>
/// Query to get all available permissions.
/// </summary>
public sealed record GetPermissionsQuery : IQuery<IReadOnlyList<PermissionResponse>>;
