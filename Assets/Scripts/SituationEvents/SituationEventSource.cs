using System.Collections.Generic;
using UnityEngine;

public enum SituationEventSourceAction {
    StartSpecificEvent,
    RollSpecificPool,
    RollAllPools,
    ResolveSpecificEvent
}

public class SituationEventSource : MonoBehaviour, IPlayerTriggerable {
    [Header("Source")]
    [Tooltip("Stable source id used by repeat/cooldown records. Empty uses GameObject name.")]
    [SerializeField] string sourceId = string.Empty;
    [Tooltip("Name saved in event history/debug output. Empty uses GameObject name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Action performed when this source is triggered.")]
    [SerializeField] SituationEventSourceAction action = SituationEventSourceAction.RollSpecificPool;

    [Header("Targets")]
    [Tooltip("Specific event used by Start Specific Event or Resolve Specific Event.")]
    [SerializeField] SituationEventDefinition eventDefinition = null;
    [Tooltip("Specific pool used by Roll Specific Pool.")]
    [SerializeField] SituationEventPoolDefinition pool = null;
    [Tooltip("Pools used by Roll All Pools.")]
    [SerializeField] List<SituationEventPoolDefinition> pools = new List<SituationEventPoolDefinition>();

    [Header("Context")]
    [Tooltip("Optional region context passed into event/pool filters.")]
    [SerializeField] RegionInfoDefinition regionContext = null;
    [Tooltip("Optional activity zone context passed into event/pool filters. Empty can fall back to PlayerActivityContext.CurrentZone.")]
    [SerializeField] ActivityZoneDefinition zoneContext = null;

    [Header("Access")]
    [Tooltip("Optional access profile checked before this source runs.")]
    [SerializeField] AccessProfileDefinition accessProfile = null;
    [Tooltip("If enabled, access checks are published to access logs/events.")]
    [SerializeField] bool publishAccessChecks = true;

    [Header("Trigger")]
    [Tooltip("If enabled, repeated player triggers can call this source more than once.")]
    [SerializeField] bool triggerRepeatedly = true;

    [Header("Debug")]
    [Tooltip("If enabled, source attempts are written to GameDebugLogger.")]
    [SerializeField] bool logAttempts;

    public string SourceId => string.IsNullOrWhiteSpace(sourceId) ? name : sourceId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public SituationEventDefinition EventDefinition => eventDefinition;
    public SituationEventPoolDefinition Pool => pool;
    public IReadOnlyList<SituationEventPoolDefinition> Pools => pools;
    public AccessProfileDefinition AccessProfile => accessProfile;
    public bool TriggerRepeatedly => triggerRepeatedly;

    public void OnPlayerTriggered(PlayerController player) {
        Apply(player);
    }

    public List<SituationEventPoolRollResult> Apply(PlayerController player) {
        var rollResults = new List<SituationEventPoolRollResult>();
        if(player == null) {
            Log("A player is required to run situation event source.", GameDebugSeverity.Warning);
            return rollResults;
        }

        if(accessProfile != null && !accessProfile.CanAccess(player, out var failureMessage)) {
            if(publishAccessChecks) {
                accessProfile.PublishChecked(player, false, SourceId, failureMessage, this);
            }
            Log(failureMessage, GameDebugSeverity.Warning);
            return rollResults;
        }

        if(accessProfile != null && publishAccessChecks) {
            accessProfile.PublishChecked(player, true, SourceId, accessProfile.PassedMessage, this);
        }

        var region = regionContext;
        var zone = zoneContext != null ? zoneContext : PlayerActivityContext.CurrentZone;
        switch(action) {
            case SituationEventSourceAction.StartSpecificEvent:
                eventDefinition?.TryStart(player, region, zone, SourceId, DisplayName, this);
                break;
            case SituationEventSourceAction.ResolveSpecificEvent:
                eventDefinition?.ResolveActive(player, region, zone, SourceId, this);
                break;
            case SituationEventSourceAction.RollAllPools:
                foreach(var entry in pools) {
                    var result = entry?.Roll(player, region, zone, SourceId, DisplayName, this);
                    if(result != null) {
                        rollResults.Add(result);
                    }
                }
                break;
            default:
                var poolResult = pool?.Roll(player, region, zone, SourceId, DisplayName, this);
                if(poolResult != null) {
                    rollResults.Add(poolResult);
                }
                break;
        }

        if(logAttempts) {
            Log($"{DisplayName} ran situation event action {action}.", GameDebugSeverity.Info);
        }

        return rollResults;
    }

    void Log(string message, GameDebugSeverity severity) {
        if(!logAttempts && severity < GameDebugSeverity.Warning) {
            return;
        }

        GameDebugLogger.Ensure().Record(severity, GameDebugCategory.WorldTrigger, message, this, "SituationEventSource");
    }
}
