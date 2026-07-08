using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameEventCategory {
    General,
    System,
    Battle,
    Activity,
    Farming,
    Resource,
    PokemonCare,
    Research,
    Quest,
    Inventory,
    SaveLoad,
    UI,
    Dialogue,
    NPC,
    Companion,
    Survival,
    RPG,
    Reputation,
    Relationship,
    WorldEvent,
    Milestone,
    Debug,
    Crafting,
    Shop,
    Encounter,
    Job,
    Transit,
    Customization,
    PokeNav,
    Map,
    Rumor,
    Calendar,
    BattleRule,
    Contest,
    Career,
    Organization,
    Assignment,
    Access,
    Law,
    Investigation,
    Risk,
    Consequence,
    WorldTrigger,
    SceneObject,
    SceneSpawn,
    WorldDiscovery,
    LocationVisit,
    Chronicle,
    Navigation,
    AreaProfile
}

public enum GameEventImportance {
    Trace,
    Info,
    Success,
    Warning,
    Error
}

public enum GameEventScope {
    Global,
    Player,
    Scene,
    Battle,
    UI
}

[Serializable]
public class GameEventValue {
    [Tooltip("Small key used by filters, UI or debugging.")]
    public string key;
    [Tooltip("String value for this event detail.")]
    public string value;

    public GameEventValue() {
    }

    public GameEventValue(string key, string value) {
        this.key = key;
        this.value = value;
    }
}

[Serializable]
public class GameEventRecord {
    [Tooltip("Stable runtime id for this event.")]
    public string id;
    [Tooltip("Readable event name.")]
    public string displayName;
    [Tooltip("Readable event message.")]
    public string message;
    [Tooltip("Broad category used by filters and future UI.")]
    public GameEventCategory category;
    [Tooltip("Importance used by filters, debug output and future UI styling.")]
    public GameEventImportance importance;
    [Tooltip("Scope that explains which part of the game this event belongs to.")]
    public GameEventScope scope;
    [Tooltip("If enabled, notification/feed systems may show this event.")]
    public bool showInFeed = true;
    [Tooltip("If enabled, the event bus also writes this event into the debug logger.")]
    public bool writeToDebugLog;
    [Tooltip("Optional script/system that published this event.")]
    public string source;
    [Tooltip("Optional Unity object name related to this event.")]
    public string contextName;
    [Tooltip("Scene name when this event was published.")]
    public string sceneName;
    [Tooltip("Unity frame when this event was published.")]
    public int frame;
    [Tooltip("Local timestamp when this event was published.")]
    public string timestamp;
    [Tooltip("Extra string key/value details attached by the publisher.")]
    public List<GameEventValue> values = new List<GameEventValue>();

    public static GameEventRecord Create(
        string id,
        string displayName,
        string message,
        GameEventCategory category,
        GameEventImportance importance,
        GameEventScope scope,
        bool showInFeed,
        bool writeToDebugLog,
        UnityEngine.Object context,
        string source,
        IEnumerable<GameEventValue> values = null
    ) {
        var record = new GameEventRecord() {
            id = string.IsNullOrWhiteSpace(id) ? "event.unnamed" : id,
            displayName = string.IsNullOrWhiteSpace(displayName) ? id : displayName,
            message = message,
            category = category,
            importance = importance,
            scope = scope,
            showInFeed = showInFeed,
            writeToDebugLog = writeToDebugLog,
            source = source,
            contextName = context != null ? context.name : null,
            sceneName = SceneManager.GetActiveScene().name,
            frame = Time.frameCount,
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")
        };

        if(values != null) {
            record.values.AddRange(values);
        }

        return record;
    }

    public string GetValue(string key) {
        if(string.IsNullOrWhiteSpace(key) || values == null) {
            return null;
        }

        foreach(var entry in values) {
            if(entry != null && entry.key == key) {
                return entry.value;
            }
        }

        return null;
    }
}
