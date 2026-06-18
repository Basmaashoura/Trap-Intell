using System.Text.Json;
using Domain.Notifications;

namespace Application.Abstractions.Notifications;

/// <summary>
/// Message sent to clients via real-time connection.
/// </summary>
public sealed class RealtimeNotificationMessage
{
    /// <summary>
    /// Notification ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Notification type code.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Notification title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Notification message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Priority level.
    /// </summary>
    public string Priority { get; set; } = string.Empty;

    /// <summary>
    /// Icon name for UI.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Color hex for UI.
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Action URL for deep linking.
    /// </summary>
    public string? ActionUrl { get; set; }

    /// <summary>
    /// Action button text.
    /// </summary>
    public string? ActionText { get; set; }

    /// <summary>
    /// Related entity type.
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// Related entity ID.
    /// </summary>
    public Guid? EntityId { get; set; }

    /// <summary>
    /// When the notification was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Additional metadata.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    public static RealtimeNotificationMessage FromNotification(
        Notification notification,
        NotificationType? type = null)
    {
        return new RealtimeNotificationMessage
        {
            Id = notification.Id,
            Type = type?.Code ?? string.Empty,
            Title = notification.Title,
            Message = notification.Message,
            Priority = notification.Priority.ToString(),
            Icon = type?.IconName,
            Color = type?.ColorHex,
            ActionUrl = notification.ActionUrl,
            ActionText = notification.ActionText,
            EntityType = notification.EntityType,
            EntityId = notification.EntityId,
            CreatedAt = notification.CreatedAt,
            Metadata = DeserializeMetadata(notification.Metadata)!
        };
    }

    private static Dictionary<string, object?>? DeserializeMetadata(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(metadataJson);

            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            foreach (JsonProperty property in document.RootElement.EnumerateObject())
            {
                result[property.Name] = ConvertElement(property.Value);
            }

            return result;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static object? ConvertElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject()
                .ToDictionary(p => p.Name, p => ConvertElement(p.Value), StringComparer.OrdinalIgnoreCase),
            JsonValueKind.Array => element.EnumerateArray()
                .Select(ConvertElement)
                .ToList(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out long l) ? l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Undefined => null,
            _ => element.ToString()
        };
    }
}
