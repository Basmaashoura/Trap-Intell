namespace Application.Accounts.Common;

/// <summary>
/// Response DTO for Account information.
/// </summary>
public sealed record AccountResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Industry { get; init; }
    public string? Website { get; init; }
    public string? Phone { get; init; }
    public string? Address { get; init; }
    public string? TaxNumber { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public int ContactCount { get; init; }
}

/// <summary>
/// Simplified account response for lists.
/// </summary>
public sealed record AccountListItemResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Industry { get; init; }
    public string? Website { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public int ContactCount { get; init; }
}

/// <summary>
/// Response DTO for Account Contact information.
/// </summary>
public sealed record AccountContactResponse
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public string? Role { get; init; }
    public bool IsPrimaryContact { get; init; }
    public bool IsDecisionMaker { get; init; }
    public bool IsActive { get; init; }
    public bool IsInviteAccepted { get; init; }
    public DateTime CreatedAt { get; init; }
}
