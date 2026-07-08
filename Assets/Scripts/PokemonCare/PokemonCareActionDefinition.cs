using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Pokemon Care/Care Action Definition")]
public class PokemonCareActionDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this care action. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in UI/debug messages. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer note or player-facing description for this care action.")]
    [TextArea][SerializeField] string description;
    [Tooltip("Broad care category used by history, requirements and future UI filters.")]
    [SerializeField] PokemonCareCategory category = PokemonCareCategory.General;
    [Tooltip("Free-form tags used by requirements, validators and future UI filters.")]
    [SerializeField] List<string> tags = new List<string>();
    [Header("Activity")]
    [Tooltip("Activity definition that gates costs, requirements, XP and rewards for this care action.")]
    [SerializeField] ActivityDefinition activity;
    [Header("Repeat Rules")]
    [Tooltip("How many times this care action can be applied to the same Pokemon per in-game day. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int dailyLimitPerPokemon;
    [Tooltip("How many in-game hours must pass before this care action can be repeated on the same Pokemon. 0 means no cooldown.")]
    [Min(0)]
    [SerializeField] int cooldownHoursPerPokemon;
    [Tooltip("Optional custom message shown when repeat rules block this care action.")]
    [SerializeField] string repeatBlockedMessage;
    [Header("Pokemon Effects")]
    [Tooltip("Friendship gained by each affected Pokemon.")]
    [Min(0)]
    [SerializeField] int friendshipGain = 2;
    [Tooltip("HP restored to each affected Pokemon. 0 means no healing.")]
    [Min(0)]
    [SerializeField] int healAmount;
    [Tooltip("If enabled, this care action can be applied even when battle HP is 0.")]
    [SerializeField] bool allowFaintedPokemon;
    [Tooltip("If enabled, clears regular and volatile status conditions.")]
    [SerializeField] bool cureStatus;
    [Tooltip("Experience granted to each affected Pokemon. 0 means none.")]
    [Min(0)]
    [SerializeField] int pokemonExperienceGain;
    [Header("Vital Resource Effects")]
    [Tooltip("Vital profile used when calculating max core health/stamina and battle stamina values. Empty uses default formulas.")]
    [SerializeField] PokemonVitalProfileDefinition vitalProfile;
    [Tooltip("If enabled, restores core health, core stamina and battle stamina to full.")]
    [SerializeField] bool restoreAllVitalsToFull;
    [Tooltip("If enabled, restores only core health and core stamina to full.")]
    [SerializeField] bool restoreCoreVitalsToFull;
    [Tooltip("If enabled, restores only battle stamina to full.")]
    [SerializeField] bool restoreBattleVitalsToFull;
    [Tooltip("Fine-grained vital resource changes. Positive restores, negative drains/damages.")]
    [SerializeField] List<PokemonVitalChange> vitalChanges = new List<PokemonVitalChange>();
    [Tooltip("Mood changes applied to each affected Pokemon.")]
    [SerializeField] List<PokemonMoodChange> moodChanges = new List<PokemonMoodChange>();
    [Tooltip("Care need requirements checked before this action can affect a Pokemon.")]
    [SerializeField] List<PokemonCareNeedRequirement> careNeedRequirements = new List<PokemonCareNeedRequirement>();
    [Tooltip("Care need changes applied to each affected Pokemon.")]
    [SerializeField] List<PokemonCareNeedChange> careNeedChanges = new List<PokemonCareNeedChange>();
    [Tooltip("Effort values granted to each affected Pokemon. Existing global EV caps still apply.")]
    [SerializeField] List<PokemonEffortValueReward> effortValueRewards = new List<PokemonEffortValueReward>();
    [Tooltip("Growth profile used by growth training rewards. Empty still records training, but stat bonus per point is only available if the Pokemon already saved one.")]
    [SerializeField] PokemonGrowthProfileDefinition growthProfile;
    [Tooltip("Growth training rewards granted to each affected Pokemon, such as care, exercise or battle drill progress.")]
    [SerializeField] List<PokemonGrowthTrainingReward> growthTrainingRewards = new List<PokemonGrowthTrainingReward>();
    [Tooltip("If enabled, successful use is stored in the Pokemon's care history.")] 
    [SerializeField] bool recordCareHistory = true;
    [Header("Events")]
    [Tooltip("Optional event published when this care action completes. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition completedEvent;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public PokemonCareCategory Category => category;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : System.Array.Empty<string>();
    public ActivityDefinition Activity => activity;
    public int DailyLimitPerPokemon => Mathf.Max(0, dailyLimitPerPokemon);
    public int CooldownHoursPerPokemon => Mathf.Max(0, cooldownHoursPerPokemon);
    public int FriendshipGain => friendshipGain;
    public int HealAmount => healAmount;
    public bool AllowFaintedPokemon => allowFaintedPokemon;
    public bool CureStatus => cureStatus;
    public int PokemonExperienceGain => pokemonExperienceGain;
    public PokemonVitalProfileDefinition VitalProfile => vitalProfile;
    public bool RestoreAllVitalsToFull => restoreAllVitalsToFull;
    public bool RestoreCoreVitalsToFull => restoreCoreVitalsToFull;
    public bool RestoreBattleVitalsToFull => restoreBattleVitalsToFull;
    public IReadOnlyList<PokemonVitalChange> VitalChanges => vitalChanges != null ? (IReadOnlyList<PokemonVitalChange>)vitalChanges : System.Array.Empty<PokemonVitalChange>();
    public IReadOnlyList<PokemonMoodChange> MoodChanges => moodChanges != null ? (IReadOnlyList<PokemonMoodChange>)moodChanges : System.Array.Empty<PokemonMoodChange>();
    public IReadOnlyList<PokemonCareNeedRequirement> CareNeedRequirements => careNeedRequirements != null ? (IReadOnlyList<PokemonCareNeedRequirement>)careNeedRequirements : System.Array.Empty<PokemonCareNeedRequirement>();
    public IReadOnlyList<PokemonCareNeedChange> CareNeedChanges => careNeedChanges != null ? (IReadOnlyList<PokemonCareNeedChange>)careNeedChanges : System.Array.Empty<PokemonCareNeedChange>();
    public IReadOnlyList<PokemonEffortValueReward> EffortValueRewards => effortValueRewards != null ? (IReadOnlyList<PokemonEffortValueReward>)effortValueRewards : System.Array.Empty<PokemonEffortValueReward>();
    public PokemonGrowthProfileDefinition GrowthProfile => growthProfile;
    public IReadOnlyList<PokemonGrowthTrainingReward> GrowthTrainingRewards => growthTrainingRewards != null ? (IReadOnlyList<PokemonGrowthTrainingReward>)growthTrainingRewards : System.Array.Empty<PokemonGrowthTrainingReward>();
    public GameEventDefinition CompletedEvent => completedEvent;

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && tags != null
            && tags.Any(entry => string.Equals(entry, tag, System.StringComparison.OrdinalIgnoreCase));
    }

    public bool CanApply(Pokemon pokemon, out string failureMessage) {
        failureMessage = null;
        if(pokemon == null) {
            failureMessage = "No Pokemon selected.";
            return false;
        }

        if(pokemon.HP <= 0 && !allowFaintedPokemon) {
            failureMessage = $"{pokemon.NickName} cannot receive care right now.";
            return false;
        }

        if(DailyLimitPerPokemon > 0 && GetTodayCount(pokemon) >= DailyLimitPerPokemon) {
            failureMessage = string.IsNullOrWhiteSpace(repeatBlockedMessage)
                ? $"{pokemon.NickName} has already received {DisplayName} today."
                : repeatBlockedMessage;
            return false;
        }

        if(CooldownHoursPerPokemon > 0) {
            int hours = pokemon.GetHoursSinceLastCare(this);
            if(hours >= 0 && hours < CooldownHoursPerPokemon) {
                failureMessage = string.IsNullOrWhiteSpace(repeatBlockedMessage)
                    ? $"{pokemon.NickName} needs {CooldownHoursPerPokemon - hours} more hour(s) before {DisplayName}."
                    : repeatBlockedMessage;
                return false;
            }
        }

        foreach(var requirement in CareNeedRequirements) {
            if(requirement != null && !requirement.IsMet(pokemon, out failureMessage)) {
                return false;
            }
        }

        return true;
    }

    public void Apply(Pokemon pokemon, int bonus = 0) {
        TryApply(pokemon, bonus, null, out _);
    }

    public bool TryApply(Pokemon pokemon, int bonus, string sourceId, out string failureMessage) {
        if(!CanApply(pokemon, out failureMessage)) {
            return false;
        }

        if(pokemon == null || (pokemon.HP <= 0 && !allowFaintedPokemon)) {
            failureMessage = "No valid Pokemon selected.";
            return false;
        }

        pokemon.IncreaseFriendship(Mathf.Max(0, friendshipGain + bonus));

        foreach(var moodChange in MoodChanges) {
            if(moodChange == null || moodChange.mood == null) {
                continue;
            }

            var amount = moodChange.amount;
            if(amount > 0) {
                amount += bonus;
            }

            pokemon.ChangeMood(moodChange.mood, amount);
        }

        if(healAmount > 0) {
            pokemon.IncreaseHP(healAmount + bonus);
        }

        ApplyVitalEffects(pokemon, bonus);

        if(cureStatus) {
            pokemon.CureStatus();
            pokemon.CureVolatileStatus();
        }

        if(pokemonExperienceGain > 0) {
            pokemon.GainExp(pokemonExperienceGain + bonus);
        }

        foreach(var needChange in CareNeedChanges) {
            if(needChange != null && needChange.need != null && needChange.amount != 0) {
                var amount = needChange.amount;
                if(amount > 0) {
                    amount += bonus;
                }
                pokemon.ChangeCareNeed(needChange.need, amount);
            }
        }

        ApplyEffortValues(pokemon, bonus);
        ApplyGrowthTraining(pokemon, bonus);
        if(recordCareHistory) {
            pokemon.RecordCareAction(this, category, sourceId);
        }

        failureMessage = null;
        return true;
    }

    int GetTodayCount(Pokemon pokemon) {
        if(pokemon == null || pokemon.CareRecords == null) {
            return 0;
        }

        int day = TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
        return pokemon.CareRecords.Count(record => record != null && record.actionId == Id && record.day == day);
    }

    void ApplyEffortValues(Pokemon pokemon, int bonus) {
        if(EffortValueRewards.Count == 0 || GlobalSettings.i == null) {
            return;
        }

        var rewards = new Dictionary<Stat, int>();
        foreach(var reward in EffortValueRewards) {
            if(reward == null || reward.amount <= 0) {
                continue;
            }

            int amount = reward.amount + Mathf.Max(0, bonus);
            if(rewards.ContainsKey(reward.stat)) {
                rewards[reward.stat] += amount;
            } else {
                rewards[reward.stat] = amount;
            }
        }

        if(rewards.Count > 0) {
            pokemon.GainEvs(rewards);
        }
    }

    void ApplyVitalEffects(Pokemon pokemon, int bonus) {
        if(pokemon == null) {
            return;
        }

        if(restoreAllVitalsToFull) {
            pokemon.RestoreVitalsToFull(vitalProfile);
            return;
        }

        if(restoreCoreVitalsToFull) {
            pokemon.RestoreCoreVitalsToFull(vitalProfile);
        }

        if(restoreBattleVitalsToFull) {
            pokemon.RestoreBattleVitalsToFull(vitalProfile);
        }

        foreach(var change in VitalChanges) {
            if(change == null) {
                continue;
            }

            int amount = change.GetAmount(pokemon);
            if(amount > 0 && bonus > 0) {
                amount += bonus;
            }

            pokemon.ChangeVitalResource(change.resource, amount, vitalProfile);
        }
    }

    void ApplyGrowthTraining(Pokemon pokemon, int bonus) {
        if(pokemon == null || GrowthTrainingRewards.Count == 0) {
            return;
        }

        foreach(var reward in GrowthTrainingRewards) {
            reward?.Apply(pokemon, growthProfile, bonus, Id, DisplayName);
        }
    }
}
