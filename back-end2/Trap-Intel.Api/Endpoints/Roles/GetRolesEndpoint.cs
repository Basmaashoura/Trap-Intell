using MediatR;
using Microsoft.AspNetCore.Mvc;
using Trap_Intel.Api.Contracts;
using Trap_Intel.Api.Authorization;
using Trap_Intel.Application.Roles.Queries.GetRoles;
using Trap_Intel.Api.Extensions;
using Trap_Intel.Api.Filters;
using Trap_Intel.Application.Abstractions.Querying;
using Trap_Intel.Domain.Identity.Authorization;

namespace Trap_Intel.Api.Endpoints.Roles;

internal sealed class GetRolesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/roles")
            .WithTags("Roles & Permissions")
            .AddEndpointFilter<ValidationFilter>()
            .RequireAuthorization();

        group.MapGet("/", HandleAsync)
            .WithName("GetRoles")
            .WithSummary("Retrieves roles for the current organization (system roles included)")
            .RequirePermission(Permissions.Users.View)
            .Produces<PagedResult<RoleDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> HandleAsync(
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken,
        [FromQuery] Guid? organizationId = null,
        [FromQuery] bool includeInactive = false,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = GlobalQueryOptions.DefaultPageSize,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = "desc")
    {
        var listQuery = new GlobalListQueryRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Search = search,
            SortBy = sortBy,
            SortDirection = sortDirection
        };

        Guid? targetOrganizationId;

        if (httpContext.User.IsSuperAdmin())
        {
            if (organizationId.HasValue)
            {
                targetOrganizationId = organizationId.Value;
            }
            else
            {
                var claimValue = httpContext.User.GetOrganizationClaimValue();
                targetOrganizationId = Guid.TryParse(claimValue, out var claimOrgId)
                    ? claimOrgId
                    : null;
            }
        }
        else
        {
            var claimValue = httpContext.User.GetOrganizationClaimValue();
            if (!Guid.TryParse(claimValue, out var claimOrgId))
            {
                return Results.Forbid();
            }

            if (organizationId.HasValue && organizationId.Value != claimOrgId)
            {
                return Results.Forbid();
            }

            targetOrganizationId = claimOrgId;
        }

        var result = await sender.Send(
            new GetRolesQuery(targetOrganizationId, includeInactive, listQuery.ToQueryOptions()),
            cancellationToken);

        if (result.IsFailure)
        {
            return Results.Problem(
                title: "Failed to retrieve roles",
                detail: result.Errors.FirstOrDefault()?.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }

        var filterKey = listQuery.BuildFilterKey(
            ("organizationid", targetOrganizationId),
            ("includeinactive", includeInactive));

        if (targetOrganizationId.HasValue)
        {
            httpContext.Response.SetListRealtimeHeaders("roles", "organization", filterKey);
        }

        return Results.Ok(result.Value);
    }
}
