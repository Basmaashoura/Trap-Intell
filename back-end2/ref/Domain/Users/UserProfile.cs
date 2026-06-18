using Domain.Common;
using SharedKernel;

namespace Domain.Users;

/// <summary>
/// Extended profile information for staff members.
/// Contains sensitive data like hourly cost (visible to admins only).
/// </summary>
public sealed class UserProfile : Entity
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The user this profile belongs to.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// The department this employee belongs to.
    /// Used for ticket auto-routing.
    /// </summary>
    public Department Department { get; private set; }

    /// <summary>
    /// The job title displayed to clients.
    /// Can be different from actual position for privacy.
    /// Example: "Support Agent" instead of "Senior Backend Developer"
    /// </summary>
    public string? DisplayJobTitle { get; private set; }

    /// <summary>
    /// The actual internal job title.
    /// Visible to management only.
    /// </summary>
    public string? InternalJobTitle { get; private set; }

    /// <summary>
    /// Employee's hourly cost for profitability calculations.
    /// Visible to Admin only - never exposed to regular staff.
    /// </summary>
    public decimal? HourlyCost { get; private set; }

    /// <summary>
    /// Employee's phone number (internal contact).
    /// </summary>
    public string? PhoneNumber { get; private set; }

    /// <summary>
    /// Date when the employee joined the company.
    /// </summary>
    public DateTime? HireDate { get; private set; }

    /// <summary>
    /// Profile photo URL.
    /// </summary>
    public Uri? AvatarUrl { get; private set; }

    /// <summary>
    /// Employee's bio or description.
    /// </summary>
    public string? Bio { get; private set; }

    /// <summary>
    /// Skills and expertise tags.
    /// </summary>
    public List<string> Skills { get; private set; } = [];

    /// <summary>
    /// Navigation property to User.
    /// </summary>
    public User? User { get; init; }

    private UserProfile()
    {
    }

    public static UserProfile Create(
        Guid userId,
        Department department,
        string? displayJobTitle = null,
        string? internalJobTitle = null,
        decimal? hourlyCost = null)
    {
        return new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Department = department,
            DisplayJobTitle = displayJobTitle,
            InternalJobTitle = internalJobTitle,
            HourlyCost = hourlyCost
        };
    }

    public void UpdateDepartment(Department department)
    {
        Department = department;
    }

    public void UpdateJobTitles(string? displayTitle, string? internalTitle)
    {
        DisplayJobTitle = displayTitle;
        InternalJobTitle = internalTitle;
    }

    public void UpdateHourlyCost(decimal? cost)
    {
        HourlyCost = cost;
    }

    public void UpdateContactInfo(string? phone)
    {
        PhoneNumber = phone;
    }

    public void UpdateProfile(
        Uri? avatarUrl,
        string? bio,
        List<string>? skills,
        DateTime? hireDate)
    {
        AvatarUrl = avatarUrl;
        Bio = bio;
        Skills = skills ?? [];
        HireDate = hireDate;
    }
}
