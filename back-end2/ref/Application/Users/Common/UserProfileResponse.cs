using Domain.Common;

namespace Application.Users.Common;

/// <summary>
/// Response DTO for User Profile (public view).
/// Does not include sensitive data like HourlyCost.
/// </summary>
public sealed record UserProfileResponse
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string Department { get; init; } = string.Empty;
    public string? DisplayJobTitle { get; init; }
    public string? PhoneNumber { get; init; }
    public DateTime? HireDate { get; init; }
    public string? AvatarUrl { get; init; }
    public string? Bio { get; init; }
    public IReadOnlyList<string> Skills { get; init; } = [];
}

/// <summary>
/// Response DTO for User Profile (admin view).
/// Includes sensitive data like HourlyCost and InternalJobTitle.
/// </summary>
public sealed record UserProfileAdminResponse
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string Department { get; init; } = string.Empty;
    public string? DisplayJobTitle { get; init; }
    public string? InternalJobTitle { get; init; }
    public decimal? HourlyCost { get; init; }
    public string? PhoneNumber { get; init; }
    public DateTime? HireDate { get; init; }
    public string? AvatarUrl { get; init; }
    public string? Bio { get; init; }
    public IReadOnlyList<string> Skills { get; init; } = [];
}

/// <summary>
/// Response DTO for User Session.
/// </summary>
public sealed record UserSessionResponse
{
    public Guid Id { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime ExpiresAt { get; init; }
    public DateTime LastActivityAt { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public string? DeviceId { get; init; }
    public bool IsActive { get; init; }
    public bool IsCurrent { get; init; }
}
