using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Trap_Intel.Application.Abstractions.Media;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Infrastructure.Configuration;
using DomainError = Trap_Intel.Domain.Abstractions.Error;

namespace Trap_Intel.Infrastructure.Media;

internal sealed class LocalMediaStorageService : IMediaStorageService
{
    private const string MediaFolderSegment = "media";

    private readonly CloudinarySettings _settings;
    private readonly ILogger<LocalMediaStorageService> _logger;
    private readonly string _storageRootPath;

    public LocalMediaStorageService(
        IOptions<CloudinarySettings> settings,
        IHostEnvironment hostEnvironment,
        ILogger<LocalMediaStorageService> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        var contentRoot = string.IsNullOrWhiteSpace(hostEnvironment.ContentRootPath)
            ? AppContext.BaseDirectory
            : hostEnvironment.ContentRootPath;

        var webRoot = Path.Combine(contentRoot, "wwwroot");
        _storageRootPath = Path.Combine(webRoot, MediaFolderSegment);

        Directory.CreateDirectory(_storageRootPath);
    }

    public async Task<Result<MediaUploadResult>> UploadImageAsync(
        MediaUploadRequest request,
        CancellationToken cancellationToken = default)
    {
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

        try
        {
            var extension = ResolveFileExtension(request.FileName, request.ContentType);
            var ownerSegment = request.OwnerId.ToString("N");
            var relativeFolder = ResolveRelativeFolder(request.Scope, ownerSegment);
            var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}{extension}";
            var relativePath = $"{relativeFolder}/{fileName}";
            var fullPath = Path.Combine(_storageRootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));

            var targetDirectory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            await using (var fileStream = new FileStream(
                fullPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true))
            {
                await request.Content.CopyToAsync(fileStream, cancellationToken);
            }

            var bytes = new FileInfo(fullPath).Length;
            var publicId = relativePath.Replace('\\', '/');
            var url = $"/{MediaFolderSegment}/{publicId}";

            var result = new MediaUploadResult(
                url,
                publicId,
                0,
                0,
                bytes,
                extension.TrimStart('.'),
                request.Scope,
                request.OwnerId);

            return Result.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Local media upload failed for owner {OwnerId} and scope {Scope}", request.OwnerId, request.Scope);
            return Result.Failure<MediaUploadResult>(
                DomainError.Custom("Media.UploadFailed", ex.Message));
        }
    }

    public Task<Result> DeleteImageAsync(
        string publicId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(publicId))
        {
            return Task.FromResult(Result.Success());
        }

        try
        {
            var normalizedPublicId = NormalizePublicId(publicId);
            if (normalizedPublicId.Contains("..", StringComparison.Ordinal))
            {
                return Task.FromResult(Result.Failure(
                    DomainError.Custom("Media.DeleteFailed", "Invalid public ID.")));
            }

            var fullPath = Path.GetFullPath(
                Path.Combine(_storageRootPath, normalizedPublicId.Replace('/', Path.DirectorySeparatorChar)));

            var rootPath = Path.GetFullPath(_storageRootPath);
            if (!fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(Result.Failure(
                    DomainError.Custom("Media.DeleteFailed", "Invalid public ID path.")));
            }

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                CleanupEmptyDirectories(Path.GetDirectoryName(fullPath), rootPath);
            }

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Local media delete failed for public ID {PublicId}", publicId);
            return Task.FromResult(Result.Failure(
                DomainError.Custom("Media.DeleteFailed", ex.Message)));
        }
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
            _logger.LogError(ex, "Local parallel media upload failed for owner {OwnerId}", request.OwnerId);
            failures.Enqueue(DomainError.Custom("Media.UploadFailed", ex.Message));
        }
        finally
        {
            semaphore.Release();
        }
    }

    private static string ResolveRelativeFolder(MediaAssetScope scope, string ownerSegment)
    {
        return scope switch
        {
            MediaAssetScope.UserAvatar => $"users/{ownerSegment}/avatar",
            MediaAssetScope.UserCover => $"users/{ownerSegment}/cover",
            MediaAssetScope.OrganizationLogo => $"organizations/{ownerSegment}/logo",
            MediaAssetScope.OrganizationCover => $"organizations/{ownerSegment}/cover",
            _ => $"users/{ownerSegment}/misc"
        };
    }

    private static string ResolveFileExtension(string fileName, string contentType)
    {
        var extension = Path.GetExtension(fileName);
        if (!string.IsNullOrWhiteSpace(extension))
        {
            return extension.ToLowerInvariant();
        }

        return contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            "image/gif" => ".gif",
            "image/bmp" => ".bmp",
            "image/svg+xml" => ".svg",
            _ => ".img"
        };
    }

    private static string NormalizePublicId(string publicId)
    {
        var normalized = publicId.Replace('\\', '/').Trim();

        if (normalized.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            if (Uri.TryCreate(normalized, UriKind.Absolute, out var uri))
            {
                normalized = uri.AbsolutePath;
            }
        }

        normalized = normalized.TrimStart('/');
        if (normalized.StartsWith("media/", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized["media/".Length..];
        }

        return normalized;
    }

    private static void CleanupEmptyDirectories(string? currentDirectory, string rootDirectory)
    {
        if (string.IsNullOrWhiteSpace(currentDirectory))
        {
            return;
        }

        var root = Path.GetFullPath(rootDirectory);
        var current = Path.GetFullPath(currentDirectory);

        while (current.StartsWith(root, StringComparison.OrdinalIgnoreCase) &&
               !string.Equals(current, root, StringComparison.OrdinalIgnoreCase))
        {
            if (Directory.EnumerateFileSystemEntries(current).Any())
            {
                return;
            }

            Directory.Delete(current);
            current = Path.GetDirectoryName(current) ?? root;
        }
    }
}
