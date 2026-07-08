using UnityEngine;

[CreateAssetMenu(menuName = "Activities/Requirements/Milestone Requirement")]
public class MilestoneRequirement : ActivityRequirement {
    [Tooltip("Milestone checked by this requirement.")]
    [SerializeField] MilestoneDefinition milestone;
    [Tooltip("If enabled, the milestone must be completed. If disabled, it must not be completed.")]
    [SerializeField] bool mustBeCompleted = true;

    public override bool IsMet(PlayerController player) {
        if(player == null || milestone == null) {
            return false;
        }

        var milestones = player.GetComponent<PlayerMilestones>();
        bool completed = milestones != null && milestones.HasMilestone(milestone);
        return mustBeCompleted ? completed : !completed;
    }
}
