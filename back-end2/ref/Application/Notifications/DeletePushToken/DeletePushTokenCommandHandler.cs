using Application.Abstractions.Data;
using Application.Abstractions.Identity;
using Application.Abstractions.Messaging;
using Application.Abstractions.Repositories;
using Domain.Notifications;
using SharedKernel;

namespace Application.Notifications.DeletePushToken;

internal sealed class DeletePushTokenCommandHandler(
    IUserPushTokenRepository pushTokenRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService)
    : ICommandHandler<DeletePushTokenCommand>
{
    public async Task<Result> Handle(
        DeletePushTokenCommand request,
        CancellationToken cancellationToken)
    {
        Guid userId = currentUserService.UserId;

        UserPushToken? token = await pushTokenRepository.GetByIdAsync(
            request.TokenId,
            cancellationToken);

        if (token is null)
        {
            return Result.Failure(NotificationErrors.PushTokenNotFound(request.TokenId));
        }

        // Verify ownership
        if (token.UserId != userId)
        {
            return Result.Failure(NotificationErrors.PushTokenNotFound(request.TokenId));
        }

        pushTokenRepository.Remove(token);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
