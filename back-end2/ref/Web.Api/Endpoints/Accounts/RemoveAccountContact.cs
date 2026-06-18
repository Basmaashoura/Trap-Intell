using Application.Abstractions.Messaging;
using Application.Accounts.RemoveAccountContact;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Accounts;

/// <summary>
/// Endpoint for removing a contact from an account.
/// </summary>
internal sealed class RemoveAccountContact : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("accounts/{accountId:guid}/contacts/{contactId:guid}", async (
            Guid accountId,
            Guid contactId,
            ICommandHandler<RemoveAccountContactCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new RemoveAccountContactCommand(accountId, contactId);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => Results.NoContent(),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.Accounts)
        .WithName("RemoveAccountContact")
        .WithSummary("Remove contact from account")
        .WithDescription("Removes a contact from an account. Cannot remove primary contact.")
        .Produces(204)
        .ProducesProblem(400)
        .ProducesProblem(404)
        .Produces(401)
        .Produces(403);
    }
}
