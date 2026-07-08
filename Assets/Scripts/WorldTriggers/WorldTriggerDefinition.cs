using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum WorldTriggerKind {
    Manual,
    GameEvent,
    TimeChanged,
    DayChanged
}

public enum WorldTriggerEventValueMode {
    Exists,
    Missing,
    Equals,
    NotEquals,
    Contains,
    GreaterOrEqual,
    LessOrEqual
}

[CreateAssetMenu(menuName = "World Triggers/World Trigger Definition")]
public class WorldTriggerDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this world trigger. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in debug/future UI. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note explaining when and why this trigger runs.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Free-form tags used by validators, requirements and future UI filters.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Trigger")]
    [Tooltip("What kind of signal can start this trigger.")]
    [SerializeField] WorldTriggerKind triggerKind = WorldTriggerKind.GameEvent;
    [Tooltip("Chance that this trigger runs after all filters pass.")]
    [Range(0f, 1f)]
    [SerializeField] float triggerChance = 1f;
    [Tooltip("If enabled, this trigger requires a player and records player-specific history.")]
    [SerializeField] bool requiresPlayer = true;

    [Header("Repeat Rules")]
    [Tooltip("How often this trigger can run.")]
    [SerializeField] ConsequenceChainRepeatMode repeatMode = ConsequenceChainRepeatMode.Unlimited;
    [Tooltip("Cooldown in in-game hours when repeat mode is Cooldown Hours.")]
    [Min(0)]
    [SerializeField] int cooldownHours;
    [Tooltip("Maximum successful trigger count. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxTriggerCount;
    [Tooltip("If enabled, successful trigger attempts are stored in PlayerWorldTriggerLog.")]
    [SerializeField] bool recordHistory = true;
    [Tooltip("If enabled, blocked trigger attempts are also stored in PlayerWorldTriggerLog.")]
    [SerializeField] bool recordBlockedAttempts;

    [Header("Game Event Filters")]
    [Tooltip("Optional exact event definition required for Game Event triggers.")]
    [SerializeField] GameEventDefinition eventDefinition = null;
    [Tooltip("Optional exact event id required for Game Event triggers. Empty disables this filter.")]
    [SerializeField] string eventId = string.Empty;
    [Tooltip("Optional accepted event ids. Empty accepts any id that passes other filters.")]
    [SerializeField] List<string> acceptedEventIds = new List<string>();
    [Tooltip("Optional accepted categories. Empty accepts any category.")]
    [SerializeField] List<GameEventCategory> acceptedCategories = new List<GameEventCategory>();
    [Tooltip("Minimum accepted event importance.")]
    [SerializeField] GameEventImportance minimumImportance = GameEventImportance.Trace;
    [Tooltip("If disabled, events hidden from feed are ignored.")]
    [SerializeField] bool includeHiddenFeedEvents = true;
    [Tooltip("Optional source string required from the published game event.")]
    [SerializeField] string requiredEventSource = string.Empty;
    [Tooltip("Optional context object name required from the published game event.")]
    [SerializeField] string requiredEventContextName = string.Empty;
    [Tooltip("Optional event value filters checked against GameEventRecord values.")]
    [SerializeField] List<WorldTriggerEventValueFilter> eventValueFilters = new List<WorldTriggerEventValueFilter>();

    [Header("Time Filters")]
    [Tooltip("If enabled, Start Day and End Day are checked.")]
    [SerializeField] bool useDayRange;
    [Tooltip("First in-game day this trigger can run.")]
    [Min(1)]
    [SerializeField] int startDay = 1;
    [Tooltip("Last in-game day this trigger can run.")]
    [Min(1)]
    [SerializeField] int endDay = 1;
    [Tooltip("Allowed weekdays. Empty accepts every weekday.")]
    [SerializeField] List<WeekDay> allowedWeekDays = new List<WeekDay>();
    [Tooltip("Allowed day periods. Empty accepts every period.")]
    [SerializeField] List<DayPeriod> allowedPeriods = new List<DayPeriod>();
    [Tooltip("Allowed exact hours. Empty accepts every hour.")]
    [SerializeField] List<int> allowedHours = new List<int>();

    [Header("World Filters")]
    [Tooltip("Optional world event whose active state gates this trigger.")]
    [SerializeField] WorldEventDefinition requiredWorldEvent = null;
    [Tooltip("Expected active state for Required World Event.")]
    [SerializeField] bool requiredWorldEventActive = true;
    [Tooltip("Optional calendar event whose active state gates this trigger.")]
    [SerializeField] CalendarEventDefinition requiredCalendarEvent = null;
    [Tooltip("Expected active state for Required Calendar Event.")]
    [SerializeField] bool requiredCalendarEventActive = true;

    [Header("Player Requirements")]
    [Tooltip("How player requirements are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode requirementMatchMode = ConsequenceRequirementMatchMode.All;
    [Tooltip("Optional reusable requirements checked before this trigger can run.")]
    [SerializeField] List<ActivityRequirement> requirements = new List<ActivityRequirement>();

    [Header("Consequences")]
    [Tooltip("Consequence chains applied when this trigger runs.")]
    [SerializeField] List<ConsequenceChainDefinition> consequenceChains = new List<ConsequenceChainDefinition>();

    [Header("Context Overrides")]
    [Tooltip("Optional source id override passed to history and consequence chains. Empty uses the runtime source/event id.")]
    [SerializeField] string sourceIdOverride = string.Empty;
    [Tooltip("Optional source name override passed to consequence chains.")]
    [SerializeField] string sourceNameOverride = string.Empty;
    [Tooltip("Optional reporter id passed to risk/law steps.")]
    [SerializeField] string reporterIdOverride = string.Empty;
    [Tooltip("Optional region passed to consequence chains.")]
    [SerializeField] RegionInfoDefinition regionOverride = null;
    [Tooltip("Optional activity zone passed to consequence chains.")]
    [SerializeField] ActivityZoneDefinition zoneOverride = null;
    [Tooltip("Optional rumor source passed to rumor consequence steps.")]
    [SerializeField] RumorSource rumorSourceOverride = null;
    [Tooltip("Optional authority faction passed to risk/law consequence steps.")]
    [SerializeField] ReputationFactionDefinition authorityFactionOverride = null;
    [Tooltip("Optional authority id override. Empty uses Authority Faction or no authority override.")]
    [SerializeField] string authorityIdOverride = string.Empty;
    [Tooltip("Optional authority display name override.")]
    [SerializeField] string authorityNameOverride = string.Empty;

    [Header("Events")]
    [Tooltip("Optional event published when this trigger runs. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition triggeredEvent = null;
    [Tooltip("Optional event published when this trigger is blocked. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition blockedEvent = null;
    [Tooltip("If enabled, trigger events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed;
    [Tooltip("If enabled, trigger events are written to GameDebugLogger.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public WorldTriggerKind TriggerKind => triggerKind;
    public float TriggerChance => Mathf.Clamp01(triggerChance);
    public bool RequiresPlayer => requiresPlayer;
    public IReadOnlyList<WorldTriggerEventValueFilter> EventValueFilters => eventValueFilters != null ? (IReadOnlyList<WorldTriggerEventValueFilter>)eventValueFilters : Array.Empty<WorldTriggerEventValueFilter>();
    public IReadOnlyList<ActivityRequirement> Requirements => requirements != null ? (IReadOnlyList<ActivityRequirement>)requirements : Array.Empty<ActivityRequirement>();
    public IReadOnlyList<ConsequenceChainDefinition> ConsequenceChains => consequenceChains != null ? (IReadOnlyList<ConsequenceChainDefinition>)consequenceChains : Array.Empty<ConsequenceChainDefinition>();

    public bool CanTrigger(PlayerController player, WorldTriggerKind kind, GameEventRecord record, string sourceId, out string failureMessage) {
        if(kind != triggerKind) {
            failureMessage = "Trigger kind does not match.";
            return false;
        }

        if(requiresPlayer && player == null) {
            failureMessage = "A player is required for this world trigger.";
            return false;
        }

        if(triggerKind == WorldTriggerKind.GameEvent && !MatchesGameEvent(record, out failureMessage)) {
            return false;
        }

        if(!MatchesTime(out failureMessage)) {
            return false;
        }

        if(!MatchesWorldState(out failureMessage)) {
            return false;
        }

        if(requiresPlayer && !ConsequenceChainDefinition.RequirementsMet(player, requirements, requirementMatchMode, out failureMessage)) {
            return false;
        }

        var log = player != null ? player.GetComponent<PlayerWorldTriggerLog>() : null;
        if(log != null && !log.CanRun(this, ResolveSourceId(sourceId, record), repeatMode, cooldownHours, maxTriggerCount, out failureMessage)) {
            return false;
        }

        failureMessage = null;
        return true;
    }

    public WorldTriggerRunResult Apply(PlayerController player, WorldTriggerKind kind, GameEventRecord record = null, string sourceId = null, string sourceName = null, UnityEngine.Object unityContext = null) {
        string resolvedSourceId = ResolveSourceId(sourceId, record);
        string resolvedSourceName = ResolveSourceName(sourceName, record);
        var result = new WorldTriggerRunResult(Id, DisplayName, kind, resolvedSourceId);
        var log = player != null ? player.GetComponent<PlayerWorldTriggerLog>() ?? player.gameObject.AddComponent<PlayerWorldTriggerLog>() : null;

        if(!CanTrigger(player, kind, record, resolvedSourceId, out var failureMessage)) {
            result.blocked = true;
            result.failureMessage = failureMessage;
            if(recordHistory && recordBlockedAttempts) {
                log?.RecordRun(this, resolvedSourceId, result);
            }
            PublishTriggerEvent(blockedEvent, "blocked", result, player, record, unityContext, GameEventImportance.Warning);
            return result;
        }

        if(UnityEngine.Random.value > TriggerChance) {
            result.blocked = true;
            result.failureMessage = "Trigger chance roll failed.";
            if(recordHistory && recordBlockedAttempts) {
                log?.RecordRun(this, resolvedSourceId, result);
            }
            PublishTriggerEvent(blockedEvent, "chance-skipped", result, player, record, unityContext, GameEventImportance.Trace);
            return result;
        }

        PublishTriggerEvent(triggeredEvent, "triggered", result, player, record, unityContext, GameEventImportance.Info);
        var context = BuildConsequenceContext(resolvedSourceId, resolvedSourceName, record, unityContext);
        foreach(var chain in ConsequenceChains) {
            if(chain == null) {
                result.skippedChains++;
                continue;
            }

            var chainResult = chain.Apply(player, context, unityContext != null ? unityContext : this);
            if(chainResult != null && !chainResult.blocked) {
                result.appliedChains++;
            } else {
                result.blockedChains++;
                if(chainResult != null && !string.IsNullOrWhiteSpace(chainResult.failureMessage)) {
                    result.messages.Add($"{chain.DisplayName}: {chainResult.failureMessage}");
                }
            }
        }

        if(recordHistory) {
            log?.RecordRun(this, resolvedSourceId, result);
        }

        return result;
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    bool MatchesGameEvent(GameEventRecord record, out string failureMessage) {
        if(record == null) {
            failureMessage = "A game event record is required.";
            return false;
        }

        if(eventDefinition != null && record.id != eventDefinition.Id) {
            failureMessage = "Game event definition filter did not match.";
            return false;
        }

        if(!string.IsNullOrWhiteSpace(eventId) && record.id != eventId) {
            failureMessage = "Game event id filter did not match.";
            return false;
        }

        if(acceptedEventIds != null && acceptedEventIds.Count > 0 && !acceptedEventIds.Contains(record.id)) {
            failureMessage = "Game event id was not in accepted ids.";
            return false;
        }

        if(acceptedCategories != null && acceptedCategories.Count > 0 && !acceptedCategories.Contains(record.category)) {
            failureMessage = "Game event category was not accepted.";
            return false;
        }

        if(record.importance < minimumImportance) {
            failureMessage = "Game event importance is too low.";
            return false;
        }

        if(!includeHiddenFeedEvents && !record.showInFeed) {
            failureMessage = "Hidden feed events are ignored.";
            return false;
        }

        if(!string.IsNullOrWhiteSpace(requiredEventSource) && record.source != requiredEventSource) {
            failureMessage = "Game event source did not match.";
            return false;
        }

        if(!string.IsNullOrWhiteSpace(requiredEventContextName) && record.contextName != requiredEventContextName) {
            failureMessage = "Game event context name did not match.";
            return false;
        }

        foreach(var filter in EventValueFilters) {
            if(filter != null && !filter.IsMet(record)) {
                failureMessage = $"Game event value filter failed: {filter.Key}";
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    bool MatchesTime(out string failureMessage) {
        var time = TimeSystem.i;
        int day = time != null ? Mathf.Max(1, time.Day) : 1;
        int hour = time != null ? Mathf.Clamp(time.Hour, 0, 23) : 0;
        DayPeriod period = time != null ? time.GetCurrentPeriod() : DayPeriod.None;

        if(useDayRange && (day < Mathf.Max(1, startDay) || day > Mathf.Max(Mathf.Max(1, startDay), endDay))) {
            failureMessage = "Current day is outside trigger day range.";
            return false;
        }

        if(allowedWeekDays != null && allowedWeekDays.Count > 0 && !allowedWeekDays.Contains(GetWeekDay(day))) {
            failureMessage = "Current weekday is not accepted.";
            return false;
        }

        if(allowedPeriods != null && allowedPeriods.Count > 0 && !allowedPeriods.Contains(period)) {
            failureMessage = "Current day period is not accepted.";
            return false;
        }

        if(allowedHours != null && allowedHours.Count > 0 && !allowedHours.Contains(hour)) {
            failureMessage = "Current hour is not accepted.";
            return false;
        }

        failureMessage = null;
        return true;
    }

    bool MatchesWorldState(out string failureMessage) {
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

        failureMessage = null;
        return true;
    }

    ConsequenceChainContext BuildConsequenceContext(string resolvedSourceId, string resolvedSourceName, GameEventRecord record, UnityEngine.Object unityContext) {
        string resolvedAuthorityId = ResolveAuthorityId();
        return new ConsequenceChainContext {
            SourceId = resolvedSourceId,
            SourceName = resolvedSourceName,
            ReporterId = !string.IsNullOrWhiteSpace(reporterIdOverride) ? reporterIdOverride : record != null ? record.source : string.Empty,
            Region = regionOverride,
            Zone = zoneOverride,
            RumorSource = rumorSourceOverride,
            AuthorityId = resolvedAuthorityId,
            AuthorityName = ResolveAuthorityName(resolvedAuthorityId),
            ContextObject = unityContext != null ? unityContext : this
        };
    }

    string ResolveSourceId(string sourceId, GameEventRecord record) {
        if(!string.IsNullOrWhiteSpace(sourceIdOverride)) {
            return sourceIdOverride;
        }

        if(!string.IsNullOrWhiteSpace(sourceId)) {
            return sourceId;
        }

        return record != null && !string.IsNullOrWhiteSpace(record.id) ? record.id : $"world-trigger:{Id}";
    }

    string ResolveSourceName(string sourceName, GameEventRecord record) {
        if(!string.IsNullOrWhiteSpace(sourceNameOverride)) {
            return sourceNameOverride;
        }

        if(!string.IsNullOrWhiteSpace(sourceName)) {
            return sourceName;
        }

        return record != null && !string.IsNullOrWhiteSpace(record.displayName) ? record.displayName : DisplayName;
    }

    string ResolveAuthorityId() {
        if(authorityFactionOverride != null) {
            return authorityFactionOverride.Id;
        }

        return authorityIdOverride;
    }

    string ResolveAuthorityName(string authorityId) {
        if(authorityFactionOverride != null) {
            return authorityFactionOverride.DisplayName;
        }

        return !string.IsNullOrWhiteSpace(authorityNameOverride) ? authorityNameOverride : authorityId;
    }

    WeekDay GetWeekDay(int day) {
        int index = Mathf.Abs(Mathf.Max(1, day) - 1) % 7;
        return (WeekDay)index;
    }

    void PublishTriggerEvent(GameEventDefinition eventDefinition, string phase, WorldTriggerRunResult result, PlayerController player, GameEventRecord record, UnityEngine.Object unityContext, GameEventImportance importance) {
        GameEventPublishing.PublishOptional(
            eventDefinition,
            $"world-trigger.{phase}.{Id}",
            phase == "triggered" ? $"{DisplayName} triggered." : $"{DisplayName} {phase}.",
            GameEventCategory.WorldTrigger,
            importance,
            unityContext != null ? unityContext : player != null ? player : this,
            "WorldTriggerDefinition",
            GameEventScope.Player,
            showInFeed: showEventsInFeed,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("triggerId", Id),
            GameEventPublishing.Value("triggerName", DisplayName),
            GameEventPublishing.Value("triggerKind", triggerKind),
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("sourceId", result != null ? result.sourceId : string.Empty),
            GameEventPublishing.Value("eventId", record != null ? record.id : string.Empty),
            GameEventPublishing.Value("appliedChains", result != null ? result.appliedChains : 0),
            GameEventPublishing.Value("blockedChains", result != null ? result.blockedChains : 0),
            GameEventPublishing.Value("blocked", result != null && result.blocked));
    }
}

[Serializable]
public class WorldTriggerEventValueFilter {
    [Tooltip("GameEventRecord value key checked by this filter.")]
    [SerializeField] string key = string.Empty;
    [Tooltip("How the event value is compared.")]
    [SerializeField] WorldTriggerEventValueMode mode = WorldTriggerEventValueMode.Equals;
    [Tooltip("Expected value used by comparison modes.")]
    [SerializeField] string expectedValue = string.Empty;

    public string Key => key;
    public WorldTriggerEventValueMode Mode => mode;
    public string ExpectedValue => expectedValue;

    public bool IsMet(GameEventRecord record) {
        if(record == null || string.IsNullOrWhiteSpace(key)) {
            return false;
        }

        string value = record.GetValue(key);
        bool exists = value != null;
        switch(mode) {
            case WorldTriggerEventValueMode.Exists:
                return exists;
            case WorldTriggerEventValueMode.Missing:
                return !exists;
            case WorldTriggerEventValueMode.NotEquals:
                return !string.Equals(value, expectedValue, StringComparison.OrdinalIgnoreCase);
            case WorldTriggerEventValueMode.Contains:
                return exists && value.IndexOf(expectedValue ?? string.Empty, StringComparison.OrdinalIgnoreCase) >= 0;
            case WorldTriggerEventValueMode.GreaterOrEqual:
                return TryParse(value, out var greaterValue) && TryParse(expectedValue, out var greaterExpected) && greaterValue >= greaterExpected;
            case WorldTriggerEventValueMode.LessOrEqual:
                return TryParse(value, out var lesserValue) && TryParse(expectedValue, out var lesserExpected) && lesserValue <= lesserExpected;
            default:
                return string.Equals(value, expectedValue, StringComparison.OrdinalIgnoreCase);
        }
    }

    bool TryParse(string value, out float number) {
        return float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out number)
            || float.TryParse(value, out number);
    }
}

public class WorldTriggerRunResult {
    public readonly string triggerId;
    public readonly string triggerName;
    public readonly WorldTriggerKind triggerKind;
    public readonly string sourceId;
    public int appliedChains;
    public int blockedChains;
    public int skippedChains;
    public bool blocked;
    public string failureMessage;
    public readonly List<string> messages = new List<string>();

    public WorldTriggerRunResult(string triggerId, string triggerName, WorldTriggerKind triggerKind, string sourceId) {
        this.triggerId = triggerId;
        this.triggerName = triggerName;
        this.triggerKind = triggerKind;
        this.sourceId = sourceId;
    }
}
