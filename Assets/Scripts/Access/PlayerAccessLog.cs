using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerAccessLog : MonoBehaviour, ISavable {
    [Tooltip("Runtime/save history of access profile checks.")]
    [SerializeField] List<PlayerAccessState> accessStates = new List<PlayerAccessState>();

    public IReadOnlyList<PlayerAccessState> AccessStates => accessStates;
    public event Action<AccessProfileDefinition, bool> OnAccessChecked;
    public event Action OnAccessLogChanged;

    public bool RecordCheck(AccessProfileDefinition profile, bool passed, string message, string contextId = null, UnityEngine.Object context = null) {
        if(profile == null) {
            return false;
        }

        var state = GetOrCreateState(profile, contextId);
        state.lastCheckedDay = GetCurrentDay();
        state.lastCheckedAbsoluteHour = GetCurrentAbsoluteHour();
        state.lastMessage = message;

        if(passed) {
            state.passedCount++;
            state.lastPassedDay = state.lastCheckedDay;
            state.lastPassedAbsoluteHour = state.lastCheckedAbsoluteHour;
        } else {
            state.deniedCount++;
            state.lastDeniedDay = state.lastCheckedDay;
            state.lastDeniedAbsoluteHour = state.lastCheckedAbsoluteHour;
            state.lastDeniedReason = message;
        }

        OnAccessChecked?.Invoke(profile, passed);
        OnAccessLogChanged?.Invoke();
        profile.PublishChecked(GetComponent<PlayerController>(), passed, contextId, message, context != null ? context : this);
        return true;
    }

    public bool CheckAndRecord(AccessProfileDefinition profile, string contextId, out string failureMessage, UnityEngine.Object context = null) {
        if(profile == null) {
            failureMessage = "No access profile assigned.";
            return false;
        }

        var player = GetComponent<PlayerController>();
        bool passed = profile.CanAccess(player, out failureMessage);
        RecordCheck(profile, passed, passed ? profile.PassedMessage : failureMessage, contextId, context);
        return passed;
    }

    public bool HasPassed(AccessProfileDefinition profile, string contextId = null) {
        return GetPassedCount(profile, contextId) > 0;
    }

    public int GetPassedCount(AccessProfileDefinition profile, string contextId = null) {
        return GetStates(profile, contextId).Sum(state => Mathf.Max(0, state.passedCount));
    }

    public int GetDeniedCount(AccessProfileDefinition profile, string contextId = null) {
        return GetStates(profile, contextId).Sum(state => Mathf.Max(0, state.deniedCount));
    }

    public string GetLastDeniedReason(AccessProfileDefinition profile, string contextId = null) {
        return GetStates(profile, contextId)
            .OrderByDescending(state => state.lastDeniedAbsoluteHour)
            .FirstOrDefault(state => !string.IsNullOrWhiteSpace(state.lastDeniedReason))
            ?.lastDeniedReason;
    }

    PlayerAccessState GetOrCreateState(AccessProfileDefinition profile, string contextId) {
        var state = GetExactState(profile, contextId);
        if(state != null) {
            return state;
        }

        state = new PlayerAccessState {
            profileId = profile.Id,
            profileName = profile.DisplayName,
            category = profile.Category,
            contextId = contextId
        };
        accessStates.Add(state);
        return state;
    }

    PlayerAccessState GetExactState(AccessProfileDefinition profile, string contextId) {
        if(profile == null) {
            return null;
        }

        return accessStates.FirstOrDefault(state => state != null
            && state.profileId == profile.Id
            && string.Equals(state.contextId, contextId, StringComparison.Ordinal));
    }

    IEnumerable<PlayerAccessState> GetStates(AccessProfileDefinition profile, string contextId) {
        if(profile == null) {
            return Enumerable.Empty<PlayerAccessState>();
        }

        return accessStates.Where(state => state != null
            && state.profileId == profile.Id
            && (string.IsNullOrWhiteSpace(contextId) || state.contextId == contextId));
    }

    int GetCurrentDay() {
        return TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    public object CaptureState() {
        return new PlayerAccessLogSaveData {
            accessStates = accessStates
                .Where(state => state != null)
                .Select(state => state.ToSaveData())
                .ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerAccessLogSaveData;
        accessStates = saveData?.accessStates?.Where(entry => entry != null).Select(entry => new PlayerAccessState(entry)).ToList() ?? new List<PlayerAccessState>();
        OnAccessLogChanged?.Invoke();
    }
}

[Serializable]
public class PlayerAccessState {
    [Tooltip("Saved access profile id.")]
    public string profileId;
    [Tooltip("Saved access profile display name for fallback/debug output.")]
    public string profileName;
    [Tooltip("Saved access category.")]
    public AccessProfileCategory category;
    [Tooltip("Optional source/gate/context id where this access profile was checked.")]
    public string contextId;
    [Tooltip("Number of successful checks.")]
    [Min(0)]
    public int passedCount;
    [Tooltip("Number of denied checks.")]
    [Min(0)]
    public int deniedCount;
    [Tooltip("In-game day of the most recent check.")]
    public int lastCheckedDay = -1;
    [Tooltip("Absolute in-game hour of the most recent check.")]
    public int lastCheckedAbsoluteHour = -1;
    [Tooltip("In-game day of the most recent successful check.")]
    public int lastPassedDay = -1;
    [Tooltip("Absolute in-game hour of the most recent successful check.")]
    public int lastPassedAbsoluteHour = -1;
    [Tooltip("In-game day of the most recent denied check.")]
    public int lastDeniedDay = -1;
    [Tooltip("Absolute in-game hour of the most recent denied check.")]
    public int lastDeniedAbsoluteHour = -1;
    [Tooltip("Last human-readable message produced by this access profile.")]
    public string lastMessage;
    [Tooltip("Last human-readable reason access was denied.")]
    public string lastDeniedReason;

    public PlayerAccessState() {
    }

    public PlayerAccessState(PlayerAccessStateSaveData saveData) {
        if(saveData == null) {
            return;
        }

        profileId = saveData.profileId;
        profileName = saveData.profileName;
        category = saveData.category;
        contextId = saveData.contextId;
        passedCount = Mathf.Max(0, saveData.passedCount);
        deniedCount = Mathf.Max(0, saveData.deniedCount);
        lastCheckedDay = saveData.lastCheckedDay;
        lastCheckedAbsoluteHour = saveData.lastCheckedAbsoluteHour;
        lastPassedDay = saveData.lastPassedDay;
        lastPassedAbsoluteHour = saveData.lastPassedAbsoluteHour;
        lastDeniedDay = saveData.lastDeniedDay;
        lastDeniedAbsoluteHour = saveData.lastDeniedAbsoluteHour;
        lastMessage = saveData.lastMessage;
        lastDeniedReason = saveData.lastDeniedReason;
    }

    public PlayerAccessStateSaveData ToSaveData() {
        return new PlayerAccessStateSaveData {
            profileId = profileId,
            profileName = profileName,
            category = category,
            contextId = contextId,
            passedCount = passedCount,
            deniedCount = deniedCount,
            lastCheckedDay = lastCheckedDay,
            lastCheckedAbsoluteHour = lastCheckedAbsoluteHour,
            lastPassedDay = lastPassedDay,
            lastPassedAbsoluteHour = lastPassedAbsoluteHour,
            lastDeniedDay = lastDeniedDay,
            lastDeniedAbsoluteHour = lastDeniedAbsoluteHour,
            lastMessage = lastMessage,
            lastDeniedReason = lastDeniedReason
        };
    }
}

[Serializable]
public class PlayerAccessLogSaveData {
    public List<PlayerAccessStateSaveData> accessStates;
}

[Serializable]
public class PlayerAccessStateSaveData {
    public string profileId;
    public string profileName;
    public AccessProfileCategory category;
    public string contextId;
    public int passedCount;
    public int deniedCount;
    public int lastCheckedDay;
    public int lastCheckedAbsoluteHour;
    public int lastPassedDay;
    public int lastPassedAbsoluteHour;
    public int lastDeniedDay;
    public int lastDeniedAbsoluteHour;
    public string lastMessage;
    public string lastDeniedReason;
}
