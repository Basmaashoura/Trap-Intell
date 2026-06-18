using MediatR;
using Microsoft.AspNetCore.Mvc;
using Trap_Intel.Api.Authorization;
using Trap_Intel.Api.Endpoints.Subscriptions.Models;
using Trap_Intel.Api.Extensions;
using Trap_Intel.Api.Filters;
using Trap_Intel.Application.Subscriptions.Commands.CreateSubscription;
using Trap_Intel.Application.Subscriptions.Commands.ManageSubscriptionLifecycle;
using Trap_Intel.Application.Subscriptions.Commands.ManageSubscriptionUsage;
using Trap_Intel.Application.Subscriptions.Commands.SetSubscriptionPaymentMethod;
using Trap_Intel.Application.Subscriptions.Queries.CheckSubscriptionQuotaOperation;
using Trap_Intel.Application.Subscriptions.Queries.GetCurrentOrganizationSubscription;
using Trap_Intel.Application.Subscriptions.Queries.GetCurrentOrganizationSubscriptionQuota;
using Trap_Intel.Application.Subscriptions.Queries.GetSubscriptionById;
using Trap_Intel.Application.Subscriptions.Queries.GetSubscriptionUsageInsights;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Identity.Authorization;
using Trap_Intel.Domain.Plans;
using Trap_Intel.Domain.Subscriptions;
using Trap_Intel.Domain.Subscriptions.Entities;

namespace Trap_Intel.Api.Endpoints.Subscriptions;

internal sealed class SubscriptionManagementEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/organizations/{organizationId:guid}/subscriptions")
            .WithTags("Billing - Subscriptions")
            .AddEndpointFilter<ValidationFilter>()
            .RequireAuthorization();

        group.MapGet("/current", GetCurrent)
            .WithName("GetCurrentOrganizationSubscription")
            .WithSummary("Gets current subscription for organization")
            .RequirePermission(Permissions.Organization.ManageBilling)
            .Produces<SubscriptionSummaryDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/current/quota", GetCurrentQuota)
            .WithName("GetCurrentOrganizationSubscriptionQuota")
            .WithSummary("Gets current subscription quota usage for organization")
            .RequirePermission(Permissions.Organization.ManageBilling)
            .Produces<SubscriptionQuotaUsageDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/{subscriptionId:guid}", GetById)
            .WithName("GetOrganizationSubscriptionById")
            .WithSummary("Gets subscription details by id for organization")
            .RequirePermission(Permissions.Organization.ManageBilling)
            .Produces<SubscriptionDetailDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/{subscriptionId:guid}/quota", GetQuotaBySubscriptionId)
            .WithName("GetSubscriptionQuotaById")
            .WithSummary("Gets quota usage details for a specific subscription")
            .RequirePermission(Permissions.Organization.ManageBilling)
            .Produces<SubscriptionQuotaUsageDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/", Create)
            .WithName("CreateOrganizationSubscription")
            .WithSummary("Creates a new subscription for organization")
            .RequirePermission(Permissions.Organization.ManageBilling)
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status409Conflict)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/{subscriptionId:guid}/payment-method", SetPaymentMethod)
            .WithName("SetSubscriptionPaymentMethod")
            .WithSummary("Assigns a payment method to a subscription")
            .RequirePermission(Permissions.Organization.ManageBilling)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/{subscriptionId:guid}/activate", Activate)
            .WithName("ActivateSubscription")
            .WithSummary("Activates a subscription")
            .RequirePermission(Permissions.Organization.ManageBilling)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/{subscriptionId:guid}/suspend", Suspend)
            .WithName("SuspendSubscription")
            .WithSummary("Suspends a subscription")
            .RequirePermission(Permissions.Organization.ManageBilling)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/{subscriptionId:guid}/cancel", Cancel)
            .WithName("CancelSubscription")
            .WithSummary("Cancels a subscription")
            .RequirePermission(Permissions.Organization.ManageBilling)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/{subscriptionId:guid}/auto-renew/enable", EnableAutoRenew)
            .WithName("EnableSubscriptionAutoRenew")
            .WithSummary("Enables subscription auto-renewal")
            .RequirePermission(Permissions.Organization.ManageBilling)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/{subscriptionId:guid}/auto-renew/disable", DisableAutoRenew)
            .WithName("DisableSubscriptionAutoRenew")
            .WithSummary("Disables subscription auto-renewal")
            .RequirePermission(Permissions.Organization.ManageBilling)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/{subscriptionId:guid}/change-plan", ChangePlan)
            .WithName("ChangeSubscriptionPlan")
            .WithSummary("Changes the subscription to a different plan")
            .RequirePermission(Permissions.Organization.ManageBilling)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/{subscriptionId:guid}/renew", Renew)
            .WithName("RenewSubscription")
            .WithSummary("Renews a subscription period")
            .RequirePermission(Permissions.Organization.ManageBilling)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/{subscriptionId:guid}/schedule-cancel", ScheduleCancellation)
            .WithName("ScheduleSubscriptionCancellation")
            .WithSummary("Schedules cancellation at the end of period")
            .RequirePermission(Permissions.Organization.ManageBilling)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/{subscriptionId:guid}/usage/insights", GetUsageInsights)
            .WithName("GetSubscriptionUsageInsights")
            .WithSummary("Gets usage snapshots and monthly usage insights")
            .RequirePermission(Permissions.Organization.ManageBilling)
            .Produces<SubscriptionUsageInsightsDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/{subscriptionId:guid}/usage/snapshots", RecordUsageSnapshot)
            .WithName("RecordSubscriptionUsageSnapshot")
            .WithSummary("Records a subscription usage snapshot")
            .RequirePermission(Permissions.Organization.ManageBilling)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/{subscriptionId:guid}/usage/monthly/{year:int}/{month:int}/finalize", FinalizeMonthlyUsage)
            .WithName("FinalizeSubscriptionMonthlyUsage")
            .WithSummary("Finalizes a monthly usage summary")
            .RequirePermission(Permissions.Organization.ManageBilling)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/{subscriptionId:guid}/usage/monthly/{year:int}/{month:int}/mark-billed", MarkMonthlyUsageAsBilled)
            .WithName("MarkSubscriptionMonthlyUsageAsBilled")
            .WithSummary("Marks monthly usage as billed by invoice")
            .RequirePermission(Permissions.Organization.ManageBilling)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/{subscriptionId:guid}/quota/check", CheckQuotaOperation)
            .WithName("CheckSubscriptionQuotaOperation")
            .WithSummary("Checks whether requested additional usage fits quota")
            .RequirePermission(Permissions.Organization.ManageBilling)
            .Produces<SubscriptionQuotaOperationCheckDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> GetCurrent(
        Guid organizationId,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.IsAuthorizedForOrganization(organizationId))
        {
            return Results.Forbid();
        }

        var result = await sender.Send(new GetCurrentOrganizationSubscriptionQuery(organizationId), cancellationToken);
        if (result.IsFailure)
        {
            var firstError = result.Errors.FirstOrDefault();
            var isNotFound = string.Equals(firstError?.Code, "Subscription.NotFound", StringComparison.OrdinalIgnoreCase);

            return Results.Problem(
                title: "Failed to retrieve subscription",
                detail: ToClientDetail(firstError),
                statusCode: isNotFound ? StatusCodes.Status404NotFound : StatusCodes.Status400BadRequest);
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> GetById(
        Guid organizationId,
        Guid subscriptionId,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.IsAuthorizedForOrganization(organizationId))
        {
            return Results.Forbid();
        }

        var result = await sender.Send(new GetSubscriptionByIdQuery(organizationId, subscriptionId), cancellationToken);
        if (result.IsFailure)
        {
            var firstError = result.Errors.FirstOrDefault();
            var isNotFound = string.Equals(firstError?.Code, "Subscription.NotFound", StringComparison.OrdinalIgnoreCase);

            return Results.Problem(
                title: "Failed to retrieve subscription",
                detail: ToClientDetail(firstError),
                statusCode: isNotFound ? StatusCodes.Status404NotFound : StatusCodes.Status400BadRequest);
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> GetCurrentQuota(
        Guid organizationId,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.IsAuthorizedForOrganization(organizationId))
        {
            return Results.Forbid();
        }

        var quotaResult = await sender.Send(new GetCurrentOrganizationSubscriptionQuotaQuery(organizationId), cancellationToken);
        if (quotaResult.IsFailure)
        {
            return ToErrorResult("Failed to retrieve current subscription quota", quotaResult.Errors.FirstOrDefault());
        }

        return Results.Ok(quotaResult.Value);
    }

    private static async Task<IResult> GetQuotaBySubscriptionId(
        Guid organizationId,
        Guid subscriptionId,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.IsAuthorizedForOrganization(organizationId))
        {
            return Results.Forbid();
        }

        var detailResult = await sender.Send(new GetSubscriptionByIdQuery(organizationId, subscriptionId), cancellationToken);
        if (detailResult.IsFailure)
        {
            return ToErrorResult("Failed to retrieve subscription quota", detailResult.Errors.FirstOrDefault());
        }

        return Results.Ok(detailResult.Value.QuotaUsage);
    }

    private static async Task<IResult> Create(
        Guid organizationId,
        [FromBody] CreateSubscriptionRequest request,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.IsAuthorizedForOrganization(organizationId))
        {
            return Results.Forbid();
        }

        if (!Enum.TryParse<BillingCycle>(request.BillingCycle, true, out var billingCycle))
        {
            return Results.Problem(
                title: "Invalid billing cycle",
                detail: "The provided billing cycle is invalid.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var command = new CreateSubscriptionCommand(
            organizationId,
            request.PlanId,
            billingCycle,
            request.IsTrial,
            request.TrialDays,
            request.ActivateImmediately);

        var result = await sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            var firstError = result.Errors.FirstOrDefault();
            var isNotFound = string.Equals(firstError?.Code, "Plan.NotFound", StringComparison.OrdinalIgnoreCase)
                || string.Equals(firstError?.Code, "Organization.NotFound", StringComparison.OrdinalIgnoreCase);
            var isConflict = string.Equals(firstError?.Code, "Subscription.AlreadyExists", StringComparison.OrdinalIgnoreCase)
                || string.Equals(firstError?.Code, "Subscription.ConcurrencyConflict", StringComparison.OrdinalIgnoreCase);

            return Results.Problem(
                title: "Failed to create subscription",
                detail: ToClientDetail(firstError),
                statusCode: isNotFound
                    ? StatusCodes.Status404NotFound
                    : isConflict
                        ? StatusCodes.Status409Conflict
                        : StatusCodes.Status400BadRequest);
        }

        return Results.Created(
            $"/api/organizations/{organizationId}/subscriptions/{result.Value}",
            new { subscriptionId = result.Value });
    }

    private static async Task<IResult> SetPaymentMethod(
        Guid organizationId,
        Guid subscriptionId,
        [FromBody] SetSubscriptionPaymentMethodRequest request,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.IsAuthorizedForOrganization(organizationId))
        {
            return Results.Forbid();
        }

        var result = await sender.Send(
            new SetSubscriptionPaymentMethodCommand(organizationId, subscriptionId, request.PaymentMethodId),
            cancellationToken);

        if (result.IsFailure)
        {
            var firstError = result.Errors.FirstOrDefault();
            var isNotFound = string.Equals(firstError?.Code, "Subscription.NotFound", StringComparison.OrdinalIgnoreCase)
                || string.Equals(firstError?.Code, "PaymentMethod.NotFound", StringComparison.OrdinalIgnoreCase);
            var isConflict = string.Equals(firstError?.Code, "Subscription.ConcurrencyConflict", StringComparison.OrdinalIgnoreCase);

            return Results.Problem(
                title: "Failed to set subscription payment method",
                detail: ToClientDetail(firstError),
                statusCode: isNotFound
                    ? StatusCodes.Status404NotFound
                    : isConflict
                        ? StatusCodes.Status409Conflict
                        : StatusCodes.Status400BadRequest);
        }

        return Results.Ok(new { message = "Subscription payment method set successfully." });
    }

    private static Task<IResult> Activate(
        Guid organizationId,
        Guid subscriptionId,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        return ApplyLifecycleActionAsync(
            organizationId,
            subscriptionId,
            SubscriptionLifecycleAction.Activate,
            sender,
            httpContext,
            cancellationToken);
    }

    private static Task<IResult> Suspend(
        Guid organizationId,
        Guid subscriptionId,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        return ApplyLifecycleActionAsync(
            organizationId,
            subscriptionId,
            SubscriptionLifecycleAction.Suspend,
            sender,
            httpContext,
            cancellationToken);
    }

    private static Task<IResult> Cancel(
        Guid organizationId,
        Guid subscriptionId,
        [FromBody] CancelSubscriptionRequest request,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        return ApplyLifecycleActionAsync(
            organizationId,
            subscriptionId,
            SubscriptionLifecycleAction.Cancel,
            sender,
            httpContext,
            cancellationToken,
            reason: request.Reason);
    }

    private static Task<IResult> EnableAutoRenew(
        Guid organizationId,
        Guid subscriptionId,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        return ApplyLifecycleActionAsync(
            organizationId,
            subscriptionId,
            SubscriptionLifecycleAction.EnableAutoRenew,
            sender,
            httpContext,
            cancellationToken);
    }

    private static Task<IResult> DisableAutoRenew(
        Guid organizationId,
        Guid subscriptionId,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        return ApplyLifecycleActionAsync(
            organizationId,
            subscriptionId,
            SubscriptionLifecycleAction.DisableAutoRenew,
            sender,
            httpContext,
            cancellationToken);
    }

    private static Task<IResult> ChangePlan(
        Guid organizationId,
        Guid subscriptionId,
        [FromBody] ChangeSubscriptionPlanRequest request,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        return ApplyLifecycleActionAsync(
            organizationId,
            subscriptionId,
            SubscriptionLifecycleAction.ChangePlan,
            sender,
            httpContext,
            cancellationToken,
            newPlanId: request.PlanId);
    }

    private static Task<IResult> Renew(
        Guid organizationId,
        Guid subscriptionId,
        [FromBody] RenewSubscriptionRequest request,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        return ApplyLifecycleActionAsync(
            organizationId,
            subscriptionId,
            SubscriptionLifecycleAction.Renew,
            sender,
            httpContext,
            cancellationToken,
            renewalEndDate: request.RenewalEndDate);
    }

    private static Task<IResult> ScheduleCancellation(
        Guid organizationId,
        Guid subscriptionId,
        [FromBody] ScheduleSubscriptionCancellationRequest request,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        return ApplyLifecycleActionAsync(
            organizationId,
            subscriptionId,
            SubscriptionLifecycleAction.ScheduleCancellation,
            sender,
            httpContext,
            cancellationToken,
            reason: request.Reason);
    }

    private static async Task<IResult> GetUsageInsights(
        Guid organizationId,
        Guid subscriptionId,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken,
        [FromQuery] int snapshotLimit = 30,
        [FromQuery] int monthlyLimit = 12)
    {
        if (!httpContext.User.IsAuthorizedForOrganization(organizationId))
        {
            return Results.Forbid();
        }

        var result = await sender.Send(
            new GetSubscriptionUsageInsightsQuery(
                organizationId,
                subscriptionId,
                snapshotLimit,
                monthlyLimit),
            cancellationToken);

        if (result.IsFailure)
        {
            return ToErrorResult("Failed to retrieve subscription usage insights", result.Errors.FirstOrDefault());
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> RecordUsageSnapshot(
        Guid organizationId,
        Guid subscriptionId,
        [FromBody] RecordSubscriptionUsageSnapshotRequest request,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.IsAuthorizedForOrganization(organizationId))
        {
            return Results.Forbid();
        }

        if (!Enum.TryParse<UsagePeriodType>(request.PeriodType, true, out var periodType))
        {
            return Results.Problem(
                title: "Invalid usage period type",
                detail: "The provided usage period type is invalid.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var command = new ManageSubscriptionUsageCommand(
            organizationId,
            subscriptionId,
            SubscriptionUsageAction.RecordSnapshot,
            request.HoneypotsActive,
            request.StorageUsedGb,
            request.ApiCallsCount,
            request.ActiveUsers,
            request.EventsCaptured,
            periodType);

        var result = await sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            return ToErrorResult("Failed to record usage snapshot", result.Errors.FirstOrDefault());
        }

        return Results.Ok(new { message = "Usage snapshot recorded successfully." });
    }

    private static async Task<IResult> FinalizeMonthlyUsage(
        Guid organizationId,
        Guid subscriptionId,
        int year,
        int month,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.IsAuthorizedForOrganization(organizationId))
        {
            return Results.Forbid();
        }

        var command = new ManageSubscriptionUsageCommand(
            organizationId,
            subscriptionId,
            SubscriptionUsageAction.FinalizeMonthlyUsage,
            Year: year,
            Month: month);

        var result = await sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            return ToErrorResult("Failed to finalize monthly usage", result.Errors.FirstOrDefault());
        }

        return Results.Ok(new { message = "Monthly usage finalized successfully." });
    }

    private static async Task<IResult> MarkMonthlyUsageAsBilled(
        Guid organizationId,
        Guid subscriptionId,
        int year,
        int month,
        [FromBody] MarkMonthlyUsageBilledRequest request,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.IsAuthorizedForOrganization(organizationId))
        {
            return Results.Forbid();
        }

        var command = new ManageSubscriptionUsageCommand(
            organizationId,
            subscriptionId,
            SubscriptionUsageAction.MarkMonthlyUsageAsBilled,
            Year: year,
            Month: month,
            InvoiceId: request.InvoiceId);

        var result = await sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            return ToErrorResult("Failed to mark monthly usage as billed", result.Errors.FirstOrDefault());
        }

        return Results.Ok(new { message = "Monthly usage marked as billed successfully." });
    }

    private static async Task<IResult> CheckQuotaOperation(
        Guid organizationId,
        Guid subscriptionId,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken,
        [FromQuery] int additionalHoneypots = 0,
        [FromQuery] decimal additionalStorageGb = 0)
    {
        if (!httpContext.User.IsAuthorizedForOrganization(organizationId))
        {
            return Results.Forbid();
        }

        var result = await sender.Send(
            new CheckSubscriptionQuotaOperationQuery(
                organizationId,
                subscriptionId,
                additionalHoneypots,
                additionalStorageGb),
            cancellationToken);

        if (result.IsFailure)
        {
            return ToErrorResult("Failed to check subscription quota operation", result.Errors.FirstOrDefault());
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> ApplyLifecycleActionAsync(
        Guid organizationId,
        Guid subscriptionId,
        SubscriptionLifecycleAction action,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken,
        Guid? newPlanId = null,
        string? reason = null,
        DateTime? renewalEndDate = null)
    {
        if (!httpContext.User.IsAuthorizedForOrganization(organizationId))
        {
            return Results.Forbid();
        }

        var command = new ManageSubscriptionLifecycleCommand(
            organizationId,
            subscriptionId,
            action,
            newPlanId,
            reason,
            renewalEndDate);

        var result = await sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            return ToErrorResult("Failed to update subscription", result.Errors.FirstOrDefault());
        }

        return Results.Ok(new { message = GetLifecycleSuccessMessage(action) });
    }

    private static IResult ToErrorResult(string title, Error? error)
    {
        var statusCode = string.Equals(error?.Code, "Subscription.NotFound", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(error?.Code, "Plan.NotFound", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(error?.Code, "Quota.NotFound", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(error?.Code, "Quota.SummaryNotFound", StringComparison.OrdinalIgnoreCase)
            ? StatusCodes.Status404NotFound
            : string.Equals(error?.Code, "Plan.PricingNotFound", StringComparison.OrdinalIgnoreCase)
                || string.Equals(error?.Code, "Plan.CannotDeactivateWithActiveSubscriptions", StringComparison.OrdinalIgnoreCase)
                || string.Equals(error?.Code, "Quota.HardLimitEnforced", StringComparison.OrdinalIgnoreCase)
                || string.Equals(error?.Code, "Quota.HoneypotLimitExceeded", StringComparison.OrdinalIgnoreCase)
                || string.Equals(error?.Code, "Quota.StorageLimitExceeded", StringComparison.OrdinalIgnoreCase)
                || string.Equals(error?.Code, "Quota.ApiCallLimitExceeded", StringComparison.OrdinalIgnoreCase)
                || string.Equals(error?.Code, "Quota.SummaryAlreadyFinalized", StringComparison.OrdinalIgnoreCase)
                || string.Equals(error?.Code, "Quota.AlreadyBilled", StringComparison.OrdinalIgnoreCase)
                || string.Equals(error?.Code, "Subscription.InvalidStatusTransition", StringComparison.OrdinalIgnoreCase)
                || string.Equals(error?.Code, "Subscription.AlreadyActive", StringComparison.OrdinalIgnoreCase)
                || string.Equals(error?.Code, "Subscription.AlreadySuspended", StringComparison.OrdinalIgnoreCase)
                || string.Equals(error?.Code, "Subscription.AlreadyCancelled", StringComparison.OrdinalIgnoreCase)
                || string.Equals(error?.Code, "Subscription.CannotRenewCancelled", StringComparison.OrdinalIgnoreCase)
                || string.Equals(error?.Code, "Subscription.CannotRenew", StringComparison.OrdinalIgnoreCase)
                || string.Equals(error?.Code, "Subscription.PlanChangeNotAllowed", StringComparison.OrdinalIgnoreCase)
                || string.Equals(error?.Code, "Subscription.CannotDowngradeWithHighUsage", StringComparison.OrdinalIgnoreCase)
                || string.Equals(error?.Code, "Subscription.ConcurrencyConflict", StringComparison.OrdinalIgnoreCase)
                ? StatusCodes.Status409Conflict
                : StatusCodes.Status400BadRequest;

        return Results.Problem(
            title: title,
            detail: ToClientDetail(error),
            statusCode: statusCode);
    }

    private static string GetLifecycleSuccessMessage(SubscriptionLifecycleAction action)
    {
        return action switch
        {
            SubscriptionLifecycleAction.Activate => "Subscription activated successfully.",
            SubscriptionLifecycleAction.Suspend => "Subscription suspended successfully.",
            SubscriptionLifecycleAction.Cancel => "Subscription cancelled successfully.",
            SubscriptionLifecycleAction.EnableAutoRenew => "Subscription auto-renew enabled successfully.",
            SubscriptionLifecycleAction.DisableAutoRenew => "Subscription auto-renew disabled successfully.",
            SubscriptionLifecycleAction.ChangePlan => "Subscription plan changed successfully.",
            SubscriptionLifecycleAction.Renew => "Subscription renewed successfully.",
            SubscriptionLifecycleAction.ScheduleCancellation => "Subscription cancellation scheduled successfully.",
            _ => "Subscription updated successfully."
        };
    }

    private static string ToClientDetail(Error? error)
    {
        return error is null ? "Request could not be completed." : $"Request failed. Code: {error.Code}.";
    }
}
