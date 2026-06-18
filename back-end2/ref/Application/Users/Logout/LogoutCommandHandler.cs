using Application.Abstractions.Identity;
using Application.Abstractions.Messaging;
using SharedKernel;

namespace Application.Users.Logout;

internal sealed class LogoutCommandHandler(IIdentityService identityService)
    : ICommandHandler<LogoutCommand>
{
    public async Task<Result> Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        return await identityService.LogoutAsync(command.UserId, cancellationToken);
    }
}
