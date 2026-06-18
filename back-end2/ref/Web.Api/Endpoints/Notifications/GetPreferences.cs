using Application.Abstractions.Messaging;
using Application.Notifications.Common;
using Application.Notifications.GetPreferences;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Notifications;

internal sealed class GetPreferences : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("notifications/preferences", async (
            IQueryHandler<GetPreferencesQuery, NotificationPreferencesResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetPreferencesQuery();

            Result<NotificationPreferencesResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Notifications)
        .RequireAuthorization()
        .WithSummary("Get notification preferences for current user");
    }
}
