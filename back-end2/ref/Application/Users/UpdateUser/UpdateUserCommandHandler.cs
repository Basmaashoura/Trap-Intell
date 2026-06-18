using Application.Abstractions.Data;
using Application.Abstractions.Identity;
using Application.Abstractions.Messaging;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Users.UpdateUser;

/// <summary>
/// Handler for UpdateUserCommand.
/// Updates a user's information (admin operation).
/// </summary>
internal sealed class UpdateUserCommandHandler : ICommandHandler<UpdateUserCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;

    public UpdateUserCommandHandler(
        IApplicationDbContext context,
        IIdentityService identityService)
    {
        _context = context;
        _identityService = identityService;
    }

    public async Task<Result> Handle(UpdateUserCommand command, CancellationToken cancellationToken)
    {
        User? user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(UserErrors.NotFound(command.UserId));
        }

        // Check if email is unique (if changed)
        if (user.Email != command.Email)
        {
            bool emailExists = await _context.Users
                .AnyAsync(u => u.Email == command.Email && u.Id != command.UserId, cancellationToken);

            if (emailExists)
            {
                return Result.Failure(UserErrors.EmailNotUnique);
            }

            // Update email in Identity as well
            Result emailUpdateResult = await _identityService.UpdateEmailAsync(
                command.UserId,
                command.Email,
                cancellationToken);

            if (emailUpdateResult.IsFailure)
            {
                return emailUpdateResult;
            }

            // Update email using domain method
            user.UpdateEmail(command.Email);
        }

        // Update user information using domain method
        user.UpdateName(command.FirstName, command.LastName);

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
