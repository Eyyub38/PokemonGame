using UnityEngine;

public class CompetitionInvitationSource : MonoBehaviour, IPlayerTriggerable {
    [Header("Invitation")]
    [Tooltip("Invitation, qualifier pass or wildcard granted by this NPC, desk, event object or terminal.")]
    [SerializeField] CompetitionInvitationDefinition invitation;
    [Tooltip("Short source id written into invitation logs. Empty uses this GameObject name.")]
    [SerializeField] string sourceId = "competition-invitation-source";

    [Header("Trigger")]
    [Tooltip("If enabled, player trigger attempts to grant the invitation.")]
    [SerializeField] bool grantOnPlayerTrigger = true;
    [Tooltip("If enabled, this trigger can be called repeatedly by the player.")]
    [SerializeField] bool triggerRepeatedly = true;

    [Header("Debug")]
    [Tooltip("If enabled, blocked invitation grants are written to GameDebug.")]
    [SerializeField] bool logBlockedAttempts = true;
    [Tooltip("If enabled, successful invitation grants are written to GameDebug.")]
    [SerializeField] bool logSuccessfulAttempts;

    public bool TriggerRepeatedly => triggerRepeatedly;
    public CompetitionInvitationDefinition Invitation => invitation;

    public void OnPlayerTriggered(PlayerController player) {
        if(!grantOnPlayerTrigger) {
            return;
        }

        TryGrant(player, out _);
    }

    public bool CanGrant(PlayerController player, out string failureMessage) {
        if(invitation == null) {
            failureMessage = "No competition invitation is assigned.";
            return false;
        }

        return invitation.CanGrant(player, out failureMessage);
    }

    public bool TryGrant(PlayerController player, out string failureMessage) {
        if(invitation == null) {
            failureMessage = "No competition invitation is assigned.";
            LogBlocked(player, failureMessage);
            return false;
        }

        if(!invitation.TryGrant(player, ResolveSourceId(), out _, out failureMessage)) {
            LogBlocked(player, failureMessage);
            return false;
        }

        if(logSuccessfulAttempts) {
            GameDebug.Success($"{invitation.DisplayName} granted.", GameDebugCategory.BattleRule, this, "CompetitionInvitationSource");
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

        GameDebug.Warning(failureMessage, GameDebugCategory.BattleRule, player != null ? player : this, "CompetitionInvitationSource");
    }
}
