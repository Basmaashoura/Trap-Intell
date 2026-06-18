using System.ComponentModel.DataAnnotations;

namespace Trap_Intel.Api.Endpoints.Organizations.Models;

public sealed record InviteUserRequest(
    string Email, 
    Guid RoleId, 
    string? PersonalMessage, 
    int? ExpirationDays);

public sealed record AcceptInvitationRequest(string Token);

public sealed record ResendInvitationRequest(int? ExpirationDays);

public sealed record RevokeInvitationRequest(string? Reason);

public sealed record UpdateOrganizationStatusRequest
{
    [Required(ErrorMessage = "Status is required")]
    public required string Status { get; init; }

    [MaxLength(1000, ErrorMessage = "Reason cannot exceed 1000 characters")]
    public string? Reason { get; init; }
}

public sealed record OrganizationInvitationResponse(
    Guid Id,
    Guid OrganizationId,
    string Email,
    Guid RoleId,
    Guid InvitedByUserId,
    string Status,
    string? PersonalMessage,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    DateTime UpdatedAt,
    DateTime? AcceptedAt,
    DateTime? DeclinedAt,
    DateTime? RevokedAt,
    bool IsExpired);
