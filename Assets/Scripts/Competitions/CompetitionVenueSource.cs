using UnityEngine;

public class CompetitionVenueSource : MonoBehaviour, IPlayerTriggerable {
    [Header("Venue")]
    [Tooltip("Venue, arena, gym or stadium represented by this scene object.")]
    [SerializeField] CompetitionVenueDefinition venue;
    [Tooltip("Optional registration source used after entering this venue.")]
    [SerializeField] CompetitionRegistrationSource registrationSource;
    [Tooltip("Optional bracket source used after entering this venue.")]
    [SerializeField] CompetitionBracketSource bracketSource;
    [Tooltip("Short source id written into venue logs. Empty uses this GameObject name.")]
    [SerializeField] string sourceId = "competition-venue-source";

    [Header("Trigger")]
    [Tooltip("If enabled, player trigger attempts to enter/use this venue.")]
    [SerializeField] bool enterVenueOnPlayerTrigger = true;
    [Tooltip("If enabled, player trigger attempts linked registration after venue access succeeds.")]
    [SerializeField] bool registerAfterEnter;
    [Tooltip("If enabled, player trigger attempts to prepare the next linked bracket match after venue access succeeds.")]
    [SerializeField] bool prepareMatchAfterEnter;
    [Tooltip("If enabled, this trigger can be called repeatedly by the player.")]
    [SerializeField] bool triggerRepeatedly = true;

    [Header("Debug")]
    [Tooltip("If enabled, blocked venue attempts are written to GameDebug.")]
    [SerializeField] bool logBlockedAttempts = true;
    [Tooltip("If enabled, successful venue attempts are written to GameDebug.")]
    [SerializeField] bool logSuccessfulAttempts;

    public bool TriggerRepeatedly => triggerRepeatedly;
    public CompetitionVenueDefinition Venue => venue;
    public CompetitionRegistrationSource RegistrationSource => registrationSource;
    public CompetitionBracketSource BracketSource => bracketSource;

    public void OnPlayerTriggered(PlayerController player) {
        if(!enterVenueOnPlayerTrigger) {
            return;
        }

        if(!TryEnter(player, out _)) {
            return;
        }

        if(registerAfterEnter && registrationSource != null) {
            registrationSource.TryRegister(player, out _);
        }

        if(prepareMatchAfterEnter && bracketSource != null) {
            bracketSource.TryPrepareNextMatch(player, out _);
        }
    }

    public bool CanEnter(PlayerController player, out string failureMessage) {
        if(venue == null) {
            failureMessage = "No competition venue is assigned.";
            return false;
        }

        return venue.CanEnter(player, out failureMessage);
    }

    public bool TryEnter(PlayerController player, out string failureMessage) {
        if(venue == null) {
            failureMessage = "No competition venue is assigned.";
            LogBlocked(player, failureMessage);
            return false;
        }

        if(!venue.CanEnter(player, out failureMessage)) {
            venue.RecordUse(player, CompetitionVenuePurpose.Enter, null, null, ResolveSourceId(), this, blocked: true, failureMessage);
            LogBlocked(player, failureMessage);
            return false;
        }

        venue.RecordUse(player, CompetitionVenuePurpose.Enter, null, null, ResolveSourceId(), this, blocked: false, null);
        if(logSuccessfulAttempts) {
            GameDebug.Success($"{venue.DisplayName} entered.", GameDebugCategory.BattleRule, this, "CompetitionVenueSource");
        }

        failureMessage = null;
        return true;
    }

    string ResolveSourceId() {
        return string.IsNullOrWhiteSpace(sourceId) ? name : sourceId;
    }

    void LogBlocked(PlayerController player, string failureMessage) {
        if(!logBlockedAttempts) {
            return;
        }

        GameDebug.Warning(failureMessage, GameDebugCategory.BattleRule, player != null ? player : this, "CompetitionVenueSource");
    }
}
