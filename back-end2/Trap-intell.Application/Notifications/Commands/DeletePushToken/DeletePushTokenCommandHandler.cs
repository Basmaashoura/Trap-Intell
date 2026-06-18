using MediatR;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Notifications;

namespace Trap_Intel.Application.Notifications.Commands.DeletePushToken;

internal sealed class DeletePushTokenCommandHandler : IRequestHandler<DeletePushTokenCommand, Result>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeletePushTokenCommandHandler(INotificationRepository notificationRepository, IUnitOfWork unitOfWork)
    {
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeletePushTokenCommand request, CancellationToken cancellationToken)
    {
        var token = await _notificationRepository.GetPushTokenAsync(request.Token, cancellationToken);

        if (token == null || token.UserId != request.UserId)
        {
            return Result.Failure(NotificationErrors.TokenNotFound);
        }

        await _notificationRepository.DeletePushTokenAsync(token, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
