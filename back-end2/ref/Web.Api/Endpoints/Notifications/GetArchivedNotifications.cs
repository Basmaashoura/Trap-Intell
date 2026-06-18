using Application.Abstractions.Messaging;
using Application.Notifications.Common;
using Application.Notifications.GetArchivedNotifications;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Notifications;

internal sealed class GetArchivedNotifications : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("notifications/archived", async (
            int? page,
            int? pageSize,
            IQueryHandler<GetArchivedNotificationsQuery, PagedResult<NotificationResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetArchivedNotificationsQuery(
                page ?? 1,
                pageSize ?? 20);

            Result<PagedResult<NotificationResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Notifications)
        .RequireAuthorization()
        .WithDescription("Gets archived notifications for the current user.")
        .Produces<PagedResult<NotificationResponse>>();
    }
}
