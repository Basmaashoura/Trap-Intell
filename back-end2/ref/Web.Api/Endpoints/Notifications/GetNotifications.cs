using Application.Abstractions.Messaging;
using Application.Notifications.Common;
using Application.Notifications.GetNotifications;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Notifications;

internal sealed class GetNotifications : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("notifications", async (
            int? page,
            int? pageSize,
            bool? isRead,
            string? types,
            string? priority,
            DateTime? fromDate,
            IQueryHandler<GetNotificationsQuery, PagedResult<NotificationResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetNotificationsQuery(
                page ?? 1,
                pageSize ?? 20,
                isRead,
                types,
                priority,
                fromDate);

            Result<PagedResult<NotificationResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Notifications)
        .RequireAuthorization()
        .WithSummary("Get paginated notifications for current user");
    }
}
