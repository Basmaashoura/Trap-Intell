using Application.Abstractions.Messaging;

namespace Application.Notifications.GetPushTokens;

public sealed record GetPushTokensQuery : IQuery<List<PushTokenResponse>>;

public sealed record PushTokenResponse(
    Guid Id,
    string Token,
    string Platform,
    string? DeviceName,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? LastUsedAt);
