using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WorldEventManager : MonoBehaviour, ISavable {
    [Tooltip("All world events this manager can evaluate from schedule rules.")]
    [SerializeField] List<WorldEventDefinition> eventDefinitions = new List<WorldEventDefinition>();
    [Tooltip("Events forced active by scene setup, debug tools or story scripts.")]
    [SerializeField] List<WorldEventDefinition> manuallyActiveEvents = new List<WorldEventDefinition>();

    readonly List<string> activeEventIds = new List<string>();

    public static WorldEventManager i { get; private set; }
    public IReadOnlyList<string> ActiveEventIds => activeEventIds;
    public event Action OnWorldEventsChanged;

    void Awake() {
        i = this;
        RefreshActiveEvents();
    }

    void OnEnable() {
        if(TimeSystem.i != null) {
            TimeSystem.i.OnTimeChanged += RefreshActiveEvents;
            TimeSystem.i.OnDayChanged += RefreshActiveEvents;
        }
    }

    void OnDisable() {
        if(TimeSystem.i != null) {
            TimeSystem.i.OnTimeChanged -= RefreshActiveEvents;
            TimeSystem.i.OnDayChanged -= RefreshActiveEvents;
        }
    }

    public void SetEventActive(WorldEventDefinition worldEvent, bool active) {
        if(worldEvent == null) {
            return;
        }

        if(active && !manuallyActiveEvents.Contains(worldEvent)) {
            manuallyActiveEvents.Add(worldEvent);
        } else if(!active) {
            manuallyActiveEvents.Remove(worldEvent);
        }

        RefreshActiveEvents();
    }

    public bool IsEventActive(WorldEventDefinition worldEvent) {
        return worldEvent != null && activeEventIds.Contains(worldEvent.Id);
    }

    public bool IsActivityBlocked(ActivityDefinition activity, out string failureMessage) {
        foreach(var worldEvent in GetActiveEventsFor(activity)) {
            if(worldEvent.BlocksActivities) {
                failureMessage = worldEvent.BlockedActivityMessage;
                return true;
            }
        }

        failureMessage = null;
        return false;
    }

    public int ModifyExperience(ActivityDefinition activity, int amount) {
        if(amount <= 0) {
            return 0;
        }

        float multiplier = 1f;
        foreach(var worldEvent in GetActiveEventsFor(activity)) {
            multiplier *= worldEvent.ExperienceMultiplier;
        }

        return Mathf.RoundToInt(amount * multiplier);
    }

    public void ApplyActivityReputation(PlayerController player, ActivityDefinition activity) {
        if(player == null || activity == null) {
            return;
        }

        var reputation = player.GetComponent<PlayerReputation>();
        if(reputation == null) {
            return;
        }

        reputation.ApplyChanges(activity.ReputationChanges);
        foreach(var worldEvent in GetActiveEventsFor(activity)) {
            reputation.ApplyChanges(worldEvent.ReputationChangesOnActivity);
        }
    }

    IEnumerable<WorldEventDefinition> GetActiveEventsFor(ActivityDefinition activity) {
        return eventDefinitions
            .Concat(manuallyActiveEvents)
            .Where(e => e != null && activeEventIds.Contains(e.Id) && e.Affects(activity))
            .Distinct();
    }

    void RefreshActiveEvents() {
        var previousIds = activeEventIds.ToList();
        var allEvents = eventDefinitions
            .Concat(manuallyActiveEvents)
            .Where(e => e != null)
            .Distinct()
            .ToList();

        var nextIds = eventDefinitions
            .Concat(manuallyActiveEvents)
            .Where(e => e != null && (manuallyActiveEvents.Contains(e) || e.IsActiveNow(TimeSystem.i)))
            .Select(e => e.Id)
            .Distinct()
            .ToList();

        bool changed = nextIds.Count != activeEventIds.Count || nextIds.Any(id => !activeEventIds.Contains(id));
        activeEventIds.Clear();
        activeEventIds.AddRange(nextIds);

        if(changed) {
            OnWorldEventsChanged?.Invoke();
            PublishWorldEventChanges(previousIds, nextIds, allEvents);
        }
    }

    void PublishWorldEventChanges(List<string> previousIds, List<string> nextIds, List<WorldEventDefinition> allEvents) {
        foreach(var eventId in nextIds.Except(previousIds)) {
            var worldEvent = allEvents.FirstOrDefault(e => e.Id == eventId);
            if(worldEvent != null) {
                PublishWorldEventState(worldEvent, active: true);
            }
        }

        foreach(var eventId in previousIds.Except(nextIds)) {
            var worldEvent = allEvents.FirstOrDefault(e => e.Id == eventId);
            if(worldEvent != null) {
                PublishWorldEventState(worldEvent, active: false);
            }
        }
    }

    void PublishWorldEventState(WorldEventDefinition worldEvent, bool active) {
        GameEventPublishing.PublishOptional(
            active ? worldEvent.ActivatedEvent : worldEvent.DeactivatedEvent,
            active ? $"world-event.activated.{worldEvent.Id}" : $"world-event.deactivated.{worldEvent.Id}",
            active ? $"{worldEvent.DisplayName} is active." : $"{worldEvent.DisplayName} ended.",
            GameEventCategory.WorldEvent,
            active ? GameEventImportance.Info : GameEventImportance.Trace,
            this,
            "WorldEventManager",
            GameEventScope.Global,
            showInFeed: true,
            writeToDebugLog: false,
            GameEventPublishing.Value("worldEventId", worldEvent.Id),
            GameEventPublishing.Value("worldEventName", worldEvent.DisplayName),
            GameEventPublishing.Value("active", active));
    }

    public object CaptureState() {
        return new WorldEventSaveData() {
            manuallyActiveEventIds = manuallyActiveEvents
                .Where(e => e != null)
                .Select(e => e.Id)
                .Distinct()
                .ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as WorldEventSaveData;
        if(saveData == null) {
            return;
        }

        manuallyActiveEvents = eventDefinitions
            .Where(e => e != null && saveData.manuallyActiveEventIds != null && saveData.manuallyActiveEventIds.Contains(e.Id))
            .ToList();
        RefreshActiveEvents();
    }
}

[Serializable]
public class WorldEventSaveData {
    public List<string> manuallyActiveEventIds;
}
