using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Trap_Intel.Application.Abstractions.Notifications;
using Trap_Intel.Application.Plans.Configuration;
using Trap_Intel.Domain.Identity;
using Trap_Intel.Domain.Notifications;
using Trap_Intel.Domain.Notifications.Enums;
using Trap_Intel.Domain.Organizations;
using Trap_Intel.Domain.Plans;
using Trap_Intel.Domain.Roles;

namespace Trap_Intel.Application.Plans.Events;

internal sealed class PlanLifecycleDomainEventNotificationHandler :
    INotificationHandler<PlanCreatedEvent>,
    INotificationHandler<PlanActivatedEvent>,
    INotificationHandler<PlanDeactivatedEvent>
{
    private readonly IPlanRepository _planRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationDispatcher _notificationDispatcher;
    private readonly ILogger<PlanLifecycleDomainEventNotificationHandler> _logger;
    private readonly PlanLifecycleNotificationOptions _options;

    public PlanLifecycleDomainEventNotificationHandler(
        IPlanRepository planRepository,
        IOrganizationRepository organizationRepository,
        IUserRepository userRepository,
        INotificationDispatcher notificationDispatcher,
        ILogger<PlanLifecycleDomainEventNotificationHandler> logger,
        IOptions<PlanLifecycleNotificationOptions> options)
    {
        _planRepository = planRepository;
        _organizationRepository = organizationRepository;
        _userRepository = userRepository;
        _notificationDispatcher = notificationDispatcher;
        _logger = logger;
        _options = options.Value;
    }

    public async Task Handle(PlanCreatedEvent notification, CancellationToken cancellationToken)
    {
        await DispatchToPlanAdminsAsync(
            type: "PlanCreated",
            title: $"New plan created: {notification.Name}",
            message: $"Plan '{notification.Name}' ({notification.Type}) was created and is now available for lifecycle management.",
            priority: NotificationPriority.Normal,
            planId: notification.PlanId,
            cancellationToken: cancellationToken);
    }

    public async Task Handle(PlanActivatedEvent notification, CancellationToken cancellationToken)
    {
        var planName = await ResolvePlanNameAsync(notification.PlanId, cancellationToken);

        await DispatchToPlanAdminsAsync(
            type: "PlanActivated",
            title: $"Plan activated: {planName}",
            message: $"Plan '{planName}' was activated and can now be used for subscription workflows.",
            priority: NotificationPriority.Normal,
            planId: notification.PlanId,
            cancellationToken: cancellationToken);
    }

    public async Task Handle(PlanDeactivatedEvent notification, CancellationToken cancellationToken)
    {
        var planName = await ResolvePlanNameAsync(notification.PlanId, cancellationToken);

        await DispatchToPlanAdminsAsync(
            type: "PlanDeactivated",
            title: $"Plan deactivated: {planName}",
            message: $"Plan '{planName}' was deactivated and can no longer be used for new subscription changes.",
            priority: NotificationPriority.High,
            planId: notification.PlanId,
            cancellationToken: cancellationToken);
    }

    private async Task<string> ResolvePlanNameAsync(Guid planId, CancellationToken cancellationToken)
    {
        var plan = await _planRepository.GetByIdAsync(planId, cancellationToken);
        return plan?.Name ?? $"Plan {planId}";
    }

    private async Task DispatchToPlanAdminsAsync(
        string type,
        string title,
        string message,
        NotificationPriority priority,
        Guid planId,
        CancellationToken cancellationToken)
    {
        var recipientIds = await ResolvePlanLifecycleRecipientsAsync(cancellationToken);
        if (recipientIds.Count == 0)
        {
            _logger.LogInformation(
                "No plan lifecycle recipients resolved for notification. Type={Type}, PlanId={PlanId}",
                type,
                planId);
            return;
        }

        foreach (var recipientId in recipientIds)
        {
            var notificationResult = Notification.Create(
                userId: recipientId,
                type: type,
                title: title,
                message: message,
                category: NotificationCategory.System,
                priority: priority,
                linkUri: $"/plans/{planId}",
                relatedEntityId: planId.ToString());

            if (notificationResult.IsFailure)
            {
                _logger.LogWarning(
                    "Failed to build plan lifecycle notification for user {UserId}. Type={Type}",
                    recipientId,
                    type);
                continue;
            }

            try
            {
                await _notificationDispatcher.DispatchAsync(notificationResult.Value, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to dispatch plan lifecycle notification for user {UserId}. Type={Type}",
                    recipientId,
                    type);
            }
        }
    }

    private async Task<IReadOnlyCollection<Guid>> ResolvePlanLifecycleRecipientsAsync(CancellationToken cancellationToken)
    {
        var organizations = await _organizationRepository.GetAllAsync(cancellationToken);
        var recipientIds = new HashSet<Guid>();

        foreach (var organization in organizations)
        {
            var superAdmins = await _userRepository.GetByRoleAsync(
                organization.Id,
                SystemRoles.SuperAdminId,
                cancellationToken);

            foreach (var superAdmin in superAdmins ?? Array.Empty<User>())
            {
                recipientIds.Add(superAdmin.Id);
            }

            if (!_options.IncludeOrganizationAdmins)
            {
                continue;
            }

            var organizationAdmins = await _userRepository.GetByRoleAsync(
                organization.Id,
                SystemRoles.OrganizationAdminId,
                cancellationToken);

            foreach (var organizationAdmin in organizationAdmins ?? Array.Empty<User>())
            {
                recipientIds.Add(organizationAdmin.Id);
            }
        }

        return recipientIds.ToList();
    }
}