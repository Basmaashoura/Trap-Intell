using MediatR;
using Trap_Intel.Application.Abstractions.Querying;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Identity;

namespace Trap_Intel.Application.Users.Queries.GetUsers;

public sealed record UserDto(
    Guid Id,
    string Email,
    string UserName,
    string FirstName,
    string LastName,
    string FullName,
    UserStatus Status,
    Guid RoleId,
    Guid OrganizationId,
    DateTime CreatedAt
);

public sealed record GetUsersQuery(
    Guid OrganizationId,
    UserStatus? Status = null,
    Guid? RoleId = null,
    GlobalQueryOptions? Query = null) : IRequest<Result<PagedResult<UserDto>>>;
