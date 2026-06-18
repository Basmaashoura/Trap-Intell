using MediatR;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Application.Organizations.Queries.GetOrganizationStatus;

public sealed record GetOrganizationStatusQuery(
    Guid OrganizationId) : IRequest<Result<OrganizationStatusDto>>;
