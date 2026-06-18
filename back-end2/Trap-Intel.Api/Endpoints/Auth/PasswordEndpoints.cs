using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Trap_Intel.Application.Authentication.Queries.ValidatePassword;
using Trap_Intel.Infrastructure.Authentication.Models;
using Trap_Intel.Infrastructure.Authentication.Services;
using Trap_Intel.Api.Endpoints.Auth.Models;
using Trap_Intel.Api.Filters;

namespace Trap_Intel.Api.Endpoints.Auth;

internal sealed class PasswordEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Authentication")
            .AddEndpointFilter<ValidationFilter>();

        group.MapPost("/validate-password", ValidatePassword)
            .WithName("ValidatePassword")
            .WithSummary("Check password strength against security policy")
            .AllowAnonymous()
            .RequireRateLimiting("auth")
            .Produces<PasswordValidationResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPost("/forgot-password", ForgotPassword)
            .WithName("ForgotPassword")
            .WithSummary("Request password reset email")
            .WithDescription("Always returns success to prevent email enumeration. Rate limited per user.")
            .AllowAnonymous()
            .RequireRateLimiting("auth")
            .Produces(StatusCodes.Status200OK);

        group.MapPost("/validate-reset-token", ValidateResetToken)
            .WithName("ValidateResetToken")
            .WithSummary("Validate password reset token")
            .AllowAnonymous()
            .RequireRateLimiting("auth")
            .Produces<ValidateResetTokenResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPost("/reset-password", ResetPassword)
            .WithName("ResetPassword")
            .WithSummary("Reset password with token")
            .AllowAnonymous()
            .RequireRateLimiting("auth")
            .Produces(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> ValidatePassword(
        PasswordValidationRequest request,
        ISender sender,
        HttpContext httpContext)
    {
        var result = await sender.Send(new ValidatePasswordQuery(request.Password));

        if (result.IsFailure)
        {
            return Results.Problem(
                title: "Password Validation Failed",
                detail: string.Join(" ", result.Errors.Select(e => e.Message)),
                statusCode: StatusCodes.Status400BadRequest,
                instance: httpContext.Request.Path);
        }

        return Results.Ok(new PasswordValidationResponse { Message = result.Value.Message, IsValid = result.Value.IsValid });
    }

    private static async Task<IResult> ForgotPassword(
        ForgotPasswordRequest request,
        IEmailTokenService emailTokenService,
        ILogger<PasswordEndpoints> logger,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var sanitizedEmail = AuthHelpers.SanitizeForLogging(request.Email);
        var ipAddress = AuthHelpers.GetClientIpAddress(httpContext);
        var userAgent = httpContext.Request.Headers.UserAgent.ToString();

        logger.LogInformation("Password reset request for: {Email} from IP: {IpAddress}", sanitizedEmail, ipAddress);

        await emailTokenService.RequestPasswordResetAsync(
            request.Email,
            ipAddress,
            userAgent,
            cancellationToken);

        return Results.Ok(new { message = "If the email exists, a password reset link has been sent." });
    }

    private static async Task<IResult> ValidateResetToken(
        ValidateResetTokenRequest request,
        IEmailTokenService emailTokenService,
        ILogger<PasswordEndpoints> logger,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Password reset token validation attempt");

        var result = await emailTokenService.ValidatePasswordResetTokenAsync(request.Token, cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Errors.FirstOrDefault();
            logger.LogWarning("Password reset token validation failed: {Error}", error?.Message);

            return Results.Problem(
                title: "Invalid Token",
                detail: error?.Message ?? "Invalid or expired password reset token",
                statusCode: StatusCodes.Status400BadRequest,
                instance: httpContext.Request.Path);
        }

        return Results.Ok(new ValidateResetTokenResponse 
        { 
            IsValid = true, 
            Message = "Token is valid. You can reset your password." 
        });
    }

    private static async Task<IResult> ResetPassword(
        ResetPasswordRequest request,
        IEmailTokenService emailTokenService,
        ILogger<PasswordEndpoints> logger,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var ipAddress = AuthHelpers.GetClientIpAddress(httpContext);

        logger.LogInformation("Password reset attempt from IP: {IpAddress}", ipAddress);

        var result = await emailTokenService.ResetPasswordAsync(
            request.Token,
            request.NewPassword,
            ipAddress,
            cancellationToken);

        if (result.IsFailure)
        {
            return Results.Problem(
                title: "Password Reset Failed",
                detail: result.Errors.FirstOrDefault()?.Message ?? "Failed to reset password",
                statusCode: StatusCodes.Status400BadRequest,
                instance: httpContext.Request.Path);
        }

        logger.LogInformation("Password reset successful");
        return Results.Ok(new { message = "Password has been successfully reset." });
    }
}
