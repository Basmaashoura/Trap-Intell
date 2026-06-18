namespace Application.Roles.Common;

/// <summary>
/// Response DTO for Role information.
/// </summary>
public sealed record RoleResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsSystemRole { get; init; }
    public bool IsActive { get; init; }
    public bool CanViewSensitiveData { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public int UserCount { get; init; }
    public int PermissionCount { get; init; }
}

/// <summary>
/// Simplified role response for lists.
/// </summary>
public sealed record RoleListItemResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsSystemRole { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public int UserCount { get; init; }
}
