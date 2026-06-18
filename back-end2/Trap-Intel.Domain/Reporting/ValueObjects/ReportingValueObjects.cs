using System;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Reporting
{
    /// <summary>
    /// KPI (Key Performance Indicator) value object.
    /// Represents a typed, validated metric with trend analysis.
    /// </summary>
    public record KPI
    {
        public string Name { get; }
        public decimal Value { get; }
        public string Unit { get; }
        public decimal? Threshold { get; }
        public KPITrend Trend { get; }

        private KPI(string name, decimal value, string unit,
            decimal? threshold, KPITrend trend)
        {
            Name = name;
            Value = value;
            Unit = unit;
            Threshold = threshold;
            Trend = trend;
        }

        /// <summary>Factory method to create a validated KPI.</summary>
        public static Result<KPI> Create(string name, decimal value,
            string unit, decimal? threshold = null,
            KPITrend trend = KPITrend.Stable)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Result.Failure<KPI>(ReportingErrors.InvalidKPIName);

            if (string.IsNullOrWhiteSpace(unit))
                return Result.Failure<KPI>(ReportingErrors.InvalidKPIUnit);

            if (threshold.HasValue && threshold.Value < 0)
                return Result.Failure<KPI>(ReportingErrors.InvalidKPIThreshold);

            return Result.Success(new KPI(name.Trim(), value, unit, threshold, trend));
        }

        /// <summary>Check if KPI has exceeded its threshold.</summary>
        public bool IsExceededThreshold =>
            Threshold.HasValue && Value > Threshold.Value;
    }

    /// <summary>KPI trend direction.</summary>
    public enum KPITrend
    {
        Improving = 0,
        Stable = 1,
        Declining = 2
    }

    /// <summary>Collection of KPIs with aggregate statistics.</summary>
    public record KPICollection
    {
        public IReadOnlyList<KPI> Items { get; }

        private KPICollection(IReadOnlyList<KPI> items)
        {
            Items = items ?? new List<KPI>();
        }

        /// <summary>Factory method to create a validated KPI collection.</summary>
        public static Result<KPICollection> Create(IEnumerable<KPI> kpis)
        {
            if (kpis == null || !kpis.Any())
                return Result.Failure<KPICollection>(ReportingErrors.EmptyKPICollection);

            return Result.Success(new KPICollection(kpis.ToList()));
        }

        public int Count => Items.Count;
        public int ExceededCount => Items.Count(k => k.IsExceededThreshold);
        public decimal AverageValue => Items.Count > 0 ? Items.Average(k => k.Value) : 0;
    }

    /// <summary>Log analysis details value object.</summary>
    public record LogDetails
    {
        public int TotalLogsAnalyzed { get; init; }
        public int CriticalEvents { get; init; }
        public int WarningEvents { get; init; }
        public int InfoEvents { get; init; }
        public TimeSpan AnalysisDuration { get; init; }
        public DateTime AnalysisStartTime { get; init; }
        public DateTime AnalysisEndTime { get; init; }

        // Private parameterless constructor for EF Core
        private LogDetails() { }

        private LogDetails(int totalLogsAnalyzed, int criticalEvents, int warningEvents, int infoEvents,
            DateTime analysisStartTime, DateTime analysisEndTime)
        {
            TotalLogsAnalyzed = totalLogsAnalyzed;
            CriticalEvents = criticalEvents;
            WarningEvents = warningEvents;
            InfoEvents = infoEvents;
            AnalysisStartTime = analysisStartTime;
            AnalysisEndTime = analysisEndTime;
            AnalysisDuration = analysisEndTime - analysisStartTime;
        }

        /// <summary>Factory method to create validated log details.</summary>
        public static Result<LogDetails> Create(int total, int critical,
            int warning, int info, DateTime start, DateTime end)
        {
            if (total < 0)
                return Result.Failure<LogDetails>(ReportingErrors.InvalidLogCount);

            if (critical < 0 || warning < 0 || info < 0)
                return Result.Failure<LogDetails>(ReportingErrors.InvalidEventCount);

            if (critical + warning + info > total)
                return Result.Failure<LogDetails>(ReportingErrors.LogCountMismatch);

            if (start >= end)
                return Result.Failure<LogDetails>(ReportingErrors.InvalidDateRange);

            return Result.Success(new LogDetails(total, critical, warning, info, start, end));
        }

        public decimal CriticalityScore =>
            TotalLogsAnalyzed > 0 ? (CriticalEvents * 100m) / TotalLogsAnalyzed : 0;

        public decimal WarningPercentage =>
            TotalLogsAnalyzed > 0 ? (WarningEvents * 100m) / TotalLogsAnalyzed : 0;

        public decimal InfoPercentage =>
            TotalLogsAnalyzed > 0 ? (InfoEvents * 100m) / TotalLogsAnalyzed : 0;
    }

    /// <summary>Recommendation value object.</summary>
    public record Recommendation
    {
        public string Title { get; }
        public string Description { get; }
        public RecommendationPriority Priority { get; }
        public string ActionItems { get; }
        public DateTime SuggestedImplementationDate { get; }

        private Recommendation(string title, string description,
            RecommendationPriority priority, string actionItems,
            DateTime implementationDate)
        {
            Title = title;
            Description = description;
            Priority = priority;
            ActionItems = actionItems;
            SuggestedImplementationDate = implementationDate;
        }

        /// <summary>Factory method to create validated recommendation.</summary>
        public static Result<Recommendation> Create(string title,
            string description, RecommendationPriority priority,
            string actionItems, DateTime implementationDate)
        {
            if (string.IsNullOrWhiteSpace(title))
                return Result.Failure<Recommendation>(
                    ReportingErrors.InvalidRecommendationTitle);

            if (string.IsNullOrWhiteSpace(description))
                return Result.Failure<Recommendation>(
                    ReportingErrors.InvalidRecommendationDescription);

            if (implementationDate < DateTime.UtcNow)
                return Result.Failure<Recommendation>(
                    ReportingErrors.InvalidImplementationDate);

            return Result.Success(new Recommendation(
                title.Trim(), description.Trim(), priority,
                actionItems.Trim(), implementationDate));
        }
    }

    /// <summary>Recommendation priority levels.</summary>
    public enum RecommendationPriority
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Critical = 3
    }

    /// <summary>Collection of recommendations.</summary>
    public record RecommendationCollection
    {
        public IReadOnlyList<Recommendation> Items { get; }

        private RecommendationCollection(IReadOnlyList<Recommendation> items)
        {
            Items = items ?? new List<Recommendation>();
        }

        public static Result<RecommendationCollection> Create(IEnumerable<Recommendation> recommendations)
        {
            if (recommendations == null)
                return Result.Failure<RecommendationCollection>(
                    ReportingErrors.InvalidRecommendationCollection);

            return Result.Success(new RecommendationCollection(recommendations.ToList()));
        }

        public int CriticalCount =>
            Items.Count(r => r.Priority == RecommendationPriority.Critical);

        public int HighCount =>
            Items.Count(r => r.Priority == RecommendationPriority.High);
    }

    /// <summary>Report title value object.</summary>
    public record ReportTitle
    {
        public string Value { get; }

        private ReportTitle(string value) => Value = value;

        public static Result<ReportTitle> Create(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return Result.Failure<ReportTitle>(ReportingErrors.InvalidReportTitle);

            var trimmed = title.Trim();
            if (trimmed.Length < 5)
                return Result.Failure<ReportTitle>(ReportingErrors.ReportTitleTooShort);

            if (trimmed.Length > 200)
                return Result.Failure<ReportTitle>(ReportingErrors.ReportTitleTooLong);

            return Result.Success(new ReportTitle(trimmed));
        }
    }

    /// <summary>Report summary value object.</summary>
    public record ReportSummary
    {
        public string Value { get; }

        private ReportSummary(string value) => Value = value;

        public static Result<ReportSummary> Create(string summary)
        {
            if (string.IsNullOrWhiteSpace(summary))
                return Result.Failure<ReportSummary>(ReportingErrors.InvalidReportSummary);

            var trimmed = summary.Trim();
            if (trimmed.Length < 10)
                return Result.Failure<ReportSummary>(ReportingErrors.ReportSummaryTooShort);

            if (trimmed.Length > 2000)
                return Result.Failure<ReportSummary>(ReportingErrors.ReportSummaryTooLong);

            return Result.Success(new ReportSummary(trimmed));
        }
    }

    /// <summary>Template name value object.</summary>
    public record TemplateName
    {
        public string Value { get; }

        private TemplateName(string value) => Value = value;

        public static Result<TemplateName> Create(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Result.Failure<TemplateName>(ReportingErrors.InvalidTemplateName);

            return Result.Success(new TemplateName(name.Trim()));
        }
    }

    /// <summary>Template guidelines value object.</summary>
    public record TemplateGuidelines
    {
        public string Value { get; }

        private TemplateGuidelines(string value) => Value = value;

        public static Result<TemplateGuidelines> Create(string guidelines)
        {
            if (string.IsNullOrWhiteSpace(guidelines))
                return Result.Failure<TemplateGuidelines>(ReportingErrors.InvalidGuidelines);

            return Result.Success(new TemplateGuidelines(guidelines.Trim()));
        }
    }
}
