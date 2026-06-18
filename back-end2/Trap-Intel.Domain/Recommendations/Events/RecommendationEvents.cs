using System;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Recommendations
{
    /// <summary>
    /// Domain events for the Recommendations domain.
    /// </summary>

    /// <summary>
    /// Raised when AI generates a new recommendation.
    /// </summary>
    public record RecommendationGeneratedEvent(
        Guid RecommendationId,
        Guid OrganizationId,
        Guid? UserId,
        Guid? DashboardViewId,
        RecommendationType Type,
        RecommendationPriority Priority,
        decimal ConfidenceScore,
        DateTime OccurredOn) : IDomainEvent;

    /// <summary>
    /// Raised when a recommendation is accepted by user.
    /// </summary>
    public record RecommendationAcceptedEvent(
        Guid RecommendationId,
        Guid OrganizationId,
        Guid UserId,
        string? Notes,
        DateTime OccurredOn) : IDomainEvent;

    /// <summary>
    /// Raised when a recommendation is rejected by user.
    /// </summary>
    public record RecommendationRejectedEvent(
        Guid RecommendationId,
        Guid OrganizationId,
        Guid UserId,
        string Reason,
        DateTime OccurredOn) : IDomainEvent;

    /// <summary>
    /// Raised when implementation of a recommendation begins.
    /// </summary>
    public record RecommendationImplementationStartedEvent(
        Guid RecommendationId,
        Guid OrganizationId,
        Guid UserId,
        DateTime TargetDate,
        DateTime OccurredOn) : IDomainEvent;

    /// <summary>
    /// Raised when a recommendation is implemented successfully.
    /// </summary>
    public record RecommendationImplementedEvent(
        Guid RecommendationId,
        Guid OrganizationId,
        Guid UserId,
        string? Notes,
        DateTime OccurredOn) : IDomainEvent;

    /// <summary>
    /// Raised when implementation fails.
    /// </summary>
    public record RecommendationImplementationFailedEvent(
        Guid RecommendationId,
        Guid OrganizationId,
        Guid UserId,
        string ErrorMessage,
        DateTime OccurredOn) : IDomainEvent;

    /// <summary>
    /// Raised when a recommendation expires.
    /// </summary>
    public record RecommendationExpiredEvent(
        Guid RecommendationId,
        Guid OrganizationId,
        DateTime OccurredOn) : IDomainEvent;

    /// <summary>
    /// Event raised when recommendation effectiveness is recorded.
    /// </summary>
    public record RecommendationEffectivenessRecordedEvent(
        Guid RecommendationId,
        Guid OrganizationId,
        bool WasEffective,
        string Feedback,
        Guid RecordedByUserId,
        DateTime OccurredOn) : IDomainEvent;
}
