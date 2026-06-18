using System.Collections.Concurrent;
using System.Net;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Trap_Intel.Application.Abstractions.Media;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Infrastructure.Configuration;
using DomainError = Trap_Intel.Domain.Abstractions.Error;

namespace Trap_Intel.Infrastructure.Media;

internal sealed class CloudinaryMediaStorageService : IMediaStorageService
{
    private readonly CloudinarySettings _settings;
    private readonly ILogger<CloudinaryMediaStorageService> _logger;
    private readonly Cloudinary? _cloudinary;

    public CloudinaryMediaStorageService(
        IOptions<CloudinarySettings> settings,
        ILogger<CloudinaryMediaStorageService> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        if (IsConfigured(_settings))
        {
            var account = new Account(_settings.CloudName, _settings.ApiKey, _settings.ApiSecret);
            _cloudinary = new Cloudinary(account);
            _cloudinary.Api.Secure = true;
        }
    }

    public async Task<Result<MediaUploadResult>> UploadImageAsync(
        MediaUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        if (_cloudinary is null)
        {
            return Result.Failure<MediaUploadResult>(
                DomainError.Custom("Media.NotConfigured", "Cloudinary settings are not configured."));
        }

        if (request.OwnerId == Guid.Empty)
        {
            return Result.Failure<MediaUploadResult>(
                DomainError.Custom("Media.InvalidOwner", "A valid media owner ID is required."));
        }

        if (request.ContentLength <= 0)
        {
            return Result.Failure<MediaUploadResult>(
                DomainError.Custom("Media.EmptyFile", "Uploaded file is empty."));
        }

        if (request.ContentLength > _settings.MaxFileSizeBytes)
        {
            return Result.Failure<MediaUploadResult>(
                DomainError.Custom("Media.FileTooLarge", $"File exceeds max size of {_settings.MaxFileSizeBytes} bytes."));
        }

        if (!request.Content.CanRead)
        {
            return Result.Failure<MediaUploadResult>(
                DomainError.Custom("Media.InvalidStream", "Uploaded file stream is not readable."));
        }

        if (!request.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<MediaUploadResult>(
                DomainError.Custom("Media.InvalidType", "Only image uploads are supported."));
        }

        var folder = ResolveFolder(request.Scope, request.OwnerId);

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(request.FileName, request.Content),
            Folder = folder,
            UseFilename = false,
            UniqueFilename = true,
            Overwrite = false,
            Invalidate = false,
            Transformation = BuildTransformation(request.Scope)
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams, cancellationToken);

        if (uploadResult.Error is not null)
        {
            _logger.LogWarning(
                "Cloudinary upload failed for owner {OwnerId} and scope {Scope}. Error: {Error}",
                request.OwnerId,
                request.Scope,
                uploadResult.Error.Message);

            return Result.Failure<MediaUploadResult>(
                DomainError.Custom("Media.UploadFailed", uploadResult.Error.Message));
        }

        if (uploadResult.StatusCode != HttpStatusCode.OK && uploadResult.StatusCode != HttpStatusCode.Created)
        {
            return Result.Failure<MediaUploadResult>(
                DomainError.Custom("Media.UploadFailed", "Cloudinary returned a non-success status code."));
        }

        if (uploadResult.SecureUrl is null || string.IsNullOrWhiteSpace(uploadResult.PublicId))
        {
            return Result.Failure<MediaUploadResult>(
                DomainError.Custom("Media.UploadFailed", "Cloudinary response did not include media URL or public ID."));
        }

        var result = new MediaUploadResult(
            uploadResult.SecureUrl.ToString(),
            uploadResult.PublicId,
            uploadResult.Width,
            uploadResult.Height,
            uploadResult.Bytes,
            uploadResult.Format ?? string.Empty,
            request.Scope,
            request.OwnerId);

        return Result.Success(result);
    }

    public async Task<Result> DeleteImageAsync(
        string publicId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(publicId))
        {
            return Result.Success();
        }

        if (_cloudinary is null)
        {
            return Result.Failure(
                DomainError.Custom("Media.NotConfigured", "Cloudinary settings are not configured."));
        }

        var deletionParams = new DeletionParams(publicId)
        {
            ResourceType = ResourceType.Image,
            Invalidate = true
        };

        var deleteResult = await _cloudinary.DestroyAsync(deletionParams);

        if (deleteResult.Error is not null)
        {
            return Result.Failure(
                DomainError.Custom("Media.DeleteFailed", deleteResult.Error.Message));
        }

        if (!string.Equals(deleteResult.Result, "ok", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(deleteResult.Result, "not found", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure(
                DomainError.Custom("Media.DeleteFailed", $"Unexpected Cloudinary deletion result '{deleteResult.Result}'."));
        }

        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<MediaUploadResult>>> UploadImagesInParallelAsync(
        IReadOnlyList<MediaUploadRequest> requests,
        CancellationToken cancellationToken = default)
    {
        if (requests.Count == 0)
        {
            return Result.Success<IReadOnlyList<MediaUploadResult>>(Array.Empty<MediaUploadResult>());
        }

        var maxParallelism = Math.Clamp(_settings.MaxParallelUploads, 1, 12);
        using var semaphore = new SemaphoreSlim(maxParallelism, maxParallelism);

        var failures = new ConcurrentQueue<DomainError>();
        var outputs = new MediaUploadResult[requests.Count];

        var tasks = requests.Select((request, index) => ProcessUploadAsync(
            request,
            index,
            semaphore,
            outputs,
            failures,
            cancellationToken));

        await Task.WhenAll(tasks);

        if (!failures.IsEmpty)
        {
            return Result.Failure<IReadOnlyList<MediaUploadResult>>(failures.First());
        }

        return Result.Success<IReadOnlyList<MediaUploadResult>>(outputs);
    }

    private async Task ProcessUploadAsync(
        MediaUploadRequest request,
        int index,
        SemaphoreSlim semaphore,
        MediaUploadResult[] outputs,
        ConcurrentQueue<DomainError> failures,
        CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken);
        try
        {
            var uploadResult = await UploadImageAsync(request, cancellationToken);
            if (uploadResult.IsFailure)
            {
                failures.Enqueue(uploadResult.Errors.First());
                return;
            }

            outputs[index] = uploadResult.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Parallel upload failed unexpectedly for owner {OwnerId}", request.OwnerId);
            failures.Enqueue(DomainError.Custom("Media.UploadFailed", ex.Message));
        }
        finally
        {
            semaphore.Release();
        }
    }

    private string ResolveFolder(MediaAssetScope scope, Guid ownerId)
    {
        var ownerSegment = ownerId.ToString("N");
        var userBase = _settings.UserMediaFolder.TrimEnd('/');
        var orgBase = _settings.OrganizationMediaFolder.TrimEnd('/');

        return scope switch
        {
            MediaAssetScope.UserAvatar => $"{userBase}/{ownerSegment}/avatar",
            MediaAssetScope.UserCover => $"{userBase}/{ownerSegment}/cover",
            MediaAssetScope.OrganizationLogo => $"{orgBase}/{ownerSegment}/logo",
            MediaAssetScope.OrganizationCover => $"{orgBase}/{ownerSegment}/cover",
            _ => $"{userBase}/{ownerSegment}/misc"
        };
    }

    private static Transformation BuildTransformation(MediaAssetScope scope)
    {
        var transformation = new Transformation().FetchFormat("auto").Quality("auto:good");

        return scope switch
        {
            MediaAssetScope.UserAvatar => transformation.Width(600).Height(600).Crop("limit"),
            MediaAssetScope.OrganizationLogo => transformation.Width(800).Height(800).Crop("limit"),
            MediaAssetScope.UserCover => transformation.Width(1920).Height(1080).Crop("limit"),
            MediaAssetScope.OrganizationCover => transformation.Width(1920).Height(1080).Crop("limit"),
            _ => transformation
        };
    }

    private static bool IsConfigured(CloudinarySettings settings)
    {
        return !string.IsNullOrWhiteSpace(settings.CloudName)
            && !string.IsNullOrWhiteSpace(settings.ApiKey)
            && !string.IsNullOrWhiteSpace(settings.ApiSecret);
    }
}
