namespace Trap_Intel.Api.Endpoints.Notifications.Models;

public sealed record RegisterPushTokenRequest(
    string Token,
    int Platform,
    string DeviceId
);
