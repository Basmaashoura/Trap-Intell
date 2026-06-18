using Application.Abstractions.Data;
using Application.Abstractions.Identity;
using Application.Abstractions.Messaging;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Users.UpdateCurrentUser;

/// <summary>
/// Handler for UpdateCurrentUserCommand.
/// Updates the current user's profile information.
/// </summary>
internal sealed class UpdateCurrentUserCommandHandler : ICommandHandler<UpdateCurrentUserCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public UpdateCurrentUserCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(UpdateCurrentUserCommand command, CancellationToken cancellationToken)
    {
        User? user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(UserErrors.NotFound(_currentUser.UserId));
        }

        // Update user information using domain method
        user.UpdateName(command.FirstName, command.LastName);

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
