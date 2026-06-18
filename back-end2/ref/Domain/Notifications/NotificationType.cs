using SharedKernel;

namespace Domain.Notifications;

/// <summary>
/// Defines a type of notification that can be sent in the system.
/// This is a lookup/reference entity that defines notification templates and defaults.
/// </summary>
public sealed class NotificationType : Entity
{
    public Guid Id { get; private set; }

    /// <summary>
    /// Unique code for this notification type (e.g., "stress.high_detected", "stress.weekly_summary").
    /// </summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>
    /// Display name for this notification type.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Description of when this notification is triggered.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Category for grouping in UI.
    /// </summary>
    public NotificationCategory Category { get; private set; }

    /// <summary>
    /// Default priority for this type.
    /// </summary>
    public NotificationPriority DefaultPriority { get; private set; }

    /// <summary>
    /// Default channels for this type (flags).
    /// </summary>
    public NotificationChannel DefaultChannels { get; private set; }

    /// <summary>
    /// Icon name for UI display (e.g., "bell", "user-plus").
    /// </summary>
    public string? IconName { get; private set; }

    /// <summary>
    /// Color hex code for UI display (e.g., "#3498db").
    /// </summary>
    public string? ColorHex { get; private set; }

    /// <summary>
    /// Whether this notification type is active.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Whether this is a system type that cannot be disabled by users.
    /// </summary>
    public bool IsSystemType { get; private set; }

    /// <summary>
    /// Template for notification title with placeholders (e.g., "High Stress Detected").
    /// </summary>
    public string TemplateTitle { get; private set; } = string.Empty;

    /// <summary>
    /// Template for notification body with placeholders.
    /// </summary>
    public string TemplateBody { get; private set; } = string.Empty;

    /// <summary>
    /// When this type was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    private NotificationType()
    {
    }

    public static NotificationType Create(
        string code,
        string name,
        NotificationCategory category,
        string templateTitle,
        string templateBody,
        NotificationPriority defaultPriority = NotificationPriority.Normal,
        NotificationChannel defaultChannels = NotificationChannel.InApp,
        string? description = null,
        string? iconName = null,
        string? colorHex = null,
        bool isSystemType = false)
    {
        var notificationType = new NotificationType
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            Description = description,
            Category = category,
            DefaultPriority = defaultPriority,
            DefaultChannels = defaultChannels,
            IconName = iconName,
            ColorHex = colorHex,
            IsActive = true,
            IsSystemType = isSystemType,
            TemplateTitle = templateTitle,
            TemplateBody = templateBody,
            CreatedAt = DateTime.UtcNow
        };

        return notificationType;
    }

    public void Update(
        string name,
        string? description,
        NotificationPriority defaultPriority,
        NotificationChannel defaultChannels,
        string? iconName,
        string? colorHex,
        string templateTitle,
        string templateBody)
    {
        Name = name;
        Description = description;
        DefaultPriority = defaultPriority;
        DefaultChannels = defaultChannels;
        IconName = iconName;
        ColorHex = colorHex;
        TemplateTitle = templateTitle;
        TemplateBody = templateBody;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}
