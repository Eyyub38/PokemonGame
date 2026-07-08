using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum ConsequenceChainRepeatMode {
    Unlimited,
    OnceEver,
    OncePerSource,
    Daily,
    CooldownHours
}

public enum ConsequenceRequirementMatchMode {
    All,
    Any
}

public enum ConsequenceStepAction {
    PublishGameEvent,
    CompleteMilestones,
    ApplyTitleGrants,
    ApplyReputationChanges,
    ApplyRelationshipChanges,
    RecordRiskIncident,
    ClearRisk,
    RecordLawViolation,
    ActivateWorldCondition,
    DeactivateWorldCondition,
    ToggleWorldCondition,
    UnlockRumor,
    HearRumor,
    SeedRumorLifecycle,
    SetSceneObjectState,
    ClearSceneObjectState,
    RecordSceneObjectInteraction,
    RecordWorldDiscovery,
    RecordLocationVisit,
    RecordChronicleEntry,
    ActivateNavigationHint,
    CompleteNavigationHint,
    ClearNavigationHint,
    EnterAreaProfile,
    ExitAreaProfile,
    ApplyLifePathRewards
}

[CreateAssetMenu(menuName = "Consequences/Consequence Chain Definition")]
public class ConsequenceChainDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this consequence chain. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in debug/future UI. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note explaining what this chain represents.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Free-form tags used by validators, requirements and future UI filters.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Repeat Rules")]
    [Tooltip("How often this chain can run.")]
    [SerializeField] ConsequenceChainRepeatMode repeatMode = ConsequenceChainRepeatMode.Unlimited;
    [Tooltip("Cooldown in in-game hours when repeat mode is Cooldown Hours.")]
    [Min(0)]
    [SerializeField] int cooldownHours;
    [Tooltip("Maximum total run count. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxRunCount;
    [Tooltip("If enabled, successful and blocked chain attempts are saved in PlayerConsequenceChainLog.")]
    [SerializeField] bool recordHistory = true;

    [Header("Requirements")]
    [Tooltip("How chain-level requirements are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode requirementMatchMode = ConsequenceRequirementMatchMode.All;
    [Tooltip("Requirements checked before the chain can run.")]
    [SerializeField] List<ActivityRequirement> requirements = new List<ActivityRequirement>();

    [Header("Execution")]
    [Tooltip("If enabled, the chain stops when a step fails to apply.")]
    [SerializeField] bool stopOnFailedStep = true;
    [Tooltip("If enabled, the chain stops when a step's own requirements fail.")]
    [SerializeField] bool stopOnFailedStepRequirement;
    [Tooltip("Steps executed in order when this chain runs.")]
    [SerializeField] List<ConsequenceChainStep> steps = new List<ConsequenceChainStep>();

    [Header("Events")]
    [Tooltip("Optional event published when this chain starts. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition startedEvent = null;
    [Tooltip("Optional event published when this chain finishes. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition completedEvent = null;
    [Tooltip("Optional event published when this chain is blocked by repeat rules or requirements.")]
    [SerializeField] GameEventDefinition blockedEvent = null;
    [Tooltip("If enabled, chain events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed;
    [Tooltip("If enabled, chain events are written to the debug log.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public ConsequenceChainRepeatMode RepeatMode => repeatMode;
    public int CooldownHours => Mathf.Max(0, cooldownHours);
    public int MaxRunCount => Mathf.Max(0, maxRunCount);
    public bool RecordHistory => recordHistory;
    public IReadOnlyList<ActivityRequirement> Requirements => requirements != null ? (IReadOnlyList<ActivityRequirement>)requirements : Array.Empty<ActivityRequirement>();
    public IReadOnlyList<ConsequenceChainStep> Steps => steps != null ? (IReadOnlyList<ConsequenceChainStep>)steps : Array.Empty<ConsequenceChainStep>();

    public bool CanRun(PlayerController player, ConsequenceChainContext context, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required for consequence chains.";
            return false;
        }

        string sourceId = context != null ? context.SourceId : null;
        var log = player.GetComponent<PlayerConsequenceChainLog>();
        if(log != null && !log.CanRun(this, sourceId, repeatMode, CooldownHours, MaxRunCount, out failureMessage)) {
            return false;
        }

        if(!RequirementsMet(player, requirements, requirementMatchMode, out failureMessage)) {
            return false;
        }

        failureMessage = null;
        return true;
    }

    public ConsequenceChainRunResult Apply(PlayerController player, ConsequenceChainContext context = null, UnityEngine.Object unityContext = null) {
        context ??= new ConsequenceChainContext();
        context.ContextObject ??= unityContext != null ? unityContext : this;
        var result = new ConsequenceChainRunResult(Id, DisplayName, context.SourceId);

        var log = player != null ? player.GetComponent<PlayerConsequenceChainLog>() ?? player.gameObject.AddComponent<PlayerConsequenceChainLog>() : null;
        if(!CanRun(player, context, out var failureMessage)) {
            result.blocked = true;
            result.failureMessage = failureMessage;
            if(recordHistory) {
                log?.RecordRun(this, context.SourceId, result);
            }
            PublishChainEvent(blockedEvent, "blocked", result, player, context, GameEventImportance.Warning);
            return result;
        }

        PublishChainEvent(startedEvent, "started", result, player, context, GameEventImportance.Info);
        foreach(var step in Steps) {
            if(step == null || !step.Enabled) {
                result.skippedSteps++;
                continue;
            }

            if(!step.RequirementsMet(player, out var stepFailure)) {
                result.failedSteps++;
                result.messages.Add($"{step.StepId}: {stepFailure}");
                if(step.StopChainWhenRequirementsFail || stopOnFailedStepRequirement) {
                    result.stoppedEarly = true;
                    break;
                }
                continue;
            }

            bool applied = step.TryApply(player, context, out var message);
            if(applied) {
                result.appliedSteps++;
            } else {
                result.failedSteps++;
                if(!string.IsNullOrWhiteSpace(message)) {
                    result.messages.Add($"{step.StepId}: {message}");
                }

                if(step.StopChainWhenFailed || stopOnFailedStep) {
                    result.stoppedEarly = true;
                    break;
                }
            }
        }

        if(recordHistory) {
            log?.RecordRun(this, context.SourceId, result);
        }

        PublishChainEvent(completedEvent, "completed", result, player, context, result.failedSteps > 0 ? GameEventImportance.Warning : GameEventImportance.Success);
        return result;
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    internal static bool RequirementsMet(PlayerController player, List<ActivityRequirement> requirements, ConsequenceRequirementMatchMode matchMode, out string failureMessage) {
        var activeRequirements = requirements?.Where(requirement => requirement != null).ToList() ?? new List<ActivityRequirement>();
        if(activeRequirements.Count == 0) {
            failureMessage = null;
            return true;
        }

        if(matchMode == ConsequenceRequirementMatchMode.Any) {
            foreach(var requirement in activeRequirements) {
                if(requirement.IsMet(player)) {
                    failureMessage = null;
                    return true;
                }
            }

            failureMessage = activeRequirements.FirstOrDefault()?.FailureMessage ?? "Consequence requirements are not met.";
            return false;
        }

        foreach(var requirement in activeRequirements) {
            if(!requirement.IsMet(player)) {
                failureMessage = requirement.FailureMessage;
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    void PublishChainEvent(GameEventDefinition eventDefinition, string phase, ConsequenceChainRunResult result, PlayerController player, ConsequenceChainContext context, GameEventImportance importance) {
        GameEventPublishing.PublishOptional(
            eventDefinition,
            $"consequence-chain.{phase}.{Id}",
            phase == "blocked" ? $"{DisplayName} blocked." : $"{DisplayName} {phase}.",
            GameEventCategory.Consequence,
            importance,
            context != null && context.ContextObject != null ? context.ContextObject : player,
            "ConsequenceChainDefinition",
            GameEventScope.Player,
            showInFeed: showEventsInFeed,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("chainId", Id),
            GameEventPublishing.Value("chainName", DisplayName),
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("sourceId", context != null ? context.SourceId : string.Empty),
            GameEventPublishing.Value("appliedSteps", result != null ? result.appliedSteps : 0),
            GameEventPublishing.Value("failedSteps", result != null ? result.failedSteps : 0),
            GameEventPublishing.Value("skippedSteps", result != null ? result.skippedSteps : 0),
            GameEventPublishing.Value("blocked", result != null && result.blocked));
    }
}

[Serializable]
public class ConsequenceChainStep {
    [Tooltip("Optional step id used by debug/history. Empty uses the action type.")]
    [SerializeField] string stepId = string.Empty;
    [Tooltip("If disabled, this step is skipped.")]
    [SerializeField] bool enabled = true;
    [Tooltip("Action executed by this step.")]
    [SerializeField] ConsequenceStepAction action = ConsequenceStepAction.PublishGameEvent;
    [Tooltip("How this step's requirements are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode requirementMatchMode = ConsequenceRequirementMatchMode.All;
    [Tooltip("Requirements checked before this step can apply.")]
    [SerializeField] List<ActivityRequirement> requirements = new List<ActivityRequirement>();
    [Tooltip("If enabled, the chain stops when this step's requirements fail.")]
    [SerializeField] bool stopChainWhenRequirementsFail;
    [Tooltip("If enabled, the chain stops when this step fails to apply.")]
    [SerializeField] bool stopChainWhenFailed = true;

    [Header("Context Overrides")]
    [Tooltip("Optional source id override for this step. Empty uses the chain context source id.")]
    [SerializeField] string sourceIdOverride = string.Empty;
    [Tooltip("Optional reporter id override for this step. Empty uses the chain context reporter id.")]
    [SerializeField] string reporterIdOverride = string.Empty;
    [Tooltip("Optional region override for this step. Empty uses the chain context region.")]
    [SerializeField] RegionInfoDefinition regionOverride = null;
    [Tooltip("Optional activity zone override for this step. Empty uses the chain context zone.")]
    [SerializeField] ActivityZoneDefinition zoneOverride = null;
    [Tooltip("Optional authority faction override for this step.")]
    [SerializeField] ReputationFactionDefinition authorityFactionOverride = null;
    [Tooltip("Optional authority id override. Empty uses Authority Faction or chain context.")]
    [SerializeField] string authorityIdOverride = string.Empty;
    [Tooltip("Optional authority display name override.")]
    [SerializeField] string authorityNameOverride = string.Empty;

    [Header("Shared Targets")]
    [Tooltip("Optional event used by Publish Game Event action.")]
    [SerializeField] GameEventDefinition gameEvent = null;
    [Tooltip("Milestones completed by Complete Milestones action.")]
    [SerializeField] List<MilestoneDefinition> milestones = new List<MilestoneDefinition>();
    [Tooltip("Title grants applied by Apply Title Grants action.")]
    [SerializeField] List<TitleGrant> titleGrants = new List<TitleGrant>();
    [Tooltip("Reputation changes applied by Apply Reputation Changes action.")]
    [SerializeField] List<ReputationChange> reputationChanges = new List<ReputationChange>();
    [Tooltip("Relationship changes applied by Apply Relationship Changes action.")]
    [SerializeField] List<RelationshipChange> relationshipChanges = new List<RelationshipChange>();
    [Tooltip("Risk incident used by Record Risk Incident action.")]
    [SerializeField] RiskIncidentDefinition riskIncident = null;
    [Tooltip("Law violation used by Record Law Violation action.")]
    [SerializeField] LawViolationDefinition lawViolation = null;
    [Tooltip("World condition used by world condition actions.")]
    [SerializeField] WorldConditionDefinition worldCondition = null;
    [Tooltip("Rumor used by rumor actions.")]
    [SerializeField] RumorDefinition rumor = null;
    [Tooltip("Optional rumor source override. Empty uses the chain context rumor source.")]
    [SerializeField] RumorSource rumorSource = null;
    [Tooltip("Scene object used by scene object state actions.")]
    [SerializeField] SceneObjectDefinition sceneObject = null;
    [Tooltip("State applied by Set Scene Object State action.")]
    [SerializeField] SceneObjectState sceneObjectState = SceneObjectState.Available;
    [Tooltip("World discovery used by Record World Discovery action.")]
    [SerializeField] WorldDiscoveryDefinition worldDiscovery = null;
    [Tooltip("Location visit used by Record Location Visit action.")]
    [SerializeField] LocationVisitDefinition locationVisit = null;
    [Tooltip("Chronicle entry used by Record Chronicle Entry action.")]
    [SerializeField] ChronicleEntryDefinition chronicleEntry = null;
    [Tooltip("Navigation hint used by navigation hint actions.")]
    [SerializeField] NavigationHintDefinition navigationHint = null;
    [Tooltip("Area profile used by area profile actions.")]
    [SerializeField] AreaProfileDefinition areaProfile = null;
    [Tooltip("Life Path XP, branch progress, tag counters or perk unlocks awarded by Apply Life Path Rewards action.")]
    [SerializeField] List<LifePathReward> lifePathRewards = new List<LifePathReward>();

    [Header("World Condition Options")]
    [Tooltip("World condition duration override in in-game hours. -1 uses the condition default, 0 means no automatic expiry.")]
    [Min(-1)]
    [SerializeField] int worldConditionDurationOverrideHours = -1;
    [Tooltip("World condition intensity. 1 uses definition values as-is.")]
    [Min(0f)]
    [SerializeField] float worldConditionIntensity = 1f;
    [Tooltip("World condition stacks added by this step.")]
    [Min(1)]
    [SerializeField] int worldConditionStacks = 1;
    [Tooltip("If enabled, activating an existing world condition refreshes it.")]
    [SerializeField] bool refreshExistingWorldCondition = true;
    [Tooltip("If enabled, activating an existing world condition adds stacks.")]
    [SerializeField] bool stackExistingWorldCondition;

    [Header("Consequence Options")]
    [Tooltip("If enabled, risk or law linked consequences are applied.")]
    [SerializeField] bool applyConsequences = true;
    [Tooltip("If enabled, Clear Risk filters by the resolved authority id.")]
    [SerializeField] bool clearRiskByAuthority = true;
    [Tooltip("If enabled, Clear Risk filters by the resolved region id.")]
    [SerializeField] bool clearRiskByRegion;
    [Tooltip("If enabled, Clear Risk filters by the resolved source id.")]
    [SerializeField] bool clearRiskBySource;

    public string StepId => string.IsNullOrWhiteSpace(stepId) ? action.ToString() : stepId;
    public bool Enabled => enabled;
    public bool StopChainWhenRequirementsFail => stopChainWhenRequirementsFail;
    public bool StopChainWhenFailed => stopChainWhenFailed;
    public ConsequenceStepAction Action => action;
    public IReadOnlyList<ActivityRequirement> Requirements => requirements != null ? (IReadOnlyList<ActivityRequirement>)requirements : Array.Empty<ActivityRequirement>();
    public GameEventDefinition GameEvent => gameEvent;
    public IReadOnlyList<MilestoneDefinition> Milestones => milestones != null ? (IReadOnlyList<MilestoneDefinition>)milestones : Array.Empty<MilestoneDefinition>();
    public IReadOnlyList<TitleGrant> TitleGrants => titleGrants != null ? (IReadOnlyList<TitleGrant>)titleGrants : Array.Empty<TitleGrant>();
    public IReadOnlyList<ReputationChange> ReputationChanges => reputationChanges != null ? (IReadOnlyList<ReputationChange>)reputationChanges : Array.Empty<ReputationChange>();
    public IReadOnlyList<RelationshipChange> RelationshipChanges => relationshipChanges != null ? (IReadOnlyList<RelationshipChange>)relationshipChanges : Array.Empty<RelationshipChange>();
    public RiskIncidentDefinition RiskIncident => riskIncident;
    public LawViolationDefinition LawViolation => lawViolation;
    public WorldConditionDefinition WorldCondition => worldCondition;
    public RumorDefinition Rumor => rumor;
    public RumorSource RumorSource => rumorSource;
    public SceneObjectDefinition SceneObject => sceneObject;
    public SceneObjectState SceneObjectState => sceneObjectState;
    public WorldDiscoveryDefinition WorldDiscovery => worldDiscovery;
    public LocationVisitDefinition LocationVisit => locationVisit;
    public ChronicleEntryDefinition ChronicleEntry => chronicleEntry;
    public NavigationHintDefinition NavigationHint => navigationHint;
    public AreaProfileDefinition AreaProfile => areaProfile;
    public IReadOnlyList<LifePathReward> LifePathRewards => lifePathRewards != null ? (IReadOnlyList<LifePathReward>)lifePathRewards : Array.Empty<LifePathReward>();

    public bool RequirementsMet(PlayerController player, out string failureMessage) {
        return ConsequenceChainDefinition.RequirementsMet(player, requirements, requirementMatchMode, out failureMessage);
    }

    public bool TryApply(PlayerController player, ConsequenceChainContext context, out string message) {
        if(player == null) {
            message = "Player is missing.";
            return false;
        }

        context ??= new ConsequenceChainContext();
        string sourceId = ResolveSourceId(context);
        string reporterId = ResolveReporterId(context);
        var region = regionOverride != null ? regionOverride : context.Region;
        var zone = zoneOverride != null ? zoneOverride : context.Zone;
        string authorityId = ResolveAuthorityId(context);
        string authorityName = ResolveAuthorityName(context, authorityId);
        var unityContext = context.ContextObject;

        switch(action) {
            case ConsequenceStepAction.PublishGameEvent:
                PublishStepEvent(player, context, sourceId);
                message = null;
                return true;
            case ConsequenceStepAction.CompleteMilestones:
                player.GetComponent<PlayerMilestones>()?.CompleteMilestones(milestones);
                message = null;
                return true;
            case ConsequenceStepAction.ApplyTitleGrants:
                player.GetComponent<PlayerTitles>()?.ApplyGrants(titleGrants, unityContext);
                message = null;
                return true;
            case ConsequenceStepAction.ApplyReputationChanges:
                player.GetComponent<PlayerReputation>()?.ApplyChanges(reputationChanges);
                message = null;
                return true;
            case ConsequenceStepAction.ApplyRelationshipChanges:
                player.GetComponent<PlayerRelationships>()?.ApplyChanges(relationshipChanges);
                message = null;
                return true;
            case ConsequenceStepAction.RecordRiskIncident:
                return RecordRisk(player, sourceId, reporterId, region, authorityId, authorityName, unityContext, out message);
            case ConsequenceStepAction.ClearRisk:
                return ClearRisk(player, sourceId, region, authorityId, unityContext, out message);
            case ConsequenceStepAction.RecordLawViolation:
                return RecordLaw(player, sourceId, reporterId, unityContext, out message);
            case ConsequenceStepAction.ActivateWorldCondition:
                return ActivateWorldCondition(player, sourceId, region, zone, context, out message);
            case ConsequenceStepAction.DeactivateWorldCondition:
                return DeactivateWorldCondition(player, sourceId, region, zone, out message);
            case ConsequenceStepAction.ToggleWorldCondition:
                return ToggleWorldCondition(player, sourceId, region, zone, context, out message);
            case ConsequenceStepAction.UnlockRumor:
                return UnlockRumor(player, sourceId, out message);
            case ConsequenceStepAction.HearRumor:
                return HearRumor(player, sourceId, context, out message);
            case ConsequenceStepAction.SeedRumorLifecycle:
                return SeedRumor(player, context, out message);
            case ConsequenceStepAction.SetSceneObjectState:
                return SetSceneObjectState(player, sourceId, unityContext, out message);
            case ConsequenceStepAction.ClearSceneObjectState:
                return ClearSceneObjectState(player, sourceId, unityContext, out message);
            case ConsequenceStepAction.RecordSceneObjectInteraction:
                return RecordSceneObjectInteraction(player, sourceId, unityContext, out message);
            case ConsequenceStepAction.RecordWorldDiscovery:
                return RecordWorldDiscovery(player, sourceId, context, unityContext, out message);
            case ConsequenceStepAction.RecordLocationVisit:
                return RecordLocationVisit(player, sourceId, context, unityContext, out message);
            case ConsequenceStepAction.RecordChronicleEntry:
                return RecordChronicleEntry(player, sourceId, context, unityContext, out message);
            case ConsequenceStepAction.ActivateNavigationHint:
                return ActivateNavigationHint(player, sourceId, context, unityContext, out message);
            case ConsequenceStepAction.CompleteNavigationHint:
                return CompleteNavigationHint(player, sourceId, context, unityContext, out message);
            case ConsequenceStepAction.ClearNavigationHint:
                return ClearNavigationHint(player, sourceId, context, unityContext, out message);
            case ConsequenceStepAction.EnterAreaProfile:
                return EnterAreaProfile(player, sourceId, context, unityContext, out message);
            case ConsequenceStepAction.ExitAreaProfile:
                return ExitAreaProfile(player, sourceId, context, unityContext, out message);
            case ConsequenceStepAction.ApplyLifePathRewards:
                return ApplyLifePathRewards(player, sourceId, context, unityContext, out message);
            default:
                message = "Unsupported consequence step action.";
                return false;
        }
    }

    bool RecordRisk(PlayerController player, string sourceId, string reporterId, RegionInfoDefinition region, string authorityId, string authorityName, UnityEngine.Object unityContext, out string message) {
        if(riskIncident == null) {
            message = "Risk incident is missing.";
            return false;
        }

        var log = player.GetComponent<PlayerRiskLog>() ?? player.gameObject.AddComponent<PlayerRiskLog>();
        log.RecordIncident(riskIncident, sourceId, reporterId, region, authorityId, authorityName, applyConsequences, unityContext);
        message = null;
        return true;
    }

    bool ClearRisk(PlayerController player, string sourceId, RegionInfoDefinition region, string authorityId, UnityEngine.Object unityContext, out string message) {
        var log = player.GetComponent<PlayerRiskLog>();
        if(log == null) {
            message = "PlayerRiskLog is missing.";
            return false;
        }

        int cleared = log.ClearRisk(
            clearRiskByAuthority ? authorityId : null,
            clearRiskByRegion && region != null ? region.Id : null,
            clearRiskBySource ? sourceId : null,
            unityContext);
        message = cleared > 0 ? null : "No matching active risk records were cleared.";
        return cleared > 0;
    }

    bool RecordLaw(PlayerController player, string sourceId, string reporterId, UnityEngine.Object unityContext, out string message) {
        if(lawViolation == null) {
            message = "Law violation is missing.";
            return false;
        }

        var log = player.GetComponent<PlayerLawLog>() ?? player.gameObject.AddComponent<PlayerLawLog>();
        log.RecordViolation(lawViolation, sourceId, reporterId, applyConsequences, unityContext);
        message = null;
        return true;
    }

    bool ActivateWorldCondition(PlayerController player, string sourceId, RegionInfoDefinition region, ActivityZoneDefinition zone, ConsequenceChainContext context, out string message) {
        if(worldCondition == null) {
            message = "World condition is missing.";
            return false;
        }

        var log = player.GetComponent<PlayerWorldConditionLog>() ?? player.gameObject.AddComponent<PlayerWorldConditionLog>();
        log.ActivateCondition(
            worldCondition,
            sourceId,
            context != null ? context.SourceName : null,
            region,
            zone,
            worldConditionDurationOverrideHours,
            worldConditionIntensity,
            worldConditionStacks,
            refreshExistingWorldCondition,
            stackExistingWorldCondition);
        message = null;
        return true;
    }

    bool DeactivateWorldCondition(PlayerController player, string sourceId, RegionInfoDefinition region, ActivityZoneDefinition zone, out string message) {
        if(worldCondition == null) {
            message = "World condition is missing.";
            return false;
        }

        var log = player.GetComponent<PlayerWorldConditionLog>();
        bool removed = log != null && log.DeactivateCondition(worldCondition, sourceId, region, zone);
        message = removed ? null : "No matching world condition was active.";
        return removed;
    }

    bool ToggleWorldCondition(PlayerController player, string sourceId, RegionInfoDefinition region, ActivityZoneDefinition zone, ConsequenceChainContext context, out string message) {
        var log = player.GetComponent<PlayerWorldConditionLog>() ?? player.gameObject.AddComponent<PlayerWorldConditionLog>();
        if(worldCondition == null) {
            message = "World condition is missing.";
            return false;
        }

        if(log.IsConditionActive(worldCondition, sourceId, region, zone)) {
            return DeactivateWorldCondition(player, sourceId, region, zone, out message);
        }

        return ActivateWorldCondition(player, sourceId, region, zone, context, out message);
    }

    bool UnlockRumor(PlayerController player, string sourceId, out string message) {
        if(rumor == null) {
            message = "Rumor is missing.";
            return false;
        }

        var log = player.GetComponent<PlayerRumorLog>() ?? player.gameObject.AddComponent<PlayerRumorLog>();
        bool unlocked = log.UnlockRumor(rumor, sourceId);
        message = unlocked ? null : "Rumor was already unlocked.";
        return true;
    }

    bool HearRumor(PlayerController player, string sourceId, ConsequenceChainContext context, out string message) {
        if(rumor == null) {
            message = "Rumor is missing.";
            return false;
        }

        var log = player.GetComponent<PlayerRumorLog>() ?? player.gameObject.AddComponent<PlayerRumorLog>();
        var resolvedSource = rumorSource != null ? rumorSource : context?.RumorSource;
        if(!rumor.CanHear(player, log, sourceId, resolvedSource, out message)) {
            return false;
        }

        rumor.Apply(player, sourceId, context != null ? context.SourceName : sourceId);
        message = null;
        return true;
    }

    bool SeedRumor(PlayerController player, ConsequenceChainContext context, out string message) {
        var resolvedSource = rumorSource != null ? rumorSource : context?.RumorSource;
        if(rumor == null || resolvedSource == null) {
            message = "Rumor and rumor source are required to seed lifecycle.";
            return false;
        }

        var lifecycle = player.GetComponent<PlayerRumorLifecycleLog>() ?? player.gameObject.AddComponent<PlayerRumorLifecycleLog>();
        bool seeded = lifecycle.SeedRumor(rumor, resolvedSource, $"consequence:{StepId}");
        message = seeded ? null : "Rumor lifecycle was not seeded.";
        return seeded;
    }

    bool SetSceneObjectState(PlayerController player, string sourceId, UnityEngine.Object unityContext, out string message) {
        if(sceneObject == null) {
            message = "Scene object is missing.";
            return false;
        }

        var log = player.GetComponent<PlayerSceneObjectLog>() ?? player.gameObject.AddComponent<PlayerSceneObjectLog>();
        log.SetState(sceneObject, sceneObjectState, sourceId, unityContext);
        message = null;
        return true;
    }

    bool ClearSceneObjectState(PlayerController player, string sourceId, UnityEngine.Object unityContext, out string message) {
        if(sceneObject == null) {
            message = "Scene object is missing.";
            return false;
        }

        var log = player.GetComponent<PlayerSceneObjectLog>();
        bool cleared = log != null && log.ClearState(sceneObject, sourceId, unityContext);
        message = cleared ? null : "Scene object state had no saved override.";
        return cleared;
    }

    bool RecordSceneObjectInteraction(PlayerController player, string sourceId, UnityEngine.Object unityContext, out string message) {
        if(sceneObject == null) {
            message = "Scene object is missing.";
            return false;
        }

        var log = player.GetComponent<PlayerSceneObjectLog>() ?? player.gameObject.AddComponent<PlayerSceneObjectLog>();
        log.RecordInteraction(sceneObject, sourceId, unityContext);
        message = null;
        return true;
    }

    bool RecordWorldDiscovery(PlayerController player, string sourceId, ConsequenceChainContext context, UnityEngine.Object unityContext, out string message) {
        if(worldDiscovery == null) {
            message = "World discovery is missing.";
            return false;
        }

        var result = worldDiscovery.Apply(player, sourceId, context != null ? context.SourceName : null, unityContext);
        message = result != null && !result.blocked ? null : result != null ? result.failureMessage : "World discovery did not return a result.";
        return result != null && !result.blocked;
    }

    bool RecordLocationVisit(PlayerController player, string sourceId, ConsequenceChainContext context, UnityEngine.Object unityContext, out string message) {
        if(locationVisit == null) {
            message = "Location visit is missing.";
            return false;
        }

        var result = locationVisit.Apply(player, sourceId, context != null ? context.SourceName : null, unityContext, applyConsequences: false);
        message = result != null && !result.blocked ? null : result != null ? result.failureMessage : "Location visit did not return a result.";
        return result != null && !result.blocked;
    }

    bool RecordChronicleEntry(PlayerController player, string sourceId, ConsequenceChainContext context, UnityEngine.Object unityContext, out string message) {
        if(chronicleEntry == null) {
            message = "Chronicle entry is missing.";
            return false;
        }

        var result = chronicleEntry.Apply(player, sourceId, context != null ? context.SourceName : null, unityContext, applyConsequences: false);
        message = result != null && !result.blocked ? null : result != null ? result.failureMessage : "Chronicle entry did not return a result.";
        return result != null && !result.blocked;
    }

    bool ActivateNavigationHint(PlayerController player, string sourceId, ConsequenceChainContext context, UnityEngine.Object unityContext, out string message) {
        if(navigationHint == null) {
            message = "Navigation hint is missing.";
            return false;
        }

        var result = navigationHint.Activate(player, sourceId, context != null ? context.SourceName : null, unityContext, applyConsequences: false);
        message = result != null && !result.blocked ? null : result != null ? result.failureMessage : "Navigation hint did not return a result.";
        return result != null && !result.blocked;
    }

    bool CompleteNavigationHint(PlayerController player, string sourceId, ConsequenceChainContext context, UnityEngine.Object unityContext, out string message) {
        if(navigationHint == null) {
            message = "Navigation hint is missing.";
            return false;
        }

        var result = navigationHint.Complete(player, sourceId, context != null ? context.SourceName : null, unityContext, applyConsequences: false);
        message = result != null && !result.blocked ? null : result != null ? result.failureMessage : "Navigation hint did not complete.";
        return result != null && !result.blocked;
    }

    bool ClearNavigationHint(PlayerController player, string sourceId, ConsequenceChainContext context, UnityEngine.Object unityContext, out string message) {
        if(navigationHint == null) {
            message = "Navigation hint is missing.";
            return false;
        }

        var result = navigationHint.Clear(player, sourceId, context != null ? context.SourceName : null, unityContext, applyConsequences: false);
        message = result != null && !result.blocked ? null : result != null ? result.failureMessage : "Navigation hint did not clear.";
        return result != null && !result.blocked;
    }

    bool EnterAreaProfile(PlayerController player, string sourceId, ConsequenceChainContext context, UnityEngine.Object unityContext, out string message) {
        if(areaProfile == null) {
            message = "Area profile is missing.";
            return false;
        }

        var result = areaProfile.Enter(player, sourceId, context != null ? context.SourceName : null, unityContext, applyConsequences: false);
        message = result != null && !result.blocked ? null : result != null ? result.failureMessage : "Area profile did not enter.";
        return result != null && !result.blocked;
    }

    bool ExitAreaProfile(PlayerController player, string sourceId, ConsequenceChainContext context, UnityEngine.Object unityContext, out string message) {
        if(areaProfile == null) {
            message = "Area profile is missing.";
            return false;
        }

        var result = areaProfile.Exit(player, sourceId, context != null ? context.SourceName : null, unityContext, applyConsequences: false);
        message = result != null && !result.blocked ? null : result != null ? result.failureMessage : "Area profile did not exit.";
        return result != null && !result.blocked;
    }

    bool ApplyLifePathRewards(PlayerController player, string sourceId, ConsequenceChainContext context, UnityEngine.Object unityContext, out string message) {
        if(lifePathRewards == null || !lifePathRewards.Any(reward => reward != null && reward.lifePath != null && reward.HasAnyPayload)) {
            message = "Life path rewards are missing.";
            return false;
        }

        player.GetComponent<PlayerLifePathLog>()?.ApplyRewards(
            lifePathRewards,
            $"consequence:{sourceId}:{StepId}",
            context != null && !string.IsNullOrWhiteSpace(context.SourceName) ? context.SourceName : StepId,
            unityContext);
        message = null;
        return true;
    }

    void PublishStepEvent(PlayerController player, ConsequenceChainContext context, string sourceId) {
        GameEventPublishing.PublishOptional(
            gameEvent,
            $"consequence-step.{StepId}",
            $"{StepId} applied.",
            GameEventCategory.Consequence,
            GameEventImportance.Info,
            context != null && context.ContextObject != null ? context.ContextObject : player,
            "ConsequenceChainStep",
            GameEventScope.Player,
            showInFeed: false,
            writeToDebugLog: false,
            GameEventPublishing.Value("stepId", StepId),
            GameEventPublishing.Value("action", action),
            GameEventPublishing.Value("sourceId", sourceId));
    }

    string ResolveSourceId(ConsequenceChainContext context) {
        return !string.IsNullOrWhiteSpace(sourceIdOverride) ? sourceIdOverride : context != null ? context.SourceId : string.Empty;
    }

    string ResolveReporterId(ConsequenceChainContext context) {
        return !string.IsNullOrWhiteSpace(reporterIdOverride) ? reporterIdOverride : context != null ? context.ReporterId : string.Empty;
    }

    string ResolveAuthorityId(ConsequenceChainContext context) {
        if(authorityFactionOverride != null) {
            return authorityFactionOverride.Id;
        }

        if(!string.IsNullOrWhiteSpace(authorityIdOverride)) {
            return authorityIdOverride;
        }

        return context != null ? context.AuthorityId : null;
    }

    string ResolveAuthorityName(ConsequenceChainContext context, string authorityId) {
        if(authorityFactionOverride != null) {
            return authorityFactionOverride.DisplayName;
        }

        if(!string.IsNullOrWhiteSpace(authorityNameOverride)) {
            return authorityNameOverride;
        }

        return !string.IsNullOrWhiteSpace(context?.AuthorityName) ? context.AuthorityName : authorityId;
    }
}

[Serializable]
public class ConsequenceChainContext {
    [Tooltip("Source id used by logs and repeat rules.")]
    public string SourceId = string.Empty;
    [Tooltip("Source display name used by logs and debug output.")]
    public string SourceName = string.Empty;
    [Tooltip("Reporter id used by risk, law and witness-like records.")]
    public string ReporterId = string.Empty;
    [Tooltip("Authority id used by risk or law filtering.")]
    public string AuthorityId = string.Empty;
    [Tooltip("Authority display name used by debug/fallback output.")]
    public string AuthorityName = string.Empty;
    [Tooltip("Region passed to steps that need regional context.")]
    public RegionInfoDefinition Region;
    [Tooltip("Activity zone passed to steps that need zone context.")]
    public ActivityZoneDefinition Zone;
    [Tooltip("Rumor source passed to rumor lifecycle steps.")]
    public RumorSource RumorSource;
    [Tooltip("Unity context object used by debug/event logs.")]
    public UnityEngine.Object ContextObject;
}

public class ConsequenceChainRunResult {
    public readonly string chainId;
    public readonly string chainName;
    public readonly string sourceId;
    public int appliedSteps;
    public int failedSteps;
    public int skippedSteps;
    public bool blocked;
    public bool stoppedEarly;
    public string failureMessage;
    public readonly List<string> messages = new List<string>();

    public ConsequenceChainRunResult(string chainId, string chainName, string sourceId) {
        this.chainId = chainId;
        this.chainName = chainName;
        this.sourceId = sourceId;
    }
}
