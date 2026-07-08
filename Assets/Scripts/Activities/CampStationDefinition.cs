using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum CampStationCategory {
    General,
    Rest,
    Sleep,
    Cooking,
    PokemonCare,
    Training,
    Social,
    Assignment,
    Research,
    Ranger,
    Travel,
    Custom
}

public enum CampStationActionType {
    Activity,
    Rest,
    Sleep,
    PokemonCareAction,
    SocialActivity,
    PokemonAssignment,
    PokemonAssignmentBoard,
    SituationEvent,
    SituationEventPool,
    RoleActivityBoard,
    LifePathRewards
}

[CreateAssetMenu(menuName = "Activities/Camp Station")]
public class CampStationDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this camp station. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in prompts, future UI and debug messages. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note or player-facing explanation of what this station does.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Broad station category used by future UI filters and content organization.")]
    [SerializeField] CampStationCategory category = CampStationCategory.General;
    [Tooltip("Free-form tags such as campfire, tent, picnic, care, ranger, cooking or research.")]
    [SerializeField] List<string> tags = new List<string>();
    [Tooltip("Optional icon used by future camp UI.")]
    [SerializeField] Sprite icon = null;

    [Header("Access")]
    [Tooltip("Optional reusable access profile checked before this station can be used.")]
    [SerializeField] AccessProfileDefinition accessProfile = null;
    [Tooltip("Additional requirements checked before this station can be used.")]
    [SerializeField] List<ActivityRequirement> requirements = new List<ActivityRequirement>();
    [Tooltip("If enabled, the player must be inside an active Activity Zone unless a source supplies a Zone Context.")]
    [SerializeField] bool requireActivityZone = true;
    [Tooltip("Specific zones that can use this station. Empty means any zone passes when no type/tag filters are set.")]
    [SerializeField] List<ActivityZoneDefinition> allowedZones = new List<ActivityZoneDefinition>();
    [Tooltip("Zone types accepted by this station. Empty means type is not checked.")]
    [SerializeField] List<ActivityZoneType> allowedZoneTypes = new List<ActivityZoneType>();
    [Tooltip("Zone tags accepted by this station. Empty means tags are not checked.")]
    [SerializeField] List<string> allowedZoneTags = new List<string>();
    [Tooltip("Message shown when this station is locked and no more specific failure exists.")]
    [SerializeField] string lockedMessage = "This camp station is not available here.";
    [Tooltip("If enabled, access profile checks are published to access logs/events when a source uses this station.")]
    [SerializeField] bool publishAccessChecks = true;

    [Header("Actions")]
    [Tooltip("Editable station actions exposed by this station.")]
    [SerializeField] List<CampStationAction> actions = new List<CampStationAction>();
    [Tooltip("If enabled, locked actions are included in snapshots unless each action hides itself.")]
    [SerializeField] bool showLockedActionsByDefault = true;
    [Tooltip("If enabled, actions without source overrides use this station id as their source id.")]
    [SerializeField] bool useStationIdAsDefaultSource = true;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public CampStationCategory Category => category;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public Sprite Icon => icon;
    public AccessProfileDefinition AccessProfile => accessProfile;
    public IReadOnlyList<ActivityRequirement> Requirements => requirements != null ? (IReadOnlyList<ActivityRequirement>)requirements : Array.Empty<ActivityRequirement>();
    public bool RequireActivityZone => requireActivityZone;
    public IReadOnlyList<ActivityZoneDefinition> AllowedZones => allowedZones != null ? (IReadOnlyList<ActivityZoneDefinition>)allowedZones : Array.Empty<ActivityZoneDefinition>();
    public IReadOnlyList<ActivityZoneType> AllowedZoneTypes => allowedZoneTypes != null ? (IReadOnlyList<ActivityZoneType>)allowedZoneTypes : Array.Empty<ActivityZoneType>();
    public IReadOnlyList<string> AllowedZoneTags => allowedZoneTags != null ? (IReadOnlyList<string>)allowedZoneTags : Array.Empty<string>();
    public IReadOnlyList<CampStationAction> Actions => actions != null ? (IReadOnlyList<CampStationAction>)actions : Array.Empty<CampStationAction>();
    public bool PublishAccessChecks => publishAccessChecks;
    public bool ShowLockedActionsByDefault => showLockedActionsByDefault;
    public bool UseStationIdAsDefaultSource => useStationIdAsDefaultSource;

    public bool CanUse(PlayerController player, ActivityZoneDefinition zone, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required to use this camp station.";
            return false;
        }

        if(accessProfile != null && !accessProfile.CanAccess(player, out failureMessage)) {
            failureMessage = string.IsNullOrWhiteSpace(failureMessage) ? lockedMessage : failureMessage;
            return false;
        }

        if(!MatchesZone(zone, out failureMessage)) {
            return false;
        }

        foreach(var requirement in Requirements) {
            if(requirement != null && !requirement.IsMet(player)) {
                failureMessage = string.IsNullOrWhiteSpace(requirement.FailureMessage) ? lockedMessage : requirement.FailureMessage;
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    public CampStationSnapshot BuildSnapshot(
        PlayerController player,
        string sourceId,
        string sourceName,
        RegionInfoDefinition region,
        ActivityZoneDefinition zone,
        bool includeLocked,
        UnityEngine.Object context = null) {
        string stationSourceId = ResolveStationSourceId(sourceId);
        string stationSourceName = string.IsNullOrWhiteSpace(sourceName) ? DisplayName : sourceName;
        bool usable = CanUse(player, zone, out var stationFailure);

        var snapshot = new CampStationSnapshot {
            stationId = Id,
            stationName = DisplayName,
            description = Description,
            category = Category,
            sourceId = stationSourceId,
            sourceName = stationSourceName,
            regionId = region != null ? region.Id : string.Empty,
            regionName = region != null ? region.DisplayName : string.Empty,
            zoneId = zone != null ? zone.Id : string.Empty,
            zoneName = zone != null ? zone.DisplayName : string.Empty,
            usable = usable,
            failureMessage = stationFailure,
            rows = new List<CampStationActionRow>()
        };

        foreach(var action in GetOrderedActions()) {
            string rowFailure = stationFailure;
            bool canRun = usable && action.CanRun(player, this, stationSourceId, stationSourceName, region, zone, out rowFailure);
            if(!canRun && string.IsNullOrWhiteSpace(rowFailure)) {
                rowFailure = stationFailure;
            }

            if(!includeLocked && !canRun) {
                continue;
            }

            if(!canRun && action.HideWhenLocked) {
                continue;
            }

            snapshot.rows.Add(action.BuildRow(this, stationSourceId, stationSourceName, region, zone, canRun, rowFailure));
        }

        return snapshot;
    }

    public bool TryRunAction(
        PlayerController player,
        string actionId,
        string sourceId,
        string sourceName,
        RegionInfoDefinition region,
        ActivityZoneDefinition zone,
        UnityEngine.Object context,
        out CampStationRunResult result) {
        string stationSourceId = ResolveStationSourceId(sourceId);
        result = CampStationRunResult.Blocked(this, null, stationSourceId, "No camp station action selected.");

        if(!CanUse(player, zone, out var failureMessage)) {
            result = CampStationRunResult.Blocked(this, null, stationSourceId, failureMessage);
            PublishAccessCheck(player, false, stationSourceId, failureMessage, context);
            return false;
        }

        PublishAccessCheck(player, true, stationSourceId, accessProfile != null ? accessProfile.PassedMessage : null, context);

        var action = FindAction(actionId);
        if(action == null) {
            result = CampStationRunResult.Blocked(this, null, stationSourceId, "Camp station action was not found.");
            return false;
        }

        return action.TryRun(player, this, stationSourceId, ResolveStationSourceName(sourceName), region, zone, context != null ? context : this, out result);
    }

    public bool TryRunFirstAvailable(
        PlayerController player,
        string sourceId,
        string sourceName,
        RegionInfoDefinition region,
        ActivityZoneDefinition zone,
        UnityEngine.Object context,
        out CampStationRunResult result) {
        string stationSourceId = ResolveStationSourceId(sourceId);
        string stationSourceName = ResolveStationSourceName(sourceName);
        result = CampStationRunResult.Blocked(this, null, stationSourceId, "No available camp station action found.");

        if(!CanUse(player, zone, out var failureMessage)) {
            result = CampStationRunResult.Blocked(this, null, stationSourceId, failureMessage);
            PublishAccessCheck(player, false, stationSourceId, failureMessage, context);
            return false;
        }

        PublishAccessCheck(player, true, stationSourceId, accessProfile != null ? accessProfile.PassedMessage : null, context);

        var action = GetOrderedActions().FirstOrDefault(candidate =>
            candidate != null && candidate.CanRun(player, this, stationSourceId, stationSourceName, region, zone, out _));

        if(action == null) {
            return false;
        }

        return action.TryRun(player, this, stationSourceId, stationSourceName, region, zone, context != null ? context : this, out result);
    }

    public CampStationAction FindAction(string actionId) {
        if(string.IsNullOrWhiteSpace(actionId)) {
            return null;
        }

        return GetOrderedActions().FirstOrDefault(action => string.Equals(action.ResolveActionId(), actionId, StringComparison.OrdinalIgnoreCase));
    }

    public IEnumerable<CampStationAction> GetOrderedActions() {
        return Actions
            .Where(action => action != null && action.HasTarget())
            .OrderByDescending(action => action.Priority)
            .ThenBy(action => action.ResolveDisplayName());
    }

    public string ResolveStationSourceId(string sourceId) {
        if(!string.IsNullOrWhiteSpace(sourceId)) {
            return sourceId;
        }

        return useStationIdAsDefaultSource ? $"camp-station:{Id}" : Id;
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    bool MatchesZone(ActivityZoneDefinition zone, out string failureMessage) {
        if(!requireActivityZone) {
            failureMessage = null;
            return true;
        }

        if(zone == null) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? "A valid camp or activity zone is required." : lockedMessage;
            return false;
        }

        bool hasFilters = AllowedZones.Count > 0 || AllowedZoneTypes.Count > 0 || AllowedZoneTags.Count > 0;
        bool matches = !hasFilters
            || AllowedZones.Contains(zone)
            || AllowedZoneTypes.Contains(zone.ZoneType)
            || AllowedZoneTags.Any(zone.HasTag);

        failureMessage = matches ? null : (string.IsNullOrWhiteSpace(lockedMessage) ? $"{DisplayName} cannot be used here." : lockedMessage);
        return matches;
    }

    string ResolveStationSourceName(string sourceName) {
        return string.IsNullOrWhiteSpace(sourceName) ? DisplayName : sourceName;
    }

    void PublishAccessCheck(PlayerController player, bool passed, string sourceId, string message, UnityEngine.Object context) {
        if(accessProfile == null || !publishAccessChecks) {
            return;
        }

        accessProfile.PublishChecked(player, passed, sourceId, message, context != null ? context : this);
    }
}

[Serializable]
public class CampStationAction {
    [Header("Identity")]
    [Tooltip("Optional stable row id used by UI actions. Empty uses the assigned target id.")]
    [SerializeField] string actionId = string.Empty;
    [Tooltip("Optional display name override for this action.")]
    [SerializeField] string displayNameOverride = string.Empty;
    [Tooltip("Optional description override for this action.")]
    [TextArea]
    [SerializeField] string descriptionOverride = string.Empty;
    [Tooltip("Suggested action label for future UI buttons, such as Rest, Cook, Care, Train or Start.")]
    [SerializeField] string actionLabel = "Use";
    [Tooltip("Higher priority actions appear first in snapshots.")]
    [SerializeField] int priority;
    [Tooltip("If enabled, this action is omitted from snapshots when locked.")]
    [SerializeField] bool hideWhenLocked;

    [Header("Type")]
    [Tooltip("Kind of behavior this camp station action runs.")]
    [SerializeField] CampStationActionType actionType = CampStationActionType.Activity;
    [Tooltip("Optional source id override saved in target logs/events. Empty uses the station/source id.")]
    [SerializeField] string sourceIdOverride = string.Empty;

    [Header("Targets")]
    [Tooltip("Activity run by Activity actions or used as cost/reward hook by rest/care actions.")]
    [SerializeField] ActivityDefinition activity = null;
    [Tooltip("Care action applied by Pokemon Care Action rows.")]
    [SerializeField] PokemonCareActionDefinition careAction = null;
    [Tooltip("Social activity run by Social Activity rows.")]
    [SerializeField] SocialActivityDefinition socialActivity = null;
    [Tooltip("Direct Pokemon assignment started by Pokemon Assignment rows.")]
    [SerializeField] PokemonAssignmentDefinition pokemonAssignment = null;
    [Tooltip("Pokemon assignment board used by Pokemon Assignment Board rows. The first available board entry can be started.")]
    [SerializeField] PokemonAssignmentBoardDefinition pokemonAssignmentBoard = null;
    [Tooltip("Specific situation event started by Situation Event rows.")]
    [SerializeField] SituationEventDefinition situationEvent = null;
    [Tooltip("Situation event pool rolled by Situation Event Pool rows.")]
    [SerializeField] SituationEventPoolDefinition situationEventPool = null;
    [Tooltip("Role activity board delegated to by Role Activity Board rows.")]
    [SerializeField] RoleActivityBoardDefinition roleActivityBoard = null;
    [Tooltip("Life Path rewards applied by Life Path Rewards rows or as extra rewards after any successful action.")]
    [SerializeField] List<LifePathReward> lifePathRewards = new List<LifePathReward>();

    [Header("Rest / Sleep")]
    [Tooltip("Hours applied to SurvivalNeedsController.Rest or PokemonCareNeedsController.ApplyRest for Rest actions.")]
    [Min(1)]
    [SerializeField] int restHours = 1;
    [Tooltip("Hours applied to SurvivalNeedsController.Sleep or PokemonCareNeedsController.ApplySleep for Sleep actions.")]
    [Min(1)]
    [SerializeField] int sleepHours = 8;
    [Tooltip("If enabled, Rest/Sleep actions affect player survival needs when the controller exists.")]
    [SerializeField] bool affectPlayerNeeds = true;
    [Tooltip("If enabled, Rest/Sleep actions affect party Pokemon care needs when the controller exists.")]
    [SerializeField] bool affectPokemonCareNeeds = true;

    [Header("Activity Behavior")]
    [Tooltip("If enabled, the assigned Activity or care action activity must pass CanPerform before this action runs.")]
    [SerializeField] bool checkActivityCanPerform = true;
    [Tooltip("If enabled, the assigned Activity or care action activity pays configured costs before this action runs.")]
    [SerializeField] bool payActivityCosts = true;
    [Tooltip("If enabled, the assigned Activity or care action activity applies rewards after the action succeeds.")]
    [SerializeField] bool applyActivityRewards = true;
    [Tooltip("If enabled, relationship rewards stored on the activity are also applied after success.")]
    [SerializeField] bool applyActivityRelationshipRewards;

    [Header("Pokemon Behavior")]
    [Tooltip("If enabled, Pokemon care actions affect every eligible party Pokemon. If disabled, only the first eligible party Pokemon is affected.")]
    [SerializeField] bool applyCareToWholeParty = true;
    [Tooltip("Party index used when Start With Specific Party Index is enabled.")]
    [Min(0)]
    [SerializeField] int partyIndex;
    [Tooltip("If enabled, Pokemon assignment rows use Party Index instead of the first eligible Pokemon.")]
    [SerializeField] bool startWithSpecificPartyIndex;
    [Tooltip("If enabled, ready assignments from this source are claimed before starting a new one.")]
    [SerializeField] bool claimReadyPokemonAssignmentsFirst;

    [Header("Extra Effects")]
    [Tooltip("Survival need changes applied after this action succeeds.")]
    [SerializeField] List<CampStationSurvivalNeedChange> survivalNeedChanges = new List<CampStationSurvivalNeedChange>();
    [Tooltip("Pokemon care need changes applied after this action succeeds.")]
    [SerializeField] List<PokemonCareNeedChange> pokemonCareNeedChanges = new List<PokemonCareNeedChange>();
    [Tooltip("If enabled, extra Pokemon care need changes affect every party Pokemon. If disabled, only the first healthy Pokemon is affected.")]
    [SerializeField] bool applyExtraCareNeedsToWholeParty = true;

    [Header("Delegation")]
    [Tooltip("Optional role board entry id used by Role Activity Board actions. Empty runs the first available row.")]
    [SerializeField] string roleBoardEntryId = string.Empty;

    [Header("Context")]
    [Tooltip("Optional region override passed into situation event, pool and role board filters.")]
    [SerializeField] RegionInfoDefinition regionOverride = null;
    [Tooltip("Optional activity zone override passed into assignment, situation event, pool and role board filters.")]
    [SerializeField] ActivityZoneDefinition zoneOverride = null;

    [Header("Access")]
    [Tooltip("Optional reusable access profile checked before this action can run.")]
    [SerializeField] AccessProfileDefinition accessProfile = null;
    [Tooltip("Additional requirements checked before this action can run.")]
    [SerializeField] List<ActivityRequirement> extraRequirements = new List<ActivityRequirement>();
    [Tooltip("Message shown when this action is locked and no more specific failure exists.")]
    [SerializeField] string lockedMessage = "This camp action is not available yet.";

    public string ActionId => actionId;
    public string DisplayNameOverride => displayNameOverride;
    public string DescriptionOverride => descriptionOverride;
    public string ActionLabel => actionLabel;
    public int Priority => priority;
    public bool HideWhenLocked => hideWhenLocked;
    public CampStationActionType ActionType => actionType;
    public string SourceIdOverride => sourceIdOverride;
    public ActivityDefinition Activity => activity;
    public PokemonCareActionDefinition CareAction => careAction;
    public SocialActivityDefinition SocialActivity => socialActivity;
    public PokemonAssignmentDefinition PokemonAssignment => pokemonAssignment;
    public PokemonAssignmentBoardDefinition PokemonAssignmentBoard => pokemonAssignmentBoard;
    public SituationEventDefinition SituationEvent => situationEvent;
    public SituationEventPoolDefinition SituationEventPool => situationEventPool;
    public RoleActivityBoardDefinition RoleActivityBoard => roleActivityBoard;
    public IReadOnlyList<LifePathReward> LifePathRewards => lifePathRewards != null ? (IReadOnlyList<LifePathReward>)lifePathRewards : Array.Empty<LifePathReward>();
    public int RestHours => Mathf.Max(1, restHours);
    public int SleepHours => Mathf.Max(1, sleepHours);
    public bool AffectPlayerNeeds => affectPlayerNeeds;
    public bool AffectPokemonCareNeeds => affectPokemonCareNeeds;
    public bool CheckActivityCanPerform => checkActivityCanPerform;
    public bool PayActivityCosts => payActivityCosts;
    public bool ApplyActivityRewards => applyActivityRewards;
    public bool ApplyActivityRelationshipRewards => applyActivityRelationshipRewards;
    public bool ApplyCareToWholeParty => applyCareToWholeParty;
    public int PartyIndex => Mathf.Max(0, partyIndex);
    public bool StartWithSpecificPartyIndex => startWithSpecificPartyIndex;
    public bool ClaimReadyPokemonAssignmentsFirst => claimReadyPokemonAssignmentsFirst;
    public IReadOnlyList<CampStationSurvivalNeedChange> SurvivalNeedChanges => survivalNeedChanges != null ? (IReadOnlyList<CampStationSurvivalNeedChange>)survivalNeedChanges : Array.Empty<CampStationSurvivalNeedChange>();
    public IReadOnlyList<PokemonCareNeedChange> PokemonCareNeedChanges => pokemonCareNeedChanges != null ? (IReadOnlyList<PokemonCareNeedChange>)pokemonCareNeedChanges : Array.Empty<PokemonCareNeedChange>();
    public bool ApplyExtraCareNeedsToWholeParty => applyExtraCareNeedsToWholeParty;
    public string RoleBoardEntryId => roleBoardEntryId;
    public RegionInfoDefinition RegionOverride => regionOverride;
    public ActivityZoneDefinition ZoneOverride => zoneOverride;
    public AccessProfileDefinition AccessProfile => accessProfile;
    public IReadOnlyList<ActivityRequirement> ExtraRequirements => extraRequirements != null ? (IReadOnlyList<ActivityRequirement>)extraRequirements : Array.Empty<ActivityRequirement>();

    public bool HasTarget() {
        switch(actionType) {
            case CampStationActionType.Activity:
                return activity != null;
            case CampStationActionType.Rest:
            case CampStationActionType.Sleep:
                return true;
            case CampStationActionType.PokemonCareAction:
                return careAction != null;
            case CampStationActionType.SocialActivity:
                return socialActivity != null;
            case CampStationActionType.PokemonAssignment:
                return pokemonAssignment != null;
            case CampStationActionType.PokemonAssignmentBoard:
                return pokemonAssignmentBoard != null;
            case CampStationActionType.SituationEvent:
                return situationEvent != null;
            case CampStationActionType.SituationEventPool:
                return situationEventPool != null;
            case CampStationActionType.RoleActivityBoard:
                return roleActivityBoard != null;
            case CampStationActionType.LifePathRewards:
                return LifePathRewards.Any(reward => reward != null);
            default:
                return false;
        }
    }

    public string ResolveActionId() {
        if(!string.IsNullOrWhiteSpace(actionId)) {
            return actionId;
        }

        switch(actionType) {
            case CampStationActionType.Activity:
                return activity != null ? activity.Id : string.Empty;
            case CampStationActionType.Rest:
                return "rest";
            case CampStationActionType.Sleep:
                return "sleep";
            case CampStationActionType.PokemonCareAction:
                return careAction != null ? careAction.Id : string.Empty;
            case CampStationActionType.SocialActivity:
                return socialActivity != null ? socialActivity.Id : string.Empty;
            case CampStationActionType.PokemonAssignment:
                return pokemonAssignment != null ? pokemonAssignment.Id : string.Empty;
            case CampStationActionType.PokemonAssignmentBoard:
                return pokemonAssignmentBoard != null ? pokemonAssignmentBoard.Id : string.Empty;
            case CampStationActionType.SituationEvent:
                return situationEvent != null ? situationEvent.Id : string.Empty;
            case CampStationActionType.SituationEventPool:
                return situationEventPool != null ? situationEventPool.Id : string.Empty;
            case CampStationActionType.RoleActivityBoard:
                return roleActivityBoard != null ? roleActivityBoard.Id : string.Empty;
            default:
                return "life-path-rewards";
        }
    }

    public string ResolveDisplayName() {
        if(!string.IsNullOrWhiteSpace(displayNameOverride)) {
            return displayNameOverride;
        }

        switch(actionType) {
            case CampStationActionType.Activity:
                return activity != null ? activity.DisplayName : string.Empty;
            case CampStationActionType.Rest:
                return "Rest";
            case CampStationActionType.Sleep:
                return "Sleep";
            case CampStationActionType.PokemonCareAction:
                return careAction != null ? careAction.DisplayName : string.Empty;
            case CampStationActionType.SocialActivity:
                return socialActivity != null ? socialActivity.DisplayName : string.Empty;
            case CampStationActionType.PokemonAssignment:
                return pokemonAssignment != null ? pokemonAssignment.DisplayName : string.Empty;
            case CampStationActionType.PokemonAssignmentBoard:
                return pokemonAssignmentBoard != null ? pokemonAssignmentBoard.DisplayName : string.Empty;
            case CampStationActionType.SituationEvent:
                return situationEvent != null ? situationEvent.DisplayName : string.Empty;
            case CampStationActionType.SituationEventPool:
                return situationEventPool != null ? situationEventPool.DisplayName : string.Empty;
            case CampStationActionType.RoleActivityBoard:
                return roleActivityBoard != null ? roleActivityBoard.DisplayName : string.Empty;
            default:
                return "Life Path Rewards";
        }
    }

    public string ResolveDescription() {
        if(!string.IsNullOrWhiteSpace(descriptionOverride)) {
            return descriptionOverride;
        }

        switch(actionType) {
            case CampStationActionType.Activity:
                return activity != null ? activity.Description : string.Empty;
            case CampStationActionType.Rest:
                return $"Rest for {RestHours} hour(s).";
            case CampStationActionType.Sleep:
                return $"Sleep for {SleepHours} hour(s).";
            case CampStationActionType.PokemonCareAction:
                return careAction != null ? careAction.Description : string.Empty;
            case CampStationActionType.SocialActivity:
                return socialActivity != null ? socialActivity.Description : string.Empty;
            case CampStationActionType.PokemonAssignment:
                return pokemonAssignment != null ? pokemonAssignment.Description : string.Empty;
            case CampStationActionType.PokemonAssignmentBoard:
                return pokemonAssignmentBoard != null ? pokemonAssignmentBoard.Description : string.Empty;
            case CampStationActionType.SituationEvent:
                return situationEvent != null ? situationEvent.Description : string.Empty;
            case CampStationActionType.SituationEventPool:
                return situationEventPool != null ? situationEventPool.Description : string.Empty;
            case CampStationActionType.RoleActivityBoard:
                return roleActivityBoard != null ? roleActivityBoard.Description : string.Empty;
            default:
                return "Applies configured Life Path rewards.";
        }
    }

    public string ResolveSourceId(CampStationDefinition station, string fallbackSourceId) {
        if(!string.IsNullOrWhiteSpace(sourceIdOverride)) {
            return sourceIdOverride;
        }

        if(station != null && station.UseStationIdAsDefaultSource) {
            return $"camp-station:{station.Id}:{ResolveActionId()}";
        }

        return !string.IsNullOrWhiteSpace(fallbackSourceId) ? fallbackSourceId : ResolveActionId();
    }

    public RegionInfoDefinition ResolveRegion(RegionInfoDefinition fallbackRegion) {
        return regionOverride != null ? regionOverride : fallbackRegion;
    }

    public ActivityZoneDefinition ResolveZone(ActivityZoneDefinition fallbackZone) {
        return zoneOverride != null ? zoneOverride : fallbackZone;
    }

    public CampStationActionRow BuildRow(
        CampStationDefinition station,
        string stationSourceId,
        string stationSourceName,
        RegionInfoDefinition region,
        ActivityZoneDefinition zone,
        bool canRun,
        string failureMessage) {
        var resolvedRegion = ResolveRegion(region);
        var resolvedZone = ResolveZone(zone);
        return new CampStationActionRow {
            actionId = ResolveActionId(),
            displayName = ResolveDisplayName(),
            description = ResolveDescription(),
            actionLabel = string.IsNullOrWhiteSpace(actionLabel) ? "Use" : actionLabel,
            actionType = actionType,
            priority = priority,
            canRun = canRun,
            failureMessage = failureMessage,
            sourceId = ResolveSourceId(station, stationSourceId),
            sourceName = string.IsNullOrWhiteSpace(stationSourceName) ? ResolveDisplayName() : stationSourceName,
            regionId = resolvedRegion != null ? resolvedRegion.Id : string.Empty,
            regionName = resolvedRegion != null ? resolvedRegion.DisplayName : string.Empty,
            zoneId = resolvedZone != null ? resolvedZone.Id : string.Empty,
            zoneName = resolvedZone != null ? resolvedZone.DisplayName : string.Empty
        };
    }

    public bool CanRun(
        PlayerController player,
        CampStationDefinition station,
        string stationSourceId,
        string stationSourceName,
        RegionInfoDefinition region,
        ActivityZoneDefinition zone,
        out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required.";
            return false;
        }

        if(!HasTarget()) {
            failureMessage = "This camp station action has no target assigned.";
            return false;
        }

        if(accessProfile != null && !accessProfile.CanAccess(player, out failureMessage)) {
            failureMessage = string.IsNullOrWhiteSpace(failureMessage) ? lockedMessage : failureMessage;
            return false;
        }

        foreach(var requirement in ExtraRequirements) {
            if(requirement != null && !requirement.IsMet(player)) {
                failureMessage = string.IsNullOrWhiteSpace(requirement.FailureMessage) ? lockedMessage : requirement.FailureMessage;
                return false;
            }
        }

        var resolvedRegion = ResolveRegion(region);
        var resolvedZone = ResolveZone(zone);
        string sourceId = ResolveSourceId(station, stationSourceId);

        switch(actionType) {
            case CampStationActionType.Activity:
                return CanUseActivity(player, activity, out failureMessage);
            case CampStationActionType.Rest:
                return CanRestOrSleep(player, rest: true, out failureMessage);
            case CampStationActionType.Sleep:
                return CanRestOrSleep(player, rest: false, out failureMessage);
            case CampStationActionType.PokemonCareAction:
                return CanUseCareAction(player, out failureMessage);
            case CampStationActionType.SocialActivity:
                return socialActivity.CanRun(player, out failureMessage);
            case CampStationActionType.PokemonAssignment:
                return FindEligiblePokemon(player, pokemonAssignment, resolvedZone, sourceId, out _, out failureMessage);
            case CampStationActionType.PokemonAssignmentBoard:
                return FindEligibleAssignmentBoardEntry(player, pokemonAssignmentBoard, resolvedZone, sourceId, out _, out _, out failureMessage);
            case CampStationActionType.SituationEvent:
                return situationEvent.CanStart(player, resolvedRegion, resolvedZone, sourceId, out failureMessage);
            case CampStationActionType.SituationEventPool:
                if(!situationEventPool.MatchesLocation(resolvedRegion, resolvedZone)) {
                    failureMessage = "Situation event pool location filters did not match.";
                    return false;
                }
                failureMessage = null;
                return true;
            case CampStationActionType.RoleActivityBoard:
                return CanUseRoleBoard(player, stationSourceName, resolvedRegion, resolvedZone, out failureMessage);
            case CampStationActionType.LifePathRewards:
                if(!LifePathRewards.Any(reward => reward != null)) {
                    failureMessage = "No Life Path rewards assigned.";
                    return false;
                }
                failureMessage = null;
                return true;
            default:
                failureMessage = "Unsupported camp station action type.";
                return false;
        }
    }

    public bool TryRun(
        PlayerController player,
        CampStationDefinition station,
        string stationSourceId,
        string stationSourceName,
        RegionInfoDefinition region,
        ActivityZoneDefinition zone,
        UnityEngine.Object context,
        out CampStationRunResult result) {
        string sourceId = ResolveSourceId(station, stationSourceId);
        string sourceName = string.IsNullOrWhiteSpace(stationSourceName) ? ResolveDisplayName() : stationSourceName;
        result = CampStationRunResult.Blocked(station, this, sourceId, "Camp station action could not run.");

        if(!CanRun(player, station, stationSourceId, stationSourceName, region, zone, out var failureMessage)) {
            result = CampStationRunResult.Blocked(station, this, sourceId, failureMessage);
            PublishActionAccess(player, false, sourceId, failureMessage, context);
            return false;
        }

        PublishActionAccess(player, true, sourceId, accessProfile != null ? accessProfile.PassedMessage : null, context);

        bool success;
        string message;
        switch(actionType) {
            case CampStationActionType.Activity:
                success = RunActivity(player, activity, out message);
                break;
            case CampStationActionType.Rest:
                success = RunRest(player, sourceId, out message);
                break;
            case CampStationActionType.Sleep:
                success = RunSleep(player, sourceId, out message);
                break;
            case CampStationActionType.PokemonCareAction:
                success = RunCareAction(player, sourceId, out message);
                break;
            case CampStationActionType.SocialActivity:
                success = socialActivity.TryRun(player, sourceId, context, out var socialResult);
                message = socialResult != null ? socialResult.message : success ? $"{socialActivity.DisplayName} completed." : "Social activity failed.";
                break;
            case CampStationActionType.PokemonAssignment:
                success = RunPokemonAssignment(player, pokemonAssignment, ResolveZone(zone), sourceId, sourceName, out message);
                break;
            case CampStationActionType.PokemonAssignmentBoard:
                success = RunPokemonAssignmentBoard(player, pokemonAssignmentBoard, ResolveZone(zone), sourceId, sourceName, out message);
                break;
            case CampStationActionType.SituationEvent:
                var eventResult = situationEvent.TryStart(player, ResolveRegion(region), ResolveZone(zone), sourceId, sourceName, context);
                success = eventResult != null && !eventResult.blocked;
                message = success ? $"{situationEvent.DisplayName} started." : eventResult != null ? eventResult.failureMessage : "Situation event failed.";
                break;
            case CampStationActionType.SituationEventPool:
                var poolResult = situationEventPool.Roll(player, ResolveRegion(region), ResolveZone(zone), sourceId, sourceName, context);
                success = poolResult != null && poolResult.startedEvents > 0 && !poolResult.blocked;
                message = success
                    ? $"{situationEventPool.DisplayName} started {poolResult.startedEvents} event(s)."
                    : poolResult != null ? poolResult.failureMessage : "Situation event pool failed.";
                break;
            case CampStationActionType.RoleActivityBoard:
                success = RunRoleBoard(player, sourceId, sourceName, ResolveRegion(region), ResolveZone(zone), context, out message);
                break;
            case CampStationActionType.LifePathRewards:
                ApplyLifePathRewards(player, sourceId, context);
                success = true;
                message = $"{ResolveDisplayName()} rewards applied.";
                break;
            default:
                success = false;
                message = "Unsupported camp station action type.";
                break;
        }

        if(success) {
            ApplyExtraEffects(player, sourceId, context);
        }

        result = success
            ? CampStationRunResult.Succeeded(station, this, sourceId, message)
            : CampStationRunResult.Blocked(station, this, sourceId, message);
        return success;
    }

    bool CanUseActivity(PlayerController player, ActivityDefinition targetActivity, out string failureMessage) {
        if(targetActivity == null) {
            failureMessage = null;
            return true;
        }

        if(checkActivityCanPerform) {
            return targetActivity.CanPerform(player, out failureMessage);
        }

        failureMessage = null;
        return true;
    }

    bool CanRestOrSleep(PlayerController player, bool rest, out string failureMessage) {
        if(!CanUseActivity(player, activity, out failureMessage)) {
            return false;
        }

        if(!affectPlayerNeeds && !affectPokemonCareNeeds) {
            failureMessage = rest ? "Rest has no configured effect." : "Sleep has no configured effect.";
            return false;
        }

        if(affectPlayerNeeds && player.GetComponent<SurvivalNeedsController>() != null) {
            failureMessage = null;
            return true;
        }

        if(affectPokemonCareNeeds && player.GetComponent<PokemonCareNeedsController>() != null) {
            failureMessage = null;
            return true;
        }

        failureMessage = rest ? "No rest controller is available." : "No sleep controller is available.";
        return false;
    }

    bool CanUseCareAction(PlayerController player, out string failureMessage) {
        failureMessage = null;
        if(careAction == null) {
            failureMessage = "No care action assigned.";
            return false;
        }

        if(!CanUseActivity(player, careAction.Activity, out failureMessage)) {
            return false;
        }

        var party = player != null ? player.GetComponent<PokemonParty>() : null;
        if(party?.Pokemons == null) {
            failureMessage = "The player has no Pokemon party.";
            return false;
        }

        var targets = ResolveCareTargets(party);
        if(targets.Count == 0 || !targets.Any(pokemon => careAction.CanApply(pokemon, out _))) {
            var first = targets.FirstOrDefault();
            if(first != null && !careAction.CanApply(first, out var careFailure)) {
                failureMessage = careFailure;
            }
            if(string.IsNullOrWhiteSpace(failureMessage)) {
                failureMessage = "No Pokemon can receive this care right now.";
            }
            return false;
        }

        return true;
    }

    bool CanUseRoleBoard(PlayerController player, string sourceName, RegionInfoDefinition region, ActivityZoneDefinition zone, out string failureMessage) {
        if(roleActivityBoard == null) {
            failureMessage = "No role activity board assigned.";
            return false;
        }

        if(!roleActivityBoard.CanUse(player, out failureMessage)) {
            return false;
        }

        if(!string.IsNullOrWhiteSpace(roleBoardEntryId)) {
            var entry = roleActivityBoard.FindEntry(roleBoardEntryId);
            if(entry == null) {
                failureMessage = "Role activity board entry was not found.";
                return false;
            }

            return entry.CanRun(player, roleActivityBoard, roleActivityBoard.ResolveBoardSourceId(null), sourceName, region, zone, out failureMessage);
        }

        var snapshot = roleActivityBoard.BuildSnapshot(player, roleActivityBoard.ResolveBoardSourceId(null), sourceName, region, zone, true);
        if(snapshot.rows.Any(row => row.canRun)) {
            failureMessage = null;
            return true;
        }

        failureMessage = "No available role board entry found.";
        return false;
    }

    bool RunActivity(PlayerController player, ActivityDefinition targetActivity, out string message) {
        if(targetActivity == null) {
            message = "No activity assigned.";
            return false;
        }

        if(payActivityCosts && !targetActivity.TryPayCosts(player, out message)) {
            return false;
        }

        if(applyActivityRewards) {
            targetActivity.ApplyRewards(player);
        }

        if(applyActivityRelationshipRewards) {
            targetActivity.ApplyRelationshipRewards(player);
        }

        message = $"{targetActivity.DisplayName} completed.";
        return true;
    }

    bool RunRest(PlayerController player, string sourceId, out string message) {
        bool applied = false;
        int hours = RestHours;
        if(activity != null && payActivityCosts && !activity.TryPayCosts(player, out message)) {
            return false;
        }

        if(affectPlayerNeeds) {
            var survival = player.GetComponent<SurvivalNeedsController>();
            if(survival != null) {
                survival.Rest(hours);
                applied = true;
            }
        }

        if(affectPokemonCareNeeds && !affectPlayerNeeds) {
            var careNeeds = player.GetComponent<PokemonCareNeedsController>();
            applied |= careNeeds != null && careNeeds.ApplyRest(hours, sourceId) > 0;
        }

        if(!applied) {
            message = "No rest effect could be applied.";
            return false;
        }

        ApplyLinkedActivityRewards(player);
        message = $"Rested for {hours} hour(s).";
        return true;
    }

    bool RunSleep(PlayerController player, string sourceId, out string message) {
        bool applied = false;
        int hours = SleepHours;
        if(activity != null && payActivityCosts && !activity.TryPayCosts(player, out message)) {
            return false;
        }

        if(affectPlayerNeeds) {
            var survival = player.GetComponent<SurvivalNeedsController>();
            if(survival != null) {
                survival.Sleep(hours);
                applied = true;
            }
        }

        if(affectPokemonCareNeeds && !affectPlayerNeeds) {
            var careNeeds = player.GetComponent<PokemonCareNeedsController>();
            applied |= careNeeds != null && careNeeds.ApplySleep(hours, sourceId) > 0;
        }

        if(!applied) {
            message = "No sleep effect could be applied.";
            return false;
        }

        ApplyLinkedActivityRewards(player);
        message = $"Slept for {hours} hour(s).";
        return true;
    }

    bool RunCareAction(PlayerController player, string sourceId, out string message) {
        var activityHook = careAction.Activity;
        if(activityHook != null && payActivityCosts && !activityHook.TryPayCosts(player, out message)) {
            return false;
        }

        var party = player.GetComponent<PokemonParty>();
        var targets = ResolveCareTargets(party).Where(pokemon => pokemon != null && careAction.CanApply(pokemon, out _)).ToList();
        if(targets.Count == 0) {
            message = "No Pokemon can receive this care right now.";
            return false;
        }

        int bonus = GetCareBonus(player, activityHook);
        int affected = 0;
        foreach(var pokemon in targets) {
            if(careAction.TryApply(pokemon, bonus, sourceId, out _)) {
                affected++;
            }
        }

        if(affected <= 0) {
            message = "Pokemon care did not affect any Pokemon.";
            return false;
        }

        ApplyLinkedActivityRewards(player, activityHook);
        message = affected == 1
            ? $"{careAction.DisplayName} completed for 1 Pokemon."
            : $"{careAction.DisplayName} completed for {affected} Pokemon.";
        return true;
    }

    void ApplyLinkedActivityRewards(PlayerController player) {
        ApplyLinkedActivityRewards(player, activity);
    }

    void ApplyLinkedActivityRewards(PlayerController player, ActivityDefinition targetActivity) {
        if(targetActivity == null) {
            return;
        }

        if(applyActivityRewards) {
            targetActivity.ApplyRewards(player);
        }

        if(applyActivityRelationshipRewards) {
            targetActivity.ApplyRelationshipRewards(player);
        }
    }

    bool RunPokemonAssignment(PlayerController player, PokemonAssignmentDefinition assignment, ActivityZoneDefinition zone, string sourceId, string sourceName, out string message) {
        var log = player.GetComponent<PlayerPokemonAssignmentLog>() ?? player.gameObject.AddComponent<PlayerPokemonAssignmentLog>();
        if(claimReadyPokemonAssignmentsFirst) {
            var readyState = log.GetReadyAssignments(assignment, sourceId).FirstOrDefault();
            if(readyState != null) {
                bool claimed = log.TryClaim(player, assignment, readyState, out message);
                if(claimed) {
                    message = $"{readyState.assignmentName} claimed.";
                }
                return claimed;
            }
        }

        Pokemon pokemon = startWithSpecificPartyIndex
            ? ResolvePokemonByIndex(player, partyIndex)
            : ResolveFirstEligiblePokemon(player, assignment, log, zone, sourceId);
        bool started = log.TryStart(player, assignment, pokemon, zone, sourceId, sourceName, out message);
        if(started) {
            message = $"{pokemon?.NickName ?? "Pokemon"} started {assignment.DisplayName}.";
        }
        return started;
    }

    bool RunPokemonAssignmentBoard(PlayerController player, PokemonAssignmentBoardDefinition board, ActivityZoneDefinition zone, string sourceId, string sourceName, out string message) {
        if(!FindEligibleAssignmentBoardEntry(player, board, zone, sourceId, out var entry, out var pokemon, out message)) {
            return false;
        }

        var assignment = entry.Assignment;
        var resolvedZone = entry.ResolveZone(zone);
        var resolvedSourceId = entry.ResolveSourceId(board, sourceId);
        var log = player.GetComponent<PlayerPokemonAssignmentLog>() ?? player.gameObject.AddComponent<PlayerPokemonAssignmentLog>();

        if(claimReadyPokemonAssignmentsFirst) {
            var readyState = log.GetReadyAssignments(assignment, resolvedSourceId).FirstOrDefault();
            if(readyState != null) {
                bool claimed = log.TryClaim(player, assignment, readyState, out message);
                if(claimed) {
                    message = $"{readyState.assignmentName} claimed.";
                }
                return claimed;
            }
        }

        bool started = log.TryStart(player, assignment, pokemon, resolvedZone, resolvedSourceId, sourceName, out message);
        if(started) {
            message = $"{pokemon?.NickName ?? "Pokemon"} started {assignment.DisplayName}.";
        }
        return started;
    }

    bool RunRoleBoard(PlayerController player, string sourceId, string sourceName, RegionInfoDefinition region, ActivityZoneDefinition zone, UnityEngine.Object context, out string message) {
        RoleActivityBoardRunResult result;
        bool success;
        if(!string.IsNullOrWhiteSpace(roleBoardEntryId)) {
            success = roleActivityBoard.TryRunEntry(player, roleBoardEntryId, sourceId, sourceName, region, zone, context, out result);
        } else {
            success = roleActivityBoard.TryRunFirstAvailable(player, sourceId, sourceName, region, zone, context, out result);
        }

        message = result != null ? result.message : success ? $"{roleActivityBoard.DisplayName} completed." : "Role activity board failed.";
        return success;
    }

    void ApplyExtraEffects(PlayerController player, string sourceId, UnityEngine.Object context) {
        foreach(var change in SurvivalNeedChanges) {
            if(change != null) {
                change.Apply(player, sourceId);
            }
        }

        ApplyExtraPokemonCareNeedChanges(player, sourceId);
        ApplyLifePathRewards(player, sourceId, context);
    }

    void ApplyExtraPokemonCareNeedChanges(PlayerController player, string sourceId) {
        if(PokemonCareNeedChanges.Count == 0 || player == null) {
            return;
        }

        var party = player.GetComponent<PokemonParty>();
        var careNeeds = player.GetComponent<PokemonCareNeedsController>();
        if(party?.Pokemons == null || careNeeds == null) {
            return;
        }

        var targets = applyExtraCareNeedsToWholeParty
            ? party.Pokemons.Where(pokemon => pokemon != null)
            : new[] { party.GetHealthyPokemon() }.Where(pokemon => pokemon != null);

        foreach(var pokemon in targets) {
            foreach(var change in PokemonCareNeedChanges) {
                if(change != null && change.need != null && change.amount != 0) {
                    careNeeds.TryChangeNeed(pokemon, change.need, change.amount, sourceId, PokemonCareNeedHourlyContext.Resting, out _);
                }
            }
        }
    }

    void ApplyLifePathRewards(PlayerController player, string sourceId, UnityEngine.Object context) {
        if(player == null || LifePathRewards.Count == 0) {
            return;
        }

        var log = player.GetComponent<PlayerLifePathLog>() ?? player.gameObject.AddComponent<PlayerLifePathLog>();
        log.ApplyRewards(LifePathRewards, sourceId, ResolveDisplayName(), context);
    }

    bool FindEligibleAssignmentBoardEntry(
        PlayerController player,
        PokemonAssignmentBoardDefinition board,
        ActivityZoneDefinition zone,
        string sourceId,
        out PokemonAssignmentBoardEntry entry,
        out Pokemon pokemon,
        out string failureMessage) {
        entry = null;
        pokemon = null;
        failureMessage = "No eligible Pokemon assignment board entry found.";
        if(board == null) {
            failureMessage = "No Pokemon assignment board assigned.";
            return false;
        }

        var log = player.GetComponent<PlayerPokemonAssignmentLog>() ?? player.gameObject.AddComponent<PlayerPokemonAssignmentLog>();
        foreach(var candidate in board.GetOrderedEntries()) {
            if(candidate == null || candidate.Assignment == null) {
                continue;
            }

            if(!candidate.RequirementsMet(player, out failureMessage)) {
                continue;
            }

            var resolvedZone = candidate.ResolveZone(zone);
            var resolvedSourceId = candidate.ResolveSourceId(board, sourceId);
            var candidatePokemon = ResolveFirstEligiblePokemon(player, candidate.Assignment, log, resolvedZone, resolvedSourceId);
            if(candidatePokemon == null) {
                continue;
            }

            entry = candidate;
            pokemon = candidatePokemon;
            failureMessage = null;
            return true;
        }

        return false;
    }

    bool FindEligiblePokemon(
        PlayerController player,
        PokemonAssignmentDefinition assignment,
        ActivityZoneDefinition zone,
        string sourceId,
        out Pokemon pokemon,
        out string failureMessage) {
        pokemon = null;
        failureMessage = "No eligible Pokemon found.";
        if(assignment == null) {
            failureMessage = "No Pokemon assignment assigned.";
            return false;
        }

        var log = player.GetComponent<PlayerPokemonAssignmentLog>() ?? player.gameObject.AddComponent<PlayerPokemonAssignmentLog>();
        pokemon = startWithSpecificPartyIndex
            ? ResolvePokemonByIndex(player, partyIndex)
            : ResolveFirstEligiblePokemon(player, assignment, log, zone, sourceId);

        return assignment.CanStart(player, pokemon, log, zone, sourceId, out failureMessage);
    }

    Pokemon ResolveFirstEligiblePokemon(PlayerController player, PokemonAssignmentDefinition assignment, PlayerPokemonAssignmentLog log, ActivityZoneDefinition zone, string sourceId) {
        var party = player != null ? player.GetComponent<PokemonParty>() : null;
        if(party?.Pokemons == null || assignment == null) {
            return null;
        }

        return party.Pokemons.FirstOrDefault(pokemon => pokemon != null && assignment.CanStart(player, pokemon, log, zone, sourceId, out _));
    }

    Pokemon ResolvePokemonByIndex(PlayerController player, int index) {
        var party = player != null ? player.GetComponent<PokemonParty>() : null;
        if(party?.Pokemons == null || index < 0 || index >= party.Pokemons.Count) {
            return null;
        }

        return party.Pokemons[index];
    }

    List<Pokemon> ResolveCareTargets(PokemonParty party) {
        if(party?.Pokemons == null) {
            return new List<Pokemon>();
        }

        if(applyCareToWholeParty) {
            return party.Pokemons.Where(pokemon => pokemon != null).ToList();
        }

        var target = party.Pokemons.FirstOrDefault(pokemon => pokemon != null && careAction != null && careAction.CanApply(pokemon, out _))
            ?? party.GetHealthyPokemon();
        return target != null ? new List<Pokemon> { target } : new List<Pokemon>();
    }

    int GetCareBonus(PlayerController player, ActivityDefinition activityHook) {
        int areaBonus = PlayerActivityContext.GetPokemonCareBonus(activityHook);
        var skill = activityHook != null ? activityHook.BonusSkill : null;
        if(player == null || skill == null) {
            return areaBonus;
        }

        return areaBonus + (player.GetComponent<PlayerProgression>()?.GetSkillLevel(skill) ?? 0);
    }

    void PublishActionAccess(PlayerController player, bool passed, string sourceId, string message, UnityEngine.Object context) {
        if(accessProfile == null) {
            return;
        }

        accessProfile.PublishChecked(player, passed, sourceId, message, context);
    }
}

[Serializable]
public class CampStationSurvivalNeedChange {
    [Tooltip("Survival need changed by this entry.")]
    public SurvivalNeedDefinition need;
    [Tooltip("Amount to add. Negative values reduce the need.")]
    public int amount;

    public bool Apply(PlayerController player, string sourceId) {
        if(player == null || need == null || amount == 0) {
            return false;
        }

        var controller = player.GetComponent<SurvivalNeedsController>();
        return controller != null && controller.TryChangeNeed(need, amount, sourceId, out _);
    }
}

[Serializable]
public class CampStationSnapshot {
    [Tooltip("Definition id of this station.")]
    public string stationId;
    [Tooltip("Display name of this station.")]
    public string stationName;
    [Tooltip("Description of this station.")]
    public string description;
    [Tooltip("Broad category used by future UI filters.")]
    public CampStationCategory category;
    [Tooltip("Resolved source id used by station actions.")]
    public string sourceId;
    [Tooltip("Resolved source name used by station actions.")]
    public string sourceName;
    [Tooltip("Region id used by rows in this snapshot.")]
    public string regionId;
    [Tooltip("Region name used by rows in this snapshot.")]
    public string regionName;
    [Tooltip("Activity zone id used by rows in this snapshot.")]
    public string zoneId;
    [Tooltip("Activity zone name used by rows in this snapshot.")]
    public string zoneName;
    [Tooltip("If enabled, the station itself passed access and zone checks.")]
    public bool usable;
    [Tooltip("Failure reason if the station itself is locked.")]
    public string failureMessage;
    [Tooltip("Rows currently visible on this station.")]
    public List<CampStationActionRow> rows = new List<CampStationActionRow>();
}

[Serializable]
public class CampStationActionRow {
    [Tooltip("Stable action id used by future UI actions.")]
    public string actionId;
    [Tooltip("Display name shown for this action.")]
    public string displayName;
    [Tooltip("Description shown for this action.")]
    public string description;
    [Tooltip("Suggested button label for this action.")]
    public string actionLabel;
    [Tooltip("Kind of behavior this action runs.")]
    public CampStationActionType actionType;
    [Tooltip("Sort priority copied from the definition.")]
    public int priority;
    [Tooltip("If enabled, this action can run right now.")]
    public bool canRun;
    [Tooltip("Failure reason shown when the action is locked.")]
    public string failureMessage;
    [Tooltip("Resolved source id used when the action runs.")]
    public string sourceId;
    [Tooltip("Resolved source name used when the action runs.")]
    public string sourceName;
    [Tooltip("Region id used by this action.")]
    public string regionId;
    [Tooltip("Region name used by this action.")]
    public string regionName;
    [Tooltip("Activity zone id used by this action.")]
    public string zoneId;
    [Tooltip("Activity zone name used by this action.")]
    public string zoneName;
}

public class CampStationRunResult {
    public readonly bool success;
    public readonly string stationId;
    public readonly string stationName;
    public readonly string actionId;
    public readonly string actionName;
    public readonly CampStationActionType actionType;
    public readonly string sourceId;
    public readonly string message;

    CampStationRunResult(bool success, CampStationDefinition station, CampStationAction action, string sourceId, string message) {
        this.success = success;
        stationId = station != null ? station.Id : string.Empty;
        stationName = station != null ? station.DisplayName : string.Empty;
        actionId = action != null ? action.ResolveActionId() : string.Empty;
        actionName = action != null ? action.ResolveDisplayName() : string.Empty;
        actionType = action != null ? action.ActionType : CampStationActionType.Activity;
        this.sourceId = sourceId;
        this.message = message;
    }

    public static CampStationRunResult Succeeded(CampStationDefinition station, CampStationAction action, string sourceId, string message) {
        return new CampStationRunResult(true, station, action, sourceId, message);
    }

    public static CampStationRunResult Blocked(CampStationDefinition station, CampStationAction action, string sourceId, string message) {
        return new CampStationRunResult(false, station, action, sourceId, message);
    }
}
