using MediatR;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Notifications;

namespace Trap_Intel.Application.Notifications.Commands.RegisterPushToken;

internal sealed class RegisterPushTokenCommandHandler : IRequestHandler<RegisterPushTokenCommand, Result>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterPushTokenCommandHandler(INotificationRepository notificationRepository, IUnitOfWork unitOfWork)
    {
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RegisterPushTokenCommand request, CancellationToken cancellationToken)
    {
        var existingToken = await _notificationRepository.GetPushTokenAsync(request.Token, cancellationToken);
        if (existingToken != null)
        {
            if (existingToken.UserId == request.UserId)
            {
                existingToken.UpdateLastUsed();
                await _notificationRepository.UpdatePushTokenAsync(existingToken, cancellationToken); // Update last interaction time
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return Result.Success();
            }

            // It exists for another user? Clear it and register.
            await _notificationRepository.DeletePushTokenAsync(existingToken, cancellationToken);
        }

        var newTokenResult = UserPushToken.Create(request.UserId, request.Token, request.Platform, request.DeviceId);
        if (newTokenResult.IsFailure)
        {
            return newTokenResult;
        }

        await _notificationRepository.AddPushTokenAsync(newTokenResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
