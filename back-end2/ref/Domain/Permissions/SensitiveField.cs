namespace Domain.Permissions;

/// <summary>
/// Defines sensitive fields that can be masked based on role permissions.
/// Used for Data Masking feature.
/// </summary>
[Flags]
public enum SensitiveField
{
    /// <summary>
    /// No sensitive fields.
    /// </summary>
    None = 0,

    /// <summary>
    /// Phone numbers.
    /// </summary>
    PhoneNumber = 1 << 0,

    /// <summary>
    /// Email addresses.
    /// </summary>
    EmailAddress = 1 << 1,

    /// <summary>
    /// Revenue and subscription amounts.
    /// </summary>
    Revenue = 1 << 2,

    /// <summary>
    /// Subscription plan values.
    /// </summary>
    SubscriptionValue = 1 << 3,

    /// <summary>
    /// Stress data details and raw keyboard metrics.
    /// </summary>
    StressDataDetails = 1 << 4,

    /// <summary>
    /// Employee hourly cost.
    /// </summary>
    HourlyCost = 1 << 5,

    /// <summary>
    /// User address.
    /// </summary>
    Address = 1 << 6,

    /// <summary>
    /// Tax identification numbers.
    /// </summary>
    TaxNumber = 1 << 7,

    /// <summary>
    /// All sensitive fields.
    /// </summary>
    All = PhoneNumber | EmailAddress | Revenue | SubscriptionValue |
          StressDataDetails | HourlyCost | Address | TaxNumber
}
