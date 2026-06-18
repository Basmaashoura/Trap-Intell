using MediatR;
using Trap_Intel.Domain.Abstractions;
using System;

namespace Trap_Intel.Application.Users.Commands.SuspendUser;

public sealed record SuspendUserCommand(
    Guid UserId,
    string Reason
) : IRequest<Result>;
