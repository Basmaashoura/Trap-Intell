using Application.Abstractions.Messaging;
using Application.Users.Common;
using Application.Users.GetCurrentUser;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

/// <summary>
/// Endpoint for getting current user's information.
/// </summary>
internal sealed class GetCurrentUser : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("users/me", async (
            IQueryHandler<GetCurrentUserQuery, UserResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetCurrentUserQuery();

            Result<UserResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.Users)
        .WithName("GetCurrentUser")
        .WithSummary("Get current user's information")
        .WithDescription("Returns the currently authenticated user's information.")
        .Produces<UserResponse>(200)
        .ProducesProblem(404)
        .Produces(401);
    }
}
