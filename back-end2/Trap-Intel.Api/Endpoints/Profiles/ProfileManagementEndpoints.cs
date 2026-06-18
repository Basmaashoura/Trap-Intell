using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Trap_Intel.Api.Authorization;
using Trap_Intel.Api.Extensions;
using Trap_Intel.Application.Abstractions.Media;
using Trap_Intel.Application.Users.Commands.UpdateCurrentUserProfile;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Identity;
using Trap_Intel.Domain.Identity.Authorization;
using Trap_Intel.Domain.Organizations;

namespace Trap_Intel.Api.Endpoints.Profiles;

internal sealed class ProfileManagementEndpoints : IEndpoint
{
    private const long MaxImageBytes = 5 * 1024 * 1024;

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/profile")
            .WithTags("Profiles")
            .RequireAuthorization();

        group.MapGet("/me", GetMyProfile)
            .WithName("GetMyProfile")
            .WithSummary("Get current user rich profile")
            .Produces<CurrentUserProfileResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPatch("/me", PatchMyProfile)
            .WithName("PatchMyProfile")
            .WithSummary("Patch current user rich profile")
            .Produces<CurrentUserProfileResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/me/avatar", UploadMyAvatar)
            .WithName("UploadMyAvatar")
            .WithSummary("Upload current user avatar image")
            .Accepts<IFormFile>("multipart/form-data")
            .DisableAntiforgery()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapDelete("/me/avatar", DeleteMyAvatar)
            .WithName("DeleteMyAvatar")
            .WithSummary("Delete current user avatar image")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/me/cover", UploadMyCover)
            .WithName("UploadMyCover")
            .WithSummary("Upload current user cover image")
            .Accepts<IFormFile>("multipart/form-data")
            .DisableAntiforgery()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapDelete("/me/cover", DeleteMyCover)
            .WithName("DeleteMyCover")
            .WithSummary("Delete current user cover image")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/me/media/batch", UploadMyMediaBatch)
            .WithName("UploadMyMediaBatch")
            .WithSummary("Upload avatar and cover in parallel for current user")
            .Accepts<UserBatchMediaUploadRequest>("multipart/form-data")
            .DisableAntiforgery()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/organizations/{organizationId:guid}", GetOrganizationProfile)
            .WithName("GetOrganizationProfile")
            .WithSummary("Get rich organization profile")
            .RequirePermission(Permissions.Organization.View)
            .Produces<OrganizationProfileResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPatch("/organizations/{organizationId:guid}", PatchOrganizationProfile)
            .WithName("PatchOrganizationProfile")
            .WithSummary("Patch rich organization profile")
            .RequirePermission(Permissions.Organization.Update)
            .Produces<OrganizationProfileResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/organizations/{organizationId:guid}/logo", UploadOrganizationLogo)
            .WithName("UploadOrganizationLogo")
            .WithSummary("Upload organization logo")
            .RequirePermission(Permissions.Organization.Update)
            .Accepts<IFormFile>("multipart/form-data")
            .DisableAntiforgery()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/organizations/{organizationId:guid}/logo", DeleteOrganizationLogo)
            .WithName("DeleteOrganizationLogo")
            .WithSummary("Delete organization logo")
            .RequirePermission(Permissions.Organization.Update)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/organizations/{organizationId:guid}/cover", UploadOrganizationCover)
            .WithName("UploadOrganizationCover")
            .WithSummary("Upload organization cover image")
            .RequirePermission(Permissions.Organization.Update)
            .Accepts<IFormFile>("multipart/form-data")
            .DisableAntiforgery()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/organizations/{organizationId:guid}/cover", DeleteOrganizationCover)
            .WithName("DeleteOrganizationCover")
            .WithSummary("Delete organization cover image")
            .RequirePermission(Permissions.Organization.Update)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/organizations/{organizationId:guid}/media/batch", UploadOrganizationMediaBatch)
            .WithName("UploadOrganizationMediaBatch")
            .WithSummary("Upload organization logo and cover in parallel")
            .RequirePermission(Permissions.Organization.Update)
            .Accepts<OrganizationBatchMediaUploadRequest>("multipart/form-data")
            .DisableAntiforgery()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetMyProfile(
        IUserRepository userRepository,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(httpContext, out var userId))
        {
            return Results.Unauthorized();
        }

        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        return Results.Ok(MapUserProfile(user));
    }

    private static async Task<IResult> PatchMyProfile(
        [FromBody] PatchCurrentUserProfileRequest request,
        ISender sender,
        IUserRepository userRepository,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(httpContext, out var userId))
        {
            return Results.Unauthorized();
        }

        var command = new UpdateCurrentUserProfileCommand(
            userId,
            request.FirstName,
            request.LastName,
            request.PhoneNumber,
            request.JobTitle,
            request.Department,
            request.Location,
            request.Bio,
            request.WebsiteUrl,
            request.LinkedInUrl,
            request.GitHubUrl,
            request.XUrl);

        var result = await sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            return ToProblem(result, "Failed to update user profile");
        }

        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        return Results.Ok(MapUserProfile(user));
    }

    private static async Task<IResult> UploadMyAvatar(
        IFormFile file,
        IUserRepository userRepository,
        IMediaStorageService mediaStorageService,
        IUnitOfWork unitOfWork,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateImage(file);
        if (validationError is not null)
        {
            return Results.BadRequest(new { message = validationError });
        }

        if (!TryGetCurrentUserId(httpContext, out var userId))
        {
            return Results.Unauthorized();
        }

        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        await using var stream = file.OpenReadStream();
        var uploadResult = await mediaStorageService.UploadImageAsync(
            new MediaUploadRequest(
                stream,
                file.FileName,
                file.ContentType ?? "application/octet-stream",
                MediaAssetScope.UserAvatar,
                user.Id,
                file.Length),
            cancellationToken);

        if (uploadResult.IsFailure)
        {
            return ToProblem(uploadResult, "Failed to upload avatar");
        }

        var previousPublicId = user.AvatarPublicId;
        var mediaUrl = ResolveAbsoluteMediaUrl(uploadResult.Value.Url, httpContext);
        var setResult = user.SetAvatar(mediaUrl, uploadResult.Value.PublicId);
        if (setResult.IsFailure)
        {
            await mediaStorageService.DeleteImageAsync(uploadResult.Value.PublicId, cancellationToken);
            return ToProblem(setResult, "Failed to save avatar metadata");
        }

        await userRepository.UpdateAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await TryDeletePreviousMediaAsync(previousPublicId, uploadResult.Value.PublicId, mediaStorageService, cancellationToken);

        return Results.Ok(new
        {
            message = "Avatar uploaded successfully.",
            user.AvatarUrl,
            user.AvatarPublicId
        });
    }

    private static async Task<IResult> DeleteMyAvatar(
        IUserRepository userRepository,
        IMediaStorageService mediaStorageService,
        IUnitOfWork unitOfWork,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(httpContext, out var userId))
        {
            return Results.Unauthorized();
        }

        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(user.AvatarUrl) && string.IsNullOrWhiteSpace(user.AvatarPublicId))
        {
            return Results.NotFound(new { message = "No avatar image found for current user." });
        }

        var previousPublicId = user.AvatarPublicId;
        var setResult = user.SetAvatar(null, null);
        if (setResult.IsFailure)
        {
            return ToProblem(setResult, "Failed to clear avatar metadata");
        }

        await userRepository.UpdateAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(previousPublicId))
        {
            await mediaStorageService.DeleteImageAsync(previousPublicId, cancellationToken);
        }

        return Results.Ok(new { message = "Avatar deleted successfully." });
    }

    private static async Task<IResult> UploadMyCover(
        IFormFile file,
        IUserRepository userRepository,
        IMediaStorageService mediaStorageService,
        IUnitOfWork unitOfWork,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateImage(file);
        if (validationError is not null)
        {
            return Results.BadRequest(new { message = validationError });
        }

        if (!TryGetCurrentUserId(httpContext, out var userId))
        {
            return Results.Unauthorized();
        }

        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        await using var stream = file.OpenReadStream();
        var uploadResult = await mediaStorageService.UploadImageAsync(
            new MediaUploadRequest(
                stream,
                file.FileName,
                file.ContentType ?? "application/octet-stream",
                MediaAssetScope.UserCover,
                user.Id,
                file.Length),
            cancellationToken);

        if (uploadResult.IsFailure)
        {
            return ToProblem(uploadResult, "Failed to upload cover image");
        }

        var previousPublicId = user.CoverImagePublicId;
        var mediaUrl = ResolveAbsoluteMediaUrl(uploadResult.Value.Url, httpContext);
        var setResult = user.SetCoverImage(mediaUrl, uploadResult.Value.PublicId);
        if (setResult.IsFailure)
        {
            await mediaStorageService.DeleteImageAsync(uploadResult.Value.PublicId, cancellationToken);
            return ToProblem(setResult, "Failed to save cover image metadata");
        }

        await userRepository.UpdateAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await TryDeletePreviousMediaAsync(previousPublicId, uploadResult.Value.PublicId, mediaStorageService, cancellationToken);

        return Results.Ok(new
        {
            message = "Cover image uploaded successfully.",
            user.CoverImageUrl,
            user.CoverImagePublicId
        });
    }

    private static async Task<IResult> DeleteMyCover(
        IUserRepository userRepository,
        IMediaStorageService mediaStorageService,
        IUnitOfWork unitOfWork,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(httpContext, out var userId))
        {
            return Results.Unauthorized();
        }

        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(user.CoverImageUrl) && string.IsNullOrWhiteSpace(user.CoverImagePublicId))
        {
            return Results.NotFound(new { message = "No cover image found for current user." });
        }

        var previousPublicId = user.CoverImagePublicId;
        var setResult = user.SetCoverImage(null, null);
        if (setResult.IsFailure)
        {
            return ToProblem(setResult, "Failed to clear cover image metadata");
        }

        await userRepository.UpdateAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(previousPublicId))
        {
            await mediaStorageService.DeleteImageAsync(previousPublicId, cancellationToken);
        }

        return Results.Ok(new { message = "Cover image deleted successfully." });
    }

    private static async Task<IResult> UploadMyMediaBatch(
        [FromForm] UserBatchMediaUploadRequest request,
        IUserRepository userRepository,
        IMediaStorageService mediaStorageService,
        IUnitOfWork unitOfWork,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(httpContext, out var userId))
        {
            return Results.Unauthorized();
        }

        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        var files = new List<(MediaAssetScope Scope, IFormFile File)>();
        if (request.Avatar is not null)
        {
            files.Add((MediaAssetScope.UserAvatar, request.Avatar));
        }

        if (request.Cover is not null)
        {
            files.Add((MediaAssetScope.UserCover, request.Cover));
        }

        if (files.Count == 0)
        {
            return Results.BadRequest(new { message = "Provide at least one file: avatar or cover." });
        }

        foreach (var (_, file) in files)
        {
            var validationError = ValidateImage(file);
            if (validationError is not null)
            {
                return Results.BadRequest(new { message = validationError });
            }
        }

        var streams = new List<Stream>();
        try
        {
            var uploadRequests = new List<MediaUploadRequest>(files.Count);
            foreach (var (scope, file) in files)
            {
                var stream = file.OpenReadStream();
                streams.Add(stream);

                uploadRequests.Add(new MediaUploadRequest(
                    stream,
                    file.FileName,
                    file.ContentType ?? "application/octet-stream",
                    scope,
                    user.Id,
                    file.Length));
            }

            var uploadResult = await mediaStorageService.UploadImagesInParallelAsync(uploadRequests, cancellationToken);
            if (uploadResult.IsFailure)
            {
                return ToProblem(uploadResult, "Failed to upload media batch");
            }

            var previousAvatarPublicId = user.AvatarPublicId;
            var previousCoverPublicId = user.CoverImagePublicId;

            var avatarUpload = uploadResult.Value.FirstOrDefault(x => x.Scope == MediaAssetScope.UserAvatar);
            if (avatarUpload is not null)
            {
                var avatarUrl = ResolveAbsoluteMediaUrl(avatarUpload.Url, httpContext);
                var setAvatar = user.SetAvatar(avatarUrl, avatarUpload.PublicId);
                if (setAvatar.IsFailure)
                {
                    await CleanupUploadedMediaAsync(uploadResult.Value, mediaStorageService, cancellationToken);
                    return ToProblem(setAvatar, "Failed to save avatar metadata");
                }
            }

            var coverUpload = uploadResult.Value.FirstOrDefault(x => x.Scope == MediaAssetScope.UserCover);
            if (coverUpload is not null)
            {
                var coverUrl = ResolveAbsoluteMediaUrl(coverUpload.Url, httpContext);
                var setCover = user.SetCoverImage(coverUrl, coverUpload.PublicId);
                if (setCover.IsFailure)
                {
                    await CleanupUploadedMediaAsync(uploadResult.Value, mediaStorageService, cancellationToken);
                    return ToProblem(setCover, "Failed to save cover metadata");
                }
            }

            await userRepository.UpdateAsync(user, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            await TryDeletePreviousMediaAsync(previousAvatarPublicId, avatarUpload?.PublicId, mediaStorageService, cancellationToken);
            await TryDeletePreviousMediaAsync(previousCoverPublicId, coverUpload?.PublicId, mediaStorageService, cancellationToken);

            return Results.Ok(new
            {
                message = "Profile media batch uploaded successfully.",
                user.AvatarUrl,
                user.CoverImageUrl,
                Uploaded = uploadResult.Value.Select(x => new
                {
                    Scope = x.Scope.ToString(),
                    Url = ResolveAbsoluteMediaUrl(x.Url, httpContext),
                    x.PublicId,
                    x.Width,
                    x.Height,
                    x.Bytes
                })
            });
        }
        finally
        {
            foreach (var stream in streams)
            {
                await stream.DisposeAsync();
            }
        }
    }

    private static async Task<IResult> GetOrganizationProfile(
        Guid organizationId,
        IOrganizationRepository organizationRepository,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!IsAuthorizedForOrganization(httpContext, organizationId))
        {
            return Results.Forbid();
        }

        var organization = await organizationRepository.GetByIdAsync(organizationId, cancellationToken);
        if (organization is null)
        {
            return Results.NotFound(new { message = "Organization not found." });
        }

        return Results.Ok(MapOrganizationProfile(organization));
    }

    private static async Task<IResult> PatchOrganizationProfile(
        Guid organizationId,
        [FromBody] PatchOrganizationProfileRequest request,
        IOrganizationRepository organizationRepository,
        IUnitOfWork unitOfWork,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!IsAuthorizedForOrganization(httpContext, organizationId))
        {
            return Results.Forbid();
        }

        var organization = await organizationRepository.GetByIdAsync(organizationId, cancellationToken);
        if (organization is null)
        {
            return Results.NotFound(new { message = "Organization not found." });
        }

        var updateResult = organization.UpdateProfile(
            request.Tagline,
            request.Description,
            request.SupportEmail,
            request.SupportPhone,
            request.HeadquartersLocation,
            request.LinkedInUrl,
            request.XUrl);

        if (updateResult.IsFailure)
        {
            return ToProblem(updateResult, "Failed to update organization profile");
        }

        await organizationRepository.UpdateAsync(organization, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Results.Ok(MapOrganizationProfile(organization));
    }

    private static async Task<IResult> UploadOrganizationLogo(
        Guid organizationId,
        IFormFile file,
        IOrganizationRepository organizationRepository,
        IMediaStorageService mediaStorageService,
        IUnitOfWork unitOfWork,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateImage(file);
        if (validationError is not null)
        {
            return Results.BadRequest(new { message = validationError });
        }

        if (!IsAuthorizedForOrganization(httpContext, organizationId))
        {
            return Results.Forbid();
        }

        var organization = await organizationRepository.GetByIdAsync(organizationId, cancellationToken);
        if (organization is null)
        {
            return Results.NotFound(new { message = "Organization not found." });
        }

        await using var stream = file.OpenReadStream();
        var uploadResult = await mediaStorageService.UploadImageAsync(
            new MediaUploadRequest(
                stream,
                file.FileName,
                file.ContentType ?? "application/octet-stream",
                MediaAssetScope.OrganizationLogo,
                organization.Id,
                file.Length),
            cancellationToken);

        if (uploadResult.IsFailure)
        {
            return ToProblem(uploadResult, "Failed to upload organization logo");
        }

        var previousPublicId = organization.LogoPublicId;
        var mediaUrl = ResolveAbsoluteMediaUrl(uploadResult.Value.Url, httpContext);
        var setResult = organization.SetLogo(mediaUrl, uploadResult.Value.PublicId);
        if (setResult.IsFailure)
        {
            await mediaStorageService.DeleteImageAsync(uploadResult.Value.PublicId, cancellationToken);
            return ToProblem(setResult, "Failed to save organization logo metadata");
        }

        await organizationRepository.UpdateAsync(organization, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await TryDeletePreviousMediaAsync(previousPublicId, uploadResult.Value.PublicId, mediaStorageService, cancellationToken);

        return Results.Ok(new
        {
            message = "Organization logo uploaded successfully.",
            organization.LogoUrl,
            organization.LogoPublicId
        });
    }

    private static async Task<IResult> DeleteOrganizationLogo(
        Guid organizationId,
        IOrganizationRepository organizationRepository,
        IMediaStorageService mediaStorageService,
        IUnitOfWork unitOfWork,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!IsAuthorizedForOrganization(httpContext, organizationId))
        {
            return Results.Forbid();
        }

        var organization = await organizationRepository.GetByIdAsync(organizationId, cancellationToken);
        if (organization is null)
        {
            return Results.NotFound(new { message = "Organization not found." });
        }

        if (string.IsNullOrWhiteSpace(organization.LogoUrl) && string.IsNullOrWhiteSpace(organization.LogoPublicId))
        {
            return Results.NotFound(new { message = "Organization logo is not set." });
        }

        var previousPublicId = organization.LogoPublicId;
        var setResult = organization.SetLogo(null, null);
        if (setResult.IsFailure)
        {
            return ToProblem(setResult, "Failed to clear organization logo metadata");
        }

        await organizationRepository.UpdateAsync(organization, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(previousPublicId))
        {
            await mediaStorageService.DeleteImageAsync(previousPublicId, cancellationToken);
        }

        return Results.Ok(new { message = "Organization logo deleted successfully." });
    }

    private static async Task<IResult> UploadOrganizationCover(
        Guid organizationId,
        IFormFile file,
        IOrganizationRepository organizationRepository,
        IMediaStorageService mediaStorageService,
        IUnitOfWork unitOfWork,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateImage(file);
        if (validationError is not null)
        {
            return Results.BadRequest(new { message = validationError });
        }

        if (!IsAuthorizedForOrganization(httpContext, organizationId))
        {
            return Results.Forbid();
        }

        var organization = await organizationRepository.GetByIdAsync(organizationId, cancellationToken);
        if (organization is null)
        {
            return Results.NotFound(new { message = "Organization not found." });
        }

        await using var stream = file.OpenReadStream();
        var uploadResult = await mediaStorageService.UploadImageAsync(
            new MediaUploadRequest(
                stream,
                file.FileName,
                file.ContentType ?? "application/octet-stream",
                MediaAssetScope.OrganizationCover,
                organization.Id,
                file.Length),
            cancellationToken);

        if (uploadResult.IsFailure)
        {
            return ToProblem(uploadResult, "Failed to upload organization cover image");
        }

        var previousPublicId = organization.CoverImagePublicId;
        var mediaUrl = ResolveAbsoluteMediaUrl(uploadResult.Value.Url, httpContext);
        var setResult = organization.SetCoverImage(mediaUrl, uploadResult.Value.PublicId);
        if (setResult.IsFailure)
        {
            await mediaStorageService.DeleteImageAsync(uploadResult.Value.PublicId, cancellationToken);
            return ToProblem(setResult, "Failed to save organization cover metadata");
        }

        await organizationRepository.UpdateAsync(organization, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await TryDeletePreviousMediaAsync(previousPublicId, uploadResult.Value.PublicId, mediaStorageService, cancellationToken);

        return Results.Ok(new
        {
            message = "Organization cover uploaded successfully.",
            organization.CoverImageUrl,
            organization.CoverImagePublicId
        });
    }

    private static async Task<IResult> DeleteOrganizationCover(
        Guid organizationId,
        IOrganizationRepository organizationRepository,
        IMediaStorageService mediaStorageService,
        IUnitOfWork unitOfWork,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!IsAuthorizedForOrganization(httpContext, organizationId))
        {
            return Results.Forbid();
        }

        var organization = await organizationRepository.GetByIdAsync(organizationId, cancellationToken);
        if (organization is null)
        {
            return Results.NotFound(new { message = "Organization not found." });
        }

        if (string.IsNullOrWhiteSpace(organization.CoverImageUrl) && string.IsNullOrWhiteSpace(organization.CoverImagePublicId))
        {
            return Results.NotFound(new { message = "Organization cover image is not set." });
        }

        var previousPublicId = organization.CoverImagePublicId;
        var setResult = organization.SetCoverImage(null, null);
        if (setResult.IsFailure)
        {
            return ToProblem(setResult, "Failed to clear organization cover metadata");
        }

        await organizationRepository.UpdateAsync(organization, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(previousPublicId))
        {
            await mediaStorageService.DeleteImageAsync(previousPublicId, cancellationToken);
        }

        return Results.Ok(new { message = "Organization cover image deleted successfully." });
    }

    private static async Task<IResult> UploadOrganizationMediaBatch(
        Guid organizationId,
        [FromForm] OrganizationBatchMediaUploadRequest request,
        IOrganizationRepository organizationRepository,
        IMediaStorageService mediaStorageService,
        IUnitOfWork unitOfWork,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!IsAuthorizedForOrganization(httpContext, organizationId))
        {
            return Results.Forbid();
        }

        var organization = await organizationRepository.GetByIdAsync(organizationId, cancellationToken);
        if (organization is null)
        {
            return Results.NotFound(new { message = "Organization not found." });
        }

        var files = new List<(MediaAssetScope Scope, IFormFile File)>();
        if (request.Logo is not null)
        {
            files.Add((MediaAssetScope.OrganizationLogo, request.Logo));
        }

        if (request.Cover is not null)
        {
            files.Add((MediaAssetScope.OrganizationCover, request.Cover));
        }

        if (files.Count == 0)
        {
            return Results.BadRequest(new { message = "Provide at least one file: logo or cover." });
        }

        foreach (var (_, file) in files)
        {
            var validationError = ValidateImage(file);
            if (validationError is not null)
            {
                return Results.BadRequest(new { message = validationError });
            }
        }

        var streams = new List<Stream>();
        try
        {
            var uploadRequests = new List<MediaUploadRequest>(files.Count);
            foreach (var (scope, file) in files)
            {
                var stream = file.OpenReadStream();
                streams.Add(stream);

                uploadRequests.Add(new MediaUploadRequest(
                    stream,
                    file.FileName,
                    file.ContentType ?? "application/octet-stream",
                    scope,
                    organization.Id,
                    file.Length));
            }

            var uploadResult = await mediaStorageService.UploadImagesInParallelAsync(uploadRequests, cancellationToken);
            if (uploadResult.IsFailure)
            {
                return ToProblem(uploadResult, "Failed to upload organization media batch");
            }

            var previousLogoPublicId = organization.LogoPublicId;
            var previousCoverPublicId = organization.CoverImagePublicId;

            var logoUpload = uploadResult.Value.FirstOrDefault(x => x.Scope == MediaAssetScope.OrganizationLogo);
            if (logoUpload is not null)
            {
                var logoUrl = ResolveAbsoluteMediaUrl(logoUpload.Url, httpContext);
                var setLogo = organization.SetLogo(logoUrl, logoUpload.PublicId);
                if (setLogo.IsFailure)
                {
                    await CleanupUploadedMediaAsync(uploadResult.Value, mediaStorageService, cancellationToken);
                    return ToProblem(setLogo, "Failed to save organization logo metadata");
                }
            }

            var coverUpload = uploadResult.Value.FirstOrDefault(x => x.Scope == MediaAssetScope.OrganizationCover);
            if (coverUpload is not null)
            {
                var coverUrl = ResolveAbsoluteMediaUrl(coverUpload.Url, httpContext);
                var setCover = organization.SetCoverImage(coverUrl, coverUpload.PublicId);
                if (setCover.IsFailure)
                {
                    await CleanupUploadedMediaAsync(uploadResult.Value, mediaStorageService, cancellationToken);
                    return ToProblem(setCover, "Failed to save organization cover metadata");
                }
            }

            await organizationRepository.UpdateAsync(organization, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            await TryDeletePreviousMediaAsync(previousLogoPublicId, logoUpload?.PublicId, mediaStorageService, cancellationToken);
            await TryDeletePreviousMediaAsync(previousCoverPublicId, coverUpload?.PublicId, mediaStorageService, cancellationToken);

            return Results.Ok(new
            {
                message = "Organization media batch uploaded successfully.",
                organization.LogoUrl,
                organization.CoverImageUrl,
                Uploaded = uploadResult.Value.Select(x => new
                {
                    Scope = x.Scope.ToString(),
                    Url = ResolveAbsoluteMediaUrl(x.Url, httpContext),
                    x.PublicId,
                    x.Width,
                    x.Height,
                    x.Bytes
                })
            });
        }
        finally
        {
            foreach (var stream in streams)
            {
                await stream.DisposeAsync();
            }
        }
    }

    private static CurrentUserProfileResponse MapUserProfile(User user)
    {
        return new CurrentUserProfileResponse(
            user.Id,
            user.OrganizationId,
            user.Email.Value,
            user.UserName.Value,
            user.FirstName.Value,
            user.LastName.Value,
            user.FullName,
            user.PhoneNumber,
            user.JobTitle,
            user.Department,
            user.Location,
            user.Bio,
            user.WebsiteUrl,
            user.LinkedInUrl,
            user.GitHubUrl,
            user.XUrl,
            user.AvatarUrl,
            user.CoverImageUrl,
            user.UpdatedAt,
            user.CreatedAt);
    }

    private static OrganizationProfileResponse MapOrganizationProfile(Organization organization)
    {
        return new OrganizationProfileResponse(
            organization.Id,
            organization.Name,
            organization.Industry,
            organization.Website,
            organization.Tagline,
            organization.Description,
            organization.SupportEmail,
            organization.SupportPhone,
            organization.HeadquartersLocation,
            organization.LinkedInUrl,
            organization.XUrl,
            organization.LogoUrl,
            organization.CoverImageUrl,
            organization.UpdatedAt,
            organization.CreatedAt);
    }

    private static string? ValidateImage(IFormFile? file)
    {
        if (file is null)
        {
            return "File is required.";
        }

        if (file.Length <= 0)
        {
            return "Uploaded file is empty.";
        }

        if (file.Length > MaxImageBytes)
        {
            return $"File size must be less than {MaxImageBytes / (1024 * 1024)} MB.";
        }

        if (string.IsNullOrWhiteSpace(file.ContentType) ||
            !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return "Only image files are supported.";
        }

        return null;
    }

    private static bool TryGetCurrentUserId(HttpContext httpContext, out Guid userId)
    {
        var claimValue = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? httpContext.User.FindFirstValue("sub");

        return Guid.TryParse(claimValue, out userId);
    }

    private static bool IsAuthorizedForOrganization(HttpContext httpContext, Guid organizationId)
    {
        return httpContext.User.IsAuthorizedForOrganization(organizationId);
    }

    private static string ResolveAbsoluteMediaUrl(string url, HttpContext httpContext)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return url;
        }

        if (Uri.TryCreate(url, UriKind.Absolute, out var parsed) &&
            (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps))
        {
            return url;
        }

        var normalizedPath = url.StartsWith('/') ? url : $"/{url}";
        return $"{httpContext.Request.Scheme}://{httpContext.Request.Host}{normalizedPath}";
    }

    private static IResult ToProblem(Result result, string title)
    {
        var firstError = result.Errors.FirstOrDefault();
        return Results.Problem(
            title: title,
            detail: firstError?.Message,
            statusCode: StatusCodes.Status400BadRequest);
    }

    private static async Task TryDeletePreviousMediaAsync(
        string? previousPublicId,
        string? currentPublicId,
        IMediaStorageService mediaStorageService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(previousPublicId))
        {
            return;
        }

        if (string.Equals(previousPublicId, currentPublicId, StringComparison.Ordinal))
        {
            return;
        }

        await mediaStorageService.DeleteImageAsync(previousPublicId, cancellationToken);
    }

    private static async Task CleanupUploadedMediaAsync(
        IReadOnlyList<MediaUploadResult> uploads,
        IMediaStorageService mediaStorageService,
        CancellationToken cancellationToken)
    {
        foreach (var upload in uploads)
        {
            if (!string.IsNullOrWhiteSpace(upload.PublicId))
            {
                await mediaStorageService.DeleteImageAsync(upload.PublicId, cancellationToken);
            }
        }
    }
}

public sealed record CurrentUserProfileResponse(
    Guid Id,
    Guid OrganizationId,
    string Email,
    string UserName,
    string FirstName,
    string LastName,
    string FullName,
    string? PhoneNumber,
    string? JobTitle,
    string? Department,
    string? Location,
    string? Bio,
    string? WebsiteUrl,
    string? LinkedInUrl,
    string? GitHubUrl,
    string? XUrl,
    string? AvatarUrl,
    string? CoverImageUrl,
    DateTime UpdatedAt,
    DateTime CreatedAt);

public sealed record OrganizationProfileResponse(
    Guid Id,
    string Name,
    string Industry,
    string Website,
    string? Tagline,
    string? Description,
    string? SupportEmail,
    string? SupportPhone,
    string? HeadquartersLocation,
    string? LinkedInUrl,
    string? XUrl,
    string? LogoUrl,
    string? CoverImageUrl,
    DateTime UpdatedAt,
    DateTime CreatedAt);

public sealed record PatchCurrentUserProfileRequest
{
    [MaxLength(100)]
    public string? FirstName { get; init; }

    [MaxLength(100)]
    public string? LastName { get; init; }

    [MaxLength(20)]
    public string? PhoneNumber { get; init; }

    [MaxLength(120)]
    public string? JobTitle { get; init; }

    [MaxLength(120)]
    public string? Department { get; init; }

    [MaxLength(200)]
    public string? Location { get; init; }

    [MaxLength(2000)]
    public string? Bio { get; init; }

    [MaxLength(500)]
    public string? WebsiteUrl { get; init; }

    [MaxLength(500)]
    public string? LinkedInUrl { get; init; }

    [MaxLength(500)]
    public string? GitHubUrl { get; init; }

    [MaxLength(500)]
    public string? XUrl { get; init; }
}

public sealed record PatchOrganizationProfileRequest
{
    [MaxLength(250)]
    public string? Tagline { get; init; }

    [MaxLength(4000)]
    public string? Description { get; init; }

    [EmailAddress]
    [MaxLength(254)]
    public string? SupportEmail { get; init; }

    [MaxLength(30)]
    public string? SupportPhone { get; init; }

    [MaxLength(250)]
    public string? HeadquartersLocation { get; init; }

    [MaxLength(500)]
    public string? LinkedInUrl { get; init; }

    [MaxLength(500)]
    public string? XUrl { get; init; }
}

public sealed class UserBatchMediaUploadRequest
{
    [FromForm(Name = "avatar")]
    public IFormFile? Avatar { get; init; }

    [FromForm(Name = "cover")]
    public IFormFile? Cover { get; init; }
}

public sealed class OrganizationBatchMediaUploadRequest
{
    [FromForm(Name = "logo")]
    public IFormFile? Logo { get; init; }

    [FromForm(Name = "cover")]
    public IFormFile? Cover { get; init; }
}
