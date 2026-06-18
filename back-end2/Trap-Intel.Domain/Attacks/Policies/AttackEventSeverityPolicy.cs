using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Attacks.Enums;
using Trap_Intel.Domain.Attacks.ValueObjects;

namespace Trap_Intel.Domain.Attacks.Policies;

/// <summary>
/// Policy for attack severity calculations and threat assessment.
/// </summary>
public static class AttackEventSeverityPolicy
{
    /// <summary>
    /// Check if attack is high severity.
    /// </summary>
    public static bool IsHighSeverity(AttackSeverity severity) =>
        severity == AttackSeverity.High || severity == AttackSeverity.Critical;

    /// <summary>
    /// Determine if attack should trigger immediate alert.
    /// </summary>
    public static bool ShouldTriggerAlert(
        AttackSeverity severity,
        decimal threatScore,
        bool hasMalware)
    {
        // Always alert on critical
        if (severity == AttackSeverity.Critical)
            return true;

        // Alert on high severity with high threat score
        if (severity == AttackSeverity.High && threatScore >= 70)
            return true;

        // Always alert on malware
        if (hasMalware)
            return true;

        return false;
    }

    /// <summary>
    /// Calculate severity based on attack characteristics.
    /// </summary>
    public static AttackSeverity CalculateSeverity(
        AttackType attackType,
        bool hasCredentials,
        bool hasMalware,
        bool hasCommand)
    {
        // Malware is always critical
        if (hasMalware)
            return AttackSeverity.Critical;

        // Command injection with actual command is high
        if (attackType == AttackType.CommandInjection && hasCommand)
            return AttackSeverity.High;

        // SQL injection is typically high
        if (attackType == AttackType.SQLInjection)
            return AttackSeverity.High;

        // Web shell is critical
        if (attackType == AttackType.WebShell)
            return AttackSeverity.Critical;

        // Brute force with credentials is medium
        if (hasCredentials && IsBruteForceAttack(attackType))
            return AttackSeverity.Medium;

        // Port scan is low
        if (attackType == AttackType.PortScan)
            return AttackSeverity.Low;

        return AttackSeverity.Medium;
    }

    /// <summary>
    /// Check if attack type is brute force variant.
    /// </summary>
    public static bool IsBruteForceAttack(AttackType attackType) =>
        attackType == AttackType.SSHBruteForce ||
        attackType == AttackType.FTPBruteForce ||
        attackType == AttackType.RDPBruteForce ||
        attackType == AttackType.TelnetBruteForce;

    /// <summary>
    /// Get severity weight for sorting/prioritization.
    /// </summary>
    public static int GetSeverityWeight(AttackSeverity severity) =>
        severity switch
        {
            AttackSeverity.Critical => 100,
            AttackSeverity.High => 75,
            AttackSeverity.Medium => 50,
            AttackSeverity.Low => 25,
            AttackSeverity.Info => 10,
            _ => 0
        };

    /// <summary>
    /// Determine if severity should be upgraded based on AI analysis.
    /// </summary>
    public static AttackSeverity? DetermineUpgradedSeverity(
        AttackSeverity currentSeverity,
        decimal threatScore,
        bool isAnomaly)
    {
        // Very high threat score upgrades to critical
        if (threatScore >= 90 && currentSeverity < AttackSeverity.Critical)
            return AttackSeverity.Critical;

        // High threat score upgrades to high
        if (threatScore >= 70 && currentSeverity < AttackSeverity.High)
            return AttackSeverity.High;

        // Anomaly detection upgrades medium to high
        if (isAnomaly && currentSeverity == AttackSeverity.Medium)
            return AttackSeverity.High;

        return null; // No upgrade
    }
}
