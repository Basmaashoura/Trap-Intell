using Application.Abstractions.Messaging;
using Application.AuditLogs.Common;
using Application.AuditLogs.GetCurrentUserAuditLogs;
using Domain.AuditLogs;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.AuditLogs;

/// <summary>
/// Endpoint for getting the current user's audit logs.
/// </summary>
internal sealed class GetCurrentUserAuditLogs : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("audit-logs/my", async (
            int pageNumber = 1,
            int pageSize = 20,
            AuditAction? action = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string sortBy = "Timestamp",
            string sortDirection = "desc",
            IQueryHandler<GetCurrentUserAuditLogsQuery, PagedResult<AuditLogListItemResponse>>? handler = null,
            CancellationToken cancellationToken = default) =>
        {
            var query = new GetCurrentUserAuditLogsQuery
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                Action = action,
                FromDate = fromDate,
                ToDate = toDate,
                SortBy = sortBy,
                SortDirection = sortDirection
            };

            Result<PagedResult<AuditLogListItemResponse>> result = await handler!.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.AuditLogs)
        .WithName("GetCurrentUserAuditLogs")
        .WithSummary("Get current user's audit logs")
        .WithDescription("Returns paginated audit logs for the currently authenticated user.")
        .Produces<PagedResult<AuditLogListItemResponse>>(200)
        .ProducesProblem(400)
        .Produces(401);
    }
}
