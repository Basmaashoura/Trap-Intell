using System.Net;
using System.Net.Http.Json;
using MediatR;
using Moq;
using Trap_Intel.Application.Abstractions.Querying;
using Trap_Intel.Application.Auditing.Commands.AcknowledgeAuditLog;
using Trap_Intel.Application.Auditing.Queries.GetAuditLogs;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Identity.Authorization;
using Trap_Intel.Tests.Integration.Infrastructure;

namespace Trap_Intel.Tests.Integration.AuditLogs;

public class AuditEndpointsSecurityTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuditEndpointsSecurityTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAuditLogs_WithAuthButNoPermission_ReturnsForbidden()
    {
        var organizationId = Guid.NewGuid();

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/organizations/{organizationId}/auditlogs?pageNumber=1&pageSize=20")
            .WithAuthenticatedOrganization(organizationId);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAuditLogs_WithViewerRoleEvenWithPermission_ReturnsForbidden()
    {
        var organizationId = Guid.NewGuid();

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/organizations/{organizationId}/auditlogs?pageNumber=1&pageSize=20")
            .WithTestAuth(organizationId, Permissions.Reports.View);
        request.Headers.Add(TestAuthenticationHandler.RoleHeader, "Viewer");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAuditLogs_WithSecurityAnalystAndPermission_ReturnsOk()
    {
        var organizationId = Guid.NewGuid();

        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<GetAuditLogsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new PagedResult<AuditTrailDto>(
                [],
                1,
                20,
                0)));

        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/organizations/{organizationId}/auditlogs?pageNumber=1&pageSize=20")
            .WithTestAuth(organizationId, Permissions.Reports.View);
        request.Headers.Add(TestAuthenticationHandler.RoleHeader, "SecurityAnalyst");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        sender.Verify(
            x => x.Send(
                It.Is<GetAuditLogsQuery>(q => q.OrganizationId == organizationId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExportAuditLogs_WithMissingExportPermission_ReturnsForbidden()
    {
        var organizationId = Guid.NewGuid();

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/organizations/{organizationId}/auditlogs/export")
            .WithTestAuth(organizationId, Permissions.Reports.View);
        request.Headers.Add(TestAuthenticationHandler.RoleHeader, "OperationsAnalyst");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AcknowledgeAuditLog_WithMissingAcknowledgePermission_ReturnsForbidden()
    {
        var organizationId = Guid.NewGuid();
        var auditId = Guid.NewGuid();

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/organizations/{organizationId}/auditlogs/{auditId}/acknowledge")
            .WithTestAuth(organizationId, Permissions.Reports.View);
        request.Headers.Add(TestAuthenticationHandler.RoleHeader, "OperationsAnalyst");
        request.Content = JsonContent.Create(new { notes = "triage" });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AcknowledgeAuditLog_WithPermissionAndRole_ReturnsNoContent()
    {
        var organizationId = Guid.NewGuid();
        var auditId = Guid.NewGuid();

        var sender = new Mock<ISender>();
        sender
            .Setup(x => x.Send(It.IsAny<AcknowledgeAuditLogCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var client = _factory.CreateClientWithSender(sender.Object);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/organizations/{organizationId}/auditlogs/{auditId}/acknowledge")
            .WithTestAuth(organizationId, Permissions.Alerts.Acknowledge);
        request.Headers.Add(TestAuthenticationHandler.RoleHeader, "OperationsAnalyst");
        request.Content = JsonContent.Create(new { notes = "triage" });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        sender.Verify(
            x => x.Send(
                It.Is<AcknowledgeAuditLogCommand>(c =>
                    c.OrganizationId == organizationId && c.AuditTrailId == auditId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TagAuditLog_WithMissingConfigurePermission_ReturnsForbidden()
    {
        var organizationId = Guid.NewGuid();
        var auditId = Guid.NewGuid();

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/organizations/{organizationId}/auditlogs/{auditId}/tags?standard=ISO27001")
            .WithTestAuth(organizationId, Permissions.Alerts.Acknowledge);
        request.Headers.Add(TestAuthenticationHandler.RoleHeader, "SecurityAnalyst");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
