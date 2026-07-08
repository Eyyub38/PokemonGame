using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerCompanionExpeditionRouteLog : MonoBehaviour, ISavable {
    [Tooltip("Runtime/save list of active companion expedition routes.")]
    [SerializeField] List<PlayerCompanionExpeditionRouteState> activeRoutes = new List<PlayerCompanionExpeditionRouteState>();
    [Tooltip("Runtime/save history of completed or failed companion expedition routes.")]
    [SerializeField] List<PlayerCompanionExpeditionRouteHistory> routeHistory = new List<PlayerCompanionExpeditionRouteHistory>();

    public IReadOnlyList<PlayerCompanionExpeditionRouteState> ActiveRoutes => activeRoutes;
    public IReadOnlyList<PlayerCompanionExpeditionRouteHistory> RouteHistory => routeHistory;
    public event Action OnCompanionRoutesChanged;

    public bool HasActiveRoute(CompanionExpeditionRouteDefinition route, string sourceId = null) {
        return route != null && activeRoutes.Any(state => state != null
            && state.routeId == route.Id
            && (string.IsNullOrWhiteSpace(sourceId) || state.sourceId == sourceId));
    }

    public bool HasActiveRouteForCompanion(string companionId) {
        return !string.IsNullOrWhiteSpace(companionId)
            && activeRoutes.Any(state => state != null && state.companionId == companionId);
    }

    public PlayerCompanionExpeditionRouteState GetActiveRoute(CompanionExpeditionRouteDefinition route, string sourceId = null) {
        return route != null
            ? activeRoutes.FirstOrDefault(state => state != null
                && state.routeId == route.Id
                && (string.IsNullOrWhiteSpace(sourceId) || state.sourceId == sourceId))
            : null;
    }

    public int GetCompletedCount(CompanionExpeditionRouteDefinition route, string sourceId = null, bool? success = null) {
        if(route == null) {
            return 0;
        }

        return routeHistory.Count(history => history != null
            && history.routeId == route.Id
            && (string.IsNullOrWhiteSpace(sourceId) || history.sourceId == sourceId)
            && (!success.HasValue || history.success == success.Value));
    }

    public bool CanStart(CompanionExpeditionRouteDefinition route, string sourceId, CompanionExpeditionRepeatMode repeatMode, int cooldownHours, out string failureMessage) {
        if(route == null) {
            failureMessage = "No companion route selected.";
            return false;
        }

        var history = GetLatestHistory(route.Id, sourceId);
        if(repeatMode == CompanionExpeditionRepeatMode.Once && history != null) {
            failureMessage = $"{route.DisplayName} has already been completed.";
            return false;
        }

        if(repeatMode == CompanionExpeditionRepeatMode.Daily && history != null && history.completedDay == GetCurrentDay()) {
            failureMessage = $"{route.DisplayName} can only be completed once per day.";
            return false;
        }

        if(repeatMode == CompanionExpeditionRepeatMode.CooldownHours && history != null && history.completedAbsoluteHour >= 0) {
            int elapsed = GetCurrentAbsoluteHour() - history.completedAbsoluteHour;
            if(elapsed < Mathf.Max(0, cooldownHours)) {
                failureMessage = $"{route.DisplayName} will be available again in {cooldownHours - elapsed} hour(s).";
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    public bool TryStart(PlayerController player, CompanionExpeditionRouteDefinition route, CompanionController companion, string sourceId, out string failureMessage) {
        if(route == null) {
            failureMessage = "No companion route selected.";
            return false;
        }

        if(!route.CanStart(player, companion, this, sourceId, out failureMessage)) {
            return false;
        }

        var state = new PlayerCompanionExpeditionRouteState {
            routeId = route.Id,
            routeName = route.DisplayName,
            sourceId = sourceId,
            companionId = companion.CompanionId,
            companionName = companion.CompanionName,
            currentStageIndex = 0,
            startedDay = GetCurrentDay(),
            startedAbsoluteHour = GetCurrentAbsoluteHour()
        };

        activeRoutes.Add(state);
        route.PublishStarted(player, companion, sourceId);
        if(!TryStartCurrentStage(player, route, state, out failureMessage)) {
            activeRoutes.Remove(state);
            return false;
        }

        OnCompanionRoutesChanged?.Invoke();
        failureMessage = null;
        return true;
    }

    public bool TryStartCurrentStage(PlayerController player, CompanionExpeditionRouteDefinition route, PlayerCompanionExpeditionRouteState state, out string failureMessage) {
        if(route == null || state == null) {
            failureMessage = "No companion route stage selected.";
            return false;
        }

        if(state.stageInProgress) {
            failureMessage = $"{state.routeName} already has an active stage.";
            return false;
        }

        var stage = route.GetStage(state.currentStageIndex);
        if(stage == null) {
            failureMessage = $"{state.routeName} has no stage at index {state.currentStageIndex}.";
            return false;
        }

        if(!stage.CanStart(player, out failureMessage)) {
            return false;
        }

        var companion = ResolveCompanion(state.companionId);
        if(companion == null) {
            failureMessage = $"{state.companionName} could not be found.";
            return false;
        }

        var expeditionLog = GetOrCreateExpeditionLog(player);
        string stageSourceId = route.GetStageSourceId(state.sourceId, state.currentStageIndex);
        if(expeditionLog == null || !expeditionLog.TryStart(player, stage.Expedition, companion, stageSourceId, out failureMessage)) {
            return false;
        }

        state.stageInProgress = true;
        state.currentStageSourceId = stageSourceId;
        state.currentStageName = stage.DisplayName;
        state.lastStageStartedAbsoluteHour = GetCurrentAbsoluteHour();
        route.PublishStageStarted(player, companion, state.sourceId, stage);
        OnCompanionRoutesChanged?.Invoke();
        failureMessage = null;
        return true;
    }

    public bool TryClaimCurrentStage(PlayerController player, CompanionExpeditionRouteDefinition route, PlayerCompanionExpeditionRouteState state, out string failureMessage) {
        if(route == null || state == null) {
            failureMessage = "No companion route selected.";
            return false;
        }

        if(!state.stageInProgress) {
            failureMessage = $"{state.routeName} has no active stage.";
            return false;
        }

        var stage = route.GetStage(state.currentStageIndex);
        if(stage == null || stage.Expedition == null) {
            failureMessage = $"{state.routeName} has an invalid current stage.";
            return false;
        }

        var expeditionLog = GetOrCreateExpeditionLog(player);
        if(expeditionLog == null) {
            failureMessage = "Companion expedition log is missing.";
            return false;
        }

        var expeditionState = expeditionLog.GetReadyExpeditions(stage.Expedition, state.currentStageSourceId).FirstOrDefault();
        if(expeditionState == null) {
            failureMessage = $"{stage.DisplayName} is not ready yet.";
            return false;
        }

        if(!expeditionLog.TryClaim(player, stage.Expedition, expeditionState, out var history, out failureMessage)) {
            return false;
        }

        var companion = ResolveCompanion(state.companionId);
        state.stageInProgress = false;
        state.currentStageSourceId = null;
        state.currentStageName = null;
        route.PublishStageClaimed(player, companion, state.sourceId, stage, history.success);

        if(history.success) {
            state.completedStages++;
            state.currentStageIndex++;
        } else {
            state.failedStages++;
            var failureMode = route.GetFailureMode(stage);
            if(failureMode == CompanionExpeditionRouteFailureMode.StopRoute) {
                FinishRoute(player, route, state, success: false, stage);
                failureMessage = null;
                return true;
            }

            if(failureMode == CompanionExpeditionRouteFailureMode.ContinueToNextStage) {
                state.currentStageIndex++;
            }
        }

        if(state.currentStageIndex >= route.Stages.Count) {
            FinishRoute(player, route, state, success: true, null);
            failureMessage = null;
            return true;
        }

        if(route.AutoStartNextStage) {
            TryStartCurrentStage(player, route, state, out _);
        }

        OnCompanionRoutesChanged?.Invoke();
        failureMessage = null;
        return true;
    }

    public bool TryAdvanceOrClaim(PlayerController player, CompanionExpeditionRouteDefinition route, string sourceId, out string failureMessage) {
        var state = GetActiveRoute(route, sourceId);
        if(state == null) {
            failureMessage = $"{route?.DisplayName ?? "Route"} is not active.";
            return false;
        }

        return state.stageInProgress
            ? TryClaimCurrentStage(player, route, state, out failureMessage)
            : TryStartCurrentStage(player, route, state, out failureMessage);
    }

    void FinishRoute(PlayerController player, CompanionExpeditionRouteDefinition route, PlayerCompanionExpeditionRouteState state, bool success, CompanionExpeditionRouteStage failedStage) {
        var companion = ResolveCompanion(state.companionId);
        activeRoutes.Remove(state);
        routeHistory.Add(PlayerCompanionExpeditionRouteHistory.FromState(state, GetCurrentDay(), GetCurrentAbsoluteHour(), success));

        if(success) {
            route.ApplyRouteCompleted(player);
            route.PublishCompleted(player, companion, state.sourceId);
        } else {
            route.ApplyRouteFailed(player);
            route.PublishFailed(player, companion, state.sourceId, failedStage);
        }

        OnCompanionRoutesChanged?.Invoke();
    }

    PlayerCompanionExpeditionRouteHistory GetLatestHistory(string routeId, string sourceId) {
        if(string.IsNullOrWhiteSpace(routeId)) {
            return null;
        }

        return routeHistory
            .Where(history => history != null
                && history.routeId == routeId
                && (string.IsNullOrWhiteSpace(sourceId) || history.sourceId == sourceId))
            .OrderByDescending(history => history.completedAbsoluteHour)
            .FirstOrDefault();
    }

    PlayerCompanionExpeditionLog GetOrCreateExpeditionLog(PlayerController player) {
        if(player == null) {
            return null;
        }

        var log = player.GetComponent<PlayerCompanionExpeditionLog>();
        return log != null ? log : player.gameObject.AddComponent<PlayerCompanionExpeditionLog>();
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
        return new PlayerCompanionExpeditionRouteLogSaveData {
            activeRoutes = activeRoutes.Where(state => state != null).Select(state => state.ToSaveData()).ToList(),
            routeHistory = routeHistory.Where(history => history != null).Select(history => history.ToSaveData()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerCompanionExpeditionRouteLogSaveData;
        activeRoutes = saveData?.activeRoutes?.Where(entry => entry != null).Select(entry => new PlayerCompanionExpeditionRouteState(entry)).ToList()
            ?? new List<PlayerCompanionExpeditionRouteState>();
        routeHistory = saveData?.routeHistory?.Where(entry => entry != null).Select(entry => new PlayerCompanionExpeditionRouteHistory(entry)).ToList()
            ?? new List<PlayerCompanionExpeditionRouteHistory>();
        OnCompanionRoutesChanged?.Invoke();
    }
}

[Serializable]
public class PlayerCompanionExpeditionRouteState {
    [Tooltip("Saved route definition id.")]
    public string routeId;
    [Tooltip("Saved route display name.")]
    public string routeName;
    [Tooltip("Board/source id where this route started.")]
    public string sourceId;
    [Tooltip("Saved companion id.")]
    public string companionId;
    [Tooltip("Saved companion display name.")]
    public string companionName;
    [Tooltip("Current route stage index.")]
    [Min(0)]
    public int currentStageIndex;
    [Tooltip("Number of route stages completed successfully.")]
    [Min(0)]
    public int completedStages;
    [Tooltip("Number of route stages failed.")]
    [Min(0)]
    public int failedStages;
    [Tooltip("If enabled, the current stage has an active underlying expedition.")]
    public bool stageInProgress;
    [Tooltip("Source id used by the currently active underlying expedition.")]
    public string currentStageSourceId;
    [Tooltip("Display name of the current active stage.")]
    public string currentStageName;
    [Tooltip("In-game day when this route started.")]
    public int startedDay;
    [Tooltip("Absolute in-game hour when this route started.")]
    public int startedAbsoluteHour;
    [Tooltip("Absolute in-game hour when the current/last stage started.")]
    public int lastStageStartedAbsoluteHour = -1;

    public PlayerCompanionExpeditionRouteState() {
    }

    public PlayerCompanionExpeditionRouteState(PlayerCompanionExpeditionRouteStateSaveData saveData) {
        if(saveData == null) {
            return;
        }

        routeId = saveData.routeId;
        routeName = saveData.routeName;
        sourceId = saveData.sourceId;
        companionId = saveData.companionId;
        companionName = saveData.companionName;
        currentStageIndex = Mathf.Max(0, saveData.currentStageIndex);
        completedStages = Mathf.Max(0, saveData.completedStages);
        failedStages = Mathf.Max(0, saveData.failedStages);
        stageInProgress = saveData.stageInProgress;
        currentStageSourceId = saveData.currentStageSourceId;
        currentStageName = saveData.currentStageName;
        startedDay = saveData.startedDay;
        startedAbsoluteHour = saveData.startedAbsoluteHour;
        lastStageStartedAbsoluteHour = saveData.lastStageStartedAbsoluteHour;
    }

    public PlayerCompanionExpeditionRouteStateSaveData ToSaveData() {
        return new PlayerCompanionExpeditionRouteStateSaveData {
            routeId = routeId,
            routeName = routeName,
            sourceId = sourceId,
            companionId = companionId,
            companionName = companionName,
            currentStageIndex = currentStageIndex,
            completedStages = completedStages,
            failedStages = failedStages,
            stageInProgress = stageInProgress,
            currentStageSourceId = currentStageSourceId,
            currentStageName = currentStageName,
            startedDay = startedDay,
            startedAbsoluteHour = startedAbsoluteHour,
            lastStageStartedAbsoluteHour = lastStageStartedAbsoluteHour
        };
    }
}

[Serializable]
public class PlayerCompanionExpeditionRouteHistory {
    [Tooltip("Saved route definition id.")]
    public string routeId;
    [Tooltip("Saved route display name.")]
    public string routeName;
    [Tooltip("Board/source id where this route started.")]
    public string sourceId;
    [Tooltip("Saved companion id.")]
    public string companionId;
    [Tooltip("Saved companion display name.")]
    public string companionName;
    [Tooltip("Number of route stages completed successfully.")]
    [Min(0)]
    public int completedStages;
    [Tooltip("Number of route stages failed.")]
    [Min(0)]
    public int failedStages;
    [Tooltip("In-game day when this route started.")]
    public int startedDay;
    [Tooltip("Absolute in-game hour when this route started.")]
    public int startedAbsoluteHour;
    [Tooltip("In-game day when this route completed or failed.")]
    public int completedDay;
    [Tooltip("Absolute in-game hour when this route completed or failed.")]
    public int completedAbsoluteHour;
    [Tooltip("If enabled, the route completed successfully.")]
    public bool success;

    public PlayerCompanionExpeditionRouteHistory() {
    }

    public PlayerCompanionExpeditionRouteHistory(PlayerCompanionExpeditionRouteHistorySaveData saveData) {
        if(saveData == null) {
            return;
        }

        routeId = saveData.routeId;
        routeName = saveData.routeName;
        sourceId = saveData.sourceId;
        companionId = saveData.companionId;
        companionName = saveData.companionName;
        completedStages = Mathf.Max(0, saveData.completedStages);
        failedStages = Mathf.Max(0, saveData.failedStages);
        startedDay = saveData.startedDay;
        startedAbsoluteHour = saveData.startedAbsoluteHour;
        completedDay = saveData.completedDay;
        completedAbsoluteHour = saveData.completedAbsoluteHour;
        success = saveData.success;
    }

    public static PlayerCompanionExpeditionRouteHistory FromState(PlayerCompanionExpeditionRouteState state, int completedDay, int completedAbsoluteHour, bool success) {
        return new PlayerCompanionExpeditionRouteHistory {
            routeId = state.routeId,
            routeName = state.routeName,
            sourceId = state.sourceId,
            companionId = state.companionId,
            companionName = state.companionName,
            completedStages = state.completedStages,
            failedStages = state.failedStages,
            startedDay = state.startedDay,
            startedAbsoluteHour = state.startedAbsoluteHour,
            completedDay = completedDay,
            completedAbsoluteHour = completedAbsoluteHour,
            success = success
        };
    }

    public PlayerCompanionExpeditionRouteHistorySaveData ToSaveData() {
        return new PlayerCompanionExpeditionRouteHistorySaveData {
            routeId = routeId,
            routeName = routeName,
            sourceId = sourceId,
            companionId = companionId,
            companionName = companionName,
            completedStages = completedStages,
            failedStages = failedStages,
            startedDay = startedDay,
            startedAbsoluteHour = startedAbsoluteHour,
            completedDay = completedDay,
            completedAbsoluteHour = completedAbsoluteHour,
            success = success
        };
    }
}

[Serializable]
public class PlayerCompanionExpeditionRouteLogSaveData {
    public List<PlayerCompanionExpeditionRouteStateSaveData> activeRoutes;
    public List<PlayerCompanionExpeditionRouteHistorySaveData> routeHistory;
}

[Serializable]
public class PlayerCompanionExpeditionRouteStateSaveData {
    public string routeId;
    public string routeName;
    public string sourceId;
    public string companionId;
    public string companionName;
    public int currentStageIndex;
    public int completedStages;
    public int failedStages;
    public bool stageInProgress;
    public string currentStageSourceId;
    public string currentStageName;
    public int startedDay;
    public int startedAbsoluteHour;
    public int lastStageStartedAbsoluteHour;
}

[Serializable]
public class PlayerCompanionExpeditionRouteHistorySaveData {
    public string routeId;
    public string routeName;
    public string sourceId;
    public string companionId;
    public string companionName;
    public int completedStages;
    public int failedStages;
    public int startedDay;
    public int startedAbsoluteHour;
    public int completedDay;
    public int completedAbsoluteHour;
    public bool success;
}
