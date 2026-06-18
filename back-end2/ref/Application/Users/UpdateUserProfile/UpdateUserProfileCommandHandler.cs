using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Users.UpdateUserProfile;

/// <summary>
/// Handler for UpdateUserProfileCommand.
/// Updates a user's profile with full access (Admin).
/// </summary>
internal sealed class UpdateUserProfileCommandHandler : ICommandHandler<UpdateUserProfileCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateUserProfileCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(UpdateUserProfileCommand command, CancellationToken cancellationToken)
    {
        // Check if user exists
        bool userExists = await _context.Users
            .AnyAsync(u => u.Id == command.UserId, cancellationToken);

        if (!userExists)
        {
            return Result.Failure(UserErrors.NotFound(command.UserId));
        }

        UserProfile? profile = await _context.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == command.UserId, cancellationToken);

        if (profile is null)
        {
            // Create profile if it doesn't exist
            profile = UserProfile.Create(
                command.UserId,
                command.Department,
                command.DisplayJobTitle,
                command.InternalJobTitle,
                command.HourlyCost);

            _context.UserProfiles.Add(profile);
        }
        else
        {
            // Update existing profile
            profile.UpdateDepartment(command.Department);
            profile.UpdateJobTitles(command.DisplayJobTitle, command.InternalJobTitle);
            profile.UpdateHourlyCost(command.HourlyCost);
        }

        // Update common fields
        profile.UpdateContactInfo(command.PhoneNumber);

        Uri? avatarUri = !string.IsNullOrWhiteSpace(command.AvatarUrl)
            ? new Uri(command.AvatarUrl)
            : null;

        profile.UpdateProfile(
            avatarUri,
            command.Bio,
            command.Skills?.ToList(),
            command.HireDate);

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
