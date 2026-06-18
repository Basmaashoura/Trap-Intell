namespace Trap_Intel.Infrastructure.Configuration;

public sealed class CloudinarySettings
{
    public const string SectionName = "Cloudinary";

    public string CloudName { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
    public string ApiSecret { get; init; } = string.Empty;

    public string UserMediaFolder { get; init; } = "trap-intel/users";
    public string OrganizationMediaFolder { get; init; } = "trap-intel/organizations";

    public int MaxFileSizeBytes { get; init; } = 5 * 1024 * 1024;
    public int MaxParallelUploads { get; init; } = 4;
}
