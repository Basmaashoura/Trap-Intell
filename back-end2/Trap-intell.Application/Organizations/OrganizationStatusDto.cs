namespace Trap_Intel.Application.Organizations;

public sealed record OrganizationStatusDto(
    Guid OrganizationId,
    string Name,
    string Status,
    DateTime UpdatedAt,
    DateTime? ApprovedAt,
    string? ApprovalNotes);
