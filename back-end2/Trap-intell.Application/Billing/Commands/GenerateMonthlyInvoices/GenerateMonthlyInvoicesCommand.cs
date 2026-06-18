using MediatR;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Application.Billing.Commands.GenerateMonthlyInvoices;

public sealed record GenerateMonthlyInvoicesCommand(
    DateTime? RunAtUtc = null,
    bool DryRun = false) : IRequest<Result<MonthlyInvoiceGenerationResultDto>>;

public sealed record MonthlyInvoiceGenerationResultDto(
    int ProcessedSubscriptions,
    int GeneratedInvoices,
    int SkippedInvoices,
    int FailedInvoices,
    IReadOnlyList<string> Errors);
