using Application.Abstractions.Messaging;
using Application.Roles.UpdateRolePermissions;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Roles;

/// <summary>
/// Endpoint for updating role permissions.
/// </summary>
internal sealed class UpdateRolePermissions : IEndpoint
{
    public sealed record Request(IReadOnlyList<Guid> PermissionIds);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("roles/{roleId:guid}/permissions", async (
            Guid roleId,
            Request request,
            ICommandHandler<UpdateRolePermissionsCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateRolePermissionsCommand(roleId, request.PermissionIds);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => Results.Ok(new { message = "Role permissions updated successfully." }),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.Roles)
        .WithName("UpdateRolePermissions")
        .WithSummary("Update role permissions")
        .WithDescription("Updates all permissions assigned to a role. System roles cannot be modified.")
        .Produces(200)
        .ProducesProblem(400)
        .ProducesProblem(404)
        .Produces(401)
        .Produces(403);
    }
}
