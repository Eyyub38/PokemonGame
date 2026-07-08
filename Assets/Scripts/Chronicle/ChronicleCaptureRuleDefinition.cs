using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum ChronicleRuleImportanceMode {
    UseEventImportance,
    Override
}

[CreateAssetMenu(menuName = "Chronicle/Capture Rule Definition")]
public class ChronicleCaptureRuleDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable id used by validation/debug systems. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in debug/future UI. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note explaining which events this rule captures.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("If disabled, this rule is ignored by PlayerChronicleLog.")]
    [SerializeField] bool enabled = true;

    [Header("Event Filters")]
    [Tooltip("If enabled, events hidden from notification/feed systems may still be captured by this rule.")]
    [SerializeField] bool includeHiddenEvents;
    [Tooltip("Minimum event importance accepted by this rule.")]
    [SerializeField] GameEventImportance minimumImportance = GameEventImportance.Info;
    [Tooltip("Optional category allow-list. Empty means any category can match.")]
    [SerializeField] List<GameEventCategory> eventCategories = new List<GameEventCategory>();
    [Tooltip("Optional scope allow-list. Empty means any scope can match.")]
    [SerializeField] List<GameEventScope> eventScopes = new List<GameEventScope>();
    [Tooltip("Optional source text that must be contained in the event source. Empty disables this filter.")]
    [SerializeField] string sourceContains = string.Empty;
    [Tooltip("Optional value key that must exist on the event. Empty disables this filter.")]
    [SerializeField] string requiredValueKey = string.Empty;
    [Tooltip("Optional value text required for Required Value Key. Empty means any value is accepted.")]
    [SerializeField] string requiredValueContains = string.Empty;

    [Header("Chronicle Output")]
    [Tooltip("Category assigned to matching chronicle records.")]
    [SerializeField] ChronicleCategory outputCategory = ChronicleCategory.General;
    [Tooltip("How importance is assigned to matching chronicle records.")]
    [SerializeField] ChronicleRuleImportanceMode importanceMode = ChronicleRuleImportanceMode.UseEventImportance;
    [Tooltip("Importance used when Importance Mode is Override.")]
    [SerializeField] GameEventImportance overrideImportance = GameEventImportance.Info;
    [Tooltip("Optional text prepended to the generated chronicle title.")]
    [SerializeField] string titlePrefix = string.Empty;
    [Tooltip("If not empty, this replaces the event message in the chronicle record.")]
    [TextArea]
    [SerializeField] string messageOverride = string.Empty;
    [Tooltip("If enabled, future UI can keep matching records when normal entries are cleared.")]
    [SerializeField] bool pinned;
    [Tooltip("Tags copied into matching chronicle records.")]
    [SerializeField] List<string> outputTags = new List<string>();

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public bool Enabled => enabled;
    public bool IncludeHiddenEvents => includeHiddenEvents;
    public GameEventImportance MinimumImportance => minimumImportance;
    public IReadOnlyList<GameEventCategory> EventCategories => eventCategories != null ? (IReadOnlyList<GameEventCategory>)eventCategories : Array.Empty<GameEventCategory>();
    public IReadOnlyList<GameEventScope> EventScopes => eventScopes != null ? (IReadOnlyList<GameEventScope>)eventScopes : Array.Empty<GameEventScope>();
    public ChronicleCategory OutputCategory => outputCategory;
    public GameEventImportance OutputImportance(GameEventRecord record) => importanceMode == ChronicleRuleImportanceMode.Override ? overrideImportance : record != null ? record.importance : overrideImportance;
    public bool Pinned => pinned;
    public IReadOnlyList<string> OutputTags => outputTags != null ? (IReadOnlyList<string>)outputTags : Array.Empty<string>();

    public bool Matches(GameEventRecord record) {
        if(!enabled || record == null) {
            return false;
        }

        if(!includeHiddenEvents && !record.showInFeed) {
            return false;
        }

        if(record.importance < minimumImportance) {
            return false;
        }

        if(eventCategories != null && eventCategories.Count > 0 && !eventCategories.Contains(record.category)) {
            return false;
        }

        if(eventScopes != null && eventScopes.Count > 0 && !eventScopes.Contains(record.scope)) {
            return false;
        }

        if(!string.IsNullOrWhiteSpace(sourceContains)) {
            string source = record.source ?? string.Empty;
            if(source.IndexOf(sourceContains, StringComparison.OrdinalIgnoreCase) < 0) {
                return false;
            }
        }

        if(!string.IsNullOrWhiteSpace(requiredValueKey)) {
            string value = record.GetValue(requiredValueKey);
            if(value == null) {
                return false;
            }

            if(!string.IsNullOrWhiteSpace(requiredValueContains)
                && value.IndexOf(requiredValueContains, StringComparison.OrdinalIgnoreCase) < 0) {
                return false;
            }
        }

        return true;
    }

    public string BuildTitle(GameEventRecord record) {
        string baseTitle = record != null && !string.IsNullOrWhiteSpace(record.displayName) ? record.displayName : record?.id ?? DisplayName;
        return string.IsNullOrWhiteSpace(titlePrefix) ? baseTitle : $"{titlePrefix}{baseTitle}";
    }

    public string BuildMessage(GameEventRecord record) {
        if(!string.IsNullOrWhiteSpace(messageOverride)) {
            return messageOverride;
        }

        return !string.IsNullOrWhiteSpace(record?.message) ? record.message : BuildTitle(record);
    }
}
