using Application.Abstractions.Messaging;
using Application.Abstractions.Repositories;
using Application.Notifications.Common;
using Domain.Notifications;
using SharedKernel;

namespace Application.Notifications.GetNotificationTypes;

internal sealed class GetNotificationTypesQueryHandler(
    INotificationTypeRepository typeRepository)
    : IQueryHandler<GetNotificationTypesQuery, List<NotificationTypeResponse>>
{
    public async Task<Result<List<NotificationTypeResponse>>> Handle(
        GetNotificationTypesQuery request,
        CancellationToken cancellationToken)
    {
        // Filter by category if provided
        List<NotificationType> types = !string.IsNullOrEmpty(request.Category) &&
            Enum.TryParse<NotificationCategory>(request.Category, true, out NotificationCategory category)
            ? await typeRepository.GetByCategoryAsync(category, cancellationToken)
            : await typeRepository.GetAllActiveAsync(cancellationToken);

        var response = types.Select(t => new NotificationTypeResponse(
            t.Id,
            t.Code,
            t.Name,
            t.Description,
            t.Category.ToString(),
            t.DefaultPriority.ToString(),
            t.IsSystemType,
            t.IconName,
            t.ColorHex)).ToList();

        return Result.Success(response);
    }
}
