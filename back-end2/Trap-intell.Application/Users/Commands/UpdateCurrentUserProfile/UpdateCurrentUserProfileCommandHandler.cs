using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Identity;

namespace Trap_Intel.Application.Users.Commands.UpdateCurrentUserProfile;

internal sealed class UpdateCurrentUserProfileCommandHandler : IRequestHandler<UpdateCurrentUserProfileCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCurrentUserProfileCommandHandler(IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateCurrentUserProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(IdentityErrors.UserNotFound_Detail(request.UserId.ToString()));
        }

        var effectiveFirstName = string.IsNullOrWhiteSpace(request.FirstName)
            ? user.FirstName.Value
            : request.FirstName;

        var effectiveLastName = string.IsNullOrWhiteSpace(request.LastName)
            ? user.LastName.Value
            : request.LastName;

        var effectivePhoneNumber = request.PhoneNumber is null
            ? user.PhoneNumber
            : request.PhoneNumber;

        var effectiveJobTitle = request.JobTitle is null
            ? user.JobTitle
            : request.JobTitle;

        var effectiveDepartment = request.Department is null
            ? user.Department
            : request.Department;

        var effectiveLocation = request.Location is null
            ? user.Location
            : request.Location;

        var effectiveBio = request.Bio is null
            ? user.Bio
            : request.Bio;

        var effectiveWebsiteUrl = request.WebsiteUrl is null
            ? user.WebsiteUrl
            : request.WebsiteUrl;

        var effectiveLinkedInUrl = request.LinkedInUrl is null
            ? user.LinkedInUrl
            : request.LinkedInUrl;

        var effectiveGitHubUrl = request.GitHubUrl is null
            ? user.GitHubUrl
            : request.GitHubUrl;

        var effectiveXUrl = request.XUrl is null
            ? user.XUrl
            : request.XUrl;

        var firstNameResult = FirstName.Create(effectiveFirstName);
        if (firstNameResult.IsFailure) return firstNameResult;

        var lastNameResult = LastName.Create(effectiveLastName);
        if (lastNameResult.IsFailure) return lastNameResult;

        var updateResult = user.UpdateProfile(
            firstNameResult.Value,
            lastNameResult.Value,
            effectivePhoneNumber,
            effectiveJobTitle,
            effectiveDepartment,
            effectiveLocation,
            effectiveBio,
            effectiveWebsiteUrl,
            effectiveLinkedInUrl,
            effectiveGitHubUrl,
            effectiveXUrl);
        if (updateResult.IsFailure) return updateResult;

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
