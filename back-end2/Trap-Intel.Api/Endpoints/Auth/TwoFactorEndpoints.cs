using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Trap_Intel.Infrastructure.Authentication.Models;
using Trap_Intel.Infrastructure.Authentication.Services;
using Trap_Intel.Api.Endpoints.Auth.Models;
using Trap_Intel.Api.Filters;
using System.Security.Claims;

namespace Trap_Intel.Api.Endpoints.Auth;

internal sealed class TwoFactorEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth/2fa")
            .WithTags("Authentication 2FA")
            .AddEndpointFilter<ValidationFilter>();

        group.MapPost("/setup", InitiateTwoFactorSetup)
            .WithName("InitiateTwoFactorSetup")
            .WithSummary("Initiate 2FA setup")
            .WithDescription("Returns QR code and secret for authenticator app setup")
            .RequireAuthorization()
            .Produces<TwoFactorSetupResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPost("/confirm", ConfirmTwoFactorSetup)
            .WithName("ConfirmTwoFactorSetup")
            .WithSummary("Confirm 2FA setup with TOTP code")
            .WithDescription("Enables 2FA and returns backup codes")
            .RequireAuthorization()
            .Produces<TwoFactorConfirmResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPost("/verify", VerifyTwoFactorCode)
            .WithName("VerifyTwoFactorCode")
            .WithSummary("Verify 2FA code during login")
            .AllowAnonymous()
            .RequireRateLimiting("auth")
            .Produces<AuthenticationResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPost("/disable", DisableTwoFactor)
            .WithName("DisableTwoFactor")
            .WithSummary("Disable 2FA")
            .WithDescription("Requires password confirmation")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPost("/backup-codes/regenerate", RegenerateBackupCodes)
            .WithName("RegenerateBackupCodes")
            .WithSummary("Regenerate backup codes")
            .WithDescription("Requires current TOTP code for verification")
            .RequireAuthorization()
            .Produces<BackupCodesResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapGet("/status", GetTwoFactorStatus)
            .WithName("GetTwoFactorStatus")
            .WithSummary("Get 2FA status for current user")
            .RequireAuthorization()
            .Produces<TwoFactorStatusResponse>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> InitiateTwoFactorSetup(
        ITwoFactorAuthService twoFactorAuthService,
        ILogger<TwoFactorEndpoints> logger,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userIdGuid = AuthHelpers.GetCurrentUserId(httpContext);
        if (!userIdGuid.HasValue)
            return Results.Unauthorized();

        logger.LogInformation("2FA setup initiated for user: {UserId}", userIdGuid.Value);

        var result = await twoFactorAuthService.InitiateSetupAsync(userIdGuid.Value, cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Errors.FirstOrDefault();
            logger.LogWarning("2FA setup initiation failed for user {UserId}: {Error}", userIdGuid.Value, error?.Message);

            return Results.Problem(
                title: "2FA Setup Failed",
                detail: error?.Message ?? "Failed to initiate 2FA setup",
                statusCode: StatusCodes.Status400BadRequest,
                instance: httpContext.Request.Path);
        }

        return Results.Ok(new TwoFactorSetupResponse
        {
            SetupToken = result.Value.SetupToken,
            Secret = result.Value.Secret,
            SecretFormatted = result.Value.SecretFormatted,
            QrCodeData = result.Value.QrCodeData,
            OtpAuthUri = result.Value.OtpAuthUri,
            ExpiresAt = result.Value.ExpiresAt
        });
    }

    private static async Task<IResult> ConfirmTwoFactorSetup(
        ConfirmTwoFactorRequest request,
        ITwoFactorAuthService twoFactorAuthService,
        ILogger<TwoFactorEndpoints> logger,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userIdGuid = AuthHelpers.GetCurrentUserId(httpContext);
        if (!userIdGuid.HasValue)
            return Results.Unauthorized();

        logger.LogInformation("2FA setup confirmation attempt for user: {UserId}", userIdGuid.Value);

        var result = await twoFactorAuthService.ConfirmSetupAsync(
            userIdGuid.Value,
            request.SetupToken,
            request.Code,
            cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Errors.FirstOrDefault();
            logger.LogWarning("2FA setup confirmation failed for user {UserId}: {Error}", userIdGuid.Value, error?.Message);

            return Results.Problem(
                title: "2FA Setup Confirmation Failed",
                detail: error?.Message ?? "Failed to confirm 2FA setup",
                statusCode: StatusCodes.Status400BadRequest,
                instance: httpContext.Request.Path);
        }

        logger.LogInformation("2FA enabled successfully for user: {UserId}", userIdGuid.Value);

        return Results.Ok(new TwoFactorConfirmResponse
        {
            Success = result.Value.Success,
            Message = result.Value.Message,
            BackupCodes = result.Value.BackupCodes
        });
    }

    private static async Task<IResult> VerifyTwoFactorCode(
        VerifyTwoFactorRequest request,
        IAuthenticationService authService,
        ITwoFactorAuthService twoFactorAuthService,
        ILogger<TwoFactorEndpoints> logger,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var ipAddress = AuthHelpers.GetClientIpAddress(httpContext);
        var userAgent = AuthHelpers.GetUserAgent(httpContext);

        logger.LogInformation("2FA verification attempt from IP: {IpAddress}", ipAddress);

        // Validate the two-factor token and get user
        var tokenValidation = await authService.ValidateTwoFactorTokenAsync(
            request.TwoFactorToken,
            cancellationToken);

        if (tokenValidation.IsFailure)
        {
            logger.LogWarning("Invalid 2FA token from IP: {IpAddress}", ipAddress);
            return Results.Problem(
                title: "Invalid Token",
                detail: "Two-factor token is invalid or expired",
                statusCode: StatusCodes.Status400BadRequest,
                instance: httpContext.Request.Path);
        }

        var validationResult = tokenValidation.Value;
        var userId = validationResult.UserId;
        var rememberMe = request.RememberMe || validationResult.RememberMe;

        // Verify the code (TOTP or backup code)
        if (request.IsBackupCode)
        {
            var backupResult = await twoFactorAuthService.VerifyBackupCodeAsync(
                userId,
                request.Code,
                ipAddress,
                cancellationToken);

            if (backupResult.IsFailure)
            {
                logger.LogWarning("2FA backup code verification failed for user {UserId}", userId);
                return Results.Problem(
                    title: "Verification Failed",
                    detail: backupResult.Errors.FirstOrDefault()?.Message ?? "Invalid backup code",
                    statusCode: StatusCodes.Status400BadRequest,
                    instance: httpContext.Request.Path);
            }

            // Log remaining codes warning
            if (backupResult.Value <= 3)
            {
                logger.LogWarning("User {UserId} has only {RemainingCodes} backup codes remaining", userId, backupResult.Value);
            }
        }
        else
        {
            var totpResult = await twoFactorAuthService.VerifyTotpCodeAsync(
                userId,
                request.Code,
                ipAddress,
                cancellationToken);

            if (totpResult.IsFailure)
            {
                logger.LogWarning("2FA TOTP verification failed for user {UserId}", userId);
                return Results.Problem(
                    title: "Verification Failed",
                    detail: totpResult.Errors.FirstOrDefault()?.Message ?? "Invalid code",
                    statusCode: StatusCodes.Status400BadRequest,
                    instance: httpContext.Request.Path);
            }
        }

        // Complete login
        var loginResult = await authService.CompleteTwoFactorLoginAsync(
            userId,
            ipAddress,
            userAgent,
            rememberMe,
            cancellationToken);

        if (loginResult.IsFailure)
        {
            logger.LogWarning("2FA login completion failed for user {UserId}", userId);
            return Results.Problem(
                title: "Login Failed",
                detail: loginResult.Errors.FirstOrDefault()?.Message ?? "Failed to complete login",
                statusCode: StatusCodes.Status400BadRequest,
                instance: httpContext.Request.Path);
        }

        logger.LogInformation("2FA verification successful, user {UserId} logged in", userId);
        return Results.Ok(loginResult.Value);
    }

    private static async Task<IResult> DisableTwoFactor(
        DisableTwoFactorRequest request,
        ITwoFactorAuthService twoFactorAuthService,
        ILogger<TwoFactorEndpoints> logger,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userIdGuid = AuthHelpers.GetCurrentUserId(httpContext);
        if (!userIdGuid.HasValue)
            return Results.Unauthorized();

        logger.LogInformation("2FA disable request for user: {UserId}", userIdGuid.Value);

        var result = await twoFactorAuthService.DisableTwoFactorAsync(
            userIdGuid.Value,
            request.Password,
            cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Errors.FirstOrDefault();
            logger.LogWarning("2FA disable failed for user {UserId}: {Error}", userIdGuid.Value, error?.Message);

            return Results.Problem(
                title: "Failed to Disable 2FA",
                detail: error?.Message ?? "Failed to disable two-factor authentication",
                statusCode: StatusCodes.Status400BadRequest,
                instance: httpContext.Request.Path);
        }

        logger.LogInformation("2FA disabled for user: {UserId}", userIdGuid.Value);
        return Results.Ok(new { message = "Two-factor authentication has been disabled." });
    }

    private static async Task<IResult> RegenerateBackupCodes(
        RegenerateBackupCodesRequest request,
        ITwoFactorAuthService twoFactorAuthService,
        ILogger<TwoFactorEndpoints> logger,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userIdGuid = AuthHelpers.GetCurrentUserId(httpContext);
        if (!userIdGuid.HasValue)
            return Results.Unauthorized();

        logger.LogInformation("Backup codes regeneration request for user: {UserId}", userIdGuid.Value);

        var result = await twoFactorAuthService.RegenerateBackupCodesAsync(
            userIdGuid.Value,
            request.Code,
            cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Errors.FirstOrDefault();
            logger.LogWarning("Backup codes regeneration failed for user {UserId}: {Error}", userIdGuid.Value, error?.Message);

            return Results.Problem(
                title: "Failed to Regenerate Codes",
                detail: error?.Message ?? "Failed to regenerate backup codes",
                statusCode: StatusCodes.Status400BadRequest,
                instance: httpContext.Request.Path);
        }

        logger.LogInformation("Backup codes regenerated for user: {UserId}", userIdGuid.Value);
        return Results.Ok(new BackupCodesResponse
        {
            BackupCodes = result.Value,
            Message = "New backup codes have been generated. Please save these securely."
        });
    }

    private static async Task<IResult> GetTwoFactorStatus(
        ITwoFactorAuthService twoFactorAuthService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userIdGuid = AuthHelpers.GetCurrentUserId(httpContext);
        if (!userIdGuid.HasValue)
            return Results.Unauthorized();

        var isEnabled = await twoFactorAuthService.IsTwoFactorEnabledAsync(userIdGuid.Value, cancellationToken);
        var remainingCodes = isEnabled 
            ? await twoFactorAuthService.GetRemainingBackupCodeCountAsync(userIdGuid.Value, cancellationToken)
            : 0;

        return Results.Ok(new TwoFactorStatusResponse
        {
            IsEnabled = isEnabled,
            RemainingBackupCodes = remainingCodes
        });
    }
}
