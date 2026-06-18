using Trap_Intel.Application.Subscriptions.Commands.ManageSubscriptionLifecycle;

namespace Trap_Intel.Tests.Subscriptions;

public class ManageSubscriptionLifecycleCommandValidatorTests
{
    [Fact]
    public void Validate_WhenActionIsCancelAndReasonMissing_ReturnsValid()
    {
        var validator = new ManageSubscriptionLifecycleCommandValidator();

        var command = new ManageSubscriptionLifecycleCommand(
            OrganizationId: Guid.NewGuid(),
            SubscriptionId: Guid.NewGuid(),
            Action: SubscriptionLifecycleAction.Cancel,
            Reason: null);

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenActionIsScheduleCancellationAndReasonMissing_ReturnsInvalid()
    {
        var validator = new ManageSubscriptionLifecycleCommandValidator();

        var command = new ManageSubscriptionLifecycleCommand(
            OrganizationId: Guid.NewGuid(),
            SubscriptionId: Guid.NewGuid(),
            Action: SubscriptionLifecycleAction.ScheduleCancellation,
            Reason: null);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            error => error.ErrorMessage == "Reason is required when scheduling subscription cancellation.");
    }
}
