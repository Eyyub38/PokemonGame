using UnityEngine;

public enum AssignmentRequirementMode {
    AssignmentUnlocked,
    AssignmentActive,
    AssignmentCompleted,
    AssignmentCompletedCount,
    AssignmentTagCompletedCount,
    AssignmentAvailable
}

[CreateAssetMenu(menuName = "Activities/Requirements/Assignment Requirement")]
public class AssignmentRequirement : ActivityRequirement {
    [Tooltip("Which assignment value this requirement checks.")]
    [SerializeField] AssignmentRequirementMode mode = AssignmentRequirementMode.AssignmentCompleted;
    [Tooltip("Assignment checked by assignment-specific modes.")]
    [SerializeField] AssignmentDefinition assignment;
    [Tooltip("Optional source id filter. Empty accepts any source.")]
    [SerializeField] string sourceId;
    [Tooltip("Tag checked by Assignment Tag Completed Count mode.")]
    [SerializeField] string tag;
    [Tooltip("Minimum count required by count modes.")]
    [Min(0)]
    [SerializeField] int requiredCount = 1;
    [Tooltip("If enabled, the selected assignment condition must be true. If disabled, it must be false.")]
    [SerializeField] bool mustBeMet = true;

    public override bool IsMet(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerAssignmentLog>() : null;
        bool result = mode switch {
            AssignmentRequirementMode.AssignmentUnlocked => log != null && log.HasUnlockedAssignment(assignment),
            AssignmentRequirementMode.AssignmentActive => log != null && log.HasActiveAssignment(assignment, sourceId),
            AssignmentRequirementMode.AssignmentCompletedCount => log != null && log.GetCompletedCount(assignment, sourceId) >= Mathf.Max(0, requiredCount),
            AssignmentRequirementMode.AssignmentTagCompletedCount => log != null && log.GetCompletedCountWithTag(tag) >= Mathf.Max(0, requiredCount),
            AssignmentRequirementMode.AssignmentAvailable => assignment != null && assignment.CanAccept(player, log, sourceId, out _),
            _ => log != null && log.GetCompletedCount(assignment, sourceId) > 0
        };

        return mustBeMet ? result : !result;
    }
}
