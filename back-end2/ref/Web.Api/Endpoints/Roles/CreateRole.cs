using Application.Abstractions.Messaging;
using Application.Roles.CreateRole;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Roles;

/// <summary>
/// Endpoint for creating a new role.
/// </summary>
internal sealed class CreateRole : IEndpoint
{
    public sealed record Request(
        string Name,
        string? Description,
        bool CanViewSensitiveData = false);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("roles", async (
            Request request,
            ICommandHandler<CreateRoleCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateRoleCommand(
                request.Name,
                request.Description,
                request.CanViewSensitiveData);

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(
                id => Results.Created($"/roles/{id}", new { id, message = "Role created successfully." }),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.Roles)
        .WithName("CreateRole")
        .WithSummary("Create a new role")
        .WithDescription("Creates a new role in the system.")
        .Produces(201)
        .ProducesProblem(400)
        .ProducesProblem(409)
        .Produces(401)
        .Produces(403);
    }
}
