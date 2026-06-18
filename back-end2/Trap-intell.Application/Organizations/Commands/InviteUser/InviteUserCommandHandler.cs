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

namespace Trap_Intel.Application.Organizations.Commands.InviteUser;

internal sealed class InviteUserCommandHandler : IRequestHandler<InviteUserCommand, Result<string>>
{
    private readonly IOrganizationInvitationRepository _invitationRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IOrganizationEmailService _organizationEmailService;
    private readonly INotificationDispatcher _notificationDispatcher;
    private readonly IListRealtimeNotifier _listRealtimeNotifier;
    private readonly ILogger<InviteUserCommandHandler> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public InviteUserCommandHandler(
        IOrganizationInvitationRepository invitationRepository,
        IOrganizationRepository organizationRepository,
        IRoleRepository roleRepository,
        IOrganizationEmailService organizationEmailService,
        INotificationDispatcher notificationDispatcher,
        IListRealtimeNotifier listRealtimeNotifier,
        ILogger<InviteUserCommandHandler> logger,
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

    public async Task<Result<string>> Handle(InviteUserCommand request, CancellationToken cancellationToken)
    {
        // 1. Check if user is already pending an invitation for this organization
        var existingPending = await _invitationRepository.ExistsPendingByEmailAsync(
            request.OrganizationId, 
            request.Email, 
            cancellationToken);

        if (existingPending)
        {
            return Result.Failure<string>(InvitationErrors.PendingInvitationExists);
        }

        var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role is null)
        {
            return Result.Failure<string>(Error.Custom("Invitation.InvalidRole", "The requested role does not exist."));
        }

        if (!role.IsSystemRole && role.OrganizationId != request.OrganizationId)
        {
            return Result.Failure<string>(Error.Custom("Invitation.InvalidRoleScope", "The requested role does not belong to this organization."));
        }

        var organization = await _organizationRepository.GetByIdAsync(request.OrganizationId, cancellationToken);
        if (organization is null)
        {
            return Result.Failure<string>(Error.Custom("Invitation.InvalidOrganization", "The target organization does not exist."));
        }

        // 2. Create the Invitation logic via Domain aggregate factory
        // This will generate a cryptographically valid Token Hash internally and return the Raw Token.
        var creationResult = OrganizationInvitation.Create(
            request.OrganizationId,
            request.Email,
            request.RoleId,
            request.InvitedByUserId,
            request.PersonalMessage,
            request.ExpirationDays);

        if (creationResult.IsFailure)
        {
            return Result.Failure<string>(creationResult.Errors);
        }

        var (invitation, rawToken) = creationResult.Value;

        // 3. Persist 
        await _invitationRepository.AddAsync(invitation, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var payload = new { invitationId = invitation.Id };
        await _listRealtimeNotifier.NotifyOrganizationListChangedAsync(
            "invitations",
            request.OrganizationId,
            action: "created",
            payload: payload,
            cancellationToken: cancellationToken);

        await _organizationEmailService.SendOrganizationInvitationAsync(
            new OrganizationInvitationEmailMessage(
                request.Email,
                BuildDisplayNameFromEmail(request.Email),
                organization.Name,
                role.Name,
                rawToken,
                request.PersonalMessage,
                invitation.ExpiresAt),
            cancellationToken);

        await DispatchInvitationSentNotificationAsync(
            request,
            invitation,
            organization.Name,
            role.Name,
            cancellationToken);

        // Return raw token mapping (this allows callers or background jobs to embed the token into an email)
        return Result.Success(rawToken);
    }

    private async Task DispatchInvitationSentNotificationAsync(
        InviteUserCommand request,
        OrganizationInvitation invitation,
        string organizationName,
        string roleName,
        CancellationToken cancellationToken)
    {
        if (request.InvitedByUserId == Guid.Empty)
        {
            return;
        }

        var notificationResult = Notification.Create(
            userId: request.InvitedByUserId,
            type: "OrganizationInvitationSent",
            title: "Organization invitation sent",
            message: $"Invitation sent to {MaskEmail(invitation.Email)} for role {roleName} in {organizationName}.",
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
                "Failed to dispatch invitation-sent notification for invitation {InvitationId}.",
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
