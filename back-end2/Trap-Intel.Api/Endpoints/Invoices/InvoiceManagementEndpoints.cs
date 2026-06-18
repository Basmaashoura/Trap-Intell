using MediatR;
using Microsoft.AspNetCore.Mvc;
using Trap_Intel.Api.Authorization;
using Trap_Intel.Api.Endpoints.Invoices.Models;
using Trap_Intel.Api.Extensions;
using Trap_Intel.Api.Filters;
using Trap_Intel.Application.Billing.Commands.CancelInvoice;
using Trap_Intel.Application.Billing.Commands.IssueInvoice;
using Trap_Intel.Application.Billing.Commands.ProcessInvoicePayment;
using Trap_Intel.Application.Billing.Commands.RefundInvoice;
using Trap_Intel.Application.Billing.Queries.ExportInvoicePdf;
using Trap_Intel.Application.Billing.Queries.GetInvoiceById;
using Trap_Intel.Application.Billing.Queries.GetOrganizationInvoices;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Billing;
using Trap_Intel.Domain.Identity.Authorization;

namespace Trap_Intel.Api.Endpoints.Invoices;

internal sealed class InvoiceManagementEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/organizations/{organizationId:guid}/invoices")
            .WithTags("Billing - Invoices")
            .AddEndpointFilter<ValidationFilter>()
            .RequireAuthorization();

        group.MapGet("/", GetInvoices)
            .WithName("GetOrganizationInvoices")
            .WithSummary("Gets invoices for organization")
            .RequirePermission(Permissions.Organization.ManageBilling)
            .Produces<IReadOnlyList<InvoiceSummaryDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/{invoiceId:guid}", GetInvoiceById)
            .WithName("GetOrganizationInvoiceById")
            .WithSummary("Gets invoice details by id")
            .RequirePermission(Permissions.Organization.ManageBilling)
            .Produces<InvoiceDetailDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/{invoiceId:guid}/pdf", ExportInvoicePdf)
            .WithName("ExportOrganizationInvoicePdf")
            .WithSummary("Exports invoice as PDF")
            .RequirePermission(Permissions.Organization.ManageBilling)
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/{invoiceId:guid}/issue", IssueInvoice)
            .WithName("IssueInvoice")
            .WithSummary("Issues a draft invoice")
            .RequirePermission(Permissions.Organization.ManageBilling)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/{invoiceId:guid}/cancel", CancelInvoice)
            .WithName("CancelInvoice")
            .WithSummary("Cancels an invoice")
            .RequirePermission(Permissions.Organization.ManageBilling)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/{invoiceId:guid}/refund", RefundInvoice)
            .WithName("RefundInvoice")
            .WithSummary("Refunds a paid invoice")
            .RequirePermission(Permissions.Organization.ManageBilling)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/{invoiceId:guid}/process-payment", ProcessPayment)
            .WithName("ProcessInvoicePayment")
            .WithSummary("Processes payment for an invoice using a payment method")
            .RequirePermission(Permissions.Organization.ManageBilling)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/{invoiceId:guid}/mark-paid", MarkPaidDeprecated)
            .WithName("MarkInvoiceAsPaidDeprecated")
            .WithSummary("DEPRECATED: this endpoint was removed. Use process-payment.")
            .RequirePermission(Permissions.Organization.ManageBilling)
            .Produces(StatusCodes.Status410Gone)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> GetInvoices(
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

        InvoiceStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<InvoiceStatus>(status, true, out var tmpStatus))
            {
                return Results.Problem(
                    title: "Invalid invoice status",
                    detail: "Unsupported status filter. Allowed values: Draft, Issued, Paid, Overdue, Cancelled, Refunded.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            parsedStatus = tmpStatus;
        }

        var result = await sender.Send(new GetOrganizationInvoicesQuery(organizationId, parsedStatus), cancellationToken);
        if (result.IsFailure)
        {
            return Results.Problem(
                title: "Failed to retrieve invoices",
                detail: ToClientDetail(result.Errors.FirstOrDefault()),
                statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> GetInvoiceById(
        Guid organizationId,
        Guid invoiceId,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.IsAuthorizedForOrganization(organizationId))
        {
            return Results.Forbid();
        }

        var result = await sender.Send(new GetInvoiceByIdQuery(organizationId, invoiceId), cancellationToken);
        if (result.IsFailure)
        {
            var firstError = result.Errors.FirstOrDefault();
            var isNotFound = string.Equals(firstError?.Code, "Invoice.NotFound", StringComparison.OrdinalIgnoreCase);

            return Results.Problem(
                title: "Failed to retrieve invoice",
                detail: ToClientDetail(firstError),
                statusCode: isNotFound ? StatusCodes.Status404NotFound : StatusCodes.Status400BadRequest);
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> IssueInvoice(
        Guid organizationId,
        Guid invoiceId,
        [FromBody] IssueInvoiceRequest? request,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.IsAuthorizedForOrganization(organizationId))
        {
            return Results.Forbid();
        }

        var command = new IssueInvoiceCommand(
            organizationId,
            invoiceId,
            request?.DaysDue ?? 30);

        var result = await sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            var firstError = result.Errors.FirstOrDefault();
            var isNotFound = string.Equals(firstError?.Code, "Invoice.NotFound", StringComparison.OrdinalIgnoreCase);

            return Results.Problem(
                title: "Failed to issue invoice",
                detail: ToClientDetail(firstError),
                statusCode: isNotFound ? StatusCodes.Status404NotFound : StatusCodes.Status400BadRequest);
        }

        return Results.Ok(new { message = "Invoice issued successfully." });
    }

    private static async Task<IResult> ExportInvoicePdf(
        Guid organizationId,
        Guid invoiceId,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.IsAuthorizedForOrganization(organizationId))
        {
            return Results.Forbid();
        }

        var result = await sender.Send(new ExportInvoicePdfQuery(organizationId, invoiceId), cancellationToken);
        if (result.IsFailure)
        {
            var firstError = result.Errors.FirstOrDefault();
            var isNotFound = string.Equals(firstError?.Code, "Invoice.NotFound", StringComparison.OrdinalIgnoreCase);

            return Results.Problem(
                title: "Failed to export invoice PDF",
                detail: ToClientDetail(firstError),
                statusCode: isNotFound ? StatusCodes.Status404NotFound : StatusCodes.Status400BadRequest);
        }

        return Results.File(
            result.Value.Content,
            result.Value.ContentType,
            result.Value.FileName);
    }

    private static async Task<IResult> ProcessPayment(
        Guid organizationId,
        Guid invoiceId,
        [FromBody] ProcessInvoicePaymentRequest request,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.IsAuthorizedForOrganization(organizationId))
        {
            return Results.Forbid();
        }

        var command = new ProcessInvoicePaymentCommand(
            organizationId,
            invoiceId,
            request.PaymentMethodId,
            ResolveIdempotencyKey(request.IdempotencyKey, httpContext));

        var result = await sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            var firstError = result.Errors.FirstOrDefault();
            var isNotFound = string.Equals(firstError?.Code, "Invoice.NotFound", StringComparison.OrdinalIgnoreCase);

            return Results.Problem(
                title: "Failed to process invoice payment",
                detail: ToClientDetail(firstError),
                statusCode: isNotFound ? StatusCodes.Status404NotFound : StatusCodes.Status400BadRequest);
        }

        return Results.Ok(new { message = "Invoice payment processed successfully.", paymentId = result.Value });
    }

    private static async Task<IResult> CancelInvoice(
        Guid organizationId,
        Guid invoiceId,
        [FromBody] CancelInvoiceRequest? request,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.IsAuthorizedForOrganization(organizationId))
        {
            return Results.Forbid();
        }

        var reason = string.IsNullOrWhiteSpace(request?.Reason)
            ? "Cancelled by organization administrator."
            : request!.Reason!;

        var command = new CancelInvoiceCommand(
            organizationId,
            invoiceId,
            reason);

        var result = await sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            var firstError = result.Errors.FirstOrDefault();
            var isNotFound = string.Equals(firstError?.Code, "Invoice.NotFound", StringComparison.OrdinalIgnoreCase);

            return Results.Problem(
                title: "Failed to cancel invoice",
                detail: ToClientDetail(firstError),
                statusCode: isNotFound ? StatusCodes.Status404NotFound : StatusCodes.Status400BadRequest);
        }

        return Results.Ok(new { message = "Invoice cancelled successfully." });
    }

    private static async Task<IResult> RefundInvoice(
        Guid organizationId,
        Guid invoiceId,
        [FromBody] RefundInvoiceRequest request,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.IsAuthorizedForOrganization(organizationId))
        {
            return Results.Forbid();
        }

        var command = new RefundInvoiceCommand(
            organizationId,
            invoiceId,
            request.RefundAmount,
            request.Reason,
            ResolveIdempotencyKey(request.IdempotencyKey, httpContext));

        var result = await sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            var firstError = result.Errors.FirstOrDefault();
            var isNotFound = string.Equals(firstError?.Code, "Invoice.NotFound", StringComparison.OrdinalIgnoreCase);

            return Results.Problem(
                title: "Failed to refund invoice",
                detail: ToClientDetail(firstError),
                statusCode: isNotFound ? StatusCodes.Status404NotFound : StatusCodes.Status400BadRequest);
        }

        return Results.Ok(new { message = "Invoice refunded successfully.", refundId = result.Value });
    }

    private static IResult MarkPaidDeprecated(
        Guid organizationId,
        Guid invoiceId,
        HttpContext httpContext)
    {
        if (!httpContext.User.IsAuthorizedForOrganization(organizationId))
        {
            return Results.Forbid();
        }

        httpContext.Response.Headers.Append("Deprecation", "true");
        httpContext.Response.Headers.Append("Sunset", "Wed, 01 Oct 2026 00:00:00 GMT");
        httpContext.Response.Headers.Append(
            "Link",
            $"</api/organizations/{organizationId}/invoices/{invoiceId}/process-payment>; rel=\"successor-version\"");

        return Results.Problem(
            title: "Endpoint removed",
            detail: "The /mark-paid endpoint was removed for security hardening. Use POST /process-payment with { \"paymentMethodId\": \"<guid>\" } or omit paymentMethodId to use the default payment method.",
            statusCode: StatusCodes.Status410Gone);
    }

    private static string ToClientDetail(Error? error)
    {
        return error is null ? "Request could not be completed." : $"Request failed. Code: {error.Code}.";
    }

    private static string? ResolveIdempotencyKey(string? requestIdempotencyKey, HttpContext httpContext)
    {
        if (httpContext.Request.Headers.TryGetValue("Idempotency-Key", out var values))
        {
            var headerValue = values.ToString();
            if (!string.IsNullOrWhiteSpace(headerValue))
            {
                return headerValue.Trim();
            }
        }

        return string.IsNullOrWhiteSpace(requestIdempotencyKey)
            ? null
            : requestIdempotencyKey.Trim();
    }
}
