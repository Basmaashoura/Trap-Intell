using MediatR;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Identity;

namespace Trap_Intel.Application.Users.Commands.ChangeUserRole;

public sealed record ChangeUserRoleCommand(Guid UserId, Guid NewRoleId) : IRequest<Result>;

