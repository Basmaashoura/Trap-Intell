using System;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Recommendations
{
    /// <summary>
    /// Value objects for the Recommendations domain.
    /// </summary>

    /// <summary>
    /// Represents a recommendation title.
    /// </summary>
    public record RecommendationTitle
    {
        public string Value { get; }

        private RecommendationTitle(string value) => Value = value;

        public static Result<RecommendationTitle> Create(string? title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return Result.Failure<RecommendationTitle>(RecommendationErrors.InvalidRecommendationTitle);

            var trimmed = title.Trim();

            if (trimmed.Length < 5 || trimmed.Length > 200)
                return Result.Failure<RecommendationTitle>(RecommendationErrors.InvalidRecommendationTitle);

            return Result.Success(new RecommendationTitle(trimmed));
        }

        public override string ToString() => Value;
    }

    /// <summary>
    /// Represents a detailed description of a recommendation.
    /// </summary>
    public record RecommendationDescription
    {
        public string Value { get; }

        private RecommendationDescription(string value) => Value = value;

        public static Result<RecommendationDescription> Create(string? description)
        {
            if (string.IsNullOrWhiteSpace(description))
                return Result.Failure<RecommendationDescription>(
                    RecommendationErrors.InvalidRecommendationDescription);

            var trimmed = description.Trim();

            if (trimmed.Length < 10 || trimmed.Length > 2000)
                return Result.Failure<RecommendationDescription>(
                    RecommendationErrors.InvalidRecommendationDescription);

            return Result.Success(new RecommendationDescription(trimmed));
        }

        public override string ToString() => Value;
    }

    /// <summary>
    /// Represents a confidence score (0-100) from AI model.
    /// Indicates how confident the AI is in this recommendation.
    /// </summary>
    public record ConfidenceScore
    {
        public decimal Value { get; }

        private ConfidenceScore(decimal value) => Value = value;

        public static Result<ConfidenceScore> Create(decimal score)
        {
            if (score < 0 || score > 100)
                return Result.Failure<ConfidenceScore>(RecommendationErrors.InvalidConfidenceScore);

            return Result.Success(new ConfidenceScore(score));
        }

        public bool IsHighConfidence => Value >= 80;
        public bool IsMediumConfidence => Value >= 50 && Value < 80;
        public bool IsLowConfidence => Value < 50;

        public override string ToString() => $"{Value:F2}%";
    }

    /// <summary>
    /// Represents an impact score (0-100) - how much this will improve security.
    /// </summary>
    public record ImpactScore
    {
        public decimal Value { get; }

        private ImpactScore(decimal value) => Value = value;

        public static Result<ImpactScore> Create(decimal score)
        {
            if (score < 0 || score > 100)
                return Result.Failure<ImpactScore>(RecommendationErrors.InvalidImpactScore);

            return Result.Success(new ImpactScore(score));
        }

        public bool IsHighImpact => Value >= 80;
        public bool IsMediumImpact => Value >= 50 && Value < 80;
        public bool IsLowImpact => Value < 50;

        public override string ToString() => $"{Value:F2}";
    }

    /// <summary>
    /// Represents actionable implementation steps.
    /// </summary>
    public record ActionStep
    {
        public int Order { get; }
        public string Title { get; }
        public string Description { get; }
        public string? Command { get; }
        public string? LinkToDocumentation { get; }

        private ActionStep(int order, string title, string description, string? command, string? link)
        {
            Order = order;
            Title = title;
            Description = description;
            Command = command;
            LinkToDocumentation = link;
        }

        public static Result<ActionStep> Create(
            int order,
            string title,
            string description,
            string? command = null,
            string? link = null)
        {
            if (order < 1)
                return Result.Failure<ActionStep>(
                    Error.Custom("Recommendations.InvalidActionOrder", "Action order must be >= 1."));

            if (string.IsNullOrWhiteSpace(title))
                return Result.Failure<ActionStep>(
                    Error.Custom("Recommendations.InvalidActionTitle", "Action title is required."));

            if (string.IsNullOrWhiteSpace(description))
                return Result.Failure<ActionStep>(
                    Error.Custom("Recommendations.InvalidActionDescription", "Action description is required."));

            return Result.Success(new ActionStep(order, title.Trim(), description.Trim(), command, link));
        }
    }

    /// <summary>
    /// Represents a collection of implementation actions.
    /// </summary>
    public record RecommendationActions
    {
        public IReadOnlyList<ActionStep> Steps { get; }

        private RecommendationActions(List<ActionStep> steps) => Steps = steps.AsReadOnly();

        public static Result<RecommendationActions> Create(List<ActionStep>? steps)
        {
            if (steps == null || steps.Count == 0)
                return Result.Success(new RecommendationActions(new List<ActionStep>()));

            var orderedSteps = steps.OrderBy(s => s.Order).ToList();
            return Result.Success(new RecommendationActions(orderedSteps));
        }

        public static RecommendationActions Empty() => new(new List<ActionStep>());
    }
}
