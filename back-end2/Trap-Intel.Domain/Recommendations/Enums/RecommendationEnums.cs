namespace Trap_Intel.Domain.Recommendations
{
    /// <summary>
    /// Enums for the Recommendations domain.
    /// </summary>

    /// <summary>
    /// Types of recommendations the AI can generate.
    /// </summary>
    public enum RecommendationType
    {
        SecurityEnhancement = 0,
        ThreatMitigation = 1,
        ConfigOptimization = 2,
        ComplianceAction = 3,
        PerformanceOptimization = 4,
        CostOptimization = 5,
        DataProtection = 6
    }

    /// <summary>
    /// Priority levels for recommendations.
    /// </summary>
    public enum RecommendationPriority
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Critical = 3
    }

    /// <summary>
    /// Status of a recommendation through its lifecycle.
    /// </summary>
    public enum RecommendationStatus
    {
        Pending = 0,           // Just generated
        Reviewed = 1,          // Reviewed by user
        Accepted = 2,          // User accepted
        Rejected = 3,          // User rejected
        Implemented = 4,       // User implemented
        InProgress = 5,        // Implementation in progress
        Failed = 6,            // Implementation failed
        Expired = 7            // Recommendation is no longer relevant
    }

    /// <summary>
    /// Category of recommendation (preventive vs reactive).
    /// </summary>
    public enum RecommendationCategory
    {
        Preventive = 0,        // Prevent future threats
        Reactive = 1,          // Response to detected threat
        Compliance = 2,        // Meet regulatory requirements
        Operational = 3,       // Improve operations
        Strategic = 4          // Long-term improvements
    }
}
