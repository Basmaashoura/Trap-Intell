namespace Application.Roles.Common;

/// <summary>
/// Response DTO for Permission information.
/// </summary>
public sealed record PermissionResponse
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Module { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
}
