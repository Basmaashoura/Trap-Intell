using MediatR;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Organizations;
using Trap_Intel.Domain.Shared;

namespace Trap_Intel.Application.Organizations.Commands.CreateOrganization;

internal sealed class CreateOrganizationCommandHandler : IRequestHandler<CreateOrganizationCommand, Result<Guid>>
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateOrganizationCommandHandler(IOrganizationRepository organizationRepository, IUnitOfWork unitOfWork)
    {
        _organizationRepository = organizationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateOrganizationCommand request, CancellationToken cancellationToken)
    {
        // 1. Check for Domain Uniqueness
        bool domainExists = await _organizationRepository.DomainExistsAsync(request.Domain, cancellationToken);
        if (domainExists)
        {
            return Result.Failure<Guid>(Error.Custom("Organization.DomainNotUnique", "The provided domain is already associated with an account."));
        }

        // 2. Map Value Objects
        var domainResult = OrganizationDomain.Create(request.Domain);
        var taxIdResult = TaxIdentifier.Create(request.TaxId);
        var contactInfoResult = ContactInfo.Create(request.ContactEmail, request.ContactPhone, request.ContactWebsite);

        // Basic aggregation of errors if value object creation fails
        var validationErrors = new List<Error>();
        if (domainResult.IsFailure) validationErrors.AddRange(domainResult.Errors);
        if (taxIdResult.IsFailure) validationErrors.AddRange(taxIdResult.Errors);
        if (contactInfoResult.IsFailure) validationErrors.AddRange(contactInfoResult.Errors);

        if (validationErrors.Any())
        {
            return Result.Failure<Guid>(validationErrors);
        }

        // 3. Setup Settings
        var settings = new Trap_Intel.Domain.Organizations.OrganizationSettings(
            request.AllowMultipleAddresses,
            request.RequireApprovalForMembers,
            request.MaximumMembers,
            request.EnableBilling,
            request.EnableApiAccess);

        // 4. Create the Organization Entity
        var organizationResult = Organization.Create(
            request.Name,
            request.Type,
            request.Industry,
            request.Size,
            domainResult.Value,
            taxIdResult.Value,
            contactInfoResult.Value,
            request.Website ?? string.Empty, // Passing empty string if null, adjust if Domain has specific expectations
            settings,
            request.ParentOrganizationId);

        if (organizationResult.IsFailure)
        {
            return Result.Failure<Guid>(organizationResult.Errors);
        }

        var organization = organizationResult.Value;

        // 5. Save to the database
        await _organizationRepository.AddAsync(organization, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(organization.Id);
    }
}
