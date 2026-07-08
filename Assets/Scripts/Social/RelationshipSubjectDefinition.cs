using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Social/Relationship Subject Definition")]
public class RelationshipSubjectDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this relationship subject. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in UI/debug messages. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer note or player-facing description for this subject.")]
    [TextArea][SerializeField] string description;
    [Header("Values")]
    [Tooltip("Starting relationship value.")]
    [SerializeField] int defaultValue;
    [Tooltip("Lowest relationship value allowed.")]
    [SerializeField] int minValue = -100;
    [Tooltip("Highest relationship value allowed.")]
    [SerializeField] int maxValue = 100;
    [Tooltip("Optional named tiers shown by future UI or used by story logic.")]
    [SerializeField] List<RelationshipTier> tiers = new List<RelationshipTier>();

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public int DefaultValue => Mathf.Clamp(defaultValue, MinValue, MaxValue);
    public int MinValue => minValue;
    public int MaxValue => Mathf.Max(minValue + 1, maxValue);
    public IReadOnlyList<RelationshipTier> Tiers => tiers;

    public string GetTierName(int value) {
        var tier = tiers
            .Where(t => value >= t.minimumValue)
            .OrderByDescending(t => t.minimumValue)
            .FirstOrDefault();

        return tier != null && !string.IsNullOrWhiteSpace(tier.displayName) ? tier.displayName : string.Empty;
    }
}

[System.Serializable]
public class RelationshipTier {
    [Tooltip("Name shown when the relationship reaches this tier.")]
    public string displayName;
    [Tooltip("Minimum value required for this tier.")]
    public int minimumValue;
}

[System.Serializable]
public class RelationshipChange {
    [Tooltip("Relationship subject whose value changes.")]
    public RelationshipSubjectDefinition subject;
    [Tooltip("Amount to add. Negative values reduce the relationship.")]
    public int amount;
}

[System.Serializable]
public class RelationshipValue {
    [Tooltip("Saved relationship subject id.")]
    public string subjectId;
    [Tooltip("Saved relationship value.")]
    public int value;
}
