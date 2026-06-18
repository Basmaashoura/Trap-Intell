using Application.Abstractions.Messaging;
using Application.Notifications.ArchiveAll;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Notifications;

internal sealed class ArchiveAll : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("notifications/archive-all", async (
            ICommandHandler<ArchiveAllCommand, int> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new ArchiveAllCommand();

            Result<int> result = await handler.Handle(command, cancellationToken);

            return result.Match(
                count => Results.Ok(new { ArchivedCount = count }),
                CustomResults.Problem);
        })
        .WithTags(Tags.Notifications)
        .RequireAuthorization()
        .WithDescription("Archives all read notifications for the current user.");
    }
}
