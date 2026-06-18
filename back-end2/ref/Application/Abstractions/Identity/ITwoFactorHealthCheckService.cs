namespace Application.Abstractions.Identity;

/// <summary>
/// Service for checking 2FA system health.
/// </summary>
public interface ITwoFactorHealthCheckService
{
    /// <summary>
    /// Performs a comprehensive health check of the 2FA system.
    /// </summary>
    /// <returns>Health check result with details.</returns>
    Task<TwoFactorHealthCheckResult> PerformHealthCheckAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of 2FA health check.
/// </summary>
public sealed record TwoFactorHealthCheckResult(
    bool IsHealthy,
    bool AuthenticatorProviderConfigured,
    bool TotpAlgorithmWorking,
    int UsersWithTwoFactorEnabled,
    int UsersWithTwoFactorPending,
    string? ErrorMessage,
    DateTime CheckedAt)
{
    /// <summary>
    /// Creates a healthy result.
    /// </summary>
    public static TwoFactorHealthCheckResult Healthy(
        int usersWithTwoFactorEnabled,
        int usersWithTwoFactorPending) => new(
            IsHealthy: true,
            AuthenticatorProviderConfigured: true,
            TotpAlgorithmWorking: true,
            UsersWithTwoFactorEnabled: usersWithTwoFactorEnabled,
            UsersWithTwoFactorPending: usersWithTwoFactorPending,
            ErrorMessage: null,
            CheckedAt: DateTime.UtcNow);

    /// <summary>
    /// Creates an unhealthy result.
    /// </summary>
    public static TwoFactorHealthCheckResult Unhealthy(string errorMessage) => new(
        IsHealthy: false,
        AuthenticatorProviderConfigured: false,
        TotpAlgorithmWorking: false,
        UsersWithTwoFactorEnabled: 0,
        UsersWithTwoFactorPending: 0,
        ErrorMessage: errorMessage,
        CheckedAt: DateTime.UtcNow);
}
