using System.Security.Cryptography;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Shared;
using Trap_Intel.Domain.Webhooks.Enums;
using Trap_Intel.Domain.Webhooks.Events;
using Trap_Intel.Domain.Webhooks.ValueObjects;

namespace Trap_Intel.Domain.Webhooks;

/// <summary>
/// Represents a webhook endpoint registered by an organization.
/// Enables real-time event delivery to external systems.
/// Supports signature verification, retry logic, and event filtering.
/// </summary>
public class Webhook : AggregateRoot<Guid>
{
    private List<WebhookEventType> _subscribedEvents = new();
    private List<WebhookDeliveryRecord> _recentDeliveries = new();

    // Private constructor for EF
    private Webhook() { }

    private Webhook(
        Guid id,
        Guid organizationId,
        string name,
        string url,
        string secretHash,
        Guid createdByUserId)
        : base(id)
    {
        OrganizationId = organizationId;
        Name = name;
        Url = url;
        SecretHash = secretHash;
        CreatedByUserId = createdByUserId;
        Status = WebhookStatus.Active;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        ConsecutiveFailures = 0;
        TotalDeliveries = 0;
        SuccessfulDeliveries = 0;
        FailedDeliveries = 0;
    }

    #region Properties

    /// <summary>
    /// Organization that owns this webhook.
    /// </summary>
    public Guid OrganizationId { get; private set; }

    /// <summary>
    /// Human-readable name for the webhook.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Optional description.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Target URL for webhook delivery.
    /// </summary>
    public string Url { get; private set; } = string.Empty;

    /// <summary>
    /// SHA-256 hash of the webhook secret (for signature verification).
    /// </summary>
    public string SecretHash { get; private set; } = string.Empty;

    /// <summary>
    /// Current status.
    /// </summary>
    public WebhookStatus Status { get; private set; }

    /// <summary>
    /// Content type for delivery (JSON, Form).
    /// </summary>
    public WebhookContentType ContentType { get; private set; } = WebhookContentType.Json;

    /// <summary>
    /// Whether SSL verification is enabled.
    /// </summary>
    public bool SslVerificationEnabled { get; private set; } = true;

    /// <summary>
    /// Custom headers to include in requests.
    /// </summary>
    public Dictionary<string, string> CustomHeaders { get; private set; } = new();

    /// <summary>
    /// Timeout in seconds for delivery.
    /// </summary>
    public int TimeoutSeconds { get; private set; } = 30;

    /// <summary>
    /// Maximum retry attempts for failed deliveries.
    /// </summary>
    public int MaxRetries { get; private set; } = 3;

    /// <summary>
    /// Events this webhook is subscribed to.
    /// </summary>
    public IReadOnlyList<WebhookEventType> SubscribedEvents => _subscribedEvents.AsReadOnly();

    /// <summary>
    /// When webhook was last triggered.
    /// </summary>
    public DateTime? LastTriggeredAt { get; private set; }

    /// <summary>
    /// When webhook last successfully delivered.
    /// </summary>
    public DateTime? LastSuccessAt { get; private set; }

    /// <summary>
    /// When webhook last failed.
    /// </summary>
    public DateTime? LastFailureAt { get; private set; }

    /// <summary>
    /// Last failure message.
    /// </summary>
    public string? LastFailureMessage { get; private set; }

    /// <summary>
    /// Consecutive failures (for circuit breaker).
    /// </summary>
    public int ConsecutiveFailures { get; private set; }

    /// <summary>
    /// Total deliveries attempted.
    /// </summary>
    public long TotalDeliveries { get; private set; }

    /// <summary>
    /// Successful deliveries.
    /// </summary>
    public long SuccessfulDeliveries { get; private set; }

    /// <summary>
    /// Failed deliveries.
    /// </summary>
    public long FailedDeliveries { get; private set; }

    /// <summary>
    /// When URL was last verified (ping test).
    /// </summary>
    public DateTime? VerifiedAt { get; private set; }

    /// <summary>
    /// Whether endpoint passed verification.
    /// </summary>
    public bool IsVerified { get; private set; }

    /// <summary>
    /// User who created this webhook.
    /// </summary>
    public Guid CreatedByUserId { get; private set; }

    /// <summary>
    /// When webhook was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When webhook was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Recent delivery records (last 50).
    /// </summary>
    public IReadOnlyList<WebhookDeliveryRecord> RecentDeliveries => _recentDeliveries.AsReadOnly();

    #endregion

    #region Factory Methods

    /// <summary>
    /// Create a new webhook.
    /// Returns the webhook entity AND the raw secret (only returned once).
    /// </summary>
    public static Result<(Webhook Webhook, string RawSecret)> Create(
        Guid organizationId,
        string name,
        string url,
        Guid createdByUserId,
        List<WebhookEventType>? subscribedEvents = null,
        string? description = null)
    {
        // Validation
        if (organizationId == Guid.Empty)
            return Result.Failure<(Webhook, string)>(WebhookErrors.InvalidOrganizationId);

        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<(Webhook, string)>(WebhookErrors.InvalidName);

        if (name.Length > 100)
            return Result.Failure<(Webhook, string)>(WebhookErrors.NameTooLong);

        if (string.IsNullOrWhiteSpace(url))
            return Result.Failure<(Webhook, string)>(WebhookErrors.InvalidUrl);

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return Result.Failure<(Webhook, string)>(WebhookErrors.InvalidUrlFormat);

        if (createdByUserId == Guid.Empty)
            return Result.Failure<(Webhook, string)>(WebhookErrors.InvalidUserId);

        // Generate secure secret
        var (rawSecret, secretHash) = GenerateSecret();

        var webhook = new Webhook(
            Guid.NewGuid(),
            organizationId,
            name.Trim(),
            url.Trim(),
            secretHash,
            createdByUserId)
        {
            Description = description?.Trim(),
            _subscribedEvents = subscribedEvents ?? new List<WebhookEventType>
            {
                WebhookEventType.AttackDetected,
                WebhookEventType.AlertCreated,
                WebhookEventType.ThreatActorIdentified
            }
        };

        webhook.RaiseDomainEvent(new WebhookCreatedEvent(
            webhook.Id,
            organizationId,
            name,
            url,
            createdByUserId,
            DateTime.UtcNow));

        return Result.Success((webhook, rawSecret));
    }

    /// <summary>
    /// Reconstruct from database.
    /// </summary>
    public static Webhook Reconstruct(
        Guid id,
        Guid organizationId,
        string name,
        string? description,
        string url,
        string secretHash,
        WebhookStatus status,
        WebhookContentType contentType,
        bool sslVerificationEnabled,
        Dictionary<string, string> customHeaders,
        int timeoutSeconds,
        int maxRetries,
        DateTime? lastTriggeredAt,
        DateTime? lastSuccessAt,
        DateTime? lastFailureAt,
        string? lastFailureMessage,
        int consecutiveFailures,
        long totalDeliveries,
        long successfulDeliveries,
        long failedDeliveries,
        DateTime? verifiedAt,
        bool isVerified,
        Guid createdByUserId,
        DateTime createdAt,
        DateTime updatedAt,
        List<WebhookEventType>? subscribedEvents = null,
        List<WebhookDeliveryRecord>? recentDeliveries = null)
    {
        return new Webhook
        {
            Id = id,
            OrganizationId = organizationId,
            Name = name,
            Description = description,
            Url = url,
            SecretHash = secretHash,
            Status = status,
            ContentType = contentType,
            SslVerificationEnabled = sslVerificationEnabled,
            CustomHeaders = customHeaders,
            TimeoutSeconds = timeoutSeconds,
            MaxRetries = maxRetries,
            LastTriggeredAt = lastTriggeredAt,
            LastSuccessAt = lastSuccessAt,
            LastFailureAt = lastFailureAt,
            LastFailureMessage = lastFailureMessage,
            ConsecutiveFailures = consecutiveFailures,
            TotalDeliveries = totalDeliveries,
            SuccessfulDeliveries = successfulDeliveries,
            FailedDeliveries = failedDeliveries,
            VerifiedAt = verifiedAt,
            IsVerified = isVerified,
            CreatedByUserId = createdByUserId,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            _subscribedEvents = subscribedEvents ?? new(),
            _recentDeliveries = recentDeliveries ?? new()
        };
    }

    #endregion

    #region Domain Behaviors

    /// <summary>
    /// Record successful delivery.
    /// </summary>
    public Result RecordSuccessfulDelivery(
        WebhookEventType eventType,
        int responseStatusCode,
        TimeSpan duration)
    {
        LastTriggeredAt = DateTime.UtcNow;
        LastSuccessAt = DateTime.UtcNow;
        TotalDeliveries++;
        SuccessfulDeliveries++;
        ConsecutiveFailures = 0;
        UpdatedAt = DateTime.UtcNow;

        // Add to recent deliveries
        _recentDeliveries.Add(new WebhookDeliveryRecord(
            DateTime.UtcNow,
            eventType,
            true,
            responseStatusCode,
            duration,
            null));

        if (_recentDeliveries.Count > 50)
        {
            _recentDeliveries.RemoveAt(0);
        }

        // If was disabled due to failures, reactivate
        if (Status == WebhookStatus.DisabledByFailures)
        {
            Status = WebhookStatus.Active;
        }

        RaiseDomainEvent(new WebhookDeliverySucceededEvent(
            Id,
            OrganizationId,
            eventType,
            responseStatusCode,
            duration,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Record failed delivery.
    /// </summary>
    public Result RecordFailedDelivery(
        WebhookEventType eventType,
        int? responseStatusCode,
        TimeSpan duration,
        string errorMessage)
    {
        LastTriggeredAt = DateTime.UtcNow;
        LastFailureAt = DateTime.UtcNow;
        LastFailureMessage = errorMessage;
        TotalDeliveries++;
        FailedDeliveries++;
        ConsecutiveFailures++;
        UpdatedAt = DateTime.UtcNow;

        // Add to recent deliveries
        _recentDeliveries.Add(new WebhookDeliveryRecord(
            DateTime.UtcNow,
            eventType,
            false,
            responseStatusCode,
            duration,
            errorMessage));

        if (_recentDeliveries.Count > 50)
        {
            _recentDeliveries.RemoveAt(0);
        }

        RaiseDomainEvent(new WebhookDeliveryFailedEvent(
            Id,
            OrganizationId,
            eventType,
            responseStatusCode,
            errorMessage,
            ConsecutiveFailures,
            DateTime.UtcNow));

        // Auto-disable after too many consecutive failures
        if (ConsecutiveFailures >= 10 && Status == WebhookStatus.Active)
        {
            Status = WebhookStatus.DisabledByFailures;

            RaiseDomainEvent(new WebhookAutoDisabledEvent(
                Id,
                OrganizationId,
                ConsecutiveFailures,
                DateTime.UtcNow));
        }

        return Result.Success();
    }

    /// <summary>
    /// Mark webhook as verified after ping test.
    /// </summary>
    public Result MarkAsVerified()
    {
        VerifiedAt = DateTime.UtcNow;
        IsVerified = true;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new WebhookVerifiedEvent(
            Id,
            OrganizationId,
            Url,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Mark verification as failed.
    /// </summary>
    public Result MarkVerificationFailed(string reason)
    {
        VerifiedAt = DateTime.UtcNow;
        IsVerified = false;
        LastFailureMessage = reason;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new WebhookVerificationFailedEvent(
            Id,
            OrganizationId,
            Url,
            reason,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Enable the webhook.
    /// </summary>
    public Result Enable()
    {
        if (Status == WebhookStatus.Deleted)
            return Result.Failure(WebhookErrors.CannotEnableDeletedWebhook);

        var oldStatus = Status;
        Status = WebhookStatus.Active;
        ConsecutiveFailures = 0;
        UpdatedAt = DateTime.UtcNow;

        if (oldStatus != WebhookStatus.Active)
        {
            RaiseDomainEvent(new WebhookStatusChangedEvent(
                Id,
                OrganizationId,
                oldStatus,
                WebhookStatus.Active,
                "Manually enabled",
                DateTime.UtcNow));
        }

        return Result.Success();
    }

    /// <summary>
    /// Disable the webhook.
    /// </summary>
    public Result Disable(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure(WebhookErrors.InvalidReason);

        var oldStatus = Status;
        Status = WebhookStatus.Disabled;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new WebhookStatusChangedEvent(
            Id,
            OrganizationId,
            oldStatus,
            WebhookStatus.Disabled,
            reason,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Delete the webhook (soft delete).
    /// </summary>
    public Result Delete()
    {
        if (Status == WebhookStatus.Deleted)
            return Result.Success(); // Already deleted

        var oldStatus = Status;
        Status = WebhookStatus.Deleted;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new WebhookDeletedEvent(
            Id,
            OrganizationId,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Update webhook URL (requires secret rotation).
    /// Returns new secret.
    /// </summary>
    public Result<string> UpdateUrl(string newUrl)
    {
        if (string.IsNullOrWhiteSpace(newUrl))
            return Result.Failure<string>(WebhookErrors.InvalidUrl);

        if (!Uri.TryCreate(newUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return Result.Failure<string>(WebhookErrors.InvalidUrlFormat);

        if (Status == WebhookStatus.Deleted)
            return Result.Failure<string>(WebhookErrors.CannotUpdateDeletedWebhook);

        var oldUrl = Url;
        Url = newUrl.Trim();

        // Rotate secret when URL changes
        var (rawSecret, secretHash) = GenerateSecret();
        SecretHash = secretHash;

        // Reset verification
        IsVerified = false;
        VerifiedAt = null;

        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new WebhookUrlUpdatedEvent(
            Id,
            OrganizationId,
            oldUrl,
            newUrl,
            DateTime.UtcNow));

        return Result.Success(rawSecret);
    }

    /// <summary>
    /// Rotate webhook secret.
    /// Returns new secret.
    /// </summary>
    public Result<string> RotateSecret()
    {
        if (Status == WebhookStatus.Deleted)
            return Result.Failure<string>(WebhookErrors.CannotUpdateDeletedWebhook);

        var (rawSecret, secretHash) = GenerateSecret();
        SecretHash = secretHash;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new WebhookSecretRotatedEvent(
            Id,
            OrganizationId,
            DateTime.UtcNow));

        return Result.Success(rawSecret);
    }

    /// <summary>
    /// Update subscribed events.
    /// </summary>
    public Result UpdateSubscribedEvents(List<WebhookEventType> events)
    {
        if (events == null || events.Count == 0)
            return Result.Failure(WebhookErrors.NoEventsSpecified);

        if (Status == WebhookStatus.Deleted)
            return Result.Failure(WebhookErrors.CannotUpdateDeletedWebhook);

        _subscribedEvents = events.Distinct().ToList();
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new WebhookEventsUpdatedEvent(
            Id,
            OrganizationId,
            _subscribedEvents,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Update webhook details.
    /// </summary>
    public Result UpdateDetails(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure(WebhookErrors.InvalidName);

        if (name.Length > 100)
            return Result.Failure(WebhookErrors.NameTooLong);

        if (Status == WebhookStatus.Deleted)
            return Result.Failure(WebhookErrors.CannotUpdateDeletedWebhook);

        Name = name.Trim();
        Description = description?.Trim();
        UpdatedAt = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Update delivery settings.
    /// </summary>
    public Result UpdateDeliverySettings(
        int timeoutSeconds,
        int maxRetries,
        bool sslVerificationEnabled,
        WebhookContentType contentType)
    {
        if (timeoutSeconds < 5 || timeoutSeconds > 120)
            return Result.Failure(WebhookErrors.InvalidTimeout);

        if (maxRetries < 0 || maxRetries > 10)
            return Result.Failure(WebhookErrors.InvalidMaxRetries);

        if (Status == WebhookStatus.Deleted)
            return Result.Failure(WebhookErrors.CannotUpdateDeletedWebhook);

        TimeoutSeconds = timeoutSeconds;
        MaxRetries = maxRetries;
        SslVerificationEnabled = sslVerificationEnabled;
        ContentType = contentType;
        UpdatedAt = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Update custom headers.
    /// </summary>
    public Result UpdateCustomHeaders(Dictionary<string, string> headers)
    {
        if (Status == WebhookStatus.Deleted)
            return Result.Failure(WebhookErrors.CannotUpdateDeletedWebhook);

        CustomHeaders = headers ?? new();
        UpdatedAt = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Check if webhook is subscribed to event type.
    /// </summary>
    public bool IsSubscribedTo(WebhookEventType eventType)
    {
        return _subscribedEvents.Contains(eventType) ||
               _subscribedEvents.Contains(WebhookEventType.All);
    }

    /// <summary>
    /// Compute signature for payload.
    /// </summary>
    public static string ComputeSignature(string secret, string payload)
    {
        var keyBytes = System.Text.Encoding.UTF8.GetBytes(secret);
        var payloadBytes = System.Text.Encoding.UTF8.GetBytes(payload);

        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(payloadBytes);
        return $"sha256={Convert.ToHexString(hashBytes).ToLowerInvariant()}";
    }

    #endregion

    #region Query Methods

    /// <summary>
    /// Check if webhook is deliverable.
    /// </summary>
    public bool IsDeliverable => Status == WebhookStatus.Active;

    /// <summary>
    /// Get success rate.
    /// </summary>
    public decimal GetSuccessRate()
    {
        if (TotalDeliveries == 0) return 100m;
        return Math.Round((decimal)SuccessfulDeliveries / TotalDeliveries * 100, 2);
    }

    /// <summary>
    /// Check if webhook is healthy (low failure rate).
    /// </summary>
    public bool IsHealthy => GetSuccessRate() >= 95 && ConsecutiveFailures < 3;

    #endregion

    #region Private Methods

    private static (string RawSecret, string SecretHash) GenerateSecret()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(32);
        var rawSecret = $"whsec_{Convert.ToBase64String(randomBytes).Replace("+", "").Replace("/", "").Replace("=", "")[..40]}";

        var hashBytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawSecret));
        var secretHash = Convert.ToHexString(hashBytes).ToLowerInvariant();

        return (rawSecret, secretHash);
    }

    #endregion
}
