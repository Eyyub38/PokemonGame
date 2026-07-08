using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum RoleActivityBoardCategory {
    General,
    Camp,
    Research,
    Ranger,
    Police,
    Farm,
    Ranch,
    Contest,
    Transit,
    Shop,
    Social,
    Custom
}

public enum RoleActivityBoardEntryType {
    Activity,
    Job,
    PokemonAssignment,
    PokemonAssignmentBoard,
    SocialActivity,
    SituationEvent,
    SituationEventPool,
    LifePathRewards
}

[CreateAssetMenu(menuName = "Activities/Role Activity Board")]
public class RoleActivityBoardDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this role board. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in future UI, prompts and debug messages. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note or player-facing explanation of this board.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Broad category used by future UI filters and content organization.")]
    [SerializeField] RoleActivityBoardCategory category = RoleActivityBoardCategory.General;
    [Tooltip("Free-form tags such as police, professor, ranger, camp, farm, festival or town.")]
    [SerializeField] List<string> tags = new List<string>();
    [Tooltip("Optional icon used by future board UI.")]
    [SerializeField] Sprite icon = null;

    [Header("Access")]
    [Tooltip("Optional reusable access profile checked before this board can be used.")]
    [SerializeField] AccessProfileDefinition accessProfile = null;
    [Tooltip("Additional requirements checked before this board can be used.")]
    [SerializeField] List<ActivityRequirement> requirements = new List<ActivityRequirement>();
    [Tooltip("Message shown when this board is locked and no more specific failure exists.")]
    [SerializeField] string lockedMessage = "This board is not available yet.";
    [Tooltip("If enabled, access profile checks are published to access logs/events when a source uses this board.")]
    [SerializeField] bool publishAccessChecks = true;

    [Header("Entries")]
    [Tooltip("Editable actions, jobs, assignments, events or reward rows exposed by this board.")]
    [SerializeField] List<RoleActivityBoardEntry> entries = new List<RoleActivityBoardEntry>();
    [Tooltip("If enabled, locked entries are included in snapshots unless each entry hides itself.")]
    [SerializeField] bool showLockedEntriesByDefault = true;
    [Tooltip("If enabled, entries without source overrides use this board id as their source id.")]
    [SerializeField] bool useBoardIdAsDefaultSource = true;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public RoleActivityBoardCategory Category => category;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public Sprite Icon => icon;
    public AccessProfileDefinition AccessProfile => accessProfile;
    public IReadOnlyList<ActivityRequirement> Requirements => requirements != null ? (IReadOnlyList<ActivityRequirement>)requirements : Array.Empty<ActivityRequirement>();
    public IReadOnlyList<RoleActivityBoardEntry> Entries => entries != null ? (IReadOnlyList<RoleActivityBoardEntry>)entries : Array.Empty<RoleActivityBoardEntry>();
    public bool PublishAccessChecks => publishAccessChecks;
    public bool ShowLockedEntriesByDefault => showLockedEntriesByDefault;
    public bool UseBoardIdAsDefaultSource => useBoardIdAsDefaultSource;

    public bool CanUse(PlayerController player, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required to use this board.";
            return false;
        }

        if(accessProfile != null && !accessProfile.CanAccess(player, out failureMessage)) {
            failureMessage = string.IsNullOrWhiteSpace(failureMessage) ? lockedMessage : failureMessage;
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

    public RoleActivityBoardSnapshot BuildSnapshot(
        PlayerController player,
        string sourceId,
        string sourceName,
        RegionInfoDefinition region,
        ActivityZoneDefinition zone,
        bool includeLocked,
        UnityEngine.Object context = null) {
        string boardSourceId = ResolveBoardSourceId(sourceId);
        string boardSourceName = string.IsNullOrWhiteSpace(sourceName) ? DisplayName : sourceName;
        bool usable = CanUse(player, out var boardFailure);

        var snapshot = new RoleActivityBoardSnapshot {
            boardId = Id,
            boardName = DisplayName,
            description = Description,
            category = Category,
            sourceId = boardSourceId,
            sourceName = boardSourceName,
            regionId = region != null ? region.Id : string.Empty,
            regionName = region != null ? region.DisplayName : string.Empty,
            zoneId = zone != null ? zone.Id : string.Empty,
            zoneName = zone != null ? zone.DisplayName : string.Empty,
            usable = usable,
            failureMessage = boardFailure,
            rows = new List<RoleActivityBoardRow>()
        };

        foreach(var entry in GetOrderedEntries()) {
            string rowFailure = boardFailure;
            bool canRun = usable && entry.CanRun(player, this, boardSourceId, boardSourceName, region, zone, out rowFailure);
            if(!canRun && string.IsNullOrWhiteSpace(rowFailure)) {
                rowFailure = boardFailure;
            }

            if(!includeLocked && !canRun) {
                continue;
            }

            if(!canRun && entry.HideWhenLocked) {
                continue;
            }

            snapshot.rows.Add(entry.BuildRow(this, boardSourceId, boardSourceName, region, zone, canRun, rowFailure));
        }

        return snapshot;
    }

    public bool TryRunEntry(
        PlayerController player,
        string entryId,
        string sourceId,
        string sourceName,
        RegionInfoDefinition region,
        ActivityZoneDefinition zone,
        UnityEngine.Object context,
        out RoleActivityBoardRunResult result) {
        result = RoleActivityBoardRunResult.Blocked(this, null, ResolveBoardSourceId(sourceId), "No board entry selected.");
        if(!CanUse(player, out var failureMessage)) {
            result = RoleActivityBoardRunResult.Blocked(this, null, ResolveBoardSourceId(sourceId), failureMessage);
            PublishAccessCheck(player, false, ResolveBoardSourceId(sourceId), failureMessage, context);
            return false;
        }

        PublishAccessCheck(player, true, ResolveBoardSourceId(sourceId), accessProfile != null ? accessProfile.PassedMessage : null, context);

        var entry = FindEntry(entryId);
        if(entry == null) {
            result = RoleActivityBoardRunResult.Blocked(this, null, ResolveBoardSourceId(sourceId), "Board entry was not found.");
            return false;
        }

        return entry.TryRun(player, this, ResolveBoardSourceId(sourceId), ResolveBoardSourceName(sourceName), region, zone, context != null ? context : this, out result);
    }

    public bool TryRunFirstAvailable(
        PlayerController player,
        string sourceId,
        string sourceName,
        RegionInfoDefinition region,
        ActivityZoneDefinition zone,
        UnityEngine.Object context,
        out RoleActivityBoardRunResult result) {
        result = RoleActivityBoardRunResult.Blocked(this, null, ResolveBoardSourceId(sourceId), "No available board entry found.");
        if(!CanUse(player, out var failureMessage)) {
            result = RoleActivityBoardRunResult.Blocked(this, null, ResolveBoardSourceId(sourceId), failureMessage);
            PublishAccessCheck(player, false, ResolveBoardSourceId(sourceId), failureMessage, context);
            return false;
        }

        PublishAccessCheck(player, true, ResolveBoardSourceId(sourceId), accessProfile != null ? accessProfile.PassedMessage : null, context);

        string boardSourceId = ResolveBoardSourceId(sourceId);
        string boardSourceName = ResolveBoardSourceName(sourceName);
        var entry = GetOrderedEntries().FirstOrDefault(candidate =>
            candidate != null && candidate.CanRun(player, this, boardSourceId, boardSourceName, region, zone, out _));

        if(entry == null) {
            return false;
        }

        return entry.TryRun(player, this, boardSourceId, boardSourceName, region, zone, context != null ? context : this, out result);
    }

    public RoleActivityBoardEntry FindEntry(string entryId) {
        if(string.IsNullOrWhiteSpace(entryId)) {
            return null;
        }

        return GetOrderedEntries().FirstOrDefault(entry => string.Equals(entry.ResolveEntryId(), entryId, StringComparison.OrdinalIgnoreCase));
    }

    public IEnumerable<RoleActivityBoardEntry> GetOrderedEntries() {
        return Entries
            .Where(entry => entry != null && entry.HasTarget())
            .OrderByDescending(entry => entry.Priority)
            .ThenBy(entry => entry.ResolveDisplayName());
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    public string ResolveBoardSourceId(string sourceId) {
        if(!string.IsNullOrWhiteSpace(sourceId)) {
            return sourceId;
        }

        return useBoardIdAsDefaultSource ? $"role-board:{Id}" : Id;
    }

    string ResolveBoardSourceName(string sourceName) {
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
public class RoleActivityBoardEntry {
    [Header("Identity")]
    [Tooltip("Optional stable row id used by UI actions. Empty uses the assigned target id.")]
    [SerializeField] string entryId = string.Empty;
    [Tooltip("Optional display name override for this row.")]
    [SerializeField] string displayNameOverride = string.Empty;
    [Tooltip("Optional description override for this row.")]
    [TextArea]
    [SerializeField] string descriptionOverride = string.Empty;
    [Tooltip("Optional action label for future UI buttons, such as Accept, Start, Roll, Help or Claim.")]
    [SerializeField] string actionLabel = "Select";
    [Tooltip("Higher priority rows appear first in snapshots.")]
    [SerializeField] int priority;
    [Tooltip("If enabled, this entry is omitted from snapshots when locked.")]
    [SerializeField] bool hideWhenLocked;

    [Header("Type")]
    [Tooltip("Kind of content this row exposes.")]
    [SerializeField] RoleActivityBoardEntryType entryType = RoleActivityBoardEntryType.Activity;
    [Tooltip("Optional source id override saved in target logs/events. Empty uses the board/source id.")]
    [SerializeField] string sourceIdOverride = string.Empty;

    [Header("Targets")]
    [Tooltip("Activity run by Activity rows.")]
    [SerializeField] ActivityDefinition activity = null;
    [Tooltip("Job accepted by Job rows.")]
    [SerializeField] JobDefinition job = null;
    [Tooltip("Direct Pokemon assignment started by Pokemon Assignment rows.")]
    [SerializeField] PokemonAssignmentDefinition pokemonAssignment = null;
    [Tooltip("Assignment board used by Pokemon Assignment Board rows. The first available entry can be started from this board row.")]
    [SerializeField] PokemonAssignmentBoardDefinition pokemonAssignmentBoard = null;
    [Tooltip("Social activity run by Social Activity rows.")]
    [SerializeField] SocialActivityDefinition socialActivity = null;
    [Tooltip("Specific situation event started by Situation Event rows.")]
    [SerializeField] SituationEventDefinition situationEvent = null;
    [Tooltip("Situation event pool rolled by Situation Event Pool rows.")]
    [SerializeField] SituationEventPoolDefinition situationEventPool = null;
    [Tooltip("Life Path rewards applied by Life Path Rewards rows.")]
    [SerializeField] List<LifePathReward> lifePathRewards = new List<LifePathReward>();

    [Header("Activity Behavior")]
    [Tooltip("If enabled, Activity rows pay their configured costs before rewards are applied.")]
    [SerializeField] bool payActivityCosts = true;
    [Tooltip("If enabled, Activity rows apply rewards and record activity completion.")]
    [SerializeField] bool applyActivityRewards = true;
    [Tooltip("If enabled, Activity rows also apply relationship rewards stored on the activity.")]
    [SerializeField] bool applyActivityRelationshipRewards;

    [Header("Assignment Behavior")]
    [Tooltip("Party index used when Start With Specific Party Index is enabled.")]
    [Min(0)]
    [SerializeField] int partyIndex;
    [Tooltip("If enabled, Pokemon assignment rows use Party Index instead of the first eligible Pokemon.")]
    [SerializeField] bool startWithSpecificPartyIndex;
    [Tooltip("If enabled, ready assignments from this source are claimed before starting a new one.")]
    [SerializeField] bool claimReadyPokemonAssignmentsFirst;

    [Header("Context")]
    [Tooltip("Optional region override passed into situation event and pool filters.")]
    [SerializeField] RegionInfoDefinition regionOverride = null;
    [Tooltip("Optional activity zone override passed into assignment, situation event and pool filters.")]
    [SerializeField] ActivityZoneDefinition zoneOverride = null;

    [Header("Access")]
    [Tooltip("Optional reusable access profile checked before this row can run.")]
    [SerializeField] AccessProfileDefinition accessProfile = null;
    [Tooltip("Additional requirements checked before this row can run.")]
    [SerializeField] List<ActivityRequirement> extraRequirements = new List<ActivityRequirement>();
    [Tooltip("Message shown when this row is locked and no more specific failure exists.")]
    [SerializeField] string lockedMessage = "This option is not available yet.";

    public string EntryId => entryId;
    public string DisplayNameOverride => displayNameOverride;
    public string DescriptionOverride => descriptionOverride;
    public string ActionLabel => actionLabel;
    public int Priority => priority;
    public bool HideWhenLocked => hideWhenLocked;
    public RoleActivityBoardEntryType EntryType => entryType;
    public string SourceIdOverride => sourceIdOverride;
    public ActivityDefinition Activity => activity;
    public JobDefinition Job => job;
    public PokemonAssignmentDefinition PokemonAssignment => pokemonAssignment;
    public PokemonAssignmentBoardDefinition PokemonAssignmentBoard => pokemonAssignmentBoard;
    public SocialActivityDefinition SocialActivity => socialActivity;
    public SituationEventDefinition SituationEvent => situationEvent;
    public SituationEventPoolDefinition SituationEventPool => situationEventPool;
    public IReadOnlyList<LifePathReward> LifePathRewards => lifePathRewards != null ? (IReadOnlyList<LifePathReward>)lifePathRewards : Array.Empty<LifePathReward>();
    public bool PayActivityCosts => payActivityCosts;
    public bool ApplyActivityRewards => applyActivityRewards;
    public bool ApplyActivityRelationshipRewards => applyActivityRelationshipRewards;
    public int PartyIndex => Mathf.Max(0, partyIndex);
    public bool StartWithSpecificPartyIndex => startWithSpecificPartyIndex;
    public bool ClaimReadyPokemonAssignmentsFirst => claimReadyPokemonAssignmentsFirst;
    public RegionInfoDefinition RegionOverride => regionOverride;
    public ActivityZoneDefinition ZoneOverride => zoneOverride;
    public AccessProfileDefinition AccessProfile => accessProfile;
    public IReadOnlyList<ActivityRequirement> ExtraRequirements => extraRequirements != null ? (IReadOnlyList<ActivityRequirement>)extraRequirements : Array.Empty<ActivityRequirement>();

    public bool HasTarget() {
        switch(entryType) {
            case RoleActivityBoardEntryType.Activity:
                return activity != null;
            case RoleActivityBoardEntryType.Job:
                return job != null;
            case RoleActivityBoardEntryType.PokemonAssignment:
                return pokemonAssignment != null;
            case RoleActivityBoardEntryType.PokemonAssignmentBoard:
                return pokemonAssignmentBoard != null;
            case RoleActivityBoardEntryType.SocialActivity:
                return socialActivity != null;
            case RoleActivityBoardEntryType.SituationEvent:
                return situationEvent != null;
            case RoleActivityBoardEntryType.SituationEventPool:
                return situationEventPool != null;
            case RoleActivityBoardEntryType.LifePathRewards:
                return LifePathRewards.Any(reward => reward != null);
            default:
                return false;
        }
    }

    public string ResolveEntryId() {
        if(!string.IsNullOrWhiteSpace(entryId)) {
            return entryId;
        }

        switch(entryType) {
            case RoleActivityBoardEntryType.Activity:
                return activity != null ? activity.Id : string.Empty;
            case RoleActivityBoardEntryType.Job:
                return job != null ? job.Id : string.Empty;
            case RoleActivityBoardEntryType.PokemonAssignment:
                return pokemonAssignment != null ? pokemonAssignment.Id : string.Empty;
            case RoleActivityBoardEntryType.PokemonAssignmentBoard:
                return pokemonAssignmentBoard != null ? pokemonAssignmentBoard.Id : string.Empty;
            case RoleActivityBoardEntryType.SocialActivity:
                return socialActivity != null ? socialActivity.Id : string.Empty;
            case RoleActivityBoardEntryType.SituationEvent:
                return situationEvent != null ? situationEvent.Id : string.Empty;
            case RoleActivityBoardEntryType.SituationEventPool:
                return situationEventPool != null ? situationEventPool.Id : string.Empty;
            default:
                return "life-path-rewards";
        }
    }

    public string ResolveDisplayName() {
        if(!string.IsNullOrWhiteSpace(displayNameOverride)) {
            return displayNameOverride;
        }

        switch(entryType) {
            case RoleActivityBoardEntryType.Activity:
                return activity != null ? activity.DisplayName : string.Empty;
            case RoleActivityBoardEntryType.Job:
                return job != null ? job.DisplayName : string.Empty;
            case RoleActivityBoardEntryType.PokemonAssignment:
                return pokemonAssignment != null ? pokemonAssignment.DisplayName : string.Empty;
            case RoleActivityBoardEntryType.PokemonAssignmentBoard:
                return pokemonAssignmentBoard != null ? pokemonAssignmentBoard.DisplayName : string.Empty;
            case RoleActivityBoardEntryType.SocialActivity:
                return socialActivity != null ? socialActivity.DisplayName : string.Empty;
            case RoleActivityBoardEntryType.SituationEvent:
                return situationEvent != null ? situationEvent.DisplayName : string.Empty;
            case RoleActivityBoardEntryType.SituationEventPool:
                return situationEventPool != null ? situationEventPool.DisplayName : string.Empty;
            default:
                return "Life Path Rewards";
        }
    }

    public string ResolveDescription() {
        if(!string.IsNullOrWhiteSpace(descriptionOverride)) {
            return descriptionOverride;
        }

        switch(entryType) {
            case RoleActivityBoardEntryType.Activity:
                return activity != null ? activity.Description : string.Empty;
            case RoleActivityBoardEntryType.Job:
                return job != null ? job.Description : string.Empty;
            case RoleActivityBoardEntryType.PokemonAssignment:
                return pokemonAssignment != null ? pokemonAssignment.Description : string.Empty;
            case RoleActivityBoardEntryType.PokemonAssignmentBoard:
                return pokemonAssignmentBoard != null ? pokemonAssignmentBoard.Description : string.Empty;
            case RoleActivityBoardEntryType.SocialActivity:
                return socialActivity != null ? socialActivity.Description : string.Empty;
            case RoleActivityBoardEntryType.SituationEvent:
                return situationEvent != null ? situationEvent.Description : string.Empty;
            case RoleActivityBoardEntryType.SituationEventPool:
                return situationEventPool != null ? situationEventPool.Description : string.Empty;
            default:
                return "Applies configured Life Path rewards.";
        }
    }

    public string ResolveSourceId(RoleActivityBoardDefinition board, string fallbackSourceId) {
        if(!string.IsNullOrWhiteSpace(sourceIdOverride)) {
            return sourceIdOverride;
        }

        if(board != null && board.UseBoardIdAsDefaultSource) {
            return $"role-board:{board.Id}:{ResolveEntryId()}";
        }

        return !string.IsNullOrWhiteSpace(fallbackSourceId) ? fallbackSourceId : ResolveEntryId();
    }

    public RegionInfoDefinition ResolveRegion(RegionInfoDefinition fallbackRegion) {
        return regionOverride != null ? regionOverride : fallbackRegion;
    }

    public ActivityZoneDefinition ResolveZone(ActivityZoneDefinition fallbackZone) {
        return zoneOverride != null ? zoneOverride : fallbackZone;
    }

    public RoleActivityBoardRow BuildRow(
        RoleActivityBoardDefinition board,
        string boardSourceId,
        string boardSourceName,
        RegionInfoDefinition region,
        ActivityZoneDefinition zone,
        bool canRun,
        string failureMessage) {
        var resolvedRegion = ResolveRegion(region);
        var resolvedZone = ResolveZone(zone);
        return new RoleActivityBoardRow {
            entryId = ResolveEntryId(),
            displayName = ResolveDisplayName(),
            description = ResolveDescription(),
            actionLabel = string.IsNullOrWhiteSpace(actionLabel) ? "Select" : actionLabel,
            entryType = entryType,
            priority = priority,
            canRun = canRun,
            failureMessage = failureMessage,
            sourceId = ResolveSourceId(board, boardSourceId),
            sourceName = string.IsNullOrWhiteSpace(boardSourceName) ? ResolveDisplayName() : boardSourceName,
            regionId = resolvedRegion != null ? resolvedRegion.Id : string.Empty,
            regionName = resolvedRegion != null ? resolvedRegion.DisplayName : string.Empty,
            zoneId = resolvedZone != null ? resolvedZone.Id : string.Empty,
            zoneName = resolvedZone != null ? resolvedZone.DisplayName : string.Empty
        };
    }

    public bool CanRun(
        PlayerController player,
        RoleActivityBoardDefinition board,
        string boardSourceId,
        string boardSourceName,
        RegionInfoDefinition region,
        ActivityZoneDefinition zone,
        out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required.";
            return false;
        }

        if(!HasTarget()) {
            failureMessage = "This board entry has no target assigned.";
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
        string sourceId = ResolveSourceId(board, boardSourceId);

        switch(entryType) {
            case RoleActivityBoardEntryType.Activity:
                return activity.CanPerform(player, out failureMessage);
            case RoleActivityBoardEntryType.Job:
                var jobLog = player.GetComponent<PlayerJobLog>();
                if(jobLog == null) {
                    failureMessage = "The player has no job log.";
                    return false;
                }
                return job.CanAccept(player, jobLog, sourceId, out failureMessage);
            case RoleActivityBoardEntryType.PokemonAssignment:
                return FindEligiblePokemon(player, pokemonAssignment, resolvedZone, sourceId, out _, out failureMessage);
            case RoleActivityBoardEntryType.PokemonAssignmentBoard:
                return FindEligibleAssignmentBoardEntry(player, pokemonAssignmentBoard, resolvedZone, sourceId, out _, out _, out failureMessage);
            case RoleActivityBoardEntryType.SocialActivity:
                return socialActivity.CanRun(player, out failureMessage);
            case RoleActivityBoardEntryType.SituationEvent:
                return situationEvent.CanStart(player, resolvedRegion, resolvedZone, sourceId, out failureMessage);
            case RoleActivityBoardEntryType.SituationEventPool:
                if(!situationEventPool.MatchesLocation(resolvedRegion, resolvedZone)) {
                    failureMessage = "Situation event pool location filters did not match.";
                    return false;
                }
                failureMessage = null;
                return true;
            case RoleActivityBoardEntryType.LifePathRewards:
                if(!LifePathRewards.Any(reward => reward != null)) {
                    failureMessage = "No Life Path rewards assigned.";
                    return false;
                }
                failureMessage = null;
                return true;
            default:
                failureMessage = "Unsupported board entry type.";
                return false;
        }
    }

    public bool TryRun(
        PlayerController player,
        RoleActivityBoardDefinition board,
        string boardSourceId,
        string boardSourceName,
        RegionInfoDefinition region,
        ActivityZoneDefinition zone,
        UnityEngine.Object context,
        out RoleActivityBoardRunResult result) {
        string sourceId = ResolveSourceId(board, boardSourceId);
        string sourceName = string.IsNullOrWhiteSpace(boardSourceName) ? ResolveDisplayName() : boardSourceName;
        result = RoleActivityBoardRunResult.Blocked(board, this, sourceId, "Board entry could not run.");

        if(!CanRun(player, board, boardSourceId, boardSourceName, region, zone, out var failureMessage)) {
            result = RoleActivityBoardRunResult.Blocked(board, this, sourceId, failureMessage);
            PublishEntryAccess(player, false, sourceId, failureMessage, context);
            return false;
        }

        PublishEntryAccess(player, true, sourceId, accessProfile != null ? accessProfile.PassedMessage : null, context);

        bool success;
        string message;
        switch(entryType) {
            case RoleActivityBoardEntryType.Activity:
                success = RunActivity(player, out message);
                break;
            case RoleActivityBoardEntryType.Job:
                success = player.GetComponent<PlayerJobLog>().Accept(job, sourceId, out message);
                if(success) {
                    message = $"{job.DisplayName} accepted.";
                }
                break;
            case RoleActivityBoardEntryType.PokemonAssignment:
                success = RunPokemonAssignment(player, pokemonAssignment, ResolveZone(zone), sourceId, sourceName, out message);
                break;
            case RoleActivityBoardEntryType.PokemonAssignmentBoard:
                success = RunPokemonAssignmentBoard(player, pokemonAssignmentBoard, ResolveZone(zone), sourceId, sourceName, out message);
                break;
            case RoleActivityBoardEntryType.SocialActivity:
                success = socialActivity.TryRun(player, sourceId, context, out var socialResult);
                message = socialResult != null ? socialResult.message : success ? $"{socialActivity.DisplayName} completed." : "Social activity failed.";
                break;
            case RoleActivityBoardEntryType.SituationEvent:
                var eventResult = situationEvent.TryStart(player, ResolveRegion(region), ResolveZone(zone), sourceId, sourceName, context);
                success = eventResult != null && !eventResult.blocked;
                message = success ? $"{situationEvent.DisplayName} started." : eventResult != null ? eventResult.failureMessage : "Situation event failed.";
                break;
            case RoleActivityBoardEntryType.SituationEventPool:
                var poolResult = situationEventPool.Roll(player, ResolveRegion(region), ResolveZone(zone), sourceId, sourceName, context);
                success = poolResult != null && poolResult.startedEvents > 0 && !poolResult.blocked;
                message = success
                    ? $"{situationEventPool.DisplayName} started {poolResult.startedEvents} event(s)."
                    : poolResult != null ? poolResult.failureMessage : "Situation event pool failed.";
                break;
            case RoleActivityBoardEntryType.LifePathRewards:
                var lifePathLog = player.GetComponent<PlayerLifePathLog>() ?? player.gameObject.AddComponent<PlayerLifePathLog>();
                lifePathLog.ApplyRewards(LifePathRewards, sourceId, ResolveDisplayName(), context);
                success = true;
                message = $"{ResolveDisplayName()} rewards applied.";
                break;
            default:
                success = false;
                message = "Unsupported board entry type.";
                break;
        }

        result = success
            ? RoleActivityBoardRunResult.Succeeded(board, this, sourceId, message)
            : RoleActivityBoardRunResult.Blocked(board, this, sourceId, message);
        return success;
    }

    bool RunActivity(PlayerController player, out string message) {
        if(payActivityCosts && !activity.TryPayCosts(player, out message)) {
            return false;
        }

        if(applyActivityRewards) {
            activity.ApplyRewards(player);
        }

        if(applyActivityRelationshipRewards) {
            activity.ApplyRelationshipRewards(player);
        }

        message = $"{activity.DisplayName} completed.";
        return true;
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

    void PublishEntryAccess(PlayerController player, bool passed, string sourceId, string message, UnityEngine.Object context) {
        if(accessProfile == null) {
            return;
        }

        accessProfile.PublishChecked(player, passed, sourceId, message, context);
    }
}

[Serializable]
public class RoleActivityBoardSnapshot {
    [Tooltip("Definition id of this board.")]
    public string boardId;
    [Tooltip("Display name of this board.")]
    public string boardName;
    [Tooltip("Description of this board.")]
    public string description;
    [Tooltip("Broad category used by future UI filters.")]
    public RoleActivityBoardCategory category;
    [Tooltip("Resolved source id used by board actions.")]
    public string sourceId;
    [Tooltip("Resolved source name used by board actions.")]
    public string sourceName;
    [Tooltip("Region id used by rows in this snapshot.")]
    public string regionId;
    [Tooltip("Region name used by rows in this snapshot.")]
    public string regionName;
    [Tooltip("Activity zone id used by rows in this snapshot.")]
    public string zoneId;
    [Tooltip("Activity zone name used by rows in this snapshot.")]
    public string zoneName;
    [Tooltip("If enabled, the board itself passed access checks.")]
    public bool usable;
    [Tooltip("Failure reason if the board itself is locked.")]
    public string failureMessage;
    [Tooltip("Rows currently visible on this board.")]
    public List<RoleActivityBoardRow> rows = new List<RoleActivityBoardRow>();
}

[Serializable]
public class RoleActivityBoardRow {
    [Tooltip("Stable row id used by future UI actions.")]
    public string entryId;
    [Tooltip("Display name shown for this row.")]
    public string displayName;
    [Tooltip("Description shown for this row.")]
    public string description;
    [Tooltip("Suggested action label for this row.")]
    public string actionLabel;
    [Tooltip("Kind of content this row exposes.")]
    public RoleActivityBoardEntryType entryType;
    [Tooltip("Sort priority copied from the definition.")]
    public int priority;
    [Tooltip("If enabled, this row can run right now.")]
    public bool canRun;
    [Tooltip("Failure reason shown when the row is locked.")]
    public string failureMessage;
    [Tooltip("Resolved source id used when the row runs.")]
    public string sourceId;
    [Tooltip("Resolved source name used when the row runs.")]
    public string sourceName;
    [Tooltip("Region id used by this row.")]
    public string regionId;
    [Tooltip("Region name used by this row.")]
    public string regionName;
    [Tooltip("Activity zone id used by this row.")]
    public string zoneId;
    [Tooltip("Activity zone name used by this row.")]
    public string zoneName;
}

public class RoleActivityBoardRunResult {
    public readonly bool success;
    public readonly string boardId;
    public readonly string boardName;
    public readonly string entryId;
    public readonly string entryName;
    public readonly RoleActivityBoardEntryType entryType;
    public readonly string sourceId;
    public readonly string message;

    RoleActivityBoardRunResult(bool success, RoleActivityBoardDefinition board, RoleActivityBoardEntry entry, string sourceId, string message) {
        this.success = success;
        boardId = board != null ? board.Id : string.Empty;
        boardName = board != null ? board.DisplayName : string.Empty;
        entryId = entry != null ? entry.ResolveEntryId() : string.Empty;
        entryName = entry != null ? entry.ResolveDisplayName() : string.Empty;
        entryType = entry != null ? entry.EntryType : RoleActivityBoardEntryType.Activity;
        this.sourceId = sourceId;
        this.message = message;
    }

    public static RoleActivityBoardRunResult Succeeded(RoleActivityBoardDefinition board, RoleActivityBoardEntry entry, string sourceId, string message) {
        return new RoleActivityBoardRunResult(true, board, entry, sourceId, message);
    }

    public static RoleActivityBoardRunResult Blocked(RoleActivityBoardDefinition board, RoleActivityBoardEntry entry, string sourceId, string message) {
        return new RoleActivityBoardRunResult(false, board, entry, sourceId, message);
    }
}
