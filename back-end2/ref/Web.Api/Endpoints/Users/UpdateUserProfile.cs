using Application.Abstractions.Messaging;
using Application.Users.UpdateUserProfile;
using Domain.Common;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

/// <summary>
/// Endpoint for updating a user's profile (Admin).
/// </summary>
internal sealed class UpdateUserProfile : IEndpoint
{
    public sealed record Request(
        Department Department,
        string? DisplayJobTitle,
        string? InternalJobTitle,
        decimal? HourlyCost,
        string? PhoneNumber,
        DateTime? HireDate,
        string? AvatarUrl,
        string? Bio,
        IReadOnlyList<string>? Skills);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("users/{userId:guid}/profile", async (
            Guid userId,
            Request request,
            ICommandHandler<UpdateUserProfileCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateUserProfileCommand(
                userId,
                request.Department,
                request.DisplayJobTitle,
                request.InternalJobTitle,
                request.HourlyCost,
                request.PhoneNumber,
                request.HireDate,
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
        .WithName("UpdateUserProfile")
        .WithSummary("Update user profile (Admin)")
        .WithDescription("Updates a user's profile including sensitive fields like HourlyCost. Admin only.")
        .Produces(200)
        .ProducesProblem(400)
        .ProducesProblem(404)
        .Produces(401)
        .Produces(403);
    }
}
