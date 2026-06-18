using MediatR;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Honeypots;

namespace Trap_Intel.Application.Honeypots.Commands.ResumeHoneypot;

internal sealed class ResumeHoneypotCommandHandler : IRequestHandler<ResumeHoneypotCommand, Result>
{
    private readonly IHoneypotRepository _honeypotRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ResumeHoneypotCommandHandler(
        IHoneypotRepository honeypotRepository,
        IUnitOfWork unitOfWork)
    {
        _honeypotRepository = honeypotRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(ResumeHoneypotCommand request, CancellationToken cancellationToken)
    {
        var honeypot = await _honeypotRepository.GetByIdAsync(request.HoneypotId, cancellationToken);

        if (honeypot == null || honeypot.OrganizationId != request.OrganizationId)
            return Result.Failure(HoneypotErrors.NotFound);

        // Transition domain state
        var result = honeypot.Resume(request.Reason);
        if (result.IsFailure)
            return result;

        await _honeypotRepository.UpdateAsync(honeypot, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
