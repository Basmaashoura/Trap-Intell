using MediatR;
using Trap_Intel.Application.Abstractions.Querying;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Notifications.Enums;

namespace Trap_Intel.Application.Notifications.Queries.GetNotifications;

public sealed record NotificationDto(
    Guid Id,
    Guid UserId,
    string Type,
    NotificationCategory Category,
    NotificationPriority Priority,
    string Title,
    string Message,
    string? LinkUri,
    string? RelatedEntityId,
    DateTime CreatedAt,
    DateTime? ReadAt,
    DateTime? ExpiresAt,
    bool IsRead,
    bool IsDismissed
);

// Pagination and filtering
public sealed record GetNotificationsQuery(
    Guid UserId,
    bool UnreadOnly = false,
    NotificationCategory? Category = null,
    NotificationPriority? Priority = null,
    GlobalQueryOptions? Query = null
) : IRequest<Result<PagedResult<NotificationDto>>>;
