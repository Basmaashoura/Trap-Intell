using Application.Abstractions.Messaging;
using Application.Users.Common;
using Application.Users.GetCurrentUserProfile;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

/// <summary>
/// Endpoint for getting the current user's profile.
/// </summary>
internal sealed class GetCurrentUserProfile : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("users/me/profile", async (
            IQueryHandler<GetCurrentUserProfileQuery, UserProfileResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetCurrentUserProfileQuery();

            Result<UserProfileResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.Users)
        .WithName("GetCurrentUserProfile")
        .WithSummary("Get current user's profile")
        .WithDescription("Returns the profile information for the currently authenticated user.")
        .Produces<UserProfileResponse>(200)
        .ProducesProblem(404)
        .Produces(401);
    }
}
