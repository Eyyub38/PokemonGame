using System;
using UnityEngine;

public enum PokemonVitalResourceKind {
    CoreHealth,
    CorePhysicalStamina,
    CoreElementalStamina,
    BattlePhysicalStamina,
    BattleElementalStamina
}

public enum PokemonVitalBlockReason {
    None,
    CoreHealthDepleted,
    CoreStaminaDepleted,
    BattlePhysicalStaminaDepleted,
    BattleElementalStaminaDepleted
}

[CreateAssetMenu(menuName = "Pokemon/Vitals/Vital Profile")]
public class PokemonVitalProfileDefinition : ScriptableObject {
    [Header("Core Health")]
    [Tooltip("Maximum long-term health multiplier based on the Pokemon's battle Max HP.")]
    [Min(0.1f)]
    [SerializeField] float coreHealthMaxMultiplier = 1f;
    [Tooltip("Flat long-term health added after the Max HP multiplier.")]
    [Min(0)]
    [SerializeField] int coreHealthFlatBonus;
    [Tooltip("Battle damage must be at least this fraction of Max HP before it can also damage core health.")]
    [Range(0f, 1f)]
    [SerializeField] float coreDamageThresholdPercent = 0.35f;
    [Tooltip("Fraction of eligible battle damage converted into core health damage.")]
    [Range(0f, 1f)]
    [SerializeField] float coreDamageFromBattleDamagePercent = 0.15f;
    [Tooltip("Fraction of overkill damage converted into core health damage when HP reaches 0.")]
    [Range(0f, 1f)]
    [SerializeField] float coreDamageFromOverkillPercent = 0.35f;

    [Header("Core Stamina")]
    [Tooltip("Base long-term physical stamina before level/stat scaling.")]
    [Min(1)]
    [SerializeField] int corePhysicalStaminaBase = 40;
    [Tooltip("Physical stamina gained per Pokemon level.")]
    [Min(0f)]
    [SerializeField] float corePhysicalStaminaPerLevel = 2f;
    [Tooltip("Speed contribution to physical stamina.")]
    [Min(0f)]
    [SerializeField] float speedToPhysicalStaminaMultiplier = 0.25f;
    [Tooltip("Base long-term elemental stamina before level/stat scaling.")]
    [Min(1)]
    [SerializeField] int coreElementalStaminaBase = 35;
    [Tooltip("Elemental stamina gained per Pokemon level.")]
    [Min(0f)]
    [SerializeField] float coreElementalStaminaPerLevel = 2f;
    [Tooltip("Special Attack contribution to elemental stamina.")]
    [Min(0f)]
    [SerializeField] float specialAttackToElementalStaminaMultiplier = 0.2f;

    [Header("Battle Stamina")]
    [Tooltip("Battle physical stamina max as a fraction of core physical stamina.")]
    [Range(0.05f, 1f)]
    [SerializeField] float battlePhysicalStaminaCoreRatio = 0.35f;
    [Tooltip("Battle elemental stamina max as a fraction of core elemental stamina.")]
    [Range(0.05f, 1f)]
    [SerializeField] float battleElementalStaminaCoreRatio = 0.35f;
    [Tooltip("How much core physical stamina is spent to restore 1 battle physical stamina when preparing a battle.")]
    [Min(0f)]
    [SerializeField] float corePhysicalCostPerBattlePhysical = 0.25f;
    [Tooltip("How much core elemental stamina is spent to restore 1 battle elemental stamina when preparing a battle.")]
    [Min(0f)]
    [SerializeField] float coreElementalCostPerBattleElemental = 0.25f;

    [Header("Availability")]
    [Tooltip("If enabled, a Pokemon with 0 core health cannot be selected for battle/travel style systems that use vital checks.")]
    [SerializeField] bool coreHealthDepletionBlocksUse = true;
    [Tooltip("If enabled, a Pokemon with either core stamina pool at 0 cannot be selected for systems that require stamina.")]
    [SerializeField] bool coreStaminaDepletionBlocksUse = true;
    [Tooltip("Normalized threshold used by future UI to show low core health warnings.")]
    [Range(0f, 1f)]
    [SerializeField] float lowCoreHealthThreshold = 0.25f;
    [Tooltip("Normalized threshold used by future UI to show low stamina warnings.")]
    [Range(0f, 1f)]
    [SerializeField] float lowCoreStaminaThreshold = 0.25f;

    public float CoreHealthMaxMultiplier => Mathf.Max(0.1f, coreHealthMaxMultiplier);
    public int CoreHealthFlatBonus => Mathf.Max(0, coreHealthFlatBonus);
    public float CoreDamageThresholdPercent => Mathf.Clamp01(coreDamageThresholdPercent);
    public float CoreDamageFromBattleDamagePercent => Mathf.Clamp01(coreDamageFromBattleDamagePercent);
    public float CoreDamageFromOverkillPercent => Mathf.Clamp01(coreDamageFromOverkillPercent);
    public int CorePhysicalStaminaBase => Mathf.Max(1, corePhysicalStaminaBase);
    public float CorePhysicalStaminaPerLevel => Mathf.Max(0f, corePhysicalStaminaPerLevel);
    public float SpeedToPhysicalStaminaMultiplier => Mathf.Max(0f, speedToPhysicalStaminaMultiplier);
    public int CoreElementalStaminaBase => Mathf.Max(1, coreElementalStaminaBase);
    public float CoreElementalStaminaPerLevel => Mathf.Max(0f, coreElementalStaminaPerLevel);
    public float SpecialAttackToElementalStaminaMultiplier => Mathf.Max(0f, specialAttackToElementalStaminaMultiplier);
    public float BattlePhysicalStaminaCoreRatio => Mathf.Clamp(battlePhysicalStaminaCoreRatio, 0.05f, 1f);
    public float BattleElementalStaminaCoreRatio => Mathf.Clamp(battleElementalStaminaCoreRatio, 0.05f, 1f);
    public float CorePhysicalCostPerBattlePhysical => Mathf.Max(0f, corePhysicalCostPerBattlePhysical);
    public float CoreElementalCostPerBattleElemental => Mathf.Max(0f, coreElementalCostPerBattleElemental);
    public bool CoreHealthDepletionBlocksUse => coreHealthDepletionBlocksUse;
    public bool CoreStaminaDepletionBlocksUse => coreStaminaDepletionBlocksUse;
    public float LowCoreHealthThreshold => Mathf.Clamp01(lowCoreHealthThreshold);
    public float LowCoreStaminaThreshold => Mathf.Clamp01(lowCoreStaminaThreshold);

    public int GetMaxCoreHealth(Pokemon pokemon) {
        if(pokemon == null) {
            return Mathf.Max(1, CoreHealthFlatBonus);
        }

        return Mathf.Max(1, Mathf.RoundToInt(pokemon.MaxHp * CoreHealthMaxMultiplier) + CoreHealthFlatBonus);
    }

    public int GetMaxCorePhysicalStamina(Pokemon pokemon) {
        if(pokemon == null) {
            return CorePhysicalStaminaBase;
        }

        return Mathf.Max(1, Mathf.RoundToInt(CorePhysicalStaminaBase + pokemon.Level * CorePhysicalStaminaPerLevel + pokemon.Speed * SpeedToPhysicalStaminaMultiplier));
    }

    public int GetMaxCoreElementalStamina(Pokemon pokemon) {
        if(pokemon == null) {
            return CoreElementalStaminaBase;
        }

        return Mathf.Max(1, Mathf.RoundToInt(CoreElementalStaminaBase + pokemon.Level * CoreElementalStaminaPerLevel + pokemon.SpAttack * SpecialAttackToElementalStaminaMultiplier));
    }

    public int GetMaxBattlePhysicalStamina(Pokemon pokemon) {
        return Mathf.Max(1, Mathf.RoundToInt(GetMaxCorePhysicalStamina(pokemon) * BattlePhysicalStaminaCoreRatio));
    }

    public int GetMaxBattleElementalStamina(Pokemon pokemon) {
        return Mathf.Max(1, Mathf.RoundToInt(GetMaxCoreElementalStamina(pokemon) * BattleElementalStaminaCoreRatio));
    }

    public int CalculateCoreDamageFromBattleDamage(Pokemon pokemon, int battleDamage, int overkillDamage = 0, bool forceCoreDamage = false) {
        if(pokemon == null || battleDamage <= 0) {
            return 0;
        }

        bool passedThreshold = forceCoreDamage || battleDamage >= Mathf.CeilToInt(pokemon.MaxHp * CoreDamageThresholdPercent);
        if(!passedThreshold && overkillDamage <= 0) {
            return 0;
        }

        float coreDamage = passedThreshold ? battleDamage * CoreDamageFromBattleDamagePercent : 0f;
        if(overkillDamage > 0) {
            coreDamage += overkillDamage * CoreDamageFromOverkillPercent;
        }

        return Mathf.Max(0, Mathf.RoundToInt(coreDamage));
    }
}

[Serializable]
public class PokemonVitalState {
    [Tooltip("If false, current values are initialized from the Pokemon's stats the first time vital logic is used.")]
    public bool initialized;
    [Tooltip("Long-term health reserve. If this reaches 0, the Pokemon needs deeper treatment.")]
    public int coreHealth;
    [Tooltip("Long-term physical stamina reserve used to refill battle physical stamina.")]
    public int corePhysicalStamina;
    [Tooltip("Long-term elemental stamina reserve used to refill battle elemental stamina.")]
    public int coreElementalStamina;
    [Tooltip("Short-term physical stamina used inside a battle or intense activity.")]
    public int battlePhysicalStamina;
    [Tooltip("Short-term elemental stamina used inside a battle or intense activity.")]
    public int battleElementalStamina;

    public void Initialize(Pokemon pokemon, PokemonVitalProfileDefinition profile = null, bool fillToMax = true) {
        initialized = true;
        if(fillToMax) {
            coreHealth = PokemonVitalDefaults.GetMaxCoreHealth(pokemon, profile);
            corePhysicalStamina = PokemonVitalDefaults.GetMaxCorePhysicalStamina(pokemon, profile);
            coreElementalStamina = PokemonVitalDefaults.GetMaxCoreElementalStamina(pokemon, profile);
            battlePhysicalStamina = PokemonVitalDefaults.GetMaxBattlePhysicalStamina(pokemon, profile);
            battleElementalStamina = PokemonVitalDefaults.GetMaxBattleElementalStamina(pokemon, profile);
        }

        Clamp(pokemon, profile);
    }

    public void Clamp(Pokemon pokemon, PokemonVitalProfileDefinition profile = null) {
        coreHealth = Mathf.Clamp(coreHealth, 0, PokemonVitalDefaults.GetMaxCoreHealth(pokemon, profile));
        corePhysicalStamina = Mathf.Clamp(corePhysicalStamina, 0, PokemonVitalDefaults.GetMaxCorePhysicalStamina(pokemon, profile));
        coreElementalStamina = Mathf.Clamp(coreElementalStamina, 0, PokemonVitalDefaults.GetMaxCoreElementalStamina(pokemon, profile));
        battlePhysicalStamina = Mathf.Clamp(battlePhysicalStamina, 0, PokemonVitalDefaults.GetMaxBattlePhysicalStamina(pokemon, profile));
        battleElementalStamina = Mathf.Clamp(battleElementalStamina, 0, PokemonVitalDefaults.GetMaxBattleElementalStamina(pokemon, profile));
    }

    public PokemonVitalSaveData ToSaveData() {
        return new PokemonVitalSaveData {
            initialized = initialized,
            coreHealth = coreHealth,
            corePhysicalStamina = corePhysicalStamina,
            coreElementalStamina = coreElementalStamina,
            battlePhysicalStamina = battlePhysicalStamina,
            battleElementalStamina = battleElementalStamina
        };
    }

    public void Restore(PokemonVitalSaveData saveData) {
        if(saveData == null) {
            initialized = false;
            return;
        }

        initialized = saveData.initialized;
        coreHealth = saveData.coreHealth;
        corePhysicalStamina = saveData.corePhysicalStamina;
        coreElementalStamina = saveData.coreElementalStamina;
        battlePhysicalStamina = saveData.battlePhysicalStamina;
        battleElementalStamina = saveData.battleElementalStamina;
    }
}

[Serializable]
public class PokemonVitalSaveData {
    public bool initialized;
    public int coreHealth;
    public int corePhysicalStamina;
    public int coreElementalStamina;
    public int battlePhysicalStamina;
    public int battleElementalStamina;
}

[Serializable]
public class PokemonVitalChange {
    [Tooltip("Vital resource affected by this change.")]
    public PokemonVitalResourceKind resource;
    [Tooltip("Positive values restore, negative values consume or damage.")]
    public int amount;
    [Tooltip("If enabled, the final amount scales by Pokemon level using Level Multiplier.")]
    public bool scaleByLevel;
    [Tooltip("Amount added per Pokemon level when Scale By Level is enabled.")]
    public float levelMultiplier;

    public int GetAmount(Pokemon pokemon) {
        int finalAmount = amount;
        if(scaleByLevel && pokemon != null) {
            finalAmount += Mathf.RoundToInt(pokemon.Level * levelMultiplier);
        }

        return finalAmount;
    }

    public bool Apply(Pokemon pokemon, PokemonVitalProfileDefinition profile = null) {
        if(pokemon == null) {
            return false;
        }

        int finalAmount = GetAmount(pokemon);
        if(finalAmount == 0) {
            return false;
        }

        return pokemon.ChangeVitalResource(resource, finalAmount, profile) != 0;
    }
}

public static class PokemonVitalDefaults {
    public static int GetMaxCoreHealth(Pokemon pokemon, PokemonVitalProfileDefinition profile = null) {
        if(profile != null) {
            return profile.GetMaxCoreHealth(pokemon);
        }

        return Mathf.Max(1, pokemon != null ? pokemon.MaxHp : 1);
    }

    public static int GetMaxCorePhysicalStamina(Pokemon pokemon, PokemonVitalProfileDefinition profile = null) {
        if(profile != null) {
            return profile.GetMaxCorePhysicalStamina(pokemon);
        }

        if(pokemon == null) {
            return 40;
        }

        return Mathf.Max(1, Mathf.RoundToInt(40 + pokemon.Level * 2f + pokemon.Speed * 0.2f));
    }

    public static int GetMaxCoreElementalStamina(Pokemon pokemon, PokemonVitalProfileDefinition profile = null) {
        if(profile != null) {
            return profile.GetMaxCoreElementalStamina(pokemon);
        }

        if(pokemon == null) {
            return 35;
        }

        return Mathf.Max(1, Mathf.RoundToInt(35 + pokemon.Level * 2f + pokemon.SpAttack * 0.2f));
    }

    public static int GetMaxBattlePhysicalStamina(Pokemon pokemon, PokemonVitalProfileDefinition profile = null) {
        if(profile != null) {
            return profile.GetMaxBattlePhysicalStamina(pokemon);
        }

        return Mathf.Max(1, Mathf.RoundToInt(GetMaxCorePhysicalStamina(pokemon, null) * 0.35f));
    }

    public static int GetMaxBattleElementalStamina(Pokemon pokemon, PokemonVitalProfileDefinition profile = null) {
        if(profile != null) {
            return profile.GetMaxBattleElementalStamina(pokemon);
        }

        return Mathf.Max(1, Mathf.RoundToInt(GetMaxCoreElementalStamina(pokemon, null) * 0.35f));
    }

    public static int CalculateFallbackCoreDamage(Pokemon pokemon, int battleDamage, int overkillDamage = 0, bool forceCoreDamage = false) {
        if(pokemon == null || battleDamage <= 0) {
            return 0;
        }

        bool passedThreshold = forceCoreDamage || battleDamage >= Mathf.CeilToInt(pokemon.MaxHp * 0.35f);
        if(!passedThreshold && overkillDamage <= 0) {
            return 0;
        }

        float coreDamage = passedThreshold ? battleDamage * 0.15f : 0f;
        if(overkillDamage > 0) {
            coreDamage += overkillDamage * 0.35f;
        }

        return Mathf.Max(0, Mathf.RoundToInt(coreDamage));
    }
}
