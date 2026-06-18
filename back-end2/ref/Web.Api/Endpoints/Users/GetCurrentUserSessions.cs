using Application.Abstractions.Messaging;
using Application.Users.Common;
using Application.Users.GetCurrentUserSessions;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

/// <summary>
/// Endpoint for getting the current user's sessions.
/// </summary>
internal sealed class GetCurrentUserSessions : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("users/me/sessions", async (
            IQueryHandler<GetCurrentUserSessionsQuery, IReadOnlyList<UserSessionResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetCurrentUserSessionsQuery();

            Result<IReadOnlyList<UserSessionResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.Users)
        .WithName("GetCurrentUserSessions")
        .WithSummary("Get current user's sessions")
        .WithDescription("Returns all active sessions for the currently authenticated user.")
        .Produces<IReadOnlyList<UserSessionResponse>>(200)
        .Produces(401);
    }
}
