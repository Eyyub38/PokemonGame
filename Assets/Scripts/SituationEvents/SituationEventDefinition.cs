using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum SituationEventCategory {
    General,
    WildPokemon,
    Weather,
    Festival,
    Crisis,
    Resource,
    Social,
    Research,
    Law,
    Transit,
    Market,
    PokemonCare,
    Competition,
    Custom
}

public enum SituationEventPhase {
    Started,
    Resolved,
    Expired,
    Blocked
}

[CreateAssetMenu(menuName = "Situation Events/Situation Event Definition")]
public class SituationEventDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this situation event. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in debug/future UI. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note or player-facing explanation of this event.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Broad category used by filters, future UI and balancing.")]
    [SerializeField] SituationEventCategory category = SituationEventCategory.General;
    [Tooltip("Higher priority events can be sorted first by future UI or selection code.")]
    [SerializeField] int priority;
    [Tooltip("Free-form tags such as route, camp, storm, festival, police, outbreak or rare.")]
    [SerializeField] List<string> tags = new List<string>();
    [Tooltip("Optional icon used by future event/map/PokeNav UI.")]
    [SerializeField] Sprite icon = null;

    [Header("Availability")]
    [Tooltip("If enabled, the event can start without being unlocked in PlayerSituationEventLog.")]
    [SerializeField] bool unlockedByDefault = true;
    [Tooltip("If disabled, another active instance of this event blocks new starts in the same region/zone context.")]
    [SerializeField] bool allowMultipleActiveInstances;
    [Tooltip("Chance that this event starts after pool selection and all filters pass.")]
    [Range(0f, 1f)]
    [SerializeField] float startChance = 1f;
    [Tooltip("Base weight used by event pools. 0 keeps the event selectable only when an entry overrides the weight.")]
    [Min(0)]
    [SerializeField] int baseWeight = 1;

    [Header("Location Scope")]
    [Tooltip("If enabled, this event can start in any region or zone.")]
    [SerializeField] bool globalScope = true;
    [Tooltip("Specific regions where this event can start when Global Scope is disabled.")]
    [SerializeField] List<RegionInfoDefinition> allowedRegions = new List<RegionInfoDefinition>();
    [Tooltip("Region tags accepted when Global Scope is disabled.")]
    [SerializeField] List<string> allowedRegionTags = new List<string>();
    [Tooltip("Specific activity zones where this event can start when Global Scope is disabled.")]
    [SerializeField] List<ActivityZoneDefinition> allowedZones = new List<ActivityZoneDefinition>();
    [Tooltip("Activity zone tags accepted when Global Scope is disabled.")]
    [SerializeField] List<string> allowedZoneTags = new List<string>();

    [Header("Time Filters")]
    [Tooltip("If enabled, Start Day and End Day are checked.")]
    [SerializeField] bool useDayRange;
    [Tooltip("First in-game day this event can start.")]
    [Min(1)]
    [SerializeField] int startDay = 1;
    [Tooltip("Last in-game day this event can start.")]
    [Min(1)]
    [SerializeField] int endDay = 1;
    [Tooltip("Allowed weekdays. Empty accepts every weekday.")]
    [SerializeField] List<WeekDay> allowedWeekDays = new List<WeekDay>();
    [Tooltip("Allowed day periods. Empty accepts every period.")]
    [SerializeField] List<DayPeriod> allowedPeriods = new List<DayPeriod>();
    [Tooltip("Allowed exact hours. Empty accepts every hour.")]
    [SerializeField] List<int> allowedHours = new List<int>();

    [Header("World State Filters")]
    [Tooltip("Optional world event whose active state gates this situation event.")]
    [SerializeField] WorldEventDefinition requiredWorldEvent = null;
    [Tooltip("Expected active state for Required World Event.")]
    [SerializeField] bool requiredWorldEventActive = true;
    [Tooltip("Optional calendar event whose active state gates this situation event.")]
    [SerializeField] CalendarEventDefinition requiredCalendarEvent = null;
    [Tooltip("Expected active state for Required Calendar Event.")]
    [SerializeField] bool requiredCalendarEventActive = true;
    [Tooltip("World conditions that must be active before this event can start.")]
    [SerializeField] List<WorldConditionDefinition> requiredWorldConditions = new List<WorldConditionDefinition>();
    [Tooltip("World conditions that block this event while active.")]
    [SerializeField] List<WorldConditionDefinition> blockedWorldConditions = new List<WorldConditionDefinition>();

    [Header("Player Requirements")]
    [Tooltip("How additional requirements are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode requirementMatchMode = ConsequenceRequirementMatchMode.All;
    [Tooltip("Optional reusable requirements checked before this event can start.")]
    [SerializeField] List<ActivityRequirement> requirements = new List<ActivityRequirement>();

    [Header("Repeat Rules")]
    [Tooltip("How often this event can start.")]
    [SerializeField] ConsequenceChainRepeatMode repeatMode = ConsequenceChainRepeatMode.CooldownHours;
    [Tooltip("Cooldown in in-game hours when repeat mode is Cooldown Hours.")]
    [Min(0)]
    [SerializeField] int cooldownHours = 24;
    [Tooltip("Maximum successful start count. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxStartCount;
    [Tooltip("If enabled, blocked start attempts are stored in PlayerSituationEventLog.")]
    [SerializeField] bool recordBlockedAttempts;

    [Header("Active Lifetime")]
    [Tooltip("How long this event remains active. 0 means it does not create a timed active state.")]
    [Min(0)]
    [SerializeField] int durationHours = 12;
    [Tooltip("If enabled, active records are expired by PlayerSituationEventLog when their duration ends.")]
    [SerializeField] bool expireAutomatically = true;

    [Header("Start Effects")]
    [Tooltip("World conditions activated when this event starts.")]
    [SerializeField] List<SituationWorldConditionActivation> worldConditionsOnStart = new List<SituationWorldConditionActivation>();
    [Tooltip("Life Path rewards awarded when this event starts.")]
    [SerializeField] List<LifePathReward> lifePathRewardsOnStart = new List<LifePathReward>();
    [Tooltip("Consequence chains applied when this event starts.")]
    [SerializeField] List<ConsequenceChainDefinition> consequenceChainsOnStart = new List<ConsequenceChainDefinition>();

    [Header("Resolve Effects")]
    [Tooltip("Life Path rewards awarded when this event is resolved.")]
    [SerializeField] List<LifePathReward> lifePathRewardsOnResolve = new List<LifePathReward>();
    [Tooltip("Consequence chains applied when this event is resolved.")]
    [SerializeField] List<ConsequenceChainDefinition> consequenceChainsOnResolve = new List<ConsequenceChainDefinition>();

    [Header("Expire Effects")]
    [Tooltip("Life Path rewards awarded when this event expires.")]
    [SerializeField] List<LifePathReward> lifePathRewardsOnExpire = new List<LifePathReward>();
    [Tooltip("Consequence chains applied when this event expires.")]
    [SerializeField] List<ConsequenceChainDefinition> consequenceChainsOnExpire = new List<ConsequenceChainDefinition>();

    [Header("Events")]
    [Tooltip("Optional event published when this situation starts.")]
    [SerializeField] GameEventDefinition startedEvent = null;
    [Tooltip("Optional event published when this situation resolves.")]
    [SerializeField] GameEventDefinition resolvedEvent = null;
    [Tooltip("Optional event published when this situation expires.")]
    [SerializeField] GameEventDefinition expiredEvent = null;
    [Tooltip("Optional event published when this situation is blocked.")]
    [SerializeField] GameEventDefinition blockedEvent = null;
    [Tooltip("If enabled, generated situation events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed = true;
    [Tooltip("If enabled, generated situation events are written to GameDebugLogger.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public SituationEventCategory Category => category;
    public int Priority => priority;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public Sprite Icon => icon;
    public bool UnlockedByDefault => unlockedByDefault;
    public bool AllowMultipleActiveInstances => allowMultipleActiveInstances;
    public float StartChance => Mathf.Clamp01(startChance);
    public int BaseWeight => Mathf.Max(0, baseWeight);
    public bool GlobalScope => globalScope;
    public IReadOnlyList<RegionInfoDefinition> AllowedRegions => allowedRegions != null ? (IReadOnlyList<RegionInfoDefinition>)allowedRegions : Array.Empty<RegionInfoDefinition>();
    public IReadOnlyList<string> AllowedRegionTags => allowedRegionTags != null ? (IReadOnlyList<string>)allowedRegionTags : Array.Empty<string>();
    public IReadOnlyList<ActivityZoneDefinition> AllowedZones => allowedZones != null ? (IReadOnlyList<ActivityZoneDefinition>)allowedZones : Array.Empty<ActivityZoneDefinition>();
    public IReadOnlyList<string> AllowedZoneTags => allowedZoneTags != null ? (IReadOnlyList<string>)allowedZoneTags : Array.Empty<string>();
    public IReadOnlyList<ActivityRequirement> Requirements => requirements != null ? (IReadOnlyList<ActivityRequirement>)requirements : Array.Empty<ActivityRequirement>();
    public IReadOnlyList<WorldConditionDefinition> RequiredWorldConditions => requiredWorldConditions != null ? (IReadOnlyList<WorldConditionDefinition>)requiredWorldConditions : Array.Empty<WorldConditionDefinition>();
    public IReadOnlyList<WorldConditionDefinition> BlockedWorldConditions => blockedWorldConditions != null ? (IReadOnlyList<WorldConditionDefinition>)blockedWorldConditions : Array.Empty<WorldConditionDefinition>();
    public ConsequenceChainRepeatMode RepeatMode => repeatMode;
    public int CooldownHours => Mathf.Max(0, cooldownHours);
    public int MaxStartCount => Mathf.Max(0, maxStartCount);
    public bool RecordBlockedAttempts => recordBlockedAttempts;
    public int DurationHours => Mathf.Max(0, durationHours);
    public bool ExpireAutomatically => expireAutomatically;
    public IReadOnlyList<SituationWorldConditionActivation> WorldConditionsOnStart => worldConditionsOnStart != null ? (IReadOnlyList<SituationWorldConditionActivation>)worldConditionsOnStart : Array.Empty<SituationWorldConditionActivation>();
    public IReadOnlyList<LifePathReward> LifePathRewardsOnStart => lifePathRewardsOnStart != null ? (IReadOnlyList<LifePathReward>)lifePathRewardsOnStart : Array.Empty<LifePathReward>();
    public IReadOnlyList<LifePathReward> LifePathRewardsOnResolve => lifePathRewardsOnResolve != null ? (IReadOnlyList<LifePathReward>)lifePathRewardsOnResolve : Array.Empty<LifePathReward>();
    public IReadOnlyList<LifePathReward> LifePathRewardsOnExpire => lifePathRewardsOnExpire != null ? (IReadOnlyList<LifePathReward>)lifePathRewardsOnExpire : Array.Empty<LifePathReward>();
    public IReadOnlyList<ConsequenceChainDefinition> ConsequenceChainsOnStart => consequenceChainsOnStart != null ? (IReadOnlyList<ConsequenceChainDefinition>)consequenceChainsOnStart : Array.Empty<ConsequenceChainDefinition>();
    public IReadOnlyList<ConsequenceChainDefinition> ConsequenceChainsOnResolve => consequenceChainsOnResolve != null ? (IReadOnlyList<ConsequenceChainDefinition>)consequenceChainsOnResolve : Array.Empty<ConsequenceChainDefinition>();
    public IReadOnlyList<ConsequenceChainDefinition> ConsequenceChainsOnExpire => consequenceChainsOnExpire != null ? (IReadOnlyList<ConsequenceChainDefinition>)consequenceChainsOnExpire : Array.Empty<ConsequenceChainDefinition>();

    public bool CanStart(PlayerController player, RegionInfoDefinition region, ActivityZoneDefinition zone, string sourceId, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required to start situation events.";
            return false;
        }

        var log = player.GetComponent<PlayerSituationEventLog>();
        if(!unlockedByDefault && !(log?.HasUnlocked(this) ?? false)) {
            failureMessage = $"{DisplayName} is not unlocked.";
            return false;
        }

        if(!allowMultipleActiveInstances && (log?.IsActive(this, region, zone) ?? false)) {
            failureMessage = $"{DisplayName} is already active here.";
            return false;
        }

        if(!MatchesLocation(region, zone)) {
            failureMessage = "Situation event location filters did not match.";
            return false;
        }

        if(!MatchesTime(out failureMessage)) {
            return false;
        }

        if(!MatchesWorldState(player, region, zone, out failureMessage)) {
            return false;
        }

        if(!ConsequenceChainDefinition.RequirementsMet(player, requirements, requirementMatchMode, out failureMessage)) {
            return false;
        }

        if(log != null && !log.CanStart(this, sourceId, repeatMode, CooldownHours, MaxStartCount, out failureMessage)) {
            return false;
        }

        failureMessage = null;
        return true;
    }

    public SituationEventStartResult TryStart(PlayerController player, RegionInfoDefinition region = null, ActivityZoneDefinition zone = null, string sourceId = null, string sourceName = null, UnityEngine.Object context = null) {
        var result = new SituationEventStartResult(this, sourceId, region, zone);
        var log = player != null ? player.GetComponent<PlayerSituationEventLog>() ?? player.gameObject.AddComponent<PlayerSituationEventLog>() : null;
        string resolvedSourceId = ResolveSourceId(sourceId);
        string resolvedSourceName = ResolveSourceName(sourceName);

        if(!CanStart(player, region, zone, resolvedSourceId, out var failureMessage)) {
            result.blocked = true;
            result.failureMessage = failureMessage;
            if(recordBlockedAttempts) {
                log?.RecordBlocked(this, resolvedSourceId, resolvedSourceName, region, zone, failureMessage);
            }
            PublishSituationEvent(blockedEvent, SituationEventPhase.Blocked, player, result, context, GameEventImportance.Warning);
            return result;
        }

        if(UnityEngine.Random.value > StartChance) {
            result.blocked = true;
            result.failureMessage = "Situation event start chance failed.";
            if(recordBlockedAttempts) {
                log?.RecordBlocked(this, resolvedSourceId, resolvedSourceName, region, zone, result.failureMessage);
            }
            PublishSituationEvent(blockedEvent, SituationEventPhase.Blocked, player, result, context, GameEventImportance.Trace);
            return result;
        }

        result.activeState = log?.RecordStarted(this, resolvedSourceId, resolvedSourceName, region, zone, DurationHours, expireAutomatically);
        ApplyPhaseEffects(player, SituationEventPhase.Started, region, zone, resolvedSourceId, resolvedSourceName, context);
        PublishSituationEvent(startedEvent, SituationEventPhase.Started, player, result, context, GameEventImportance.Info);
        return result;
    }

    public int ResolveActive(PlayerController player, RegionInfoDefinition region = null, ActivityZoneDefinition zone = null, string sourceId = null, UnityEngine.Object context = null) {
        if(player == null) {
            return 0;
        }

        int resolved = player.GetComponent<PlayerSituationEventLog>()?.Resolve(this, sourceId, region, zone) ?? 0;
        if(resolved > 0) {
            ApplyPhaseEffects(player, SituationEventPhase.Resolved, region, zone, ResolveSourceId(sourceId), DisplayName, context);
            PublishSituationEvent(resolvedEvent, SituationEventPhase.Resolved, player, new SituationEventStartResult(this, sourceId, region, zone), context, GameEventImportance.Success);
        }

        return resolved;
    }

    public void ApplyExpiredEffects(PlayerController player, PlayerSituationEventState state, UnityEngine.Object context = null) {
        if(player == null || state == null) {
            return;
        }

        ApplyPhaseEffects(player, SituationEventPhase.Expired, state.ResolveRegion(), state.ResolveZone(), state.sourceId, state.sourceName, context);
        PublishSituationEvent(expiredEvent, SituationEventPhase.Expired, player, new SituationEventStartResult(this, state.sourceId, state.ResolveRegion(), state.ResolveZone()), context, GameEventImportance.Info);
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

    bool MatchesTime(out string failureMessage) {
        var time = TimeSystem.i;
        int day = time != null ? Mathf.Max(1, time.Day) : 1;
        int hour = time != null ? Mathf.Clamp(time.Hour, 0, 23) : 0;
        DayPeriod period = time != null ? time.GetCurrentPeriod() : DayPeriod.None;

        if(useDayRange && (day < Mathf.Max(1, startDay) || day > Mathf.Max(Mathf.Max(1, startDay), endDay))) {
            failureMessage = "Current day is outside situation event day range.";
            return false;
        }

        if(allowedWeekDays != null && allowedWeekDays.Count > 0 && !allowedWeekDays.Contains(GetWeekDay(day))) {
            failureMessage = "Current weekday is not accepted by this situation event.";
            return false;
        }

        if(allowedPeriods != null && allowedPeriods.Count > 0 && !allowedPeriods.Contains(period)) {
            failureMessage = "Current day period is not accepted by this situation event.";
            return false;
        }

        if(allowedHours != null && allowedHours.Count > 0 && !allowedHours.Contains(hour)) {
            failureMessage = "Current hour is not accepted by this situation event.";
            return false;
        }

        failureMessage = null;
        return true;
    }

    bool MatchesWorldState(PlayerController player, RegionInfoDefinition region, ActivityZoneDefinition zone, out string failureMessage) {
        if(requiredWorldEvent != null) {
            bool active = WorldEventManager.i != null && WorldEventManager.i.IsEventActive(requiredWorldEvent);
            if(active != requiredWorldEventActive) {
                failureMessage = "Required world event state did not match.";
                return false;
            }
        }

        if(requiredCalendarEvent != null && requiredCalendarEvent.IsActiveNow() != requiredCalendarEventActive) {
            failureMessage = "Required calendar event state did not match.";
            return false;
        }

        var conditionLog = player != null ? player.GetComponent<PlayerWorldConditionLog>() : null;
        foreach(var condition in RequiredWorldConditions) {
            if(condition != null && !(conditionLog?.IsConditionActive(condition, null, region, zone) ?? false)) {
                failureMessage = $"{condition.DisplayName} is required.";
                return false;
            }
        }

        foreach(var condition in BlockedWorldConditions) {
            if(condition != null && (conditionLog?.IsConditionActive(condition, null, region, zone) ?? false)) {
                failureMessage = $"{condition.DisplayName} blocks this event.";
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    void ApplyPhaseEffects(PlayerController player, SituationEventPhase phase, RegionInfoDefinition region, ActivityZoneDefinition zone, string sourceId, string sourceName, UnityEngine.Object context) {
        if(player == null) {
            return;
        }

        var lifePathRewards = phase switch {
            SituationEventPhase.Resolved => LifePathRewardsOnResolve,
            SituationEventPhase.Expired => LifePathRewardsOnExpire,
            _ => LifePathRewardsOnStart
        };

        var chains = phase switch {
            SituationEventPhase.Resolved => ConsequenceChainsOnResolve,
            SituationEventPhase.Expired => ConsequenceChainsOnExpire,
            _ => ConsequenceChainsOnStart
        };

        if(phase == SituationEventPhase.Started) {
            ApplyWorldConditionEffects(player, region, zone, sourceId, sourceName);
        }

        player.GetComponent<PlayerLifePathLog>()?.ApplyRewards(lifePathRewards, sourceId, sourceName, context != null ? context : this);

        var chainContext = new ConsequenceChainContext {
            SourceId = sourceId,
            SourceName = sourceName,
            Region = region,
            Zone = zone,
            ContextObject = context != null ? context : this
        };

        foreach(var chain in chains) {
            chain?.Apply(player, chainContext, context != null ? context : this);
        }
    }

    void ApplyWorldConditionEffects(PlayerController player, RegionInfoDefinition region, ActivityZoneDefinition zone, string sourceId, string sourceName) {
        var conditionLog = player.GetComponent<PlayerWorldConditionLog>() ?? player.gameObject.AddComponent<PlayerWorldConditionLog>();
        foreach(var activation in WorldConditionsOnStart) {
            activation?.Apply(conditionLog, sourceId, sourceName, region, zone);
        }
    }

    void PublishSituationEvent(GameEventDefinition eventDefinition, SituationEventPhase phase, PlayerController player, SituationEventStartResult result, UnityEngine.Object context, GameEventImportance importance) {
        string sourceId = result != null ? ResolveSourceId(result.sourceId) : $"situation:{Id}";
        GameEventPublishing.PublishOptional(
            eventDefinition,
            $"situation-event.{phase.ToString().ToLowerInvariant()}.{Id}",
            phase == SituationEventPhase.Started ? $"{DisplayName} started." : $"{DisplayName} {phase.ToString().ToLowerInvariant()}.",
            GameEventCategory.WorldEvent,
            importance,
            context != null ? context : player != null ? (UnityEngine.Object)player : this,
            "SituationEventDefinition",
            GameEventScope.Player,
            showInFeed: showEventsInFeed,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("situationEventId", Id),
            GameEventPublishing.Value("situationEventName", DisplayName),
            GameEventPublishing.Value("category", category),
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("sourceId", sourceId),
            GameEventPublishing.Value("regionId", result?.region != null ? result.region.Id : string.Empty),
            GameEventPublishing.Value("zoneId", result?.zone != null ? result.zone.Id : string.Empty),
            GameEventPublishing.Value("blocked", result != null && result.blocked),
            GameEventPublishing.Value("failureMessage", result != null ? result.failureMessage : string.Empty));
    }

    string ResolveSourceId(string sourceId) {
        return string.IsNullOrWhiteSpace(sourceId) ? $"situation:{Id}" : sourceId;
    }

    string ResolveSourceName(string sourceName) {
        return string.IsNullOrWhiteSpace(sourceName) ? DisplayName : sourceName;
    }

    WeekDay GetWeekDay(int day) {
        int index = Mathf.Abs(Mathf.Max(1, day) - 1) % 7;
        return (WeekDay)index;
    }
}

[Serializable]
public class SituationWorldConditionActivation {
    [Tooltip("World condition activated by this situation event.")]
    public WorldConditionDefinition condition;
    [Tooltip("Duration override in in-game hours. -1 uses condition default, 0 means no automatic expiry.")]
    [Min(-1)]
    public int durationOverrideHours = -1;
    [Tooltip("Intensity of this condition instance. 1 uses definition values as-is.")]
    [Min(0f)]
    public float intensity = 1f;
    [Tooltip("Stacks added by this activation.")]
    [Min(1)]
    public int stacks = 1;
    [Tooltip("If enabled, an existing matching condition refreshes its timer/intensity.")]
    public bool refreshExisting = true;
    [Tooltip("If enabled, an existing matching condition gains stacks.")]
    public bool stackExisting;

    public void Apply(PlayerWorldConditionLog log, string sourceId, string sourceName, RegionInfoDefinition region, ActivityZoneDefinition zone) {
        if(log == null || condition == null) {
            return;
        }

        log.ActivateCondition(condition, sourceId, sourceName, region, zone, durationOverrideHours, intensity, stacks, refreshExisting, stackExisting);
    }
}

public class SituationEventStartResult {
    public readonly string eventId;
    public readonly string eventName;
    public readonly string sourceId;
    public readonly RegionInfoDefinition region;
    public readonly ActivityZoneDefinition zone;
    public PlayerSituationEventState activeState;
    public bool blocked;
    public string failureMessage;

    public SituationEventStartResult(SituationEventDefinition definition, string sourceId, RegionInfoDefinition region, ActivityZoneDefinition zone) {
        eventId = definition != null ? definition.Id : string.Empty;
        eventName = definition != null ? definition.DisplayName : string.Empty;
        this.sourceId = sourceId;
        this.region = region;
        this.zone = zone;
    }
}
