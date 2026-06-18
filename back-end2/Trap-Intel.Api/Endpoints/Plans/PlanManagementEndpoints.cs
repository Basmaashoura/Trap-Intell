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
            .WithTags("Billing - Plans")
            .AddEndpointFilter<ValidationFilter>()
            .RequireAuthorization();

        group.MapGet("/", GetPlans)
            .WithName("GetPlans")
            .WithSummary("Gets active subscription plans")
            .RequirePermission(Permissions.Organization.ManageBilling)
            .Produces<IReadOnlyList<PlanSummaryDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/all", GetAllPlans)
            .WithName("GetAllPlans")
            .WithSummary("Gets all plans including inactive ones")
            .RequirePermission(Permissions.System.ManagePlans)
            .Produces<IReadOnlyList<PlanSummaryDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/{planId:guid}", GetPlanById)
            .WithName("GetPlanById")
            .WithSummary("Gets full details for a single plan")
            .RequirePermission(Permissions.Organization.ManageBilling)
            .Produces<PlanDetailDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/{planId:guid}/pricing", GetPlanPricing)
            .WithName("GetPlanPricing")
            .WithSummary("Gets pricing matrix for a single plan")
            .RequirePermission(Permissions.Organization.ManageBilling)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/{planId:guid}/quota-template", GetPlanQuotaTemplate)
            .WithName("GetPlanQuotaTemplate")
            .WithSummary("Gets quota template of a plan")
            .RequirePermission(Permissions.Organization.ManageBilling)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/", CreatePlan)
            .WithName("CreatePlan")
            .WithSummary("Creates a new subscription plan")
            .RequirePermission(Permissions.System.ManagePlans)
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status409Conflict)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/{planId:guid}/activate", ActivatePlan)
            .WithName("ActivatePlan")
            .WithSummary("Activates a subscription plan")
            .RequirePermission(Permissions.System.ManagePlans)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/{planId:guid}/deactivate", DeactivatePlan)
            .WithName("DeactivatePlan")
            .WithSummary("Deactivates a subscription plan")
            .RequirePermission(Permissions.System.ManagePlans)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status409Conflict)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> GetPlans(
        [FromQuery] string? type,
        ISender sender,
        CancellationToken cancellationToken)
    {
        PlanType? parsedType = null;
        if (!string.IsNullOrWhiteSpace(type))
        {
            if (!Enum.TryParse<PlanType>(type, true, out var tmpType))
            {
                return Results.Problem(
                    title: "Invalid plan type",
                    detail: "Invalid plan type query value.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            parsedType = tmpType;
        }

        var result = await sender.Send(new GetPlansQuery(parsedType), cancellationToken);
        if (result.IsFailure)
        {
            return Results.Problem(
                title: "Failed to retrieve plans",
                detail: result.Errors.FirstOrDefault()?.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> GetAllPlans(
        [FromQuery] string? type,
        ISender sender,
        CancellationToken cancellationToken)
    {
        PlanType? parsedType = null;
        if (!string.IsNullOrWhiteSpace(type))
        {
            if (!Enum.TryParse<PlanType>(type, true, out var tmpType))
            {
                return Results.Problem(
                    title: "Invalid plan type",
                    detail: "Invalid plan type query value.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            parsedType = tmpType;
        }

        var result = await sender.Send(new GetPlansQuery(parsedType, IncludeInactive: true), cancellationToken);
        if (result.IsFailure)
        {
            return Results.Problem(
                title: "Failed to retrieve plans",
                detail: result.Errors.FirstOrDefault()?.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> GetPlanById(
        Guid planId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPlanByIdQuery(planId), cancellationToken);
        if (result.IsFailure)
        {
            var firstError = result.Errors.FirstOrDefault();
            var isNotFound = string.Equals(firstError?.Code, "Plan.NotFound", StringComparison.OrdinalIgnoreCase);

            return Results.Problem(
                title: "Failed to retrieve plan",
                detail: firstError?.Message,
                statusCode: isNotFound ? StatusCodes.Status404NotFound : StatusCodes.Status400BadRequest);
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> GetPlanPricing(
        Guid planId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPlanByIdQuery(planId), cancellationToken);
        if (result.IsFailure)
        {
            var firstError = result.Errors.FirstOrDefault();
            var isNotFound = string.Equals(firstError?.Code, "Plan.NotFound", StringComparison.OrdinalIgnoreCase);

            return Results.Problem(
                title: "Failed to retrieve plan pricing",
                detail: firstError?.Message,
                statusCode: isNotFound ? StatusCodes.Status404NotFound : StatusCodes.Status400BadRequest);
        }

        var pricing = result.Value.Pricing
            .Select(x => new
            {
                billingCycle = x.BillingCycle.ToString(),
                amount = x.Amount,
                currency = x.Currency,
                setupFee = x.SetupFee
            })
            .ToList();

        return Results.Ok(new
        {
            planId = result.Value.Id,
            planName = result.Value.Name,
            pricing
        });
    }

    private static async Task<IResult> GetPlanQuotaTemplate(
        Guid planId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPlanByIdQuery(planId), cancellationToken);
        if (result.IsFailure)
        {
            var firstError = result.Errors.FirstOrDefault();
            var isNotFound = string.Equals(firstError?.Code, "Plan.NotFound", StringComparison.OrdinalIgnoreCase);

            return Results.Problem(
                title: "Failed to retrieve plan quota template",
                detail: firstError?.Message,
                statusCode: isNotFound ? StatusCodes.Status404NotFound : StatusCodes.Status400BadRequest);
        }

        if (result.Value.Quota is null)
        {
            return Results.Ok(new
            {
                planId = result.Value.Id,
                planName = result.Value.Name,
                hasQuotaTemplate = false
            });
        }

        return Results.Ok(new
        {
            planId = result.Value.Id,
            planName = result.Value.Name,
            hasQuotaTemplate = true,
            quota = result.Value.Quota
        });
    }

    private static async Task<IResult> CreatePlan(
        [FromBody] CreatePlanRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<PlanType>(request.Type, true, out var planType))
        {
            return Results.Problem(
                title: "Invalid plan type",
                detail: "The provided plan type is invalid.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (!Enum.TryParse<SupportLevel>(request.SupportLevel, true, out var supportLevel))
        {
            return Results.Problem(
                title: "Invalid support level",
                detail: "The provided support level is invalid.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (!Enum.TryParse<ComplianceLevel>(request.ComplianceLevel, true, out var complianceLevel))
        {
            return Results.Problem(
                title: "Invalid compliance level",
                detail: "The provided compliance level is invalid.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (!Enum.TryParse<CustomizationLevel>(request.CustomizationLevel, true, out var customizationLevel))
        {
            return Results.Problem(
                title: "Invalid customization level",
                detail: "The provided customization level is invalid.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (!Enum.TryParse<BillingCycle>(request.BillingCycle, true, out var billingCycle))
        {
            return Results.Problem(
                title: "Invalid billing cycle",
                detail: "The provided billing cycle is invalid.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var command = new CreatePlanCommand(
            request.Name,
            request.Description,
            planType,
            supportLevel,
            request.SupportResponseTimeMinutes,
            request.IncludesDedicatedManager,
            complianceLevel,
            request.RequiredCertifications ?? Array.Empty<string>(),
            request.ComplianceAuditingIncluded,
            customizationLevel,
            billingCycle,
            request.PriceAmount,
            request.Currency,
            request.SetupFee);

        var result = await sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            var firstError = result.Errors.FirstOrDefault();
            var isConflict = string.Equals(firstError?.Code, "Plan.DuplicateName", StringComparison.OrdinalIgnoreCase);

            return Results.Problem(
                title: "Failed to create plan",
                detail: firstError?.Message,
                statusCode: isConflict ? StatusCodes.Status409Conflict : StatusCodes.Status400BadRequest);
        }

        return Results.Created($"/api/plans/{result.Value}", new { planId = result.Value });
    }

    private static Task<IResult> ActivatePlan(
        Guid planId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        return ApplyPlanLifecycleActionAsync(planId, PlanLifecycleAction.Activate, sender, cancellationToken);
    }

    private static Task<IResult> DeactivatePlan(
        Guid planId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        return ApplyPlanLifecycleActionAsync(planId, PlanLifecycleAction.Deactivate, sender, cancellationToken);
    }

    private static async Task<IResult> ApplyPlanLifecycleActionAsync(
        Guid planId,
        PlanLifecycleAction action,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ManagePlanLifecycleCommand(planId, action), cancellationToken);
        if (result.IsFailure)
        {
            var firstError = result.Errors.FirstOrDefault();
            var statusCode = string.Equals(firstError?.Code, "Plan.NotFound", StringComparison.OrdinalIgnoreCase)
                ? StatusCodes.Status404NotFound
                : string.Equals(firstError?.Code, "Plan.CannotDeactivateWithActiveSubscriptions", StringComparison.OrdinalIgnoreCase)
                    ? StatusCodes.Status409Conflict
                    : StatusCodes.Status400BadRequest;

            return Results.Problem(
                title: "Failed to update plan status",
                detail: firstError?.Message,
                statusCode: statusCode);
        }

        var message = action == PlanLifecycleAction.Activate
            ? "Plan activated successfully."
            : "Plan deactivated successfully.";

        return Results.Ok(new { message });
    }
}
