using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameEventBus : MonoBehaviour {
    [Header("Lifetime")]
    [Tooltip("If enabled, this event bus survives scene loads.")]
    [SerializeField] bool dontDestroyOnLoad = true;

    [Header("History")]
    [Tooltip("Maximum number of recent events kept in memory for debug/feed systems.")]
    [Min(1)]
    [SerializeField] int maxHistory = 250;
    [Tooltip("Runtime history of recently published events.")]
    [SerializeField] List<GameEventRecord> history = new List<GameEventRecord>();

    [Header("Debug")]
    [Tooltip("If enabled, every event is written to GameDebugLogger even when the event definition does not request it.")]
    [SerializeField] bool forceDebugLogging;

    public static GameEventBus i { get; private set; }
    public static event Action<GameEventRecord> GlobalEventPublished;

    public IReadOnlyList<GameEventRecord> History => history;
    public event Action<GameEventRecord> OnEventPublished;

    void Awake() {
        if(i != null && i != this) {
            Destroy(gameObject);
            return;
        }

        i = this;
        if(dontDestroyOnLoad) {
            DontDestroyOnLoad(gameObject);
        }
    }

    public static GameEventBus Ensure() {
        if(i != null) {
            return i;
        }

        var existing = FindAnyObjectByType<GameEventBus>();
        if(existing != null) {
            i = existing;
            return i;
        }

        var go = new GameObject("GameEventBus");
        return go.AddComponent<GameEventBus>();
    }

    public static void Subscribe(Action<GameEventRecord> handler, bool replayHistory = false) {
        if(handler == null) {
            return;
        }

        GlobalEventPublished += handler;
        if(replayHistory) {
            Ensure().ReplayTo(handler);
        }
    }

    public static void Unsubscribe(Action<GameEventRecord> handler) {
        if(handler != null) {
            GlobalEventPublished -= handler;
        }
    }

    public static GameEventRecord Publish(
        GameEventDefinition definition,
        string messageOverride = null,
        UnityEngine.Object context = null,
        string sourceOverride = null,
        GameEventScope? scopeOverride = null,
        IEnumerable<GameEventValue> values = null
    ) {
        if(definition == null) {
            return Publish(
                "event.missing-definition",
                messageOverride,
                GameEventCategory.Debug,
                GameEventImportance.Warning,
                context,
                sourceOverride,
                scopeOverride ?? GameEventScope.Global,
                showInFeed: false,
                writeToDebugLog: true,
                values);
        }

        return Publish(definition.CreateRecord(messageOverride, context, sourceOverride, scopeOverride, values), context);
    }

    public static GameEventRecord Publish(
        string id,
        string message,
        GameEventCategory category = GameEventCategory.General,
        GameEventImportance importance = GameEventImportance.Info,
        UnityEngine.Object context = null,
        string source = null,
        GameEventScope scope = GameEventScope.Global,
        bool showInFeed = true,
        bool writeToDebugLog = false,
        IEnumerable<GameEventValue> values = null
    ) {
        var displayName = string.IsNullOrWhiteSpace(id) ? "Runtime Event" : id;
        var record = GameEventRecord.Create(id, displayName, message, category, importance, scope, showInFeed, writeToDebugLog, context, source, values);
        return Publish(record, context);
    }

    public static GameEventRecord Publish(GameEventRecord record, UnityEngine.Object context = null) {
        if(record == null) {
            return null;
        }

        Ensure().PublishInternal(record, context);
        return record;
    }

    public IReadOnlyList<GameEventRecord> GetHistory(
        GameEventCategory? category = null,
        GameEventImportance? minimumImportance = null,
        bool includeHiddenFeedEvents = true
    ) {
        return history
            .Where(e => e != null)
            .Where(e => includeHiddenFeedEvents || e.showInFeed)
            .Where(e => !category.HasValue || e.category == category.Value)
            .Where(e => !minimumImportance.HasValue || e.importance >= minimumImportance.Value)
            .ToList();
    }

    void PublishInternal(GameEventRecord record, UnityEngine.Object context) {
        history.Add(record);
        while(history.Count > Mathf.Max(1, maxHistory)) {
            history.RemoveAt(0);
        }

        if(forceDebugLogging || record.writeToDebugLog) {
            WriteToDebugLog(record, context);
        }

        OnEventPublished?.Invoke(record);
        GlobalEventPublished?.Invoke(record);
    }

    void ReplayTo(Action<GameEventRecord> handler) {
        foreach(var record in history) {
            handler?.Invoke(record);
        }
    }

    void WriteToDebugLog(GameEventRecord record, UnityEngine.Object context) {
        GameDebugLogger.Ensure().Record(
            ToDebugSeverity(record.importance),
            ToDebugCategory(record.category),
            record.message,
            context,
            string.IsNullOrWhiteSpace(record.source) ? record.id : record.source);
    }

    GameDebugSeverity ToDebugSeverity(GameEventImportance importance) {
        return importance switch {
            GameEventImportance.Trace => GameDebugSeverity.Trace,
            GameEventImportance.Success => GameDebugSeverity.Success,
            GameEventImportance.Warning => GameDebugSeverity.Warning,
            GameEventImportance.Error => GameDebugSeverity.Error,
            _ => GameDebugSeverity.Info
        };
    }

    GameDebugCategory ToDebugCategory(GameEventCategory category) {
        return category switch {
            GameEventCategory.Battle => GameDebugCategory.Battle,
            GameEventCategory.Encounter => GameDebugCategory.Encounter,
            GameEventCategory.Activity => GameDebugCategory.Activity,
            GameEventCategory.Farming => GameDebugCategory.Farming,
            GameEventCategory.Resource => GameDebugCategory.Resource,
            GameEventCategory.Crafting => GameDebugCategory.Crafting,
            GameEventCategory.PokemonCare => GameDebugCategory.PokemonCare,
            GameEventCategory.Research => GameDebugCategory.Research,
            GameEventCategory.Quest => GameDebugCategory.Quest,
            GameEventCategory.RPG => GameDebugCategory.RPG,
            GameEventCategory.Inventory => GameDebugCategory.Inventory,
            GameEventCategory.Shop => GameDebugCategory.Shop,
            GameEventCategory.NPC => GameDebugCategory.NPC,
            GameEventCategory.Job => GameDebugCategory.Job,
            GameEventCategory.Transit => GameDebugCategory.Transit,
            GameEventCategory.Customization => GameDebugCategory.Customization,
            GameEventCategory.PokeNav => GameDebugCategory.PokeNav,
            GameEventCategory.Map => GameDebugCategory.Map,
            GameEventCategory.Rumor => GameDebugCategory.Rumor,
            GameEventCategory.Calendar => GameDebugCategory.Calendar,
            GameEventCategory.Access => GameDebugCategory.Access,
            GameEventCategory.Law => GameDebugCategory.Law,
            GameEventCategory.Investigation => GameDebugCategory.Investigation,
            GameEventCategory.Risk => GameDebugCategory.Risk,
            GameEventCategory.Consequence => GameDebugCategory.Consequence,
            GameEventCategory.WorldTrigger => GameDebugCategory.WorldTrigger,
            GameEventCategory.SceneObject => GameDebugCategory.SceneObject,
            GameEventCategory.SceneSpawn => GameDebugCategory.SceneSpawn,
            GameEventCategory.WorldDiscovery => GameDebugCategory.WorldDiscovery,
            GameEventCategory.LocationVisit => GameDebugCategory.LocationVisit,
            GameEventCategory.Chronicle => GameDebugCategory.Chronicle,
            GameEventCategory.Navigation => GameDebugCategory.Navigation,
            GameEventCategory.AreaProfile => GameDebugCategory.AreaProfile,
            GameEventCategory.SaveLoad => GameDebugCategory.SaveLoad,
            GameEventCategory.UI => GameDebugCategory.UI,
            GameEventCategory.Debug => GameDebugCategory.Validation,
            _ => GameDebugCategory.General
        };
    }
}
