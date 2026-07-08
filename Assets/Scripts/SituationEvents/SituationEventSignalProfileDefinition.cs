using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum SituationEventSignalTrigger {
    Manual,
    Start,
    TimeChanged,
    DayChanged,
    SurvivalNeedChanged,
    PokemonCareNeedChanged,
    AreaProfileChanged
}

public enum SituationEventSignalMode {
    Always,
    SurvivalWorstStateAtOrBelow,
    SpecificSurvivalNeedStateAtOrBelow,
    AnyPokemonCareNeedStateAtOrBelow,
    SpecificPokemonCareNeedStateAtOrBelow,
    HealthyPartyCountAtMost,
    PartyAverageHpPercentAtOrBelow,
    FollowingCompanionCountAtLeast,
    NoFollowingCompanions,
    RequiredQuestStatus,
    ActiveAreaProfile,
    ActiveActivityZoneTag,
    WorldConditionState
}

[CreateAssetMenu(menuName = "Situation Events/Situation Event Signal Profile")]
public class SituationEventSignalProfileDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this signal profile. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in debug/future UI. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note explaining which runtime conditions this profile watches.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Free-form tags such as route, camp, fatigue, care, quest, town or weather.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Rules")]
    [Tooltip("Signal rules evaluated by SituationEventSignalController.")]
    [SerializeField] List<SituationEventSignalRule> rules = new List<SituationEventSignalRule>();

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public IReadOnlyList<string> Tags => tags != null ? tags : Array.Empty<string>();
    public IReadOnlyList<SituationEventSignalRule> Rules => rules != null ? rules : Array.Empty<SituationEventSignalRule>();

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }
}

[Serializable]
public class SituationEventSignalRule {
    [Header("Identity")]
    [Tooltip("Stable id for this rule inside its profile. Empty uses Display Name or mode.")]
    [SerializeField] string ruleId = string.Empty;
    [Tooltip("Readable rule name for debug/future UI. Empty uses Rule Id.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("If disabled, this rule is ignored.")]
    [SerializeField] bool enabled = true;

    [Header("Trigger")]
    [Tooltip("Runtime signal types that can evaluate this rule. Empty accepts every trigger.")]
    [SerializeField] List<SituationEventSignalTrigger> triggers = new List<SituationEventSignalTrigger>();
    [Tooltip("Signal condition checked before rolling pools.")]
    [SerializeField] SituationEventSignalMode mode = SituationEventSignalMode.Always;
    [Tooltip("Chance that this rule evaluates after trigger, cooldown and condition checks pass.")]
    [Range(0f, 1f)]
    [SerializeField] float evaluateChance = 1f;
    [Tooltip("In-game hour cooldown between evaluations for this rule. 0 means no rule-level cooldown.")]
    [Min(0)]
    [SerializeField] int cooldownHours = 4;
    [Tooltip("If enabled, blocked evaluations are recorded in PlayerSituationEventSignalLog.")]
    [SerializeField] bool recordBlockedEvaluations;

    [Header("Pools")]
    [Tooltip("Situation event pools rolled when this rule passes.")]
    [SerializeField] List<SituationEventPoolDefinition> pools = new List<SituationEventPoolDefinition>();
    [Tooltip("Optional source id override written into pool/event logs.")]
    [SerializeField] string sourceIdOverride = string.Empty;
    [Tooltip("Optional source name override written into pool/event logs.")]
    [SerializeField] string sourceNameOverride = string.Empty;
    [Tooltip("Optional region context override passed into pool/event filters.")]
    [SerializeField] RegionInfoDefinition regionOverride = null;
    [Tooltip("Optional activity zone context override passed into pool/event filters.")]
    [SerializeField] ActivityZoneDefinition zoneOverride = null;

    [Header("Shared Requirements")]
    [Tooltip("Reusable requirements checked after the signal condition passes.")]
    [SerializeField] List<ActivityRequirement> extraRequirements = new List<ActivityRequirement>();

    [Header("Survival Filters")]
    [Tooltip("Survival need checked by Specific Survival Need State mode.")]
    [SerializeField] SurvivalNeedDefinition survivalNeed = null;
    [Tooltip("Current survival state must be at or below this state. Low accepts Critical and Low.")]
    [SerializeField] SurvivalNeedState survivalStateThreshold = SurvivalNeedState.Low;

    [Header("Pokemon Care Filters")]
    [Tooltip("Pokemon care need checked by Specific Pokemon Care Need State mode.")]
    [SerializeField] PokemonCareNeedDefinition pokemonCareNeed = null;
    [Tooltip("Current Pokemon care state must be at or below this state. Low accepts Critical and Low.")]
    [SerializeField] PokemonCareNeedState pokemonCareStateThreshold = PokemonCareNeedState.Low;

    [Header("Party Filters")]
    [Tooltip("Maximum healthy party Pokemon count accepted by Healthy Party Count At Most mode.")]
    [Min(0)]
    [SerializeField] int maxHealthyPartyCount = 1;
    [Tooltip("Maximum average party HP ratio accepted by Party Average HP Percent mode.")]
    [Range(0f, 1f)]
    [SerializeField] float maxAveragePartyHpRatio = 0.4f;

    [Header("Companion Filters")]
    [Tooltip("Minimum following companion count accepted by Following Companion Count At Least mode.")]
    [Min(0)]
    [SerializeField] int minFollowingCompanionCount = 1;

    [Header("Quest Filters")]
    [Tooltip("Quest checked by Required Quest Status mode.")]
    [SerializeField] QuestBase requiredQuest = null;
    [Tooltip("Quest status accepted by Required Quest Status mode.")]
    [SerializeField] QuestStatus requiredQuestStatus = QuestStatus.Started;

    [Header("Area / World Filters")]
    [Tooltip("Area profile checked by Active Area Profile mode.")]
    [SerializeField] AreaProfileDefinition requiredAreaProfile = null;
    [Tooltip("Optional active area tag checked by Active Area Profile mode.")]
    [SerializeField] string requiredAreaProfileTag = string.Empty;
    [Tooltip("Activity zone tag checked by Active Activity Zone Tag mode.")]
    [SerializeField] string requiredActivityZoneTag = string.Empty;
    [Tooltip("World condition checked by World Condition State mode.")]
    [SerializeField] WorldConditionDefinition requiredWorldCondition = null;
    [Tooltip("Expected active state for World Condition State mode.")]
    [SerializeField] bool requiredWorldConditionActive = true;

    public string RuleId => !string.IsNullOrWhiteSpace(ruleId) ? ruleId : !string.IsNullOrWhiteSpace(displayName) ? displayName : mode.ToString();
    public string DisplayName => !string.IsNullOrWhiteSpace(displayName) ? displayName : RuleId;
    public bool Enabled => enabled;
    public SituationEventSignalMode Mode => mode;
    public IReadOnlyList<SituationEventSignalTrigger> Triggers => triggers != null ? triggers : Array.Empty<SituationEventSignalTrigger>();
    public float EvaluateChance => Mathf.Clamp01(evaluateChance);
    public int CooldownHours => Mathf.Max(0, cooldownHours);
    public bool RecordBlockedEvaluations => recordBlockedEvaluations;
    public IReadOnlyList<SituationEventPoolDefinition> Pools => pools != null ? pools : Array.Empty<SituationEventPoolDefinition>();
    public IReadOnlyList<ActivityRequirement> ExtraRequirements => extraRequirements != null ? extraRequirements : Array.Empty<ActivityRequirement>();
    public SurvivalNeedDefinition SurvivalNeed => survivalNeed;
    public SurvivalNeedState SurvivalStateThreshold => survivalStateThreshold;
    public PokemonCareNeedDefinition PokemonCareNeed => pokemonCareNeed;
    public PokemonCareNeedState PokemonCareStateThreshold => pokemonCareStateThreshold;
    public QuestBase RequiredQuest => requiredQuest;
    public AreaProfileDefinition RequiredAreaProfile => requiredAreaProfile;
    public string RequiredAreaProfileTag => requiredAreaProfileTag;
    public string RequiredActivityZoneTag => requiredActivityZoneTag;
    public WorldConditionDefinition RequiredWorldCondition => requiredWorldCondition;
    public bool RequiredWorldConditionActive => requiredWorldConditionActive;

    public bool AcceptsTrigger(SituationEventSignalTrigger trigger) {
        return Triggers.Count == 0 || Triggers.Contains(trigger);
    }

    public string ResolveSourceId(SituationEventSignalProfileDefinition profile) {
        if(!string.IsNullOrWhiteSpace(sourceIdOverride)) {
            return sourceIdOverride;
        }

        return profile != null ? $"situation-signal:{profile.Id}:{RuleId}" : $"situation-signal:{RuleId}";
    }

    public string ResolveSourceName(SituationEventSignalProfileDefinition profile) {
        if(!string.IsNullOrWhiteSpace(sourceNameOverride)) {
            return sourceNameOverride;
        }

        return profile != null ? $"{profile.DisplayName}/{DisplayName}" : DisplayName;
    }

    public RegionInfoDefinition ResolveRegion(RegionInfoDefinition fallbackRegion) {
        return regionOverride != null ? regionOverride : fallbackRegion;
    }

    public ActivityZoneDefinition ResolveZone(ActivityZoneDefinition fallbackZone) {
        return zoneOverride != null ? zoneOverride : fallbackZone;
    }

    public bool CanEvaluate(PlayerController player, SituationEventSignalProfileDefinition profile, PlayerSituationEventSignalLog log, SituationEventSignalTrigger trigger, out string failureMessage) {
        if(!enabled) {
            failureMessage = "Signal rule is disabled.";
            return false;
        }

        if(!AcceptsTrigger(trigger)) {
            failureMessage = "Signal trigger did not match.";
            return false;
        }

        if(player == null) {
            failureMessage = "A player is required to evaluate situation event signals.";
            return false;
        }

        if(Pools.Count == 0) {
            failureMessage = "Signal rule has no pools.";
            return false;
        }

        if(log != null && !log.CanEvaluate(profile, this, CooldownHours, out failureMessage)) {
            return false;
        }

        if(UnityEngine.Random.value > EvaluateChance) {
            failureMessage = "Signal rule chance failed.";
            return false;
        }

        if(!SignalConditionMet(player, out failureMessage)) {
            return false;
        }

        foreach(var requirement in ExtraRequirements) {
            if(requirement != null && !requirement.IsMet(player)) {
                failureMessage = requirement.FailureMessage;
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    public bool SignalConditionMet(PlayerController player, out string failureMessage) {
        switch(mode) {
            case SituationEventSignalMode.SurvivalWorstStateAtOrBelow:
                return CheckSurvivalWorstState(player, out failureMessage);
            case SituationEventSignalMode.SpecificSurvivalNeedStateAtOrBelow:
                return CheckSpecificSurvivalNeed(player, out failureMessage);
            case SituationEventSignalMode.AnyPokemonCareNeedStateAtOrBelow:
                return CheckAnyPokemonCareNeed(player, out failureMessage);
            case SituationEventSignalMode.SpecificPokemonCareNeedStateAtOrBelow:
                return CheckSpecificPokemonCareNeed(player, out failureMessage);
            case SituationEventSignalMode.HealthyPartyCountAtMost:
                return CheckHealthyPartyCount(player, out failureMessage);
            case SituationEventSignalMode.PartyAverageHpPercentAtOrBelow:
                return CheckPartyAverageHp(player, out failureMessage);
            case SituationEventSignalMode.FollowingCompanionCountAtLeast:
                return CheckFollowingCompanions(player, out failureMessage);
            case SituationEventSignalMode.NoFollowingCompanions:
                return CheckNoFollowingCompanions(player, out failureMessage);
            case SituationEventSignalMode.RequiredQuestStatus:
                return CheckQuestStatus(player, out failureMessage);
            case SituationEventSignalMode.ActiveAreaProfile:
                return CheckActiveAreaProfile(player, out failureMessage);
            case SituationEventSignalMode.ActiveActivityZoneTag:
                return CheckActivityZoneTag(out failureMessage);
            case SituationEventSignalMode.WorldConditionState:
                return CheckWorldCondition(player, out failureMessage);
            default:
                failureMessage = null;
                return true;
        }
    }

    bool CheckSurvivalWorstState(PlayerController player, out string failureMessage) {
        var controller = player.GetComponent<SurvivalNeedsController>();
        if(controller != null && SurvivalStateAtOrBelow(controller.GetWorstState(), survivalStateThreshold)) {
            failureMessage = null;
            return true;
        }

        failureMessage = "Survival worst state did not match signal threshold.";
        return false;
    }

    bool CheckSpecificSurvivalNeed(PlayerController player, out string failureMessage) {
        var need = player.GetComponent<SurvivalNeedsController>()?.GetNeed(survivalNeed);
        if(need != null && SurvivalStateAtOrBelow(need.State, survivalStateThreshold)) {
            failureMessage = null;
            return true;
        }

        failureMessage = "Specific survival need state did not match signal threshold.";
        return false;
    }

    bool CheckAnyPokemonCareNeed(PlayerController player, out string failureMessage) {
        var controller = player.GetComponent<PokemonCareNeedsController>();
        var party = player.GetComponent<PokemonParty>();
        if(controller == null || party?.Pokemons == null) {
            failureMessage = "Pokemon care needs or party are missing.";
            return false;
        }

        foreach(var pokemon in party.Pokemons) {
            foreach(var need in controller.NeedDefinitions) {
                if(pokemon != null && need != null && PokemonCareStateAtOrBelow(need.GetState(pokemon.GetCareNeedValue(need)), pokemonCareStateThreshold)) {
                    failureMessage = null;
                    return true;
                }
            }
        }

        failureMessage = "No Pokemon care need matched signal threshold.";
        return false;
    }

    bool CheckSpecificPokemonCareNeed(PlayerController player, out string failureMessage) {
        var party = player.GetComponent<PokemonParty>();
        if(pokemonCareNeed == null || party?.Pokemons == null) {
            failureMessage = "Pokemon care need or party are missing.";
            return false;
        }

        foreach(var pokemon in party.Pokemons) {
            if(pokemon != null && PokemonCareStateAtOrBelow(pokemonCareNeed.GetState(pokemon.GetCareNeedValue(pokemonCareNeed)), pokemonCareStateThreshold)) {
                failureMessage = null;
                return true;
            }
        }

        failureMessage = "Specific Pokemon care need did not match signal threshold.";
        return false;
    }

    bool CheckHealthyPartyCount(PlayerController player, out string failureMessage) {
        var party = player.GetComponent<PokemonParty>();
        int healthy = party?.Pokemons != null ? party.Pokemons.Count(pokemon => pokemon != null && pokemon.HP > 0) : 0;
        if(healthy <= Mathf.Max(0, maxHealthyPartyCount)) {
            failureMessage = null;
            return true;
        }

        failureMessage = "Healthy party count is above signal threshold.";
        return false;
    }

    bool CheckPartyAverageHp(PlayerController player, out string failureMessage) {
        var party = player.GetComponent<PokemonParty>();
        var pokemon = party?.Pokemons?.Where(entry => entry != null && entry.MaxHp > 0).ToList();
        if(pokemon != null && pokemon.Count > 0 && pokemon.Average(entry => entry.HP / (float)entry.MaxHp) <= maxAveragePartyHpRatio) {
            failureMessage = null;
            return true;
        }

        failureMessage = "Party average HP is above signal threshold.";
        return false;
    }

    bool CheckFollowingCompanions(PlayerController player, out string failureMessage) {
        int count = CompanionController.GetFollowingCompanions(player).Count();
        if(count >= Mathf.Max(0, minFollowingCompanionCount)) {
            failureMessage = null;
            return true;
        }

        failureMessage = "Following companion count is below signal threshold.";
        return false;
    }

    bool CheckNoFollowingCompanions(PlayerController player, out string failureMessage) {
        if(!CompanionController.GetFollowingCompanions(player).Any()) {
            failureMessage = null;
            return true;
        }

        failureMessage = "A companion is following the player.";
        return false;
    }

    bool CheckQuestStatus(PlayerController player, out string failureMessage) {
        if(requiredQuest == null) {
            failureMessage = "Signal rule has no required quest.";
            return false;
        }

        var quest = player.GetComponent<QuestList>()?.GetQuest(requiredQuest.Name);
        if(quest != null && quest.Status == requiredQuestStatus) {
            failureMessage = null;
            return true;
        }

        failureMessage = "Required quest status did not match.";
        return false;
    }

    bool CheckActiveAreaProfile(PlayerController player, out string failureMessage) {
        var log = player.GetComponent<PlayerAreaProfileLog>();
        if(log != null && log.HasActiveArea(requiredAreaProfile, tag: requiredAreaProfileTag)) {
            failureMessage = null;
            return true;
        }

        failureMessage = "Required area profile/tag is not active.";
        return false;
    }

    bool CheckActivityZoneTag(out string failureMessage) {
        if(!string.IsNullOrWhiteSpace(requiredActivityZoneTag) && PlayerActivityContext.HasActiveTag(requiredActivityZoneTag)) {
            failureMessage = null;
            return true;
        }

        failureMessage = "Required activity zone tag is not active.";
        return false;
    }

    bool CheckWorldCondition(PlayerController player, out string failureMessage) {
        if(requiredWorldCondition == null) {
            failureMessage = "Signal rule has no required world condition.";
            return false;
        }

        bool active = player.GetComponent<PlayerWorldConditionLog>()?.IsConditionActive(requiredWorldCondition, null) ?? false;
        if(active == requiredWorldConditionActive) {
            failureMessage = null;
            return true;
        }

        failureMessage = "Required world condition state did not match.";
        return false;
    }

    bool SurvivalStateAtOrBelow(SurvivalNeedState current, SurvivalNeedState threshold) {
        return (int)current <= (int)threshold;
    }

    bool PokemonCareStateAtOrBelow(PokemonCareNeedState current, PokemonCareNeedState threshold) {
        return (int)current <= (int)threshold;
    }
}
