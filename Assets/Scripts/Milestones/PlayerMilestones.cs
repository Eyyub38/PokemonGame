using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerMilestones : MonoBehaviour, ISavable {
    [Tooltip("Runtime/save ids for completed milestones.")]
    [SerializeField] List<string> completedMilestoneIds = new List<string>();

    public IReadOnlyList<string> CompletedMilestoneIds => completedMilestoneIds;
    public event Action<MilestoneDefinition> OnMilestoneCompleted;

    public bool HasMilestone(MilestoneDefinition milestone) {
        return milestone != null && HasMilestone(milestone.Id);
    }

    public bool HasMilestone(string milestoneId) {
        return !string.IsNullOrWhiteSpace(milestoneId) && completedMilestoneIds.Contains(milestoneId);
    }

    public bool CompleteMilestone(MilestoneDefinition milestone) {
        if(milestone == null || HasMilestone(milestone)) {
            return false;
        }

        completedMilestoneIds.Add(milestone.Id);
        OnMilestoneCompleted?.Invoke(milestone);
        PublishMilestoneEvent(milestone);
        return true;
    }

    public void CompleteMilestones(IEnumerable<MilestoneDefinition> milestones) {
        if(milestones == null) {
            return;
        }

        foreach(var milestone in milestones) {
            CompleteMilestone(milestone);
        }
    }

    public object CaptureState() {
        return completedMilestoneIds.Distinct().ToList();
    }

    public void RestoreState(object state) {
        completedMilestoneIds = state as List<string> ?? new List<string>();
    }

    void PublishMilestoneEvent(MilestoneDefinition milestone) {
        GameEventPublishing.PublishOptional(
            milestone.CompletedEvent,
            $"milestone.completed.{milestone.Id}",
            $"{milestone.DisplayName} completed.",
            GameEventCategory.Milestone,
            GameEventImportance.Success,
            this,
            "PlayerMilestones",
            GameEventScope.Player,
            showInFeed: !milestone.Hidden,
            writeToDebugLog: false,
            GameEventPublishing.Value("milestoneId", milestone.Id),
            GameEventPublishing.Value("milestoneName", milestone.DisplayName),
            GameEventPublishing.Value("hidden", milestone.Hidden));
    }
}
