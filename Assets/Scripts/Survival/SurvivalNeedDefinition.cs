using UnityEngine;

[CreateAssetMenu(menuName = "Survival/Need Definition")]
public class SurvivalNeedDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this need. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in UI/debug messages. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer note or player-facing description for this need.")]
    [TextArea][SerializeField] string description;
    [Header("Values")]
    [Tooltip("Maximum value this need can reach.")]
    [Min(1)]
    [SerializeField] int maxValue = 100;
    [Tooltip("Value at or below which this need counts as low.")]
    [Min(0)]
    [SerializeField] int lowThreshold = 30;
    [Tooltip("Value at or below which this need counts as critical.")]
    [Min(0)]
    [SerializeField] int criticalThreshold = 10;
    [Header("Hourly Changes")]
    [Tooltip("Amount lost each in-game hour during normal activity.")]
    [Min(0)]
    [SerializeField] int hourlyDecay;
    [Tooltip("Amount gained each in-game hour while resting.")]
    [SerializeField] int hourlyRestGain;
    [Tooltip("Amount gained each in-game hour while sleeping.")]
    [SerializeField] int hourlySleepGain;

    [Header("Events")]
    [Tooltip("Optional event published when this need becomes low. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition lowEvent;
    [Tooltip("Optional event published when this need becomes critical. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition criticalEvent;
    [Tooltip("Optional event published when this need recovers from low or critical. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition recoveredEvent;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public int MaxValue => Mathf.Max(1, maxValue);
    public int LowThreshold => Mathf.Clamp(lowThreshold, 0, MaxValue);
    public int CriticalThreshold => Mathf.Clamp(criticalThreshold, 0, LowThreshold);
    public int HourlyDecay => hourlyDecay;
    public int HourlyRestGain => hourlyRestGain;
    public int HourlySleepGain => hourlySleepGain;
    public GameEventDefinition LowEvent => lowEvent;
    public GameEventDefinition CriticalEvent => criticalEvent;
    public GameEventDefinition RecoveredEvent => recoveredEvent;
}
