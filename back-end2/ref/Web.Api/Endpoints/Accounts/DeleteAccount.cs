using Application.Abstractions.Messaging;
using Application.Accounts.DeleteAccount;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Accounts;

/// <summary>
/// Endpoint for deleting an account.
/// </summary>
internal sealed class DeleteAccount : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("accounts/{accountId:guid}", async (
            Guid accountId,
            ICommandHandler<DeleteAccountCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new DeleteAccountCommand(accountId);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => Results.NoContent(),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.Accounts)
        .WithName("DeleteAccount")
        .WithSummary("Delete an account")
        .WithDescription("Permanently deletes an account. Cannot delete accounts with active contacts.")
        .Produces(204)
        .ProducesProblem(400)
        .ProducesProblem(404)
        .Produces(401)
        .Produces(403);
    }
}
