using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum CompanionExpeditionRouteFailureMode {
    StopRoute,
    ContinueToNextStage,
    RetrySameStage
}

public enum CompanionExpeditionRouteStageFailureMode {
    UseRouteDefault,
    StopRoute,
    ContinueToNextStage,
    RetrySameStage
}

[CreateAssetMenu(menuName = "Companion/Expedition Route Definition")]
public class CompanionExpeditionRouteDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this companion expedition route. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in UI/debug messages. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer note or player-facing description for this route.")]
    [TextArea]
    [SerializeField] string description;
    [Tooltip("Free-form tags used by validators and future UI filters.")]
    [SerializeField] List<string> tags = new List<string>();
    [Tooltip("Priority used by future UI sorting. Higher priority appears first.")]
    [SerializeField] int priority;

    [Header("Repeat Rules")]
    [Tooltip("How often this route can be completed.")]
    [SerializeField] CompanionExpeditionRepeatMode repeatMode = CompanionExpeditionRepeatMode.Repeatable;
    [Tooltip("Cooldown in in-game hours when Repeat Mode is Cooldown Hours.")]
    [Min(0)]
    [SerializeField] int cooldownHours;
    [Tooltip("If enabled, the same route cannot be started while already active from the same source.")]
    [SerializeField] bool blockDuplicateActiveRoute = true;
    [Tooltip("If enabled, a companion cannot start this route while already active in another route.")]
    [SerializeField] bool blockBusyCompanion = true;

    [Header("Companion Requirements")]
    [Tooltip("Optional role required before this route can start.")]
    [SerializeField] CompanionRoleDefinition requiredRole;
    [Tooltip("Optional active perk required before this route can start.")]
    [SerializeField] CompanionPerkDefinition requiredPerk;
    [Tooltip("Minimum companion bond level required before this route can start.")]
    [SerializeField] CompanionBondLevel minimumBondLevel = CompanionBondLevel.Stranger;
    [Tooltip("Minimum raw bond points required before this route can start.")]
    [Min(0)]
    [SerializeField] int minimumBondPoints;
    [Tooltip("Additional requirements checked before this route can start.")]
    [SerializeField] List<ActivityRequirement> requirements = new List<ActivityRequirement>();
    [Tooltip("Message shown when access rules block this route.")]
    [SerializeField] string lockedMessage = "This companion route is not available yet.";

    [Header("Stages")]
    [Tooltip("Ordered expedition stages that make up this route.")]
    [SerializeField] List<CompanionExpeditionRouteStage> stages = new List<CompanionExpeditionRouteStage>();
    [Tooltip("Default behavior when a stage expedition fails.")]
    [SerializeField] CompanionExpeditionRouteFailureMode defaultFailureMode = CompanionExpeditionRouteFailureMode.StopRoute;
    [Tooltip("If enabled, the next stage starts automatically after a claim when possible.")]
    [SerializeField] bool autoStartNextStage = true;

    [Header("Route Outcomes")]
    [Tooltip("Outcomes rolled when the full route completes successfully.")]
    [SerializeField] List<ActivityOutcomeDefinition> routeSuccessOutcomes = new List<ActivityOutcomeDefinition>();
    [Tooltip("Outcomes rolled when the route stops because a stage failed.")]
    [SerializeField] List<ActivityOutcomeDefinition> routeFailureOutcomes = new List<ActivityOutcomeDefinition>();

    [Header("Events")]
    [Tooltip("Optional event published when the route starts. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition startedEvent;
    [Tooltip("Optional event published when a route stage starts. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition stageStartedEvent;
    [Tooltip("Optional event published when a route stage is claimed. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition stageClaimedEvent;
    [Tooltip("Optional event published when the route completes. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition completedEvent;
    [Tooltip("Optional event published when the route fails/stops. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition failedEvent;
    [Tooltip("If enabled, route events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed = true;
    [Tooltip("If enabled, route events are written to the debug log.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : System.Array.Empty<string>();
    public int Priority => priority;
    public CompanionExpeditionRepeatMode RepeatMode => repeatMode;
    public int CooldownHours => Mathf.Max(0, cooldownHours);
    public bool BlockDuplicateActiveRoute => blockDuplicateActiveRoute;
    public bool BlockBusyCompanion => blockBusyCompanion;
    public CompanionRoleDefinition RequiredRole => requiredRole;
    public CompanionPerkDefinition RequiredPerk => requiredPerk;
    public CompanionBondLevel MinimumBondLevel => minimumBondLevel;
    public int MinimumBondPoints => Mathf.Max(0, minimumBondPoints);
    public IReadOnlyList<ActivityRequirement> Requirements => requirements != null ? (IReadOnlyList<ActivityRequirement>)requirements : System.Array.Empty<ActivityRequirement>();
    public IReadOnlyList<CompanionExpeditionRouteStage> Stages => stages != null ? (IReadOnlyList<CompanionExpeditionRouteStage>)stages : System.Array.Empty<CompanionExpeditionRouteStage>();
    public CompanionExpeditionRouteFailureMode DefaultFailureMode => defaultFailureMode;
    public bool AutoStartNextStage => autoStartNextStage;
    public IReadOnlyList<ActivityOutcomeDefinition> RouteSuccessOutcomes => routeSuccessOutcomes != null ? (IReadOnlyList<ActivityOutcomeDefinition>)routeSuccessOutcomes : System.Array.Empty<ActivityOutcomeDefinition>();
    public IReadOnlyList<ActivityOutcomeDefinition> RouteFailureOutcomes => routeFailureOutcomes != null ? (IReadOnlyList<ActivityOutcomeDefinition>)routeFailureOutcomes : System.Array.Empty<ActivityOutcomeDefinition>();

    public bool CanStart(PlayerController player, CompanionController companion, PlayerCompanionExpeditionRouteLog routeLog, string sourceId, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required to start companion routes.";
            return false;
        }

        if(companion == null) {
            failureMessage = "No companion selected.";
            return false;
        }

        if(Stages.Count == 0 || GetStage(0)?.Expedition == null) {
            failureMessage = $"{DisplayName} has no usable first stage.";
            return false;
        }

        if(blockDuplicateActiveRoute && routeLog != null && routeLog.HasActiveRoute(this, sourceId)) {
            failureMessage = $"{DisplayName} is already active.";
            return false;
        }

        if(blockBusyCompanion && routeLog != null && routeLog.HasActiveRouteForCompanion(companion.CompanionId)) {
            failureMessage = $"{companion.CompanionName} is already on a route.";
            return false;
        }

        if(routeLog != null && !routeLog.CanStart(this, sourceId, repeatMode, CooldownHours, out failureMessage)) {
            return false;
        }

        if(requiredRole != null && companion.RoleDefinition != requiredRole) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{DisplayName} requires {requiredRole.DisplayName}." : lockedMessage;
            return false;
        }

        if(requiredPerk != null && !companion.HasActivePerk(requiredPerk)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{DisplayName} requires {requiredPerk.DisplayName}." : lockedMessage;
            return false;
        }

        if(companion.BondLevel < minimumBondLevel || companion.BondPoints < MinimumBondPoints) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{companion.CompanionName} needs a stronger bond first." : lockedMessage;
            return false;
        }

        foreach(var requirement in Requirements) {
            if(requirement != null && !requirement.IsMet(player)) {
                failureMessage = requirement.FailureMessage;
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    public CompanionExpeditionRouteStage GetStage(int index) {
        return index >= 0 && index < Stages.Count ? Stages[index] : null;
    }

    public string GetStageSourceId(string routeSourceId, int stageIndex) {
        var stage = GetStage(stageIndex);
        if(stage != null && !string.IsNullOrWhiteSpace(stage.SourceIdOverride)) {
            return stage.SourceIdOverride;
        }

        string normalizedSource = string.IsNullOrWhiteSpace(routeSourceId) ? "default" : routeSourceId;
        string stageId = stage != null ? stage.GetStageId(stageIndex) : stageIndex.ToString();
        return $"{Id}:{normalizedSource}:{stageId}";
    }

    public CompanionExpeditionRouteFailureMode GetFailureMode(CompanionExpeditionRouteStage stage) {
        if(stage == null || stage.FailureMode == CompanionExpeditionRouteStageFailureMode.UseRouteDefault) {
            return defaultFailureMode;
        }

        return stage.FailureMode switch {
            CompanionExpeditionRouteStageFailureMode.ContinueToNextStage => CompanionExpeditionRouteFailureMode.ContinueToNextStage,
            CompanionExpeditionRouteStageFailureMode.RetrySameStage => CompanionExpeditionRouteFailureMode.RetrySameStage,
            _ => CompanionExpeditionRouteFailureMode.StopRoute
        };
    }

    public void ApplyRouteCompleted(PlayerController player) {
        foreach(var outcome in RouteSuccessOutcomes) {
            outcome?.TryApply(player);
        }
    }

    public void ApplyRouteFailed(PlayerController player) {
        foreach(var outcome in RouteFailureOutcomes) {
            outcome?.TryApply(player);
        }
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, System.StringComparison.OrdinalIgnoreCase));
    }

    public void PublishStarted(PlayerController player, CompanionController companion, string sourceId) {
        PublishRouteEvent(startedEvent, "started", GameEventImportance.Info, player, companion, sourceId, null, false, null);
    }

    public void PublishStageStarted(PlayerController player, CompanionController companion, string sourceId, CompanionExpeditionRouteStage stage) {
        PublishRouteEvent(stageStartedEvent, "stage-started", GameEventImportance.Info, player, companion, sourceId, stage, false, null);
    }

    public void PublishStageClaimed(PlayerController player, CompanionController companion, string sourceId, CompanionExpeditionRouteStage stage, bool success) {
        PublishRouteEvent(stageClaimedEvent, "stage-claimed", success ? GameEventImportance.Success : GameEventImportance.Warning, player, companion, sourceId, stage, success, null);
    }

    public void PublishCompleted(PlayerController player, CompanionController companion, string sourceId) {
        PublishRouteEvent(completedEvent, "completed", GameEventImportance.Success, player, companion, sourceId, null, true, null);
    }

    public void PublishFailed(PlayerController player, CompanionController companion, string sourceId, CompanionExpeditionRouteStage stage) {
        PublishRouteEvent(failedEvent, "failed", GameEventImportance.Warning, player, companion, sourceId, stage, false, null);
    }

    void PublishRouteEvent(GameEventDefinition eventDefinition, string phase, GameEventImportance importance, PlayerController player, CompanionController companion, string sourceId, CompanionExpeditionRouteStage stage, bool success, string extraMessage) {
        GameEventPublishing.PublishOptional(
            eventDefinition,
            $"companion.route.{phase}.{Id}.{companion?.CompanionId}",
            extraMessage ?? $"{companion?.CompanionName ?? "Companion"} {phase} {DisplayName}.",
            GameEventCategory.Companion,
            importance,
            player != null ? player : companion,
            "CompanionExpeditionRouteDefinition",
            GameEventScope.Player,
            showInFeed: showEventsInFeed,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("routeId", Id),
            GameEventPublishing.Value("routeName", DisplayName),
            GameEventPublishing.Value("sourceId", sourceId),
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("stageId", stage != null ? stage.GetStageId(GetStageIndex(stage)) : null),
            GameEventPublishing.Value("stageName", stage != null ? stage.DisplayName : null),
            GameEventPublishing.Value("companionId", companion != null ? companion.CompanionId : null),
            GameEventPublishing.Value("companionName", companion != null ? companion.CompanionName : null),
            GameEventPublishing.Value("success", success));
    }

    int GetStageIndex(CompanionExpeditionRouteStage stage) {
        for(int i = 0; i < Stages.Count; i++) {
            if(ReferenceEquals(Stages[i], stage)) {
                return i;
            }
        }

        return -1;
    }
}

[System.Serializable]
public class CompanionExpeditionRouteStage {
    [Tooltip("Optional stable id for this stage. Empty uses the stage index.")]
    public string stageId;
    [Tooltip("Name shown in future route UI. Empty uses the expedition name.")]
    public string displayName;
    [Tooltip("Designer note or player-facing description for this route stage.")]
    [TextArea]
    public string description;
    [Tooltip("Expedition started for this stage.")]
    public CompanionExpeditionDefinition expedition;
    [Tooltip("Optional source id used for the underlying expedition. Empty generates routeId/sourceId/stageId.")]
    public string sourceIdOverride;
    [Tooltip("Additional requirements checked before this stage can start.")]
    public List<ActivityRequirement> requirements = new List<ActivityRequirement>();
    [Tooltip("How this stage behaves when its expedition fails.")]
    public CompanionExpeditionRouteStageFailureMode failureMode = CompanionExpeditionRouteStageFailureMode.UseRouteDefault;

    public CompanionExpeditionDefinition Expedition => expedition;
    public string SourceIdOverride => sourceIdOverride;
    public IReadOnlyList<ActivityRequirement> Requirements => requirements != null ? (IReadOnlyList<ActivityRequirement>)requirements : System.Array.Empty<ActivityRequirement>();
    public CompanionExpeditionRouteStageFailureMode FailureMode => failureMode;
    public string DisplayName => !string.IsNullOrWhiteSpace(displayName) ? displayName : expedition != null ? expedition.DisplayName : "Route Stage";

    public string GetStageId(int index) {
        return !string.IsNullOrWhiteSpace(stageId) ? stageId : Mathf.Max(0, index).ToString();
    }

    public bool CanStart(PlayerController player, out string failureMessage) {
        if(expedition == null) {
            failureMessage = $"{DisplayName} has no expedition.";
            return false;
        }

        foreach(var requirement in Requirements) {
            if(requirement != null && !requirement.IsMet(player)) {
                failureMessage = requirement.FailureMessage;
                return false;
            }
        }

        failureMessage = null;
        return true;
    }
}
