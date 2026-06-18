using Application.Abstractions.Messaging;
using Application.Notifications.UpdatePreferences;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Notifications;

internal sealed class UpdatePreferences : IEndpoint
{
    public sealed record Request(
        bool InAppEnabled,
        bool EmailEnabled,
        bool PushEnabled,
        bool SmsEnabled,
        bool EmailDigestEnabled,
        string EmailDigestFrequency,
        TimeOnly? EmailDigestTime,
        bool QuietHoursEnabled,
        TimeOnly? QuietHoursStart,
        TimeOnly? QuietHoursEnd,
        string? QuietHoursTimezone);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("notifications/preferences", async (
            Request request,
            ICommandHandler<UpdatePreferencesCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdatePreferencesCommand(
                request.InAppEnabled,
                request.EmailEnabled,
                request.PushEnabled,
                request.SmsEnabled,
                request.EmailDigestEnabled,
                request.EmailDigestFrequency,
                request.EmailDigestTime,
                request.QuietHoursEnabled,
                request.QuietHoursStart,
                request.QuietHoursEnd,
                request.QuietHoursTimezone);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Notifications)
        .RequireAuthorization()
        .WithSummary("Update notification preferences for current user");
    }
}
