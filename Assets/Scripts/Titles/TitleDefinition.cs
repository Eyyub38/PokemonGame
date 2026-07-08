using System.Collections.Generic;
using UnityEngine;

public enum TitleKind {
    Title,
    Badge,
    Permit,
    License,
    Rank,
    Medal,
    Role
}

[CreateAssetMenu(menuName = "Titles/Title Definition")]
public class TitleDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this title, badge or permit. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in UI/debug messages. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer note or player-facing description for what this title represents.")]
    [TextArea]
    [SerializeField] string description;
    [Tooltip("Broad kind used by UI filters and access logic.")]
    [SerializeField] TitleKind kind = TitleKind.Title;
    [Tooltip("Free-form tags used by access requirements, dialog conditions and future UI filters.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Lifetime Defaults")]
    [Tooltip("If enabled, grants are permanent unless the grant overrides duration.")]
    [SerializeField] bool permanentByDefault = true;
    [Tooltip("Default duration in in-game hours when this title is granted temporarily. Ignored if permanent by default is enabled.")]
    [Min(0)]
    [SerializeField] int defaultDurationHours;
    [Tooltip("If disabled, temporary grants are ignored and this title can only be held permanently.")]
    [SerializeField] bool canBeTemporary = true;

    [Header("Events")]
    [Tooltip("Optional event published when this title is granted.")]
    [SerializeField] GameEventDefinition grantedEvent;
    [Tooltip("Optional event published when this title is revoked.")]
    [SerializeField] GameEventDefinition revokedEvent;
    [Tooltip("Optional event published when a temporary grant expires.")]
    [SerializeField] GameEventDefinition expiredEvent;
    [Tooltip("If enabled, grant/revoke/expire events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed = true;
    [Tooltip("If enabled, grant/revoke/expire events are written to the debug log.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public TitleKind Kind => kind;
    public IReadOnlyList<string> Tags => tags;
    public bool PermanentByDefault => permanentByDefault;
    public int DefaultDurationHours => Mathf.Max(0, defaultDurationHours);
    public bool CanBeTemporary => canBeTemporary;
    public GameEventDefinition GrantedEvent => grantedEvent;
    public GameEventDefinition RevokedEvent => revokedEvent;
    public GameEventDefinition ExpiredEvent => expiredEvent;
    public bool ShowEventsInFeed => showEventsInFeed;
    public bool WriteEventsToDebugLog => writeEventsToDebugLog;

    public bool HasTag(string tag) {
        if(string.IsNullOrWhiteSpace(tag) || tags == null) {
            return false;
        }

        foreach(var entry in tags) {
            if(string.Equals(entry, tag, System.StringComparison.OrdinalIgnoreCase)) {
                return true;
            }
        }

        return false;
    }
}
