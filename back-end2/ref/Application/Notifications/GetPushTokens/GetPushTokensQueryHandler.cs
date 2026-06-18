using Application.Abstractions.Identity;
using Application.Abstractions.Messaging;
using Application.Abstractions.Repositories;
using Domain.Notifications;
using SharedKernel;

namespace Application.Notifications.GetPushTokens;

internal sealed class GetPushTokensQueryHandler(
    IUserPushTokenRepository pushTokenRepository,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetPushTokensQuery, List<PushTokenResponse>>
{
    public async Task<Result<List<PushTokenResponse>>> Handle(
        GetPushTokensQuery request,
        CancellationToken cancellationToken)
    {
        Guid userId = currentUserService.UserId;

        List<UserPushToken> tokens = await pushTokenRepository.GetActiveByUserIdAsync(
            userId,
            cancellationToken);

        var response = tokens.Select(t => new PushTokenResponse(
            t.Id,
            MaskToken(t.Token),
            t.Platform.ToString(),
            t.DeviceName,
            t.IsActive,
            t.CreatedAt,
            t.LastUsedAt)).ToList();

        return Result.Success(response);
    }

    private static string MaskToken(string token)
    {
        if (token.Length <= 8)
        {
            return new string('*', token.Length);
        }

        return string.Concat(token.AsSpan(0, 4), "****", token.AsSpan(token.Length - 4));
    }
}
