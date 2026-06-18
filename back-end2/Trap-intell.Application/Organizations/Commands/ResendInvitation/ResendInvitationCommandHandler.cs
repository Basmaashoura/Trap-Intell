using MediatR;
using Microsoft.Extensions.Logging;
using Trap_Intel.Application.Abstractions.Identity;
using Trap_Intel.Application.Abstractions.Notifications;
using Trap_Intel.Application.Abstractions.RealTime;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Invitations;
using Trap_Intel.Domain.Notifications;
using Trap_Intel.Domain.Notifications.Enums;
using Trap_Intel.Domain.Organizations;
using Trap_Intel.Domain.Roles;

namespace Trap_Intel.Application.Organizations.Commands.ResendInvitation;

internal sealed class ResendInvitationCommandHandler : IRequestHandler<ResendInvitationCommand, Result<string>>
{
    private readonly IOrganizationInvitationRepository _invitationRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IOrganizationEmailService _organizationEmailService;
    private readonly INotificationDispatcher _notificationDispatcher;
    private readonly IListRealtimeNotifier _listRealtimeNotifier;
    private readonly ILogger<ResendInvitationCommandHandler> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public ResendInvitationCommandHandler(
        IOrganizationInvitationRepository invitationRepository,
        IOrganizationRepository organizationRepository,
        IRoleRepository roleRepository,
        IOrganizationEmailService organizationEmailService,
        INotificationDispatcher notificationDispatcher,
        IListRealtimeNotifier listRealtimeNotifier,
        ILogger<ResendInvitationCommandHandler> logger,
        IUnitOfWork unitOfWork)
    {
        _invitationRepository = invitationRepository;
        _organizationRepository = organizationRepository;
        _roleRepository = roleRepository;
        _organizationEmailService = organizationEmailService;
        _notificationDispatcher = notificationDispatcher;
        _listRealtimeNotifier = listRealtimeNotifier;
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<string>> Handle(ResendInvitationCommand request, CancellationToken cancellationToken)
    {
        if (request.ExpirationDays < 1 || request.ExpirationDays > 30)
        {
            return Result.Failure<string>(InvitationErrors.InvalidExpirationDays);
        }

        var invitation = await _invitationRepository.GetByIdAsync(request.InvitationId, cancellationToken);

        if (invitation is null || invitation.OrganizationId != request.OrganizationId)
        {
            return Result.Failure<string>(InvitationErrors.NotFoundById(request.InvitationId));
        }

        var role = await _roleRepository.GetByIdAsync(invitation.RoleId, cancellationToken);
        if (role is null)
        {
            return Result.Failure<string>(Error.Custom("Invitation.InvalidRole", "The invitation role no longer exists."));
        }

        if (!role.IsSystemRole && role.OrganizationId != invitation.OrganizationId)
        {
            return Result.Failure<string>(Error.Custom("Invitation.InvalidRoleScope", "The invitation role scope is no longer valid for this organization."));
        }

        var organization = await _organizationRepository.GetByIdAsync(invitation.OrganizationId, cancellationToken);
        if (organization is null)
        {
            return Result.Failure<string>(Error.Custom("Invitation.InvalidOrganization", "The target organization no longer exists."));
        }

        var resendResult = invitation.Resend(request.RequestedByUserId, request.ExpirationDays);

        if (resendResult.IsFailure)
        {
            return Result.Failure<string>(resendResult.Errors);
        }

        await _invitationRepository.UpdateAsync(invitation, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var payload = new { invitationId = invitation.Id };
        await _listRealtimeNotifier.NotifyOrganizationListChangedAsync(
            "invitations",
            invitation.OrganizationId,
            action: "updated",
            payload: payload,
            cancellationToken: cancellationToken);

        await _organizationEmailService.SendOrganizationInvitationAsync(
            new OrganizationInvitationEmailMessage(
                invitation.Email,
                BuildDisplayNameFromEmail(invitation.Email),
                organization.Name,
                role.Name,
                resendResult.Value,
                invitation.PersonalMessage,
                invitation.ExpiresAt),
            cancellationToken);

        await DispatchInvitationResentNotificationAsync(
            request,
            invitation,
            organization.Name,
            role.Name,
            cancellationToken);

        return Result.Success(resendResult.Value);
    }

    private async Task DispatchInvitationResentNotificationAsync(
        ResendInvitationCommand request,
        OrganizationInvitation invitation,
        string organizationName,
        string roleName,
        CancellationToken cancellationToken)
    {
        if (request.RequestedByUserId == Guid.Empty)
        {
            return;
        }

        var notificationResult = Notification.Create(
            userId: request.RequestedByUserId,
            type: "OrganizationInvitationResent",
            title: "Invitation resent",
            message: $"Invitation resent to {MaskEmail(invitation.Email)} for role {roleName} in {organizationName}.",
            category: NotificationCategory.Team,
            priority: NotificationPriority.Normal,
            linkUri: $"/organizations/{request.OrganizationId}/invitations",
            relatedEntityId: invitation.Id.ToString());

        if (notificationResult.IsFailure)
        {
            return;
        }

        try
        {
            await _notificationDispatcher.DispatchAsync(notificationResult.Value, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to dispatch invitation-resent notification for invitation {InvitationId}.",
                invitation.Id);
        }
    }

    private static string BuildDisplayNameFromEmail(string email)
    {
        var atIndex = email.IndexOf('@');
        if (atIndex <= 0)
        {
            return "User";
        }

        var localPart = email[..atIndex].Trim();
        if (string.IsNullOrWhiteSpace(localPart))
        {
            return "User";
        }

        return localPart.Length <= 80 ? localPart : localPart[..80];
    }

    private static string MaskEmail(string email)
    {
        var atIndex = email.IndexOf('@');
        if (atIndex <= 1 || atIndex == email.Length - 1)
        {
            return "***";
        }

        var localPart = email[..atIndex];
        var domain = email[(atIndex + 1)..];
        var visible = localPart.Length <= 2 ? localPart[..1] : localPart[..2];

        return $"{visible}***@{domain}";
    }
}
