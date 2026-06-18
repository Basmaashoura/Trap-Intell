using Application.Abstractions.Messaging;
using Application.Users.Common;
using Application.Users.GetUserSessions;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

/// <summary>
/// Endpoint for getting user's sessions.
/// </summary>
internal sealed class GetUserSessions : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("users/{userId:guid}/sessions", async (
            Guid userId,
            IQueryHandler<GetUserSessionsQuery, IReadOnlyList<UserSessionResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetUserSessionsQuery(userId);

            Result<IReadOnlyList<UserSessionResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.Users)
        .WithName("GetUserSessions")
        .WithSummary("Get user sessions")
        .WithDescription("Returns all active sessions for a user.")
        .Produces<IReadOnlyList<UserSessionResponse>>(200)
        .ProducesProblem(404)
        .Produces(401);
    }
}
