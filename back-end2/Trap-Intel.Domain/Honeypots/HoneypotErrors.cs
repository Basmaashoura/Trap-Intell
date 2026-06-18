using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Honeypots
{
    /// <summary>
    /// Error codes and factory methods for the Honeypots domain.
    /// Follows DDD error handling patterns with semantic error codes.
    /// </summary>
    public static class HoneypotErrors
    {
        // Not found errors
        public static readonly Error NotFound = Error.Custom(
            "Honeypot.NotFound",
            "The requested honeypot does not exist.");

        public static readonly Error NotFoundByExternalId = Error.Custom(
            "Honeypot.NotFoundByExternalId",
            "Honeypot with the specified external service ID was not found.");

        // Validation errors - Basic properties
        public static readonly Error InvalidName = Error.Custom(
            "Honeypot.InvalidName",
            "Honeypot name cannot be empty and must be between 3 and 255 characters.");

        public static readonly Error InvalidType = Error.Custom(
            "Honeypot.InvalidType",
            "Invalid honeypot type specified.");

        public static readonly Error InvalidPort = Error.Custom(
            "Honeypot.InvalidPort",
            "Port must be between 1 and 65535.");

        public static readonly Error InvalidConfiguration = Error.Custom(
            "Honeypot.InvalidConfiguration",
            "Honeypot configuration is invalid.");

        // Quota errors
        public static readonly Error QuotaExceeded = Error.Custom(
            "Honeypot.QuotaExceeded",
            "Honeypot deployment would exceed subscription quota.");

        public static readonly Error StorageQuotaExceeded = Error.Custom(
            "Honeypot.StorageQuotaExceeded",
            "Storage quota has been exceeded for this honeypot.");

        public static readonly Error MaxHoneypotsReached = Error.Custom(
            "Honeypot.MaxHoneypotsReached",
            "Maximum number of honeypots allowed for this subscription has been reached.");

        // Deployment errors
        public static readonly Error DeploymentFailed = Error.Custom(
            "Honeypot.DeploymentFailed",
            "Failed to deploy honeypot to external service.");

        public static readonly Error DeploymentInProgress = Error.Custom(
            "Honeypot.DeploymentInProgress",
            "Honeypot deployment is already in progress.");

        public static readonly Error CannotDeployInactiveSubscription = Error.Custom(
            "Honeypot.CannotDeployInactiveSubscription",
            "Cannot deploy honeypot for inactive subscription.");

        public static readonly Error ExternalServiceUnavailable = Error.Custom(
            "Honeypot.ExternalServiceUnavailable",
            "External honeypot service is currently unavailable.");

        public static readonly Error ExternalServiceError = Error.Custom(
            "Honeypot.ExternalServiceError",
            "External honeypot service returned an error.");

        // State transition errors
        public static readonly Error AlreadyActive = Error.Custom(
            "Honeypot.AlreadyActive",
            "Honeypot is already in active state.");

        public static readonly Error AlreadyPaused = Error.Custom(
            "Honeypot.AlreadyPaused",
            "Honeypot is already paused.");

        public static readonly Error AlreadyTerminated = Error.Custom(
            "Honeypot.AlreadyTerminated",
            "Honeypot has already been terminated.");

        public static readonly Error CannotPauseInactiveHoneypot = Error.Custom(
            "Honeypot.CannotPauseInactiveHoneypot",
            "Cannot pause honeypot that is not active.");

        public static readonly Error CannotResumeActiveHoneypot = Error.Custom(
            "Honeypot.CannotResumeActiveHoneypot",
            "Cannot resume honeypot that is already active.");

        public static readonly Error CannotTerminateTerminatedHoneypot = Error.Custom(
            "Honeypot.CannotTerminateTerminatedHoneypot",
            "Cannot terminate honeypot that is already terminated.");

        public static readonly Error InvalidStatusTransition = Error.Custom(
            "Honeypot.InvalidStatusTransition",
            "The requested status transition is not allowed.");

        // Health check errors
        public static readonly Error HealthCheckFailed = Error.Custom(
            "Honeypot.HealthCheckFailed",
            "Honeypot health check failed.");

        public static readonly Error HealthCheckTimeout = Error.Custom(
            "Honeypot.HealthCheckTimeout",
            "Honeypot health check timed out.");

        public static readonly Error ConsecutiveHealthCheckFailures = Error.Custom(
            "Honeypot.ConsecutiveHealthCheckFailures",
            "Honeypot has failed consecutive health checks.");

        // Configuration errors
        public static readonly Error CannotUpdateTerminatedHoneypot = Error.Custom(
            "Honeypot.CannotUpdateTerminatedHoneypot",
            "Cannot update configuration of a terminated honeypot.");

        public static readonly Error CannotUpdateDeployingHoneypot = Error.Custom(
            "Honeypot.CannotUpdateDeployingHoneypot",
            "Cannot update configuration while honeypot is being deployed.");

        public static readonly Error InvalidConfigurationUpdate = Error.Custom(
            "Honeypot.InvalidConfigurationUpdate",
            "The configuration update is invalid.");

        // External service linking
        public static readonly Error ExternalServiceAlreadyLinked = Error.Custom(
            "Honeypot.ExternalServiceAlreadyLinked",
            "Honeypot is already linked to an external service.");

        public static readonly Error ExternalServiceNotLinked = Error.Custom(
            "Honeypot.ExternalServiceNotLinked",
            "Honeypot is not linked to an external service.");

        public static readonly Error InvalidExternalServiceId = Error.Custom(
            "Honeypot.InvalidExternalServiceId",
            "External service ID cannot be empty.");

        // Organization and subscription
        public static readonly Error InvalidOrganizationId = Error.Custom(
            "Honeypot.InvalidOrganizationId",
            "Organization ID cannot be empty.");

        public static readonly Error InvalidSubscriptionId = Error.Custom(
            "Honeypot.InvalidSubscriptionId",
            "Subscription ID cannot be empty.");

        public static readonly Error OrganizationNotFound = Error.Custom(
            "Honeypot.OrganizationNotFound",
            "Organization not found.");

        public static readonly Error SubscriptionNotFound = Error.Custom(
            "Honeypot.SubscriptionNotFound",
            "Subscription not found.");

        // Termination errors
        public static readonly Error CannotTerminateActiveHoneypot = Error.Custom(
            "Honeypot.CannotTerminateActiveHoneypot",
            "Cannot terminate active honeypot. Pause it first.");

        public static readonly Error TerminationFailed = Error.Custom(
            "Honeypot.TerminationFailed",
            "Failed to terminate honeypot on external service.");

        // Event capture errors
        public static readonly Error CannotCaptureEventsOnInactiveHoneypot = Error.Custom(
            "Honeypot.CannotCaptureEventsOnInactiveHoneypot",
            "Cannot capture events on inactive honeypot.");

        public static readonly Error EventLogFetchFailed = Error.Custom(
            "Honeypot.EventLogFetchFailed",
            "Failed to fetch event logs from external service.");

        // Network info errors
        public static readonly Error InvalidIpAddress = Error.Custom(
            "Honeypot.InvalidIpAddress",
            "Invalid IP address specified.");

        public static readonly Error NetworkInfoNotAvailable = Error.Custom(
            "Honeypot.NetworkInfoNotAvailable",
            "Network information is not available for this honeypot.");

        // Rate limiting
        public static readonly Error RateLimitExceeded = Error.Custom(
            "Honeypot.RateLimitExceeded",
            "Too many honeypot operations. Please try again later.");

        // Concurrency errors
        public static readonly Error ConcurrencyConflict = Error.Custom(
            "Honeypot.ConcurrencyConflict",
            "Honeypot was modified by another process. Please refresh and try again.");

        // NEW: Heartbeat errors (Phase 2A-2)
        public static readonly Error NotDeployed = Error.Custom(
            "Honeypot.NotDeployed",
            "Honeypot must be deployed before recording heartbeat.");

        public static readonly Error InvalidAgentId = Error.Custom(
            "Honeypot.InvalidAgentId",
            "Agent ID cannot be empty.");

        public static readonly Error HeartbeatTimeout = Error.Custom(
            "Honeypot.HeartbeatTimeout",
            "Honeypot heartbeat has timed out.");

        public static readonly Error AlreadyOffline = Error.Custom(
            "Honeypot.AlreadyOffline",
            "Honeypot is already marked as offline.");

        public static readonly Error InvalidNote = Error.Custom(
            "Honeypot.InvalidNote",
            "Note cannot be empty.");

        public static readonly Error NoteTooLong = Error.Custom(
            "Honeypot.NoteTooLong",
            "Note cannot exceed 1000 characters.");
    }
}
