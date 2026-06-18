using Trap_Intel.Domain.Webhooks.Enums;

namespace Trap_Intel.Domain.Webhooks;

/// <summary>
/// Repository interface for Webhook aggregate.
/// </summary>
public interface IWebhookRepository
{
    /// <summary>
    /// Get webhook by ID.
    /// </summary>
    Task<Webhook?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all webhooks for an organization.
    /// </summary>
    Task<IReadOnlyList<Webhook>> GetByOrganizationAsync(
        Guid organizationId,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get active webhooks for an organization.
    /// </summary>
    Task<IReadOnlyList<Webhook>> GetActiveByOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get webhooks subscribed to specific event type for an organization.
    /// </summary>
    Task<IReadOnlyList<Webhook>> GetByEventTypeAsync(
        Guid organizationId,
        WebhookEventType eventType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all deliverable webhooks for an event type across all organizations.
    /// Used by webhook delivery service.
    /// </summary>
    Task<IReadOnlyList<Webhook>> GetDeliverableByEventTypeAsync(
        WebhookEventType eventType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get webhooks that were auto-disabled and need attention.
    /// </summary>
    Task<IReadOnlyList<Webhook>> GetDisabledByFailuresAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Count webhooks for an organization.
    /// </summary>
    Task<int> CountByOrganizationAsync(
        Guid organizationId,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if webhook URL exists in organization.
    /// </summary>
    Task<bool> ExistsByUrlAsync(
        Guid organizationId,
        string url,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Add new webhook.
    /// </summary>
    Task AddAsync(Webhook webhook, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update existing webhook.
    /// </summary>
    Task UpdateAsync(Webhook webhook, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete webhook (hard delete).
    /// </summary>
    Task DeleteAsync(Webhook webhook, CancellationToken cancellationToken = default);
}
