namespace Trap_Intel.Tests.Integration.Infrastructure;

internal static class TestHttpRequestMessageExtensions
{
    public static HttpRequestMessage WithAuthenticatedOrganization(
        this HttpRequestMessage request,
        Guid organizationId)
    {
        request.Headers.Add(TestAuthenticationHandler.AuthHeader, "true");
        request.Headers.Add(TestAuthenticationHandler.OrganizationIdHeader, organizationId.ToString());

        return request;
    }

    public static HttpRequestMessage WithAuthenticatedOrganizationAndPermissions(
        this HttpRequestMessage request,
        Guid organizationId,
        params string[] permissions)
    {
        request.WithAuthenticatedOrganization(organizationId);

        if (permissions.Length > 0)
        {
            request.Headers.Add(
                TestAuthenticationHandler.PermissionsHeader,
                string.Join(',', permissions));
        }

        return request;
    }

    public static HttpRequestMessage WithAuthenticatedOrganizationRole(
        this HttpRequestMessage request,
        Guid organizationId,
        string role)
    {
        request.WithAuthenticatedOrganization(organizationId);
        request.Headers.Add(TestAuthenticationHandler.RoleHeader, role);

        return request;
    }

    public static HttpRequestMessage WithTestAuth(
        this HttpRequestMessage request,
        Guid organizationId,
        params string[] permissions)
    {
        request.Headers.Add(TestAuthenticationHandler.AuthHeader, "true");
        request.Headers.Add(TestAuthenticationHandler.UserIdHeader, Guid.NewGuid().ToString());
        request.Headers.Add(TestAuthenticationHandler.OrganizationIdHeader, organizationId.ToString());

        if (permissions.Length > 0)
        {
            request.Headers.Add(
                TestAuthenticationHandler.PermissionsHeader,
                string.Join(',', permissions));
        }

        return request;
    }
}
