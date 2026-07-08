using UnityEngine;

public enum CustomizationRequirementMode {
    EquippedPart,
    EquippedPartTag,
    EquippedSlot,
    UnlockedPart,
    CurrentPreset
}

[CreateAssetMenu(menuName = "Activities/Requirements/Customization Requirement")]
public class CustomizationRequirement : ActivityRequirement {
    [Tooltip("Which customization value this requirement checks.")]
    [SerializeField] CustomizationRequirementMode mode = CustomizationRequirementMode.EquippedPart;
    [Tooltip("Customization part checked by Equipped Part and Unlocked Part modes.")]
    [SerializeField] CustomizationPartDefinition part;
    [Tooltip("Customization preset checked by Current Preset mode.")]
    [SerializeField] CustomizationPresetDefinition preset;
    [Tooltip("Slot checked by Equipped Slot mode.")]
    [SerializeField] CustomizationSlot slot = CustomizationSlot.Outfit;
    [Tooltip("Tag checked by Equipped Part Tag mode.")]
    [SerializeField] string tag;
    [Tooltip("If enabled, the selected customization condition must be true. If disabled, it must be false.")]
    [SerializeField] bool mustBeMet = true;

    public override bool IsMet(PlayerController player) {
        var customization = player != null ? player.GetComponent<PlayerCustomization>() : null;
        bool result = mode switch {
            CustomizationRequirementMode.EquippedPartTag => customization != null && customization.HasEquippedPartWithTag(tag),
            CustomizationRequirementMode.EquippedSlot => customization != null && customization.HasEquippedSlot(slot),
            CustomizationRequirementMode.UnlockedPart => customization != null && customization.HasUnlockedPart(part),
            CustomizationRequirementMode.CurrentPreset => customization != null && customization.CurrentPreset == preset,
            _ => customization != null && customization.HasEquippedPart(part)
        };

        return mustBeMet ? result : !result;
    }
}
