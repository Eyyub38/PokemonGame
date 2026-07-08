using System.Collections.Generic;
using UnityEngine;

public class WorldTriggerController : MonoBehaviour {
    [Header("Triggers")]
    [Tooltip("World triggers evaluated by this controller.")]
    [SerializeField] List<WorldTriggerDefinition> triggers = new List<WorldTriggerDefinition>();
    [Tooltip("Optional player override. Empty uses PlayerController.i or the first loaded PlayerController.")]
    [SerializeField] PlayerController playerOverride = null;

    [Header("Signals")]
    [Tooltip("If enabled, this controller listens to GameEventBus.")]
    [SerializeField] bool listenToGameEvents = true;
    [Tooltip("If enabled, event bus history is replayed when this controller enables.")]
    [SerializeField] bool replayGameEventHistoryOnEnable;
    [Tooltip("If enabled, this controller evaluates Time Changed triggers.")]
    [SerializeField] bool listenToTimeChanges = true;
    [Tooltip("If enabled, this controller evaluates Day Changed triggers.")]
    [SerializeField] bool listenToDayChanges = true;
    [Tooltip("If enabled, Manual triggers are evaluated once at Start.")]
    [SerializeField] bool evaluateManualTriggersOnStart;

    [Header("Debug")]
    [Tooltip("If enabled, controller attempts are written to GameDebugLogger.")]
    [SerializeField] bool logAttempts;

    bool timeSubscribed;

    public IReadOnlyList<WorldTriggerDefinition> Triggers => triggers;

    void OnEnable() {
        if(listenToGameEvents) {
            GameEventBus.Subscribe(HandleGameEvent, replayGameEventHistoryOnEnable);
        }

        SubscribeTime();
    }

    void Start() {
        SubscribeTime();
        if(evaluateManualTriggersOnStart) {
            Evaluate(WorldTriggerKind.Manual, null, "world-trigger-controller:start", name);
        }
    }

    void OnDisable() {
        if(listenToGameEvents) {
            GameEventBus.Unsubscribe(HandleGameEvent);
        }

        UnsubscribeTime();
    }

    [ContextMenu("Evaluate Manual Triggers")]
    public void EvaluateManualTriggersFromContextMenu() {
        Evaluate(WorldTriggerKind.Manual, null, "world-trigger-controller:context-menu", name);
    }

    public List<WorldTriggerRunResult> Evaluate(WorldTriggerKind kind, GameEventRecord record = null, string sourceId = null, string sourceName = null) {
        var results = new List<WorldTriggerRunResult>();
        var player = ResolvePlayer();
        foreach(var trigger in triggers) {
            if(trigger == null || trigger.TriggerKind != kind) {
                continue;
            }

            var result = trigger.Apply(player, kind, record, sourceId, sourceName, this);
            if(result != null) {
                results.Add(result);
                if(logAttempts) {
                    GameDebugLogger.Ensure().Record(
                        result.blocked ? GameDebugSeverity.Warning : GameDebugSeverity.Info,
                        GameDebugCategory.WorldTrigger,
                        result.blocked ? $"{trigger.DisplayName} blocked: {result.failureMessage}" : $"{trigger.DisplayName} applied {result.appliedChains} chain(s).",
                        this,
                        "WorldTriggerController");
                }
            }
        }

        return results;
    }

    void HandleGameEvent(GameEventRecord record) {
        Evaluate(WorldTriggerKind.GameEvent, record, record != null ? record.id : null, record != null ? record.displayName : null);
    }

    void HandleTimeChanged() {
        string sourceId = TimeSystem.i != null ? $"time:{TimeSystem.i.Day}:{TimeSystem.i.Hour}" : "time:unknown";
        Evaluate(WorldTriggerKind.TimeChanged, null, sourceId, "Time Changed");
    }

    void HandleDayChanged() {
        string sourceId = TimeSystem.i != null ? $"day:{TimeSystem.i.Day}" : "day:unknown";
        Evaluate(WorldTriggerKind.DayChanged, null, sourceId, "Day Changed");
    }

    void SubscribeTime() {
        if(timeSubscribed || TimeSystem.i == null) {
            return;
        }

        if(listenToTimeChanges) {
            TimeSystem.i.OnTimeChanged += HandleTimeChanged;
        }

        if(listenToDayChanges) {
            TimeSystem.i.OnDayChanged += HandleDayChanged;
        }

        timeSubscribed = listenToTimeChanges || listenToDayChanges;
    }

    void UnsubscribeTime() {
        if(!timeSubscribed || TimeSystem.i == null) {
            timeSubscribed = false;
            return;
        }

        TimeSystem.i.OnTimeChanged -= HandleTimeChanged;
        TimeSystem.i.OnDayChanged -= HandleDayChanged;
        timeSubscribed = false;
    }

    PlayerController ResolvePlayer() {
        if(playerOverride != null) {
            return playerOverride;
        }

        if(PlayerController.i != null) {
            return PlayerController.i;
        }

        return FindAnyObjectByType<PlayerController>();
    }
}
