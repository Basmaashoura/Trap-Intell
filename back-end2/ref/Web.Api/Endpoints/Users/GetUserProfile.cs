using Application.Abstractions.Messaging;
using Application.Users.Common;
using Application.Users.GetUserProfile;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

/// <summary>
/// Endpoint for getting a user's profile.
/// </summary>
internal sealed class GetUserProfile : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("users/{userId:guid}/profile", async (
            Guid userId,
            IQueryHandler<GetUserProfileQuery, UserProfileResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetUserProfileQuery(userId);

            Result<UserProfileResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.Users)
        .WithName("GetUserProfile")
        .WithSummary("Get user profile")
        .WithDescription("Returns the profile information for a user.")
        .Produces<UserProfileResponse>(200)
        .ProducesProblem(404)
        .Produces(401);
    }
}
