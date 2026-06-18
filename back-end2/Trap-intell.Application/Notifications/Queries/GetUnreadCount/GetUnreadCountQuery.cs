using MediatR;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Application.Notifications.Queries.GetUnreadCount;

public sealed record GetUnreadCountQuery(Guid UserId) : IRequest<Result<int>>;
