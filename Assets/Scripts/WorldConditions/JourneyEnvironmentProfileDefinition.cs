using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum JourneyEnvironmentEvaluationTrigger {
    Manual,
    Start,
    TimeChanged,
    AreaProfileChanged,
    WorldConditionChanged
}

public enum JourneyEnvironmentPokemonTargetMode {
    AllParty,
    LeadPokemon,
    HealthyParty,
    FaintedParty
}

[CreateAssetMenu(menuName = "Journey/Environment Profile")]
public class JourneyEnvironmentProfileDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this journey environment profile. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in debug/future UI. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note explaining where and why these journey modifiers apply.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Free-form tags such as rain, heat, cave, route, camp, safe, storm or mountain.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Rules")]
    [Tooltip("Environment rules evaluated by JourneyEnvironmentController.")]
    [SerializeField] List<JourneyEnvironmentRule> rules = new List<JourneyEnvironmentRule>();

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public IReadOnlyList<string> Tags => tags != null ? tags : Array.Empty<string>();
    public IReadOnlyList<JourneyEnvironmentRule> Rules => rules != null ? rules : Array.Empty<JourneyEnvironmentRule>();

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }
}

[Serializable]
public class JourneyEnvironmentRule {
    [Header("Identity")]
    [Tooltip("Stable id for this rule inside its profile. Empty uses Display Name or 'environment-rule'.")]
    [SerializeField] string ruleId = string.Empty;
    [Tooltip("Readable rule name for debug/future UI. Empty uses Rule Id.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("If disabled, this rule is ignored.")]
    [SerializeField] bool enabled = true;

    [Header("Evaluation")]
    [Tooltip("Runtime triggers that can evaluate this rule. Empty accepts every trigger.")]
    [SerializeField] List<JourneyEnvironmentEvaluationTrigger> triggers = new List<JourneyEnvironmentEvaluationTrigger>();
    [Tooltip("Chance that this rule applies after trigger, interval and condition checks pass.")]
    [Range(0f, 1f)]
    [SerializeField] float evaluateChance = 1f;
    [Tooltip("Minimum in-game hours between successful applications. 1 means hourly. 0 disables interval checks.")]
    [Min(0)]
    [SerializeField] int intervalHours = 1;
    [Tooltip("If enabled, blocked evaluations are stored in PlayerJourneyEnvironmentLog.")]
    [SerializeField] bool recordBlockedEvaluations;

    [Header("Scope")]
    [Tooltip("If enabled, this rule can apply in every region/zone as long as other requirements pass.")]
    [SerializeField] bool globalScope = true;
    [Tooltip("Specific regions accepted when Global Scope is disabled.")]
    [SerializeField] List<RegionInfoDefinition> regions = new List<RegionInfoDefinition>();
    [Tooltip("Region tags accepted when Global Scope is disabled.")]
    [SerializeField] List<string> regionTags = new List<string>();
    [Tooltip("Specific activity zones accepted when Global Scope is disabled.")]
    [SerializeField] List<ActivityZoneDefinition> zones = new List<ActivityZoneDefinition>();
    [Tooltip("Activity zone tags accepted when Global Scope is disabled.")]
    [SerializeField] List<string> zoneTags = new List<string>();
    [Tooltip("Optional region override used for pool rolls and logs when this rule applies.")]
    [SerializeField] RegionInfoDefinition regionOverride = null;
    [Tooltip("Optional activity zone override used for pool rolls and logs when this rule applies.")]
    [SerializeField] ActivityZoneDefinition zoneOverride = null;

    [Header("Time / Conditions")]
    [Tooltip("If set, the rule applies only during these day periods.")]
    [SerializeField] List<DayPeriod> allowedPeriods = new List<DayPeriod>();
    [Tooltip("World conditions that must be active in the resolved region/zone.")]
    [SerializeField] List<WorldConditionDefinition> requiredWorldConditions = new List<WorldConditionDefinition>();
    [Tooltip("Any active world condition with one of these tags can satisfy the tag requirement.")]
    [SerializeField] List<string> requiredWorldConditionTags = new List<string>();
    [Tooltip("World conditions that block this rule while active in the resolved region/zone.")]
    [SerializeField] List<WorldConditionDefinition> blockedWorldConditions = new List<WorldConditionDefinition>();
    [Tooltip("Any active world condition with one of these tags blocks this rule.")]
    [SerializeField] List<string> blockedWorldConditionTags = new List<string>();
    [Tooltip("Reusable requirements checked after scope and world condition checks.")]
    [SerializeField] List<ActivityRequirement> extraRequirements = new List<ActivityRequirement>();

    [Header("Survival Effects")]
    [Tooltip("Survival need changes applied per evaluated hour. Negative values drain; positive values restore.")]
    [SerializeField] List<JourneySurvivalNeedModifier> survivalNeedChanges = new List<JourneySurvivalNeedModifier>();

    [Header("Pokemon Care Effects")]
    [Tooltip("Which party Pokemon receive Pokemon care changes.")]
    [SerializeField] JourneyEnvironmentPokemonTargetMode pokemonTargetMode = JourneyEnvironmentPokemonTargetMode.AllParty;
    [Tooltip("Pokemon care need changes applied per evaluated hour. Negative values drain; positive values restore.")]
    [SerializeField] List<JourneyPokemonCareNeedModifier> pokemonCareNeedChanges = new List<JourneyPokemonCareNeedModifier>();

    [Header("Event / Reward Hooks")]
    [Tooltip("Optional situation event pools rolled when this rule successfully applies.")]
    [SerializeField] List<SituationEventPoolDefinition> situationEventPools = new List<SituationEventPoolDefinition>();
    [Tooltip("Optional Life Path rewards granted when this rule successfully applies.")]
    [SerializeField] List<LifePathReward> lifePathRewards = new List<LifePathReward>();

    public string RuleId => !string.IsNullOrWhiteSpace(ruleId) ? ruleId : !string.IsNullOrWhiteSpace(displayName) ? displayName : "environment-rule";
    public string DisplayName => !string.IsNullOrWhiteSpace(displayName) ? displayName : RuleId;
    public bool Enabled => enabled;
    public IReadOnlyList<JourneyEnvironmentEvaluationTrigger> Triggers => triggers != null ? triggers : Array.Empty<JourneyEnvironmentEvaluationTrigger>();
    public float EvaluateChance => Mathf.Clamp01(evaluateChance);
    public int IntervalHours => Mathf.Max(0, intervalHours);
    public bool RecordBlockedEvaluations => recordBlockedEvaluations;
    public bool GlobalScope => globalScope;
    public IReadOnlyList<RegionInfoDefinition> Regions => regions != null ? regions : Array.Empty<RegionInfoDefinition>();
    public IReadOnlyList<string> RegionTags => regionTags != null ? regionTags : Array.Empty<string>();
    public IReadOnlyList<ActivityZoneDefinition> Zones => zones != null ? zones : Array.Empty<ActivityZoneDefinition>();
    public IReadOnlyList<string> ZoneTags => zoneTags != null ? zoneTags : Array.Empty<string>();
    public IReadOnlyList<DayPeriod> AllowedPeriods => allowedPeriods != null ? allowedPeriods : Array.Empty<DayPeriod>();
    public IReadOnlyList<WorldConditionDefinition> RequiredWorldConditions => requiredWorldConditions != null ? requiredWorldConditions : Array.Empty<WorldConditionDefinition>();
    public IReadOnlyList<string> RequiredWorldConditionTags => requiredWorldConditionTags != null ? requiredWorldConditionTags : Array.Empty<string>();
    public IReadOnlyList<WorldConditionDefinition> BlockedWorldConditions => blockedWorldConditions != null ? blockedWorldConditions : Array.Empty<WorldConditionDefinition>();
    public IReadOnlyList<string> BlockedWorldConditionTags => blockedWorldConditionTags != null ? blockedWorldConditionTags : Array.Empty<string>();
    public IReadOnlyList<ActivityRequirement> ExtraRequirements => extraRequirements != null ? extraRequirements : Array.Empty<ActivityRequirement>();
    public IReadOnlyList<JourneySurvivalNeedModifier> SurvivalNeedChanges => survivalNeedChanges != null ? survivalNeedChanges : Array.Empty<JourneySurvivalNeedModifier>();
    public JourneyEnvironmentPokemonTargetMode PokemonTargetMode => pokemonTargetMode;
    public IReadOnlyList<JourneyPokemonCareNeedModifier> PokemonCareNeedChanges => pokemonCareNeedChanges != null ? pokemonCareNeedChanges : Array.Empty<JourneyPokemonCareNeedModifier>();
    public IReadOnlyList<SituationEventPoolDefinition> SituationEventPools => situationEventPools != null ? situationEventPools : Array.Empty<SituationEventPoolDefinition>();
    public IReadOnlyList<LifePathReward> LifePathRewards => lifePathRewards != null ? lifePathRewards : Array.Empty<LifePathReward>();

    public bool HasAnyPayload => SurvivalNeedChanges.Any(entry => entry != null && entry.Need != null && entry.AmountPerHour != 0)
        || PokemonCareNeedChanges.Any(entry => entry != null && entry.Need != null && entry.AmountPerHour != 0)
        || SituationEventPools.Any(pool => pool != null)
        || LifePathRewards.Any(reward => reward != null && reward.lifePath != null && reward.HasAnyPayload);

    public bool AcceptsTrigger(JourneyEnvironmentEvaluationTrigger trigger) {
        return Triggers.Count == 0 || Triggers.Contains(trigger);
    }

    public RegionInfoDefinition ResolveRegion(RegionInfoDefinition fallbackRegion) {
        return regionOverride != null ? regionOverride : fallbackRegion;
    }

    public ActivityZoneDefinition ResolveZone(ActivityZoneDefinition fallbackZone) {
        return zoneOverride != null ? zoneOverride : fallbackZone;
    }

    public string ResolveSourceId(JourneyEnvironmentProfileDefinition profile) {
        return profile != null ? $"journey-environment:{profile.Id}:{RuleId}" : $"journey-environment:{RuleId}";
    }

    public string ResolveSourceName(JourneyEnvironmentProfileDefinition profile) {
        return profile != null ? $"{profile.DisplayName}/{DisplayName}" : DisplayName;
    }

    public bool CanEvaluate(
        PlayerController player,
        JourneyEnvironmentProfileDefinition profile,
        PlayerJourneyEnvironmentLog log,
        JourneyEnvironmentEvaluationTrigger trigger,
        RegionInfoDefinition region,
        ActivityZoneDefinition zone,
        out string failureMessage) {
        if(!enabled) {
            failureMessage = "Journey environment rule is disabled.";
            return false;
        }

        if(!AcceptsTrigger(trigger)) {
            failureMessage = "Journey environment trigger did not match.";
            return false;
        }

        if(player == null) {
            failureMessage = "A player is required.";
            return false;
        }

        if(!HasAnyPayload) {
            failureMessage = "Journey environment rule has no effects, pools or rewards.";
            return false;
        }

        if(log != null && !log.CanEvaluate(profile, this, IntervalHours, out failureMessage)) {
            return false;
        }

        if(UnityEngine.Random.value > EvaluateChance) {
            failureMessage = "Journey environment chance failed.";
            return false;
        }

        if(!MatchesTime()) {
            failureMessage = "Journey environment time period did not match.";
            return false;
        }

        if(!MatchesScope(region, zone)) {
            failureMessage = "Journey environment scope did not match.";
            return false;
        }

        if(!MatchesWorldConditions(player, region, zone, out failureMessage)) {
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

    bool MatchesTime() {
        return AllowedPeriods.Count == 0
            || TimeSystem.i == null
            || AllowedPeriods.Contains(TimeSystem.i.CurrentPeriod);
    }

    public bool MatchesScope(RegionInfoDefinition region, ActivityZoneDefinition zone) {
        if(globalScope) {
            return true;
        }

        bool hasFilters = Regions.Count > 0 || RegionTags.Count > 0 || Zones.Count > 0 || ZoneTags.Count > 0;
        if(!hasFilters) {
            return false;
        }

        if(region != null && (Regions.Contains(region) || RegionTags.Any(region.HasTag))) {
            return true;
        }

        if(zone != null && (Zones.Contains(zone) || ZoneTags.Any(zone.HasTag))) {
            return true;
        }

        return region != null && zone != null && Regions.Any(entry => entry != null && entry.ActivityZones.Contains(zone));
    }

    bool MatchesWorldConditions(PlayerController player, RegionInfoDefinition region, ActivityZoneDefinition zone, out string failureMessage) {
        var log = player != null ? player.GetComponent<PlayerWorldConditionLog>() : null;

        foreach(var condition in RequiredWorldConditions) {
            if(condition != null && !(log?.IsConditionActive(condition, null, region, zone) ?? false)) {
                failureMessage = $"Required world condition '{condition.DisplayName}' is not active.";
                return false;
            }
        }

        foreach(var condition in BlockedWorldConditions) {
            if(condition != null && (log?.IsConditionActive(condition, null, region, zone) ?? false)) {
                failureMessage = $"Blocked world condition '{condition.DisplayName}' is active.";
                return false;
            }
        }

        var activeStates = log != null ? log.GetActiveConditionStates(null, zone, region) : new List<PlayerWorldConditionState>();
        if(RequiredWorldConditionTags.Any(tag => !string.IsNullOrWhiteSpace(tag))
            && !RequiredWorldConditionTags.Where(tag => !string.IsNullOrWhiteSpace(tag)).Any(tag => activeStates.Any(state => state.ResolveDefinition()?.HasTag(tag) ?? false))) {
            failureMessage = "Required world condition tag is not active.";
            return false;
        }

        if(BlockedWorldConditionTags.Where(tag => !string.IsNullOrWhiteSpace(tag)).Any(tag => activeStates.Any(state => state.ResolveDefinition()?.HasTag(tag) ?? false))) {
            failureMessage = "Blocked world condition tag is active.";
            return false;
        }

        failureMessage = null;
        return true;
    }
}

[Serializable]
public class JourneySurvivalNeedModifier {
    [Tooltip("Survival need affected by this environment rule.")]
    [SerializeField] SurvivalNeedDefinition need = null;
    [Tooltip("Amount applied per evaluated hour. Negative drains, positive restores.")]
    [SerializeField] int amountPerHour = 0;

    public SurvivalNeedDefinition Need => need;
    public int AmountPerHour => amountPerHour;
}

[Serializable]
public class JourneyPokemonCareNeedModifier {
    [Tooltip("Pokemon care need affected by this environment rule.")]
    [SerializeField] PokemonCareNeedDefinition need = null;
    [Tooltip("Amount applied per evaluated hour. Negative drains, positive restores.")]
    [SerializeField] int amountPerHour = 0;
    [Tooltip("Care context used for logs and threshold events.")]
    [SerializeField] PokemonCareNeedHourlyContext context = PokemonCareNeedHourlyContext.Active;

    public PokemonCareNeedDefinition Need => need;
    public int AmountPerHour => amountPerHour;
    public PokemonCareNeedHourlyContext Context => context;
}
