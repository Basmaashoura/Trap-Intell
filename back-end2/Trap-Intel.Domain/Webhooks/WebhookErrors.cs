using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Webhooks;

public static class WebhookErrors
{
    #region Validation Errors

    public static readonly Error InvalidOrganizationId = Error.Custom(
        "Webhook.InvalidOrganizationId",
        "Organization ID cannot be empty");

    public static readonly Error InvalidUserId = Error.Custom(
        "Webhook.InvalidUserId",
        "User ID cannot be empty");

    public static readonly Error InvalidName = Error.Custom(
        "Webhook.InvalidName",
        "Webhook name cannot be empty");

    public static readonly Error NameTooLong = Error.Custom(
        "Webhook.NameTooLong",
        "Webhook name cannot exceed 100 characters");

    public static readonly Error InvalidUrl = Error.Custom(
        "Webhook.InvalidUrl",
        "Webhook URL cannot be empty");

    public static readonly Error InvalidUrlFormat = Error.Custom(
        "Webhook.InvalidUrlFormat",
        "Webhook URL must be a valid HTTP or HTTPS URL");

    public static readonly Error InvalidReason = Error.Custom(
        "Webhook.InvalidReason",
        "Reason cannot be empty");

    public static readonly Error NoEventsSpecified = Error.Custom(
        "Webhook.NoEventsSpecified",
        "At least one event type must be specified");

    public static readonly Error InvalidTimeout = Error.Custom(
        "Webhook.InvalidTimeout",
        "Timeout must be between 5 and 120 seconds");

    public static readonly Error InvalidMaxRetries = Error.Custom(
        "Webhook.InvalidMaxRetries",
        "Max retries must be between 0 and 10");

    #endregion

    #region Status Errors

    public static readonly Error CannotEnableDeletedWebhook = Error.Custom(
        "Webhook.CannotEnableDeleted",
        "Cannot enable a deleted webhook");

    public static readonly Error CannotUpdateDeletedWebhook = Error.Custom(
        "Webhook.CannotUpdateDeleted",
        "Cannot update a deleted webhook");

    public static readonly Error WebhookNotActive = Error.Custom(
        "Webhook.NotActive",
        "Webhook is not active");

    #endregion

    #region Query Errors

    public static readonly Error WebhookNotFound = Error.Custom(
        "Webhook.NotFound",
        "Webhook not found");

    #endregion

    #region Quota Errors

    public static readonly Error MaxWebhooksReached = Error.Custom(
        "Webhook.MaxWebhooksReached",
        "Maximum number of webhooks reached for this organization");

    #endregion

    #region Delivery Errors

    public static readonly Error DeliveryFailed = Error.Custom(
        "Webhook.DeliveryFailed",
        "Webhook delivery failed");

    public static readonly Error VerificationFailed = Error.Custom(
        "Webhook.VerificationFailed",
        "Webhook endpoint verification failed");

    #endregion

    #region Factory Methods

    public static Error NotFoundById(Guid webhookId) => Error.Custom(
        "Webhook.NotFound",
        $"Webhook with ID '{webhookId}' not found");

    public static Error DeliveryFailedWithReason(string reason) => Error.Custom(
        "Webhook.DeliveryFailed",
        $"Webhook delivery failed: {reason}");

    #endregion
}
