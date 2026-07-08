using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Notifications/Notification Definition")]
public class NotificationDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable id for this notification template. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Default title used by this notification. Empty uses the asset name.")]
    [SerializeField] string title;
    [Tooltip("Designer note explaining when this notification should be used.")]
    [TextArea][SerializeField] string description;

    [Header("Display")]
    [Tooltip("Default message used when code does not provide a custom message.")]
    [TextArea][SerializeField] string defaultMessage;
    [Tooltip("Visual/log group for this notification.")]
    [SerializeField] NotificationKind kind = NotificationKind.General;
    [Tooltip("Importance level used by future UI styling and filters.")]
    [SerializeField] NotificationPriority priority = NotificationPriority.Normal;
    [Tooltip("Channel used by future UI tabs or filters.")]
    [SerializeField] NotificationChannel channel = NotificationChannel.Gameplay;

    [Header("Behavior")]
    [Tooltip("If enabled, this notification stays when normal feed entries are cleared.")]
    [SerializeField] bool pinned;
    [Tooltip("If enabled, publishing this notification also writes to GameDebugLogger.")]
    [SerializeField] bool writeToDebugLog;
    [Tooltip("Extra details copied into records created from this definition.")]
    [SerializeField] List<GameEventValue> defaultValues = new List<GameEventValue>();

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string Title => string.IsNullOrWhiteSpace(title) ? name : title;
    public string Description => description;
    public string DefaultMessage => defaultMessage;
    public NotificationKind Kind => kind;
    public NotificationPriority Priority => priority;
    public NotificationChannel Channel => channel;
    public bool Pinned => pinned;
    public bool WriteToDebugLog => writeToDebugLog;
    public IReadOnlyList<GameEventValue> DefaultValues => defaultValues;

    public NotificationRecord CreateRecord(string messageOverride = null, string source = null, IEnumerable<GameEventValue> values = null) {
        var mergedValues = new List<GameEventValue>();
        if(defaultValues != null) {
            mergedValues.AddRange(defaultValues);
        }
        if(values != null) {
            mergedValues.AddRange(values);
        }

        return NotificationRecord.Create(
            Title,
            string.IsNullOrWhiteSpace(messageOverride) ? defaultMessage : messageOverride,
            kind,
            priority,
            channel,
            sourceEventId: Id,
            sourceEventRecordKey: null,
            source: source,
            pinned: pinned,
            values: mergedValues);
    }
}
