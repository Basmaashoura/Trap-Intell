using Application.Abstractions.Messaging;
using Application.Accounts.CreateAccount;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Accounts;

/// <summary>
/// Endpoint for creating a new account.
/// </summary>
internal sealed class CreateAccount : IEndpoint
{
    public sealed record Request(
        string Name,
        string? Industry,
        string? Website,
        string? Phone,
        string? Address,
        string? TaxNumber);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("accounts", async (
            Request request,
            ICommandHandler<CreateAccountCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateAccountCommand(
                request.Name,
                request.Industry,
                request.Website,
                request.Phone,
                request.Address,
                request.TaxNumber);

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(
                id => Results.Created($"/accounts/{id}", new { id, message = "Account created successfully." }),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.Accounts)
        .WithName("CreateAccount")
        .WithSummary("Create a new account")
        .WithDescription("Creates a new client account in the system.")
        .Produces(201)
        .ProducesProblem(400)
        .ProducesProblem(409)
        .Produces(401)
        .Produces(403);
    }
}
