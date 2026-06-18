using Application.Abstractions.Data;
using Application.Abstractions.Identity;
using Application.Abstractions.Messaging;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Users.RevokeSession;

/// <summary>
/// Handler for RevokeSessionCommand.
/// Revokes a specific user session in both Domain and Identity layers.
/// </summary>
internal sealed class RevokeSessionCommandHandler : ICommandHandler<RevokeSessionCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;

    public RevokeSessionCommandHandler(
        IApplicationDbContext context,
        IIdentityService identityService)
    {
        _context = context;
        _identityService = identityService;
    }

    public async Task<Result> Handle(RevokeSessionCommand command, CancellationToken cancellationToken)
    {
        UserSession? session = await _context.UserSessions
            .FirstOrDefaultAsync(s => s.Id == command.SessionId && s.UserId == command.UserId, cancellationToken);

        if (session is null)
        {
            return Result.Failure(UserErrors.SessionNotFound(command.SessionId));
        }

        if (!session.IsActive)
        {
            return Result.Success(); // Already revoked
        }

        // Revoke in Domain layer (raises UserSessionRevokedDomainEvent)
        session.Revoke("Session revoked by administrator");

        // Revoke refresh token in Identity layer using token hash
        if (!string.IsNullOrEmpty(session.RefreshTokenHash))
        {
            await _identityService.RevokeRefreshTokenAsync(
                command.UserId,
                session.RefreshTokenHash,
                cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
