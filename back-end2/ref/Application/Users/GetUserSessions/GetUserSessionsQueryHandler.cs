using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Users.Common;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Users.GetUserSessions;

/// <summary>
/// Handler for GetUserSessionsQuery.
/// Returns all active sessions for a user.
/// </summary>
internal sealed class GetUserSessionsQueryHandler : IQueryHandler<GetUserSessionsQuery, IReadOnlyList<UserSessionResponse>>
{
    private readonly IApplicationDbContext _context;

    public GetUserSessionsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<IReadOnlyList<UserSessionResponse>>> Handle(
        GetUserSessionsQuery query,
        CancellationToken cancellationToken)
    {
        // Check if user exists
        bool userExists = await _context.Users
            .AnyAsync(u => u.Id == query.UserId, cancellationToken);

        if (!userExists)
        {
            return Result.Failure<IReadOnlyList<UserSessionResponse>>(UserErrors.NotFound(query.UserId));
        }

        List<UserSessionResponse> sessions = await _context.UserSessions
            .AsNoTracking()
            .Where(s => s.UserId == query.UserId && s.IsActive)
            .OrderByDescending(s => s.LastActivityAt)
            .Select(s => new UserSessionResponse
            {
                Id = s.Id,
                CreatedAt = s.CreatedAt,
                ExpiresAt = s.ExpiresAt,
                LastActivityAt = s.LastActivityAt,
                IpAddress = s.IpAddress,
                UserAgent = s.UserAgent,
                DeviceId = s.DeviceId,
                IsActive = s.IsActive,
                IsCurrent = query.CurrentSessionToken != null && s.TokenHash == query.CurrentSessionToken
            })
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<UserSessionResponse>>(sessions);
    }
}
