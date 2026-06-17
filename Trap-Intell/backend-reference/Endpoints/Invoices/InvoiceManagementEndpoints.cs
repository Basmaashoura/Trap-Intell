using MediatR;
using Microsoft.AspNetCore.Mvc;
using Trap_Intel.Api.Authorization;
using Trap_Intel.Api.Endpoints.Invoices.Models;
using Trap_Intel.Api.Extensions;
using Trap_Intel.Api.Filters;
using Trap_Intel.Application.Billing.Commands.CancelInvoice;
using Trap_Intel.Application.Billing.Commands.ProcessInvoicePayment;
using Trap_Intel.Application.Billing.Queries.GetInvoices;
using Trap_Intel.Domain.Identity.Authorization;

namespace Trap_Intel.Api.Endpoints.Invoices;

internal sealed class InvoiceManagementEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/organizations/{organizationId:guid}/invoices")
            .WithTags("Invoices")
            .AddEndpointFilter<ValidationFilter>()
            .RequireAuthorization();

        group.MapGet("/", GetInvoices)
            .WithName("GetInvoices")
            .WithSummary("List invoices for organization")
            .RequirePermission(Permissions.Organization.ManageBilling)
            .Produces<PagedResult<InvoiceDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapGet("/{invoiceId:guid}", GetInvoiceById)
            .WithName("GetInvoiceById")
            .WithSummary("Get invoice details")
            .RequirePermission(Permissions.Organization.ManageBilling)
            .Produces<InvoiceDetailDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/{invoiceId:guid}/issue", IssueInvoice)
            .WithName("IssueInvoice")
            .WithSummary("Issue an invoice")
            .RequirePermission(Permissions.Organization.ManageBilling)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/{invoiceId:guid}/pay", ProcessPayment)
            .WithName("ProcessInvoicePayment")
            .WithSummary("Process payment for invoice")
            .RequirePermission(Permissions.Organization.ManageBilling)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/{invoiceId:guid}/cancel", CancelInvoice)
            .WithName("CancelInvoice")
            .WithSummary("Cancel an invoice")
            .RequirePermission(Permissions.Organization.ManageBilling)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/{invoiceId:guid}/pdf", ExportInvoicePdf)
            .WithName("ExportInvoicePdf")
            .WithSummary("Export invoice as PDF")
            .RequirePermission(Permissions.Organization.ManageBilling)
            .Produces("application/pdf", StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetInvoices(
        Guid organizationId,
        [FromQuery] string? status,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        // Implementation retrieves invoices with optional status filtering
    }

    private static async Task<IResult> GetInvoiceById(
        Guid organizationId,
        Guid invoiceId,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        // Implementation retrieves invoice details
    }

    private static async Task<IResult> IssueInvoice(
        Guid organizationId,
        Guid invoiceId,
        [FromBody] IssueInvoiceRequest? request,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        // Implementation issues the invoice
    }

    private static async Task<IResult> ProcessPayment(
        Guid organizationId,
        Guid invoiceId,
        [FromBody] ProcessInvoicePaymentRequest request,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        // Implementation processes invoice payment
    }

    private static async Task<IResult> CancelInvoice(
        Guid organizationId,
        Guid invoiceId,
        [FromBody] CancelInvoiceRequest request,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        // Implementation cancels the invoice
    }

    private static async Task<IResult> ExportInvoicePdf(
        Guid organizationId,
        Guid invoiceId,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        // Implementation exports invoice as PDF
    }
}
