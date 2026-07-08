using UnityEngine;

public enum ActivityPermissionRequirementMode {
    CurrentZoneAllowsActivity,
    PermissionAppliesToCurrentZone,
    PermissionRequirementsMet
}

[CreateAssetMenu(menuName = "Activities/Requirements/Activity Permission Requirement")]
public class ActivityPermissionRequirement : ActivityRequirement {
    [Tooltip("Which activity permission value this requirement checks.")]
    [SerializeField] ActivityPermissionRequirementMode mode = ActivityPermissionRequirementMode.CurrentZoneAllowsActivity;
    [Tooltip("Activity checked by this requirement. Empty cannot match permission checks that need an activity.")]
    [SerializeField] ActivityDefinition activity;
    [Tooltip("Specific permission checked by permission-specific modes.")]
    [SerializeField] ActivityPermissionDefinition permission;
    [Tooltip("If enabled, the selected permission condition must be true. If disabled, it must be false.")]
    [SerializeField] bool mustBeMet = true;

    public override bool IsMet(PlayerController player) {
        var zone = PlayerActivityContext.CurrentZone;
        bool result = mode switch {
            ActivityPermissionRequirementMode.PermissionAppliesToCurrentZone => permission != null && permission.AppliesTo(activity, zone),
            ActivityPermissionRequirementMode.PermissionRequirementsMet => permission != null && permission.AppliesTo(activity, zone) && permission.RequirementsMet(player, out _),
            _ => activity != null && PlayerActivityContext.CanPerform(activity, player, out _)
        };

        return mustBeMet ? result : !result;
    }
}
