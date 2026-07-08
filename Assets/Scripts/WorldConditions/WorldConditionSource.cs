using UnityEngine;

public enum WorldConditionSourceMode {
    Activate,
    Deactivate,
    Toggle
}

public class WorldConditionSource : MonoBehaviour, IPlayerTriggerable {
    [Header("Source")]
    [Tooltip("Optional source id used by save/repeat rules. Empty uses GameObject name.")]
    [SerializeField] string sourceId = string.Empty;
    [Tooltip("Name shown in debug/future UI. Empty uses GameObject name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Condition this source activates, deactivates or toggles.")]
    [SerializeField] WorldConditionDefinition condition = null;
    [Tooltip("Optional region limit for this condition instance.")]
    [SerializeField] RegionInfoDefinition region = null;
    [Tooltip("Optional activity zone limit for this condition instance.")]
    [SerializeField] ActivityZoneDefinition zone = null;

    [Header("Activation")]
    [Tooltip("What happens when this source is triggered by the player.")]
    [SerializeField] WorldConditionSourceMode mode = WorldConditionSourceMode.Activate;
    [Tooltip("Duration override in in-game hours. -1 uses the condition default, 0 means no automatic expiry.")]
    [Min(-1)]
    [SerializeField] int durationOverrideHours = -1;
    [Tooltip("Strength of this condition instance. 1 uses definition values as-is.")]
    [Min(0f)]
    [SerializeField] float intensity = 1f;
    [Tooltip("Number of stacks applied when activating this condition.")]
    [Min(1)]
    [SerializeField] int stacks = 1;
    [Tooltip("If enabled, triggering an already active condition refreshes its duration and intensity.")]
    [SerializeField] bool refreshExisting = true;
    [Tooltip("If enabled, triggering an already active condition adds stacks.")]
    [SerializeField] bool stackExisting = false;
    [Tooltip("If enabled, repeated player triggers can call OnPlayerTriggered more than once.")]
    [SerializeField] bool triggerRepeatedly = true;

    [Header("Debug")]
    [Tooltip("If enabled, trigger attempts are written to GameEventBus/GameDebugLogger.")]
    [SerializeField] bool logTriggerAttempts = false;

    public string SourceId => string.IsNullOrWhiteSpace(sourceId) ? name : sourceId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public WorldConditionDefinition Condition => condition;
    public RegionInfoDefinition Region => region;
    public ActivityZoneDefinition Zone => zone;
    public int DurationOverrideHours => durationOverrideHours;
    public bool TriggerRepeatedly => triggerRepeatedly;

    public void OnPlayerTriggered(PlayerController player) {
        if(player == null) {
            PublishSourceEvent(null, "blocked", "A player is required to change world conditions.", GameEventImportance.Warning);
            return;
        }

        if(condition == null) {
            PublishSourceEvent(player, "blocked", "No world condition is assigned.", GameEventImportance.Warning);
            return;
        }

        var log = player.GetComponent<PlayerWorldConditionLog>() ?? player.gameObject.AddComponent<PlayerWorldConditionLog>();
        bool changed = mode switch {
            WorldConditionSourceMode.Deactivate => log.DeactivateCondition(condition, SourceId, region, zone),
            WorldConditionSourceMode.Toggle => Toggle(log),
            _ => log.ActivateCondition(condition, SourceId, DisplayName, region, zone, durationOverrideHours, intensity, stacks, refreshExisting, stackExisting) != null
        };

        PublishSourceEvent(player, changed ? "changed" : "unchanged", changed ? $"{DisplayName} updated {condition.DisplayName}." : $"{DisplayName} did not change {condition.DisplayName}.", changed ? GameEventImportance.Info : GameEventImportance.Trace);
    }

    bool Toggle(PlayerWorldConditionLog log) {
        if(log == null || condition == null) {
            return false;
        }

        if(log.IsConditionActive(condition, SourceId, region, zone)) {
            return log.DeactivateCondition(condition, SourceId, region, zone);
        }

        return log.ActivateCondition(condition, SourceId, DisplayName, region, zone, durationOverrideHours, intensity, stacks, refreshExisting, stackExisting) != null;
    }

    void PublishSourceEvent(PlayerController player, string phase, string message, GameEventImportance importance) {
        if(!logTriggerAttempts && importance < GameEventImportance.Warning) {
            return;
        }

        GameEventPublishing.PublishOptional(
            null,
            $"world-condition-source.{phase}.{SourceId}",
            message,
            GameEventCategory.WorldEvent,
            importance,
            player != null ? player : this,
            "WorldConditionSource",
            GameEventScope.Player,
            showInFeed: false,
            writeToDebugLog: logTriggerAttempts,
            GameEventPublishing.Value("sourceId", SourceId),
            GameEventPublishing.Value("sourceName", DisplayName),
            GameEventPublishing.Value("conditionId", condition != null ? condition.Id : string.Empty),
            GameEventPublishing.Value("conditionName", condition != null ? condition.DisplayName : string.Empty),
            GameEventPublishing.Value("phase", phase));
    }
}
