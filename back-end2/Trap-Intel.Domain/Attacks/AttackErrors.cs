using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Attacks;

public static class AttackErrors
{
    public static readonly Error InvalidData = Error.Custom(
        "AttackEvent.InvalidData",
        "Attack event data is invalid or incomplete");

    public static readonly Error AlreadyAnalyzed = Error.Custom(
        "AttackEvent.AlreadyAnalyzed",
        "Attack event has already been analyzed by AI");

    public static readonly Error NotAnalyzed = Error.Custom(
        "AttackEvent.NotAnalyzed",
        "Attack event has not been analyzed yet");

    public static readonly Error InvalidThreatScore = Error.Custom(
        "AttackEvent.InvalidThreatScore",
        "Threat score must be between 0 and 100");

    public static readonly Error InvalidJson = Error.Custom(
        "AttackEvent.InvalidJson",
        "Raw data JSON is invalid or corrupted");

    public static readonly Error NotFound = Error.Custom(
        "AttackEvent.NotFound",
        "Attack event not found");

    public static readonly Error AlreadyMarkedAsAnomaly = Error.Custom(
        "AttackEvent.AlreadyMarkedAsAnomaly",
        "Attack event is already marked as anomaly");
}
