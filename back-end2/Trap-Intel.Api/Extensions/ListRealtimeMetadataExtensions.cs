using Trap_Intel.Api.Contracts;

namespace Trap_Intel.Api.Extensions;

internal static class ListRealtimeMetadataExtensions
{
    public static string BuildFilterKey(
        this GlobalListQueryRequest listQuery,
        params (string Name, object? Value)[] filters)
    {
        var parts = new List<string>();

        foreach (var (name, value) in filters)
        {
            var normalizedValue = NormalizeValue(value);
            if (normalizedValue is null)
            {
                continue;
            }

            parts.Add($"{name.ToLowerInvariant()}={normalizedValue}");
        }

        var search = NormalizeValue(listQuery.Search);
        if (search is not null)
        {
            parts.Add($"search={search}");
        }

        var sortBy = NormalizeValue(listQuery.SortBy);
        if (sortBy is not null)
        {
            parts.Add($"sortby={sortBy}");
        }

        var sortDirection = NormalizeValue(listQuery.SortDirection);
        if (sortDirection is not null)
        {
            parts.Add($"sortdir={sortDirection}");
        }

        return parts.Count == 0 ? "all" : string.Join("|", parts);
    }

    public static void SetListRealtimeHeaders(
        this HttpResponse response,
        string entity,
        string scope,
        string filterKey)
    {
        response.Headers["X-Realtime-Entity"] = entity;
        response.Headers["X-Realtime-Scope"] = scope;
        response.Headers["X-Realtime-Filter-Key"] = filterKey;
        response.Headers["X-Realtime-Hub"] = "/hubs/lists";
    }

    private static string? NormalizeValue(object? value)
    {
        if (value is null)
        {
            return null;
        }

        return value switch
        {
            string stringValue when string.IsNullOrWhiteSpace(stringValue) => null,
            string stringValue => stringValue.Trim().ToLowerInvariant(),
            bool boolValue => boolValue ? "true" : "false",
            Guid guidValue when guidValue == Guid.Empty => null,
            Guid guidValue => guidValue.ToString("N"),
            Enum enumValue => enumValue.ToString().ToLowerInvariant(),
            _ => value.ToString()?.Trim().ToLowerInvariant()
        };
    }
}
