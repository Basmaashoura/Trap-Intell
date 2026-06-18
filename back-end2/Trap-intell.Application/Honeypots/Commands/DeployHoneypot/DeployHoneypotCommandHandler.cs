using MediatR;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Honeypots;

namespace Trap_Intel.Application.Honeypots.Commands.DeployHoneypot;

internal sealed class DeployHoneypotCommandHandler : IRequestHandler<DeployHoneypotCommand, Result>
{
    private readonly IHoneypotRepository _honeypotRepository;
    private readonly IUnitOfWork _unitOfWork;

    // NOTE: Ideally, an IExternalHoneypotService should be injected here 
    // to actually dispatch the deployment via Terraform / Kubernetes / Docker.

    public DeployHoneypotCommandHandler(
        IHoneypotRepository honeypotRepository,
        IUnitOfWork unitOfWork)
    {
        _honeypotRepository = honeypotRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeployHoneypotCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate quota, validate active subscriptions (Handled by business rules in domain or here)

        // 2. Resolve or parse Configuration (assuming port and JSON representation stored in Base64 for now)
        // Note: For real scenario, we might use JsonSerializer to parse the template into options
        // Just simulating the Domain Factory usage for standard setup:
        var configResult = HoneypotConfiguration.Create(
            port: request.Type == HoneypotType.SSH ? 22 : 80, 
            captureLevel: LogCaptureLevel.Verbose);

        if (configResult.IsFailure)
        {
            return configResult;
        }

        // 3. Create the Honeypot Domain Aggregate
        var createResult = Honeypot.Create(
            request.OrganizationId,
            request.SubscriptionId,
            request.Name,
            request.Type,
            configResult.Value,
            request.Location
        );

        if (createResult.IsFailure)
        {
            return createResult;
        }

        var honeypot = createResult.Value;

        // 4. Mark state transit (Normally done via IExternalHoneypotService callbacks later, 
        //    but we simulate deployment initiation here for the blueprint).
        honeypot.MarkAsDeployed();

        // 5. Save
        await _honeypotRepository.AddAsync(honeypot, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
