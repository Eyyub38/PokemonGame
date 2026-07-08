using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class GameEventUnityEvent : UnityEvent<GameEventRecord> {
}

public class GameEventListener : MonoBehaviour {
    [Header("Filters")]
    [Tooltip("Optional specific event definition to listen for. Empty listens by category/importance filters.")]
    [SerializeField] GameEventDefinition eventFilter;
    [Tooltip("Optional category filter. Empty means all categories.")]
    [SerializeField] List<GameEventCategory> categoryFilters = new List<GameEventCategory>();
    [Tooltip("Minimum importance accepted by this listener.")]
    [SerializeField] GameEventImportance minimumImportance = GameEventImportance.Trace;
    [Tooltip("If disabled, events marked hidden from feed are ignored.")]
    [SerializeField] bool includeHiddenFeedEvents = true;

    [Header("History")]
    [Tooltip("If enabled, existing event bus history is replayed to this listener when enabled.")]
    [SerializeField] bool replayHistoryOnEnable;

    [Header("Output")]
    [Tooltip("Invoked when an event passes all filters.")]
    [SerializeField] GameEventUnityEvent onEventPublished = new GameEventUnityEvent();

    public GameEventUnityEvent OnEventPublished => onEventPublished;

    void OnEnable() {
        GameEventBus.Subscribe(HandleEvent, replayHistoryOnEnable);
    }

    void OnDisable() {
        GameEventBus.Unsubscribe(HandleEvent);
    }

    void HandleEvent(GameEventRecord record) {
        if(!Matches(record)) {
            return;
        }

        onEventPublished?.Invoke(record);
    }

    bool Matches(GameEventRecord record) {
        if(record == null) {
            return false;
        }

        if(eventFilter != null && record.id != eventFilter.Id) {
            return false;
        }

        if(!includeHiddenFeedEvents && !record.showInFeed) {
            return false;
        }

        if(categoryFilters.Count > 0 && !categoryFilters.Contains(record.category)) {
            return false;
        }

        return record.importance >= minimumImportance;
    }
}
