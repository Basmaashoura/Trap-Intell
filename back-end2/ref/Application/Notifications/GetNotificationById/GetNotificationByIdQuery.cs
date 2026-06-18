#pragma warning disable CA1054 // URI parameters should not be strings

using Application.Abstractions.Messaging;

namespace Application.Notifications.GetNotificationById;

public sealed record GetNotificationByIdQuery(Guid NotificationId) : IQuery<NotificationDetailResponse>;

public sealed record NotificationDetailResponse(
    Guid Id,
    string TypeCode,
    string TypeName,
    string Title,
    string Message,
    string Priority,
    string? IconName,
    string? ColorHex,
    string? EntityType,
    Guid? EntityId,
    string? ActionUrl,
    string? ActionText,
    string? Metadata,
    string? GroupKey,
    bool IsRead,
    DateTime? ReadAt,
    bool IsDismissed,
    DateTime? DismissedAt,
    bool IsArchived,
    DateTime? ArchivedAt,
    DateTime? ScheduledFor,
    DateTime? ExpiresAt,
    DateTime CreatedAt,
    string? CreatedByName,
    List<DeliveryInfo> Deliveries);

public sealed record DeliveryInfo(
    Guid Id,
    string Channel,
    string Status,
    DateTime? SentAt,
    DateTime? FailedAt,
    string? FailureReason);
