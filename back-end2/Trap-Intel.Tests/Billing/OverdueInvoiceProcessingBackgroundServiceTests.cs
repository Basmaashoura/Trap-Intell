using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Trap_Intel.Application.Billing.Commands.ProcessOverdueInvoices;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Infrastructure.Billing.BackgroundServices;

namespace Trap_Intel.Tests.Billing;

public class OverdueInvoiceProcessingBackgroundServiceTests
{
    [Fact]
    public async Task StartAsync_WhenRunOnStartupIsEnabled_ExecutesProcessingImmediately()
    {
        var sender = new Mock<ISender>();
        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        sender
            .Setup(x => x.Send(It.IsAny<ProcessOverdueInvoicesCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new OverdueInvoiceProcessingResultDto(
                ProcessedInvoices: 0,
                MarkedOverdueInvoices: 0,
                LateFeeAppliedInvoices: 0,
                FailedInvoices: 0,
                Errors: Array.Empty<string>())))
            .Callback(() => completion.TrySetResult(true));

        var service = CreateService(sender.Object, runOnStartup: true);

        await service.StartAsync(CancellationToken.None);
        await completion.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await service.StopAsync(CancellationToken.None);

        sender.Verify(
            x => x.Send(It.IsAny<ProcessOverdueInvoicesCommand>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task StartAsync_WhenRunOnStartupIsDisabled_DoesNotExecuteImmediateProcessing()
    {
        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<ProcessOverdueInvoicesCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new OverdueInvoiceProcessingResultDto(
                ProcessedInvoices: 0,
                MarkedOverdueInvoices: 0,
                LateFeeAppliedInvoices: 0,
                FailedInvoices: 0,
                Errors: Array.Empty<string>())));

        var service = CreateService(sender.Object, runOnStartup: false);

        await service.StartAsync(CancellationToken.None);
        await Task.Delay(150);
        await service.StopAsync(CancellationToken.None);

        sender.Verify(
            x => x.Send(It.IsAny<ProcessOverdueInvoicesCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static OverdueInvoiceProcessingBackgroundService CreateService(ISender sender, bool runOnStartup)
    {
        var scopedProvider = new Mock<IServiceProvider>();
        scopedProvider
            .Setup(provider => provider.GetService(typeof(ISender)))
            .Returns(sender);

        var scope = new Mock<IServiceScope>();
        scope
            .SetupGet(s => s.ServiceProvider)
            .Returns(scopedProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory
            .Setup(factory => factory.CreateScope())
            .Returns(scope.Object);

        var rootProvider = new Mock<IServiceProvider>();
        rootProvider
            .Setup(provider => provider.GetService(typeof(IServiceScopeFactory)))
            .Returns(scopeFactory.Object);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Billing:OverdueProcessing:RunOnStartup"] = runOnStartup.ToString()
            })
            .Build();

        return new OverdueInvoiceProcessingBackgroundService(
            rootProvider.Object,
            NullLogger<OverdueInvoiceProcessingBackgroundService>.Instance,
            configuration);
    }
}
