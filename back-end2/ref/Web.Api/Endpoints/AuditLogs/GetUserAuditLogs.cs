using Application.Abstractions.Messaging;
using Application.AuditLogs.Common;
using Application.AuditLogs.GetUserAuditLogs;
using Domain.AuditLogs;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.AuditLogs;

/// <summary>
/// Endpoint for getting audit logs for a specific user.
/// </summary>
internal sealed class GetUserAuditLogs : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("users/{userId:guid}/audit-logs", async (
            Guid userId,
            int pageNumber = 1,
            int pageSize = 20,
            AuditAction? action = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string sortBy = "Timestamp",
            string sortDirection = "desc",
            IQueryHandler<GetUserAuditLogsQuery, PagedResult<AuditLogListItemResponse>>? handler = null,
            CancellationToken cancellationToken = default) =>
        {
            var query = new GetUserAuditLogsQuery
            {
                UserId = userId,
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
        .WithName("GetUserAuditLogs")
        .WithSummary("Get user's audit logs")
        .WithDescription("Returns paginated audit logs for a specific user.")
        .Produces<PagedResult<AuditLogListItemResponse>>(200)
        .ProducesProblem(400)
        .ProducesProblem(404)
        .Produces(401)
        .Produces(403);
    }
}
