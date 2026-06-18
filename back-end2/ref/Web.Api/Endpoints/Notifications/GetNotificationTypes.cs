using Application.Abstractions.Messaging;
using Application.Notifications.Common;
using Application.Notifications.GetNotificationTypes;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Notifications;

internal sealed class GetNotificationTypes : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("notifications/types", async (
            string? category,
            IQueryHandler<GetNotificationTypesQuery, List<NotificationTypeResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetNotificationTypesQuery(category);

            Result<List<NotificationTypeResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Notifications)
        .RequireAuthorization()
        .WithSummary("Get all notification types");
    }
}
