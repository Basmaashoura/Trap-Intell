using MediatR;
using Microsoft.AspNetCore.Mvc;
using Trap_Intel.Api.Authorization;
using Trap_Intel.Api.Endpoints.PaymentMethods.Models;
using Trap_Intel.Api.Extensions;
using Trap_Intel.Api.Filters;
using Trap_Intel.Application.Billing.Commands.CreatePaymentMethod;
using Trap_Intel.Application.Billing.Commands.DeactivatePaymentMethod;
using Trap_Intel.Application.Billing.Commands.SetDefaultPaymentMethod;
using Trap_Intel.Application.Billing.Commands.UpdatePaymentMethod;
using Trap_Intel.Application.Billing.Queries.GetOrganizationPaymentMethods;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Billing;
using Trap_Intel.Domain.Identity.Authorization;

namespace Trap_Intel.Api.Endpoints.PaymentMethods;

internal sealed class PaymentMethodManagementEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/organizations/{organizationId:guid}/payment-methods")
            .WithTags("Billing - Payment Methods")
            .AddEndpointFilter<ValidationFilter>()
            .RequireAuthorization();

        group.MapGet("/", GetPaymentMethods)
            .WithName("GetOrganizationPaymentMethods")
            .WithSummary("Gets payment methods for organization")
            .RequirePermission(Permissions.Organization.ManageBilling)
            .Produces<IReadOnlyList<PaymentMethodSummaryDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/", CreatePaymentMethod)
            .WithName("CreatePaymentMethod")
            .WithSummary("Creates a payment method for organization")
            .RequirePermission(Permissions.Organization.ManageBilling)
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status409Conflict)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPut("/{paymentMethodId:guid}", UpdatePaymentMethod)
            .WithName("UpdatePaymentMethod")
            .WithSummary("Updates payment method details")
            .RequirePermission(Permissions.Organization.ManageBilling)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/{paymentMethodId:guid}/set-default", SetDefaultPaymentMethod)
            .WithName("SetDefaultPaymentMethod")
            .WithSummary("Sets default payment method for organization")
            .RequirePermission(Permissions.Organization.ManageBilling)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/{paymentMethodId:guid}/deactivate", DeactivatePaymentMethod)
            .WithName("DeactivatePaymentMethod")
            .WithSummary("Deactivates a payment method")
            .RequirePermission(Permissions.Organization.ManageBilling)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> GetPaymentMethods(
        Guid organizationId,
        [FromQuery] string? status,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.IsAuthorizedForOrganization(organizationId))
        {
            return Results.Forbid();
        }

        PaymentMethodStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<PaymentMethodStatus>(status, true, out var statusValue))
            {
                return Results.Problem(
                    title: "Invalid payment method status",
                    detail: "Unsupported status filter. Allowed values: Active, Inactive, Expired, Suspended.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            parsedStatus = statusValue;
        }

        var result = await sender.Send(new GetOrganizationPaymentMethodsQuery(organizationId, parsedStatus), cancellationToken);
        if (result.IsFailure)
        {
            return Results.Problem(
                title: "Failed to retrieve payment methods",
                detail: ToClientDetail(result.Errors.FirstOrDefault()),
                statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> CreatePaymentMethod(
        Guid organizationId,
        [FromBody] CreatePaymentMethodRequest request,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.IsAuthorizedForOrganization(organizationId))
        {
            return Results.Forbid();
        }

        if (!Enum.TryParse<PaymentMethodType>(request.Type, true, out var paymentMethodType))
        {
            return Results.Problem(
                title: "Invalid payment method type",
                detail: "The provided payment method type is invalid.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var command = new CreatePaymentMethodCommand(
            organizationId,
            paymentMethodType,
            request.LastFourDigits,
            request.CardBrand,
            request.PaymentProcessor,
            request.Token,
            request.ExpiresAt,
            request.BillingContactEmail,
            request.IsDefault);

        var result = await sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            var firstError = result.Errors.FirstOrDefault();
            var isConflict = string.Equals(firstError?.Code, "PaymentMethod.DefaultConflict", StringComparison.OrdinalIgnoreCase);

            return Results.Problem(
                title: "Failed to create payment method",
                detail: ToClientDetail(firstError),
                statusCode: isConflict ? StatusCodes.Status409Conflict : StatusCodes.Status400BadRequest);
        }

        return Results.Created(
            $"/api/organizations/{organizationId}/payment-methods/{result.Value}",
            new { paymentMethodId = result.Value });
    }

    private static async Task<IResult> UpdatePaymentMethod(
        Guid organizationId,
        Guid paymentMethodId,
        [FromBody] UpdatePaymentMethodRequest request,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.IsAuthorizedForOrganization(organizationId))
        {
            return Results.Forbid();
        }

        var command = new UpdatePaymentMethodCommand(
            organizationId,
            paymentMethodId,
            request.LastFourDigits,
            request.CardBrand,
            request.PaymentProcessor,
            request.Token,
            request.ExpiresAt,
            request.BillingContactEmail);

        var result = await sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            var firstError = result.Errors.FirstOrDefault();
            var isNotFound = string.Equals(firstError?.Code, "PaymentMethod.NotFound", StringComparison.OrdinalIgnoreCase);

            return Results.Problem(
                title: "Failed to update payment method",
                detail: ToClientDetail(firstError),
                statusCode: isNotFound ? StatusCodes.Status404NotFound : StatusCodes.Status400BadRequest);
        }

        return Results.Ok(new { message = "Payment method updated successfully." });
    }

    private static async Task<IResult> SetDefaultPaymentMethod(
        Guid organizationId,
        Guid paymentMethodId,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.IsAuthorizedForOrganization(organizationId))
        {
            return Results.Forbid();
        }

        var result = await sender.Send(new SetDefaultPaymentMethodCommand(organizationId, paymentMethodId), cancellationToken);
        if (result.IsFailure)
        {
            var firstError = result.Errors.FirstOrDefault();
            var isNotFound = string.Equals(firstError?.Code, "PaymentMethod.NotFound", StringComparison.OrdinalIgnoreCase);
            var isConflict = string.Equals(firstError?.Code, "PaymentMethod.DefaultConflict", StringComparison.OrdinalIgnoreCase);

            return Results.Problem(
                title: "Failed to set default payment method",
                detail: ToClientDetail(firstError),
                statusCode: isNotFound
                    ? StatusCodes.Status404NotFound
                    : isConflict
                        ? StatusCodes.Status409Conflict
                        : StatusCodes.Status400BadRequest);
        }

        return Results.Ok(new { message = "Default payment method updated successfully." });
    }

    private static async Task<IResult> DeactivatePaymentMethod(
        Guid organizationId,
        Guid paymentMethodId,
        [FromBody] DeactivatePaymentMethodRequest? request,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.IsAuthorizedForOrganization(organizationId))
        {
            return Results.Forbid();
        }

        var reason = string.IsNullOrWhiteSpace(request?.Reason)
            ? "Deactivated by organization administrator"
            : request!.Reason!;

        var result = await sender.Send(
            new DeactivatePaymentMethodCommand(organizationId, paymentMethodId, reason),
            cancellationToken);

        if (result.IsFailure)
        {
            var firstError = result.Errors.FirstOrDefault();
            var isNotFound = string.Equals(firstError?.Code, "PaymentMethod.NotFound", StringComparison.OrdinalIgnoreCase);
            var isConflict = string.Equals(firstError?.Code, "PaymentMethod.DefaultConflict", StringComparison.OrdinalIgnoreCase);

            return Results.Problem(
                title: "Failed to deactivate payment method",
                detail: ToClientDetail(firstError),
                statusCode: isNotFound
                    ? StatusCodes.Status404NotFound
                    : isConflict
                        ? StatusCodes.Status409Conflict
                        : StatusCodes.Status400BadRequest);
        }

        return Results.Ok(new { message = "Payment method deactivated successfully." });
    }

    private static string ToClientDetail(Error? error)
    {
        return error is null ? "Request could not be completed." : $"Request failed. Code: {error.Code}.";
    }
}
