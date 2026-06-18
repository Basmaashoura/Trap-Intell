using MediatR;
using Trap_Intel.Application.Abstractions.RealTime;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Auditing;

namespace Trap_Intel.Application.Auditing.Commands.TagAuditLog;

internal sealed class TagAuditLogCommandHandler : IRequestHandler<TagAuditLogCommand, Result>
{
    private readonly IAuditTrailRepository _auditRepository;
    private readonly IListRealtimeNotifier _listRealtimeNotifier;
    private readonly IUnitOfWork _unitOfWork;

    public TagAuditLogCommandHandler(
        IAuditTrailRepository auditRepository,
        IListRealtimeNotifier listRealtimeNotifier,
        IUnitOfWork unitOfWork)
    {
        _auditRepository = auditRepository;
        _listRealtimeNotifier = listRealtimeNotifier;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(TagAuditLogCommand request, CancellationToken cancellationToken)
    {
        var audit = await _auditRepository.GetByIdAsync(request.AuditTrailId);

        if (audit is null || audit.OrganizationId != request.OrganizationId)
        {
            // Do not leak existence info, return generic invalid id or not found
            return Result.Failure(AuditingErrors.InvalidResourceId);
        }

        var result = audit.AddComplianceStandard(request.Standard);

        if (result.IsFailure)
        {
            return result;
        }

        await _auditRepository.UpdateAsync(audit);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var payload = new { auditTrailId = request.AuditTrailId, standard = request.Standard.ToString() };
        await _listRealtimeNotifier.NotifyOrganizationListChangedAsync(
            "auditlogs",
            request.OrganizationId,
            action: "updated",
            payload: payload,
            cancellationToken: cancellationToken);

        return Result.Success();
    }
}
