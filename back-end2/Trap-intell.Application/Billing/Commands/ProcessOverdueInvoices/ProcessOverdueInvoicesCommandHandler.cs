using MediatR;
using Microsoft.Extensions.Logging;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Billing;

namespace Trap_Intel.Application.Billing.Commands.ProcessOverdueInvoices;

internal sealed class ProcessOverdueInvoicesCommandHandler : IRequestHandler<ProcessOverdueInvoicesCommand, Result<OverdueInvoiceProcessingResultDto>>
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProcessOverdueInvoicesCommandHandler> _logger;

    public ProcessOverdueInvoicesCommandHandler(
        IInvoiceRepository invoiceRepository,
        IUnitOfWork unitOfWork,
        ILogger<ProcessOverdueInvoicesCommandHandler> logger)
    {
        _invoiceRepository = invoiceRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<OverdueInvoiceProcessingResultDto>> Handle(ProcessOverdueInvoicesCommand request, CancellationToken cancellationToken)
    {
        var runAtUtc = NormalizeRunDate(request.RunAtUtc);

        var processedInvoices = 0;
        var markedOverdueInvoices = 0;
        var lateFeeAppliedInvoices = 0;
        var failedInvoices = 0;
        var errors = new List<string>();

        var candidates = await _invoiceRepository.GetOverdueAsync(cancellationToken);

        foreach (var invoice in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            processedInvoices++;

            var hasChanges = false;

            try
            {
                var shouldMarkOverdue =
                    invoice.Status == InvoiceStatus.Issued &&
                    invoice.DueDate.HasValue &&
                    invoice.DueDate.Value < runAtUtc;

                if (shouldMarkOverdue)
                {
                    if (!request.DryRun)
                    {
                        var markResult = invoice.MarkAsOverdue();
                        if (markResult.IsFailure)
                        {
                            failedInvoices++;
                            errors.Add($"Invoice {invoice.Id}: {markResult.Errors.FirstOrDefault()?.Code}");
                            continue;
                        }
                    }

                    markedOverdueInvoices++;
                    hasChanges = !request.DryRun;
                }

                if (request.ApplyLateFees)
                {
                    var canApplyLateFee = request.DryRun
                        ? (shouldMarkOverdue || invoice.Status == InvoiceStatus.Overdue) && !invoice.HasLateFeeApplied
                        : invoice.Status == InvoiceStatus.Overdue && !invoice.HasLateFeeApplied;

                    if (canApplyLateFee)
                    {
                        var projectedLateFee = request.DryRun && shouldMarkOverdue
                            ? CalculateProjectedLateFee(invoice, request.LateFeePercent, runAtUtc)
                            : invoice.CalculateLateFee(request.LateFeePercent);

                        if (projectedLateFee > 0)
                        {
                            if (!request.DryRun)
                            {
                                var lateFeeResult = invoice.ApplyLateFee(request.LateFeePercent);
                                if (lateFeeResult.IsFailure)
                                {
                                    failedInvoices++;
                                    errors.Add($"Invoice {invoice.Id}: {lateFeeResult.Errors.FirstOrDefault()?.Code}");
                                    continue;
                                }

                                hasChanges = true;
                            }

                            lateFeeAppliedInvoices++;
                        }
                    }
                }

                if (!request.DryRun && hasChanges)
                {
                    await _invoiceRepository.UpdateAsync(invoice, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                failedInvoices++;
                errors.Add($"Invoice {invoice.Id}: {ex.Message}");
            }
        }

        if (!request.DryRun)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation(
            "Overdue invoice processing completed. RunAt={RunAt}, Processed={Processed}, MarkedOverdue={MarkedOverdue}, LateFeesApplied={LateFeesApplied}, Failed={Failed}, DryRun={DryRun}",
            runAtUtc,
            processedInvoices,
            markedOverdueInvoices,
            lateFeeAppliedInvoices,
            failedInvoices,
            request.DryRun);

        var summary = new OverdueInvoiceProcessingResultDto(
            processedInvoices,
            markedOverdueInvoices,
            lateFeeAppliedInvoices,
            failedInvoices,
            errors);

        return Result.Success(summary);
    }

    private static DateTime NormalizeRunDate(DateTime? runAtUtc)
    {
        var value = runAtUtc ?? DateTime.UtcNow;

        if (value.Kind == DateTimeKind.Unspecified)
        {
            return DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        return value.Kind == DateTimeKind.Local
            ? value.ToUniversalTime()
            : value;
    }

    private static decimal CalculateProjectedLateFee(
        Invoice invoice,
        decimal lateFeePercent,
        DateTime runAtUtc,
        int gracePeriodDays = 7)
    {
        if (!invoice.DueDate.HasValue)
        {
            return 0;
        }

        var daysOverdue = (runAtUtc - invoice.DueDate.Value).Days;
        if (daysOverdue <= gracePeriodDays)
        {
            return 0;
        }

        return Math.Round(invoice.Amount.TotalAmount * (lateFeePercent / 100m), 2);
    }
}
