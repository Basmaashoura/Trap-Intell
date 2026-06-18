using Application.Abstractions.Messaging;
using Application.Accounts.Common;
using Application.Accounts.GetAccountById;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Accounts;

/// <summary>
/// Endpoint for getting an account by ID.
/// </summary>
internal sealed class GetAccountById : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("accounts/{accountId:guid}", async (
            Guid accountId,
            IQueryHandler<GetAccountByIdQuery, AccountResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetAccountByIdQuery(accountId);

            Result<AccountResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.Accounts)
        .WithName("GetAccountById")
        .WithSummary("Get account by ID")
        .WithDescription("Returns detailed information about a specific account.")
        .Produces<AccountResponse>(200)
        .ProducesProblem(404)
        .Produces(401);
    }
}
