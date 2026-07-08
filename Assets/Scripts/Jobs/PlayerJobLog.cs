using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerJobLog : MonoBehaviour, ISavable {
    [Tooltip("Runtime/save list of currently accepted jobs.")]
    [SerializeField] List<PlayerJobState> activeJobs = new List<PlayerJobState>();
    [Tooltip("Runtime/save completion history used by repeat/cooldown rules.")]
    [SerializeField] List<PlayerJobCompletionState> completedJobs = new List<PlayerJobCompletionState>();

    public IReadOnlyList<PlayerJobState> ActiveJobs => activeJobs;
    public IReadOnlyList<PlayerJobCompletionState> CompletedJobs => completedJobs;
    public event Action<JobDefinition> OnJobAccepted;
    public event Action<JobDefinition> OnJobCompleted;
    public event Action<string> OnJobAbandoned;

    public bool HasActiveJob(JobDefinition job, string boardId = null) {
        return job != null && HasActiveJob(job.Id, boardId);
    }

    public bool HasActiveJob(string jobId, string boardId = null) {
        if(string.IsNullOrWhiteSpace(jobId)) {
            return false;
        }

        return activeJobs.Any(j => j != null
            && j.jobId == jobId
            && (string.IsNullOrWhiteSpace(boardId) || j.boardId == boardId));
    }

    public PlayerJobState GetActiveJob(JobDefinition job, string boardId = null) {
        return job != null ? GetActiveJob(job.Id, boardId) : null;
    }

    public PlayerJobState GetActiveJob(string jobId, string boardId = null) {
        if(string.IsNullOrWhiteSpace(jobId)) {
            return null;
        }

        return activeJobs.FirstOrDefault(j => j != null
            && j.jobId == jobId
            && (string.IsNullOrWhiteSpace(boardId) || j.boardId == boardId));
    }

    public bool CanAccept(JobDefinition job, string boardId, JobRepeatMode repeatMode, int cooldownHours, out string failureMessage) {
        if(job == null) {
            failureMessage = "No job selected.";
            return false;
        }

        var history = GetCompletion(job.Id, boardId);
        if(repeatMode == JobRepeatMode.Once && history != null && history.completedCount > 0) {
            failureMessage = $"{job.DisplayName} has already been completed.";
            return false;
        }

        if(repeatMode == JobRepeatMode.Daily && history != null && history.lastCompletedDay == GetCurrentDay()) {
            failureMessage = $"{job.DisplayName} can only be completed once per day.";
            return false;
        }

        if(repeatMode == JobRepeatMode.CooldownHours && history != null && history.lastCompletedAbsoluteHour >= 0) {
            int elapsed = GetCurrentAbsoluteHour() - history.lastCompletedAbsoluteHour;
            if(elapsed < Mathf.Max(0, cooldownHours)) {
                failureMessage = $"{job.DisplayName} will be available again in {cooldownHours - elapsed} hour(s).";
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    public bool Accept(JobDefinition job, string boardId, out string failureMessage) {
        if(job == null) {
            failureMessage = "No job selected.";
            return false;
        }

        if(!job.CanAccept(GetComponent<PlayerController>(), this, boardId, out failureMessage)) {
            return false;
        }

        var state = new PlayerJobState {
            jobId = job.Id,
            jobName = job.DisplayName,
            boardId = boardId,
            acceptedDay = GetCurrentDay(),
            acceptedAbsoluteHour = GetCurrentAbsoluteHour(),
            objectiveBaselines = job.CaptureBaselines(GetComponent<PlayerController>())
        };
        activeJobs.Add(state);
        OnJobAccepted?.Invoke(job);
        job.PublishAccepted(GetComponent<PlayerController>(), boardId);
        return true;
    }

    public bool Complete(JobDefinition job, string boardId, out string failureMessage) {
        if(job == null) {
            failureMessage = "No job selected.";
            return false;
        }

        var state = GetActiveJob(job, boardId);
        if(state == null) {
            failureMessage = $"{job.DisplayName} is not active.";
            return false;
        }

        if(!job.TryComplete(GetComponent<PlayerController>(), state, out failureMessage)) {
            return false;
        }

        activeJobs.Remove(state);
        RecordCompletion(job, boardId);
        OnJobCompleted?.Invoke(job);
        return true;
    }

    public bool Abandon(JobDefinition job, string boardId = null) {
        return job != null && Abandon(job.Id, boardId);
    }

    public bool Abandon(string jobId, string boardId = null) {
        var state = GetActiveJob(jobId, boardId);
        if(state == null) {
            return false;
        }

        activeJobs.Remove(state);
        OnJobAbandoned?.Invoke(jobId);
        GameEventPublishing.PublishOptional(
            null,
            $"job.abandoned.{jobId}",
            $"{state.jobName} abandoned.",
            GameEventCategory.Job,
            GameEventImportance.Info,
            this,
            "PlayerJobLog",
            GameEventScope.Player,
            showInFeed: true,
            writeToDebugLog: false,
            GameEventPublishing.Value("jobId", jobId),
            GameEventPublishing.Value("jobName", state.jobName),
            GameEventPublishing.Value("boardId", state.boardId));
        return true;
    }

    public int GetCompletedCount(JobDefinition job, string boardId = null) {
        return job != null ? GetCompletedCount(job.Id, boardId) : 0;
    }

    public int GetCompletedCount(string jobId, string boardId = null) {
        var history = GetCompletion(jobId, boardId);
        return history != null ? Mathf.Max(0, history.completedCount) : 0;
    }

    PlayerJobCompletionState GetCompletion(string jobId, string boardId) {
        if(string.IsNullOrWhiteSpace(jobId)) {
            return null;
        }

        return completedJobs.FirstOrDefault(j => j != null
            && j.jobId == jobId
            && (string.IsNullOrWhiteSpace(boardId) || j.boardId == boardId));
    }

    void RecordCompletion(JobDefinition job, string boardId) {
        var history = completedJobs.FirstOrDefault(j => j != null && j.jobId == job.Id && j.boardId == boardId);
        if(history == null) {
            history = new PlayerJobCompletionState {
                jobId = job.Id,
                jobName = job.DisplayName,
                boardId = boardId
            };
            completedJobs.Add(history);
        }

        history.completedCount++;
        history.lastCompletedDay = GetCurrentDay();
        history.lastCompletedAbsoluteHour = GetCurrentAbsoluteHour();
    }

    int GetCurrentDay() {
        return TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    public object CaptureState() {
        return new PlayerJobLogSaveData {
            activeJobs = activeJobs.Where(j => j != null).Select(j => j.ToSaveData()).ToList(),
            completedJobs = completedJobs.Where(j => j != null).Select(j => j.ToSaveData()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerJobLogSaveData;
        activeJobs = saveData?.activeJobs?.Where(j => j != null).Select(j => new PlayerJobState(j)).ToList() ?? new List<PlayerJobState>();
        completedJobs = saveData?.completedJobs?.Where(j => j != null).Select(j => new PlayerJobCompletionState(j)).ToList() ?? new List<PlayerJobCompletionState>();
    }
}

[Serializable]
public class PlayerJobState {
    [Tooltip("Saved job id.")]
    public string jobId;
    [Tooltip("Saved job display name.")]
    public string jobName;
    [Tooltip("Board id where this job was accepted.")]
    public string boardId;
    [Tooltip("In-game day when this job was accepted.")]
    public int acceptedDay;
    [Tooltip("Absolute in-game hour when this job was accepted.")]
    public int acceptedAbsoluteHour;
    [Tooltip("Objective baseline counts captured on accept.")]
    public List<JobObjectiveBaseline> objectiveBaselines = new List<JobObjectiveBaseline>();

    public PlayerJobState() {
    }

    public PlayerJobState(PlayerJobStateSaveData saveData) {
        if(saveData == null) {
            return;
        }

        jobId = saveData.jobId;
        jobName = saveData.jobName;
        boardId = saveData.boardId;
        acceptedDay = saveData.acceptedDay;
        acceptedAbsoluteHour = saveData.acceptedAbsoluteHour;
        objectiveBaselines = saveData.objectiveBaselines ?? new List<JobObjectiveBaseline>();
    }

    public int GetBaseline(int objectiveIndex) {
        var baseline = objectiveBaselines?.FirstOrDefault(b => b != null && b.objectiveIndex == objectiveIndex);
        return baseline != null ? baseline.baseline : 0;
    }

    public PlayerJobStateSaveData ToSaveData() {
        return new PlayerJobStateSaveData {
            jobId = jobId,
            jobName = jobName,
            boardId = boardId,
            acceptedDay = acceptedDay,
            acceptedAbsoluteHour = acceptedAbsoluteHour,
            objectiveBaselines = objectiveBaselines
        };
    }
}

[Serializable]
public class PlayerJobCompletionState {
    [Tooltip("Saved job id.")]
    public string jobId;
    [Tooltip("Saved job display name.")]
    public string jobName;
    [Tooltip("Board id where this job was completed.")]
    public string boardId;
    [Tooltip("Total number of completions for this job/board pair.")]
    [Min(0)]
    public int completedCount;
    [Tooltip("In-game day when this job was last completed.")]
    public int lastCompletedDay = -1;
    [Tooltip("Absolute in-game hour when this job was last completed.")]
    public int lastCompletedAbsoluteHour = -1;

    public PlayerJobCompletionState() {
    }

    public PlayerJobCompletionState(PlayerJobCompletionStateSaveData saveData) {
        if(saveData == null) {
            return;
        }

        jobId = saveData.jobId;
        jobName = saveData.jobName;
        boardId = saveData.boardId;
        completedCount = Mathf.Max(0, saveData.completedCount);
        lastCompletedDay = saveData.lastCompletedDay;
        lastCompletedAbsoluteHour = saveData.lastCompletedAbsoluteHour;
    }

    public PlayerJobCompletionStateSaveData ToSaveData() {
        return new PlayerJobCompletionStateSaveData {
            jobId = jobId,
            jobName = jobName,
            boardId = boardId,
            completedCount = completedCount,
            lastCompletedDay = lastCompletedDay,
            lastCompletedAbsoluteHour = lastCompletedAbsoluteHour
        };
    }
}

[Serializable]
public class PlayerJobLogSaveData {
    public List<PlayerJobStateSaveData> activeJobs;
    public List<PlayerJobCompletionStateSaveData> completedJobs;
}

[Serializable]
public class PlayerJobStateSaveData {
    public string jobId;
    public string jobName;
    public string boardId;
    public int acceptedDay;
    public int acceptedAbsoluteHour;
    public List<JobObjectiveBaseline> objectiveBaselines;
}

[Serializable]
public class PlayerJobCompletionStateSaveData {
    public string jobId;
    public string jobName;
    public string boardId;
    public int completedCount;
    public int lastCompletedDay;
    public int lastCompletedAbsoluteHour;
}
