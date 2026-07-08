using UnityEngine;

public enum CareerRequirementMode {
    CareerUnlocked,
    CareerJoined,
    CareerPointsAtLeast,
    CareerRankAtLeast,
    CareerTagJoined,
    CareerCanJoin
}

[CreateAssetMenu(menuName = "Activities/Requirements/Career Requirement")]
public class CareerRequirement : ActivityRequirement {
    [Tooltip("Which career value this requirement checks.")]
    [SerializeField] CareerRequirementMode mode = CareerRequirementMode.CareerJoined;
    [Tooltip("Career path checked by career-specific modes.")]
    [SerializeField] CareerPathDefinition career;
    [Tooltip("Tag checked by Career Tag Joined mode.")]
    [SerializeField] string tag;
    [Tooltip("Minimum points or rank index required by numeric modes.")]
    [Min(0)]
    [SerializeField] int requiredValue;
    [Tooltip("If enabled, mentor-only career checks are evaluated as if a mentor is present.")]
    [SerializeField] bool evaluateAsMentor;
    [Tooltip("If enabled, the selected career condition must be true. If disabled, it must be false.")]
    [SerializeField] bool mustBeMet = true;

    public override bool IsMet(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerCareerLog>() : null;
        bool result = mode switch {
            CareerRequirementMode.CareerUnlocked => log != null && log.HasUnlockedCareer(career),
            CareerRequirementMode.CareerPointsAtLeast => log != null && log.GetPoints(career) >= Mathf.Max(0, requiredValue),
            CareerRequirementMode.CareerRankAtLeast => log != null && log.HasReachedRank(career, requiredValue),
            CareerRequirementMode.CareerTagJoined => log != null && log.HasJoinedCareerWithTag(tag),
            CareerRequirementMode.CareerCanJoin => career != null && career.CanJoin(player, evaluateAsMentor, out _),
            _ => log != null && log.HasJoinedCareer(career)
        };

        return mustBeMet ? result : !result;
    }
}
