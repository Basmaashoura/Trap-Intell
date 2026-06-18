using Application.Abstractions.Messaging;
using Application.Users.UpdateUser;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

/// <summary>
/// Endpoint for updating a user (admin operation).
/// </summary>
internal sealed class UpdateUser : IEndpoint
{
    public sealed record Request(string FirstName, string LastName, string Email);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("users/{userId:guid}", async (
            Guid userId,
            Request request,
            ICommandHandler<UpdateUserCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateUserCommand(
                userId,
                request.FirstName,
                request.LastName,
                request.Email);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => Results.Ok(new { message = "User updated successfully." }),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.Users)
        .WithName("UpdateUser")
        .WithSummary("Update user information")
        .WithDescription("Updates a user's information (admin operation).")
        .Produces(200)
        .ProducesProblem(400)
        .ProducesProblem(404)
        .ProducesProblem(409)
        .Produces(401)
        .Produces(403);
    }
}
