using MediatR;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Honeypots;

namespace Trap_Intel.Application.Honeypots.Commands.TerminateHoneypot;

internal sealed class TerminateHoneypotCommandHandler : IRequestHandler<TerminateHoneypotCommand, Result>
{
    private readonly IHoneypotRepository _honeypotRepository;
    private readonly IUnitOfWork _unitOfWork;

    public TerminateHoneypotCommandHandler(
        IHoneypotRepository honeypotRepository,
        IUnitOfWork unitOfWork)
    {
        _honeypotRepository = honeypotRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(TerminateHoneypotCommand request, CancellationToken cancellationToken)
    {
        var honeypot = await _honeypotRepository.GetByIdAsync(request.HoneypotId, cancellationToken);

        if (honeypot == null || honeypot.OrganizationId != request.OrganizationId)
            return Result.Failure(HoneypotErrors.NotFound);

        // Terminate kills the Honeypot logic container entirely and removes active networking limits
        var result = honeypot.Terminate(request.Reason);
        if (result.IsFailure)
            return result;

        await _honeypotRepository.UpdateAsync(honeypot, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
