using Application.Abstractions.Data;
using Application.Abstractions.Identity;
using Application.Abstractions.Messaging;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Users.UpdateCurrentUserProfile;

/// <summary>
/// Handler for UpdateCurrentUserProfileCommand.
/// Updates the current user's profile.
/// </summary>
internal sealed class UpdateCurrentUserProfileCommandHandler : ICommandHandler<UpdateCurrentUserProfileCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public UpdateCurrentUserProfileCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(UpdateCurrentUserProfileCommand command, CancellationToken cancellationToken)
    {
        UserProfile? profile = await _context.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == _currentUser.UserId, cancellationToken);

        if (profile is null)
        {
            return Result.Failure(UserErrors.ProfileNotFound(_currentUser.UserId));
        }

        // Update contact info
        profile.UpdateContactInfo(command.PhoneNumber);

        // Update profile
        Uri? avatarUri = !string.IsNullOrWhiteSpace(command.AvatarUrl) 
            ? new Uri(command.AvatarUrl) 
            : null;

        profile.UpdateProfile(
            avatarUri,
            command.Bio,
            command.Skills?.ToList(),
            profile.HireDate); // Keep existing hire date

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
