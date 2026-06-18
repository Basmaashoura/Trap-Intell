using Application.Abstractions.Messaging;
using Application.Notifications.Common;
using SharedKernel;

namespace Application.Notifications.GetPreferences;

/// <summary>
/// Query to get current user's notification preferences.
/// </summary>
public sealed record GetPreferencesQuery : IQuery<NotificationPreferencesResponse>;
