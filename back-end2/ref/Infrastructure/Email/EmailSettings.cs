namespace Infrastructure.Email;

/// <summary>
/// Email configuration settings.
/// </summary>
public sealed class EmailSettings
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Email";

    /// <summary>
    /// SMTP server host.
    /// </summary>
    public string SmtpHost { get; init; } = "localhost";

    /// <summary>
    /// SMTP server port.
    /// </summary>
    public int SmtpPort { get; init; } = 1025;

    /// <summary>
    /// Whether to use SSL/TLS.
    /// </summary>
    public bool UseSsl { get; init; }

    /// <summary>
    /// SMTP username (optional for local dev).
    /// </summary>
    public string? Username { get; init; }

    /// <summary>
    /// SMTP password (optional for local dev).
    /// </summary>
    public string? Password { get; init; }

    /// <summary>
    /// Default sender email address.
    /// </summary>
    public string FromEmail { get; init; } = "noreply@onex.com";

    /// <summary>
    /// Default sender display name.
    /// </summary>
    public string FromName { get; init; } = "ONEX System";

    /// <summary>
    /// Application base URL for generating links.
    /// </summary>
    public string AppBaseUrl { get; init; } = "http://localhost:3000";

    /// <summary>
    /// Whether to enable email sending (false = log only).
    /// </summary>
    public bool EnableSending { get; init; } = true;
}
