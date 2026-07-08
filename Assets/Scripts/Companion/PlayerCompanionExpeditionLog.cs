using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerCompanionExpeditionLog : MonoBehaviour, ISavable {
    [Tooltip("Runtime/save list of active companion expeditions.")]
    [SerializeField] List<PlayerCompanionExpeditionState> activeExpeditions = new List<PlayerCompanionExpeditionState>();
    [Tooltip("Runtime/save history of claimed companion expeditions.")]
    [SerializeField] List<PlayerCompanionExpeditionHistory> expeditionHistory = new List<PlayerCompanionExpeditionHistory>();

    public IReadOnlyList<PlayerCompanionExpeditionState> ActiveExpeditions => activeExpeditions;
    public IReadOnlyList<PlayerCompanionExpeditionHistory> ExpeditionHistory => expeditionHistory;
    public event Action OnCompanionExpeditionsChanged;
    public event Action<PlayerCompanionExpeditionHistory> OnCompanionExpeditionClaimed;

    public bool HasActiveExpedition(CompanionExpeditionDefinition expedition, string sourceId = null) {
        return expedition != null && activeExpeditions.Any(state => state != null
            && state.expeditionId == expedition.Id
            && (string.IsNullOrWhiteSpace(sourceId) || state.sourceId == sourceId));
    }

    public bool HasActiveExpeditionForCompanion(string companionId) {
        return !string.IsNullOrWhiteSpace(companionId)
            && activeExpeditions.Any(state => state != null && state.companionId == companionId);
    }

    public List<PlayerCompanionExpeditionState> GetReadyExpeditions(CompanionExpeditionDefinition expedition = null, string sourceId = null) {
        return activeExpeditions
            .Where(state => state != null
                && state.IsReady()
                && (expedition == null || state.expeditionId == expedition.Id)
                && (string.IsNullOrWhiteSpace(sourceId) || state.sourceId == sourceId))
            .ToList();
    }

    public int GetCompletedCount(CompanionExpeditionDefinition expedition, string sourceId = null, bool? success = null) {
        if(expedition == null) {
            return 0;
        }

        return expeditionHistory.Count(history => history != null
            && history.expeditionId == expedition.Id
            && (string.IsNullOrWhiteSpace(sourceId) || history.sourceId == sourceId)
            && (!success.HasValue || history.success == success.Value));
    }

    public bool CanStart(CompanionExpeditionDefinition expedition, string sourceId, CompanionExpeditionRepeatMode repeatMode, int cooldownHours, out string failureMessage) {
        if(expedition == null) {
            failureMessage = "No companion expedition selected.";
            return false;
        }

        var history = GetLatestHistory(expedition.Id, sourceId);
        if(repeatMode == CompanionExpeditionRepeatMode.Once && history != null) {
            failureMessage = $"{expedition.DisplayName} has already been completed.";
            return false;
        }

        if(repeatMode == CompanionExpeditionRepeatMode.Daily && history != null && history.claimedDay == GetCurrentDay()) {
            failureMessage = $"{expedition.DisplayName} can only be completed once per day.";
            return false;
        }

        if(repeatMode == CompanionExpeditionRepeatMode.CooldownHours && history != null && history.claimedAbsoluteHour >= 0) {
            int elapsed = GetCurrentAbsoluteHour() - history.claimedAbsoluteHour;
            if(elapsed < Mathf.Max(0, cooldownHours)) {
                failureMessage = $"{expedition.DisplayName} will be available again in {cooldownHours - elapsed} hour(s).";
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    public bool TryStart(PlayerController player, CompanionExpeditionDefinition expedition, CompanionController companion, string sourceId, out string failureMessage) {
        if(expedition == null) {
            failureMessage = "No companion expedition selected.";
            return false;
        }

        if(!expedition.CanStart(player, companion, this, sourceId, out failureMessage)) {
            return false;
        }

        if(expedition.StartActivity != null && !expedition.StartActivity.TryPayCosts(player, out failureMessage)) {
            return false;
        }

        int currentHour = GetCurrentAbsoluteHour();
        var state = new PlayerCompanionExpeditionState {
            expeditionId = expedition.Id,
            expeditionName = expedition.DisplayName,
            category = expedition.Category,
            sourceId = sourceId,
            companionId = companion.CompanionId,
            companionName = companion.CompanionName,
            roleId = companion.RoleDefinition != null ? companion.RoleDefinition.Id : null,
            startedDay = GetCurrentDay(),
            startedAbsoluteHour = currentHour,
            readyAbsoluteHour = currentHour + expedition.DurationHours,
            successChance = expedition.GetSuccessChance(companion)
        };

        activeExpeditions.Add(state);
        if(expedition.StopFollowingOnStart) {
            companion.StopFollowing();
        }

        expedition.ApplyStarted(player, companion, sourceId);
        OnCompanionExpeditionsChanged?.Invoke();
        failureMessage = null;
        return true;
    }

    public bool TryClaim(PlayerController player, CompanionExpeditionDefinition expedition, PlayerCompanionExpeditionState state, out string failureMessage) {
        return TryClaim(player, expedition, state, out _, out failureMessage);
    }

    public bool TryClaim(PlayerController player, CompanionExpeditionDefinition expedition, PlayerCompanionExpeditionState state, out PlayerCompanionExpeditionHistory claimedHistory, out string failureMessage) {
        claimedHistory = null;
        if(expedition == null) {
            failureMessage = "No companion expedition selected.";
            return false;
        }

        if(state == null) {
            state = GetReadyExpeditions(expedition).FirstOrDefault();
        }

        if(!expedition.CanClaim(player, state, out failureMessage)) {
            return false;
        }

        if(expedition.ClaimActivity != null && !expedition.ClaimActivity.TryPayCosts(player, out failureMessage)) {
            return false;
        }

        var companion = ResolveCompanion(state.companionId);
        bool success = UnityEngine.Random.value <= Mathf.Clamp01(state.successChance);
        expedition.ApplyClaimed(player, companion, state.sourceId, success, state.successChance);
        if(expedition.ResumeFollowingOnClaim && companion != null && player != null) {
            companion.StartFollowing(player);
        }

        activeExpeditions.Remove(state);
        claimedHistory = PlayerCompanionExpeditionHistory.FromState(state, GetCurrentDay(), GetCurrentAbsoluteHour(), success);
        expeditionHistory.Add(claimedHistory);
        OnCompanionExpeditionClaimed?.Invoke(claimedHistory);
        OnCompanionExpeditionsChanged?.Invoke();
        failureMessage = null;
        return true;
    }

    public bool TryClaimFirstReady(PlayerController player, CompanionExpeditionDefinition expedition, string sourceId, out string failureMessage) {
        var state = GetReadyExpeditions(expedition, sourceId).FirstOrDefault();
        return TryClaim(player, expedition, state, out failureMessage);
    }

    public bool TryClaimFirstReady(PlayerController player, CompanionExpeditionDefinition expedition, string sourceId, out PlayerCompanionExpeditionHistory claimedHistory, out string failureMessage) {
        var state = GetReadyExpeditions(expedition, sourceId).FirstOrDefault();
        return TryClaim(player, expedition, state, out claimedHistory, out failureMessage);
    }

    public int ClaimAllReady(PlayerController player, CompanionExpeditionDefinition expedition, string sourceId) {
        int claimed = 0;
        foreach(var state in GetReadyExpeditions(expedition, sourceId).ToList()) {
            if(TryClaim(player, expedition, state, out _)) {
                claimed++;
            }
        }
        return claimed;
    }

    PlayerCompanionExpeditionHistory GetLatestHistory(string expeditionId, string sourceId) {
        if(string.IsNullOrWhiteSpace(expeditionId)) {
            return null;
        }

        return expeditionHistory
            .Where(history => history != null
                && history.expeditionId == expeditionId
                && (string.IsNullOrWhiteSpace(sourceId) || history.sourceId == sourceId))
            .OrderByDescending(history => history.claimedAbsoluteHour)
            .FirstOrDefault();
    }

    CompanionController ResolveCompanion(string companionId) {
        if(string.IsNullOrWhiteSpace(companionId)) {
            return null;
        }

        return FindObjectsByType<CompanionController>(FindObjectsInactive.Include)
            .FirstOrDefault(companion => companion != null && companion.CompanionId == companionId);
    }

    int GetCurrentDay() {
        return TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    public object CaptureState() {
        return new PlayerCompanionExpeditionLogSaveData {
            activeExpeditions = activeExpeditions.Where(state => state != null).Select(state => state.ToSaveData()).ToList(),
            expeditionHistory = expeditionHistory.Where(history => history != null).Select(history => history.ToSaveData()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerCompanionExpeditionLogSaveData;
        activeExpeditions = saveData?.activeExpeditions?.Where(entry => entry != null).Select(entry => new PlayerCompanionExpeditionState(entry)).ToList()
            ?? new List<PlayerCompanionExpeditionState>();
        expeditionHistory = saveData?.expeditionHistory?.Where(entry => entry != null).Select(entry => new PlayerCompanionExpeditionHistory(entry)).ToList()
            ?? new List<PlayerCompanionExpeditionHistory>();
        OnCompanionExpeditionsChanged?.Invoke();
    }
}

[Serializable]
public class PlayerCompanionExpeditionState {
    [Tooltip("Saved expedition definition id.")]
    public string expeditionId;
    [Tooltip("Saved expedition display name.")]
    public string expeditionName;
    [Tooltip("Saved expedition category.")]
    public CompanionExpeditionCategory category;
    [Tooltip("Board/source id where this expedition started.")]
    public string sourceId;
    [Tooltip("Saved companion id.")]
    public string companionId;
    [Tooltip("Saved companion display name.")]
    public string companionName;
    [Tooltip("Saved companion role id.")]
    public string roleId;
    [Tooltip("In-game day when this expedition started.")]
    public int startedDay;
    [Tooltip("Absolute in-game hour when this expedition started.")]
    public int startedAbsoluteHour;
    [Tooltip("Absolute in-game hour when this expedition can be claimed.")]
    public int readyAbsoluteHour;
    [Tooltip("Captured success chance used when this expedition is claimed.")]
    [Range(0f, 1f)]
    public float successChance;

    public PlayerCompanionExpeditionState() {
    }

    public PlayerCompanionExpeditionState(PlayerCompanionExpeditionStateSaveData saveData) {
        if(saveData == null) {
            return;
        }

        expeditionId = saveData.expeditionId;
        expeditionName = saveData.expeditionName;
        category = saveData.category;
        sourceId = saveData.sourceId;
        companionId = saveData.companionId;
        companionName = saveData.companionName;
        roleId = saveData.roleId;
        startedDay = saveData.startedDay;
        startedAbsoluteHour = saveData.startedAbsoluteHour;
        readyAbsoluteHour = saveData.readyAbsoluteHour;
        successChance = Mathf.Clamp01(saveData.successChance);
    }

    public bool IsReady() {
        int currentHour = TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
        return currentHour >= readyAbsoluteHour;
    }

    public PlayerCompanionExpeditionStateSaveData ToSaveData() {
        return new PlayerCompanionExpeditionStateSaveData {
            expeditionId = expeditionId,
            expeditionName = expeditionName,
            category = category,
            sourceId = sourceId,
            companionId = companionId,
            companionName = companionName,
            roleId = roleId,
            startedDay = startedDay,
            startedAbsoluteHour = startedAbsoluteHour,
            readyAbsoluteHour = readyAbsoluteHour,
            successChance = successChance
        };
    }
}

[Serializable]
public class PlayerCompanionExpeditionHistory {
    [Tooltip("Saved expedition definition id.")]
    public string expeditionId;
    [Tooltip("Saved expedition display name.")]
    public string expeditionName;
    [Tooltip("Board/source id where this expedition started.")]
    public string sourceId;
    [Tooltip("Saved companion id.")]
    public string companionId;
    [Tooltip("Saved companion display name.")]
    public string companionName;
    [Tooltip("In-game day when this expedition started.")]
    public int startedDay;
    [Tooltip("Absolute in-game hour when this expedition started.")]
    public int startedAbsoluteHour;
    [Tooltip("In-game day when this expedition was claimed.")]
    public int claimedDay;
    [Tooltip("Absolute in-game hour when this expedition was claimed.")]
    public int claimedAbsoluteHour;
    [Tooltip("If enabled, the expedition succeeded.")]
    public bool success;
    [Tooltip("Captured success chance used for this result.")]
    [Range(0f, 1f)]
    public float successChance;

    public PlayerCompanionExpeditionHistory() {
    }

    public PlayerCompanionExpeditionHistory(PlayerCompanionExpeditionHistorySaveData saveData) {
        if(saveData == null) {
            return;
        }

        expeditionId = saveData.expeditionId;
        expeditionName = saveData.expeditionName;
        sourceId = saveData.sourceId;
        companionId = saveData.companionId;
        companionName = saveData.companionName;
        startedDay = saveData.startedDay;
        startedAbsoluteHour = saveData.startedAbsoluteHour;
        claimedDay = saveData.claimedDay;
        claimedAbsoluteHour = saveData.claimedAbsoluteHour;
        success = saveData.success;
        successChance = Mathf.Clamp01(saveData.successChance);
    }

    public static PlayerCompanionExpeditionHistory FromState(PlayerCompanionExpeditionState state, int claimedDay, int claimedAbsoluteHour, bool success) {
        return new PlayerCompanionExpeditionHistory {
            expeditionId = state.expeditionId,
            expeditionName = state.expeditionName,
            sourceId = state.sourceId,
            companionId = state.companionId,
            companionName = state.companionName,
            startedDay = state.startedDay,
            startedAbsoluteHour = state.startedAbsoluteHour,
            claimedDay = claimedDay,
            claimedAbsoluteHour = claimedAbsoluteHour,
            success = success,
            successChance = state.successChance
        };
    }

    public PlayerCompanionExpeditionHistorySaveData ToSaveData() {
        return new PlayerCompanionExpeditionHistorySaveData {
            expeditionId = expeditionId,
            expeditionName = expeditionName,
            sourceId = sourceId,
            companionId = companionId,
            companionName = companionName,
            startedDay = startedDay,
            startedAbsoluteHour = startedAbsoluteHour,
            claimedDay = claimedDay,
            claimedAbsoluteHour = claimedAbsoluteHour,
            success = success,
            successChance = successChance
        };
    }
}

[Serializable]
public class PlayerCompanionExpeditionLogSaveData {
    public List<PlayerCompanionExpeditionStateSaveData> activeExpeditions;
    public List<PlayerCompanionExpeditionHistorySaveData> expeditionHistory;
}

[Serializable]
public class PlayerCompanionExpeditionStateSaveData {
    public string expeditionId;
    public string expeditionName;
    public CompanionExpeditionCategory category;
    public string sourceId;
    public string companionId;
    public string companionName;
    public string roleId;
    public int startedDay;
    public int startedAbsoluteHour;
    public int readyAbsoluteHour;
    public float successChance;
}

[Serializable]
public class PlayerCompanionExpeditionHistorySaveData {
    public string expeditionId;
    public string expeditionName;
    public string sourceId;
    public string companionId;
    public string companionName;
    public int startedDay;
    public int startedAbsoluteHour;
    public int claimedDay;
    public int claimedAbsoluteHour;
    public bool success;
    public float successChance;
}
