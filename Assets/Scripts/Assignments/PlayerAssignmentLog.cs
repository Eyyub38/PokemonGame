using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerAssignmentLog : MonoBehaviour, ISavable {
    [Tooltip("Runtime/save ids for assignments unlocked for the player.")]
    [SerializeField] List<string> unlockedAssignmentIds = new List<string>();
    [Tooltip("Runtime/save list of currently accepted assignments.")]
    [SerializeField] List<PlayerAssignmentState> activeAssignments = new List<PlayerAssignmentState>();
    [Tooltip("Runtime/save completion history used by repeat/cooldown rules.")]
    [SerializeField] List<PlayerAssignmentCompletionState> completedAssignments = new List<PlayerAssignmentCompletionState>();

    public IReadOnlyList<string> UnlockedAssignmentIds => unlockedAssignmentIds;
    public IReadOnlyList<PlayerAssignmentState> ActiveAssignments => activeAssignments;
    public IReadOnlyList<PlayerAssignmentCompletionState> CompletedAssignments => completedAssignments;
    public event Action<AssignmentDefinition> OnAssignmentUnlocked;
    public event Action<AssignmentDefinition> OnAssignmentAccepted;
    public event Action<AssignmentDefinition> OnAssignmentCompleted;
    public event Action<string> OnAssignmentAbandoned;
    public event Action OnAssignmentLogChanged;

    public bool HasUnlockedAssignment(AssignmentDefinition assignment) {
        return assignment != null && (assignment.UnlockedByDefault || HasUnlockedAssignment(assignment.Id));
    }

    public bool HasUnlockedAssignment(string assignmentId) {
        return !string.IsNullOrWhiteSpace(assignmentId) && unlockedAssignmentIds.Contains(assignmentId);
    }

    public bool UnlockAssignment(AssignmentDefinition assignment, string sourceId = null) {
        if(assignment == null || HasUnlockedAssignment(assignment.Id)) {
            return false;
        }

        unlockedAssignmentIds.Add(assignment.Id);
        OnAssignmentUnlocked?.Invoke(assignment);
        OnAssignmentLogChanged?.Invoke();
        assignment.PublishUnlocked(GetComponent<PlayerController>(), sourceId);
        return true;
    }

    public bool HasActiveAssignment(AssignmentDefinition assignment, string sourceId = null) {
        return assignment != null && HasActiveAssignment(assignment.Id, sourceId);
    }

    public bool HasActiveAssignment(string assignmentId, string sourceId = null) {
        if(string.IsNullOrWhiteSpace(assignmentId)) {
            return false;
        }

        return activeAssignments.Any(a => a != null
            && a.assignmentId == assignmentId
            && (string.IsNullOrWhiteSpace(sourceId) || a.sourceId == sourceId));
    }

    public PlayerAssignmentState GetActiveAssignment(AssignmentDefinition assignment, string sourceId = null) {
        return assignment != null ? GetActiveAssignment(assignment.Id, sourceId) : null;
    }

    public PlayerAssignmentState GetActiveAssignment(string assignmentId, string sourceId = null) {
        if(string.IsNullOrWhiteSpace(assignmentId)) {
            return null;
        }

        return activeAssignments.FirstOrDefault(a => a != null
            && a.assignmentId == assignmentId
            && (string.IsNullOrWhiteSpace(sourceId) || a.sourceId == sourceId));
    }

    public bool CanAccept(AssignmentDefinition assignment, string sourceId, AssignmentRepeatMode repeatMode, int cooldownHours, out string failureMessage) {
        if(assignment == null) {
            failureMessage = "No assignment selected.";
            return false;
        }

        var history = GetCompletion(assignment.Id, sourceId);
        if(repeatMode == AssignmentRepeatMode.Once && history != null && history.completedCount > 0) {
            failureMessage = $"{assignment.DisplayName} has already been completed.";
            return false;
        }

        if(repeatMode == AssignmentRepeatMode.Daily && history != null && history.lastCompletedDay == GetCurrentDay()) {
            failureMessage = $"{assignment.DisplayName} can only be completed once per day.";
            return false;
        }

        if(repeatMode == AssignmentRepeatMode.CooldownHours && history != null && history.lastCompletedAbsoluteHour >= 0) {
            int elapsed = GetCurrentAbsoluteHour() - history.lastCompletedAbsoluteHour;
            if(elapsed < Mathf.Max(0, cooldownHours)) {
                failureMessage = $"{assignment.DisplayName} will be available again in {cooldownHours - elapsed} hour(s).";
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    public bool Accept(AssignmentDefinition assignment, string sourceId, out string failureMessage) {
        if(assignment == null) {
            failureMessage = "No assignment selected.";
            return false;
        }

        if(!assignment.CanAccept(GetComponent<PlayerController>(), this, sourceId, out failureMessage)) {
            return false;
        }

        var state = new PlayerAssignmentState {
            assignmentId = assignment.Id,
            assignmentName = assignment.DisplayName,
            sourceId = sourceId,
            acceptedDay = GetCurrentDay(),
            acceptedAbsoluteHour = GetCurrentAbsoluteHour()
        };
        activeAssignments.Add(state);
        assignment.ApplyAcceptanceEffects(GetComponent<PlayerController>(), sourceId);
        OnAssignmentAccepted?.Invoke(assignment);
        OnAssignmentLogChanged?.Invoke();
        assignment.PublishAccepted(GetComponent<PlayerController>(), sourceId);
        return true;
    }

    public bool Complete(AssignmentDefinition assignment, string sourceId, out string failureMessage) {
        if(assignment == null) {
            failureMessage = "No assignment selected.";
            return false;
        }

        var state = GetActiveAssignment(assignment, sourceId);
        if(state == null) {
            failureMessage = $"{assignment.DisplayName} is not active.";
            return false;
        }

        if(!assignment.CanComplete(GetComponent<PlayerController>(), state, out failureMessage)) {
            return false;
        }

        assignment.ApplyCompletionRewards(GetComponent<PlayerController>());
        activeAssignments.Remove(state);
        RecordCompletion(assignment, sourceId);
        OnAssignmentCompleted?.Invoke(assignment);
        OnAssignmentLogChanged?.Invoke();
        assignment.PublishCompleted(GetComponent<PlayerController>(), sourceId);
        failureMessage = null;
        return true;
    }

    public bool Abandon(AssignmentDefinition assignment, string sourceId = null) {
        return assignment != null && Abandon(assignment.Id, sourceId);
    }

    public bool Abandon(string assignmentId, string sourceId = null) {
        var state = GetActiveAssignment(assignmentId, sourceId);
        if(state == null) {
            return false;
        }

        activeAssignments.Remove(state);
        OnAssignmentAbandoned?.Invoke(assignmentId);
        OnAssignmentLogChanged?.Invoke();
        GameEventPublishing.PublishOptional(
            null,
            $"assignment.abandoned.{assignmentId}",
            $"{state.assignmentName} abandoned.",
            GameEventCategory.Assignment,
            GameEventImportance.Info,
            this,
            "PlayerAssignmentLog",
            GameEventScope.Player,
            showInFeed: true,
            writeToDebugLog: false,
            GameEventPublishing.Value("assignmentId", assignmentId),
            GameEventPublishing.Value("assignmentName", state.assignmentName),
            GameEventPublishing.Value("sourceId", state.sourceId));
        return true;
    }

    public int GetCompletedCount(AssignmentDefinition assignment, string sourceId = null) {
        return assignment != null ? GetCompletedCount(assignment.Id, sourceId) : 0;
    }

    public int GetCompletedCount(string assignmentId, string sourceId = null) {
        var history = GetCompletion(assignmentId, sourceId);
        return history != null ? Mathf.Max(0, history.completedCount) : 0;
    }

    public int GetCompletedCountWithTag(string tag) {
        if(string.IsNullOrWhiteSpace(tag)) {
            return 0;
        }

        int count = 0;
        foreach(var history in completedAssignments) {
            var assignment = ResolveAssignment(history?.assignmentId);
            if(assignment != null && assignment.HasTag(tag)) {
                count += Mathf.Max(0, history.completedCount);
            }
        }

        return count;
    }

    PlayerAssignmentCompletionState GetCompletion(string assignmentId, string sourceId) {
        if(string.IsNullOrWhiteSpace(assignmentId)) {
            return null;
        }

        return completedAssignments.FirstOrDefault(a => a != null
            && a.assignmentId == assignmentId
            && (string.IsNullOrWhiteSpace(sourceId) || a.sourceId == sourceId));
    }

    void RecordCompletion(AssignmentDefinition assignment, string sourceId) {
        var history = completedAssignments.FirstOrDefault(a => a != null && a.assignmentId == assignment.Id && a.sourceId == sourceId);
        if(history == null) {
            history = new PlayerAssignmentCompletionState {
                assignmentId = assignment.Id,
                assignmentName = assignment.DisplayName,
                sourceId = sourceId
            };
            completedAssignments.Add(history);
        }

        history.completedCount++;
        history.lastCompletedDay = GetCurrentDay();
        history.lastCompletedAbsoluteHour = GetCurrentAbsoluteHour();
    }

    AssignmentDefinition ResolveAssignment(string assignmentId) {
        if(string.IsNullOrWhiteSpace(assignmentId)) {
            return null;
        }

        return Resources.LoadAll<AssignmentDefinition>("").FirstOrDefault(assignment => assignment != null && assignment.Id == assignmentId);
    }

    int GetCurrentDay() {
        return TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    public object CaptureState() {
        return new PlayerAssignmentLogSaveData {
            unlockedAssignmentIds = unlockedAssignmentIds.Distinct().ToList(),
            activeAssignments = activeAssignments.Where(a => a != null).Select(a => a.ToSaveData()).ToList(),
            completedAssignments = completedAssignments.Where(a => a != null).Select(a => a.ToSaveData()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerAssignmentLogSaveData;
        unlockedAssignmentIds = saveData?.unlockedAssignmentIds?.Distinct().ToList() ?? new List<string>();
        activeAssignments = saveData?.activeAssignments?.Where(a => a != null).Select(a => new PlayerAssignmentState(a)).ToList() ?? new List<PlayerAssignmentState>();
        completedAssignments = saveData?.completedAssignments?.Where(a => a != null).Select(a => new PlayerAssignmentCompletionState(a)).ToList() ?? new List<PlayerAssignmentCompletionState>();
        OnAssignmentLogChanged?.Invoke();
    }
}

[Serializable]
public class PlayerAssignmentState {
    [Tooltip("Saved assignment id.")]
    public string assignmentId;
    [Tooltip("Saved assignment display name.")]
    public string assignmentName;
    [Tooltip("Source id where this assignment was accepted.")]
    public string sourceId;
    [Tooltip("In-game day when this assignment was accepted.")]
    public int acceptedDay;
    [Tooltip("Absolute in-game hour when this assignment was accepted.")]
    public int acceptedAbsoluteHour;

    public PlayerAssignmentState() {
    }

    public PlayerAssignmentState(PlayerAssignmentStateSaveData saveData) {
        if(saveData == null) {
            return;
        }

        assignmentId = saveData.assignmentId;
        assignmentName = saveData.assignmentName;
        sourceId = saveData.sourceId;
        acceptedDay = saveData.acceptedDay;
        acceptedAbsoluteHour = saveData.acceptedAbsoluteHour;
    }

    public PlayerAssignmentStateSaveData ToSaveData() {
        return new PlayerAssignmentStateSaveData {
            assignmentId = assignmentId,
            assignmentName = assignmentName,
            sourceId = sourceId,
            acceptedDay = acceptedDay,
            acceptedAbsoluteHour = acceptedAbsoluteHour
        };
    }
}

[Serializable]
public class PlayerAssignmentCompletionState {
    [Tooltip("Saved assignment id.")]
    public string assignmentId;
    [Tooltip("Saved assignment display name.")]
    public string assignmentName;
    [Tooltip("Source id where this assignment was completed.")]
    public string sourceId;
    [Tooltip("Total number of completions for this assignment/source pair.")]
    [Min(0)]
    public int completedCount;
    [Tooltip("In-game day when this assignment was last completed.")]
    public int lastCompletedDay = -1;
    [Tooltip("Absolute in-game hour when this assignment was last completed.")]
    public int lastCompletedAbsoluteHour = -1;

    public PlayerAssignmentCompletionState() {
    }

    public PlayerAssignmentCompletionState(PlayerAssignmentCompletionStateSaveData saveData) {
        if(saveData == null) {
            return;
        }

        assignmentId = saveData.assignmentId;
        assignmentName = saveData.assignmentName;
        sourceId = saveData.sourceId;
        completedCount = Mathf.Max(0, saveData.completedCount);
        lastCompletedDay = saveData.lastCompletedDay;
        lastCompletedAbsoluteHour = saveData.lastCompletedAbsoluteHour;
    }

    public PlayerAssignmentCompletionStateSaveData ToSaveData() {
        return new PlayerAssignmentCompletionStateSaveData {
            assignmentId = assignmentId,
            assignmentName = assignmentName,
            sourceId = sourceId,
            completedCount = completedCount,
            lastCompletedDay = lastCompletedDay,
            lastCompletedAbsoluteHour = lastCompletedAbsoluteHour
        };
    }
}

[Serializable]
public class PlayerAssignmentLogSaveData {
    public List<string> unlockedAssignmentIds;
    public List<PlayerAssignmentStateSaveData> activeAssignments;
    public List<PlayerAssignmentCompletionStateSaveData> completedAssignments;
}

[Serializable]
public class PlayerAssignmentStateSaveData {
    public string assignmentId;
    public string assignmentName;
    public string sourceId;
    public int acceptedDay;
    public int acceptedAbsoluteHour;
}

[Serializable]
public class PlayerAssignmentCompletionStateSaveData {
    public string assignmentId;
    public string assignmentName;
    public string sourceId;
    public int completedCount;
    public int lastCompletedDay;
    public int lastCompletedAbsoluteHour;
}
