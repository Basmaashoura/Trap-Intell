using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Attacks.Enums;
using Trap_Intel.Domain.Attacks.ValueObjects;

namespace Trap_Intel.Domain.Attacks.Policies;

/// <summary>
/// Policy for AI analysis validation and processing.
/// </summary>
public static class AttackEventAnalysisPolicy
{
    /// <summary>
    /// Validate AI analysis input.
    /// </summary>
    public static Result ValidateAnalysis(
        bool isAlreadyAnalyzed,
        decimal threatScore,
        List<MitreTechnique>? techniques)
    {
        if (isAlreadyAnalyzed)
            return Result.Failure(AttackErrors.AlreadyAnalyzed);

        if (threatScore < 0 || threatScore > 100)
            return Result.Failure(AttackErrors.InvalidThreatScore);

        return Result.Success();
    }

    /// <summary>
    /// Process AI analysis result and determine state changes.
    /// </summary>
    public static AnalysisState ProcessAnalysis(
        decimal threatScore,
        AttackIntent intent,
        List<MitreTechnique>? mitreTechniques,
        AttackSeverity currentSeverity,
        AttackSeverity? suggestedSeverity,
        bool isAnomaly)
    {
        var state = new AnalysisState
        {
            ThreatScore = threatScore,
            Intent = intent,
            MitreTechniques = mitreTechniques ?? new List<MitreTechnique>(),
            IsAnomaly = isAnomaly,
            OriginalSeverity = currentSeverity
        };

        // Determine if severity should be upgraded
        if (suggestedSeverity.HasValue && suggestedSeverity.Value > currentSeverity)
        {
            state.NewSeverity = suggestedSeverity.Value;
            state.SeverityUpgraded = true;
        }
        else
        {
            // Check if analysis indicates upgrade
            var autoUpgrade = AttackEventSeverityPolicy.DetermineUpgradedSeverity(
                currentSeverity, threatScore, isAnomaly);

            if (autoUpgrade.HasValue)
            {
                state.NewSeverity = autoUpgrade.Value;
                state.SeverityUpgraded = true;
            }
            else
            {
                state.NewSeverity = currentSeverity;
            }
        }

        // Determine if alert should be triggered
        state.ShouldTriggerAlert = AttackEventSeverityPolicy.ShouldTriggerAlert(
            state.NewSeverity,
            threatScore,
            false); // Malware already handled at creation

        return state;
    }

    /// <summary>
    /// Get MITRE ATT&CK tactics from techniques.
    /// </summary>
    public static List<string> GetTactics(List<MitreTechnique> techniques)
    {
        return techniques
            .Where(t => !string.IsNullOrWhiteSpace(t.TacticName))
            .Select(t => t.TacticName!)
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// Calculate overall risk score based on analysis.
    /// </summary>
    public static decimal CalculateRiskScore(
        decimal threatScore,
        AttackSeverity severity,
        int mitreTechniqueCount,
        bool isAnomaly)
    {
        // Base score from threat score (40%)
        var baseScore = threatScore * 0.4m;

        // Severity weight (30%)
        var severityWeight = AttackEventSeverityPolicy.GetSeverityWeight(severity);
        var severityScore = severityWeight * 0.3m;

        // MITRE technique count (20%) - more techniques = more sophisticated
        var techniqueScore = Math.Min(mitreTechniqueCount * 5, 20);

        // Anomaly bonus (10%)
        var anomalyScore = isAnomaly ? 10 : 0;

        return Math.Min(100, baseScore + severityScore + techniqueScore + anomalyScore);
    }
}

/// <summary>
/// State object for analysis processing.
/// </summary>
public class AnalysisState
{
    public decimal ThreatScore { get; set; }
    public AttackIntent Intent { get; set; }
    public List<MitreTechnique> MitreTechniques { get; set; } = new();
    public bool IsAnomaly { get; set; }
    public AttackSeverity OriginalSeverity { get; set; }
    public AttackSeverity NewSeverity { get; set; }
    public bool SeverityUpgraded { get; set; }
    public bool ShouldTriggerAlert { get; set; }
}
