using MediatR;
using Trap_Intel.Domain.Abstractions;
using System;

namespace Trap_Intel.Application.Users.Commands.UnsuspendUser;

public sealed record UnsuspendUserCommand(
    Guid UserId
) : IRequest<Result>;
