using MediatR;
using Microsoft.AspNetCore.Mvc;
using Trap_Intel.Api.Authorization;
using Trap_Intel.Api.Endpoints.Subscriptions.Models;
using Trap_Intel.Api.Extensions;
using Trap_Intel.Api.Filters;
using Trap_Intel.Application.Subscriptions.Commands.CreateSubscription;
using Trap_Intel.Application.Subscriptions.Commands.ManageSubscription;
using Trap_Intel.Application.Subscriptions.Queries.GetSubscription;
using Trap_Intel.Domain.Identity.Authorization;

namespace Trap_Intel.Api.Endpoints.Subscriptions;

internal sealed class SubscriptionManagementEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/organizations/{organizationId:guid}/subscriptions")
            .WithTags("Subscriptions")
            .AddEndpointFilter<ValidationFilter>()
            .RequireAuthorization();

        group.MapGet("/current", GetCurrent)
            .WithName("GetCurrentSubscription")
            .WithSummary("Get current active subscription for organization")
            .RequirePermission(Permissions.Organization.ManageBilling)
            .Produces<SubscriptionDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/{subscriptionId:guid}", GetById)
            .WithName("GetSubscriptionById")
            .WithSummary("Get subscription details")
            .RequirePermission(Permissions.Organization.ManageBilling)
            .Produces<SubscriptionDetailDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/", Create)
            .WithName("CreateSubscription")
            .WithSummary("Create new subscription for organization")
            .RequirePermission(Permissions.Organization.ManageBilling)
            .Produces<SubscriptionDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapPost("/{subscriptionId:guid}/activate", Activate)
            .WithName("ActivateSubscription")
            .WithSummary("Activates a subscription")
            .RequirePermission(Permissions.Organization.ManageBilling)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/{subscriptionId:guid}/suspend", Suspend)
            .WithName("SuspendSubscription")
            .WithSummary("Suspends a subscription")
            .RequirePermission(Permissions.Organization.ManageBilling)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/{subscriptionId:guid}/cancel", Cancel)
            .WithName("CancelSubscription")
            .WithSummary("Cancel subscription")
            .RequirePermission(Permissions.Organization.ManageBilling)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetCurrent(
        Guid organizationId,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        // Implementation retrieves current active subscription
    }

    private static async Task<IResult> GetById(
        Guid organizationId,
        Guid subscriptionId,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        // Implementation retrieves subscription details
    }

    private static async Task<IResult> Create(
        Guid organizationId,
        [FromBody] CreateSubscriptionRequest request,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        // Implementation creates new subscription
    }

    private static async Task<IResult> Activate(
        Guid organizationId,
        Guid subscriptionId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        // Implementation activates subscription
    }

    private static async Task<IResult> Suspend(
        Guid organizationId,
        Guid subscriptionId,
        [FromBody] SuspendSubscriptionRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        // Implementation suspends subscription
    }

    private static async Task<IResult> Cancel(
        Guid organizationId,
        Guid subscriptionId,
        [FromBody] CancelSubscriptionRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        // Implementation cancels subscription
    }
}
