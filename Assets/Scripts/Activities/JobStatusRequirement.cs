using UnityEngine;

public enum JobStatusRequirementMode {
    ActiveJob,
    CompletedJobCount
}

[CreateAssetMenu(menuName = "Activities/Requirements/Job Status Requirement")]
public class JobStatusRequirement : ActivityRequirement {
    [Tooltip("Which job state this requirement checks.")]
    [SerializeField] JobStatusRequirementMode mode = JobStatusRequirementMode.ActiveJob;
    [Tooltip("Job checked by this requirement.")]
    [SerializeField] JobDefinition job;
    [Tooltip("Optional board id filter. Empty accepts any board.")]
    [SerializeField] string boardId;
    [Tooltip("Minimum completed count required when mode is Completed Job Count.")]
    [Min(0)]
    [SerializeField] int requiredCount = 1;
    [Tooltip("If enabled, the selected job condition must be true. If disabled, it must be false.")]
    [SerializeField] bool mustMatch = true;

    public override bool IsMet(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerJobLog>() : null;
        bool result = false;

        if(log != null && job != null) {
            result = mode == JobStatusRequirementMode.ActiveJob
                ? log.HasActiveJob(job, boardId)
                : log.GetCompletedCount(job, boardId) >= Mathf.Max(0, requiredCount);
        }

        return mustMatch ? result : !result;
    }
}
