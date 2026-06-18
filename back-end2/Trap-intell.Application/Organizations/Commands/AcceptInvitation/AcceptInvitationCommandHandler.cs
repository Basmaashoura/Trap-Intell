using MediatR;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using Trap_Intel.Application.Abstractions.Identity;
using Trap_Intel.Application.Abstractions.Notifications;
using Trap_Intel.Application.Abstractions.RealTime;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Identity;
using Trap_Intel.Domain.Invitations;
using Trap_Intel.Domain.Notifications;
using Trap_Intel.Domain.Notifications.Enums;
using Trap_Intel.Domain.Organizations;
using Trap_Intel.Domain.Roles;

namespace Trap_Intel.Application.Organizations.Commands.AcceptInvitation;

internal sealed class AcceptInvitationCommandHandler : IRequestHandler<AcceptInvitationCommand, Result>
{
    private readonly IOrganizationInvitationRepository _invitationRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IOrganizationEmailService _organizationEmailService;
    private readonly INotificationDispatcher _notificationDispatcher;
    private readonly IListRealtimeNotifier _listRealtimeNotifier;
    private readonly ILogger<AcceptInvitationCommandHandler> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public AcceptInvitationCommandHandler(
        IOrganizationInvitationRepository invitationRepository,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IOrganizationRepository organizationRepository,
        IOrganizationEmailService organizationEmailService,
        INotificationDispatcher notificationDispatcher,
        IListRealtimeNotifier listRealtimeNotifier,
        ILogger<AcceptInvitationCommandHandler> logger,
        IUnitOfWork unitOfWork)
    {
        _invitationRepository = invitationRepository;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _organizationRepository = organizationRepository;
        _organizationEmailService = organizationEmailService;
        _notificationDispatcher = notificationDispatcher;
        _listRealtimeNotifier = listRealtimeNotifier;
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(AcceptInvitationCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RawToken))
        {
            return Result.Failure(InvitationErrors.InvalidToken);
        }

        // 1. Recreate the hash from the raw token to lookup the invitation
        var tokenHash = ComputeTokenHash(request.RawToken);

        // 2. Fetch the invitation
        var invitation = await _invitationRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (invitation == null)
        {
            return Result.Failure(InvitationErrors.InvalidToken);
        }

        // 3. Accept flow must be performed by the authenticated user only.
        var user = await _userRepository.GetByIdAsync(request.AcceptingUserId, cancellationToken);
        if (user == null)
        {
            return Result.Failure(Error.Custom("Organization.UserNotFound", "Authenticated user account was not found."));
        }

        if (!string.Equals(user.Email.Value, invitation.Email, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure(Error.Custom(
                "Invitation.EmailMismatch",
                "You can only accept invitations sent to your account email."));
        }

        var role = await _roleRepository.GetByIdAsync(invitation.RoleId, cancellationToken);
        if (role is null)
        {
            return Result.Failure(Error.Custom("Invitation.InvalidRole", "The invitation role no longer exists."));
        }

        if (!role.IsSystemRole && role.OrganizationId != invitation.OrganizationId)
        {
            return Result.Failure(Error.Custom("Invitation.InvalidRoleScope", "The invitation role scope is no longer valid for this organization."));
        }

        var organization = await _organizationRepository.GetByIdAsync(invitation.OrganizationId, cancellationToken);
        if (organization is null)
        {
            return Result.Failure(Error.Custom("Invitation.InvalidOrganization", "The target organization no longer exists."));
        }

        // 4. Accept the invitation
        var acceptResult = invitation.Accept(user.Id);

        if (acceptResult.IsFailure)
        {
            return acceptResult;
        }

        // 5. Apply the Organization and Role from the Invitation to the User
        // If your business handles multi-tenancy differently, adapt this.
        // Currently, joining sets the org and role on the user.
        var joinResult = user.JoinOrganization(invitation.OrganizationId, invitation.RoleId);
        if (joinResult.IsFailure)
        {
            return joinResult;
        }

        // 6. Persist changes
        await _invitationRepository.UpdateAsync(invitation, cancellationToken);
        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var invitationPayload = new { invitationId = invitation.Id };
        await _listRealtimeNotifier.NotifyOrganizationListChangedAsync(
            "invitations",
            invitation.OrganizationId,
            action: "updated",
            payload: invitationPayload,
            cancellationToken: cancellationToken);

        var userPayload = new { userId = user.Id };
        await _listRealtimeNotifier.NotifyOrganizationListChangedAsync(
            "users",
            invitation.OrganizationId,
            action: "updated",
            payload: userPayload,
            cancellationToken: cancellationToken);

        await _organizationEmailService.SendOrganizationWelcomeAsync(
            new OrganizationWelcomeEmailMessage(
                user.Email.Value,
                ResolveDisplayName(user.FullName, user.Email.Value),
                organization.Name,
                role.Name),
            cancellationToken);

        await DispatchAcceptanceNotificationsAsync(
            invitation,
            user.Id,
            organization.Name,
            role.Name,
            cancellationToken);

        return Result.Success();
    }

    private async Task DispatchAcceptanceNotificationsAsync(
        OrganizationInvitation invitation,
        Guid acceptingUserId,
        string organizationName,
        string roleName,
        CancellationToken cancellationToken)
    {
        var acceptedUserNotification = Notification.Create(
            userId: acceptingUserId,
            type: "OrganizationInvitationAccepted",
            title: "Welcome to your organization",
            message: $"You joined {organizationName} as {roleName}.",
            category: NotificationCategory.Team,
            priority: NotificationPriority.Normal,
            linkUri: $"/organizations/{invitation.OrganizationId}",
            relatedEntityId: invitation.Id.ToString());

        if (acceptedUserNotification.IsSuccess)
        {
            await TryDispatchNotificationAsync(acceptedUserNotification.Value, invitation.Id, cancellationToken);
        }

        if (invitation.InvitedByUserId == Guid.Empty || invitation.InvitedByUserId == acceptingUserId)
        {
            return;
        }

        var inviterNotification = Notification.Create(
            userId: invitation.InvitedByUserId,
            type: "OrganizationMemberJoined",
            title: "Invitation accepted",
            message: $"{MaskEmail(invitation.Email)} joined {organizationName} as {roleName}.",
            category: NotificationCategory.Team,
            priority: NotificationPriority.Normal,
            linkUri: $"/organizations/{invitation.OrganizationId}/invitations",
            relatedEntityId: invitation.Id.ToString());

        if (inviterNotification.IsSuccess)
        {
            await TryDispatchNotificationAsync(inviterNotification.Value, invitation.Id, cancellationToken);
        }
    }

    private async Task TryDispatchNotificationAsync(
        Notification notification,
        Guid invitationId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _notificationDispatcher.DispatchAsync(notification, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to dispatch acceptance notification for invitation {InvitationId}.",
                invitationId);
        }
    }

    private static string ResolveDisplayName(string? fullName, string email)
    {
        if (!string.IsNullOrWhiteSpace(fullName))
        {
            var trimmed = fullName.Trim();
            return trimmed.Length <= 120 ? trimmed : trimmed[..120];
        }

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

    private static string ComputeTokenHash(string rawToken)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(rawToken);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
