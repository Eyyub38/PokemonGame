using UnityEngine;

public enum TitleRequirementMode {
    SpecificTitle,
    TitleTag,
    TitleKind
}

[CreateAssetMenu(menuName = "Activities/Requirements/Title Requirement")]
public class TitleRequirement : ActivityRequirement {
    [Tooltip("Which title property this requirement checks.")]
    [SerializeField] TitleRequirementMode mode = TitleRequirementMode.SpecificTitle;
    [Tooltip("Specific title, badge or permit required when mode is Specific Title.")]
    [SerializeField] TitleDefinition requiredTitle;
    [Tooltip("Free-form title tag required when mode is Title Tag.")]
    [SerializeField] string requiredTag;
    [Tooltip("Title kind required when mode is Title Kind.")]
    [SerializeField] TitleKind requiredKind = TitleKind.Title;
    [Tooltip("If enabled, the selected title condition must be true. If disabled, it must be false.")]
    [SerializeField] bool mustHave = true;

    public override bool IsMet(PlayerController player) {
        var titles = player != null ? player.GetComponent<PlayerTitles>() : null;
        bool result = mode switch {
            TitleRequirementMode.TitleTag => titles != null && titles.HasTitleWithTag(requiredTag),
            TitleRequirementMode.TitleKind => titles != null && titles.HasTitleKind(requiredKind),
            _ => titles != null && titles.HasTitle(requiredTitle)
        };

        return mustHave ? result : !result;
    }
}
