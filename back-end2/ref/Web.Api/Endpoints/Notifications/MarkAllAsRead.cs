using Application.Abstractions.Messaging;
using Application.Notifications.MarkAllAsRead;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Notifications;

internal sealed class MarkAllAsRead : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("notifications/read-all", async (
            ICommandHandler<MarkAllAsReadCommand, int> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new MarkAllAsReadCommand();

            Result<int> result = await handler.Handle(command, cancellationToken);

            return result.Match(
                count => Results.Ok(new { markedCount = count }),
                CustomResults.Problem);
        })
        .WithTags(Tags.Notifications)
        .RequireAuthorization()
        .WithSummary("Mark all notifications as read");
    }
}
