using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Companion/Perk Definition")]
public class CompanionPerkDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this companion perk. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in UI/debug messages. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer note or player-facing description for this perk.")]
    [TextArea]
    [SerializeField] string description;
    [Tooltip("Free-form tags used by validators and future UI filters.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Unlock")]
    [Tooltip("Minimum companion bond level required before this perk becomes active.")]
    [SerializeField] CompanionBondLevel minimumBondLevel = CompanionBondLevel.Stranger;
    [Tooltip("Minimum raw bond points required before this perk becomes active.")]
    [Min(0)]
    [SerializeField] int minimumBondPoints;

    [Header("Activity Targets")]
    [Tooltip("If enabled, this perk can affect every activity.")]
    [SerializeField] bool affectsAllActivities = true;
    [Tooltip("Specific activities affected when Affects All Activities is disabled.")]
    [SerializeField] List<ActivityDefinition> affectedActivities = new List<ActivityDefinition>();
    [Tooltip("Activity tags affected when Affects All Activities is disabled.")]
    [SerializeField] List<string> affectedActivityTags = new List<string>();

    [Header("Experience")]
    [Tooltip("Multiplier applied to trainer XP. 1 means no change.")]
    [Min(0f)]
    [SerializeField] float experienceMultiplier = 1f;
    [Tooltip("Flat trainer XP added after the multiplier.")]
    [SerializeField] int flatExperienceBonus;

    [Header("Output Bonuses")]
    [Tooltip("Extra yield bonus used by farming and resource gathering.")]
    [SerializeField] int yieldBonus;
    [Tooltip("Extra research points added when studying a subject.")]
    [SerializeField] int researchPointBonus;
    [Tooltip("Extra care power added to Pokemon care actions.")]
    [SerializeField] int pokemonCareBonus;

    [Header("Survival Support")]
    [Tooltip("Extra stamina support reported by the companion.")]
    [SerializeField] int staminaSupportBonus;
    [Tooltip("Extra survival support reported by the companion.")]
    [SerializeField] int survivalSupportBonus;
    [Tooltip("Extra bond points gained when this companion gains bond.")]
    [SerializeField] int bondGainBonus;

    [Header("Cost Multipliers")]
    [Tooltip("Multiplier applied to item costs. 1 means no change.")]
    [Min(0f)]
    [SerializeField] float itemCostMultiplier = 1f;
    [Tooltip("Multiplier applied to tool durability costs. 1 means no change.")]
    [Min(0f)]
    [SerializeField] float toolDurabilityCostMultiplier = 1f;
    [Tooltip("Multiplier applied to survival need costs. 1 means no change.")]
    [Min(0f)]
    [SerializeField] float needCostMultiplier = 1f;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : System.Array.Empty<string>();
    public CompanionBondLevel MinimumBondLevel => minimumBondLevel;
    public int MinimumBondPoints => Mathf.Max(0, minimumBondPoints);
    public bool AffectsAllActivities => affectsAllActivities;
    public IReadOnlyList<ActivityDefinition> AffectedActivities => affectedActivities != null ? (IReadOnlyList<ActivityDefinition>)affectedActivities : System.Array.Empty<ActivityDefinition>();
    public IReadOnlyList<string> AffectedActivityTags => affectedActivityTags != null ? (IReadOnlyList<string>)affectedActivityTags : System.Array.Empty<string>();
    public float ExperienceMultiplier => experienceMultiplier;
    public int FlatExperienceBonus => flatExperienceBonus;
    public int YieldBonus => yieldBonus;
    public int ResearchPointBonus => researchPointBonus;
    public int PokemonCareBonus => pokemonCareBonus;
    public int StaminaSupportBonus => staminaSupportBonus;
    public int SurvivalSupportBonus => survivalSupportBonus;
    public int BondGainBonus => bondGainBonus;
    public float ItemCostMultiplier => itemCostMultiplier;
    public float ToolDurabilityCostMultiplier => toolDurabilityCostMultiplier;
    public float NeedCostMultiplier => needCostMultiplier;

    public bool IsUnlockedBy(CompanionController companion) {
        if(companion == null) {
            return false;
        }

        return companion.BondLevel >= minimumBondLevel && companion.BondPoints >= MinimumBondPoints;
    }

    public bool Affects(ActivityDefinition activity) {
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

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, System.StringComparison.OrdinalIgnoreCase));
    }
}
