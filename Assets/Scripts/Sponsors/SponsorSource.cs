using UnityEngine;

public class SponsorSource : MonoBehaviour, IPlayerTriggerable {
    [Header("Sponsor")]
    [Tooltip("Sponsor granted by this NPC, counter, tournament desk, contract object or terminal.")]
    [SerializeField] SponsorDefinition sponsor;
    [Tooltip("Short source id written into sponsor logs. Empty uses this GameObject name.")]
    [SerializeField] string sourceId = "sponsor-source";

    [Header("Trigger")]
    [Tooltip("If enabled, player trigger attempts to grant the sponsor.")]
    [SerializeField] bool grantOnPlayerTrigger = true;
    [Tooltip("If enabled, this trigger can be called repeatedly by the player.")]
    [SerializeField] bool triggerRepeatedly = true;

    [Header("Debug")]
    [Tooltip("If enabled, blocked sponsor grants are written to GameDebug.")]
    [SerializeField] bool logBlockedAttempts = true;
    [Tooltip("If enabled, successful sponsor grants are written to GameDebug.")]
    [SerializeField] bool logSuccessfulAttempts;

    public bool TriggerRepeatedly => triggerRepeatedly;
    public SponsorDefinition Sponsor => sponsor;

    public void OnPlayerTriggered(PlayerController player) {
        if(!grantOnPlayerTrigger) {
            return;
        }

        TryGrant(player, out _);
    }

    public bool CanGrant(PlayerController player, out string failureMessage) {
        if(sponsor == null) {
            failureMessage = "No sponsor is assigned.";
            return false;
        }

        return sponsor.CanGrant(player, out failureMessage);
    }

    public bool TryGrant(PlayerController player, out string failureMessage) {
        if(sponsor == null) {
            failureMessage = "No sponsor is assigned.";
            LogBlocked(player, failureMessage);
            return false;
        }

        if(!sponsor.TryGrant(player, ResolveSourceId(), out _, out failureMessage)) {
            LogBlocked(player, failureMessage);
            return false;
        }

        if(logSuccessfulAttempts) {
            GameDebug.Success($"{sponsor.DisplayName} sponsorship granted.", GameDebugCategory.Shop, this, "SponsorSource");
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

        GameDebug.Warning(failureMessage, GameDebugCategory.Shop, player != null ? player : this, "SponsorSource");
    }
}
