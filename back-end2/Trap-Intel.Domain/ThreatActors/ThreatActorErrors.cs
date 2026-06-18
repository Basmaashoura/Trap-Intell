using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.ThreatActors;

public static class ThreatActorErrors
{
    #region Core Errors

    public static readonly Error NotFound = Error.Custom(
        "ThreatActor.NotFound",
        "Threat actor not found");

    public static readonly Error InvalidThreatActorId = Error.Custom(
        "ThreatActor.InvalidThreatActorId",
        "Threat actor ID cannot be empty");

    public static readonly Error InvalidOrganizationId = Error.Custom(
        "ThreatActor.InvalidOrganizationId",
        "Organization ID cannot be empty");

    public static readonly Error InvalidUserId = Error.Custom(
        "ThreatActor.InvalidUserId",
        "User ID cannot be empty");

    public static readonly Error InvalidReason = Error.Custom(
        "ThreatActor.InvalidReason",
        "Reason cannot be empty");

    public static readonly Error InvalidHoneypotId = Error.Custom(
        "ThreatActor.InvalidHoneypotId",
        "Honeypot ID cannot be empty");

    public static readonly Error InvalidAttackEventId = Error.Custom(
        "ThreatActor.InvalidAttackEventId",
        "Attack event ID cannot be empty");

    #endregion

    #region IP Errors

    public static readonly Error InvalidIPAddress = Error.Custom(
        "ThreatActor.InvalidIPAddress",
        "IP address cannot be empty or invalid");

    public static readonly Error IPAlreadyAssociated = Error.Custom(
        "ThreatActor.IPAlreadyAssociated",
        "IP address is already associated with this threat actor");

    public static readonly Error IPNotFound = Error.Custom(
        "ThreatActor.IPNotFound",
        "IP address not found in threat actor profile");

    public static readonly Error IPAlreadyBlocked = Error.Custom(
        "ThreatActor.IPAlreadyBlocked",
        "IP address is already blocked");

    public static readonly Error IPNotBlocked = Error.Custom(
        "ThreatActor.IPNotBlocked",
        "IP address is not blocked");

    public static readonly Error InvalidReputationScore = Error.Custom(
        "ThreatActor.InvalidReputationScore",
        "Reputation score must be between 0 and 100");

    #endregion

    #region Threat Level Errors

    public static readonly Error InvalidThreatLevel = Error.Custom(
        "ThreatActor.InvalidThreatLevel",
        "Invalid threat level specified");

    public static readonly Error CannotEscalateHigher = Error.Custom(
        "ThreatActor.CannotEscalateHigher",
        "Threat level is already at maximum");

    public static readonly Error CannotDeescalateLower = Error.Custom(
        "ThreatActor.CannotDeescalateLower",
        "Threat level is already at minimum");

    #endregion

    #region TTP Errors

    public static readonly Error InvalidTTP = Error.Custom(
        "ThreatActor.InvalidTTP",
        "Invalid TTP (technique) information");

    public static readonly Error TTPNotFound = Error.Custom(
        "ThreatActor.TTPNotFound",
        "TTP not found in threat actor profile");

    public static readonly Error TTPAlreadyExists = Error.Custom(
        "ThreatActor.TTPAlreadyExists",
        "TTP already exists for this threat actor");

    public static readonly Error InvalidConfidenceScore = Error.Custom(
        "ThreatActor.InvalidConfidenceScore",
        "Confidence score must be between 0 and 100");

    #endregion

    #region Behavior Pattern Errors

    public static readonly Error InvalidPatternCategory = Error.Custom(
        "ThreatActor.InvalidPatternCategory",
        "Pattern category cannot be empty");

    public static readonly Error InvalidPatternDescription = Error.Custom(
        "ThreatActor.InvalidPatternDescription",
        "Pattern description cannot be empty");

    public static readonly Error PatternNotFound = Error.Custom(
        "ThreatActor.PatternNotFound",
        "Behavior pattern not found");

    public static readonly Error PatternAlreadyExists = Error.Custom(
        "ThreatActor.PatternAlreadyExists",
        "Behavior pattern already exists for this threat actor");

    #endregion

    #region Note Errors

    public static readonly Error InvalidNote = Error.Custom(
        "ThreatActor.InvalidNote",
        "Note content cannot be empty");

    public static readonly Error NoteTooLong = Error.Custom(
        "ThreatActor.NoteTooLong",
        "Note content cannot exceed 10,000 characters");

    public static readonly Error NoteNotFound = Error.Custom(
        "ThreatActor.NoteNotFound",
        "Intelligence note not found");

    public static readonly Error NoteDeleted = Error.Custom(
        "ThreatActor.NoteDeleted",
        "Cannot modify a deleted note");

    public static readonly Error NoteNotDeleted = Error.Custom(
        "ThreatActor.NoteNotDeleted",
        "Note is not deleted");

    public static readonly Error NoteRestoreExpired = Error.Custom(
        "ThreatActor.NoteRestoreExpired",
        "Cannot restore note after 30 days");

    public static readonly Error InvalidNoteSource = Error.Custom(
        "ThreatActor.InvalidNoteSource",
        "Note source cannot be empty");

    public static readonly Error CannotEditNote = Error.Custom(
        "ThreatActor.CannotEditNote",
        "You do not have permission to edit this note");

    public static readonly Error CannotDeleteNote = Error.Custom(
        "ThreatActor.CannotDeleteNote",
        "You do not have permission to delete this note");

    #endregion

    #region Correlation Errors

    public static readonly Error AttackAlreadyCorrelated = Error.Custom(
        "ThreatActor.AttackAlreadyCorrelated",
        "Attack event is already correlated to this threat actor");

    public static readonly Error CannotMergeSelf = Error.Custom(
        "ThreatActor.CannotMergeSelf",
        "Cannot merge threat actor with itself");

    public static readonly Error CannotMergeDifferentOrg = Error.Custom(
        "ThreatActor.CannotMergeDifferentOrg",
        "Cannot merge threat actors from different organizations");

    #endregion

    #region Status Errors

    public static readonly Error AlreadyNeutralized = Error.Custom(
        "ThreatActor.AlreadyNeutralized",
        "Threat actor is already neutralized");

    public static readonly Error AlreadyFalsePositive = Error.Custom(
        "ThreatActor.AlreadyFalsePositive",
        "Threat actor is already marked as false positive");

    public static readonly Error InvalidStatus = Error.Custom(
        "ThreatActor.InvalidStatus",
        "Invalid threat actor status");

    public static readonly Error CannotModifyFalsePositive = Error.Custom(
        "ThreatActor.CannotModifyFalsePositive",
        "Cannot modify a threat actor marked as false positive");

    #endregion

    #region Alias Errors

    public static readonly Error InvalidAlias = Error.Custom(
        "ThreatActor.InvalidAlias",
        "Alias cannot be empty");

    public static readonly Error AliasTooLong = Error.Custom(
        "ThreatActor.AliasTooLong",
        "Alias cannot exceed 100 characters");

    #endregion

    #region Motivation Errors

    public static readonly Error InvalidMotivation = Error.Custom(
        "ThreatActor.InvalidMotivation",
        "Motivation cannot be Unknown when explicitly setting");

    #endregion

    #region Factory Methods

    public static Error NotFoundById(Guid threatActorId) => Error.Custom(
        "ThreatActor.NotFound",
        $"Threat actor with ID '{threatActorId}' not found");

    public static Error IPNotFoundByAddress(string ipAddress) => Error.Custom(
        "ThreatActor.IPNotFound",
        $"IP address '{ipAddress}' not found in threat actor profile");

    public static Error NoteNotFoundById(Guid noteId) => Error.Custom(
        "ThreatActor.NoteNotFound",
        $"Intelligence note with ID '{noteId}' not found");

    public static Error TTPNotFoundById(Guid ttpId) => Error.Custom(
        "ThreatActor.TTPNotFound",
        $"TTP with ID '{ttpId}' not found");

    public static Error PatternNotFoundById(Guid patternId) => Error.Custom(
        "ThreatActor.PatternNotFound",
        $"Behavior pattern with ID '{patternId}' not found");

    #endregion
}
