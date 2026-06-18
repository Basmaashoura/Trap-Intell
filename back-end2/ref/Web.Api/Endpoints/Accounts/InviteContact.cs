using Application.Abstractions.Messaging;
using Application.Accounts.InviteContact;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Accounts;

/// <summary>
/// Endpoint for inviting a contact to an account via email.
/// Contact is created with MINIMAL permissions.
/// Primary contact should set proper permissions after invitation is accepted.
/// </summary>
internal sealed class InviteContact : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("accounts/{accountId:guid}/invitations", async (
            Guid accountId,
            InviteContactRequest request,
            ICommandHandler<InviteContactCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new InviteContactCommand(
                accountId,
                request.Email,
                request.FirstName,
                request.LastName,
                request.Role,
                request.ExpirationDays);

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(
                id => Results.Created($"/accounts/{accountId}/contacts/{id}", new { id }),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.Accounts)
        .WithName("InviteContact")
        .WithSummary("Invite a contact via email")
        .WithDescription("Sends an invitation email to add a new contact. Contact is created with minimal permissions - use UpdateContactPermissions after acceptance to set proper permissions.")
        .Produces<object>(201)
        .ProducesProblem(400)
        .ProducesProblem(403)
        .ProducesProblem(404);
    }
}

/// <summary>
/// Request body for inviting a contact.
/// Note: Contact is created with MINIMAL permissions.
/// Use UpdateContactPermissions endpoint after invitation is accepted to set proper permissions.
/// </summary>
public sealed record InviteContactRequest(
    string Email,
    string FirstName,
    string LastName,
    string? Role = null,
    int ExpirationDays = 7);
