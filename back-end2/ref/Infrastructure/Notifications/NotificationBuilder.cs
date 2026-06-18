using System.Text.Json;
using Application.Abstractions.Data;
using Application.Abstractions.Notifications;
using Application.Abstractions.Repositories;
using Domain.Notifications;
using Domain.Users;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Notifications;

/// <summary>
/// Fluent builder for creating and sending notifications.
/// </summary>
internal sealed class NotificationBuilder : INotificationBuilder
{
    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationTypeRepository _typeRepository;
    private readonly INotificationDispatcher _dispatcher;
    private readonly IRealtimeNotificationService _realtimeService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ApplicationDbContext _context;

    private string _typeCode = string.Empty;
    private readonly List<Guid> _recipientUserIds = [];
    private readonly List<string> _recipientRoleNames = [];
    private readonly Dictionary<string, object> _data = [];
    private NotificationPriority? _priority;
    private NotificationChannel? _channels;
    private string? _entityType;
    private Guid? _entityId;
    private string? _actionUrl;
    private string? _actionText;
    private string? _groupKey;
    private DateTime? _scheduledFor;
    private DateTime? _expiresAt;
    private Guid? _createdByUserId;


    public NotificationBuilder(
        INotificationRepository notificationRepository,
        INotificationTypeRepository typeRepository,
        INotificationDispatcher dispatcher,
        IRealtimeNotificationService realtimeService,
        IUnitOfWork unitOfWork,
        ApplicationDbContext context)
    {
        _notificationRepository = notificationRepository;
        _typeRepository = typeRepository;
        _dispatcher = dispatcher;
        _realtimeService = realtimeService;
        _unitOfWork = unitOfWork;
        _context = context;
    }

    public INotificationBuilder WithType(string typeCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(typeCode);
        _typeCode = typeCode;
        return this;
    }

    public INotificationBuilder ToUser(Guid userId)
    {
        if (userId != Guid.Empty)
        {
            _recipientUserIds.Add(userId);
        }
        return this;
    }

    public INotificationBuilder ToUsers(IEnumerable<Guid> userIds)
    {
        _recipientUserIds.AddRange(userIds.Where(id => id != Guid.Empty));
        return this;
    }

    public INotificationBuilder ToRole(string roleName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roleName);
        _recipientRoleNames.Add(roleName);
        return this;
    }

    public INotificationBuilder WithData(object data)
    {
        Dictionary<string, object> dict = TemplateEngine.ObjectToDictionary(data);
        foreach (KeyValuePair<string, object> kvp in dict)
        {
            _data[kvp.Key] = kvp.Value;
        }
        return this;
    }

    public INotificationBuilder WithData(string key, object value)
    {
        _data[key] = value;
        return this;
    }

    public INotificationBuilder WithPriority(NotificationPriority priority)
    {
        _priority = priority;
        return this;
    }

    public INotificationBuilder WithChannels(NotificationChannel channels)
    {
        _channels = channels;
        return this;
    }

    public INotificationBuilder WithEntity(string entityType, Guid entityId)
    {
        _entityType = entityType;
        _entityId = entityId;
        return this;
    }

    public INotificationBuilder WithAction(string url, string text)
    {
        _actionUrl = url;
        _actionText = text;
        return this;
    }

    public INotificationBuilder GroupBy(string groupKey)
    {
        _groupKey = groupKey;
        return this;
    }

    public INotificationBuilder ScheduleFor(DateTime sendAt)
    {
        _scheduledFor = sendAt;
        return this;
    }

    public INotificationBuilder ExpiresAt(DateTime expiresAt)
    {
        _expiresAt = expiresAt;
        return this;
    }

    public INotificationBuilder CreatedBy(Guid userId)
    {
        _createdByUserId = userId;
        return this;
    }

    public INotificationBuilder FromContext(NotificationContext context)
    {
        _recipientUserIds.AddRange(context.RecipientUserIds);

        foreach (KeyValuePair<string, object> kvp in context.Data)
        {
            _data[kvp.Key] = kvp.Value;
        }

        _priority = context.Priority;
        _channels = context.Channels;
        _entityType = context.EntityType;
        _entityId = context.EntityId;
        _actionUrl = context.ActionUrl;
        _actionText = context.ActionText;
        _groupKey = context.GroupKey;
        _scheduledFor = context.ScheduledFor;
        _expiresAt = context.ExpiresAt;
        _createdByUserId = context.CreatedByUserId;

        return this;
    }

    public async Task<NotificationResult> SendAsync(CancellationToken cancellationToken = default)
    {
        // Resolve role-based recipients
        await ResolveRoleRecipientsAsync(cancellationToken);

        if (_recipientUserIds.Count == 0)
        {
            return NotificationResult.Skipped();
        }

        // Validate type code
        if (string.IsNullOrWhiteSpace(_typeCode))
        {
            return NotificationResult.Failure("Notification type code is required");
        }

        // Get notification type
        NotificationType? type = await _typeRepository.GetByCodeAsync(_typeCode, cancellationToken);

        if (type is null)
        {
            return NotificationResult.Failure($"Notification type '{_typeCode}' not found");
        }

        // Process templates
        string title = TemplateEngine.Process(type.TemplateTitle, _data);
        string message = TemplateEngine.Process(type.TemplateBody, _data);

        // Determine priority and channels
        NotificationPriority priority = _priority ?? type.DefaultPriority;
        NotificationChannel channels = _channels ?? type.DefaultChannels;

        // Serialize metadata
        string? metadata = _data.Count > 0
            ? JsonSerializer.Serialize(_data)
            : null;


        var notifications = new List<Notification>();
        var notificationContexts = new List<(Notification Notification, NotificationChannel EffectiveChannels, bool ShouldDispatch)>();
        var failedRecipients = new Dictionary<Guid, string>();

        // Get all user preferences in one query (batch)
        var distinctUserIds = _recipientUserIds.Distinct().ToList();
        Dictionary<Guid, UserNotificationPreferences> preferencesMap = await GetUserPreferencesAsync(distinctUserIds, cancellationToken);
        Dictionary<Guid, UserNotificationTypeSetting> typeSettingsMap = await GetTypeSettingsAsync(distinctUserIds, type.Id, cancellationToken);

        DateTime utcNow = DateTime.UtcNow;

        foreach (Guid userId in distinctUserIds)
        {
            try
            {
                preferencesMap.TryGetValue(userId, out UserNotificationPreferences? preferences);
                typeSettingsMap.TryGetValue(userId, out UserNotificationTypeSetting? typeSetting);

                // Skip if user disabled this type (unless it's a system type)
                if (!type.IsSystemType && typeSetting is not null && !typeSetting.IsEnabled)
                {
                    continue;
                }

                NotificationChannel effectiveChannels = channels;
                if (typeSetting?.Channels.HasValue == true)
                {
                    effectiveChannels = typeSetting.Channels.Value;
                }

                if (preferences is not null)
                {
                    effectiveChannels &= preferences.GetEnabledChannels();
                }

                if (effectiveChannels == NotificationChannel.None)
                {
                    continue;
                }

                DateTime? finalSchedule = _scheduledFor;

                if (priority != NotificationPriority.Urgent && preferences is not null)
                {
                    DateTime? quietResume = preferences.GetQuietHoursResumeTime(utcNow);
                    if (quietResume.HasValue)
                    {
                        finalSchedule = finalSchedule.HasValue && finalSchedule.Value > quietResume.Value
                            ? finalSchedule
                            : quietResume;
                    }
                }

                // Create notification
                var notification = Notification.Create(
                    userId,
                    type.Id,
                    title,
                    message,
                    priority,
                    _entityType,
                    _entityId,
                    _actionUrl,
                    _actionText,
                    metadata,
                    _groupKey,
                    finalSchedule,
                    _expiresAt,
                    _createdByUserId);

                notifications.Add(notification);

                bool shouldDispatch = !finalSchedule.HasValue || finalSchedule.Value <= utcNow;

                notificationContexts.Add((notification, effectiveChannels, shouldDispatch));
            }
            catch (Exception ex)
            {
                failedRecipients[userId] = ex.Message;
            }
        }

        if (notifications.Count == 0)
        {
            return NotificationResult.Skipped();
        }

        // Save all notifications in one batch
        _notificationRepository.AddRange(notifications);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Dispatch notifications that should be sent now
        foreach ((Notification notification, NotificationChannel effectiveChannels, bool shouldDispatch) in notificationContexts)
        {
            if (!shouldDispatch)
            {
                continue;
            }

            try
            {
                await _dispatcher.DispatchAsync(notification, effectiveChannels, cancellationToken);

                // Send real-time notification for InApp channel
                if (effectiveChannels.HasFlag(NotificationChannel.InApp))
                {
                    var realtimeMessage = RealtimeNotificationMessage.FromNotification(notification, type);
                    await _realtimeService.SendToUserAsync(notification.UserId, realtimeMessage, cancellationToken);

                    // Update unread count
                    int unreadCount = await _notificationRepository.GetUnreadCountAsync(notification.UserId, cancellationToken);
                    await _realtimeService.NotifyUnreadCountAsync(notification.UserId, unreadCount, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                failedRecipients[notification.UserId] = ex.Message;
            }
        }

        var notificationIds = notifications.Select(n => n.Id).ToList();

        if (failedRecipients.Count > 0 && notificationIds.Count > 0)
        {
            return NotificationResult.PartialSuccess(notificationIds, notificationIds.Count, failedRecipients);
        }

        return NotificationResult.Success(notificationIds, notificationIds.Count);
    }

    private async Task ResolveRoleRecipientsAsync(CancellationToken cancellationToken)
    {
        if (_recipientRoleNames.Count == 0)
        {
            return;
        }

        List<Guid> userIds = await _context.UserRoles
            .Where(ur => _context.Roles
                .Where(r => _recipientRoleNames.Contains(r.Name) && r.IsActive)
                .Select(r => r.Id)
                .Contains(ur.RoleId))
            .Select(ur => ur.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        _recipientUserIds.AddRange(userIds);
    }

    private async Task<Dictionary<Guid, UserNotificationPreferences>> GetUserPreferencesAsync(
        List<Guid> userIds,
        CancellationToken cancellationToken)
    {
        List<UserNotificationPreferences> preferences = await _context.UserNotificationPreferences
            .Where(p => userIds.Contains(p.UserId))
            .ToListAsync(cancellationToken);

        return preferences.ToDictionary(p => p.UserId);
    }

    private async Task<Dictionary<Guid, UserNotificationTypeSetting>> GetTypeSettingsAsync(
        List<Guid> userIds,
        Guid typeId,
        CancellationToken cancellationToken)
    {
        List<UserNotificationTypeSetting> settings = await _context.UserNotificationTypeSettings
            .Where(s => userIds.Contains(s.UserId) && s.TypeId == typeId)
            .ToListAsync(cancellationToken);

        return settings.ToDictionary(s => s.UserId);
    }
}
