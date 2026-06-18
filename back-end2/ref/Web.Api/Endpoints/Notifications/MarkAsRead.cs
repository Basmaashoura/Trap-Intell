using Application.Abstractions.Messaging;
using Application.Notifications.MarkAsRead;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Notifications;

internal sealed class MarkAsRead : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("notifications/{id:guid}/read", async (
            Guid id,
            ICommandHandler<MarkAsReadCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new MarkAsReadCommand(id);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Notifications)
        .RequireAuthorization()
        .WithSummary("Mark a notification as read");
    }
}
