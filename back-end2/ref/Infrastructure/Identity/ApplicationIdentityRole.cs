using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity;

/// <summary>
/// ASP.NET Identity role that syncs with Domain Role.
/// Used for Identity's built-in role management.
/// </summary>
public sealed class ApplicationIdentityRole : IdentityRole<Guid>
{
    /// <summary>
    /// Links to Domain Role.Id for synchronization.
    /// </summary>
    public Guid DomainRoleId { get; set; }

    /// <summary>
    /// When the role was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    private ApplicationIdentityRole()
    {
    }

    public static ApplicationIdentityRole Create(Guid domainRoleId, string name)
    {
        return new ApplicationIdentityRole
        {
            Id = Guid.NewGuid(),
            DomainRoleId = domainRoleId,
            Name = name,
            NormalizedName = name.ToUpperInvariant(),
            CreatedAt = DateTime.UtcNow
        };
    }
}
