using Application.Abstractions.Data;
using Application.Abstractions.Identity;
using Application.Abstractions.Messaging;
using Application.Abstractions.Repositories;
using Domain.Notifications;
using SharedKernel;

namespace Application.Notifications.RegisterPushToken;

internal sealed class RegisterPushTokenCommandHandler(
    IUserPushTokenRepository pushTokenRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService)
    : ICommandHandler<RegisterPushTokenCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        RegisterPushTokenCommand request,
        CancellationToken cancellationToken)
    {
        Guid userId = currentUserService.UserId;

        // Parse platform
        if (!Enum.TryParse<PushPlatform>(request.Platform, true, out PushPlatform platform))
        {
            return Result.Failure<Guid>(NotificationErrors.InvalidChannel);
        }

        // Check if token already exists
        UserPushToken? existingToken = await pushTokenRepository.GetByTokenAsync(
            request.Token,
            cancellationToken);

        if (existingToken is not null)
        {
            if (existingToken.UserId == userId)
            {
                // Same user, just update
                existingToken.Activate();
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return Result.Success(existingToken.Id);
            }
            else
            {
                // Different user - deactivate old token
                existingToken.Deactivate();
            }
        }

        // Create new token
        var token = UserPushToken.Create(
            userId,
            request.Token,
            platform,
            request.DeviceName);

        pushTokenRepository.Add(token);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(token.Id);
    }
}
