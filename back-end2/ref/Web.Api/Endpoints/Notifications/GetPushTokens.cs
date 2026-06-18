using Application.Abstractions.Messaging;
using Application.Notifications.GetPushTokens;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Notifications;

internal sealed class GetPushTokens : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("notifications/push-tokens", async (
            IQueryHandler<GetPushTokensQuery, List<PushTokenResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetPushTokensQuery();

            Result<List<PushTokenResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Notifications)
        .RequireAuthorization()
        .WithDescription("Gets all push tokens for the current user.")
        .Produces<List<PushTokenResponse>>();
    }
}
