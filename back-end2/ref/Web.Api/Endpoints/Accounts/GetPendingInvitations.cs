using Application.Abstractions.Messaging;
using Application.Accounts.GetPendingInvitations;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Accounts;

/// <summary>
/// Endpoint for getting pending invitations for an account.
/// </summary>
internal sealed class GetPendingInvitations : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("accounts/{accountId:guid}/invitations", async (
            Guid accountId,
            IQueryHandler<GetPendingInvitationsQuery, IReadOnlyList<PendingInvitationResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetPendingInvitationsQuery(accountId);

            Result<IReadOnlyList<PendingInvitationResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.Accounts)
        .WithName("GetPendingInvitations")
        .WithSummary("Get pending invitations")
        .WithDescription("Returns all pending (not yet accepted) invitations for an account.")
        .Produces<IReadOnlyList<PendingInvitationResponse>>(200)
        .ProducesProblem(403)
        .ProducesProblem(404);
    }
}
