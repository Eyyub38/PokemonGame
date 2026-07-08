using UnityEngine;

public class CompetitionRegistrationSource : MonoBehaviour, IPlayerTriggerable {
    [Header("Registration")]
    [Tooltip("Registration definition used by this NPC, desk, counter, gate or terminal.")]
    [SerializeField] CompetitionRegistrationDefinition registration;
    [Tooltip("Optional bracket source that can prepare the next match after registration.")]
    [SerializeField] CompetitionBracketSource bracketSource;
    [Tooltip("Short source id written into logs. Empty uses this GameObject name.")]
    [SerializeField] string sourceId = "competition-registration-source";

    [Header("Trigger")]
    [Tooltip("If enabled, player trigger attempts to register.")]
    [SerializeField] bool registerOnPlayerTrigger = true;
    [Tooltip("If enabled, a successful registration immediately asks the linked bracket source to prepare the next match.")]
    [SerializeField] bool prepareMatchAfterRegistration;
    [Tooltip("If enabled, this trigger can be called repeatedly by the player.")]
    [SerializeField] bool triggerRepeatedly = true;

    [Header("Debug")]
    [Tooltip("If enabled, blocked registration attempts are written to GameDebug.")]
    [SerializeField] bool logBlockedAttempts = true;
    [Tooltip("If enabled, successful registration attempts are written to GameDebug.")]
    [SerializeField] bool logSuccessfulAttempts;

    public bool TriggerRepeatedly => triggerRepeatedly;
    public CompetitionRegistrationDefinition Registration => registration;
    public CompetitionBracketSource BracketSource => bracketSource;

    public void OnPlayerTriggered(PlayerController player) {
        if(!registerOnPlayerTrigger) {
            return;
        }

        TryRegister(player, out _);
    }

    public bool CanRegister(PlayerController player, out string failureMessage) {
        if(registration == null) {
            failureMessage = "No competition registration is assigned.";
            return false;
        }

        return registration.CanRegister(player, out failureMessage);
    }

    public bool TryRegister(PlayerController player, out string failureMessage) {
        if(registration == null) {
            failureMessage = "No competition registration is assigned.";
            LogBlocked(player, failureMessage);
            return false;
        }

        if(!registration.TryRegister(player, ResolveSourceId(), out _, out failureMessage)) {
            LogBlocked(player, failureMessage);
            return false;
        }

        if(logSuccessfulAttempts) {
            GameDebug.Success($"{registration.DisplayName} registered.", GameDebugCategory.BattleRule, this, "CompetitionRegistrationSource");
        }

        if(prepareMatchAfterRegistration && bracketSource != null) {
            bracketSource.TryPrepareNextMatch(player, out _);
        }

        return true;
    }

    string ResolveSourceId() {
        return string.IsNullOrWhiteSpace(sourceId) ? name : sourceId;
    }

    void LogBlocked(PlayerController player, string failureMessage) {
        if(!logBlockedAttempts) {
            return;
        }

        GameDebug.Warning(failureMessage, GameDebugCategory.BattleRule, player != null ? player : this, "CompetitionRegistrationSource");
    }
}
