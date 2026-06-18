using MediatR;
using Trap_Intel.Application.Abstractions.RealTime;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Invitations;

namespace Trap_Intel.Application.Organizations.Commands.RevokeInvitation;

internal sealed class RevokeInvitationCommandHandler : IRequestHandler<RevokeInvitationCommand, Result>
{
    private readonly IOrganizationInvitationRepository _invitationRepository;
    private readonly IListRealtimeNotifier _listRealtimeNotifier;
    private readonly IUnitOfWork _unitOfWork;

    public RevokeInvitationCommandHandler(
        IOrganizationInvitationRepository invitationRepository,
        IListRealtimeNotifier listRealtimeNotifier,
        IUnitOfWork unitOfWork)
    {
        _invitationRepository = invitationRepository;
        _listRealtimeNotifier = listRealtimeNotifier;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RevokeInvitationCommand request, CancellationToken cancellationToken)
    {
        var invitation = await _invitationRepository.GetByIdAsync(request.InvitationId, cancellationToken);

        if (invitation is null || invitation.OrganizationId != request.OrganizationId)
        {
            return Result.Failure(InvitationErrors.NotFoundById(request.InvitationId));
        }

        var revokeResult = invitation.Revoke(request.RevokedByUserId, request.Reason);

        if (revokeResult.IsFailure)
        {
            return revokeResult;
        }

        await _invitationRepository.UpdateAsync(invitation, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var payload = new { invitationId = invitation.Id };
        await _listRealtimeNotifier.NotifyOrganizationListChangedAsync(
            "invitations",
            request.OrganizationId,
            action: "updated",
            payload: payload,
            cancellationToken: cancellationToken);

        return Result.Success();
    }
}
