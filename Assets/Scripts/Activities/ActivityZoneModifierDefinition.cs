using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Activities/Activity Zone Modifier Definition")]
public class ActivityZoneModifierDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable id used by validation/debug systems. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in editor/debug output. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer notes explaining what this modifier represents.")]
    [TextArea]
    [SerializeField] string description;

    [Header("Targets")]
    [Tooltip("If enabled, this modifier affects every activity allowed by the active zone.")]
    [SerializeField] bool affectsAllActivities = true;
    [Tooltip("Specific activities affected when Affects All Activities is disabled.")]
    [SerializeField] List<ActivityDefinition> affectedActivities = new List<ActivityDefinition>();
    [Tooltip("Activity tags affected when Affects All Activities is disabled. Tags are set on Activity Definition assets.")]
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
    public bool AffectsAllActivities => affectsAllActivities;
    public IReadOnlyList<ActivityDefinition> AffectedActivities => affectedActivities;
    public IReadOnlyList<string> AffectedActivityTags => affectedActivityTags;
    public float ExperienceMultiplier => experienceMultiplier;
    public int FlatExperienceBonus => flatExperienceBonus;
    public int YieldBonus => yieldBonus;
    public int ResearchPointBonus => researchPointBonus;
    public int PokemonCareBonus => pokemonCareBonus;
    public float ItemCostMultiplier => itemCostMultiplier;
    public float ToolDurabilityCostMultiplier => toolDurabilityCostMultiplier;
    public float NeedCostMultiplier => needCostMultiplier;

    public bool Affects(ActivityDefinition activity) {
        if(activity == null) {
            return false;
        }

        if(affectsAllActivities) {
            return true;
        }

        if(affectedActivities != null && affectedActivities.Contains(activity)) {
            return true;
        }

        if(affectedActivityTags == null) {
            return false;
        }

        foreach(var tag in affectedActivityTags) {
            if(activity.HasTag(tag)) {
                return true;
            }
        }

        return false;
    }
}
