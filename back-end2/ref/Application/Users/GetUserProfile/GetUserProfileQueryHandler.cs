using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Users.Common;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Users.GetUserProfile;

/// <summary>
/// Handler for GetUserProfileQuery.
/// Returns user profile information.
/// </summary>
internal sealed class GetUserProfileQueryHandler : IQueryHandler<GetUserProfileQuery, UserProfileResponse>
{
    private readonly IApplicationDbContext _context;

    public GetUserProfileQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<UserProfileResponse>> Handle(
        GetUserProfileQuery query,
        CancellationToken cancellationToken)
    {
        UserProfileResponse? profile = await _context.UserProfiles
            .AsNoTracking()
            .Where(p => p.UserId == query.UserId)
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
            return Result.Failure<UserProfileResponse>(UserErrors.ProfileNotFound(query.UserId));
        }

        return Result.Success(profile);
    }
}
