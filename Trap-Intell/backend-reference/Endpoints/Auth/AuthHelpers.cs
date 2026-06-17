using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Trap_Intel.Api.Endpoints.Auth;

internal static class AuthHelpers
{
    public static string GetClientIpAddress(HttpContext httpContext)
    {
        // Implementation extracts client IP from request headers or connection
    }

    public static string GetUserAgent(HttpContext httpContext)
    {
        // Implementation extracts User-Agent header
    }

    public static string SanitizeForLogging(string input)
    {
        // Implementation removes sensitive data from logs
    }

    public static string SanitizeIpAddress(string ip)
    {
        // Implementation masks IP addresses for privacy
    }

    public static Guid? GetCurrentUserId(HttpContext httpContext)
    {
        // Implementation extracts User ID from claims
    }
}
