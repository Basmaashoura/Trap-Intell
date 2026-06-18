using System;
using System.Collections.Generic;
using Trap_Intel.Domain.Shared;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Recommendations
{
    /// <summary>
    /// Represents an AI-generated recommendation for honeypot configuration or security enhancement.
    /// Manages the full lifecycle: generated ? accepted/rejected ? implemented/failed.
    /// This is where the AI integration point is - receives predictions from ML model.
    /// </summary>
    public class AIRecommendation : AggregateRoot<Guid>
    {
        private AIRecommendation() { }

        private AIRecommendation(
            Guid id,
            Guid organizationId,
            RecommendationType type,
            RecommendationTitle title,
            RecommendationDescription description,
            ConfidenceScore confidenceScore,
            ImpactScore impactScore,
            RecommendationPriority priority,
            RecommendationCategory category)
            : base(id)
        {
            OrganizationId = organizationId;
            Type = type;
            Title = title;
            Description = description;
            ConfidenceScore = confidenceScore;
            ImpactScore = impactScore;
            Priority = priority;
            Category = category;
            Status = RecommendationStatus.Pending;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        // Properties
        public Guid OrganizationId { get; private set; }
        public Guid? UserId { get; private set; }
        public Guid? DashboardViewId { get; private set; }
        public RecommendationType Type { get; private set; }
        public RecommendationTitle Title { get; private set; } = null!;
        public RecommendationDescription Description { get; private set; } = null!;
        public ConfidenceScore ConfidenceScore { get; private set; } = null!;
        public ImpactScore ImpactScore { get; private set; } = null!;
        public RecommendationPriority Priority { get; private set; }
        public RecommendationCategory Category { get; private set; }
        public RecommendationStatus Status { get; private set; }
        public RecommendationActions Actions { get; private set; } = null!;
        public DateTime? ExpiresAt { get; private set; }
        public string? TriggerEvent { get; private set; }
        public DateTime? AcceptedAt { get; private set; }
        public Guid? AcceptedBy { get; private set; }
        public string? AcceptanceNotes { get; private set; }
        public DateTime? RejectedAt { get; private set; }
        public Guid? RejectedBy { get; private set; }
        public string? RejectionReason { get; private set; }
        public DateTime? ImplementationStartedAt { get; private set; }
        public DateTime? ImplementationTargetDate { get; private set; }
        public DateTime? ImplementedAt { get; private set; }
        public Guid? ImplementedBy { get; private set; }
        public string? ImplementationNotes { get; private set; }
        public DateTime? FailedAt { get; private set; }
        public Guid? FailedBy { get; private set; }
        public string? FailureMessage { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        #region Factory Methods

        /// <summary>
        /// Factory method to create a new AI recommendation (from ML model output).
        /// This is the main integration point with the external AI service.
        /// </summary>
        public static Result<AIRecommendation> CreateFromAI(
            Guid organizationId,
            RecommendationType type,
            RecommendationTitle title,
            RecommendationDescription description,
            ConfidenceScore confidenceScore,
            ImpactScore impactScore,
            RecommendationPriority priority,
            RecommendationCategory category,
            RecommendationActions? actions = null,
            DateTime? expiresAt = null,
            string? triggerEvent = null,
            Guid? userId = null,
            Guid? dashboardViewId = null)
        {
            // Validation
            if (organizationId == Guid.Empty)
                return Result.Failure<AIRecommendation>(
                    Error.Custom("Recommendations.InvalidOrganization", "Organization ID cannot be empty."));

            if (title is null)
                return Result.Failure<AIRecommendation>(RecommendationErrors.InvalidRecommendationTitle);

            if (description is null)
                return Result.Failure<AIRecommendation>(RecommendationErrors.InvalidRecommendationDescription);

            if (confidenceScore is null)
                return Result.Failure<AIRecommendation>(RecommendationErrors.InvalidConfidenceScore);

            if (impactScore is null)
                return Result.Failure<AIRecommendation>(RecommendationErrors.InvalidImpactScore);

            var recommendation = new AIRecommendation(
                Guid.NewGuid(),
                organizationId,
                type,
                title,
                description,
                confidenceScore,
                impactScore,
                priority,
                category)
            {
                UserId = userId,
                DashboardViewId = dashboardViewId,
                Actions = actions ?? RecommendationActions.Empty(),
                ExpiresAt = expiresAt,
                TriggerEvent = triggerEvent
            };

            recommendation.RaiseDomainEvent(new RecommendationGeneratedEvent(
                recommendation.Id,
                organizationId,
                userId,
                dashboardViewId,
                type,
                priority,
                confidenceScore.Value,
                DateTime.UtcNow));

            return Result.Success(recommendation);
        }

        /// <summary>
        /// Factory method to reconstruct recommendation from database.
        /// </summary>
        public static AIRecommendation Reconstruct(
            Guid id,
            Guid organizationId,
            Guid? userId,
            Guid? dashboardViewId,
            RecommendationType type,
            RecommendationTitle title,
            RecommendationDescription description,
            ConfidenceScore confidenceScore,
            ImpactScore impactScore,
            RecommendationPriority priority,
            RecommendationCategory category,
            RecommendationStatus status,
            RecommendationActions actions,
            DateTime? expiresAt,
            string? triggerEvent,
            DateTime? acceptedAt,
            Guid? acceptedBy,
            string? acceptanceNotes,
            DateTime? rejectedAt,
            Guid? rejectedBy,
            string? rejectionReason,
            DateTime? implementationStartedAt,
            DateTime? implementationTargetDate,
            DateTime? implementedAt,
            Guid? implementedBy,
            string? implementationNotes,
            DateTime? failedAt,
            Guid? failedBy,
            string? failureMessage,
            DateTime createdAt,
            DateTime updatedAt)
        {
            var recommendation = new AIRecommendation(
                id,
                organizationId,
                type,
                title,
                description,
                confidenceScore,
                impactScore,
                priority,
                category)
            {
                UserId = userId,
                DashboardViewId = dashboardViewId,
                Status = status,
                Actions = actions,
                ExpiresAt = expiresAt,
                TriggerEvent = triggerEvent,
                AcceptedAt = acceptedAt,
                AcceptedBy = acceptedBy,
                AcceptanceNotes = acceptanceNotes,
                RejectedAt = rejectedAt,
                RejectedBy = rejectedBy,
                RejectionReason = rejectionReason,
                ImplementationStartedAt = implementationStartedAt,
                ImplementationTargetDate = implementationTargetDate,
                ImplementedAt = implementedAt,
                ImplementedBy = implementedBy,
                ImplementationNotes = implementationNotes,
                FailedAt = failedAt,
                FailedBy = failedBy,
                FailureMessage = failureMessage,
                CreatedAt = createdAt,
                UpdatedAt = updatedAt
            };

            return recommendation;
        }

        #endregion

        #region Domain Operations

        /// <summary>
        /// User accepts this recommendation (agrees to implement).
        /// </summary>
        public Result Accept(Guid userId, string? notes = null)
        {
            if (userId == Guid.Empty)
                return Result.Failure(
                    Error.Custom("Recommendations.InvalidUserId", "User ID cannot be empty."));

            if (Status != RecommendationStatus.Pending && Status != RecommendationStatus.Reviewed)
                return Result.Failure(
                    Error.Custom("Recommendations.CannotAccept", 
                        "Only pending or reviewed recommendations can be accepted."));

            if (Status == RecommendationStatus.Rejected)
                return Result.Failure(RecommendationErrors.CannotAcceptRejected);

            Status = RecommendationStatus.Accepted;
            AcceptedAt = DateTime.UtcNow;
            AcceptedBy = userId;
            AcceptanceNotes = notes;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new RecommendationAcceptedEvent(
                Id,
                OrganizationId,
                userId,
                notes,
                DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>
        /// User rejects this recommendation (won't implement).
        /// </summary>
        public Result Reject(Guid userId, string reason)
        {
            if (userId == Guid.Empty)
                return Result.Failure(
                    Error.Custom("Recommendations.InvalidUserId", "User ID cannot be empty."));

            if (string.IsNullOrWhiteSpace(reason))
                return Result.Failure(
                    Error.Custom("Recommendations.InvalidReason", "Rejection reason is required."));

            if (Status == RecommendationStatus.Accepted)
                return Result.Failure(RecommendationErrors.CannotRejectAccepted);

            if (Status == RecommendationStatus.Implemented)
                return Result.Failure(
                    Error.Custom("Recommendations.CannotRejectImplemented", 
                        "Cannot reject an already implemented recommendation."));

            Status = RecommendationStatus.Rejected;
            RejectedAt = DateTime.UtcNow;
            RejectedBy = userId;
            RejectionReason = reason.Trim();
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new RecommendationRejectedEvent(
                Id,
                OrganizationId,
                userId,
                reason,
                DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>
        /// Start implementation of this recommendation.
        /// </summary>
        public Result StartImplementation(Guid userId, DateTime? targetDate = null)
        {
            if (userId == Guid.Empty)
                return Result.Failure(
                    Error.Custom("Recommendations.InvalidUserId", "User ID cannot be empty."));

            if (Status != RecommendationStatus.Accepted)
                return Result.Failure(
                    Error.Custom("Recommendations.CannotStartImplementation", 
                        "Only accepted recommendations can be implemented."));

            if (IsExpired)
                return Result.Failure(RecommendationErrors.CannotImplementExpired);

            Status = RecommendationStatus.InProgress;
            ImplementationStartedAt = DateTime.UtcNow;
            ImplementationTargetDate = targetDate;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new RecommendationImplementationStartedEvent(
                Id,
                OrganizationId,
                userId,
                targetDate ?? DateTime.UtcNow.AddDays(7),
                DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>
        /// Mark recommendation as successfully implemented.
        /// </summary>
        public Result MarkAsImplemented(Guid userId, string? notes = null)
        {
            if (userId == Guid.Empty)
                return Result.Failure(
                    Error.Custom("Recommendations.InvalidUserId", "User ID cannot be empty."));

            if (Status != RecommendationStatus.InProgress && Status != RecommendationStatus.Accepted)
                return Result.Failure(
                    Error.Custom("Recommendations.CannotMarkImplemented", 
                        "Recommendation must be in progress or accepted."));

            Status = RecommendationStatus.Implemented;
            ImplementedAt = DateTime.UtcNow;
            ImplementedBy = userId;
            ImplementationNotes = notes;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new RecommendationImplementedEvent(
                Id,
                OrganizationId,
                userId,
                notes,
                DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>
        /// Mark implementation as failed.
        /// </summary>
        public Result MarkImplementationFailed(Guid userId, string errorMessage)
        {
            if (userId == Guid.Empty)
                return Result.Failure(
                    Error.Custom("Recommendations.InvalidUserId", "User ID cannot be empty."));

            if (string.IsNullOrWhiteSpace(errorMessage))
                return Result.Failure(
                    Error.Custom("Recommendations.InvalidErrorMessage", "Error message is required."));

            if (Status != RecommendationStatus.InProgress)
                return Result.Failure(
                    Error.Custom("Recommendations.NotInProgress", 
                        "Only in-progress recommendations can fail."));

            Status = RecommendationStatus.Failed;
            FailedAt = DateTime.UtcNow;
            FailedBy = userId;
            FailureMessage = errorMessage.Trim();
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new RecommendationImplementationFailedEvent(
                Id,
                OrganizationId,
                userId,
                errorMessage,
                DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>
        /// Mark recommendation as expired (no longer relevant).
        /// </summary>
        public Result Expire()
        {
            if (Status != RecommendationStatus.Pending)
                return Result.Failure(RecommendationErrors.CannotExpireNonPending);

            Status = RecommendationStatus.Expired;
            ExpiresAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new RecommendationExpiredEvent(
                Id,
                OrganizationId,
                DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>
        /// Check if this recommendation has expired.
        /// </summary>
        public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;

        /// <summary>
        /// Check if this recommendation requires urgent action (high priority and high confidence).
        /// </summary>
        public bool IsUrgent =>
            Priority == RecommendationPriority.Critical &&
            ConfidenceScore.IsHighConfidence;

        /// <summary>
        /// Check if this recommendation is still actionable.
        /// </summary>
        public bool IsActionable =>
            Status == RecommendationStatus.Pending ||
            Status == RecommendationStatus.Reviewed ||
            Status == RecommendationStatus.Accepted;

        #endregion

        #region Effectiveness Tracking

        /// <summary>
        /// Record the effectiveness of this recommendation after implementation.
        /// Used to improve AI model accuracy.
        /// </summary>
        public Result RecordEffectiveness(
            bool wasEffective,
            string feedback,
            Guid recordedByUserId)
        {
            if (Status != RecommendationStatus.Implemented)
                return Result.Failure(
                    Error.Custom("Recommendation.NotImplemented", 
                        "Effectiveness can only be recorded for implemented recommendations."));
                
            if (string.IsNullOrWhiteSpace(feedback))
                return Result.Failure(
                    Error.Custom("Recommendation.InvalidFeedback", 
                        "Effectiveness feedback cannot be empty."));
                
            if (recordedByUserId == Guid.Empty)
                return Result.Failure(
                    Error.Custom("Recommendation.InvalidUserId", 
                        "User ID cannot be empty."));
                
            UpdatedAt = DateTime.UtcNow;
            
            RaiseDomainEvent(new RecommendationEffectivenessRecordedEvent(
                Id,
                OrganizationId,
                wasEffective,
                feedback,
                recordedByUserId,
                DateTime.UtcNow));
                
            return Result.Success();
        }

        /// <summary>
        /// Auto-expire stale recommendations.
        /// </summary>
        public Result AutoExpire()
        {
            if (Status != RecommendationStatus.Pending)
                return Result.Failure(
                    Error.Custom("Recommendation.CannotAutoExpire", 
                        "Only pending recommendations can auto-expire."));
                
            if (!IsExpired)
                return Result.Failure(
                    Error.Custom("Recommendation.NotExpiredYet", 
                        "Recommendation has not expired yet."));
                
            // Call existing Expire method
            return Expire();
        }

        #endregion
    }
}
