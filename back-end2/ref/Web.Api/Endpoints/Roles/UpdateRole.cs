using Application.Abstractions.Messaging;
using Application.Roles.UpdateRole;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Roles;

/// <summary>
/// Endpoint for updating a role.
/// </summary>
internal sealed class UpdateRole : IEndpoint
{
    public sealed record Request(
        string Name,
        string? Description,
        bool CanViewSensitiveData);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("roles/{roleId:guid}", async (
            Guid roleId,
            Request request,
            ICommandHandler<UpdateRoleCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateRoleCommand(
                roleId,
                request.Name,
                request.Description,
                request.CanViewSensitiveData);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => Results.Ok(new { message = "Role updated successfully." }),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.Roles)
        .WithName("UpdateRole")
        .WithSummary("Update a role")
        .WithDescription("Updates an existing role. System roles cannot be modified.")
        .Produces(200)
        .ProducesProblem(400)
        .ProducesProblem(404)
        .ProducesProblem(409)
        .Produces(401)
        .Produces(403);
    }
}
