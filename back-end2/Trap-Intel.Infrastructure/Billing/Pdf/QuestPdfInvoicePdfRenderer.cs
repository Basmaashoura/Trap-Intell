using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Trap_Intel.Application.Abstractions.Billing;

namespace Trap_Intel.Infrastructure.Billing.Pdf;

internal sealed class QuestPdfInvoicePdfRenderer : IInvoicePdfRenderer
{
    private static readonly string[] AccentPalette =
    [
        "#123C69",
        "#1E5F74",
        "#2E8A99",
        "#F4B942"
    ];

    static QuestPdfInvoicePdfRenderer()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<byte[]> RenderAsync(InvoicePdfPayload payload, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(text => text.FontSize(10).FontColor("#243447"));

                page.Header().Element(header => ComposeHeader(header, payload));
                page.Content().Element(content => ComposeContent(content, payload));
                page.Footer().Element(ComposeFooter);
            });
        });

        var bytes = document.GeneratePdf();
        return Task.FromResult(bytes);
    }

    private static void ComposeHeader(IContainer container, InvoicePdfPayload payload)
    {
        container.Column(column =>
        {
            column.Spacing(6);

            column.Item().Row(row =>
            {
                row.RelativeItem().Column(left =>
                {
                    left.Spacing(4);
                    left.Item().Text("TRAP-INTEL INVOICE")
                        .FontSize(21)
                        .SemiBold()
                        .FontColor(AccentPalette[0]);
                    left.Item().Text(payload.OrganizationName)
                        .FontSize(11)
                        .FontColor("#4E5D6C");
                });

                row.ConstantItem(210).AlignRight().Column(right =>
                {
                    right.Spacing(2);
                    right.Item().Text($"Invoice: {payload.InvoiceNumber}")
                        .FontSize(11)
                        .SemiBold()
                        .FontColor(AccentPalette[1]);
                    right.Item().Text($"Issue: {FormatDate(payload.IssueDate)}")
                        .FontColor("#4E5D6C");
                    right.Item().Text($"Due: {FormatDate(payload.DueDate)}")
                        .FontColor(payload.IsOverdue ? Colors.Red.Darken2 : "#4E5D6C");
                    right.Item().Text($"Status: {(payload.IsOverdue ? "Overdue" : "Active")}")
                        .FontColor(payload.IsOverdue ? Colors.Red.Darken2 : AccentPalette[2]);
                });
            });

            column.Item().PaddingTop(6).LineHorizontal(1).LineColor("#D9E2EC");
        });
    }

    private static void ComposeContent(IContainer container, InvoicePdfPayload payload)
    {
        container.Column(column =>
        {
            column.Spacing(16);

            column.Item().Element(c => ComposePeriodCard(c, payload));
            column.Item().Element(c => ComposeAmountTable(c, payload));
            column.Item().Element(c => ComposeUsageCard(c, payload));

            if (payload.Notes.Count > 0)
            {
                column.Item().Element(c => ComposeNotes(c, payload));
            }
        });
    }

    private static void ComposePeriodCard(IContainer container, InvoicePdfPayload payload)
    {
        container.Border(1)
            .BorderColor("#D9E2EC")
            .Background("#F8FAFC")
            .Padding(12)
            .Column(column =>
            {
                column.Spacing(8);
                column.Item().Text("Billing Period")
                    .SemiBold()
                    .FontColor(AccentPalette[1]);

                column.Item().Row(row =>
                {
                    row.RelativeItem().Text($"Start: {FormatDate(payload.BillingPeriodStart)}");
                    row.RelativeItem().Text($"End: {FormatDate(payload.BillingPeriodEnd)}");
                    row.RelativeItem().AlignRight().Text($"Tax Id: {payload.TaxId ?? "N/A"}");
                });
            });
    }

    private static void ComposeAmountTable(IContainer container, InvoicePdfPayload payload)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2);
                columns.RelativeColumn();
            });

            table.Header(header =>
            {
                header.Cell().Background(AccentPalette[0]).Padding(8).Text("Charge Breakdown")
                    .FontColor(Colors.White).SemiBold();
                header.Cell().Background(AccentPalette[0]).Padding(8).AlignRight().Text(payload.Currency)
                    .FontColor(Colors.White).SemiBold();
            });

            AddAmountRow(table, "Base Plan", payload.BaseAmount);
            AddAmountRow(table, "Usage Overage", payload.OverageAmount);
            AddAmountRow(table, "Tax", payload.TaxAmount, $"Rate {payload.TaxRate:P0}");
            AddAmountRow(table, "Discount", -payload.Discount);

            table.Cell().ColumnSpan(2).PaddingVertical(2).LineHorizontal(1).LineColor("#C5D2E0");

            table.Cell()
                .Background("#ECFDF3")
                .Padding(8)
                .Text("Total")
                .SemiBold()
                .FontColor("#0E7A45");

            table.Cell()
                .Background("#ECFDF3")
                .Padding(8)
                .AlignRight()
                .Text(FormatMoney(payload.TotalAmount, payload.Currency))
                .SemiBold()
                .FontColor("#0E7A45");
        });
    }

    private static void AddAmountRow(TableDescriptor table, string title, decimal amount, string? note = null)
    {
        table.Cell().BorderBottom(1).BorderColor("#E6EDF5").Padding(8).Column(column =>
        {
            column.Spacing(2);
            column.Item().Text(title);
            if (!string.IsNullOrWhiteSpace(note))
            {
                column.Item().Text(note).FontSize(8).FontColor("#6A7B8C");
            }
        });

        table.Cell().BorderBottom(1).BorderColor("#E6EDF5").Padding(8).AlignRight().Text(value =>
        {
            value.Span(amount < 0 ? "-" : string.Empty).FontColor(amount < 0 ? Colors.Red.Darken2 : "#243447");
            value.Span(FormatMoney(Math.Abs(amount), string.Empty)).FontColor(amount < 0 ? Colors.Red.Darken2 : "#243447");
        });
    }

    private static void ComposeUsageCard(IContainer container, InvoicePdfPayload payload)
    {
        container.Border(1)
            .BorderColor("#D9E2EC")
            .Padding(12)
            .Column(column =>
            {
                column.Spacing(8);
                column.Item().Text("Usage Snapshot")
                    .SemiBold()
                    .FontColor(AccentPalette[1]);

                column.Item().Row(row =>
                {
                    row.RelativeItem().Element(stat => ComposeMetric(stat, "Honeypots", payload.HoneypotsUsed.ToString(CultureInfo.InvariantCulture)));
                    row.RelativeItem().Element(stat => ComposeMetric(stat, "Storage", $"{payload.StorageUsedGb:0.##} GB"));
                    row.RelativeItem().Element(stat => ComposeMetric(stat, "Usage Charges", FormatMoney(payload.UsageOverageCharges, payload.Currency)));
                });
            });
    }

    private static void ComposeMetric(IContainer container, string label, string value)
    {
        container.Border(1)
            .BorderColor("#E6EDF5")
            .Padding(8)
            .Column(column =>
            {
                column.Spacing(3);
                column.Item().Text(label).FontSize(8).FontColor("#6A7B8C");
                column.Item().Text(value).SemiBold().FontColor(AccentPalette[0]);
            });
    }

    private static void ComposeNotes(IContainer container, InvoicePdfPayload payload)
    {
        container.Border(1)
            .BorderColor("#F4D7A1")
            .Background("#FFF9E8")
            .Padding(12)
            .Column(column =>
            {
                column.Spacing(6);
                column.Item().Text("Notes")
                    .SemiBold()
                    .FontColor("#8B5E00");

                foreach (var note in payload.Notes)
                {
                    column.Item().Text($"- {note}").FontSize(9).FontColor("#5E4A18");
                }
            });
    }

    private static void ComposeFooter(IContainer container)
    {
        container.PaddingTop(6).Column(column =>
        {
            column.Item().LineHorizontal(1).LineColor("#D9E2EC");
            column.Item().PaddingTop(5).AlignCenter().Text(text =>
            {
                text.Span("Generated by Trap-Intel Billing Engine").FontSize(8).FontColor("#6A7B8C");
            });
        });
    }

    private static string FormatDate(DateTime? value)
    {
        return value.HasValue
            ? value.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            : "N/A";
    }

    private static string FormatDate(DateTime value)
    {
        return value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private static string FormatMoney(decimal amount, string currency)
    {
        var value = amount.ToString("N2", CultureInfo.InvariantCulture);
        return string.IsNullOrWhiteSpace(currency) ? value : $"{value} {currency}";
    }
}
