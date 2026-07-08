using UnityEngine;

[CreateAssetMenu(menuName = "Activities/Requirements/Activity Completion Requirement")]
public class ActivityCompletionRequirement : ActivityRequirement {
    [Tooltip("Activity whose journal history is checked.")]
    [SerializeField] ActivityDefinition activity;
    [Tooltip("Minimum total completions required across the save file.")]
    [Min(0)]
    [SerializeField] int minimumLifetimeCompletions = 1;
    [Tooltip("Minimum number of different in-game days where this activity has been completed.")]
    [Min(0)]
    [SerializeField] int minimumActiveDays;

    public override bool IsMet(PlayerController player) {
        if(player == null || activity == null) {
            return false;
        }

        var journal = player.GetComponent<PlayerActivityJournal>();
        if(journal == null) {
            return false;
        }

        if(minimumLifetimeCompletions > 0 && journal.GetLifetimeCompletions(activity) < minimumLifetimeCompletions) {
            return false;
        }

        if(minimumActiveDays > 0 && journal.GetActiveDays(activity) < minimumActiveDays) {
            return false;
        }

        return true;
    }
}
