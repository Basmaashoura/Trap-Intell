namespace Trap_Intel.Infrastructure.Notifications.RealTime;

internal static class ListUpdateGroups
{
    internal const string AllFilter = "all";

    internal static string Organization(string entity, Guid organizationId, string? filterKey)
    {
        return $"list_org_{organizationId:N}_{NormalizeToken(entity)}_{NormalizeToken(filterKey)}";
    }

    internal static string User(string entity, Guid userId, string? filterKey)
    {
        return $"list_user_{userId:N}_{NormalizeToken(entity)}_{NormalizeToken(filterKey)}";
    }

    internal static string NormalizeEntity(string entity)
    {
        return NormalizeToken(entity);
    }

    internal static string NormalizeFilter(string? filterKey)
    {
        return NormalizeToken(filterKey);
    }

    private static string NormalizeToken(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return AllFilter;
        }

        var normalizedChars = rawValue
            .Trim()
            .ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '_')
            .ToArray();

        var normalized = new string(normalizedChars);

        if (normalized.Length == 0)
        {
            return AllFilter;
        }

        const int maxLength = 120;
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }
}
