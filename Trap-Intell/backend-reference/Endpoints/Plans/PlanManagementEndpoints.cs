using MediatR;
using Microsoft.AspNetCore.Mvc;
using Trap_Intel.Api.Authorization;
using Trap_Intel.Api.Endpoints.Plans.Models;
using Trap_Intel.Api.Filters;
using Trap_Intel.Application.Plans.Commands.CreatePlan;
using Trap_Intel.Application.Plans.Commands.ManagePlanLifecycle;
using Trap_Intel.Application.Plans.Queries.GetPlanById;
using Trap_Intel.Application.Plans.Queries.GetPlans;
using Trap_Intel.Domain.Identity.Authorization;
using Trap_Intel.Domain.Plans;

namespace Trap_Intel.Api.Endpoints.Plans;

internal sealed class PlanManagementEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/plans")
            .WithTags("Plans")
            .AddEndpointFilter<ValidationFilter>()
            .RequireAuthorization();

        group.MapGet("/", GetPlans)
            .WithName("GetPlans")
            .WithSummary("List all available billing plans")
            .AllowAnonymous()
            .Produces<IEnumerable<PlanDto>>(StatusCodes.Status200OK);

        group.MapGet("/{planId:guid}", GetPlanById)
            .WithName("GetPlanById")
            .WithSummary("Get detailed plan information")
            .AllowAnonymous()
            .Produces<PlanDetailDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/", CreatePlan)
            .WithName("CreatePlan")
            .WithSummary("Create new billing plan")
            .RequirePermission(Permissions.Billing.ManagePlans)
            .Produces<PlanDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapPut("/{planId:guid}/activate", ActivatePlan)
            .WithName("ActivatePlan")
            .WithSummary("Activate billing plan")
            .RequirePermission(Permissions.Billing.ManagePlans)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/{planId:guid}/deactivate", DeactivatePlan)
            .WithName("DeactivatePlan")
            .WithSummary("Deactivate billing plan")
            .RequirePermission(Permissions.Billing.ManagePlans)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetPlans(
        [FromQuery] string? type,
        ISender sender,
        CancellationToken cancellationToken)
    {
        // Implementation retrieves all plans, optionally filtered by type
    }

    private static async Task<IResult> GetPlanById(
        Guid planId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        // Implementation retrieves detailed plan information
    }

    private static async Task<IResult> CreatePlan(
        [FromBody] CreatePlanRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        // Implementation creates new billing plan
    }

    private static async Task<IResult> ActivatePlan(
        Guid planId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        // Implementation activates plan
    }

    private static async Task<IResult> DeactivatePlan(
        Guid planId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        // Implementation deactivates plan
    }
}
