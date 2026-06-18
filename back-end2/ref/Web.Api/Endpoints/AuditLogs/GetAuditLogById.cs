using Application.Abstractions.Messaging;
using Application.AuditLogs.Common;
using Application.AuditLogs.GetAuditLogById;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.AuditLogs;

/// <summary>
/// Endpoint for getting an audit log by ID.
/// </summary>
internal sealed class GetAuditLogById : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("audit-logs/{auditLogId:guid}", async (
            Guid auditLogId,
            IQueryHandler<GetAuditLogByIdQuery, AuditLogDetailResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetAuditLogByIdQuery(auditLogId);

            Result<AuditLogDetailResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.AuditLogs)
        .WithName("GetAuditLogById")
        .WithSummary("Get audit log by ID")
        .WithDescription("Returns detailed information about a specific audit log including old/new values. Admin only.")
        .Produces<AuditLogDetailResponse>(200)
        .ProducesProblem(404)
        .Produces(401)
        .Produces(403);
    }
}
