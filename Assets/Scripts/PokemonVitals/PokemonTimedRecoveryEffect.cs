using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PokemonTimedRecoveryEffect {
    [Tooltip("Stable source id for debugging/status output, usually the item id/name.")]
    public string sourceId;
    [Tooltip("Display name shown in status/debug messages.")]
    public string sourceName;
    [Tooltip("Remaining Pokemon turns. 0 means this effect is not turn-limited.")]
    [Min(0)]
    public int remainingTurns;
    [Tooltip("Remaining battles. 0 means this effect is not battle-limited.")]
    [Min(0)]
    public int remainingBattles;
    [Tooltip("Multiplier applied to positive HP healing received while this effect is active. 1 means unchanged.")]
    [Min(0f)]
    public float healingReceivedMultiplier = 1f;
    [Tooltip("Multiplier applied to positive core/battle vital recovery received while this effect is active. 1 means unchanged.")]
    [Min(0f)]
    public float vitalRecoveryMultiplier = 1f;
    [Tooltip("Percent of Max HP restored at the end of each Pokemon turn.")]
    [Range(0f, 1f)]
    public float hpPercentPerTurn;
    [Tooltip("Percent of max core health restored at the end of each Pokemon turn.")]
    [Range(0f, 1f)]
    public float coreHealthPercentPerTurn;
    [Tooltip("Percent of max core physical stamina restored at the end of each Pokemon turn.")]
    [Range(0f, 1f)]
    public float corePhysicalStaminaPercentPerTurn;
    [Tooltip("Percent of max core elemental stamina restored at the end of each Pokemon turn.")]
    [Range(0f, 1f)]
    public float coreElementalStaminaPercentPerTurn;
    [Tooltip("Percent of max battle physical stamina restored at the end of each Pokemon turn.")]
    [Range(0f, 1f)]
    public float battlePhysicalStaminaPercentPerTurn;
    [Tooltip("Percent of max battle elemental stamina restored at the end of each Pokemon turn.")]
    [Range(0f, 1f)]
    public float battleElementalStaminaPercentPerTurn;
    [Tooltip("Temporary stat modifiers active while this effect remains.")]
    public List<PowerMechanicStatModifier> statModifiers = new List<PowerMechanicStatModifier>();

    public bool IsExpired => remainingTurns == 0 && remainingBattles == 0;
    public bool HasTurnDuration => remainingTurns > 0;
    public bool HasBattleDuration => remainingBattles > 0;
    public bool HasStatModifiers => statModifiers != null && statModifiers.Count > 0;
    public bool HasEndTurnRecovery => hpPercentPerTurn > 0f
        || coreHealthPercentPerTurn > 0f
        || corePhysicalStaminaPercentPerTurn > 0f
        || coreElementalStaminaPercentPerTurn > 0f
        || battlePhysicalStaminaPercentPerTurn > 0f
        || battleElementalStaminaPercentPerTurn > 0f;

    public PokemonTimedRecoveryEffect() {
    }

    public PokemonTimedRecoveryEffect(
        string sourceId,
        string sourceName,
        int remainingTurns,
        int remainingBattles,
        float healingReceivedMultiplier,
        float vitalRecoveryMultiplier,
        float hpPercentPerTurn,
        float coreHealthPercentPerTurn,
        float corePhysicalStaminaPercentPerTurn,
        float coreElementalStaminaPercentPerTurn,
        float battlePhysicalStaminaPercentPerTurn,
        float battleElementalStaminaPercentPerTurn,
        IReadOnlyList<PowerMechanicStatModifier> statModifiers) {
        this.sourceId = sourceId;
        this.sourceName = sourceName;
        this.remainingTurns = Mathf.Max(0, remainingTurns);
        this.remainingBattles = Mathf.Max(0, remainingBattles);
        this.healingReceivedMultiplier = Mathf.Max(0f, healingReceivedMultiplier);
        this.vitalRecoveryMultiplier = Mathf.Max(0f, vitalRecoveryMultiplier);
        this.hpPercentPerTurn = Mathf.Clamp01(hpPercentPerTurn);
        this.coreHealthPercentPerTurn = Mathf.Clamp01(coreHealthPercentPerTurn);
        this.corePhysicalStaminaPercentPerTurn = Mathf.Clamp01(corePhysicalStaminaPercentPerTurn);
        this.coreElementalStaminaPercentPerTurn = Mathf.Clamp01(coreElementalStaminaPercentPerTurn);
        this.battlePhysicalStaminaPercentPerTurn = Mathf.Clamp01(battlePhysicalStaminaPercentPerTurn);
        this.battleElementalStaminaPercentPerTurn = Mathf.Clamp01(battleElementalStaminaPercentPerTurn);
        this.statModifiers = statModifiers != null
            ? new List<PowerMechanicStatModifier>(statModifiers)
            : new List<PowerMechanicStatModifier>();
    }

    public int ApplyStat(Stat stat, int value) {
        if(statModifiers == null || statModifiers.Count == 0) {
            return value;
        }

        int modified = value;
        foreach(var modifier in statModifiers) {
            if(modifier != null) {
                modified = modifier.Apply(stat, modified);
            }
        }

        return modified;
    }

    public bool TickTurn(Pokemon pokemon, PokemonVitalProfileDefinition profile = null) {
        if(pokemon == null) {
            return false;
        }

        bool changed = ApplyEndTurnRecovery(pokemon, profile);
        if(remainingTurns > 0) {
            remainingTurns--;
            changed = true;
        }

        return changed;
    }

    public bool TickBattle() {
        if(remainingBattles <= 0) {
            return false;
        }

        remainingBattles--;
        return true;
    }

    bool ApplyEndTurnRecovery(Pokemon pokemon, PokemonVitalProfileDefinition profile) {
        bool changed = false;

        if(hpPercentPerTurn > 0f) {
            int amount = Mathf.Max(1, Mathf.RoundToInt(pokemon.MaxHp * hpPercentPerTurn));
            changed |= pokemon.IncreaseHPWithResult(amount);
        }

        changed |= RestorePercent(pokemon, PokemonVitalResourceKind.CoreHealth, coreHealthPercentPerTurn, profile);
        changed |= RestorePercent(pokemon, PokemonVitalResourceKind.CorePhysicalStamina, corePhysicalStaminaPercentPerTurn, profile);
        changed |= RestorePercent(pokemon, PokemonVitalResourceKind.CoreElementalStamina, coreElementalStaminaPercentPerTurn, profile);
        changed |= RestorePercent(pokemon, PokemonVitalResourceKind.BattlePhysicalStamina, battlePhysicalStaminaPercentPerTurn, profile);
        changed |= RestorePercent(pokemon, PokemonVitalResourceKind.BattleElementalStamina, battleElementalStaminaPercentPerTurn, profile);

        return changed;
    }

    bool RestorePercent(Pokemon pokemon, PokemonVitalResourceKind resource, float percent, PokemonVitalProfileDefinition profile) {
        if(percent <= 0f) {
            return false;
        }

        int amount = Mathf.Max(1, Mathf.RoundToInt(pokemon.GetVitalMax(resource, profile) * percent));
        return pokemon.ChangeVitalResource(resource, amount, profile) != 0;
    }
}
