using Application.Abstractions.Messaging;
using Application.Accounts.UpdateContactPermissions;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Accounts;

/// <summary>
/// Endpoint for updating a contact's permissions.
/// </summary>
internal sealed class UpdateContactPermissions : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("accounts/{accountId:guid}/contacts/{contactId:guid}/permissions", async (
            Guid accountId,
            Guid contactId,
            UpdateContactPermissionsRequest request,
            ICommandHandler<UpdateContactPermissionsCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateContactPermissionsCommand(
                accountId,
                contactId,
                request.CanCreateTickets,
                request.CanViewAllTickets,
                request.CanViewStressData,
                request.CanViewReports,
                request.CanViewAnalytics,
                request.CanExportData,
                request.CanManageContacts,
                request.CanManageSuggestions,
                request.CanDownloadFiles,
                request.ReceiveNotifications);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.Accounts)
        .WithName("UpdateContactPermissions")
        .WithSummary("Update contact permissions")
        .WithDescription("Updates the permissions for a specific contact. Only primary contacts or admins can perform this action.")
        .Produces(204)
        .ProducesProblem(400)
        .ProducesProblem(403)
        .ProducesProblem(404);
    }
}

/// <summary>
/// Request body for updating contact permissions.
/// </summary>
public sealed record UpdateContactPermissionsRequest(
    bool CanCreateTickets,
    bool CanViewAllTickets,
    bool CanViewStressData,
    bool CanViewReports,
    bool CanViewAnalytics,
    bool CanExportData,
    bool CanManageContacts,
    bool CanManageSuggestions,
    bool CanDownloadFiles,
    bool ReceiveNotifications);
