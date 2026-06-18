using FluentValidation;
using MediatR;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Organizations;

namespace Trap_Intel.Application.Organizations.Commands.CreateOrganization;

public sealed record CreateOrganizationCommand(
    string Name,
    OrganizationType Type,
    string Industry,
    int Size,
    string Domain,
    string TaxId,
    string ContactEmail,
    string ContactPhone,
    string? ContactWebsite,
    string? Website,
    bool AllowMultipleAddresses,
    bool RequireApprovalForMembers,
    int MaximumMembers,
    bool EnableBilling,
    bool EnableApiAccess,
    Guid? ParentOrganizationId = null
) : IRequest<Result<Guid>>;

public sealed class CreateOrganizationCommandValidator : AbstractValidator<CreateOrganizationCommand>
{
    public CreateOrganizationCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Domain).NotEmpty();
        RuleFor(x => x.TaxId).NotEmpty();
        RuleFor(x => x.ContactEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.ContactPhone).NotEmpty();
    }
}
