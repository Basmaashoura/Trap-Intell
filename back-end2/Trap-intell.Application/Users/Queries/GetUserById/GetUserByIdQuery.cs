using MediatR;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Application.Users.Queries.GetUsers; // Reusing UserDto

namespace Trap_Intel.Application.Users.Queries.GetUserById;

public sealed record GetUserByIdQuery(Guid UserId) : IRequest<Result<UserDto>>;
