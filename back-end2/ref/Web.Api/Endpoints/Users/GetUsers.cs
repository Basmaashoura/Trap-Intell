using Application.Abstractions.Messaging;
using Application.Users.GetUsers;
using Domain.Users;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

/// <summary>
/// Endpoint for getting paginated list of users.
/// </summary>
internal sealed class GetUsers : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("users", async (
            int? pageNumber,
            int? pageSize,
            string? searchTerm,
            UserStatus? status,
            AccountType? accountType,
            string? sortBy,
            string? sortDirection,
            IQueryHandler<GetUsersQuery, PagedResult<UserListItemResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetUsersQuery
            {
                PageNumber = pageNumber ?? 1,
                PageSize = pageSize ?? 10,
                SearchTerm = searchTerm,
                Status = status,
                AccountType = accountType,
                SortBy = sortBy ?? "CreatedAt",
                SortDirection = sortDirection ?? "desc"
            };

            Result<PagedResult<UserListItemResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.Users)
        .WithName("GetUsers")
        .WithSummary("Get paginated list of users")
        .WithDescription("Returns a paginated list of users with optional filtering and sorting.")
        .Produces<PagedResult<UserListItemResponse>>(200)
        .ProducesProblem(400)
        .Produces(401);
    }
}
