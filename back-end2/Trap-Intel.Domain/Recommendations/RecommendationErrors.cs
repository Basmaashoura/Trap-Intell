using System;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Recommendations
{
    /// <summary>
    /// Error definitions for the Recommendations domain.
    /// </summary>
    public static class RecommendationErrors
    {
        public static readonly Error RecommendationNotFound = Error.Custom(
            "Recommendations.RecommendationNotFound",
            "The specified recommendation does not exist.");

        public static readonly Error InvalidRecommendationTitle = Error.Custom(
            "Recommendations.InvalidRecommendationTitle",
            "The recommendation title is invalid.");

        public static readonly Error InvalidRecommendationDescription = Error.Custom(
            "Recommendations.InvalidRecommendationDescription",
            "The recommendation description is invalid.");

        public static readonly Error InvalidConfidenceScore = Error.Custom(
            "Recommendations.InvalidConfidenceScore",
            "The confidence score must be between 0 and 100.");

        public static readonly Error InvalidImpactScore = Error.Custom(
            "Recommendations.InvalidImpactScore",
            "The impact score must be between 0 and 100.");

        public static readonly Error CannotAcceptRejected = Error.Custom(
            "Recommendations.CannotAcceptRejected",
            "Cannot accept a rejected recommendation.");

        public static readonly Error CannotRejectAccepted = Error.Custom(
            "Recommendations.CannotRejectAccepted",
            "Cannot reject an accepted recommendation.");

        public static readonly Error CannotImplementExpired = Error.Custom(
            "Recommendations.CannotImplementExpired",
            "Cannot implement an expired recommendation.");

        public static readonly Error AlreadyImplemented = Error.Custom(
            "Recommendations.AlreadyImplemented",
            "This recommendation is already implemented.");

        public static readonly Error InvalidAction = Error.Custom(
            "Recommendations.InvalidAction",
            "The action data is invalid.");

        public static readonly Error CannotExpireNonPending = Error.Custom(
            "Recommendations.CannotExpireNonPending",
            "Only pending recommendations can be marked as expired.");

        public static Error RecommendationNotFound_Detail(string recommendationId)
        {
            return Error.Custom("Recommendations.RecommendationNotFound",
                $"Recommendation '{recommendationId}' not found.");
        }
    }
}
