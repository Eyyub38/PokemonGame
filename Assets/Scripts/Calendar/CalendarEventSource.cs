using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum CalendarEventSourceType {
    PokeNav,
    NoticeBoard,
    NPC,
    Stadium,
    ContestHall,
    Shop,
    PoliceStation,
    ResearchLab,
    TransitStation,
    Club,
    Custom
}

public enum CalendarEventRevealMode {
    FirstAvailable,
    RandomAvailable,
    AllAvailable
}

public class CalendarEventSource : MonoBehaviour, IPlayerTriggerable {
    [Header("Source")]
    [Tooltip("Optional source id used by save/reveal logs. Empty uses GameObject name.")]
    [SerializeField] string sourceId;
    [Tooltip("Name shown in debug/future UI. Empty uses GameObject name.")]
    [SerializeField] string displayName;
    [Tooltip("Broad source type used by filters and future UI.")]
    [SerializeField] CalendarEventSourceType sourceType = CalendarEventSourceType.PokeNav;
    [Tooltip("Events this source can reveal.")]
    [SerializeField] List<CalendarEventDefinition> events = new List<CalendarEventDefinition>();

    [Header("Reveal")]
    [Tooltip("How this source chooses events when triggered.")]
    [SerializeField] CalendarEventRevealMode revealMode = CalendarEventRevealMode.AllAvailable;
    [Tooltip("If enabled, this source unlocks listed events when the player triggers it.")]
    [SerializeField] bool unlockEventsOnTrigger = true;
    [Tooltip("If enabled, this source immediately reveals event info when the player triggers it.")]
    [SerializeField] bool revealOnPlayerTrigger = true;
    [Tooltip("If enabled, repeated player triggers can call OnPlayerTriggered more than once.")]
    [SerializeField] bool triggerRepeatedly = true;

    [Header("Access")]
    [Tooltip("Optional title, badge, permit or license required before this source can be used.")]
    [SerializeField] TitleDefinition requiredTitle;
    [Tooltip("Optional faction whose reputation gates this source.")]
    [SerializeField] ReputationFactionDefinition requiredFaction;
    [Tooltip("Minimum required reputation with the selected faction.")]
    [SerializeField] int requiredReputation;
    [Tooltip("Message shown when source access is blocked.")]
    [SerializeField] string lockedMessage = "This calendar source is not available right now.";

    [Header("Debug")]
    [Tooltip("If enabled, reveal attempts are written to GameEventBus/GameDebugLogger.")]
    [SerializeField] bool logRevealAttempts;

    public string SourceId => string.IsNullOrWhiteSpace(sourceId) ? name : sourceId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public CalendarEventSourceType SourceType => sourceType;
    public IReadOnlyList<CalendarEventDefinition> Events => events;
    public bool TriggerRepeatedly => triggerRepeatedly;

    public void OnPlayerTriggered(PlayerController player) {
        if(player == null) {
            return;
        }

        if(!CanUse(player, out var failureMessage)) {
            PublishSourceEvent(player, "blocked", failureMessage, GameEventImportance.Warning);
            return;
        }

        var log = player.GetComponent<PlayerCalendarLog>() ?? player.gameObject.AddComponent<PlayerCalendarLog>();
        if(unlockEventsOnTrigger) {
            foreach(var calendarEvent in events) {
                log.UnlockEvent(calendarEvent, SourceId);
            }
        }

        if(revealOnPlayerTrigger) {
            TryReveal(player, out _);
        }
    }

    public bool CanUse(PlayerController player, out string failureMessage) {
        if(requiredTitle != null && !(player?.GetComponent<PlayerTitles>()?.HasTitle(requiredTitle) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need {requiredTitle.DisplayName}." : lockedMessage;
            return false;
        }

        if(requiredFaction != null) {
            int reputation = player?.GetComponent<PlayerReputation>()?.GetReputation(requiredFaction) ?? 0;
            if(reputation < requiredReputation) {
                failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need more reputation with {requiredFaction.DisplayName}." : lockedMessage;
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    public List<CalendarEventDefinition> GetVisibleEvents(PlayerController player) {
        if(player == null || !CanUse(player, out _)) {
            return new List<CalendarEventDefinition>();
        }

        var log = player.GetComponent<PlayerCalendarLog>();
        return (events ?? new List<CalendarEventDefinition>())
            .Where(calendarEvent => calendarEvent != null && calendarEvent.CanShow(player, log, out _))
            .OrderByDescending(calendarEvent => calendarEvent.Important)
            .ThenByDescending(calendarEvent => calendarEvent.Priority)
            .ThenBy(calendarEvent => calendarEvent.Title)
            .ToList();
    }

    public bool TryReveal(PlayerController player, out List<CalendarEventDefinition> revealedEvents) {
        revealedEvents = new List<CalendarEventDefinition>();
        if(player == null) {
            PublishSourceEvent(null, "blocked", "A player is required to reveal calendar events.", GameEventImportance.Warning);
            return false;
        }

        if(!CanUse(player, out var failureMessage)) {
            PublishSourceEvent(player, "blocked", failureMessage, GameEventImportance.Warning);
            return false;
        }

        var available = GetVisibleEvents(player);
        if(available.Count == 0) {
            PublishSourceEvent(player, "empty", $"{DisplayName} has no visible calendar events.", GameEventImportance.Trace);
            return false;
        }

        if(revealMode == CalendarEventRevealMode.AllAvailable) {
            revealedEvents.AddRange(available);
        } else if(revealMode == CalendarEventRevealMode.FirstAvailable) {
            revealedEvents.Add(available[0]);
        } else {
            revealedEvents.Add(available[Random.Range(0, available.Count)]);
        }

        foreach(var calendarEvent in revealedEvents) {
            calendarEvent.Reveal(player, SourceId, DisplayName);
        }

        PublishSourceEvent(player, "revealed", $"{DisplayName} revealed {revealedEvents.Count} event(s).", GameEventImportance.Info);
        return true;
    }

    public bool TryReveal(PlayerController player, CalendarEventDefinition calendarEvent, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required to reveal calendar events.";
            return false;
        }

        if(!CanUse(player, out failureMessage)) {
            return false;
        }

        if(calendarEvent == null || !events.Contains(calendarEvent)) {
            failureMessage = "This event is not available from this source.";
            return false;
        }

        var log = player.GetComponent<PlayerCalendarLog>() ?? player.gameObject.AddComponent<PlayerCalendarLog>();
        if(!calendarEvent.CanShow(player, log, out failureMessage)) {
            return false;
        }

        calendarEvent.Reveal(player, SourceId, DisplayName);
        PublishSourceEvent(player, "revealed", $"{DisplayName} revealed {calendarEvent.Title}.", GameEventImportance.Info);
        failureMessage = null;
        return true;
    }

    void PublishSourceEvent(PlayerController player, string phase, string message, GameEventImportance importance) {
        if(!logRevealAttempts && importance < GameEventImportance.Warning) {
            return;
        }

        GameEventPublishing.PublishOptional(
            null,
            $"calendar-source.{phase}.{SourceId}",
            message,
            GameEventCategory.Calendar,
            importance,
            player != null ? player : this,
            "CalendarEventSource",
            GameEventScope.Player,
            showInFeed: false,
            writeToDebugLog: logRevealAttempts,
            GameEventPublishing.Value("sourceId", SourceId),
            GameEventPublishing.Value("sourceName", DisplayName),
            GameEventPublishing.Value("sourceType", sourceType),
            GameEventPublishing.Value("phase", phase));
    }
}
