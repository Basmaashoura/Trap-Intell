using Application.Abstractions.Messaging;
using Application.Accounts.UpdateAccountContact;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Accounts;

/// <summary>
/// Endpoint for updating an account contact.
/// </summary>
internal sealed class UpdateAccountContact : IEndpoint
{
    public sealed record Request(
        string? Role,
        bool IsPrimaryContact,
        bool IsDecisionMaker);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("accounts/{accountId:guid}/contacts/{contactId:guid}", async (
            Guid accountId,
            Guid contactId,
            Request request,
            ICommandHandler<UpdateAccountContactCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateAccountContactCommand(
                accountId,
                contactId,
                request.Role,
                request.IsPrimaryContact,
                request.IsDecisionMaker);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => Results.Ok(new { message = "Contact updated successfully." }),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.Accounts)
        .WithName("UpdateAccountContact")
        .WithSummary("Update account contact")
        .WithDescription("Updates an account contact's information.")
        .Produces(200)
        .ProducesProblem(400)
        .ProducesProblem(404)
        .Produces(401)
        .Produces(403);
    }
}
