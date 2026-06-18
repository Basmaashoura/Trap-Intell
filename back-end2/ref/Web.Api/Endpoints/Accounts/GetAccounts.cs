using Application.Abstractions.Messaging;
using Application.Accounts.Common;
using Application.Accounts.GetAccounts;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Accounts;

/// <summary>
/// Endpoint for getting paginated list of accounts.
/// </summary>
internal sealed class GetAccounts : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("accounts", async (
            int? pageNumber,
            int? pageSize,
            string? searchTerm,
            bool? isActive,
            string? industry,
            string? sortBy,
            string? sortDirection,
            IQueryHandler<GetAccountsQuery, PagedResult<AccountListItemResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetAccountsQuery
            {
                PageNumber = pageNumber ?? 1,
                PageSize = pageSize ?? 10,
                SearchTerm = searchTerm,
                IsActive = isActive,
                Industry = industry,
                SortBy = sortBy ?? "Name",
                SortDirection = sortDirection ?? "asc"
            };

            Result<PagedResult<AccountListItemResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.Accounts)
        .WithName("GetAccounts")
        .WithSummary("Get paginated list of accounts")
        .WithDescription("Returns a paginated list of accounts with optional filtering and sorting.")
        .Produces<PagedResult<AccountListItemResponse>>(200)
        .ProducesProblem(400)
        .Produces(401);
    }
}
