using Application.Abstractions.Messaging;
using Application.Accounts.DeactivateAccount;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Accounts;

/// <summary>
/// Endpoint for deactivating an account.
/// </summary>
internal sealed class DeactivateAccount : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("accounts/{accountId:guid}/deactivate", async (
            Guid accountId,
            ICommandHandler<DeactivateAccountCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new DeactivateAccountCommand(accountId);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => Results.Ok(new { message = "Account deactivated successfully." }),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.Accounts)
        .WithName("DeactivateAccount")
        .WithSummary("Deactivate an account")
        .WithDescription("Deactivates an account.")
        .Produces(200)
        .ProducesProblem(404)
        .Produces(401)
        .Produces(403);
    }
}
