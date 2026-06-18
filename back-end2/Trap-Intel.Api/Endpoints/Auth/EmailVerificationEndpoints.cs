using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Trap_Intel.Infrastructure.Authentication.Models;
using Trap_Intel.Infrastructure.Authentication.Services;
using Trap_Intel.Api.Endpoints.Auth.Models;
using Trap_Intel.Api.Filters;
using System.Security.Claims;

namespace Trap_Intel.Api.Endpoints.Auth;

internal sealed class EmailVerificationEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Authentication")
            .AddEndpointFilter<ValidationFilter>();

        group.MapPost("/verify-email", VerifyEmail)
            .WithName("VerifyEmail")
            .WithSummary("Verify email address with token")
            .AllowAnonymous()
            .RequireRateLimiting("auth")
            .Produces(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPost("/resend-verification", ResendEmailVerification)
            .WithName("ResendEmailVerification")
            .WithSummary("Resend email verification link")
            .WithDescription("Always returns success to prevent email enumeration")
            .AllowAnonymous()
            .RequireRateLimiting("auth")
            .Produces(StatusCodes.Status200OK);
    }

    private static async Task<IResult> VerifyEmail(
        VerifyEmailRequest request,
        IEmailTokenService emailTokenService,
        ILogger<EmailVerificationEndpoints> logger,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.UserId, out var userId))
        {
            return Results.Problem(
                title: "Invalid Request",
                detail: "Invalid user ID format",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var result = await emailTokenService.VerifyEmailAsync(request.Token, cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Errors.FirstOrDefault();
            logger.LogWarning("Email verification failed for user {UserId}: {Error}", userId, error?.Message);

            return Results.Problem(
                title: "Verification Failed",
                detail: error?.Message ?? "Invalid or expired verification token",
                statusCode: StatusCodes.Status400BadRequest,
                instance: httpContext.Request.Path);
        }

        logger.LogInformation("Email verified successfully for user {UserId}", userId);
        return Results.Ok(new { message = "Email verified successfully. You can now login." });
    }

    private static async Task<IResult> ResendEmailVerification(
        ResendVerificationRequest request,
        IEmailTokenService emailTokenService,
        ILogger<EmailVerificationEndpoints> logger,
        CancellationToken cancellationToken)
    {
        var sanitizedEmail = AuthHelpers.SanitizeForLogging(request.Email);
        logger.LogInformation("Resend email verification requested for: {Email}", sanitizedEmail);

        // This method should return success even if user doesn't exist or is already verified
        // to prevent email enumeration attacks
        await emailTokenService.ResendEmailVerificationAsync(request.Email, cancellationToken);

        return Results.Ok(new { message = "If the email exists and is not verified, a verification link has been sent." });
    }
}
