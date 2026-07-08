using UnityEngine;

[CreateAssetMenu(menuName = "Pokemon Mood/Mood Definition")]
public class PokemonMoodDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this mood. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in UI/debug messages. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer note or player-facing description for this mood.")]
    [TextArea][SerializeField] string description;
    [Header("Values")]
    [Tooltip("Starting value new Pokemon use for this mood.")]
    [SerializeField] int defaultValue = 50;
    [Tooltip("Lowest allowed value for this mood.")]
    [SerializeField] int minValue = 0;
    [Tooltip("Highest allowed value for this mood.")]
    [SerializeField] int maxValue = 100;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public int DefaultValue => Mathf.Clamp(defaultValue, MinValue, MaxValue);
    public int MinValue => minValue;
    public int MaxValue => Mathf.Max(minValue + 1, maxValue);
}

[System.Serializable]
public class PokemonMoodChange {
    [Tooltip("Mood definition to change.")]
    public PokemonMoodDefinition mood;
    [Tooltip("Amount to add. Negative values reduce the mood.")]
    public int amount;
}

[System.Serializable]
public class PokemonMoodValue {
    [Tooltip("Saved mood id.")]
    public string moodId;
    [Tooltip("Saved mood value.")]
    public int value;
}
