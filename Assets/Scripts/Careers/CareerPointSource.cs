using System.Collections.Generic;
using UnityEngine;

public class CareerPointSource : MonoBehaviour, IPlayerTriggerable {
    [Header("Source")]
    [Tooltip("Stable source id used by career logs. Empty uses GameObject name.")]
    [SerializeField] string sourceId;
    [Tooltip("Name shown in debug/future UI. Empty uses GameObject name.")]
    [SerializeField] string displayName;
    [Tooltip("Career point grants applied when this source is triggered.")]
    [SerializeField] List<CareerPointGrant> pointGrants = new List<CareerPointGrant>();
    [Tooltip("If enabled, repeated player triggers can call OnPlayerTriggered more than once.")]
    [SerializeField] bool triggerRepeatedly = true;

    [Header("Access")]
    [Tooltip("Optional title, badge, permit or license required before this source can be used.")]
    [SerializeField] TitleDefinition requiredTitle;
    [Tooltip("Optional activity zone that must currently be active.")]
    [SerializeField] ActivityZoneDefinition requiredActivityZone;
    [Tooltip("Optional tag that must exist on the current activity zone.")]
    [SerializeField] string requiredActivityZoneTag;
    [Tooltip("Message shown when source access is blocked.")]
    [SerializeField] string lockedMessage = "This career point source is not available right now.";

    [Header("Debug")]
    [Tooltip("If enabled, trigger attempts are written to GameDebug.")]
    [SerializeField] bool logAttempts;

    public string SourceId => string.IsNullOrWhiteSpace(sourceId) ? name : sourceId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public IReadOnlyList<CareerPointGrant> PointGrants => pointGrants;
    public bool TriggerRepeatedly => triggerRepeatedly;

    public void OnPlayerTriggered(PlayerController player) {
        if(player == null) {
            return;
        }

        if(!CanUse(player, out var failureMessage)) {
            PublishSourceEvent(player, "blocked", failureMessage, GameEventImportance.Warning);
            return;
        }

        var log = player.GetComponent<PlayerCareerLog>() ?? player.gameObject.AddComponent<PlayerCareerLog>();
        log.ApplyPointGrants(pointGrants, SourceId);
        PublishSourceEvent(player, "granted", $"{DisplayName} granted career progress.", GameEventImportance.Info);
    }

    public bool CanUse(PlayerController player, out string failureMessage) {
        if(requiredTitle != null && !(player?.GetComponent<PlayerTitles>()?.HasTitle(requiredTitle) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need {requiredTitle.DisplayName}." : lockedMessage;
            return false;
        }

        if(requiredActivityZone != null && PlayerActivityContext.CurrentZone != requiredActivityZone) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You must be in {requiredActivityZone.DisplayName}." : lockedMessage;
            return false;
        }

        if(!string.IsNullOrWhiteSpace(requiredActivityZoneTag) && !PlayerActivityContext.HasActiveTag(requiredActivityZoneTag)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? "This area does not allow that career action." : lockedMessage;
            return false;
        }

        failureMessage = null;
        return true;
    }

    void PublishSourceEvent(PlayerController player, string phase, string message, GameEventImportance importance) {
        if(logAttempts) {
            GameDebug.Step(message, GameDebugCategory.Career, player != null ? player : this, "CareerPointSource");
        }

        GameEventPublishing.PublishOptional(
            null,
            $"career-source.{phase}.{SourceId}",
            message,
            GameEventCategory.Career,
            importance,
            player != null ? player : this,
            "CareerPointSource",
            GameEventScope.Player,
            showInFeed: false,
            writeToDebugLog: logAttempts,
            GameEventPublishing.Value("sourceId", SourceId),
            GameEventPublishing.Value("sourceName", DisplayName),
            GameEventPublishing.Value("phase", phase));
    }
}
