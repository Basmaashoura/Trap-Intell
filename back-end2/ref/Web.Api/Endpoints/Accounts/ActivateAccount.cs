using Application.Abstractions.Messaging;
using Application.Accounts.ActivateAccount;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Accounts;

/// <summary>
/// Endpoint for activating an account.
/// </summary>
internal sealed class ActivateAccount : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("accounts/{accountId:guid}/activate", async (
            Guid accountId,
            ICommandHandler<ActivateAccountCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new ActivateAccountCommand(accountId);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => Results.Ok(new { message = "Account activated successfully." }),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.Accounts)
        .WithName("ActivateAccount")
        .WithSummary("Activate an account")
        .WithDescription("Activates a previously deactivated account.")
        .Produces(200)
        .ProducesProblem(404)
        .Produces(401)
        .Produces(403);
    }
}
