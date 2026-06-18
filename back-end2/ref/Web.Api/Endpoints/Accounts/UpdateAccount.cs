using Application.Abstractions.Messaging;
using Application.Accounts.UpdateAccount;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Accounts;

/// <summary>
/// Endpoint for updating an account.
/// </summary>
internal sealed class UpdateAccount : IEndpoint
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
        app.MapPut("accounts/{accountId:guid}", async (
            Guid accountId,
            Request request,
            ICommandHandler<UpdateAccountCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateAccountCommand(
                accountId,
                request.Name,
                request.Industry,
                request.Website,
                request.Phone,
                request.Address,
                request.TaxNumber);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => Results.Ok(new { message = "Account updated successfully." }),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.Accounts)
        .WithName("UpdateAccount")
        .WithSummary("Update an account")
        .WithDescription("Updates an existing account.")
        .Produces(200)
        .ProducesProblem(400)
        .ProducesProblem(404)
        .ProducesProblem(409)
        .Produces(401)
        .Produces(403);
    }
}
