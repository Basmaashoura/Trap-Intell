using Domain.Users;

namespace Application.Users.GetUsers;

/// <summary>
/// Simplified user information for list display.
/// </summary>
public sealed record UserListItemResponse
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public AccountType AccountType { get; init; }
    public UserStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public bool IsActive => Status == UserStatus.Active;
}
