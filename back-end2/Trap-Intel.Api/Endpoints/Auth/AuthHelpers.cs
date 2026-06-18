using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Trap_Intel.Api.Endpoints.Auth;

internal static class AuthHelpers
{
    public static string GetClientIpAddress(HttpContext httpContext)
    {
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var clientIp = forwardedFor.Split(',').First().Trim();
            return SanitizeIpAddress(clientIp);
        }

        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    public static string GetUserAgent(HttpContext httpContext)
    {
        var userAgent = httpContext.Request.Headers.UserAgent.ToString();
        return userAgent.Length > 500 ? userAgent[..500] : userAgent;
    }

    public static string SanitizeForLogging(string input)
    {
        if (string.IsNullOrEmpty(input))
            return "[empty]";

        var sanitized = input
            .Replace("\r", "")
            .Replace("\n", "")
            .Replace("\t", "");

        return sanitized.Length > 100 ? sanitized[..100] + "..." : sanitized;
    }

    public static string SanitizeIpAddress(string ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
            return "unknown";

        var sanitized = new string(ip.Where(c => char.IsDigit(c) || c == '.' || c == ':').ToArray());
        return sanitized.Length > 45 ? sanitized[..45] : sanitized;
    }

    public static Guid? GetCurrentUserId(HttpContext httpContext)
    {
        var userIdClaim = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? httpContext.User.FindFirst("sub")?.Value;

        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}

