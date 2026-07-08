using UnityEngine;

public enum ActivityAreaRequirementMode {
    AnyActiveZone,
    SpecificZone,
    ZoneType,
    ZoneTag
}

[CreateAssetMenu(menuName = "Activities/Requirements/Area Requirement")]
public class ActivityAreaRequirement : ActivityRequirement {
    [Tooltip("Which area property this requirement checks.")]
    [SerializeField] ActivityAreaRequirementMode mode = ActivityAreaRequirementMode.AnyActiveZone;
    [Tooltip("Specific zone required when mode is Specific Zone.")]
    [SerializeField] ActivityZoneDefinition requiredZone;
    [Tooltip("Zone type required when mode is Zone Type.")]
    [SerializeField] ActivityZoneType requiredZoneType = ActivityZoneType.General;
    [Tooltip("Free-form tag required when mode is Zone Tag.")]
    [SerializeField] string requiredTag;
    [Tooltip("If enabled, the selected area condition must be true. If disabled, it must be false.")]
    [SerializeField] bool mustMatch = true;

    public override bool IsMet(PlayerController player) {
        bool result = mode switch {
            ActivityAreaRequirementMode.SpecificZone => PlayerActivityContext.HasActiveZone(requiredZone),
            ActivityAreaRequirementMode.ZoneType => PlayerActivityContext.HasActiveZoneType(requiredZoneType),
            ActivityAreaRequirementMode.ZoneTag => PlayerActivityContext.HasActiveTag(requiredTag),
            _ => PlayerActivityContext.CurrentZone != null
        };

        return mustMatch ? result : !result;
    }
}
