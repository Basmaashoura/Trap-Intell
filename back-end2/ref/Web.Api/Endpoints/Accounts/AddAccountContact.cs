using Application.Abstractions.Messaging;
using Application.Accounts.AddAccountContact;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Accounts;

/// <summary>
/// Endpoint for adding a contact to an account.
/// </summary>
internal sealed class AddAccountContact : IEndpoint
{
    public sealed record Request(
        Guid UserId,
        string? Role,
        bool IsPrimaryContact = false,
        bool IsDecisionMaker = false);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("accounts/{accountId:guid}/contacts", async (
            Guid accountId,
            Request request,
            ICommandHandler<AddAccountContactCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new AddAccountContactCommand(
                accountId,
                request.UserId,
                request.Role,
                request.IsPrimaryContact,
                request.IsDecisionMaker);

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(
                id => Results.Created($"/accounts/{accountId}/contacts/{id}", new { id, message = "Contact added successfully." }),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.Accounts)
        .WithName("AddAccountContact")
        .WithSummary("Add contact to account")
        .WithDescription("Adds a user as a contact to an account.")
        .Produces(201)
        .ProducesProblem(400)
        .ProducesProblem(404)
        .ProducesProblem(409)
        .Produces(401)
        .Produces(403);
    }
}
