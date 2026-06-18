using MediatR;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Trap_Intel.Application.Billing.Commands.ProcessOverdueInvoices;
using Trap_Intel.Infrastructure.Billing.Observability;

namespace Trap_Intel.Infrastructure.Billing.BackgroundServices;

internal sealed class OverdueInvoiceProcessingBackgroundService : BackgroundService
{
    private static readonly TimeOnly DailyRunTimeUtc = new(2, 0);

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OverdueInvoiceProcessingBackgroundService> _logger;
    private readonly bool _runOnStartup;

    public OverdueInvoiceProcessingBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<OverdueInvoiceProcessingBackgroundService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _runOnStartup = configuration.GetValue<bool>("Billing:OverdueProcessing:RunOnStartup");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Overdue invoice processing background service started.");

        if (_runOnStartup && !stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Overdue invoice processing startup run is enabled. Executing immediate run.");
            await RunProcessingAsync(stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var delay = GetDelayUntilNextRun(DateTime.UtcNow);
                await Task.Delay(delay, stoppingToken);

                if (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                await RunProcessingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Overdue invoice processing background run failed.");
                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            }
        }

        _logger.LogInformation("Overdue invoice processing background service stopped.");
    }

    private async Task RunProcessingAsync(CancellationToken cancellationToken)
    {
        BillingBackgroundServiceMetrics.RecordOverdueRunStarted();
        var runStart = Stopwatch.GetTimestamp();

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();

            var result = await sender.Send(
                new ProcessOverdueInvoicesCommand(
                    RunAtUtc: DateTime.UtcNow,
                    ApplyLateFees: true,
                    LateFeePercent: 5m,
                    DryRun: false),
                cancellationToken);

            var elapsedMs = Stopwatch.GetElapsedTime(runStart).TotalMilliseconds;

            if (result.IsFailure)
            {
                BillingBackgroundServiceMetrics.RecordOverdueRunCommandFailed(elapsedMs);

                _logger.LogWarning(
                    "Overdue invoice processing command failed. Error={Error}, DurationMs={DurationMs}",
                    result.Errors.FirstOrDefault()?.Code,
                    elapsedMs);
                return;
            }

            var summary = result.Value;

            BillingBackgroundServiceMetrics.RecordOverdueRunCompleted(
                summary.ProcessedInvoices,
                summary.MarkedOverdueInvoices,
                summary.LateFeeAppliedInvoices,
                summary.FailedInvoices,
                elapsedMs);

            _logger.LogInformation(
                "Overdue invoice processing run completed. Processed={Processed}, MarkedOverdue={MarkedOverdue}, LateFeesApplied={LateFeesApplied}, Failed={Failed}, DurationMs={DurationMs}",
                summary.ProcessedInvoices,
                summary.MarkedOverdueInvoices,
                summary.LateFeeAppliedInvoices,
                summary.FailedInvoices,
                elapsedMs);

            if (summary.Errors.Count > 0)
            {
                foreach (var error in summary.Errors)
                {
                    _logger.LogWarning("Overdue invoice processing detail: {Error}", error);
                }
            }
        }
        catch
        {
            var elapsedMs = Stopwatch.GetElapsedTime(runStart).TotalMilliseconds;
            BillingBackgroundServiceMetrics.RecordOverdueRunFaulted(elapsedMs);
            throw;
        }
    }

    private static TimeSpan GetDelayUntilNextRun(DateTime utcNow)
    {
        var nextRunDate = utcNow.Date;
        if (TimeOnly.FromDateTime(utcNow) >= DailyRunTimeUtc)
        {
            nextRunDate = nextRunDate.AddDays(1);
        }

        var nextRun = nextRunDate.Add(DailyRunTimeUtc.ToTimeSpan());
        var delay = nextRun - utcNow;

        return delay <= TimeSpan.Zero ? TimeSpan.FromMinutes(1) : delay;
    }
}
