#pragma warning disable CA1054 // URI parameters should not be strings

namespace Application.Notifications.Common;

/// <summary>
/// Response DTO for a notification.
/// </summary>
public sealed record NotificationResponse(
    Guid Id,
    string Type,
    string TypeName,
    string Title,
    string Message,
    string Priority,
    string? Icon,
    string? Color,
    string? EntityType,
    Guid? EntityId,
    string? ActionUrl,
    string? ActionText,
    bool IsRead,
    DateTime? ReadAt,
    DateTime CreatedAt,
    string? CreatedByName);

/// <summary>
/// Response DTO for a notification type.
/// </summary>
public sealed record NotificationTypeResponse(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string Category,
    string DefaultPriority,
    bool IsSystemType,
    string? Icon,
    string? Color);

/// <summary>
/// Response DTO for user notification preferences.
/// </summary>
public sealed record NotificationPreferencesResponse(
    bool InAppEnabled,
    bool EmailEnabled,
    bool PushEnabled,
    bool SmsEnabled,
    bool EmailDigestEnabled,
    string EmailDigestFrequency,
    TimeOnly? EmailDigestTime,
    bool QuietHoursEnabled,
    TimeOnly? QuietHoursStart,
    TimeOnly? QuietHoursEnd,
    string? QuietHoursTimezone,
    List<NotificationTypeSettingResponse> TypeSettings);

/// <summary>
/// Response DTO for a notification type setting.
/// </summary>
public sealed record NotificationTypeSettingResponse(
    Guid TypeId,
    string TypeCode,
    string TypeName,
    bool IsEnabled,
    List<string> Channels);
