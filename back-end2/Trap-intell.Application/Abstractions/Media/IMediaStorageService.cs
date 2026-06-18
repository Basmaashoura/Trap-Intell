using System.IO;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Application.Abstractions.Media;

public enum MediaAssetScope
{
    UserAvatar = 0,
    UserCover = 1,
    OrganizationLogo = 2,
    OrganizationCover = 3
}

public sealed record MediaUploadRequest(
    Stream Content,
    string FileName,
    string ContentType,
    MediaAssetScope Scope,
    Guid OwnerId,
    long ContentLength);

public sealed record MediaUploadResult(
    string Url,
    string PublicId,
    int Width,
    int Height,
    long Bytes,
    string Format,
    MediaAssetScope Scope,
    Guid OwnerId);

public interface IMediaStorageService
{
    Task<Result<MediaUploadResult>> UploadImageAsync(
        MediaUploadRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteImageAsync(
        string publicId,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<MediaUploadResult>>> UploadImagesInParallelAsync(
        IReadOnlyList<MediaUploadRequest> requests,
        CancellationToken cancellationToken = default);
}
