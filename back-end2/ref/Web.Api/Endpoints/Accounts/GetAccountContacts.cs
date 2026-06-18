using Application.Abstractions.Messaging;
using Application.Accounts.Common;
using Application.Accounts.GetAccountContacts;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Accounts;

/// <summary>
/// Endpoint for getting account contacts.
/// </summary>
internal sealed class GetAccountContacts : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("accounts/{accountId:guid}/contacts", async (
            Guid accountId,
            IQueryHandler<GetAccountContactsQuery, IReadOnlyList<AccountContactResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetAccountContactsQuery(accountId);

            Result<IReadOnlyList<AccountContactResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.Accounts)
        .WithName("GetAccountContacts")
        .WithSummary("Get account contacts")
        .WithDescription("Returns all contacts for an account.")
        .Produces<IReadOnlyList<AccountContactResponse>>(200)
        .ProducesProblem(404)
        .Produces(401);
    }
}
