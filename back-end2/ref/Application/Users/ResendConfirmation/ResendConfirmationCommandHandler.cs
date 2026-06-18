using Application.Abstractions.Email;
using Application.Abstractions.Data;
using Application.Abstractions.Identity;
using Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Users.ResendConfirmation;

/// <summary>
/// Handler for ResendConfirmationCommand.
/// Generates a new confirmation token and sends it via email.
/// </summary>
internal sealed class ResendConfirmationCommandHandler : ICommandHandler<ResendConfirmationCommand>
{
    private readonly IIdentityService _identityService;
    private readonly IEmailService _emailService;
    private readonly IApplicationDbContext _context;

    public ResendConfirmationCommandHandler(
        IIdentityService identityService,
        IEmailService emailService,
        IApplicationDbContext context)
    {
        _identityService = identityService;
        _emailService = emailService;
        _context = context;
    }

    public async Task<Result> Handle(ResendConfirmationCommand command, CancellationToken cancellationToken)
    {
        // Find user by email
        Domain.Users.User? user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == command.Email, cancellationToken);

        if (user is null)
        {
            // SECURITY: Don't reveal if email exists
            await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
            return Result.Success();
        }

        // Generate new confirmation token
        Result<string> tokenResult = await _identityService.GenerateEmailConfirmationTokenAsync(
            user.Id,
            cancellationToken);

        if (tokenResult.IsFailure)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
            return Result.Success();
        }

        // Send confirmation email
        await _emailService.SendEmailConfirmationAsync(
            command.Email,
            tokenResult.Value,
            cancellationToken);

        // SECURITY: Always return success (timing-safe)
        return Result.Success();
    }
}
