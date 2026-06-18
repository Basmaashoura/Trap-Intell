using Domain.Users;
using SharedKernel;

namespace Domain.Notifications;

/// <summary>
/// User's global notification preferences.
/// </summary>
public sealed class UserNotificationPreferences : Entity
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The user these preferences belong to.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Whether in-app notifications are enabled.
    /// </summary>
    public bool InAppEnabled { get; private set; } = true;

    /// <summary>
    /// Whether email notifications are enabled.
    /// </summary>
    public bool EmailEnabled { get; private set; } = true;

    /// <summary>
    /// Whether push notifications are enabled.
    /// </summary>
    public bool PushEnabled { get; private set; } = true;

    /// <summary>
    /// Whether SMS notifications are enabled.
    /// </summary>
    public bool SmsEnabled { get; private set; }

    /// <summary>
    /// Whether email digest is enabled.
    /// </summary>
    public bool EmailDigestEnabled { get; private set; }

    /// <summary>
    /// Frequency of email digest.
    /// </summary>
    public DigestFrequency EmailDigestFrequency { get; private set; } = DigestFrequency.Daily;

    /// <summary>
    /// Time to send email digest (e.g., 09:00).
    /// </summary>
    public TimeOnly? EmailDigestTime { get; private set; }

    /// <summary>
    /// Whether quiet hours are enabled.
    /// </summary>
    public bool QuietHoursEnabled { get; private set; }

    /// <summary>
    /// Start of quiet hours (e.g., 22:00).
    /// </summary>
    public TimeOnly? QuietHoursStart { get; private set; }

    /// <summary>
    /// End of quiet hours (e.g., 08:00).
    /// </summary>
    public TimeOnly? QuietHoursEnd { get; private set; }

    /// <summary>
    /// Timezone for quiet hours (e.g., "Africa/Cairo").
    /// </summary>
    public string? QuietHoursTimezone { get; private set; }

    /// <summary>
    /// When these preferences were last updated.
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    // Navigation property
    public User? User { get; init; }

    private UserNotificationPreferences()
    {
    }

    public static UserNotificationPreferences CreateDefault(Guid userId)
    {
        return new UserNotificationPreferences
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            InAppEnabled = true,
            EmailEnabled = true,
            PushEnabled = true,
            SmsEnabled = false,
            EmailDigestEnabled = false,
            EmailDigestFrequency = DigestFrequency.Daily,
            QuietHoursEnabled = false,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void UpdateChannelSettings(
        bool inAppEnabled,
        bool emailEnabled,
        bool pushEnabled,
        bool smsEnabled)
    {
        InAppEnabled = inAppEnabled;
        EmailEnabled = emailEnabled;
        PushEnabled = pushEnabled;
        SmsEnabled = smsEnabled;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDigestSettings(
        bool enabled,
        DigestFrequency frequency,
        TimeOnly? time)
    {
        EmailDigestEnabled = enabled;
        EmailDigestFrequency = frequency;
        EmailDigestTime = time;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateQuietHours(
        bool enabled,
        TimeOnly? start,
        TimeOnly? end,
        string? timezone)
    {
        QuietHoursEnabled = enabled;
        QuietHoursStart = start;
        QuietHoursEnd = end;
        QuietHoursTimezone = timezone;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if the given time is within quiet hours.
    /// </summary>
    public bool IsInQuietHours(DateTime utcNow)
    {
        if (!QuietHoursEnabled || !QuietHoursStart.HasValue || !QuietHoursEnd.HasValue)
        {
            return false;
        }

        TimeZoneInfo tz = GetQuietHoursTimeZone();
        DateTime userLocalTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, tz);
        var currentTime = TimeOnly.FromDateTime(userLocalTime);

        TimeOnly start = QuietHoursStart.Value;
        TimeOnly end = QuietHoursEnd.Value;

        if (start > end)
        {
            return currentTime >= start || currentTime <= end;
        }

        return currentTime >= start && currentTime <= end;
    }

    public DateTime? GetQuietHoursResumeTime(DateTime utcNow)
    {
        if (!IsInQuietHours(utcNow) || !QuietHoursStart.HasValue || !QuietHoursEnd.HasValue)
        {
            return null;
        }

        TimeZoneInfo tz = GetQuietHoursTimeZone();
        DateTime userLocalTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, tz);
        var currentDate = DateOnly.FromDateTime(userLocalTime);
        TimeOnly start = QuietHoursStart.Value;
        TimeOnly end = QuietHoursEnd.Value;

        DateTime quietEndLocal;

        if (start > end)
        {
            bool beforeEnd = userLocalTime.TimeOfDay <= end.ToTimeSpan();
            DateOnly endDate = beforeEnd ? currentDate : currentDate.AddDays(1);
            quietEndLocal = endDate.ToDateTime(end);
        }
        else
        {
            var todayEnd = currentDate.ToDateTime(end);

            quietEndLocal = userLocalTime.TimeOfDay <= end.ToTimeSpan()
                ? todayEnd
                : currentDate.AddDays(1).ToDateTime(end);
        }

        return TimeZoneInfo.ConvertTimeToUtc(quietEndLocal, tz);
    }

    private TimeZoneInfo GetQuietHoursTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(QuietHoursTimezone ?? "UTC");
        }
        catch
        {
            return TimeZoneInfo.Utc;
        }
    }

    /// <summary>
    /// Gets enabled channels as flags.
    /// </summary>
    public NotificationChannel GetEnabledChannels()
    {
        NotificationChannel channels = NotificationChannel.None;

        if (InAppEnabled)
        {
            channels |= NotificationChannel.InApp;
        }
        if (EmailEnabled)
        {
            channels |= NotificationChannel.Email;
        }
        if (PushEnabled)
        {
            channels |= NotificationChannel.Push;
        }
        if (SmsEnabled)
        {
            channels |= NotificationChannel.Sms;
        }

        return channels;
    }
}
