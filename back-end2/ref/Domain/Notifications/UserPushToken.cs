using Domain.Users;
using SharedKernel;

namespace Domain.Notifications;

/// <summary>
/// Stores a user's push notification token for a specific device.
/// </summary>
public sealed class UserPushToken : Entity
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The user this token belongs to.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// The push notification token from the device.
    /// </summary>
    public string Token { get; private set; } = string.Empty;

    /// <summary>
    /// The platform this token is for.
    /// </summary>
    public PushPlatform Platform { get; private set; }

    /// <summary>
    /// Optional device name for display.
    /// </summary>
    public string? DeviceName { get; private set; }

    /// <summary>
    /// Whether this token is currently active.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// When this token was last used successfully.
    /// </summary>
    public DateTime LastUsedAt { get; private set; }

    /// <summary>
    /// When this token was registered.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    // Navigation property
    public User? User { get; init; }

    private UserPushToken()
    {
    }

    public static UserPushToken Create(
        Guid userId,
        string token,
        PushPlatform platform,
        string? deviceName = null)
    {
        return new UserPushToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = token,
            Platform = platform,
            DeviceName = deviceName,
            IsActive = true,
            LastUsedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateToken(string token)
    {
        Token = token;
        LastUsedAt = DateTime.UtcNow;
    }

    public void UpdateLastUsed()
    {
        LastUsedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        LastUsedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
