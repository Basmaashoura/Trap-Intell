using MediatR;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using Trap_Intel.Domain.Identity;

namespace Trap_Intel.Application.Identity.Events;

internal sealed class UserSuspendedDomainEventHandler : INotificationHandler<UserSuspendedEvent>
{
    private readonly Trap_Intel.Domain.Identity.IRefreshTokenRepository _refreshTokenRepository;
    private readonly ILogger<UserSuspendedDomainEventHandler> _logger;

    public UserSuspendedDomainEventHandler(
        Trap_Intel.Domain.Identity.IRefreshTokenRepository refreshTokenRepository,
        ILogger<UserSuspendedDomainEventHandler> logger)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _logger = logger;
    }

    public async Task Handle(UserSuspendedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Domain event caught for Suspended User {UserId}. Revoking all active sessions...", notification.UserId);

        var count = await _refreshTokenRepository.RevokeAllForUserAsync(
            notification.UserId, 
            "Account suspended administratively", 
            cancellationToken);

        _logger.LogInformation("Successfully revoked {Count} sessions for user {UserId} following suspension.", count, notification.UserId);
    }
}
