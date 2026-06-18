using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Notifications.Enums;

namespace Trap_Intel.Domain.Notifications;

/// <summary>
/// Value Object to hold a push notification device token for users.
/// Used to signal devices asynchronously across Web, iOS, Android.
/// 
/// Re-engineered for Trap-Intel using Records.
/// </summary>
public sealed class UserPushToken : Entity<Guid>
{
    private UserPushToken()
    {
    }

    private UserPushToken(Guid id, Guid userId, string token, PushPlatform platform, string deviceId) 
        : base(id)
    {
        UserId = userId;
        Token = token;
        Platform = platform;
        DeviceId = deviceId;
        CreatedAt = DateTime.UtcNow;
        LastUsedAt = DateTime.UtcNow;
    }

    public Guid UserId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public PushPlatform Platform { get; private set; }
    public string DeviceId { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }
    public DateTime LastUsedAt { get; private set; }

    public static Result<UserPushToken> Create(
        Guid userId,
        string token,
        PushPlatform platform,
        string deviceId)
    {
        if (userId == Guid.Empty)
            return Result.Failure<UserPushToken>(NotificationErrors.TargetUserRequired);

        if (string.IsNullOrWhiteSpace(token))
            return Result.Failure<UserPushToken>(NotificationErrors.InvalidToken);

        var pushToken = new UserPushToken(Guid.NewGuid(), userId, token, platform, deviceId);
        return Result.Success(pushToken);
    }

    public void UpdateLastUsed()
    {
        LastUsedAt = DateTime.UtcNow;
    }
}
