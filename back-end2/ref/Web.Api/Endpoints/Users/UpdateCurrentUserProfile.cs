using Application.Abstractions.Messaging;
using Application.Users.UpdateCurrentUserProfile;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

/// <summary>
/// Endpoint for updating the current user's profile.
/// </summary>
internal sealed class UpdateCurrentUserProfile : IEndpoint
{
    public sealed record Request(
        string? PhoneNumber,
        string? AvatarUrl,
        string? Bio,
        IReadOnlyList<string>? Skills);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("users/me/profile", async (
            Request request,
            ICommandHandler<UpdateCurrentUserProfileCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateCurrentUserProfileCommand(
                request.PhoneNumber,
                request.AvatarUrl,
                request.Bio,
                request.Skills);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => Results.Ok(new { message = "Profile updated successfully." }),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.Users)
        .WithName("UpdateCurrentUserProfile")
        .WithSummary("Update current user's profile")
        .WithDescription("Updates the profile information for the currently authenticated user.")
        .Produces(200)
        .ProducesProblem(400)
        .ProducesProblem(404)
        .Produces(401);
    }
}
