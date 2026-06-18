using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Trap_Intel.Application.Abstractions.RealTime;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Auditing;

namespace Trap_Intel.Application.Auditing.Commands.AcknowledgeAuditLog;

internal sealed class AcknowledgeAuditLogCommandHandler : IRequestHandler<AcknowledgeAuditLogCommand, Result>
{
    private readonly IAuditTrailRepository _auditTrailRepository;
    private readonly IListRealtimeNotifier _listRealtimeNotifier;
    private readonly IUnitOfWork _unitOfWork;

    public AcknowledgeAuditLogCommandHandler(
        IAuditTrailRepository auditTrailRepository,
        IListRealtimeNotifier listRealtimeNotifier,
        IUnitOfWork unitOfWork)
    {
        _auditTrailRepository = auditTrailRepository;
        _listRealtimeNotifier = listRealtimeNotifier;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(AcknowledgeAuditLogCommand request, CancellationToken cancellationToken)
    {
        var auditTrail = await _auditTrailRepository.GetByIdAsync(request.AuditTrailId);

        if (auditTrail is null)
        {
            return Result.Failure(AuditingErrors.AuditTrailNotFound);
        }

        if (auditTrail.OrganizationId != request.OrganizationId)
        {
            return Result.Failure(AuditingErrors.InvalidResourceId);
        }

        var result = auditTrail.Acknowledge(request.UserId, request.Notes);

        if (result.IsFailure)
        {
            return result;
        }

        await _auditTrailRepository.UpdateAsync(auditTrail);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var payload = new { auditTrailId = request.AuditTrailId };
        await _listRealtimeNotifier.NotifyOrganizationListChangedAsync(
            "auditlogs",
            request.OrganizationId,
            action: "updated",
            payload: payload,
            cancellationToken: cancellationToken);

        return Result.Success();
    }
}
