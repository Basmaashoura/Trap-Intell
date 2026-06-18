using MediatR;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Application.Roles.Queries.GetPermissions;

public sealed record GetPermissionsQuery() : IRequest<Result<IEnumerable<string>>>;
