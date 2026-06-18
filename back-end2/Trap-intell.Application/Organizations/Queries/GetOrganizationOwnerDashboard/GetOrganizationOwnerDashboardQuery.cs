using MediatR;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Application.Organizations.Queries.GetOrganizationOwnerDashboard;

public sealed record GetOrganizationOwnerDashboardQuery(
    Guid OrganizationId,
    int LastNDays = 30) : IRequest<Result<OrganizationOwnerDashboardDto>>;
