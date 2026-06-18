using SharedKernel;

namespace Domain.Subscriptions;

public static class SubscriptionErrors
{
    public static Error PlanNotFound(Guid planId) => Error.NotFound(
        "Subscriptions.PlanNotFound",
        $"The plan with Id = '{planId}' was not found");

    public static Error SubscriptionNotFound(Guid subscriptionId) => Error.NotFound(
        "Subscriptions.SubscriptionNotFound",
        $"The subscription with Id = '{subscriptionId}' was not found");

    public static Error InvoiceNotFound(Guid invoiceId) => Error.NotFound(
        "Subscriptions.InvoiceNotFound",
        $"The invoice with Id = '{invoiceId}' was not found");

    public static readonly Error AlreadySubscribed = Error.Conflict(
        "Subscriptions.AlreadySubscribed",
        "This account already has an active subscription");

    public static readonly Error PlanInactive = Error.Validation(
        "Subscriptions.PlanInactive",
        "This plan is no longer available");

    public static readonly Error AlreadyCancelled = Error.Validation(
        "Subscriptions.AlreadyCancelled",
        "This subscription is already cancelled");

    public static readonly Error InvoiceAlreadyPaid = Error.Validation(
        "Subscriptions.InvoiceAlreadyPaid",
        "This invoice has already been paid");
}
