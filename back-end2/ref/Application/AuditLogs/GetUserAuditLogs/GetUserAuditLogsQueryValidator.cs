using Application.AuditLogs.Common;
using FluentValidation;

namespace Application.AuditLogs.GetUserAuditLogs;

/// <summary>
/// Validator for GetUserAuditLogsQuery.
/// </summary>
internal sealed class GetUserAuditLogsQueryValidator : AbstractValidator<GetUserAuditLogsQuery>
{
    public GetUserAuditLogsQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");

        RuleFor(x => x.PageNumber)
            .GreaterThan(0)
            .WithMessage("Page number must be greater than 0");

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .WithMessage("Page size must be greater than 0")
            .LessThanOrEqualTo(100)
            .WithMessage("Page size must not exceed 100");

        RuleFor(x => x.SortBy)
            .Must(sortBy => string.IsNullOrWhiteSpace(sortBy) ||
                            AuditLogSortConfiguration.Instance.IsSortable(sortBy))
            .WithMessage(x => $"Sort by must be one of: {string.Join(", ", AuditLogSortConfiguration.Instance.GetSortableFieldNames())}");

        RuleFor(x => x.SortDirection)
            .Must(direction => string.IsNullOrWhiteSpace(direction) ||
                               direction.Equals("asc", StringComparison.OrdinalIgnoreCase) ||
                               direction.Equals("desc", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Sort direction must be 'asc' or 'desc'");

        RuleFor(x => x.FromDate)
            .LessThanOrEqualTo(x => x.ToDate)
            .WithMessage("From date must be before or equal to To date")
            .When(x => x.FromDate.HasValue && x.ToDate.HasValue);
    }
}
