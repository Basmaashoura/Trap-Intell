using Domain.Users;
using SharedKernel;

namespace Domain.Notifications;

/// <summary>
/// User's settings for a specific notification type.
/// </summary>
public sealed class UserNotificationTypeSetting : Entity
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The user these settings belong to.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// The notification type these settings apply to.
    /// </summary>
    public Guid TypeId { get; private set; }

    /// <summary>
    /// Whether notifications of this type are enabled for the user.
    /// </summary>
    public bool IsEnabled { get; private set; } = true;

    /// <summary>
    /// Override channels for this type (null = use defaults).
    /// </summary>
    public NotificationChannel? Channels { get; private set; }

    /// <summary>
    /// When these settings were last updated.
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    // Navigation properties
    public User? User { get; init; }
    public NotificationType? Type { get; init; }

    private UserNotificationTypeSetting()
    {
    }

    public static UserNotificationTypeSetting Create(
        Guid userId,
        Guid typeId,
        bool isEnabled = true,
        NotificationChannel? channels = null)
    {
        return new UserNotificationTypeSetting
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TypeId = typeId,
            IsEnabled = isEnabled,
            Channels = channels,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Update(bool isEnabled, NotificationChannel? channels)
    {
        IsEnabled = isEnabled;
        Channels = channels;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Enable()
    {
        IsEnabled = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Disable()
    {
        IsEnabled = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
