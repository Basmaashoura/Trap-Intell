using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Trap_Intel.Application.Billing.Commands.GenerateMonthlyInvoices;

namespace Trap_Intel.Infrastructure.Billing.BackgroundServices;

internal sealed class MonthlyInvoiceGenerationBackgroundService : BackgroundService
{
    private static readonly TimeOnly DailyRunTimeUtc = new(1, 0);

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MonthlyInvoiceGenerationBackgroundService> _logger;

    public MonthlyInvoiceGenerationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<MonthlyInvoiceGenerationBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Monthly invoice generation background service started.");

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

                await RunGenerationAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Monthly invoice generation background run failed.");
                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            }
        }

        _logger.LogInformation("Monthly invoice generation background service stopped.");
    }

    private async Task RunGenerationAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var result = await sender.Send(new GenerateMonthlyInvoicesCommand(DateTime.UtcNow), cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning(
                "Monthly invoice generation command failed. Error={Error}",
                result.Errors.FirstOrDefault()?.Code);
            return;
        }

        var summary = result.Value;

        _logger.LogInformation(
            "Monthly invoice generation run completed. Processed={Processed}, Generated={Generated}, Skipped={Skipped}, Failed={Failed}",
            summary.ProcessedSubscriptions,
            summary.GeneratedInvoices,
            summary.SkippedInvoices,
            summary.FailedInvoices);

        if (summary.Errors.Count > 0)
        {
            foreach (var error in summary.Errors)
            {
                _logger.LogWarning("Monthly invoice generation detail: {Error}", error);
            }
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
