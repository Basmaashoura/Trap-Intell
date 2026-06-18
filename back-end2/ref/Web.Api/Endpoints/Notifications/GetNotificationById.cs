using Application.Abstractions.Messaging;
using Application.Notifications.GetNotificationById;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Notifications;

internal sealed class GetNotificationById : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("notifications/{id:guid}", async (
            Guid id,
            IQueryHandler<GetNotificationByIdQuery, NotificationDetailResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetNotificationByIdQuery(id);

            Result<NotificationDetailResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Notifications)
        .RequireAuthorization()
        .WithDescription("Gets a notification by ID with full details including deliveries.")
        .Produces<NotificationDetailResponse>()
        .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
