using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum NotificationKind {
    General,
    System,
    Battle,
    Activity,
    Item,
    Quest,
    Dialogue,
    NPC,
    Companion,
    Survival,
    Progression,
    Social,
    World,
    Debug
}

public enum NotificationPriority {
    Low,
    Normal,
    High,
    Critical
}

public enum NotificationChannel {
    Gameplay,
    Story,
    Combat,
    System,
    Debug
}

[Serializable]
public class NotificationRecord {
    [Tooltip("Unique runtime id for this notification entry.")]
    public string id;
    [Tooltip("Short title for UI/feed display.")]
    public string title;
    [Tooltip("Main notification message.")]
    public string message;
    [Tooltip("Visual/log group for this notification.")]
    public NotificationKind kind;
    [Tooltip("Importance level used by future UI styling and filters.")]
    public NotificationPriority priority;
    [Tooltip("Channel used by future UI tabs or filters.")]
    public NotificationChannel channel;
    [Tooltip("If this was created from GameEventBus, the source event id is stored here.")]
    public string sourceEventId;
    [Tooltip("Unique source event record key used to avoid duplicate feed entries during history replay.")]
    public string sourceEventRecordKey;
    [Tooltip("System or script that created this notification.")]
    public string source;
    [Tooltip("Scene name when this notification was created.")]
    public string sceneName;
    [Tooltip("Unity frame when this notification was created.")]
    public int frame;
    [Tooltip("Local timestamp when this notification was created.")]
    public string timestamp;
    [Tooltip("Whether the player/UI has marked this notification as read.")]
    public bool read;
    [Tooltip("Whether this notification should be kept even if a UI clears normal feed entries.")]
    public bool pinned;
    [Tooltip("Extra key/value details for future UI, filters or debug tools.")]
    public List<GameEventValue> values = new List<GameEventValue>();

    public static NotificationRecord Create(
        string title,
        string message,
        NotificationKind kind,
        NotificationPriority priority,
        NotificationChannel channel,
        string sourceEventId = null,
        string sourceEventRecordKey = null,
        string source = null,
        bool pinned = false,
        IEnumerable<GameEventValue> values = null
    ) {
        var record = new NotificationRecord() {
            id = Guid.NewGuid().ToString("N"),
            title = title,
            message = message,
            kind = kind,
            priority = priority,
            channel = channel,
            sourceEventId = sourceEventId,
            sourceEventRecordKey = sourceEventRecordKey,
            source = source,
            sceneName = SceneManager.GetActiveScene().name,
            frame = Time.frameCount,
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
            pinned = pinned
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
