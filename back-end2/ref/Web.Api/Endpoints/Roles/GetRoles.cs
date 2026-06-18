using Application.Abstractions.Messaging;
using Application.Roles.Common;
using Application.Roles.GetRoles;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Roles;

/// <summary>
/// Endpoint for getting paginated list of roles.
/// </summary>
internal sealed class GetRoles : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("roles", async (
            int? pageNumber,
            int? pageSize,
            string? searchTerm,
            bool? isActive,
            string? sortBy,
            string? sortDirection,
            IQueryHandler<GetRolesQuery, PagedResult<RoleListItemResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetRolesQuery
            {
                PageNumber = pageNumber ?? 1,
                PageSize = pageSize ?? 10,
                SearchTerm = searchTerm,
                IsActive = isActive,
                SortBy = sortBy ?? "Name",
                SortDirection = sortDirection ?? "asc"
            };

            Result<PagedResult<RoleListItemResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.Roles)
        .WithName("GetRoles")
        .WithSummary("Get paginated list of roles")
        .WithDescription("Returns a paginated list of roles with optional filtering and sorting.")
        .Produces<PagedResult<RoleListItemResponse>>(200)
        .ProducesProblem(400)
        .Produces(401);
    }
}
