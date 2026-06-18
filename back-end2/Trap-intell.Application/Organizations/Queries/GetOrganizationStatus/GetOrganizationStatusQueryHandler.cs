using MediatR;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Organizations;

namespace Trap_Intel.Application.Organizations.Queries.GetOrganizationStatus;

internal sealed class GetOrganizationStatusQueryHandler : IRequestHandler<GetOrganizationStatusQuery, Result<OrganizationStatusDto>>
{
    private readonly IOrganizationRepository _organizationRepository;

    public GetOrganizationStatusQueryHandler(IOrganizationRepository organizationRepository)
    {
        _organizationRepository = organizationRepository;
    }

    public async Task<Result<OrganizationStatusDto>> Handle(GetOrganizationStatusQuery request, CancellationToken cancellationToken)
    {
        var organization = await _organizationRepository.GetByIdAsync(request.OrganizationId, cancellationToken);

        if (organization is null)
        {
            return Result.Failure<OrganizationStatusDto>(OrganizationErrors.OrganizationNotFound);
        }

        return Result.Success(new OrganizationStatusDto(
            organization.Id,
            organization.Name,
            organization.Status.ToString(),
            organization.UpdatedAt,
            organization.ApprovedAt,
            organization.ApprovalNotes));
    }
}
