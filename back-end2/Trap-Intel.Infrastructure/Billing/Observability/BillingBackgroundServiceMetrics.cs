using System.Diagnostics.Metrics;

namespace Trap_Intel.Infrastructure.Billing.Observability;

internal static class BillingBackgroundServiceMetrics
{
    private static readonly Meter Meter = new("TrapIntel.Infrastructure.Billing.BackgroundServices", "1.0.0");

    private static readonly Counter<long> OverdueInvoiceRunsStarted =
        Meter.CreateCounter<long>(
            name: "billing.overdue_invoice_runs.started",
            unit: "{run}",
            description: "Number of overdue invoice processing runs started.");

    private static readonly Counter<long> OverdueInvoiceRunsCompleted =
        Meter.CreateCounter<long>(
            name: "billing.overdue_invoice_runs.completed",
            unit: "{run}",
            description: "Number of overdue invoice processing runs completed successfully.");

    private static readonly Counter<long> OverdueInvoiceRunsCommandFailed =
        Meter.CreateCounter<long>(
            name: "billing.overdue_invoice_runs.command_failed",
            unit: "{run}",
            description: "Number of overdue invoice processing runs that returned a command failure result.");

    private static readonly Counter<long> OverdueInvoiceRunsFaulted =
        Meter.CreateCounter<long>(
            name: "billing.overdue_invoice_runs.faulted",
            unit: "{run}",
            description: "Number of overdue invoice processing runs that faulted with an exception.");

    private static readonly Counter<long> OverdueInvoiceProcessedInvoices =
        Meter.CreateCounter<long>(
            name: "billing.overdue_invoice_runs.invoices_processed",
            unit: "{invoice}",
            description: "Total invoices processed across overdue invoice background runs.");

    private static readonly Counter<long> OverdueInvoiceMarkedInvoices =
        Meter.CreateCounter<long>(
            name: "billing.overdue_invoice_runs.invoices_marked_overdue",
            unit: "{invoice}",
            description: "Total invoices marked overdue across overdue invoice background runs.");

    private static readonly Counter<long> OverdueInvoiceLateFeesApplied =
        Meter.CreateCounter<long>(
            name: "billing.overdue_invoice_runs.late_fees_applied",
            unit: "{invoice}",
            description: "Total invoices that received late fees across overdue invoice background runs.");

    private static readonly Counter<long> OverdueInvoiceFailedInvoices =
        Meter.CreateCounter<long>(
            name: "billing.overdue_invoice_runs.invoices_failed",
            unit: "{invoice}",
            description: "Total invoices that failed processing across overdue invoice background runs.");

    private static readonly Histogram<double> OverdueInvoiceRunDurationMs =
        Meter.CreateHistogram<double>(
            name: "billing.overdue_invoice_runs.duration_ms",
            unit: "ms",
            description: "Duration of overdue invoice processing runs in milliseconds.");

    public static void RecordOverdueRunStarted()
    {
        OverdueInvoiceRunsStarted.Add(1);
    }

    public static void RecordOverdueRunCompleted(
        int processedInvoices,
        int markedOverdueInvoices,
        int lateFeeAppliedInvoices,
        int failedInvoices,
        double durationMs)
    {
        OverdueInvoiceRunsCompleted.Add(1);
        OverdueInvoiceProcessedInvoices.Add(ToNonNegativeLong(processedInvoices));
        OverdueInvoiceMarkedInvoices.Add(ToNonNegativeLong(markedOverdueInvoices));
        OverdueInvoiceLateFeesApplied.Add(ToNonNegativeLong(lateFeeAppliedInvoices));
        OverdueInvoiceFailedInvoices.Add(ToNonNegativeLong(failedInvoices));
        OverdueInvoiceRunDurationMs.Record(ToNonNegativeDouble(durationMs));
    }

    public static void RecordOverdueRunCommandFailed(double durationMs)
    {
        OverdueInvoiceRunsCommandFailed.Add(1);
        OverdueInvoiceRunDurationMs.Record(ToNonNegativeDouble(durationMs));
    }

    public static void RecordOverdueRunFaulted(double durationMs)
    {
        OverdueInvoiceRunsFaulted.Add(1);
        OverdueInvoiceRunDurationMs.Record(ToNonNegativeDouble(durationMs));
    }

    private static long ToNonNegativeLong(int value)
    {
        return value < 0 ? 0 : value;
    }

    private static double ToNonNegativeDouble(double value)
    {
        return value < 0 ? 0 : value;
    }
}
