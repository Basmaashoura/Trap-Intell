using SharedKernel;

namespace Domain.Permissions;

/// <summary>
/// Defines data masking rules for a role.
/// Specifies which sensitive fields are visible or masked.
/// </summary>
public sealed class DataMaskingPolicy : Entity
{
    /// <summary>
    /// Default mask value for hidden text fields.
    /// </summary>
    public const string TextMask = "******";

    /// <summary>
    /// Default mask value for hidden email fields.
    /// </summary>
    public const string EmailMask = "Private Info";

    /// <summary>
    /// Default mask value for hidden financial fields.
    /// </summary>
    public const string FinancialMask = "Confidential";

    public Guid Id { get; private set; }

    /// <summary>
    /// The role this policy applies to.
    /// </summary>
    public Guid RoleId { get; private set; }

    /// <summary>
    /// Fields that are visible to users with this role.
    /// Uses SensitiveField flags.
    /// </summary>
    public SensitiveField VisibleFields { get; private set; }

    /// <summary>
    /// Custom mask value for phone numbers.
    /// </summary>
    public string PhoneMask { get; private set; } = TextMask;

    /// <summary>
    /// Custom mask value for emails.
    /// </summary>
    public string EmailMaskValue { get; private set; } = EmailMask;

    /// <summary>
    /// Custom mask value for financial data.
    /// </summary>
    public string FinancialMaskValue { get; private set; } = FinancialMask;

    /// <summary>
    /// Navigation property to Role.
    /// </summary>
    public Role? Role { get; init; }

    private DataMaskingPolicy()
    {
    }

    /// <summary>
    /// Creates a policy with all fields masked (most restrictive).
    /// </summary>
    public static DataMaskingPolicy CreateRestrictive(Guid roleId)
    {
        return new DataMaskingPolicy
        {
            Id = Guid.NewGuid(),
            RoleId = roleId,
            VisibleFields = SensitiveField.None
        };
    }

    /// <summary>
    /// Creates a policy with all fields visible (least restrictive).
    /// </summary>
    public static DataMaskingPolicy CreatePermissive(Guid roleId)
    {
        return new DataMaskingPolicy
        {
            Id = Guid.NewGuid(),
            RoleId = roleId,
            VisibleFields = SensitiveField.All
        };
    }

    /// <summary>
    /// Creates a custom policy with specific visible fields.
    /// </summary>
    public static DataMaskingPolicy Create(Guid roleId, SensitiveField visibleFields)
    {
        return new DataMaskingPolicy
        {
            Id = Guid.NewGuid(),
            RoleId = roleId,
            VisibleFields = visibleFields
        };
    }

    /// <summary>
    /// Checks if a specific field is visible.
    /// </summary>
    public bool IsFieldVisible(SensitiveField field)
    {
        return VisibleFields.HasFlag(field);
    }

    /// <summary>
    /// Gets the masked value for a field.
    /// </summary>
    public string GetMaskedValue(SensitiveField field)
    {
        return field switch
        {
            SensitiveField.PhoneNumber => PhoneMask,
            SensitiveField.EmailAddress => EmailMaskValue,
            SensitiveField.Revenue or
            SensitiveField.SubscriptionValue or
            SensitiveField.StressDataDetails or
            SensitiveField.HourlyCost => FinancialMaskValue,
            _ => TextMask
        };
    }

    /// <summary>
    /// Masks a value if the field is not visible.
    /// </summary>
    public string MaskIfNeeded(SensitiveField field, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return IsFieldVisible(field) ? value : GetMaskedValue(field);
    }

    /// <summary>
    /// Masks a decimal value if the field is not visible.
    /// </summary>
    public string MaskIfNeeded(SensitiveField field, decimal? value, string format = "N2")
    {
        if (!value.HasValue)
        {
            return string.Empty;
        }

        return IsFieldVisible(field) ? value.Value.ToString(format, System.Globalization.CultureInfo.InvariantCulture) : GetMaskedValue(field);
    }

    /// <summary>
    /// Updates the visible fields.
    /// </summary>
    public void UpdateVisibleFields(SensitiveField visibleFields)
    {
        VisibleFields = visibleFields;
    }

    /// <summary>
    /// Adds a field to visible fields.
    /// </summary>
    public void ShowField(SensitiveField field)
    {
        VisibleFields |= field;
    }

    /// <summary>
    /// Removes a field from visible fields.
    /// </summary>
    public void HideField(SensitiveField field)
    {
        VisibleFields &= ~field;
    }

    /// <summary>
    /// Updates custom mask values.
    /// </summary>
    public void UpdateMasks(string? phoneMask, string? emailMask, string? financialMask)
    {
        if (!string.IsNullOrEmpty(phoneMask))
        {
            PhoneMask = phoneMask;
        }

        if (!string.IsNullOrEmpty(emailMask))
        {
            EmailMaskValue = emailMask;
        }

        if (!string.IsNullOrEmpty(financialMask))
        {
            FinancialMaskValue = financialMask;
        }
    }
}
