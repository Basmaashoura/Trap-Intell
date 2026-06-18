using Application.Abstractions.Data;
using Application.Abstractions.Identity;
using Application.Abstractions.Messaging;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Users.RevokeAllSessions;

/// <summary>
/// Handler for RevokeAllSessionsCommand.
/// Revokes all sessions for a user in both Domain and Identity layers.
/// </summary>
internal sealed class RevokeAllSessionsCommandHandler : ICommandHandler<RevokeAllSessionsCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;

    public RevokeAllSessionsCommandHandler(
        IApplicationDbContext context,
        IIdentityService identityService)
    {
        _context = context;
        _identityService = identityService;
    }

    public async Task<Result> Handle(RevokeAllSessionsCommand command, CancellationToken cancellationToken)
    {
        // Check if user exists
        bool userExists = await _context.Users
            .AnyAsync(u => u.Id == command.UserId, cancellationToken);

        if (!userExists)
        {
            return Result.Failure(UserErrors.NotFound(command.UserId));
        }

        // Revoke all sessions in Domain layer
        List<UserSession> sessions = await _context.UserSessions
            .Where(s => s.UserId == command.UserId && s.IsActive)
            .ToListAsync(cancellationToken);

        foreach (UserSession session in sessions)
        {
            session.Revoke("All sessions revoked");
        }

        // Revoke all tokens in Identity layer (invalidates all JWTs)
        Result identityResult = await _identityService.RevokeAllTokensAsync(
            command.UserId,
            cancellationToken);

        if (identityResult.IsFailure)
        {
            return identityResult;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
