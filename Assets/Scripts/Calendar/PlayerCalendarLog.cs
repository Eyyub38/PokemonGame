using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerCalendarLog : MonoBehaviour, ISavable {
    [Tooltip("Runtime/save ids for calendar events unlocked for the player.")]
    [SerializeField] List<string> unlockedEventIds = new List<string>();
    [Tooltip("Runtime/save ids for calendar events dismissed in future UI.")]
    [SerializeField] List<string> dismissedEventIds = new List<string>();
    [Tooltip("Runtime/save history of seen/completed calendar events.")]
    [SerializeField] List<PlayerCalendarEventState> eventHistory = new List<PlayerCalendarEventState>();

    public IReadOnlyList<string> UnlockedEventIds => unlockedEventIds;
    public IReadOnlyList<string> DismissedEventIds => dismissedEventIds;
    public IReadOnlyList<PlayerCalendarEventState> EventHistory => eventHistory;
    public event Action<CalendarEventDefinition> OnEventUnlocked;
    public event Action<CalendarEventDefinition> OnEventSeen;
    public event Action<CalendarEventDefinition> OnEventCompleted;
    public event Action OnCalendarChanged;

    public bool HasUnlockedEvent(CalendarEventDefinition calendarEvent) {
        return calendarEvent != null && (calendarEvent.UnlockedByDefault || HasUnlockedEvent(calendarEvent.Id));
    }

    public bool HasUnlockedEvent(string eventId) {
        return !string.IsNullOrWhiteSpace(eventId) && unlockedEventIds.Contains(eventId);
    }

    public bool UnlockEvent(CalendarEventDefinition calendarEvent, string source = null) {
        if(calendarEvent == null || HasUnlockedEvent(calendarEvent.Id)) {
            return false;
        }

        unlockedEventIds.Add(calendarEvent.Id);
        OnEventUnlocked?.Invoke(calendarEvent);
        OnCalendarChanged?.Invoke();
        PublishCalendarLogEvent("unlocked", calendarEvent.Id, calendarEvent.Title, source, GameEventImportance.Success);
        return true;
    }

    public void MarkSeen(CalendarEventDefinition calendarEvent, string sourceId = null, string sourceName = null) {
        if(calendarEvent == null) {
            return;
        }

        var state = GetOrCreateState(calendarEvent);
        if(state.seenCount == 0) {
            state.firstSeenDay = GetCurrentDay();
            state.firstSeenAbsoluteHour = GetCurrentAbsoluteHour();
        }

        state.seenCount++;
        state.lastSeenDay = GetCurrentDay();
        state.lastSeenAbsoluteHour = GetCurrentAbsoluteHour();
        state.lastSourceId = sourceId;
        state.lastSourceName = sourceName;
        OnEventSeen?.Invoke(calendarEvent);
        OnCalendarChanged?.Invoke();
    }

    public bool CompleteEvent(CalendarEventDefinition calendarEvent, string sourceId = null) {
        if(calendarEvent == null) {
            return false;
        }

        var state = GetOrCreateState(calendarEvent);
        state.completedCount++;
        state.lastCompletedDay = GetCurrentDay();
        state.lastCompletedAbsoluteHour = GetCurrentAbsoluteHour();
        OnEventCompleted?.Invoke(calendarEvent);
        OnCalendarChanged?.Invoke();
        calendarEvent.PublishCompleted(GetComponent<PlayerController>(), sourceId);
        return true;
    }

    public bool HasSeenEvent(CalendarEventDefinition calendarEvent) {
        return calendarEvent != null && GetSeenCount(calendarEvent) > 0;
    }

    public int GetSeenCount(CalendarEventDefinition calendarEvent) {
        var state = calendarEvent != null ? GetState(calendarEvent.Id) : null;
        return state != null ? Mathf.Max(0, state.seenCount) : 0;
    }

    public bool HasCompletedEvent(CalendarEventDefinition calendarEvent) {
        return calendarEvent != null && GetCompletedCount(calendarEvent) > 0;
    }

    public int GetCompletedCount(CalendarEventDefinition calendarEvent) {
        var state = calendarEvent != null ? GetState(calendarEvent.Id) : null;
        return state != null ? Mathf.Max(0, state.completedCount) : 0;
    }

    public int GetSeenCountWithTag(string tag) {
        if(string.IsNullOrWhiteSpace(tag)) {
            return 0;
        }

        int count = 0;
        foreach(var state in eventHistory) {
            if(state == null || state.seenCount <= 0) {
                continue;
            }

            var calendarEvent = ResolveEvent(state.eventId);
            if(calendarEvent != null && calendarEvent.HasTag(tag)) {
                count += state.seenCount;
            }
        }

        return count;
    }

    public int GetSeenCountByCategory(CalendarEventCategory category) {
        int count = 0;
        foreach(var state in eventHistory) {
            if(state == null || state.seenCount <= 0) {
                continue;
            }

            var calendarEvent = ResolveEvent(state.eventId);
            if(calendarEvent != null && calendarEvent.Category == category) {
                count += state.seenCount;
            }
        }

        return count;
    }

    public bool IsDismissed(CalendarEventDefinition calendarEvent) {
        return calendarEvent != null && dismissedEventIds.Contains(calendarEvent.Id);
    }

    public void SetDismissed(CalendarEventDefinition calendarEvent, bool dismissed = true) {
        if(calendarEvent == null) {
            return;
        }

        if(dismissed && !dismissedEventIds.Contains(calendarEvent.Id)) {
            dismissedEventIds.Add(calendarEvent.Id);
        } else if(!dismissed) {
            dismissedEventIds.Remove(calendarEvent.Id);
        }

        OnCalendarChanged?.Invoke();
    }

    public List<CalendarEventDefinition> GetVisibleEvents(IEnumerable<CalendarEventDefinition> events, bool includeDismissed = false) {
        var player = GetComponent<PlayerController>();
        return (events ?? Enumerable.Empty<CalendarEventDefinition>())
            .Where(calendarEvent => calendarEvent != null && calendarEvent.CanShow(player, this, out _))
            .Where(calendarEvent => includeDismissed || !IsDismissed(calendarEvent))
            .OrderByDescending(calendarEvent => calendarEvent.Important)
            .ThenByDescending(calendarEvent => calendarEvent.Priority)
            .ThenBy(calendarEvent => calendarEvent.GetNextOccurrenceDay(GetCurrentDay(), 60) < 0 ? int.MaxValue : calendarEvent.GetNextOccurrenceDay(GetCurrentDay(), 60))
            .ThenBy(calendarEvent => calendarEvent.Title)
            .ToList();
    }

    public List<CalendarEventDefinition> GetActiveEvents() {
        var player = GetComponent<PlayerController>();
        return Resources.LoadAll<CalendarEventDefinition>("")
            .Where(calendarEvent => calendarEvent != null && calendarEvent.IsActiveNow() && calendarEvent.CanShow(player, this, out _))
            .OrderByDescending(calendarEvent => calendarEvent.Priority)
            .ThenBy(calendarEvent => calendarEvent.Title)
            .ToList();
    }

    PlayerCalendarEventState GetOrCreateState(CalendarEventDefinition calendarEvent) {
        var state = GetState(calendarEvent.Id);
        if(state != null) {
            return state;
        }

        state = new PlayerCalendarEventState {
            eventId = calendarEvent.Id,
            eventTitle = calendarEvent.Title
        };
        eventHistory.Add(state);
        return state;
    }

    PlayerCalendarEventState GetState(string eventId) {
        if(string.IsNullOrWhiteSpace(eventId)) {
            return null;
        }

        return eventHistory.FirstOrDefault(state => state != null && state.eventId == eventId);
    }

    CalendarEventDefinition ResolveEvent(string eventId) {
        if(string.IsNullOrWhiteSpace(eventId)) {
            return null;
        }

        return Resources.LoadAll<CalendarEventDefinition>("").FirstOrDefault(calendarEvent => calendarEvent != null && calendarEvent.Id == eventId);
    }

    int GetCurrentDay() {
        return TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    void PublishCalendarLogEvent(string phase, string eventId, string eventTitle, string source, GameEventImportance importance) {
        GameEventPublishing.PublishOptional(
            null,
            $"calendar.{phase}.{eventId}",
            $"{eventTitle} {phase}.",
            GameEventCategory.Calendar,
            importance,
            this,
            "PlayerCalendarLog",
            GameEventScope.Player,
            showInFeed: true,
            writeToDebugLog: false,
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("eventId", eventId),
            GameEventPublishing.Value("eventTitle", eventTitle),
            GameEventPublishing.Value("source", source));
    }

    public object CaptureState() {
        return new PlayerCalendarLogSaveData {
            unlockedEventIds = unlockedEventIds.Distinct().ToList(),
            dismissedEventIds = dismissedEventIds.Distinct().ToList(),
            eventHistory = eventHistory.Where(state => state != null).Select(state => state.ToSaveData()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerCalendarLogSaveData;
        unlockedEventIds = saveData?.unlockedEventIds?.Distinct().ToList() ?? new List<string>();
        dismissedEventIds = saveData?.dismissedEventIds?.Distinct().ToList() ?? new List<string>();
        eventHistory = saveData?.eventHistory?.Where(s => s != null).Select(s => new PlayerCalendarEventState(s)).ToList() ?? new List<PlayerCalendarEventState>();
        OnCalendarChanged?.Invoke();
    }
}

[Serializable]
public class PlayerCalendarEventState {
    [Tooltip("Saved calendar event id.")]
    public string eventId;
    [Tooltip("Saved event title for fallback/debug output.")]
    public string eventTitle;
    [Tooltip("How many times the player has seen this event.")]
    [Min(0)]
    public int seenCount;
    [Tooltip("How many times the player completed or resolved this event.")]
    [Min(0)]
    public int completedCount;
    [Tooltip("First in-game day this event was seen.")]
    public int firstSeenDay = -1;
    [Tooltip("First absolute in-game hour this event was seen.")]
    public int firstSeenAbsoluteHour = -1;
    [Tooltip("Last in-game day this event was seen.")]
    public int lastSeenDay = -1;
    [Tooltip("Last absolute in-game hour this event was seen.")]
    public int lastSeenAbsoluteHour = -1;
    [Tooltip("Last in-game day this event was completed.")]
    public int lastCompletedDay = -1;
    [Tooltip("Last absolute in-game hour this event was completed.")]
    public int lastCompletedAbsoluteHour = -1;
    [Tooltip("Last source id that revealed this event.")]
    public string lastSourceId;
    [Tooltip("Last source name that revealed this event.")]
    public string lastSourceName;

    public PlayerCalendarEventState() {
    }

    public PlayerCalendarEventState(PlayerCalendarEventStateSaveData saveData) {
        if(saveData == null) {
            return;
        }

        eventId = saveData.eventId;
        eventTitle = saveData.eventTitle;
        seenCount = Mathf.Max(0, saveData.seenCount);
        completedCount = Mathf.Max(0, saveData.completedCount);
        firstSeenDay = saveData.firstSeenDay;
        firstSeenAbsoluteHour = saveData.firstSeenAbsoluteHour;
        lastSeenDay = saveData.lastSeenDay;
        lastSeenAbsoluteHour = saveData.lastSeenAbsoluteHour;
        lastCompletedDay = saveData.lastCompletedDay;
        lastCompletedAbsoluteHour = saveData.lastCompletedAbsoluteHour;
        lastSourceId = saveData.lastSourceId;
        lastSourceName = saveData.lastSourceName;
    }

    public PlayerCalendarEventStateSaveData ToSaveData() {
        return new PlayerCalendarEventStateSaveData {
            eventId = eventId,
            eventTitle = eventTitle,
            seenCount = seenCount,
            completedCount = completedCount,
            firstSeenDay = firstSeenDay,
            firstSeenAbsoluteHour = firstSeenAbsoluteHour,
            lastSeenDay = lastSeenDay,
            lastSeenAbsoluteHour = lastSeenAbsoluteHour,
            lastCompletedDay = lastCompletedDay,
            lastCompletedAbsoluteHour = lastCompletedAbsoluteHour,
            lastSourceId = lastSourceId,
            lastSourceName = lastSourceName
        };
    }
}

[Serializable]
public class PlayerCalendarLogSaveData {
    public List<string> unlockedEventIds;
    public List<string> dismissedEventIds;
    public List<PlayerCalendarEventStateSaveData> eventHistory;
}

[Serializable]
public class PlayerCalendarEventStateSaveData {
    public string eventId;
    public string eventTitle;
    public int seenCount;
    public int completedCount;
    public int firstSeenDay;
    public int firstSeenAbsoluteHour;
    public int lastSeenDay;
    public int lastSeenAbsoluteHour;
    public int lastCompletedDay;
    public int lastCompletedAbsoluteHour;
    public string lastSourceId;
    public string lastSourceName;
}
