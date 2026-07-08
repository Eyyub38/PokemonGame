using UnityEngine;

public enum OriginRequirementMode {
    AnyOriginSelected,
    NoOriginSelected,
    SpecificOrigin,
    Category,
    Tag
}

[CreateAssetMenu(menuName = "Activities/Requirements/Origin Requirement")]
public class OriginRequirement : ActivityRequirement {
    [Tooltip("How this requirement checks the player's selected origin.")]
    [SerializeField] OriginRequirementMode mode = OriginRequirementMode.AnyOriginSelected;
    [Tooltip("Origin checked by Specific Origin mode.")]
    [SerializeField] PlayerOriginDefinition origin = null;
    [Tooltip("Origin category checked by Category mode.")]
    [SerializeField] PlayerOriginCategory category = PlayerOriginCategory.General;
    [Tooltip("Origin tag checked by Tag mode.")]
    [SerializeField] string tag = string.Empty;
    [Tooltip("If enabled, the selected condition must be true. If disabled, the selected condition must be false.")]
    [SerializeField] bool expected = true;

    public override bool IsMet(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerOriginLog>() : null;
        bool value = mode switch {
            OriginRequirementMode.AnyOriginSelected => log != null && log.HasSelectedOrigin,
            OriginRequirementMode.NoOriginSelected => log == null || !log.HasSelectedOrigin,
            OriginRequirementMode.SpecificOrigin => log != null && log.HasOrigin(origin),
            OriginRequirementMode.Category => log != null && log.HasOriginCategory(category),
            OriginRequirementMode.Tag => log != null && log.HasOriginTag(tag),
            _ => false
        };

        return expected ? value : !value;
    }
}
