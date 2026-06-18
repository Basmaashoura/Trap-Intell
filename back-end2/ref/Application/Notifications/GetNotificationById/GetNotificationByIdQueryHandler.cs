using Application.Abstractions.Identity;
using Application.Abstractions.Messaging;
using Application.Abstractions.Repositories;
using Domain.Notifications;
using SharedKernel;

namespace Application.Notifications.GetNotificationById;

internal sealed class GetNotificationByIdQueryHandler(
    INotificationRepository notificationRepository,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetNotificationByIdQuery, NotificationDetailResponse>
{
    public async Task<Result<NotificationDetailResponse>> Handle(
        GetNotificationByIdQuery request,
        CancellationToken cancellationToken)
    {
        Guid userId = currentUserService.UserId;

        Notification? notification = await notificationRepository.GetByIdWithTypeAsync(
            request.NotificationId,
            cancellationToken);

        if (notification is null || notification.UserId != userId)
        {
            return Result.Failure<NotificationDetailResponse>(
                NotificationErrors.NotFound(request.NotificationId));
        }

        var deliveries = notification.Deliveries.Select(d => new DeliveryInfo(
            d.Id,
            d.Channel.ToString(),
            d.Status.ToString(),
            d.SentAt,
            d.DeliveredAt,
            d.FailureReason)).ToList();

        var response = new NotificationDetailResponse(
            notification.Id,
            notification.Type?.Code ?? string.Empty,
            notification.Type?.Name ?? string.Empty,
            notification.Title,
            notification.Message,
            notification.Priority.ToString(),
            notification.Type?.IconName,
            notification.Type?.ColorHex,
            notification.EntityType,
            notification.EntityId,
            notification.ActionUrl,
            notification.ActionText,
            notification.Metadata,
            notification.GroupKey,
            notification.IsRead,
            notification.ReadAt,
            notification.IsDismissed,
            notification.DismissedAt,
            notification.IsArchived,
            notification.ArchivedAt,
            notification.ScheduledFor,
            notification.ExpiresAt,
            notification.CreatedAt,
            notification.CreatedBy?.FullName,
            deliveries);

        return Result.Success(response);
    }
}
