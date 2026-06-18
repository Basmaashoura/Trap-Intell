using Application.Abstractions.Messaging;
using Application.Notifications.UpdateTypeSettings;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Notifications;

internal sealed class UpdateTypeSettings : IEndpoint
{
    public sealed record Request(
        bool IsEnabled,
        List<string>? Channels);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("notifications/types/{typeId:guid}/settings", async (
            Guid typeId,
            Request request,
            ICommandHandler<UpdateTypeSettingsCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateTypeSettingsCommand(
                typeId,
                request.IsEnabled,
                request.Channels);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Notifications)
        .RequireAuthorization()
        .WithSummary("Update settings for a specific notification type");
    }
}
