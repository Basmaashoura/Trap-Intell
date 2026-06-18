using Trap_Intel.Application.Abstractions.Querying;

namespace Trap_Intel.Api.Contracts;

public sealed class GlobalListQueryRequest
{
    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = GlobalQueryOptions.DefaultPageSize;

    public string? Search { get; init; }

    public string? SortBy { get; init; }

    public string? SortDirection { get; init; } = "desc";

    public GlobalQueryOptions ToQueryOptions()
    {
        var sortDirection = string.IsNullOrWhiteSpace(SortDirection)
            ? "desc"
            : SortDirection;

        return new GlobalQueryOptions(
            PageNumber,
            PageSize,
            Search,
            SortBy,
            sortDirection);
    }
}
