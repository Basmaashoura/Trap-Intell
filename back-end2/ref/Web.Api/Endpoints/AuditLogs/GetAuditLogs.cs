using Application.Abstractions.Messaging;
using Application.AuditLogs.Common;
using Application.AuditLogs.GetAuditLogs;
using Domain.AuditLogs;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.AuditLogs;

/// <summary>
/// Endpoint for getting paginated audit logs.
/// </summary>
internal sealed class GetAuditLogs : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("audit-logs", async (
            int? pageNumber,
            int? pageSize,
            Guid? userId,
            AuditAction? action,
            string? entityType,
            DateTime? fromDate,
            DateTime? toDate,
            string? sortBy,
            string? sortDirection,
            IQueryHandler<GetAuditLogsQuery, PagedResult<AuditLogListItemResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetAuditLogsQuery
            {
                PageNumber = pageNumber ?? 1,
                PageSize = pageSize ?? 10,
                UserId = userId,
                Action = action,
                EntityType = entityType,
                FromDate = fromDate,
                ToDate = toDate,
                SortBy = sortBy ?? "Timestamp",
                SortDirection = sortDirection ?? "desc"
            };

            Result<PagedResult<AuditLogListItemResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.AuditLogs)
        .WithName("GetAuditLogs")
        .WithSummary("Get paginated audit logs")
        .WithDescription("Returns a paginated list of audit logs with optional filtering and sorting. Admin only.")
        .Produces<PagedResult<AuditLogListItemResponse>>(200)
        .ProducesProblem(400)
        .Produces(401)
        .Produces(403);
    }
}
