using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Situation Events/Situation Event Pool")]
public class SituationEventPoolDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this situation event pool. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in debug/future UI. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note explaining where and why this pool rolls.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Free-form tags such as route, night, town, festival, danger or rare.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Location Scope")]
    [Tooltip("If enabled, this pool can roll in any region or zone.")]
    [SerializeField] bool globalScope = true;
    [Tooltip("Specific regions where this pool can roll when Global Scope is disabled.")]
    [SerializeField] List<RegionInfoDefinition> allowedRegions = new List<RegionInfoDefinition>();
    [Tooltip("Region tags accepted when Global Scope is disabled.")]
    [SerializeField] List<string> allowedRegionTags = new List<string>();
    [Tooltip("Specific activity zones where this pool can roll when Global Scope is disabled.")]
    [SerializeField] List<ActivityZoneDefinition> allowedZones = new List<ActivityZoneDefinition>();
    [Tooltip("Activity zone tags accepted when Global Scope is disabled.")]
    [SerializeField] List<string> allowedZoneTags = new List<string>();

    [Header("Roll Rules")]
    [Tooltip("Chance that this pool attempts to pick events when evaluated.")]
    [Range(0f, 1f)]
    [SerializeField] float rollChance = 1f;
    [Tooltip("Maximum events this pool can start per roll.")]
    [Min(1)]
    [SerializeField] int maxEventsPerRoll = 1;
    [Tooltip("If enabled, the same event definition cannot be selected twice in one roll.")]
    [SerializeField] bool preventDuplicateEventsPerRoll = true;
    [Tooltip("If enabled, candidates are sorted by priority before weighted selection ties are resolved.")]
    [SerializeField] bool preferHigherPriorityEvents = true;

    [Header("Entries")]
    [Tooltip("Editable event entries rolled by this pool.")]
    [SerializeField] List<SituationEventPoolEntry> entries = new List<SituationEventPoolEntry>();

    [Header("Events")]
    [Tooltip("Optional event published when this pool rolls at least one event.")]
    [SerializeField] GameEventDefinition rolledEvent = null;
    [Tooltip("Optional event published when this pool is blocked or has no candidates.")]
    [SerializeField] GameEventDefinition blockedEvent = null;
    [Tooltip("If enabled, pool roll events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed;
    [Tooltip("If enabled, pool roll events are written to GameDebugLogger.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public bool GlobalScope => globalScope;
    public IReadOnlyList<RegionInfoDefinition> AllowedRegions => allowedRegions != null ? (IReadOnlyList<RegionInfoDefinition>)allowedRegions : Array.Empty<RegionInfoDefinition>();
    public IReadOnlyList<string> AllowedRegionTags => allowedRegionTags != null ? (IReadOnlyList<string>)allowedRegionTags : Array.Empty<string>();
    public IReadOnlyList<ActivityZoneDefinition> AllowedZones => allowedZones != null ? (IReadOnlyList<ActivityZoneDefinition>)allowedZones : Array.Empty<ActivityZoneDefinition>();
    public IReadOnlyList<string> AllowedZoneTags => allowedZoneTags != null ? (IReadOnlyList<string>)allowedZoneTags : Array.Empty<string>();
    public float RollChance => Mathf.Clamp01(rollChance);
    public int MaxEventsPerRoll => Mathf.Max(1, maxEventsPerRoll);
    public IReadOnlyList<SituationEventPoolEntry> Entries => entries != null ? (IReadOnlyList<SituationEventPoolEntry>)entries : Array.Empty<SituationEventPoolEntry>();

    public SituationEventPoolRollResult Roll(PlayerController player, RegionInfoDefinition region = null, ActivityZoneDefinition zone = null, string sourceId = null, string sourceName = null, UnityEngine.Object context = null) {
        var result = new SituationEventPoolRollResult(this, sourceId, region, zone);
        if(player == null) {
            result.blocked = true;
            result.failureMessage = "A player is required to roll situation events.";
            PublishPoolEvent(blockedEvent, result, context, GameEventImportance.Warning);
            return result;
        }

        if(!MatchesLocation(region, zone)) {
            result.blocked = true;
            result.failureMessage = "Situation event pool location filters did not match.";
            PublishPoolEvent(blockedEvent, result, context, GameEventImportance.Trace);
            return result;
        }

        if(UnityEngine.Random.value > RollChance) {
            result.blocked = true;
            result.failureMessage = "Situation event pool roll chance failed.";
            PublishPoolEvent(blockedEvent, result, context, GameEventImportance.Trace);
            return result;
        }

        var candidates = BuildCandidates(player, region, zone, sourceId).ToList();
        if(candidates.Count == 0) {
            result.blocked = true;
            result.failureMessage = "Situation event pool had no valid candidates.";
            PublishPoolEvent(blockedEvent, result, context, GameEventImportance.Trace);
            return result;
        }

        var selectedIds = new HashSet<string>();
        for(int i = 0; i < MaxEventsPerRoll && candidates.Count > 0; i++) {
            var candidate = PickWeighted(candidates);
            if(candidate == null || candidate.entry == null || candidate.entry.Event == null) {
                break;
            }

            var startResult = candidate.entry.Event.TryStart(player, region, zone, ResolveSourceId(sourceId), ResolveSourceName(sourceName), context != null ? context : this);
            result.attemptedEvents++;
            if(startResult != null && !startResult.blocked) {
                result.startedEvents++;
                result.startedEventIds.Add(candidate.entry.Event.Id);
                if(preventDuplicateEventsPerRoll) {
                    selectedIds.Add(candidate.entry.Event.Id);
                }
            } else {
                result.blockedEvents++;
                if(startResult != null && !string.IsNullOrWhiteSpace(startResult.failureMessage)) {
                    result.messages.Add($"{candidate.entry.Event.DisplayName}: {startResult.failureMessage}");
                }
            }

            candidates.Remove(candidate);
            if(preventDuplicateEventsPerRoll && selectedIds.Count > 0) {
                candidates.RemoveAll(entry => entry.entry?.Event != null && selectedIds.Contains(entry.entry.Event.Id));
            }
        }

        PublishPoolEvent(result.startedEvents > 0 ? rolledEvent : blockedEvent, result, context, result.startedEvents > 0 ? GameEventImportance.Info : GameEventImportance.Trace);
        return result;
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

    IEnumerable<SituationEventPoolCandidate> BuildCandidates(PlayerController player, RegionInfoDefinition region, ActivityZoneDefinition zone, string sourceId) {
        foreach(var entry in Entries) {
            if(entry == null || entry.Event == null) {
                continue;
            }

            if(!entry.Event.CanStart(player, region, zone, ResolveSourceId(sourceId), out _)) {
                continue;
            }

            if(!entry.RequirementsMet(player)) {
                continue;
            }

            int weight = entry.CalculateWeight(player, region, zone);
            if(weight <= 0) {
                continue;
            }

            yield return new SituationEventPoolCandidate(entry, weight);
        }
    }

    SituationEventPoolCandidate PickWeighted(List<SituationEventPoolCandidate> candidates) {
        if(candidates == null || candidates.Count == 0) {
            return null;
        }

        if(preferHigherPriorityEvents) {
            candidates = candidates
                .OrderByDescending(candidate => candidate.entry.Event.Priority)
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

    void PublishPoolEvent(GameEventDefinition eventDefinition, SituationEventPoolRollResult result, UnityEngine.Object context, GameEventImportance importance) {
        GameEventPublishing.PublishOptional(
            eventDefinition,
            $"situation-event-pool.roll.{Id}",
            result != null && result.startedEvents > 0 ? $"{DisplayName} started {result.startedEvents} event(s)." : $"{DisplayName} did not start an event.",
            GameEventCategory.WorldEvent,
            importance,
            context != null ? context : this,
            "SituationEventPoolDefinition",
            GameEventScope.Player,
            showInFeed: showEventsInFeed,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("poolId", Id),
            GameEventPublishing.Value("poolName", DisplayName),
            GameEventPublishing.Value("sourceId", result != null ? result.sourceId : string.Empty),
            GameEventPublishing.Value("startedEvents", result != null ? result.startedEvents : 0),
            GameEventPublishing.Value("attemptedEvents", result != null ? result.attemptedEvents : 0),
            GameEventPublishing.Value("blockedEvents", result != null ? result.blockedEvents : 0),
            GameEventPublishing.Value("failureMessage", result != null ? result.failureMessage : string.Empty));
    }

    string ResolveSourceId(string sourceId) {
        return string.IsNullOrWhiteSpace(sourceId) ? $"situation-pool:{Id}" : sourceId;
    }

    string ResolveSourceName(string sourceName) {
        return string.IsNullOrWhiteSpace(sourceName) ? DisplayName : sourceName;
    }
}

[Serializable]
public class SituationEventPoolEntry {
    [Tooltip("Situation event selected by this entry.")]
    [SerializeField] SituationEventDefinition eventDefinition = null;
    [Tooltip("Base selection weight for this event. 0 falls back to the event's Base Weight.")]
    [Min(0)]
    [SerializeField] int weight;
    [Tooltip("Additional requirements checked only for this pool entry.")]
    [SerializeField] List<ActivityRequirement> extraRequirements = new List<ActivityRequirement>();
    [Tooltip("Optional weight modifiers controlled by active world conditions.")]
    [SerializeField] List<SituationEventWeightModifier> weightModifiers = new List<SituationEventWeightModifier>();

    public SituationEventDefinition Event => eventDefinition;
    public int Weight => Mathf.Max(0, weight);
    public IReadOnlyList<ActivityRequirement> ExtraRequirements => extraRequirements != null ? (IReadOnlyList<ActivityRequirement>)extraRequirements : Array.Empty<ActivityRequirement>();
    public IReadOnlyList<SituationEventWeightModifier> WeightModifiers => weightModifiers != null ? (IReadOnlyList<SituationEventWeightModifier>)weightModifiers : Array.Empty<SituationEventWeightModifier>();

    public bool RequirementsMet(PlayerController player) {
        foreach(var requirement in ExtraRequirements) {
            if(requirement != null && !requirement.IsMet(player)) {
                return false;
            }
        }

        return true;
    }

    public int CalculateWeight(PlayerController player, RegionInfoDefinition region, ActivityZoneDefinition zone) {
        int value = Weight > 0 ? Weight : eventDefinition != null ? eventDefinition.BaseWeight : 0;
        foreach(var modifier in WeightModifiers) {
            value = modifier != null ? modifier.Apply(value, player, region, zone) : value;
        }

        return Mathf.Max(0, value);
    }
}

[Serializable]
public class SituationEventWeightModifier {
    [Tooltip("World condition checked by this modifier.")]
    public WorldConditionDefinition condition;
    [Tooltip("If enabled, the condition must be active. If disabled, the modifier applies when it is inactive.")]
    public bool requireActive = true;
    [Tooltip("Flat weight added after the active/inactive check passes.")]
    public int addWeight;
    [Tooltip("Weight multiplier applied after Add Weight. 1 means no multiplier.")]
    [Min(0f)]
    public float multiplier = 1f;

    public int Apply(int currentWeight, PlayerController player, RegionInfoDefinition region, ActivityZoneDefinition zone) {
        if(condition == null) {
            return currentWeight;
        }

        bool active = player != null && (player.GetComponent<PlayerWorldConditionLog>()?.IsConditionActive(condition, null, region, zone) ?? false);
        if(active != requireActive) {
            return currentWeight;
        }

        return Mathf.RoundToInt((currentWeight + addWeight) * Mathf.Max(0f, multiplier));
    }
}

class SituationEventPoolCandidate {
    public readonly SituationEventPoolEntry entry;
    public readonly int weight;

    public SituationEventPoolCandidate(SituationEventPoolEntry entry, int weight) {
        this.entry = entry;
        this.weight = weight;
    }
}

public class SituationEventPoolRollResult {
    public readonly string poolId;
    public readonly string poolName;
    public readonly string sourceId;
    public readonly RegionInfoDefinition region;
    public readonly ActivityZoneDefinition zone;
    public int attemptedEvents;
    public int startedEvents;
    public int blockedEvents;
    public bool blocked;
    public string failureMessage;
    public readonly List<string> startedEventIds = new List<string>();
    public readonly List<string> messages = new List<string>();

    public SituationEventPoolRollResult(SituationEventPoolDefinition pool, string sourceId, RegionInfoDefinition region, ActivityZoneDefinition zone) {
        poolId = pool != null ? pool.Id : string.Empty;
        poolName = pool != null ? pool.DisplayName : string.Empty;
        this.sourceId = sourceId;
        this.region = region;
        this.zone = zone;
    }
}
