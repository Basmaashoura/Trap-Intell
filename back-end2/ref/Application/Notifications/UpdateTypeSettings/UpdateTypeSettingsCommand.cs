using Application.Abstractions.Messaging;
using SharedKernel;

namespace Application.Notifications.UpdateTypeSettings;

/// <summary>
/// Command to update settings for a specific notification type.
/// </summary>
public sealed record UpdateTypeSettingsCommand(
Guid TypeId,
bool IsEnabled,
List<string>? Channels) : ICommand;
