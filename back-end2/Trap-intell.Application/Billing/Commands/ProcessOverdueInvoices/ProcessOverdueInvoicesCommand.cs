using FluentValidation;
using MediatR;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Application.Billing.Commands.ProcessOverdueInvoices;

public sealed record ProcessOverdueInvoicesCommand(
    DateTime? RunAtUtc = null,
    bool ApplyLateFees = true,
    decimal LateFeePercent = 5m,
    bool DryRun = false) : IRequest<Result<OverdueInvoiceProcessingResultDto>>;

public sealed class ProcessOverdueInvoicesCommandValidator : AbstractValidator<ProcessOverdueInvoicesCommand>
{
    public ProcessOverdueInvoicesCommandValidator()
    {
        RuleFor(x => x.LateFeePercent)
            .InclusiveBetween(0, 100);
    }
}

public sealed record OverdueInvoiceProcessingResultDto(
    int ProcessedInvoices,
    int MarkedOverdueInvoices,
    int LateFeeAppliedInvoices,
    int FailedInvoices,
    IReadOnlyList<string> Errors);
