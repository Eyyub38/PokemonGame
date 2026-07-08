using UnityEngine;

public enum LifestyleRequirementMode {
    SpecificLifestylePoints,
    SpecificLifestyleRank,
    AnyLifestyleWithTag,
    AnyLifestyleInCategory,
    DominantLifestyle,
    DominantLifestyleHasTag
}

[CreateAssetMenu(menuName = "Activities/Requirements/Lifestyle Requirement")]
public class LifestyleRequirement : ActivityRequirement {
    [Tooltip("How this requirement checks player lifestyle state.")]
    [SerializeField] LifestyleRequirementMode mode = LifestyleRequirementMode.SpecificLifestylePoints;
    [Tooltip("Lifestyle checked by Specific Lifestyle and Dominant Lifestyle modes.")]
    [SerializeField] PlayerLifestyleDefinition lifestyle = null;
    [Tooltip("Lifestyle category checked by Any Lifestyle In Category mode.")]
    [SerializeField] PlayerLifestyleCategory category = PlayerLifestyleCategory.General;
    [Tooltip("Lifestyle tag checked by tag-based modes.")]
    [SerializeField] string tag = string.Empty;
    [Tooltip("Minimum points required by point/tag/category modes.")]
    [Min(0)]
    [SerializeField] int minimumPoints = 1;
    [Tooltip("Minimum rank index required by Specific Lifestyle Rank mode. 0 is the first rank in the Lifestyle Definition.")]
    [Min(0)]
    [SerializeField] int minimumRankIndex = 0;
    [Tooltip("If enabled, the selected condition must be true. If disabled, the selected condition must be false.")]
    [SerializeField] bool expected = true;

    public override bool IsMet(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerLifestyleLog>() : null;
        bool value = mode switch {
            LifestyleRequirementMode.SpecificLifestylePoints => log != null && log.HasLifestyle(lifestyle, minimumPoints),
            LifestyleRequirementMode.SpecificLifestyleRank => log != null && log.GetRankIndex(lifestyle) >= minimumRankIndex,
            LifestyleRequirementMode.AnyLifestyleWithTag => log != null && log.HasLifestyleTag(tag, minimumPoints),
            LifestyleRequirementMode.AnyLifestyleInCategory => log != null && log.HasLifestyleCategory(category, minimumPoints),
            LifestyleRequirementMode.DominantLifestyle => log != null && log.DominantLifestyleIs(lifestyle),
            LifestyleRequirementMode.DominantLifestyleHasTag => DominantHasTag(log),
            _ => false
        };

        return expected ? value : !value;
    }

    bool DominantHasTag(PlayerLifestyleLog log) {
        if(log == null || string.IsNullOrWhiteSpace(tag)) {
            return false;
        }

        var dominant = log.GetDominantLifestyle();
        if(dominant == null) {
            return false;
        }

        foreach(var definition in Resources.LoadAll<PlayerLifestyleDefinition>("")) {
            if(definition != null && definition.Id == dominant.lifestyleId) {
                return definition.HasTag(tag);
            }
        }

        return false;
    }
}
