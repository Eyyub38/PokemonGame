using UnityEngine;

[CreateAssetMenu(menuName = "Reputation/Faction Definition")]
public class ReputationFactionDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this faction. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in UI/debug messages. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer note or player-facing description for this faction.")]
    [TextArea][SerializeField] string description;
    [Header("Values")]
    [Tooltip("Starting reputation value.")]
    [SerializeField] int defaultValue;
    [Tooltip("Lowest reputation value allowed.")]
    [SerializeField] int minValue = -100;
    [Tooltip("Highest reputation value allowed.")]
    [SerializeField] int maxValue = 100;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public int DefaultValue => Mathf.Clamp(defaultValue, MinValue, MaxValue);
    public int MinValue => minValue;
    public int MaxValue => Mathf.Max(minValue + 1, maxValue);
}

[System.Serializable]
public class ReputationChange {
    [Tooltip("Faction whose reputation changes.")]
    public ReputationFactionDefinition faction;
    [Tooltip("Amount to add. Negative values reduce reputation.")]
    public int amount;
}

[System.Serializable]
public class ReputationValue {
    [Tooltip("Saved faction id.")]
    public string factionId;
    [Tooltip("Saved reputation value.")]
    public int value;
}
