using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Events/Game Event Definition")]
public class GameEventDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable id used by save/debug/filter systems. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Readable event name shown by future UI/debug tools. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer note explaining when this event should be published.")]
    [TextArea][SerializeField] string description;

    [Header("Classification")]
    [Tooltip("Broad category used by filters and future UI.")]
    [SerializeField] GameEventCategory category = GameEventCategory.General;
    [Tooltip("Default importance used when this event is published.")]
    [SerializeField] GameEventImportance importance = GameEventImportance.Info;
    [Tooltip("Default scope used when this event is published.")]
    [SerializeField] GameEventScope scope = GameEventScope.Global;

    [Header("Output")]
    [Tooltip("If enabled, notification/feed systems may show this event.")]
    [SerializeField] bool showInFeed = true;
    [Tooltip("If enabled, this event is also written into GameDebugLogger.")]
    [SerializeField] bool writeToDebugLog;
    [Tooltip("Fallback message used when code does not provide a custom message.")]
    [TextArea][SerializeField] string defaultMessage;
    [Tooltip("Fallback source label used when code does not provide one.")]
    [SerializeField] string defaultSource;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public GameEventCategory Category => category;
    public GameEventImportance Importance => importance;
    public GameEventScope Scope => scope;
    public bool ShowInFeed => showInFeed;
    public bool WriteToDebugLog => writeToDebugLog;
    public string DefaultMessage => defaultMessage;
    public string DefaultSource => defaultSource;

    public GameEventRecord CreateRecord(
        string messageOverride = null,
        UnityEngine.Object context = null,
        string sourceOverride = null,
        GameEventScope? scopeOverride = null,
        IEnumerable<GameEventValue> values = null
    ) {
        string message = string.IsNullOrWhiteSpace(messageOverride) ? defaultMessage : messageOverride;
        string source = string.IsNullOrWhiteSpace(sourceOverride) ? defaultSource : sourceOverride;

        return GameEventRecord.Create(
            Id,
            DisplayName,
            message,
            category,
            importance,
            scopeOverride ?? scope,
            showInFeed,
            writeToDebugLog,
            context,
            source,
            values);
    }
}
