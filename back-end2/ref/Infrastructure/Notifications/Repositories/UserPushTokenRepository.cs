using Application.Abstractions.Repositories;
using Domain.Notifications;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Notifications.Repositories;

internal sealed class UserPushTokenRepository : IUserPushTokenRepository
{
    private readonly ApplicationDbContext _context;

    public UserPushTokenRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserPushToken?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.UserPushTokens
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<UserPushToken?> GetByTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        return await _context.UserPushTokens
            .FirstOrDefaultAsync(t => t.Token == token, cancellationToken);
    }

    public async Task<List<UserPushToken>> GetActiveByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.UserPushTokens
            .Where(t => t.UserId == userId && t.IsActive)
            .ToListAsync(cancellationToken);
    }

    public void Add(UserPushToken token)
    {
        _context.UserPushTokens.Add(token);
    }

    public void Remove(UserPushToken token)
    {
        _context.UserPushTokens.Remove(token);
    }
}
