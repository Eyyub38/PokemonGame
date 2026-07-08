using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum AreaProfileCategory {
    General,
    Region,
    Town,
    Route,
    Building,
    Facility,
    Dungeon,
    Cave,
    Forest,
    WildArea,
    ActivityZone,
    Shop,
    Transit,
    Event,
    Custom
}

public enum AreaProfileOperation {
    Enter,
    Exit
}

public enum AreaWorldConditionOperation {
    Activate,
    Deactivate,
    Toggle
}

[CreateAssetMenu(menuName = "Area Profiles/Area Profile Definition")]
public class AreaProfileDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this area profile. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in debug/future UI. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note explaining what this area profile represents.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Broad area category used by validators, requirements and future UI filters.")]
    [SerializeField] AreaProfileCategory category = AreaProfileCategory.General;
    [Tooltip("Free-form tags such as town, route, lab, market, mine, farm, restricted or story.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Area Context")]
    [Tooltip("Region connected to this area.")]
    [SerializeField] RegionInfoDefinition region = null;
    [Tooltip("Activity zone connected to this area.")]
    [SerializeField] ActivityZoneDefinition activityZone = null;
    [Tooltip("Map marker connected to this area.")]
    [SerializeField] MapMarkerDefinition mapMarker = null;
    [Tooltip("Optional scene name connected to this area. Empty uses Region scene or active scene.")]
    [SerializeField] string sceneName = string.Empty;
    [Tooltip("Optional custom area key such as route segment, building room, field id or cave floor.")]
    [SerializeField] string areaKey = string.Empty;

    [Header("Repeat Rules")]
    [Tooltip("How often this area can be entered for the current player.")]
    [SerializeField] ConsequenceChainRepeatMode repeatMode = ConsequenceChainRepeatMode.Unlimited;
    [Tooltip("Cooldown in in-game hours when Repeat Mode is Cooldown Hours.")]
    [Min(0)]
    [SerializeField] int cooldownHours;
    [Tooltip("Maximum successful enter records for this definition. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxEnterCount;
    [Tooltip("If enabled, area enter/exit operations are stored in PlayerAreaProfileLog.")]
    [SerializeField] bool recordHistory = true;
    [Tooltip("If enabled, blocked enter attempts are also stored in PlayerAreaProfileLog.")]
    [SerializeField] bool recordBlockedAttempts;
    [Tooltip("If enabled, Exit requires this area to be active in PlayerAreaProfileLog.")]
    [SerializeField] bool requireActiveStateToExit = true;

    [Header("Requirements")]
    [Tooltip("How enter requirements are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode requirementMatchMode = ConsequenceRequirementMatchMode.All;
    [Tooltip("Requirements checked before this area can apply its enter actions.")]
    [SerializeField] List<ActivityRequirement> requirements = new List<ActivityRequirement>();

    [Header("Enter Actions")]
    [Tooltip("Location visits recorded when this area is entered.")]
    [SerializeField] List<LocationVisitDefinition> enterLocationVisits = new List<LocationVisitDefinition>();
    [Tooltip("World discoveries applied when this area is entered.")]
    [SerializeField] List<WorldDiscoveryDefinition> enterWorldDiscoveries = new List<WorldDiscoveryDefinition>();
    [Tooltip("Navigation hint actions applied when this area is entered.")]
    [SerializeField] List<AreaNavigationHintAction> enterNavigationHints = new List<AreaNavigationHintAction>();
    [Tooltip("Chronicle entries recorded when this area is entered.")]
    [SerializeField] List<ChronicleEntryDefinition> enterChronicleEntries = new List<ChronicleEntryDefinition>();
    [Tooltip("World condition changes applied when this area is entered.")]
    [SerializeField] List<AreaWorldConditionChange> enterWorldConditions = new List<AreaWorldConditionChange>();
    [Tooltip("Consequence chains applied after this area is entered.")]
    [SerializeField] List<ConsequenceChainDefinition> enteredChains = new List<ConsequenceChainDefinition>();

    [Header("Exit Actions")]
    [Tooltip("Navigation hint actions applied when this area is exited.")]
    [SerializeField] List<AreaNavigationHintAction> exitNavigationHints = new List<AreaNavigationHintAction>();
    [Tooltip("Chronicle entries recorded when this area is exited.")]
    [SerializeField] List<ChronicleEntryDefinition> exitChronicleEntries = new List<ChronicleEntryDefinition>();
    [Tooltip("World condition changes applied when this area is exited.")]
    [SerializeField] List<AreaWorldConditionChange> exitWorldConditions = new List<AreaWorldConditionChange>();
    [Tooltip("Consequence chains applied after this area is exited.")]
    [SerializeField] List<ConsequenceChainDefinition> exitedChains = new List<ConsequenceChainDefinition>();

    [Header("Events")]
    [Tooltip("Optional event published when this area is entered. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition enteredEvent = null;
    [Tooltip("Optional event published when this area is exited. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition exitedEvent = null;
    [Tooltip("Optional event published when this area enter/exit is blocked. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition blockedEvent = null;
    [Tooltip("If enabled, area events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed = true;
    [Tooltip("If enabled, area events are written to GameDebugLogger.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public AreaProfileCategory Category => category;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public RegionInfoDefinition Region => region;
    public ActivityZoneDefinition ActivityZone => activityZone;
    public MapMarkerDefinition MapMarker => mapMarker;
    public string SceneName => sceneName;
    public string AreaKey => areaKey;
    public ConsequenceChainRepeatMode RepeatMode => repeatMode;
    public int CooldownHours => Mathf.Max(0, cooldownHours);
    public int MaxEnterCount => Mathf.Max(0, maxEnterCount);
    public bool RecordHistory => recordHistory;
    public bool RecordBlockedAttempts => recordBlockedAttempts;
    public bool RequireActiveStateToExit => requireActiveStateToExit;
    public IReadOnlyList<ActivityRequirement> Requirements => requirements != null ? (IReadOnlyList<ActivityRequirement>)requirements : Array.Empty<ActivityRequirement>();
    public IReadOnlyList<LocationVisitDefinition> EnterLocationVisits => enterLocationVisits != null ? (IReadOnlyList<LocationVisitDefinition>)enterLocationVisits : Array.Empty<LocationVisitDefinition>();
    public IReadOnlyList<WorldDiscoveryDefinition> EnterWorldDiscoveries => enterWorldDiscoveries != null ? (IReadOnlyList<WorldDiscoveryDefinition>)enterWorldDiscoveries : Array.Empty<WorldDiscoveryDefinition>();
    public IReadOnlyList<AreaNavigationHintAction> EnterNavigationHints => enterNavigationHints != null ? (IReadOnlyList<AreaNavigationHintAction>)enterNavigationHints : Array.Empty<AreaNavigationHintAction>();
    public IReadOnlyList<ChronicleEntryDefinition> EnterChronicleEntries => enterChronicleEntries != null ? (IReadOnlyList<ChronicleEntryDefinition>)enterChronicleEntries : Array.Empty<ChronicleEntryDefinition>();
    public IReadOnlyList<AreaWorldConditionChange> EnterWorldConditions => enterWorldConditions != null ? (IReadOnlyList<AreaWorldConditionChange>)enterWorldConditions : Array.Empty<AreaWorldConditionChange>();
    public IReadOnlyList<ConsequenceChainDefinition> EnteredChains => enteredChains != null ? (IReadOnlyList<ConsequenceChainDefinition>)enteredChains : Array.Empty<ConsequenceChainDefinition>();
    public IReadOnlyList<AreaNavigationHintAction> ExitNavigationHints => exitNavigationHints != null ? (IReadOnlyList<AreaNavigationHintAction>)exitNavigationHints : Array.Empty<AreaNavigationHintAction>();
    public IReadOnlyList<ChronicleEntryDefinition> ExitChronicleEntries => exitChronicleEntries != null ? (IReadOnlyList<ChronicleEntryDefinition>)exitChronicleEntries : Array.Empty<ChronicleEntryDefinition>();
    public IReadOnlyList<AreaWorldConditionChange> ExitWorldConditions => exitWorldConditions != null ? (IReadOnlyList<AreaWorldConditionChange>)exitWorldConditions : Array.Empty<AreaWorldConditionChange>();
    public IReadOnlyList<ConsequenceChainDefinition> ExitedChains => exitedChains != null ? (IReadOnlyList<ConsequenceChainDefinition>)exitedChains : Array.Empty<ConsequenceChainDefinition>();

    public bool CanEnter(PlayerController player, PlayerAreaProfileLog log, string sourceId, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required for area profiles.";
            return false;
        }

        if(log != null && !log.CanEnter(this, sourceId, repeatMode, CooldownHours, MaxEnterCount, out failureMessage)) {
            return false;
        }

        if(!ConsequenceChainDefinition.RequirementsMet(player, requirements, requirementMatchMode, out failureMessage)) {
            return false;
        }

        failureMessage = null;
        return true;
    }

    public AreaProfileResult Enter(PlayerController player, string sourceId = null, string sourceName = null, UnityEngine.Object context = null, Vector3? worldPosition = null, bool applyConsequences = true) {
        var result = CreateResult(AreaProfileOperation.Enter, sourceId, sourceName, worldPosition);
        var log = player != null ? player.GetComponent<PlayerAreaProfileLog>() ?? player.gameObject.AddComponent<PlayerAreaProfileLog>() : null;

        if(!CanEnter(player, log, result.sourceId, out var failureMessage)) {
            result.blocked = true;
            result.failureMessage = failureMessage;
            if(recordHistory && recordBlockedAttempts) {
                log?.RecordBlocked(this, result);
            }

            PublishAreaEvent(blockedEvent, "blocked", result, player, context, GameEventImportance.Warning);
            return result;
        }

        ApplyEnterActions(player, result, context, worldPosition);
        if(recordHistory) {
            result.state = log?.EnterArea(this, result);
        }

        if(applyConsequences) {
            ApplyChains(player, enteredChains, result, context, "entered");
        }

        log?.SyncRecordWithResult(result);
        PublishAreaEvent(enteredEvent, "entered", result, player, context, GameEventImportance.Success);
        return result;
    }

    public AreaProfileResult Exit(PlayerController player, string sourceId = null, string sourceName = null, UnityEngine.Object context = null, Vector3? worldPosition = null, bool applyConsequences = true) {
        var result = CreateResult(AreaProfileOperation.Exit, sourceId, sourceName, worldPosition);
        var log = player != null ? player.GetComponent<PlayerAreaProfileLog>() ?? player.gameObject.AddComponent<PlayerAreaProfileLog>() : null;

        if(player == null) {
            result.blocked = true;
            result.failureMessage = "A player is required for area profiles.";
            PublishAreaEvent(blockedEvent, "blocked", result, player, context, GameEventImportance.Warning);
            return result;
        }

        if(requireActiveStateToExit && (log == null || !log.HasActiveArea(this, result.sourceId))) {
            result.blocked = true;
            result.failureMessage = $"{DisplayName} is not active.";
            PublishAreaEvent(blockedEvent, "blocked", result, player, context, GameEventImportance.Warning);
            return result;
        }

        ApplyExitActions(player, result, context, worldPosition);
        if(recordHistory) {
            log?.ExitArea(this, result);
        }

        if(applyConsequences) {
            ApplyChains(player, exitedChains, result, context, "exited");
        }

        log?.SyncRecordWithResult(result);
        PublishAreaEvent(exitedEvent, "exited", result, player, context, GameEventImportance.Info);
        return result;
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    AreaProfileResult CreateResult(AreaProfileOperation operation, string sourceId, string sourceName, Vector3? worldPosition) {
        return new AreaProfileResult(
            Id,
            DisplayName,
            category,
            operation,
            NormalizeSourceId(sourceId),
            sourceName,
            ResolveSceneName(),
            ResolveAreaKey(),
            worldPosition);
    }

    void ApplyEnterActions(PlayerController player, AreaProfileResult result, UnityEngine.Object context, Vector3? worldPosition) {
        ApplyLocationVisits(player, result, context);
        ApplyWorldDiscoveries(player, result, context);
        ApplyNavigationHints(player, enterNavigationHints, result, context, worldPosition);
        ApplyChronicleEntries(player, enterChronicleEntries, result, context);
        ApplyWorldConditions(player, enterWorldConditions, result);
    }

    void ApplyExitActions(PlayerController player, AreaProfileResult result, UnityEngine.Object context, Vector3? worldPosition) {
        ApplyNavigationHints(player, exitNavigationHints, result, context, worldPosition);
        ApplyChronicleEntries(player, exitChronicleEntries, result, context);
        ApplyWorldConditions(player, exitWorldConditions, result);
    }

    void ApplyLocationVisits(PlayerController player, AreaProfileResult result, UnityEngine.Object context) {
        foreach(var visit in EnterLocationVisits) {
            if(visit == null) {
                result.skippedActions++;
                continue;
            }

            var visitResult = visit.Apply(player, result.sourceId, result.sourceName, context != null ? context : this, applyConsequences: false);
            if(visitResult != null && !visitResult.blocked) {
                result.locationVisitsApplied++;
            } else {
                result.blockedActions++;
                if(visitResult != null && !string.IsNullOrWhiteSpace(visitResult.failureMessage)) {
                    result.messages.Add($"{visit.DisplayName}: {visitResult.failureMessage}");
                }
            }
        }
    }

    void ApplyWorldDiscoveries(PlayerController player, AreaProfileResult result, UnityEngine.Object context) {
        foreach(var discovery in EnterWorldDiscoveries) {
            if(discovery == null) {
                result.skippedActions++;
                continue;
            }

            var discoveryResult = discovery.Apply(player, result.sourceId, result.sourceName, context != null ? context : this);
            if(discoveryResult != null && !discoveryResult.blocked) {
                result.worldDiscoveriesApplied++;
            } else {
                result.blockedActions++;
                if(discoveryResult != null && !string.IsNullOrWhiteSpace(discoveryResult.failureMessage)) {
                    result.messages.Add($"{discovery.DisplayName}: {discoveryResult.failureMessage}");
                }
            }
        }
    }

    void ApplyNavigationHints(PlayerController player, IEnumerable<AreaNavigationHintAction> actions, AreaProfileResult result, UnityEngine.Object context, Vector3? worldPosition) {
        foreach(var action in actions ?? Array.Empty<AreaNavigationHintAction>()) {
            if(action == null || action.Hint == null) {
                result.skippedActions++;
                continue;
            }

            var hintResult = action.Apply(player, result.sourceId, result.sourceName, context != null ? context : this, worldPosition);
            if(hintResult != null && !hintResult.blocked) {
                result.navigationHintsApplied++;
            } else {
                result.blockedActions++;
                if(hintResult != null && !string.IsNullOrWhiteSpace(hintResult.failureMessage)) {
                    result.messages.Add($"{action.Hint.DisplayName}: {hintResult.failureMessage}");
                }
            }
        }
    }

    void ApplyChronicleEntries(PlayerController player, IEnumerable<ChronicleEntryDefinition> entries, AreaProfileResult result, UnityEngine.Object context) {
        foreach(var entry in entries ?? Array.Empty<ChronicleEntryDefinition>()) {
            if(entry == null) {
                result.skippedActions++;
                continue;
            }

            var entryResult = entry.Apply(player, result.sourceId, result.sourceName, context != null ? context : this, applyConsequences: false);
            if(entryResult != null && !entryResult.blocked) {
                result.chronicleEntriesApplied++;
            } else {
                result.blockedActions++;
                if(entryResult != null && !string.IsNullOrWhiteSpace(entryResult.failureMessage)) {
                    result.messages.Add($"{entry.DisplayName}: {entryResult.failureMessage}");
                }
            }
        }
    }

    void ApplyWorldConditions(PlayerController player, IEnumerable<AreaWorldConditionChange> changes, AreaProfileResult result) {
        foreach(var change in changes ?? Array.Empty<AreaWorldConditionChange>()) {
            if(change == null || change.Condition == null) {
                result.skippedActions++;
                continue;
            }

            if(change.Apply(player, result.sourceId, result.sourceName, region, activityZone)) {
                result.worldConditionsApplied++;
            } else {
                result.blockedActions++;
                result.messages.Add($"{change.Condition.DisplayName}: world condition did not change.");
            }
        }
    }

    void ApplyChains(PlayerController player, IEnumerable<ConsequenceChainDefinition> chains, AreaProfileResult result, UnityEngine.Object context, string phase) {
        if(player == null || chains == null) {
            return;
        }

        var chainContext = new ConsequenceChainContext {
            SourceId = $"{result.sourceId}:{phase}",
            SourceName = result.sourceName,
            Region = region,
            Zone = activityZone,
            ContextObject = context != null ? context : this
        };

        foreach(var chain in chains) {
            if(chain == null) {
                result.skippedChains++;
                continue;
            }

            var chainResult = chain.Apply(player, chainContext, context != null ? context : this);
            if(chainResult != null && !chainResult.blocked) {
                result.appliedChains++;
            } else {
                result.blockedChains++;
                if(chainResult != null && !string.IsNullOrWhiteSpace(chainResult.failureMessage)) {
                    result.messages.Add($"{chain.DisplayName}: {chainResult.failureMessage}");
                }
            }
        }
    }

    string ResolveSceneName() {
        if(!string.IsNullOrWhiteSpace(sceneName)) {
            return sceneName;
        }

        if(region != null && !string.IsNullOrWhiteSpace(region.SceneName)) {
            return region.SceneName;
        }

        return SceneManager.GetActiveScene().name;
    }

    string ResolveAreaKey() {
        if(!string.IsNullOrWhiteSpace(areaKey)) {
            return areaKey;
        }

        if(activityZone != null) {
            return activityZone.Id;
        }

        if(mapMarker != null) {
            return mapMarker.Id;
        }

        if(region != null) {
            return region.Id;
        }

        return Id;
    }

    static string NormalizeSourceId(string sourceId) {
        return string.IsNullOrWhiteSpace(sourceId) ? "area-profile" : sourceId;
    }

    void PublishAreaEvent(GameEventDefinition eventDefinition, string phase, AreaProfileResult result, PlayerController player, UnityEngine.Object context, GameEventImportance importance) {
        GameEventPublishing.PublishOptional(
            eventDefinition,
            $"area-profile.{phase}.{Id}",
            phase == "blocked" ? $"{DisplayName} blocked: {result?.failureMessage}" : $"{DisplayName} {phase}.",
            GameEventCategory.AreaProfile,
            importance,
            context != null ? context : player != null ? player : this,
            "AreaProfileDefinition",
            GameEventScope.Player,
            showInFeed: showEventsInFeed,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("areaProfileId", Id),
            GameEventPublishing.Value("areaProfileName", DisplayName),
            GameEventPublishing.Value("category", category),
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("sourceId", result != null ? result.sourceId : string.Empty),
            GameEventPublishing.Value("sceneName", result != null ? result.sceneName : string.Empty),
            GameEventPublishing.Value("areaKey", result != null ? result.areaKey : string.Empty),
            GameEventPublishing.Value("region", region != null ? region.Id : string.Empty),
            GameEventPublishing.Value("activityZone", activityZone != null ? activityZone.Id : string.Empty),
            GameEventPublishing.Value("blocked", result != null && result.blocked));
    }
}

[Serializable]
public class AreaNavigationHintAction {
    [Tooltip("Navigation hint affected by this action.")]
    [SerializeField] NavigationHintDefinition hint = null;
    [Tooltip("Operation applied to the selected navigation hint.")]
    [SerializeField] NavigationHintOperation operation = NavigationHintOperation.Activate;

    public NavigationHintDefinition Hint => hint;
    public NavigationHintOperation Operation => operation;

    public NavigationHintResult Apply(PlayerController player, string sourceId, string sourceName, UnityEngine.Object context, Vector3? worldPosition) {
        if(hint == null) {
            return null;
        }

        switch(operation) {
            case NavigationHintOperation.Complete:
                return hint.Complete(player, sourceId, sourceName, context, applyConsequences: false);
            case NavigationHintOperation.Clear:
                return hint.Clear(player, sourceId, sourceName, context, applyConsequences: false);
            default:
                return hint.Activate(player, sourceId, sourceName, context, worldPosition, applyConsequences: false);
        }
    }
}

[Serializable]
public class AreaWorldConditionChange {
    [Tooltip("World condition affected by this area action.")]
    [SerializeField] WorldConditionDefinition condition = null;
    [Tooltip("Operation applied to the selected world condition.")]
    [SerializeField] AreaWorldConditionOperation operation = AreaWorldConditionOperation.Activate;
    [Tooltip("Duration override in in-game hours. -1 uses the condition default, 0 means no automatic expiry.")]
    [Min(-1)]
    [SerializeField] int durationOverrideHours = -1;
    [Tooltip("Strength of this condition instance. 1 uses definition values as-is.")]
    [Min(0f)]
    [SerializeField] float intensity = 1f;
    [Tooltip("Number of stacks applied when activating this condition.")]
    [Min(1)]
    [SerializeField] int stacks = 1;
    [Tooltip("If enabled, activating an already active condition refreshes its duration and intensity.")]
    [SerializeField] bool refreshExisting = true;
    [Tooltip("If enabled, activating an already active condition adds stacks.")]
    [SerializeField] bool stackExisting;

    public WorldConditionDefinition Condition => condition;
    public AreaWorldConditionOperation Operation => operation;

    public bool Apply(PlayerController player, string sourceId, string sourceName, RegionInfoDefinition region, ActivityZoneDefinition zone) {
        if(player == null || condition == null) {
            return false;
        }

        var log = player.GetComponent<PlayerWorldConditionLog>() ?? player.gameObject.AddComponent<PlayerWorldConditionLog>();
        switch(operation) {
            case AreaWorldConditionOperation.Deactivate:
                return log.DeactivateCondition(condition, sourceId, region, zone);
            case AreaWorldConditionOperation.Toggle:
                if(log.IsConditionActive(condition, sourceId, region, zone)) {
                    return log.DeactivateCondition(condition, sourceId, region, zone);
                }
                return log.ActivateCondition(condition, sourceId, sourceName, region, zone, durationOverrideHours, intensity, stacks, refreshExisting, stackExisting) != null;
            default:
                return log.ActivateCondition(condition, sourceId, sourceName, region, zone, durationOverrideHours, intensity, stacks, refreshExisting, stackExisting) != null;
        }
    }
}

public class AreaProfileResult {
    public readonly string areaProfileId;
    public readonly string areaProfileName;
    public readonly AreaProfileCategory category;
    public readonly AreaProfileOperation operation;
    public readonly string sourceId;
    public readonly string sourceName;
    public readonly string sceneName;
    public readonly string areaKey;
    public readonly Vector3? worldPosition;
    public PlayerAreaProfileState state;
    public PlayerAreaProfileRecord record;
    public int locationVisitsApplied;
    public int worldDiscoveriesApplied;
    public int navigationHintsApplied;
    public int chronicleEntriesApplied;
    public int worldConditionsApplied;
    public int skippedActions;
    public int blockedActions;
    public int appliedChains;
    public int blockedChains;
    public int skippedChains;
    public bool blocked;
    public string failureMessage;
    public readonly List<string> messages = new List<string>();

    public AreaProfileResult(string areaProfileId, string areaProfileName, AreaProfileCategory category, AreaProfileOperation operation, string sourceId, string sourceName, string sceneName, string areaKey, Vector3? worldPosition) {
        this.areaProfileId = areaProfileId;
        this.areaProfileName = areaProfileName;
        this.category = category;
        this.operation = operation;
        this.sourceId = sourceId;
        this.sourceName = sourceName;
        this.sceneName = sceneName;
        this.areaKey = areaKey;
        this.worldPosition = worldPosition;
    }
}
