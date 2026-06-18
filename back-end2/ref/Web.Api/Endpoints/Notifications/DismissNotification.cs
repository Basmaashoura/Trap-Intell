using Application.Abstractions.Messaging;
using Application.Notifications.DismissNotification;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Notifications;

internal sealed class DismissNotification : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("notifications/{id:guid}", async (
            Guid id,
            ICommandHandler<DismissNotificationCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new DismissNotificationCommand(id);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Notifications)
        .RequireAuthorization()
        .WithSummary("Dismiss a notification");
    }
}
