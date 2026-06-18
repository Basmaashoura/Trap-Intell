using Application.Abstractions.Messaging;

namespace Application.Notifications.ArchiveAll;

public sealed record ArchiveAllCommand : ICommand<int>;
