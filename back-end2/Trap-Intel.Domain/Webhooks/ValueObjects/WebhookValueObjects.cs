using Trap_Intel.Domain.Webhooks.Enums;

namespace Trap_Intel.Domain.Webhooks.ValueObjects;

/// <summary>
/// Record of a webhook delivery attempt.
/// </summary>
public record WebhookDeliveryRecord
{
    /// <summary>
    /// When the delivery was attempted.
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// Event type that triggered the delivery.
    /// </summary>
    public WebhookEventType EventType { get; init; }

    /// <summary>
    /// Whether the delivery was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// HTTP response status code (if received).
    /// </summary>
    public int? ResponseStatusCode { get; init; }

    /// <summary>
    /// How long the delivery took.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Error message (if failed).
    /// </summary>
    public string? ErrorMessage { get; init; }

    public WebhookDeliveryRecord(
        DateTime timestamp,
        WebhookEventType eventType,
        bool success,
        int? responseStatusCode,
        TimeSpan duration,
        string? errorMessage = null)
    {
        Timestamp = timestamp;
        EventType = eventType;
        Success = success;
        ResponseStatusCode = responseStatusCode;
        Duration = duration;
        ErrorMessage = errorMessage;
    }
}

/// <summary>
/// Webhook payload to be delivered.
/// </summary>
public record WebhookPayload
{
    /// <summary>
    /// Unique delivery ID for idempotency.
    /// </summary>
    public string DeliveryId { get; init; }

    /// <summary>
    /// Event type.
    /// </summary>
    public string EventType { get; init; }

    /// <summary>
    /// When the event occurred.
    /// </summary>
    public DateTime EventTime { get; init; }

    /// <summary>
    /// Organization ID.
    /// </summary>
    public Guid OrganizationId { get; init; }

    /// <summary>
    /// Event-specific data.
    /// </summary>
    public object Data { get; init; }

    public WebhookPayload(
        string deliveryId,
        string eventType,
        DateTime eventTime,
        Guid organizationId,
        object data)
    {
        DeliveryId = deliveryId;
        EventType = eventType;
        EventTime = eventTime;
        OrganizationId = organizationId;
        Data = data;
    }

    public static WebhookPayload Create(
        WebhookEventType eventType,
        Guid organizationId,
        object data)
    {
        return new WebhookPayload(
            Guid.NewGuid().ToString("N"),
            eventType.ToString(),
            DateTime.UtcNow,
            organizationId,
            data);
    }
}

/// <summary>
/// Webhook delivery result.
/// </summary>
public record WebhookDeliveryResult
{
    /// <summary>
    /// Whether delivery was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// HTTP status code.
    /// </summary>
    public int? StatusCode { get; init; }

    /// <summary>
    /// Response body (truncated).
    /// </summary>
    public string? ResponseBody { get; init; }

    /// <summary>
    /// Error message (if failed).
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// How long the request took.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Attempt number.
    /// </summary>
    public int AttemptNumber { get; init; }

    public static WebhookDeliveryResult CreateSuccess(int statusCode, string? responseBody, TimeSpan duration, int attempt)
    {
        return new WebhookDeliveryResult
        {
            Success = true,
            StatusCode = statusCode,
            ResponseBody = responseBody?.Length > 500 ? responseBody[..500] : responseBody,
            Duration = duration,
            AttemptNumber = attempt
        };
    }

    public static WebhookDeliveryResult CreateFailure(string errorMessage, int? statusCode, TimeSpan duration, int attempt)
    {
        return new WebhookDeliveryResult
        {
            Success = false,
            StatusCode = statusCode,
            ErrorMessage = errorMessage,
            Duration = duration,
            AttemptNumber = attempt
        };
    }
}

/// <summary>
/// Webhook statistics summary.
/// </summary>
public record WebhookStats
{
    /// <summary>
    /// Total deliveries.
    /// </summary>
    public long TotalDeliveries { get; init; }

    /// <summary>
    /// Successful deliveries.
    /// </summary>
    public long SuccessfulDeliveries { get; init; }

    /// <summary>
    /// Failed deliveries.
    /// </summary>
    public long FailedDeliveries { get; init; }

    /// <summary>
    /// Success rate percentage.
    /// </summary>
    public decimal SuccessRate { get; init; }

    /// <summary>
    /// Average response time.
    /// </summary>
    public TimeSpan AverageResponseTime { get; init; }

    /// <summary>
    /// Last delivery time.
    /// </summary>
    public DateTime? LastDeliveryAt { get; init; }

    /// <summary>
    /// Current consecutive failures.
    /// </summary>
    public int ConsecutiveFailures { get; init; }

    /// <summary>
    /// Whether webhook is healthy.
    /// </summary>
    public bool IsHealthy { get; init; }

    public static WebhookStats Calculate(
        long totalDeliveries,
        long successfulDeliveries,
        long failedDeliveries,
        TimeSpan averageResponseTime,
        DateTime? lastDeliveryAt,
        int consecutiveFailures)
    {
        var successRate = totalDeliveries > 0
            ? Math.Round((decimal)successfulDeliveries / totalDeliveries * 100, 2)
            : 100m;

        return new WebhookStats
        {
            TotalDeliveries = totalDeliveries,
            SuccessfulDeliveries = successfulDeliveries,
            FailedDeliveries = failedDeliveries,
            SuccessRate = successRate,
            AverageResponseTime = averageResponseTime,
            LastDeliveryAt = lastDeliveryAt,
            ConsecutiveFailures = consecutiveFailures,
            IsHealthy = successRate >= 95 && consecutiveFailures < 3
        };
    }
}
