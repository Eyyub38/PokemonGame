using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum PokemonGrowthSource {
    General,
    Battle,
    Care,
    Training,
    Assignment,
    Camp,
    Food,
    Research,
    Contest,
    Travel,
    Custom
}

[CreateAssetMenu(menuName = "Pokemon/Core Growth/Growth Profile")]
public class PokemonGrowthProfileDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable id saved into Pokemon growth state. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in debug output or future UI. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note explaining the intended growth model, such as wild, starter, bred, trained or legendary.")]
    [TextArea]
    [SerializeField] string description = string.Empty;

    [Header("Potential")]
    [Tooltip("Potential rolls applied once when this profile initializes a Pokemon. These replace classic IV-style hidden values with editable per-stat multipliers.")]
    [SerializeField] List<PokemonPotentialRoll> potentialRolls = new List<PokemonPotentialRoll>();
    [Tooltip("Flat fallback minimum potential multiplier used when a stat has no explicit roll entry.")]
    [Range(-0.5f, 0.5f)]
    [SerializeField] float defaultPotentialMinMultiplier;
    [Tooltip("Flat fallback maximum potential multiplier used when a stat has no explicit roll entry.")]
    [Range(-0.5f, 0.5f)]
    [SerializeField] float defaultPotentialMaxMultiplier = 0.05f;

    [Header("Training")]
    [Tooltip("Training rules that convert earned training points into stat bonuses.")]
    [SerializeField] List<PokemonTrainingStatRule> trainingRules = new List<PokemonTrainingStatRule>();
    [Tooltip("Maximum total training points across all stats. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int totalTrainingCap = 510;
    [Tooltip("Maximum training points per stat when that stat has no explicit rule. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int defaultTrainingCapPerStat = 100;
    [Tooltip("Stat points granted per training point when that stat has no explicit rule.")]
    [Min(0f)]
    [SerializeField] float defaultStatBonusPerTrainingPoint = 0.02f;

    [Header("Starting Traits")]
    [Tooltip("Traits that may be applied during initialization. Chance is rolled independently for each entry.")]
    [SerializeField] List<PokemonStartingTraitRoll> startingTraits = new List<PokemonStartingTraitRoll>();

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public IReadOnlyList<PokemonPotentialRoll> PotentialRolls => potentialRolls != null ? potentialRolls : Array.Empty<PokemonPotentialRoll>();
    public float DefaultPotentialMinMultiplier => Mathf.Min(defaultPotentialMinMultiplier, defaultPotentialMaxMultiplier);
    public float DefaultPotentialMaxMultiplier => Mathf.Max(defaultPotentialMinMultiplier, defaultPotentialMaxMultiplier);
    public IReadOnlyList<PokemonTrainingStatRule> TrainingRules => trainingRules != null ? trainingRules : Array.Empty<PokemonTrainingStatRule>();
    public int TotalTrainingCap => Mathf.Max(0, totalTrainingCap);
    public int DefaultTrainingCapPerStat => Mathf.Max(0, defaultTrainingCapPerStat);
    public float DefaultStatBonusPerTrainingPoint => Mathf.Max(0f, defaultStatBonusPerTrainingPoint);
    public IReadOnlyList<PokemonStartingTraitRoll> StartingTraits => startingTraits != null ? startingTraits : Array.Empty<PokemonStartingTraitRoll>();

    public PokemonGrowthState CreateState(Pokemon pokemon) {
        var state = new PokemonGrowthState {
            initialized = true,
            profileId = Id,
            profileName = DisplayName
        };

        foreach(var stat in PokemonGrowthUtility.CoreStats) {
            var roll = FindPotentialRoll(stat);
            state.SetPotential(stat, roll.RollMultiplier(), roll.FlatBonus);
            state.SetTrainingRule(stat, GetStatBonusPerTrainingPoint(stat));
        }

        foreach(var traitRoll in StartingTraits) {
            if(traitRoll != null && traitRoll.ShouldApply()) {
                traitRoll.Trait?.ApplyTo(state, "profile-start");
            }
        }

        state.ClampTraining(this);
        return state;
    }

    public int GetTrainingCap(Stat stat) {
        var rule = FindTrainingRule(stat);
        return rule != null ? rule.TrainingCap : DefaultTrainingCapPerStat;
    }

    public float GetStatBonusPerTrainingPoint(Stat stat) {
        var rule = FindTrainingRule(stat);
        return rule != null ? rule.StatBonusPerTrainingPoint : DefaultStatBonusPerTrainingPoint;
    }

    PokemonPotentialRoll FindPotentialRoll(Stat stat) {
        return PotentialRolls.FirstOrDefault(roll => roll != null && roll.Stat == stat)
            ?? new PokemonPotentialRoll(stat, DefaultPotentialMinMultiplier, DefaultPotentialMaxMultiplier, 0);
    }

    PokemonTrainingStatRule FindTrainingRule(Stat stat) {
        return TrainingRules.FirstOrDefault(rule => rule != null && rule.Stat == stat);
    }
}

[CreateAssetMenu(menuName = "Pokemon/Core Growth/Passive Trait")]
public class PokemonPassiveTraitDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable id saved into Pokemon growth state. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in debug output or future UI. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note explaining what this trait represents.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Free-form tags such as battle, care, travel, shy, brave, aquatic, starter or rare.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Stat Effects")]
    [Tooltip("Stat modifiers copied into the Pokemon's growth state when this trait is applied.")]
    [SerializeField] List<PokemonGrowthStatModifier> statModifiers = new List<PokemonGrowthStatModifier>();

    [Header("General Effects")]
    [Tooltip("Multiplier applied by helper methods when this Pokemon gains friendship through growth-aware systems.")]
    [Min(0f)]
    [SerializeField] float friendshipGainMultiplier = 1f;
    [Tooltip("Multiplier applied by helper methods when this Pokemon gains experience through growth-aware systems.")]
    [Min(0f)]
    [SerializeField] float experienceGainMultiplier = 1f;
    [Tooltip("Flat bonus that future care systems can read when calculating care results.")]
    [SerializeField] int careBonus;
    [Tooltip("Flat bonus that future assignment systems can read when calculating assignment success.")]
    [SerializeField] int assignmentBonus;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public IReadOnlyList<string> Tags => tags != null ? tags : Array.Empty<string>();
    public IReadOnlyList<PokemonGrowthStatModifier> StatModifiers => statModifiers != null ? statModifiers : Array.Empty<PokemonGrowthStatModifier>();
    public float FriendshipGainMultiplier => Mathf.Max(0f, friendshipGainMultiplier);
    public float ExperienceGainMultiplier => Mathf.Max(0f, experienceGainMultiplier);
    public int CareBonus => careBonus;
    public int AssignmentBonus => assignmentBonus;

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    public void ApplyTo(PokemonGrowthState state, string sourceId = null) {
        if(state == null) {
            return;
        }

        state.AddTrait(this, sourceId);
    }
}

[Serializable]
public class PokemonPotentialRoll {
    [Tooltip("Stat affected by this potential roll.")]
    [SerializeField] Stat stat = Stat.Attack;
    [Tooltip("Minimum multiplier roll. 0.05 means +5 percent.")]
    [Range(-0.5f, 0.5f)]
    [SerializeField] float minMultiplier;
    [Tooltip("Maximum multiplier roll. 0.10 means +10 percent.")]
    [Range(-0.5f, 0.5f)]
    [SerializeField] float maxMultiplier = 0.05f;
    [Tooltip("Flat stat bonus applied in addition to the multiplier.")]
    [SerializeField] int flatBonus;

    public Stat Stat => stat;
    public float MinMultiplier => Mathf.Min(minMultiplier, maxMultiplier);
    public float MaxMultiplier => Mathf.Max(minMultiplier, maxMultiplier);
    public int FlatBonus => flatBonus;

    public PokemonPotentialRoll() {
    }

    public PokemonPotentialRoll(Stat stat, float minMultiplier, float maxMultiplier, int flatBonus) {
        this.stat = stat;
        this.minMultiplier = minMultiplier;
        this.maxMultiplier = maxMultiplier;
        this.flatBonus = flatBonus;
    }

    public float RollMultiplier() {
        return UnityEngine.Random.Range(MinMultiplier, MaxMultiplier);
    }
}

[Serializable]
public class PokemonTrainingStatRule {
    [Tooltip("Stat affected by this training rule.")]
    [SerializeField] Stat stat = Stat.Attack;
    [Tooltip("Maximum training points that can be invested in this stat. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int trainingCap = 100;
    [Tooltip("Stat points gained per training point.")]
    [Min(0f)]
    [SerializeField] float statBonusPerTrainingPoint = 0.02f;

    public Stat Stat => stat;
    public int TrainingCap => Mathf.Max(0, trainingCap);
    public float StatBonusPerTrainingPoint => Mathf.Max(0f, statBonusPerTrainingPoint);
}

[Serializable]
public class PokemonStartingTraitRoll {
    [Tooltip("Trait that may be applied when the growth profile initializes a Pokemon.")]
    [SerializeField] PokemonPassiveTraitDefinition trait;
    [Tooltip("Chance that this trait is applied during initialization.")]
    [Range(0f, 1f)]
    [SerializeField] float chance = 1f;

    public PokemonPassiveTraitDefinition Trait => trait;
    public float Chance => Mathf.Clamp01(chance);

    public bool ShouldApply() {
        return trait != null && UnityEngine.Random.value <= Chance;
    }
}

[Serializable]
public class PokemonGrowthStatModifier {
    [Tooltip("Stat affected by this modifier.")]
    public Stat stat = Stat.Attack;
    [Tooltip("Flat stat amount added after potential/training calculations.")]
    public int flatBonus;
    [Tooltip("Multiplier added on top of 1. 0.10 means +10 percent.")]
    public float multiplierBonus;
    [Tooltip("Source id saved for debug output, such as a trait id, item id or training id.")]
    public string sourceId;
    [Tooltip("Human-readable source name saved for debug output.")]
    public string sourceName;

    public PokemonGrowthStatModifier Clone() {
        return new PokemonGrowthStatModifier {
            stat = stat,
            flatBonus = flatBonus,
            multiplierBonus = multiplierBonus,
            sourceId = sourceId,
            sourceName = sourceName
        };
    }
}

[Serializable]
public class PokemonGrowthState {
    [Tooltip("If false, growth values have not been initialized from a growth profile yet.")]
    public bool initialized;
    [Tooltip("Growth profile id used when this state was initialized.")]
    public string profileId;
    [Tooltip("Growth profile display name saved for fallback/debug output.")]
    public string profileName;
    [Tooltip("Rolled potential values per stat.")]
    public List<PokemonPotentialValue> potentialValues = new List<PokemonPotentialValue>();
    [Tooltip("Training points earned per stat.")]
    public List<PokemonTrainingValue> trainingValues = new List<PokemonTrainingValue>();
    [Tooltip("Applied passive trait records.")]
    public List<PokemonGrowthTraitRecord> traits = new List<PokemonGrowthTraitRecord>();
    [Tooltip("Flat/multiplier stat modifiers copied from traits, items or other growth systems.")]
    public List<PokemonGrowthStatModifier> statModifiers = new List<PokemonGrowthStatModifier>();
    [Tooltip("Training history records for future UI/debugging.")]
    public List<PokemonGrowthTrainingRecord> trainingRecords = new List<PokemonGrowthTrainingRecord>();

    public int GetTraining(Stat stat) {
        return trainingValues?.FirstOrDefault(value => value != null && value.stat == stat)?.points ?? 0;
    }

    public int GetTotalTraining() {
        return trainingValues != null ? trainingValues.Where(value => value != null).Sum(value => Mathf.Max(0, value.points)) : 0;
    }

    public float GetPotentialMultiplier(Stat stat) {
        return potentialValues?.FirstOrDefault(value => value != null && value.stat == stat)?.multiplierBonus ?? 0f;
    }

    public int GetPotentialFlatBonus(Stat stat) {
        return potentialValues?.FirstOrDefault(value => value != null && value.stat == stat)?.flatBonus ?? 0;
    }

    public float GetStatMultiplierBonus(Stat stat) {
        float bonus = GetPotentialMultiplier(stat);
        if(statModifiers != null) {
            bonus += statModifiers.Where(modifier => modifier != null && modifier.stat == stat).Sum(modifier => modifier.multiplierBonus);
        }
        return bonus;
    }

    public int GetFlatStatBonus(Stat stat, PokemonGrowthProfileDefinition profile = null) {
        int bonus = GetPotentialFlatBonus(stat);
        bonus += Mathf.RoundToInt(GetTraining(stat) * GetTrainingStatBonusPerPoint(stat, profile));

        if(statModifiers != null) {
            bonus += statModifiers.Where(modifier => modifier != null && modifier.stat == stat).Sum(modifier => modifier.flatBonus);
        }

        return bonus;
    }

    public void SetPotential(Stat stat, float multiplierBonus, int flatBonus) {
        if(potentialValues == null) {
            potentialValues = new List<PokemonPotentialValue>();
        }

        var value = potentialValues.FirstOrDefault(entry => entry != null && entry.stat == stat);
        if(value == null) {
            value = new PokemonPotentialValue { stat = stat };
            potentialValues.Add(value);
        }

        value.multiplierBonus = multiplierBonus;
        value.flatBonus = flatBonus;
    }

    public int AddTraining(Stat stat, int points, PokemonGrowthProfileDefinition profile, PokemonGrowthSource source, string sourceId, string sourceName) {
        if(points <= 0) {
            return 0;
        }

        if(trainingValues == null) {
            trainingValues = new List<PokemonTrainingValue>();
        }

        int statCap = profile != null ? profile.GetTrainingCap(stat) : 0;
        int totalCap = profile != null ? profile.TotalTrainingCap : 0;
        int allowed = points;
        if(statCap > 0) {
            allowed = Mathf.Min(allowed, Mathf.Max(0, statCap - GetTraining(stat)));
        }

        if(totalCap > 0) {
            allowed = Mathf.Min(allowed, Mathf.Max(0, totalCap - GetTotalTraining()));
        }

        if(allowed <= 0) {
            return 0;
        }

        var value = trainingValues.FirstOrDefault(entry => entry != null && entry.stat == stat);
        if(value == null) {
            value = new PokemonTrainingValue { stat = stat };
            trainingValues.Add(value);
        }

        value.statBonusPerPoint = GetTrainingStatBonusPerPoint(stat, profile);
        value.points += allowed;
        RecordTraining(stat, allowed, source, sourceId, sourceName);
        return allowed;
    }

    public void SetTrainingRule(Stat stat, float statBonusPerPoint) {
        if(trainingValues == null) {
            trainingValues = new List<PokemonTrainingValue>();
        }

        var value = trainingValues.FirstOrDefault(entry => entry != null && entry.stat == stat);
        if(value == null) {
            value = new PokemonTrainingValue { stat = stat };
            trainingValues.Add(value);
        }

        value.statBonusPerPoint = Mathf.Max(0f, statBonusPerPoint);
    }

    public void AddTrait(PokemonPassiveTraitDefinition trait, string sourceId = null) {
        if(trait == null) {
            return;
        }

        if(traits == null) {
            traits = new List<PokemonGrowthTraitRecord>();
        }

        if(traits.Any(record => record != null && string.Equals(record.traitId, trait.Id, StringComparison.OrdinalIgnoreCase))) {
            return;
        }

        traits.Add(new PokemonGrowthTraitRecord {
            traitId = trait.Id,
            traitName = trait.DisplayName,
            sourceId = sourceId
        });

        if(statModifiers == null) {
            statModifiers = new List<PokemonGrowthStatModifier>();
        }

        foreach(var modifier in trait.StatModifiers) {
            if(modifier == null) {
                continue;
            }

            var clone = modifier.Clone();
            clone.sourceId = string.IsNullOrWhiteSpace(clone.sourceId) ? trait.Id : clone.sourceId;
            clone.sourceName = string.IsNullOrWhiteSpace(clone.sourceName) ? trait.DisplayName : clone.sourceName;
            statModifiers.Add(clone);
        }
    }

    public bool HasTrait(string traitId) {
        return !string.IsNullOrWhiteSpace(traitId)
            && traits != null
            && traits.Any(record => record != null && string.Equals(record.traitId, traitId, StringComparison.OrdinalIgnoreCase));
    }

    public void ClampTraining(PokemonGrowthProfileDefinition profile) {
        if(profile == null || trainingValues == null) {
            return;
        }

        foreach(var value in trainingValues.Where(value => value != null)) {
            int cap = profile.GetTrainingCap(value.stat);
            if(cap > 0) {
                value.points = Mathf.Clamp(value.points, 0, cap);
            }
        }

        int totalCap = profile.TotalTrainingCap;
        if(totalCap <= 0) {
            return;
        }

        int overflow = GetTotalTraining() - totalCap;
        for(int i = trainingValues.Count - 1; i >= 0 && overflow > 0; i--) {
            var value = trainingValues[i];
            if(value == null || value.points <= 0) {
                continue;
            }

            int remove = Mathf.Min(overflow, value.points);
            value.points -= remove;
            overflow -= remove;
        }
    }

    void RecordTraining(Stat stat, int points, PokemonGrowthSource source, string sourceId, string sourceName) {
        if(trainingRecords == null) {
            trainingRecords = new List<PokemonGrowthTrainingRecord>();
        }

        trainingRecords.Add(new PokemonGrowthTrainingRecord {
            stat = stat,
            points = points,
            source = source,
            sourceId = sourceId,
            sourceName = sourceName,
            day = TimeSystem.i != null ? TimeSystem.i.Day : 0,
            absoluteHour = TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0
        });

        if(trainingRecords.Count > 60) {
            trainingRecords.RemoveAt(0);
        }
    }

    float GetTrainingStatBonusPerPoint(Stat stat, PokemonGrowthProfileDefinition profile) {
        if(profile != null) {
            return profile.GetStatBonusPerTrainingPoint(stat);
        }

        var value = trainingValues?.FirstOrDefault(entry => entry != null && entry.stat == stat);
        return value != null ? Mathf.Max(0f, value.statBonusPerPoint) : 0f;
    }
}

[Serializable]
public class PokemonPotentialValue {
    [Tooltip("Stat affected by this potential value.")]
    public Stat stat;
    [Tooltip("Permanent multiplier bonus. 0.05 means +5 percent.")]
    public float multiplierBonus;
    [Tooltip("Permanent flat bonus.")]
    public int flatBonus;
}

[Serializable]
public class PokemonTrainingValue {
    [Tooltip("Stat affected by this training value.")]
    public Stat stat;
    [Tooltip("Training points invested into this stat.")]
    public int points;
    [Tooltip("Stat bonus gained per training point. Saved so training keeps working after the profile asset is not directly available.")]
    public float statBonusPerPoint;
}

[Serializable]
public class PokemonGrowthTraitRecord {
    [Tooltip("Applied trait id.")]
    public string traitId;
    [Tooltip("Applied trait display name saved for fallback/debug output.")]
    public string traitName;
    [Tooltip("Source id that granted the trait.")]
    public string sourceId;
}

[Serializable]
public class PokemonGrowthTrainingRecord {
    [Tooltip("Stat trained by this record.")]
    public Stat stat;
    [Tooltip("Training points gained.")]
    public int points;
    [Tooltip("Broad source type for this training gain.")]
    public PokemonGrowthSource source;
    [Tooltip("Specific source id, such as an activity, care action, assignment or item.")]
    public string sourceId;
    [Tooltip("Specific source name saved for fallback/debug output.")]
    public string sourceName;
    [Tooltip("In-game day when this training was recorded.")]
    public int day;
    [Tooltip("Absolute in-game hour when this training was recorded.")]
    public int absoluteHour;
}

[Serializable]
public class PokemonGrowthTrainingReward {
    [Tooltip("Stat that receives training points.")]
    public Stat stat = Stat.Attack;
    [Tooltip("Training points granted before bonus values are added.")]
    [Min(0)]
    public int points = 1;
    [Tooltip("Broad source used in the Pokemon's training history.")]
    public PokemonGrowthSource source = PokemonGrowthSource.Training;
    [Tooltip("If enabled, the reward is ignored unless the Pokemon growth state has already been initialized.")]
    public bool requireInitializedGrowth;

    public int Apply(Pokemon pokemon, PokemonGrowthProfileDefinition profile, int bonus, string sourceId, string sourceName) {
        if(pokemon == null || points <= 0) {
            return 0;
        }

        if(requireInitializedGrowth && (pokemon.GrowthState == null || !pokemon.GrowthState.initialized)) {
            return 0;
        }

        return pokemon.GainGrowthTraining(stat, points + Mathf.Max(0, bonus), profile, source, sourceId, sourceName);
    }
}

public static class PokemonGrowthUtility {
    public static readonly Stat[] CoreStats = {
        Stat.HitPoints,
        Stat.Attack,
        Stat.Defense,
        Stat.SpAttack,
        Stat.SpDefense,
        Stat.Speed
    };
}
