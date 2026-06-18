using Microsoft.AspNetCore.Identity;

namespace Trap_Intel.Infrastructure.Authentication.Identity;

/// <summary>
/// Infrastructure representation of a User specifically for ASP.NET Core Identity.
/// This allows us to use mature identity providers and stores while keeping our 
/// Domain Entity (Trap_Intel.Domain.Identity.User) pure.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    // You can add properties here mapping directly to the pure Domain `User` 
    // properties you wish to manage through ASP.NET Identity, or just keep minimal 
    // routing back to the Domain Entity ID.

    // A reference back to our Domain Organization
    public Guid OrganizationId { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}
