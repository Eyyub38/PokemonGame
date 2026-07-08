using UnityEngine;

[CreateAssetMenu(menuName = "Activities/Requirements/Skill Level Requirement")]
public class SkillLevelRequirement : ActivityRequirement {
    [Tooltip("Player skill to check.")]
    [SerializeField] PlayerSkillDefinition skill;
    [Tooltip("Minimum skill level required.")]
    [Min(1)]
    [SerializeField] int requiredLevel = 1;

    public override bool IsMet(PlayerController player) {
        if(skill == null || player == null) {
            return false;
        }

        var progression = player.GetComponent<PlayerProgression>();
        return progression != null && progression.GetSkillLevel(skill) >= requiredLevel;
    }
}
