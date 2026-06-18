using MediatR;
using Microsoft.Extensions.Logging;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Billing;
using Trap_Intel.Domain.Plans;
using Trap_Intel.Domain.Subscriptions;

namespace Trap_Intel.Application.Billing.Commands.GenerateMonthlyInvoices;

internal sealed class GenerateMonthlyInvoicesCommandHandler : IRequestHandler<GenerateMonthlyInvoicesCommand, Result<MonthlyInvoiceGenerationResultDto>>
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IPlanRepository _planRepository;
    private readonly IInvoiceNumberGenerator _invoiceNumberGenerator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GenerateMonthlyInvoicesCommandHandler> _logger;

    public GenerateMonthlyInvoicesCommandHandler(
        ISubscriptionRepository subscriptionRepository,
        IInvoiceRepository invoiceRepository,
        IPlanRepository planRepository,
        IInvoiceNumberGenerator invoiceNumberGenerator,
        IUnitOfWork unitOfWork,
        ILogger<GenerateMonthlyInvoicesCommandHandler> logger)
    {
        _subscriptionRepository = subscriptionRepository;
        _invoiceRepository = invoiceRepository;
        _planRepository = planRepository;
        _invoiceNumberGenerator = invoiceNumberGenerator;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<MonthlyInvoiceGenerationResultDto>> Handle(GenerateMonthlyInvoicesCommand request, CancellationToken cancellationToken)
    {
        var runAtUtc = NormalizeRunDate(request.RunAtUtc);

        var processedSubscriptions = 0;
        var generatedInvoices = 0;
        var skippedInvoices = 0;
        var failedInvoices = 0;
        var errors = new List<string>();

        var createInvoiceService = new CreateInvoiceService(
            _subscriptionRepository,
            _invoiceRepository,
            _planRepository,
            _invoiceNumberGenerator);

        var activeSubscriptions = await _subscriptionRepository.GetByStatusAsync(SubscriptionStatus.Active, cancellationToken);
        var monthlySubscriptions = activeSubscriptions.Where(subscription => subscription.BillingCycle == BillingCycle.Monthly);

        foreach (var subscription in monthlySubscriptions)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var periodEnd = subscription.Period.EndDate;
            if (!periodEnd.HasValue || periodEnd.Value.Date > runAtUtc.Date)
            {
                continue;
            }

            processedSubscriptions++;

            if (!subscription.IsAutoRenew || subscription.IsCancellationScheduled)
            {
                skippedInvoices++;
                continue;
            }

            var periodStart = subscription.Period.StartDate;
            var isAlreadyInvoiced = await _invoiceRepository.ExistsForSubscriptionPeriodAsync(
                subscription.Id,
                periodStart,
                periodEnd.Value,
                cancellationToken);
            if (isAlreadyInvoiced)
            {
                skippedInvoices++;
                continue;
            }

            if (request.DryRun)
            {
                generatedInvoices++;
                continue;
            }

            var invoiceResult = await createInvoiceService.CreateAsync(
                subscriptionId: subscription.Id,
                billingPeriodStart: periodStart,
                billingPeriodEnd: periodEnd.Value,
                taxRate: 0,
                overageCharges: subscription.CurrentUsage.OverageCharges,
                cancellationToken: cancellationToken);

            if (invoiceResult.IsFailure)
            {
                failedInvoices++;
                errors.Add($"Subscription {subscription.Id}: {invoiceResult.Errors.FirstOrDefault()?.Code}");
                continue;
            }

            var invoice = invoiceResult.Value;
            var issueResult = invoice.Issue(daysDue: 30);
            if (issueResult.IsFailure)
            {
                failedInvoices++;
                errors.Add($"Invoice {invoice.Id}: {issueResult.Errors.FirstOrDefault()?.Code}");
                continue;
            }

            generatedInvoices++;
        }

        if (!request.DryRun)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation(
            "Monthly invoice generation completed. RunAt={RunAt}, Processed={Processed}, Generated={Generated}, Skipped={Skipped}, Failed={Failed}, DryRun={DryRun}",
            runAtUtc,
            processedSubscriptions,
            generatedInvoices,
            skippedInvoices,
            failedInvoices,
            request.DryRun);

        var summary = new MonthlyInvoiceGenerationResultDto(
            processedSubscriptions,
            generatedInvoices,
            skippedInvoices,
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
}
