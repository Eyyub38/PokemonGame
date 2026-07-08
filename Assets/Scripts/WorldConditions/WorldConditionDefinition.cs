using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum WorldConditionCategory {
    General,
    Weather,
    Festival,
    Crisis,
    Economy,
    Law,
    Ecology,
    Encounter,
    Transit,
    Market,
    Research,
    PokemonCare,
    Custom
}

[CreateAssetMenu(menuName = "World Conditions/Condition Definition")]
public class WorldConditionDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this world condition. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in debug/future UI. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note or player-facing description for this world condition.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Broad category used by requirements, validators and future UI filters.")]
    [SerializeField] WorldConditionCategory category = WorldConditionCategory.General;
    [Tooltip("Higher priority conditions are evaluated first for block messages and future UI sorting.")]
    [SerializeField] int priority = 0;
    [Tooltip("Free-form tags such as rainy, emergency, sale, migration, curfew or festival.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Scope")]
    [Tooltip("If enabled, this condition applies regardless of region or activity zone.")]
    [SerializeField] bool globalScope = true;
    [Tooltip("Specific regions affected when Global Scope is disabled.")]
    [SerializeField] List<RegionInfoDefinition> regions = new List<RegionInfoDefinition>();
    [Tooltip("Region tags affected when Global Scope is disabled.")]
    [SerializeField] List<string> regionTags = new List<string>();
    [Tooltip("Specific activity zones affected when Global Scope is disabled.")]
    [SerializeField] List<ActivityZoneDefinition> zones = new List<ActivityZoneDefinition>();
    [Tooltip("Activity zone tags affected when Global Scope is disabled.")]
    [SerializeField] List<string> zoneTags = new List<string>();

    [Header("Activity Targets")]
    [Tooltip("If enabled, this condition can affect every activity in its scope.")]
    [SerializeField] bool affectsAllActivities = true;
    [Tooltip("Specific activities affected when Affects All Activities is disabled.")]
    [SerializeField] List<ActivityDefinition> affectedActivities = new List<ActivityDefinition>();
    [Tooltip("Activity tags affected when Affects All Activities is disabled.")]
    [SerializeField] List<string> affectedActivityTags = new List<string>();
    [Tooltip("If enabled, affected activities cannot be performed while this condition is active.")]
    [SerializeField] bool blocksActivities = false;
    [Tooltip("Message shown when this condition blocks an activity.")]
    [TextArea]
    [SerializeField] string blockedActivityMessage = "This activity is unavailable right now.";

    [Header("Duration")]
    [Tooltip("Default in-game duration when a source activates this condition. 0 means no automatic expiry.")]
    [Min(0)]
    [SerializeField] int defaultDurationHours = 24;

    [Header("Experience")]
    [Tooltip("Multiplier applied to trainer XP. 1 means no change.")]
    [Min(0f)]
    [SerializeField] float experienceMultiplier = 1f;
    [Tooltip("Flat trainer XP added after the multiplier.")]
    [SerializeField] int flatExperienceBonus = 0;

    [Header("Output Bonuses")]
    [Tooltip("Extra yield bonus used by farming and resource gathering.")]
    [SerializeField] int yieldBonus = 0;
    [Tooltip("Extra research points added when studying a subject.")]
    [SerializeField] int researchPointBonus = 0;
    [Tooltip("Extra care power added to Pokemon care actions.")]
    [SerializeField] int pokemonCareBonus = 0;

    [Header("Cost Multipliers")]
    [Tooltip("Multiplier applied to activity item costs. 1 means no change.")]
    [Min(0f)]
    [SerializeField] float itemCostMultiplier = 1f;
    [Tooltip("Multiplier applied to tool durability costs. 1 means no change.")]
    [Min(0f)]
    [SerializeField] float toolDurabilityCostMultiplier = 1f;
    [Tooltip("Multiplier applied to survival need costs. 1 means no change.")]
    [Min(0f)]
    [SerializeField] float needCostMultiplier = 1f;

    [Header("Future System Modifiers")]
    [Tooltip("Multiplier future encounter systems can use for encounter rates. 1 means no change.")]
    [Min(0f)]
    [SerializeField] float encounterRateMultiplier = 1f;
    [Tooltip("Multiplier future shop systems can use for listed prices. 1 means no change.")]
    [Min(0f)]
    [SerializeField] float shopPriceMultiplier = 1f;
    [Tooltip("Multiplier future rumor systems can use for spread speed. 1 means no change.")]
    [Min(0f)]
    [SerializeField] float rumorSpreadSpeedMultiplier = 1f;
    [Tooltip("Multiplier future rumor systems can use for decay speed. 1 means no change.")]
    [Min(0f)]
    [SerializeField] float rumorDecaySpeedMultiplier = 1f;

    [Header("Events")]
    [Tooltip("Optional event published when this condition becomes active. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition activatedEvent = null;
    [Tooltip("Optional event published when this condition is manually removed or expires. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition deactivatedEvent = null;
    [Tooltip("If enabled, condition state changes may appear in the notification feed.")]
    [SerializeField] bool showConditionEventsInFeed = false;
    [Tooltip("If enabled, condition state changes are written to the debug log.")]
    [SerializeField] bool writeConditionEventsToDebugLog = false;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public WorldConditionCategory Category => category;
    public int Priority => priority;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : System.Array.Empty<string>();
    public bool GlobalScope => globalScope;
    public IReadOnlyList<RegionInfoDefinition> Regions => regions != null ? (IReadOnlyList<RegionInfoDefinition>)regions : System.Array.Empty<RegionInfoDefinition>();
    public IReadOnlyList<string> RegionTags => regionTags != null ? (IReadOnlyList<string>)regionTags : System.Array.Empty<string>();
    public IReadOnlyList<ActivityZoneDefinition> Zones => zones != null ? (IReadOnlyList<ActivityZoneDefinition>)zones : System.Array.Empty<ActivityZoneDefinition>();
    public IReadOnlyList<string> ZoneTags => zoneTags != null ? (IReadOnlyList<string>)zoneTags : System.Array.Empty<string>();
    public bool AffectsAllActivities => affectsAllActivities;
    public IReadOnlyList<ActivityDefinition> AffectedActivities => affectedActivities != null ? (IReadOnlyList<ActivityDefinition>)affectedActivities : System.Array.Empty<ActivityDefinition>();
    public IReadOnlyList<string> AffectedActivityTags => affectedActivityTags != null ? (IReadOnlyList<string>)affectedActivityTags : System.Array.Empty<string>();
    public bool BlocksActivities => blocksActivities;
    public string BlockedActivityMessage => string.IsNullOrWhiteSpace(blockedActivityMessage) ? "This activity is unavailable right now." : blockedActivityMessage;
    public int DefaultDurationHours => Mathf.Max(0, defaultDurationHours);
    public float ExperienceMultiplier => Mathf.Max(0f, experienceMultiplier);
    public int FlatExperienceBonus => flatExperienceBonus;
    public int YieldBonus => yieldBonus;
    public int ResearchPointBonus => researchPointBonus;
    public int PokemonCareBonus => pokemonCareBonus;
    public float ItemCostMultiplier => Mathf.Max(0f, itemCostMultiplier);
    public float ToolDurabilityCostMultiplier => Mathf.Max(0f, toolDurabilityCostMultiplier);
    public float NeedCostMultiplier => Mathf.Max(0f, needCostMultiplier);
    public float EncounterRateMultiplier => Mathf.Max(0f, encounterRateMultiplier);
    public float ShopPriceMultiplier => Mathf.Max(0f, shopPriceMultiplier);
    public float RumorSpreadSpeedMultiplier => Mathf.Max(0f, rumorSpreadSpeedMultiplier);
    public float RumorDecaySpeedMultiplier => Mathf.Max(0f, rumorDecaySpeedMultiplier);
    public GameEventDefinition ActivatedEvent => activatedEvent;
    public GameEventDefinition DeactivatedEvent => deactivatedEvent;
    public bool ShowConditionEventsInFeed => showConditionEventsInFeed;
    public bool WriteConditionEventsToDebugLog => writeConditionEventsToDebugLog;

    public bool Affects(ActivityDefinition activity, ActivityZoneDefinition activeZone = null, RegionInfoDefinition activeRegion = null) {
        return AffectsActivity(activity) && MatchesScope(activeRegion, activeZone);
    }

    public bool AffectsActivity(ActivityDefinition activity) {
        if(activity == null) {
            return false;
        }

        if(affectsAllActivities) {
            return true;
        }

        if(AffectedActivities.Contains(activity)) {
            return true;
        }

        return AffectedActivityTags.Any(tag => activity.HasTag(tag));
    }

    public bool MatchesScope(RegionInfoDefinition activeRegion = null, ActivityZoneDefinition activeZone = null) {
        if(globalScope) {
            return true;
        }

        bool hasScopeFilters = Regions.Count > 0 || RegionTags.Count > 0 || Zones.Count > 0 || ZoneTags.Count > 0;
        if(!hasScopeFilters) {
            return false;
        }

        return MatchesRegion(activeRegion, activeZone) || MatchesZone(activeZone);
    }

    public bool MatchesRegion(RegionInfoDefinition activeRegion, ActivityZoneDefinition activeZone = null) {
        if(activeRegion != null) {
            if(Regions.Contains(activeRegion) || RegionTags.Any(activeRegion.HasTag)) {
                return true;
            }
        }

        if(activeZone == null) {
            return false;
        }

        foreach(var region in Regions) {
            if(region != null && region.ActivityZones != null && region.ActivityZones.Contains(activeZone)) {
                return true;
            }
        }

        return false;
    }

    public bool MatchesZone(ActivityZoneDefinition activeZone) {
        if(activeZone == null) {
            return false;
        }

        if(Zones.Contains(activeZone)) {
            return true;
        }

        return ZoneTags.Any(activeZone.HasTag);
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, System.StringComparison.OrdinalIgnoreCase));
    }
}
