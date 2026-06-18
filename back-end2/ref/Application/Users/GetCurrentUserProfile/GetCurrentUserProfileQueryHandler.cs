using Application.Abstractions.Data;
using Application.Abstractions.Identity;
using Application.Abstractions.Messaging;
using Application.Users.Common;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Users.GetCurrentUserProfile;

/// <summary>
/// Handler for GetCurrentUserProfileQuery.
/// Returns the current user's profile.
/// </summary>
internal sealed class GetCurrentUserProfileQueryHandler : IQueryHandler<GetCurrentUserProfileQuery, UserProfileResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetCurrentUserProfileQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<UserProfileResponse>> Handle(
        GetCurrentUserProfileQuery query,
        CancellationToken cancellationToken)
    {
        UserProfileResponse? profile = await _context.UserProfiles
            .AsNoTracking()
            .Where(p => p.UserId == _currentUser.UserId)
            .Select(p => new UserProfileResponse
            {
                Id = p.Id,
                UserId = p.UserId,
                Department = p.Department.ToString(),
                DisplayJobTitle = p.DisplayJobTitle,
                PhoneNumber = p.PhoneNumber,
                HireDate = p.HireDate,
                AvatarUrl = p.AvatarUrl != null ? p.AvatarUrl.ToString() : null,
                Bio = p.Bio,
                Skills = p.Skills
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (profile is null)
        {
            return Result.Failure<UserProfileResponse>(UserErrors.ProfileNotFound(_currentUser.UserId));
        }

        return Result.Success(profile);
    }
}
