using Application.Abstractions.Data;
using Application.Abstractions.Identity;
using Application.Abstractions.Messaging;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Users.Login2FA;

/// <summary>
/// Handler for completing 2FA login.
/// </summary>
internal sealed class Login2FACommandHandler(
    IApplicationDbContext context,
    IIdentityService identityService) : ICommandHandler<Login2FACommand, TokenResponse>
{
    public async Task<Result<TokenResponse>> Handle(Login2FACommand command, CancellationToken cancellationToken)
    {
        // Verify 2FA code and get tokens
        Result<TokenResponse> result = await identityService.VerifyTwoFactorAsync(
            command.Email,
            command.Code,
            cancellationToken);

        if (result.IsFailure)
        {
            return result;
        }

        // Update Domain User's last login
        User? user = await context.Users
            .SingleOrDefaultAsync(u => u.Email == command.Email, cancellationToken);

        if (user is not null)
        {
            user.RecordLogin(null, null);
            await context.SaveChangesAsync(cancellationToken);
        }

        return result;
    }
}
