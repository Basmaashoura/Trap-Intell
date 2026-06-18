using Application.Abstractions.Data;
using Application.Abstractions.Identity;
using Application.Abstractions.Messaging;
using Application.Users.Common;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Users.GetCurrentUserSessions;

/// <summary>
/// Handler for GetCurrentUserSessionsQuery.
/// Returns all active sessions for the current user.
/// </summary>
internal sealed class GetCurrentUserSessionsQueryHandler : IQueryHandler<GetCurrentUserSessionsQuery, IReadOnlyList<UserSessionResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetCurrentUserSessionsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<UserSessionResponse>>> Handle(
        GetCurrentUserSessionsQuery query,
        CancellationToken cancellationToken)
    {
        List<UserSessionResponse> sessions = await _context.UserSessions
            .AsNoTracking()
            .Where(s => s.UserId == _currentUser.UserId && s.IsActive)
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
