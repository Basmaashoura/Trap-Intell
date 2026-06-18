using Application.Abstractions.Messaging;
using Application.Notifications.GetUnreadCount;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Notifications;

internal sealed class GetUnreadCount : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("notifications/unread-count", async (
            IQueryHandler<GetUnreadCountQuery, int> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetUnreadCountQuery();

            Result<int> result = await handler.Handle(query, cancellationToken);

            return result.Match(
                count => Results.Ok(new { count }),
                CustomResults.Problem);
        })
        .WithTags(Tags.Notifications)
        .RequireAuthorization()
        .WithSummary("Get unread notification count for current user");
    }
}
