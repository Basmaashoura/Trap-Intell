using Application.Abstractions.Data;
using Application.Abstractions.Identity;
using Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Users.ConfirmEmail;

/// <summary>
/// Handler for ConfirmEmailCommand.
/// Confirms the user's email address using the provided token.
/// </summary>
internal sealed class ConfirmEmailCommandHandler : ICommandHandler<ConfirmEmailCommand>
{
    private readonly IIdentityService _identityService;
    private readonly IApplicationDbContext _context;

    public ConfirmEmailCommandHandler(
        IIdentityService identityService,
        IApplicationDbContext context)
    {
        _identityService = identityService;
        _context = context;
    }

    public async Task<Result> Handle(ConfirmEmailCommand command, CancellationToken cancellationToken)
    {
        // Find user by email to get domain user ID
        Domain.Users.User? user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == command.Email, cancellationToken);

        if (user is null)
        {
            return Result.Failure(IdentityErrors.UserNotFound);
        }

        // Confirm email using token
        Result result = await _identityService.ConfirmEmailAsync(
            user.Id,
            command.Token,
            cancellationToken);

        if (result.IsFailure)
        {
            return result;
        }

        // Domain event (EmailConfirmedDomainEvent) is raised inside IdentityService

        return Result.Success();
    }
}
