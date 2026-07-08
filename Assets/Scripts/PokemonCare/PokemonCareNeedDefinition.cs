using UnityEngine;

public enum PokemonCareNeedState {
    Critical,
    Low,
    Normal,
    High
}

public enum PokemonCareNeedHourlyContext {
    Active,
    Resting,
    Sleeping
}

[CreateAssetMenu(menuName = "Pokemon Care/Care Need Definition")]
public class PokemonCareNeedDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this Pokemon care need. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in UI/debug messages. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer note or player-facing explanation for this care need.")]
    [TextArea]
    [SerializeField] string description;

    [Header("Values")]
    [Tooltip("Starting value used when a Pokemon does not have saved data for this need.")]
    [SerializeField] int defaultValue = 50;
    [Tooltip("Lowest allowed value for this need.")]
    [SerializeField] int minValue = 0;
    [Tooltip("Highest allowed value for this need.")]
    [SerializeField] int maxValue = 100;

    [Header("Thresholds")]
    [Tooltip("Value at or below which this care need counts as low.")]
    [SerializeField] int lowThreshold = 30;
    [Tooltip("Value at or below which this care need counts as critical.")]
    [SerializeField] int criticalThreshold = 10;

    [Header("Passive Changes")]
    [Tooltip("Amount applied each in-game hour while the Pokemon is active in the party. Negative values drain the need.")]
    [SerializeField] int hourlyActiveChange;
    [Tooltip("Amount applied each rest hour when the player rests. Negative values drain the need.")]
    [SerializeField] int hourlyRestChange;
    [Tooltip("Amount applied each sleep hour when the player sleeps. Negative values drain the need.")]
    [SerializeField] int hourlySleepChange;
    [Tooltip("If enabled, passive hourly changes can affect fainted Pokemon too.")]
    [SerializeField] bool passiveChangesAffectFaintedPokemon;

    [Header("Events")]
    [Tooltip("Optional event published when this care need becomes low. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition lowEvent;
    [Tooltip("Optional event published when this care need becomes critical. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition criticalEvent;
    [Tooltip("Optional event published when this care need recovers from low or critical. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition recoveredEvent;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public int DefaultValue => Mathf.Clamp(defaultValue, MinValue, MaxValue);
    public int MinValue => minValue;
    public int MaxValue => Mathf.Max(minValue + 1, maxValue);
    public int LowThreshold => Mathf.Clamp(lowThreshold, MinValue, MaxValue);
    public int CriticalThreshold => Mathf.Clamp(criticalThreshold, MinValue, LowThreshold);
    public int HourlyActiveChange => hourlyActiveChange;
    public int HourlyRestChange => hourlyRestChange;
    public int HourlySleepChange => hourlySleepChange;
    public bool PassiveChangesAffectFaintedPokemon => passiveChangesAffectFaintedPokemon;
    public GameEventDefinition LowEvent => lowEvent;
    public GameEventDefinition CriticalEvent => criticalEvent;
    public GameEventDefinition RecoveredEvent => recoveredEvent;

    public int GetHourlyChange(PokemonCareNeedHourlyContext context) {
        return context switch {
            PokemonCareNeedHourlyContext.Resting => hourlyRestChange,
            PokemonCareNeedHourlyContext.Sleeping => hourlySleepChange,
            _ => hourlyActiveChange
        };
    }

    public PokemonCareNeedState GetState(int value) {
        int clampedValue = Mathf.Clamp(value, MinValue, MaxValue);
        if(clampedValue <= CriticalThreshold) return PokemonCareNeedState.Critical;
        if(clampedValue <= LowThreshold) return PokemonCareNeedState.Low;
        if(clampedValue >= Mathf.RoundToInt(MaxValue * 0.85f)) return PokemonCareNeedState.High;
        return PokemonCareNeedState.Normal;
    }
}

[System.Serializable]
public class PokemonCareNeedChange {
    [Tooltip("Care need changed by this entry.")]
    public PokemonCareNeedDefinition need;
    [Tooltip("Amount to add. Negative values reduce the need.")]
    public int amount;
}

[System.Serializable]
public class PokemonCareNeedRequirement {
    [Tooltip("Care need checked by this requirement.")]
    public PokemonCareNeedDefinition need;
    [Tooltip("Minimum value required before the care action can apply.")]
    public int minimumValue;
    [Tooltip("Maximum value allowed before the care action can apply. 0 disables the maximum check.")]
    [Min(0)]
    public int maximumValue;
    [Tooltip("Message used when this need requirement blocks the care action.")]
    public string failureMessage;

    public bool IsMet(Pokemon pokemon, out string message) {
        message = null;
        if(pokemon == null || need == null) {
            return true;
        }

        int value = pokemon.GetCareNeedValue(need);
        if(value < minimumValue) {
            message = string.IsNullOrWhiteSpace(failureMessage) ? $"{pokemon.NickName} needs more {need.DisplayName}." : failureMessage;
            return false;
        }

        if(maximumValue > 0 && value > maximumValue) {
            message = string.IsNullOrWhiteSpace(failureMessage) ? $"{pokemon.NickName} has too much {need.DisplayName} for this." : failureMessage;
            return false;
        }

        return true;
    }
}

[System.Serializable]
public class PokemonEffortValueReward {
    [Tooltip("Stat effort value to increase.")]
    public Stat stat = Stat.Attack;
    [Tooltip("EV amount to add. Existing GlobalSettings caps still apply.")]
    [Min(0)]
    public int amount;
}
