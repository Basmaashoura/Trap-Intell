using MediatR;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Application.Users.Commands.DeactivateUser;

public sealed record DeactivateUserCommand(Guid UserId, string Reason) : IRequest<Result>;
