namespace Trap_Intel.Domain.Recommendations
{
    /// <summary>
    /// Repository interface for AIRecommendation aggregate root.
    /// Abstracts data access for recommendations.
    /// </summary>
    public interface IAIRecommendationRepository
    {
        /// <summary>
        /// Get recommendation by ID.
        /// </summary>
        Task<AIRecommendation?> GetByIdAsync(Guid recommendationId);

        /// <summary>
        /// Get all pending recommendations for an organization.
        /// </summary>
        Task<IReadOnlyList<AIRecommendation>> GetPendingAsync(
            Guid organizationId,
            int pageNumber = 1,
            int pageSize = 50);

        /// <summary>
        /// Get urgent recommendations (critical priority, high confidence).
        /// </summary>
        Task<IReadOnlyList<AIRecommendation>> GetUrgentAsync(
            Guid organizationId,
            int pageNumber = 1,
            int pageSize = 50);

        /// <summary>
        /// Get recommendations by type.
        /// </summary>
        Task<IReadOnlyList<AIRecommendation>> GetByTypeAsync(
            Guid organizationId,
            RecommendationType type,
            int pageNumber = 1,
            int pageSize = 50);

        /// <summary>
        /// Get recommendations by priority.
        /// </summary>
        Task<IReadOnlyList<AIRecommendation>> GetByPriorityAsync(
            Guid organizationId,
            RecommendationPriority priority,
            int pageNumber = 1,
            int pageSize = 50);

        /// <summary>
        /// Get recommendations by status.
        /// </summary>
        Task<IReadOnlyList<AIRecommendation>> GetByStatusAsync(
            Guid organizationId,
            RecommendationStatus status,
            int pageNumber = 1,
            int pageSize = 50);

        /// <summary>
        /// Get recommendations for a specific user.
        /// </summary>
        Task<IReadOnlyList<AIRecommendation>> GetByUserAsync(
            Guid organizationId,
            Guid userId,
            int pageNumber = 1,
            int pageSize = 50);

        /// <summary>
        /// Get recommendations for a dashboard view.
        /// </summary>
        Task<IReadOnlyList<AIRecommendation>> GetByDashboardViewAsync(
            Guid dashboardViewId,
            int pageNumber = 1,
            int pageSize = 50);

        /// <summary>
        /// Get recommendations accepted within a date range.
        /// </summary>
        Task<IReadOnlyList<AIRecommendation>> GetAcceptedAsync(
            Guid organizationId,
            DateTime startDate,
            DateTime endDate,
            int pageNumber = 1,
            int pageSize = 50);

        /// <summary>
        /// Get recommendations implemented within a date range.
        /// </summary>
        Task<IReadOnlyList<AIRecommendation>> GetImplementedAsync(
            Guid organizationId,
            DateTime startDate,
            DateTime endDate,
            int pageNumber = 1,
            int pageSize = 50);

        /// <summary>
        /// Get recommendations by category.
        /// </summary>
        Task<IReadOnlyList<AIRecommendation>> GetByCategoryAsync(
            Guid organizationId,
            RecommendationCategory category,
            int pageNumber = 1,
            int pageSize = 50);

        /// <summary>
        /// Get recently expired recommendations (that should be cleaned up).
        /// </summary>
        Task<IReadOnlyList<AIRecommendation>> GetExpiredAsync(
            Guid organizationId,
            int pageNumber = 1,
            int pageSize = 50);

        /// <summary>
        /// Get high-confidence recommendations.
        /// </summary>
        Task<IReadOnlyList<AIRecommendation>> GetHighConfidenceAsync(
            Guid organizationId,
            decimal minConfidence = 80,
            int pageNumber = 1,
            int pageSize = 50);

        /// <summary>
        /// Count pending recommendations.
        /// </summary>
        Task<int> CountPendingAsync(Guid organizationId);

        /// <summary>
        /// Count accepted recommendations.
        /// </summary>
        Task<int> CountAcceptedAsync(Guid organizationId);

        /// <summary>
        /// Count implemented recommendations.
        /// </summary>
        Task<int> CountImplementedAsync(Guid organizationId);

        /// <summary>
        /// Get statistics about recommendations for an organization.
        /// </summary>
        Task<RecommendationStatistics> GetStatisticsAsync(Guid organizationId);

        /// <summary>
        /// Add new recommendation.
        /// </summary>
        Task AddAsync(AIRecommendation recommendation);

        /// <summary>
        /// Update existing recommendation.
        /// </summary>
        Task UpdateAsync(AIRecommendation recommendation);

        /// <summary>
        /// Add multiple recommendations in batch.
        /// </summary>
        Task AddBatchAsync(IEnumerable<AIRecommendation> recommendations);

        /// <summary>
        /// Delete recommendation.
        /// </summary>
        Task DeleteAsync(Guid recommendationId);
    }

    /// <summary>
    /// Statistics about recommendations for dashboard/reporting.
    /// </summary>
    public record RecommendationStatistics
    {
        public int TotalRecommendations { get; init; }
        public int PendingCount { get; init; }
        public int AcceptedCount { get; init; }
        public int ImplementedCount { get; init; }
        public int RejectedCount { get; init; }
        public int FailedCount { get; init; }
        public int ExpiredCount { get; init; }
        public decimal AverageConfidenceScore { get; init; }
        public decimal AverageImpactScore { get; init; }
        public int CriticalPriorityCount { get; init; }
        public decimal ImplementationRate { get; init; } // Implemented / (Implemented + Rejected)
    }
}
