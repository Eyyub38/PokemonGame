using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Activities/Journey Incident Board")]
public class JourneyIncidentBoardDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this journey incident board. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in future UI, prompts and debug messages. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note or player-facing explanation of this board.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Free-form tags such as route, camp, ranger, transit, town, danger or rare.")]
    [SerializeField] List<string> tags = new List<string>();
    [Tooltip("Optional icon used by future board UI.")]
    [SerializeField] Sprite icon = null;

    [Header("Access")]
    [Tooltip("Optional reusable access profile checked before this board can roll incidents.")]
    [SerializeField] AccessProfileDefinition accessProfile = null;
    [Tooltip("Additional requirements checked before this board can roll incidents.")]
    [SerializeField] List<ActivityRequirement> requirements = new List<ActivityRequirement>();
    [Tooltip("Message shown when this board is locked and no more specific failure exists.")]
    [SerializeField] string lockedMessage = "This journey incident board is not available right now.";
    [Tooltip("If enabled, access profile checks are published to access logs/events when a source uses this board.")]
    [SerializeField] bool publishAccessChecks = true;

    [Header("Location Scope")]
    [Tooltip("If enabled, this board can roll in any region or zone.")]
    [SerializeField] bool globalScope = true;
    [Tooltip("Specific regions where this board can roll when Global Scope is disabled.")]
    [SerializeField] List<RegionInfoDefinition> allowedRegions = new List<RegionInfoDefinition>();
    [Tooltip("Region tags accepted when Global Scope is disabled.")]
    [SerializeField] List<string> allowedRegionTags = new List<string>();
    [Tooltip("Specific activity zones where this board can roll when Global Scope is disabled.")]
    [SerializeField] List<ActivityZoneDefinition> allowedZones = new List<ActivityZoneDefinition>();
    [Tooltip("Activity zone tags accepted when Global Scope is disabled.")]
    [SerializeField] List<string> allowedZoneTags = new List<string>();

    [Header("Roll Rules")]
    [Tooltip("Chance that this board attempts to pick incidents when evaluated.")]
    [Range(0f, 1f)]
    [SerializeField] float rollChance = 1f;
    [Tooltip("Maximum incidents this board can activate per roll.")]
    [Min(1)]
    [SerializeField] int maxIncidentsPerRoll = 1;
    [Tooltip("If enabled, the same incident definition cannot be selected twice in one roll.")]
    [SerializeField] bool preventDuplicateIncidentsPerRoll = true;
    [Tooltip("If enabled, candidates are sorted by priority before weighted selection ties are resolved.")]
    [SerializeField] bool preferHigherPriorityIncidents = true;
    [Tooltip("If enabled, entries without source overrides use this board id as their source id.")]
    [SerializeField] bool useBoardIdAsDefaultSource = true;

    [Header("Entries")]
    [Tooltip("Editable incident entries rolled by this board.")]
    [SerializeField] List<JourneyIncidentBoardEntry> entries = new List<JourneyIncidentBoardEntry>();
    [Tooltip("If enabled, locked entries are included in snapshots unless each entry hides itself.")]
    [SerializeField] bool showLockedEntriesByDefault = true;

    [Header("Events")]
    [Tooltip("Optional event published when this board activates at least one incident.")]
    [SerializeField] GameEventDefinition rolledEvent = null;
    [Tooltip("Optional event published when this board is blocked or has no candidates.")]
    [SerializeField] GameEventDefinition blockedEvent = null;
    [Tooltip("If enabled, board roll events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed;
    [Tooltip("If enabled, board roll events are written to GameDebugLogger.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public Sprite Icon => icon;
    public AccessProfileDefinition AccessProfile => accessProfile;
    public IReadOnlyList<ActivityRequirement> Requirements => requirements != null ? (IReadOnlyList<ActivityRequirement>)requirements : Array.Empty<ActivityRequirement>();
    public bool GlobalScope => globalScope;
    public IReadOnlyList<RegionInfoDefinition> AllowedRegions => allowedRegions != null ? (IReadOnlyList<RegionInfoDefinition>)allowedRegions : Array.Empty<RegionInfoDefinition>();
    public IReadOnlyList<string> AllowedRegionTags => allowedRegionTags != null ? (IReadOnlyList<string>)allowedRegionTags : Array.Empty<string>();
    public IReadOnlyList<ActivityZoneDefinition> AllowedZones => allowedZones != null ? (IReadOnlyList<ActivityZoneDefinition>)allowedZones : Array.Empty<ActivityZoneDefinition>();
    public IReadOnlyList<string> AllowedZoneTags => allowedZoneTags != null ? (IReadOnlyList<string>)allowedZoneTags : Array.Empty<string>();
    public float RollChance => Mathf.Clamp01(rollChance);
    public int MaxIncidentsPerRoll => Mathf.Max(1, maxIncidentsPerRoll);
    public bool UseBoardIdAsDefaultSource => useBoardIdAsDefaultSource;
    public IReadOnlyList<JourneyIncidentBoardEntry> Entries => entries != null ? (IReadOnlyList<JourneyIncidentBoardEntry>)entries : Array.Empty<JourneyIncidentBoardEntry>();
    public bool ShowLockedEntriesByDefault => showLockedEntriesByDefault;

    public bool CanUse(PlayerController player, RegionInfoDefinition region, ActivityZoneDefinition zone, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required to use this journey incident board.";
            return false;
        }

        if(accessProfile != null && !accessProfile.CanAccess(player, out failureMessage)) {
            failureMessage = string.IsNullOrWhiteSpace(failureMessage) ? lockedMessage : failureMessage;
            return false;
        }

        if(!MatchesLocation(region, zone)) {
            failureMessage = "Journey incident board location filters did not match.";
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

    public JourneyIncidentBoardSnapshot BuildSnapshot(
        PlayerController player,
        PlayerJourneyIncidentLog log,
        string sourceId,
        string sourceName,
        RegionInfoDefinition region,
        ActivityZoneDefinition zone,
        bool includeLocked,
        UnityEngine.Object context = null) {
        string boardSourceId = ResolveBoardSourceId(sourceId);
        string boardSourceName = string.IsNullOrWhiteSpace(sourceName) ? DisplayName : sourceName;
        bool usable = CanUse(player, region, zone, out var boardFailure);

        var snapshot = new JourneyIncidentBoardSnapshot {
            boardId = Id,
            boardName = DisplayName,
            description = Description,
            sourceId = boardSourceId,
            sourceName = boardSourceName,
            regionId = region != null ? region.Id : string.Empty,
            regionName = region != null ? region.DisplayName : string.Empty,
            zoneId = zone != null ? zone.Id : string.Empty,
            zoneName = zone != null ? zone.DisplayName : string.Empty,
            usable = usable,
            failureMessage = boardFailure,
            rows = new List<JourneyIncidentBoardRow>()
        };

        foreach(var entry in GetOrderedEntries()) {
            string rowFailure = boardFailure;
            bool canActivate = usable && entry.CanActivate(player, log, this, boardSourceId, region, zone, out rowFailure);
            if(!canActivate && string.IsNullOrWhiteSpace(rowFailure)) {
                rowFailure = boardFailure;
            }

            if(!includeLocked && !canActivate) {
                continue;
            }

            if(!canActivate && entry.HideWhenLocked) {
                continue;
            }

            snapshot.rows.Add(entry.BuildRow(this, boardSourceId, boardSourceName, region, zone, canActivate, rowFailure));
        }

        return snapshot;
    }

    public JourneyIncidentBoardRollResult Roll(
        PlayerController player,
        RegionInfoDefinition region = null,
        ActivityZoneDefinition zone = null,
        string sourceId = null,
        string sourceName = null,
        UnityEngine.Object context = null) {
        string boardSourceId = ResolveBoardSourceId(sourceId);
        string boardSourceName = ResolveBoardSourceName(sourceName);
        var result = new JourneyIncidentBoardRollResult(this, boardSourceId, region, zone);
        var log = player != null ? player.GetComponent<PlayerJourneyIncidentLog>() ?? player.gameObject.AddComponent<PlayerJourneyIncidentLog>() : null;

        if(!CanUse(player, region, zone, out var failureMessage)) {
            result.blocked = true;
            result.failureMessage = failureMessage;
            PublishAccessCheck(player, false, boardSourceId, failureMessage, context);
            PublishBoardEvent(blockedEvent, result, context, GameEventImportance.Warning);
            return result;
        }

        PublishAccessCheck(player, true, boardSourceId, accessProfile != null ? accessProfile.PassedMessage : null, context);
        if(UnityEngine.Random.value > RollChance) {
            result.blocked = true;
            result.failureMessage = "Journey incident board roll chance failed.";
            PublishBoardEvent(blockedEvent, result, context, GameEventImportance.Trace);
            return result;
        }

        var candidates = BuildCandidates(player, log, region, zone, boardSourceId).ToList();
        if(candidates.Count == 0) {
            result.blocked = true;
            result.failureMessage = "Journey incident board had no valid candidates.";
            PublishBoardEvent(blockedEvent, result, context, GameEventImportance.Trace);
            return result;
        }

        var selectedIds = new HashSet<string>();
        for(int i = 0; i < MaxIncidentsPerRoll && candidates.Count > 0; i++) {
            var candidate = PickWeighted(candidates);
            if(candidate == null || candidate.entry == null || candidate.entry.Incident == null) {
                break;
            }

            string incidentSourceId = candidate.entry.ResolveSourceId(this, boardSourceId);
            var activationResult = candidate.entry.Incident.Activate(player, region, zone, incidentSourceId, boardSourceName, context != null ? context : this);
            result.attemptedIncidents++;
            if(activationResult != null && !activationResult.blocked) {
                result.activatedIncidents++;
                result.activatedIncidentIds.Add(candidate.entry.Incident.Id);
                if(preventDuplicateIncidentsPerRoll) {
                    selectedIds.Add(candidate.entry.Incident.Id);
                }
            } else {
                result.blockedIncidents++;
                if(activationResult != null && !string.IsNullOrWhiteSpace(activationResult.failureMessage)) {
                    result.messages.Add($"{candidate.entry.Incident.DisplayName}: {activationResult.failureMessage}");
                }
            }

            candidates.Remove(candidate);
            if(preventDuplicateIncidentsPerRoll && selectedIds.Count > 0) {
                candidates.RemoveAll(entry => entry.entry?.Incident != null && selectedIds.Contains(entry.entry.Incident.Id));
            }
        }

        PublishBoardEvent(result.activatedIncidents > 0 ? rolledEvent : blockedEvent, result, context, result.activatedIncidents > 0 ? GameEventImportance.Info : GameEventImportance.Trace);
        return result;
    }

    public IEnumerable<JourneyIncidentBoardEntry> GetOrderedEntries() {
        return Entries
            .Where(entry => entry != null && entry.Incident != null)
            .OrderByDescending(entry => entry.Priority)
            .ThenBy(entry => entry.ResolveDisplayName());
    }

    public bool MatchesLocation(RegionInfoDefinition region, ActivityZoneDefinition zone) {
        if(globalScope) {
            return true;
        }

        bool hasFilters = AllowedRegions.Count > 0 || AllowedRegionTags.Count > 0 || AllowedZones.Count > 0 || AllowedZoneTags.Count > 0;
        if(!hasFilters) {
            return false;
        }

        if(region != null && (AllowedRegions.Contains(region) || AllowedRegionTags.Any(region.HasTag))) {
            return true;
        }

        if(zone != null && (AllowedZones.Contains(zone) || AllowedZoneTags.Any(zone.HasTag))) {
            return true;
        }

        return region != null && zone != null && AllowedRegions.Any(entry => entry != null && entry.ActivityZones.Contains(zone));
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    public string ResolveBoardSourceId(string sourceId) {
        if(!string.IsNullOrWhiteSpace(sourceId)) {
            return sourceId;
        }

        return useBoardIdAsDefaultSource ? $"journey-board:{Id}" : Id;
    }

    IEnumerable<JourneyIncidentBoardCandidate> BuildCandidates(PlayerController player, PlayerJourneyIncidentLog log, RegionInfoDefinition region, ActivityZoneDefinition zone, string boardSourceId) {
        foreach(var entry in Entries) {
            if(entry == null || entry.Incident == null) {
                continue;
            }

            if(!entry.CanActivate(player, log, this, boardSourceId, region, zone, out _)) {
                continue;
            }

            int weight = entry.CalculateWeight(player, region, zone);
            if(weight <= 0) {
                continue;
            }

            yield return new JourneyIncidentBoardCandidate(entry, weight);
        }
    }

    JourneyIncidentBoardCandidate PickWeighted(List<JourneyIncidentBoardCandidate> candidates) {
        if(candidates == null || candidates.Count == 0) {
            return null;
        }

        if(preferHigherPriorityIncidents) {
            candidates = candidates
                .OrderByDescending(candidate => candidate.entry.Incident.Priority)
                .ThenByDescending(candidate => candidate.weight)
                .ToList();
        }

        int totalWeight = candidates.Sum(candidate => Mathf.Max(0, candidate.weight));
        if(totalWeight <= 0) {
            return null;
        }

        int roll = UnityEngine.Random.Range(0, totalWeight);
        int cursor = 0;
        foreach(var candidate in candidates) {
            cursor += Mathf.Max(0, candidate.weight);
            if(roll < cursor) {
                return candidate;
            }
        }

        return candidates[candidates.Count - 1];
    }

    void PublishBoardEvent(GameEventDefinition eventDefinition, JourneyIncidentBoardRollResult result, UnityEngine.Object context, GameEventImportance importance) {
        GameEventPublishing.PublishOptional(
            eventDefinition,
            $"journey-incident-board.roll.{Id}",
            result != null && result.activatedIncidents > 0 ? $"{DisplayName} activated {result.activatedIncidents} incident(s)." : $"{DisplayName} did not activate an incident.",
            GameEventCategory.WorldEvent,
            importance,
            context != null ? context : this,
            "JourneyIncidentBoardDefinition",
            GameEventScope.Player,
            showInFeed: showEventsInFeed,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("boardId", Id),
            GameEventPublishing.Value("boardName", DisplayName),
            GameEventPublishing.Value("sourceId", result != null ? result.sourceId : string.Empty),
            GameEventPublishing.Value("activatedIncidents", result != null ? result.activatedIncidents : 0),
            GameEventPublishing.Value("attemptedIncidents", result != null ? result.attemptedIncidents : 0),
            GameEventPublishing.Value("blockedIncidents", result != null ? result.blockedIncidents : 0),
            GameEventPublishing.Value("failureMessage", result != null ? result.failureMessage : string.Empty));
    }

    void PublishAccessCheck(PlayerController player, bool passed, string sourceId, string message, UnityEngine.Object context) {
        if(accessProfile == null || !publishAccessChecks) {
            return;
        }

        accessProfile.PublishChecked(player, passed, sourceId, message, context != null ? context : this);
    }

    string ResolveBoardSourceName(string sourceName) {
        return string.IsNullOrWhiteSpace(sourceName) ? DisplayName : sourceName;
    }
}

[Serializable]
public class JourneyIncidentBoardEntry {
    [Header("Identity")]
    [Tooltip("Optional stable row id used by UI actions. Empty uses the assigned incident id.")]
    [SerializeField] string entryId = string.Empty;
    [Tooltip("Optional display name override for this row.")]
    [SerializeField] string displayNameOverride = string.Empty;
    [Tooltip("Optional description override for this row.")]
    [TextArea]
    [SerializeField] string descriptionOverride = string.Empty;
    [Tooltip("Suggested action label for future UI buttons, such as Investigate, Help, Follow or Ignore.")]
    [SerializeField] string actionLabel = "Investigate";
    [Tooltip("Higher priority rows appear first in snapshots.")]
    [SerializeField] int priority;
    [Tooltip("If enabled, this entry is omitted from snapshots when locked.")]
    [SerializeField] bool hideWhenLocked;

    [Header("Target")]
    [Tooltip("Journey incident activated by this board entry.")]
    [SerializeField] JourneyIncidentDefinition incident = null;
    [Tooltip("Optional source id override saved in target logs/events. Empty uses the board/source id.")]
    [SerializeField] string sourceIdOverride = string.Empty;
    [Tooltip("Base selection weight for this incident. 0 falls back to the incident's Base Weight.")]
    [Min(0)]
    [SerializeField] int weight;
    [Tooltip("Additional requirements checked only for this board entry.")]
    [SerializeField] List<ActivityRequirement> extraRequirements = new List<ActivityRequirement>();
    [Tooltip("Optional weight modifiers controlled by active world conditions.")]
    [SerializeField] List<SituationEventWeightModifier> weightModifiers = new List<SituationEventWeightModifier>();

    public string EntryId => entryId;
    public string DisplayNameOverride => displayNameOverride;
    public string DescriptionOverride => descriptionOverride;
    public string ActionLabel => actionLabel;
    public int Priority => priority;
    public bool HideWhenLocked => hideWhenLocked;
    public JourneyIncidentDefinition Incident => incident;
    public string SourceIdOverride => sourceIdOverride;
    public int Weight => Mathf.Max(0, weight);
    public IReadOnlyList<ActivityRequirement> ExtraRequirements => extraRequirements != null ? (IReadOnlyList<ActivityRequirement>)extraRequirements : Array.Empty<ActivityRequirement>();
    public IReadOnlyList<SituationEventWeightModifier> WeightModifiers => weightModifiers != null ? (IReadOnlyList<SituationEventWeightModifier>)weightModifiers : Array.Empty<SituationEventWeightModifier>();

    public bool CanActivate(
        PlayerController player,
        PlayerJourneyIncidentLog log,
        JourneyIncidentBoardDefinition board,
        string boardSourceId,
        RegionInfoDefinition region,
        ActivityZoneDefinition zone,
        out string failureMessage) {
        if(incident == null) {
            failureMessage = "No journey incident assigned.";
            return false;
        }

        foreach(var requirement in ExtraRequirements) {
            if(requirement != null && !requirement.IsMet(player)) {
                failureMessage = requirement.FailureMessage;
                return false;
            }
        }

        return incident.CanActivate(player, log, region, zone, ResolveSourceId(board, boardSourceId), out failureMessage);
    }

    public int CalculateWeight(PlayerController player, RegionInfoDefinition region, ActivityZoneDefinition zone) {
        int value = Weight > 0 ? Weight : incident != null ? incident.BaseWeight : 0;
        foreach(var modifier in WeightModifiers) {
            value = modifier != null ? modifier.Apply(value, player, region, zone) : value;
        }

        return Mathf.Max(0, value);
    }

    public string ResolveEntryId() {
        if(!string.IsNullOrWhiteSpace(entryId)) {
            return entryId;
        }

        return incident != null ? incident.Id : string.Empty;
    }

    public string ResolveDisplayName() {
        if(!string.IsNullOrWhiteSpace(displayNameOverride)) {
            return displayNameOverride;
        }

        return incident != null ? incident.DisplayName : string.Empty;
    }

    public string ResolveDescription() {
        if(!string.IsNullOrWhiteSpace(descriptionOverride)) {
            return descriptionOverride;
        }

        return incident != null ? incident.Description : string.Empty;
    }

    public string ResolveSourceId(JourneyIncidentBoardDefinition board, string boardSourceId) {
        if(!string.IsNullOrWhiteSpace(sourceIdOverride)) {
            return sourceIdOverride;
        }

        return !string.IsNullOrWhiteSpace(boardSourceId) ? boardSourceId : board != null ? board.ResolveBoardSourceId(null) : string.Empty;
    }

    public JourneyIncidentBoardRow BuildRow(
        JourneyIncidentBoardDefinition board,
        string boardSourceId,
        string boardSourceName,
        RegionInfoDefinition region,
        ActivityZoneDefinition zone,
        bool canActivate,
        string failureMessage) {
        return new JourneyIncidentBoardRow {
            entryId = ResolveEntryId(),
            incidentId = incident != null ? incident.Id : string.Empty,
            displayName = ResolveDisplayName(),
            description = ResolveDescription(),
            actionLabel = string.IsNullOrWhiteSpace(actionLabel) ? "Investigate" : actionLabel,
            category = incident != null ? incident.Category : JourneyIncidentCategory.General,
            severity = incident != null ? incident.Severity : JourneyIncidentSeverity.Info,
            priority = priority,
            weight = CalculateWeight(PlayerController.i, region, zone),
            canActivate = canActivate,
            failureMessage = failureMessage,
            sourceId = ResolveSourceId(board, boardSourceId),
            sourceName = string.IsNullOrWhiteSpace(boardSourceName) ? ResolveDisplayName() : boardSourceName,
            regionId = region != null ? region.Id : string.Empty,
            regionName = region != null ? region.DisplayName : string.Empty,
            zoneId = zone != null ? zone.Id : string.Empty,
            zoneName = zone != null ? zone.DisplayName : string.Empty
        };
    }
}

[Serializable]
public class JourneyIncidentBoardSnapshot {
    [Tooltip("Definition id of this board.")]
    public string boardId;
    [Tooltip("Display name of this board.")]
    public string boardName;
    [Tooltip("Description shown for this board.")]
    public string description;
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
    [Tooltip("If enabled, the board itself passed access and location checks.")]
    public bool usable;
    [Tooltip("Failure reason if the board itself is locked.")]
    public string failureMessage;
    [Tooltip("Rows currently visible on this board.")]
    public List<JourneyIncidentBoardRow> rows = new List<JourneyIncidentBoardRow>();
}

[Serializable]
public class JourneyIncidentBoardRow {
    [Tooltip("Stable row id used by future UI actions.")]
    public string entryId;
    [Tooltip("Incident definition id.")]
    public string incidentId;
    [Tooltip("Display name shown for this row.")]
    public string displayName;
    [Tooltip("Description shown for this row.")]
    public string description;
    [Tooltip("Suggested button label for this row.")]
    public string actionLabel;
    [Tooltip("Incident category used by filters.")]
    public JourneyIncidentCategory category;
    [Tooltip("Incident severity used by filters and future UI color.")]
    public JourneyIncidentSeverity severity;
    [Tooltip("Sort priority copied from the board entry.")]
    public int priority;
    [Tooltip("Current calculated roll weight.")]
    public int weight;
    [Tooltip("If enabled, this row can activate right now.")]
    public bool canActivate;
    [Tooltip("Failure reason shown when the row is locked.")]
    public string failureMessage;
    [Tooltip("Resolved source id used when the incident activates.")]
    public string sourceId;
    [Tooltip("Resolved source name used when the incident activates.")]
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

class JourneyIncidentBoardCandidate {
    public readonly JourneyIncidentBoardEntry entry;
    public readonly int weight;

    public JourneyIncidentBoardCandidate(JourneyIncidentBoardEntry entry, int weight) {
        this.entry = entry;
        this.weight = weight;
    }
}

public class JourneyIncidentBoardRollResult {
    public readonly string boardId;
    public readonly string boardName;
    public readonly string sourceId;
    public readonly RegionInfoDefinition region;
    public readonly ActivityZoneDefinition zone;
    public int attemptedIncidents;
    public int activatedIncidents;
    public int blockedIncidents;
    public bool blocked;
    public string failureMessage;
    public readonly List<string> activatedIncidentIds = new List<string>();
    public readonly List<string> messages = new List<string>();

    public JourneyIncidentBoardRollResult(JourneyIncidentBoardDefinition board, string sourceId, RegionInfoDefinition region, ActivityZoneDefinition zone) {
        boardId = board != null ? board.Id : string.Empty;
        boardName = board != null ? board.DisplayName : string.Empty;
        this.sourceId = sourceId;
        this.region = region;
        this.zone = zone;
    }
}
