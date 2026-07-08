using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum CampStationUIActionResultKind {
    None,
    Refreshed,
    Viewed,
    ActionRan,
    FirstAvailableRan,
    Blocked
}

public class CampStationUIManager : MonoBehaviour {
    [Header("Player")]
    [Tooltip("Player whose camp station state is shown. Empty uses PlayerController.i or the first PlayerController in the scene.")]
    [SerializeField] PlayerController playerOverride = null;
    [Tooltip("If enabled, missing PlayerCampStationLog is created when UI actions need it.")]
    [SerializeField] bool createMissingLogForActions = true;

    [Header("Station")]
    [Tooltip("Optional overworld camp station source used as the primary station/action context.")]
    [SerializeField] CampStationSource source = null;
    [Tooltip("Camp station shown when Source is empty or Source has no station.")]
    [SerializeField] CampStationDefinition station = null;
    [Tooltip("Optional region context passed into station actions. Empty uses Source region context.")]
    [SerializeField] RegionInfoDefinition regionContext = null;
    [Tooltip("Optional activity zone context passed into station actions. Empty uses Source zone context or PlayerActivityContext.CurrentZone.")]
    [SerializeField] ActivityZoneDefinition zoneContext = null;
    [Tooltip("Source id used when no CampStationSource is assigned.")]
    [SerializeField] string uiSourceId = "ui:camp-station";
    [Tooltip("Source name used when no CampStationSource is assigned.")]
    [SerializeField] string uiSourceName = "Camp Station";

    [Header("Snapshot")]
    [Tooltip("If enabled, Refresh is called when this component starts.")]
    [SerializeField] bool refreshOnStart = true;
    [Tooltip("If enabled, Refresh is called after every successful or blocked action.")]
    [SerializeField] bool refreshAfterActions = true;
    [Tooltip("If enabled, locked action rows are included with a failure reason.")]
    [SerializeField] bool includeLockedActions = true;
    [Tooltip("Maximum action rows copied into the snapshot. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxActionRows = 30;
    [Tooltip("Maximum history rows copied into the snapshot. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxHistoryRows = 30;

    [Header("Debug")]
    [Tooltip("If enabled, successful UI backend actions are written to GameDebug.")]
    [SerializeField] bool logSuccessfulActions;
    [Tooltip("If enabled, blocked UI backend actions are written to GameDebug.")]
    [SerializeField] bool logBlockedActions = true;

    CampStationUIScreenSnapshot currentSnapshot = new CampStationUIScreenSnapshot();
    CampStationUIActionResult lastResult = new CampStationUIActionResult();

    public CampStationUIScreenSnapshot CurrentSnapshot => currentSnapshot;
    public CampStationUIActionResult LastResult => lastResult;
    public CampStationSource Source => source;
    public CampStationDefinition Station => station;
    public RegionInfoDefinition RegionContext => regionContext;
    public ActivityZoneDefinition ZoneContext => zoneContext;
    public event Action<CampStationUIScreenSnapshot> OnSnapshotChanged;
    public event Action<CampStationUIActionResult> OnActionResult;

    void Start() {
        if(refreshOnStart) {
            Refresh();
        }
    }

    [ContextMenu("Refresh Camp Station Snapshot")]
    public CampStationUIScreenSnapshot RefreshFromContextMenu() {
        return Refresh();
    }

    public CampStationUIScreenSnapshot Refresh() {
        var player = ResolvePlayer();
        var log = player != null ? player.GetComponent<PlayerCampStationLog>() : null;
        var region = ResolveRegion();
        var zone = ResolveZone();
        var stationSnapshot = BuildStationSnapshot(player, region, zone);
        var historyRows = BuildHistoryRows(log).ToList();

        currentSnapshot = new CampStationUIScreenSnapshot {
            hasPlayer = player != null,
            playerName = player != null ? player.name : string.Empty,
            sourceId = ResolveSourceId(),
            sourceName = ResolveSourceName(),
            regionId = region != null ? region.Id : string.Empty,
            regionName = region != null ? region.DisplayName : string.Empty,
            zoneId = zone != null ? zone.Id : string.Empty,
            zoneName = zone != null ? zone.DisplayName : string.Empty,
            day = TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1,
            hour = TimeSystem.i != null ? Mathf.Clamp(TimeSystem.i.Hour, 0, 23) : 0,
            absoluteHour = GetCurrentAbsoluteHour(),
            station = stationSnapshot,
            actionCount = stationSnapshot != null && stationSnapshot.rows != null ? stationSnapshot.rows.Count : 0,
            availableActionCount = stationSnapshot != null && stationSnapshot.rows != null ? stationSnapshot.rows.Count(row => row != null && row.canRun) : 0,
            lockedActionCount = stationSnapshot != null && stationSnapshot.rows != null ? stationSnapshot.rows.Count(row => row != null && !row.canRun) : 0,
            historyCount = historyRows.Count,
            history = historyRows,
            lastResult = lastResult
        };

        OnSnapshotChanged?.Invoke(currentSnapshot);
        return currentSnapshot;
    }

    public bool TryRecordView(out string feedback) {
        var player = ResolvePlayer();
        var resolvedStation = ResolveStation();
        if(player == null) {
            return Block("A player is required to record camp station views.", out feedback);
        }

        if(resolvedStation == null) {
            return Block("No camp station is assigned.", out feedback);
        }

        var log = GetLog(player, createMissingLogForActions);
        log?.RecordView(resolvedStation, ResolveSourceId(), ResolveSourceName(), ResolveRegion(), ResolveZone());
        return Succeed(CampStationUIActionResultKind.Viewed, $"{resolvedStation.DisplayName} viewed.", out feedback);
    }

    public bool TryRunAction(string actionId, out string feedback) {
        var player = ResolvePlayer();
        if(player == null) {
            return Block("A player is required to run camp station actions.", out feedback);
        }

        if(string.IsNullOrWhiteSpace(actionId)) {
            return Block("No camp station action id was provided.", out feedback);
        }

        CampStationRunResult result = null;
        bool success;
        if(source != null && source.Station != null) {
            success = source.TryRunAction(actionId, player, out result);
        } else {
            var resolvedStation = ResolveStation();
            if(resolvedStation == null) {
                return Block("No camp station is assigned.", out feedback);
            }

            success = resolvedStation.TryRunAction(player, actionId, ResolveSourceId(), ResolveSourceName(), ResolveRegion(), ResolveZone(), this, out result);
            RecordRun(player, result);
        }

        feedback = result != null ? result.message : success ? "Camp station action completed." : "Camp station action failed.";
        return success
            ? Succeed(CampStationUIActionResultKind.ActionRan, feedback, out feedback)
            : Block(feedback, out feedback);
    }

    public bool TryRunFirstAvailable(out string feedback) {
        var player = ResolvePlayer();
        if(player == null) {
            return Block("A player is required to run camp station actions.", out feedback);
        }

        CampStationRunResult result = null;
        bool success;
        if(source != null && source.Station != null) {
            success = source.TryRunFirstAvailable(player, out result);
        } else {
            var resolvedStation = ResolveStation();
            if(resolvedStation == null) {
                return Block("No camp station is assigned.", out feedback);
            }

            success = resolvedStation.TryRunFirstAvailable(player, ResolveSourceId(), ResolveSourceName(), ResolveRegion(), ResolveZone(), this, out result);
            RecordRun(player, result);
        }

        feedback = result != null ? result.message : success ? "Camp station action completed." : "No available camp station action found.";
        return success
            ? Succeed(CampStationUIActionResultKind.FirstAvailableRan, feedback, out feedback)
            : Block(feedback, out feedback);
    }

    public CampStationActionRow FindActionRow(string actionId) {
        return currentSnapshot?.station?.rows?
            .FirstOrDefault(row => row != null && string.Equals(row.actionId, actionId, StringComparison.OrdinalIgnoreCase));
    }

    CampStationSnapshot BuildStationSnapshot(PlayerController player, RegionInfoDefinition region, ActivityZoneDefinition zone) {
        CampStationSnapshot snapshot = null;
        if(source != null && source.Station != null) {
            snapshot = source.GetSnapshot(player);
        } else {
            var resolvedStation = ResolveStation();
            if(resolvedStation != null) {
                snapshot = resolvedStation.BuildSnapshot(player, ResolveSourceId(), ResolveSourceName(), region, zone, includeLockedActions, this);
            }
        }

        if(snapshot?.rows != null && maxActionRows > 0 && snapshot.rows.Count > maxActionRows) {
            snapshot.rows = snapshot.rows.Take(maxActionRows).ToList();
        }

        return snapshot;
    }

    IEnumerable<CampStationHistoryRow> BuildHistoryRows(PlayerCampStationLog log) {
        var rows = log != null
            ? log.History
                .Where(record => record != null)
                .OrderByDescending(record => record.absoluteHour)
                .ThenByDescending(record => record.day)
                .Select(CampStationHistoryRow.FromHistory)
            : Enumerable.Empty<CampStationHistoryRow>();

        return maxHistoryRows > 0 ? rows.Take(maxHistoryRows) : rows;
    }

    void RecordRun(PlayerController player, CampStationRunResult result) {
        if(player == null || result == null) {
            return;
        }

        var log = GetLog(player, createMissingLogForActions);
        log?.RecordRun(result, ResolveRegion(), ResolveZone());
    }

    PlayerCampStationLog GetLog(PlayerController player, bool createIfMissing) {
        if(player == null) {
            return null;
        }

        var log = player.GetComponent<PlayerCampStationLog>();
        return log != null || !createIfMissing ? log : player.gameObject.AddComponent<PlayerCampStationLog>();
    }

    CampStationDefinition ResolveStation() {
        return station != null ? station : source != null ? source.Station : null;
    }

    PlayerController ResolvePlayer() {
        if(playerOverride != null) {
            return playerOverride;
        }

        if(PlayerController.i != null) {
            return PlayerController.i;
        }

        return FindAnyObjectByType<PlayerController>();
    }

    RegionInfoDefinition ResolveRegion() {
        if(regionContext != null) {
            return regionContext;
        }

        return source != null ? source.RegionContext : null;
    }

    ActivityZoneDefinition ResolveZone() {
        if(zoneContext != null) {
            return zoneContext;
        }

        return source != null && source.ZoneContext != null ? source.ZoneContext : PlayerActivityContext.CurrentZone;
    }

    string ResolveSourceId() {
        if(source != null) {
            return source.SourceId;
        }

        if(!string.IsNullOrWhiteSpace(uiSourceId)) {
            return uiSourceId;
        }

        var resolvedStation = ResolveStation();
        return resolvedStation != null ? resolvedStation.ResolveStationSourceId(null) : "ui:camp-station";
    }

    string ResolveSourceName() {
        if(source != null) {
            return source.DisplayName;
        }

        if(!string.IsNullOrWhiteSpace(uiSourceName)) {
            return uiSourceName;
        }

        var resolvedStation = ResolveStation();
        return resolvedStation != null ? resolvedStation.DisplayName : "Camp Station";
    }

    bool Succeed(CampStationUIActionResultKind kind, string message, out string feedback) {
        feedback = message;
        SetLastResult(kind, true, message);
        if(logSuccessfulActions) {
            GameDebug.Success(message, GameDebugCategory.Activity, this, "CampStationUIManager");
        }
        return true;
    }

    bool Block(string message, out string feedback) {
        feedback = string.IsNullOrWhiteSpace(message) ? "Camp station action was blocked." : message;
        SetLastResult(CampStationUIActionResultKind.Blocked, false, feedback);
        if(logBlockedActions) {
            GameDebug.Warning(feedback, GameDebugCategory.Activity, this, "CampStationUIManager");
        }
        return false;
    }

    void SetLastResult(CampStationUIActionResultKind kind, bool success, string message) {
        lastResult = new CampStationUIActionResult {
            kind = kind,
            success = success,
            message = message,
            day = TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1,
            hour = TimeSystem.i != null ? Mathf.Clamp(TimeSystem.i.Hour, 0, 23) : 0,
            absoluteHour = GetCurrentAbsoluteHour()
        };

        OnActionResult?.Invoke(lastResult);
        if(refreshAfterActions) {
            Refresh();
        }
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }
}

[Serializable]
public class CampStationUIScreenSnapshot {
    [Tooltip("If enabled, a player was resolved for this snapshot.")]
    public bool hasPlayer;
    [Tooltip("Resolved player object name.")]
    public string playerName;
    [Tooltip("Resolved source id used by UI actions.")]
    public string sourceId;
    [Tooltip("Resolved source name used by UI actions.")]
    public string sourceName;
    [Tooltip("Resolved region id.")]
    public string regionId;
    [Tooltip("Resolved region display name.")]
    public string regionName;
    [Tooltip("Resolved activity zone id.")]
    public string zoneId;
    [Tooltip("Resolved activity zone display name.")]
    public string zoneName;
    [Tooltip("Current in-game day.")]
    public int day;
    [Tooltip("Current in-game hour.")]
    public int hour;
    [Tooltip("Current absolute in-game hour.")]
    public int absoluteHour;
    [Tooltip("Camp station snapshot used by station action UI panels.")]
    public CampStationSnapshot station;
    [Tooltip("Visible action row count.")]
    public int actionCount;
    [Tooltip("Action rows that can run right now.")]
    public int availableActionCount;
    [Tooltip("Action rows that are visible but locked.")]
    public int lockedActionCount;
    [Tooltip("History row count.")]
    public int historyCount;
    [Tooltip("Recent station history rows.")]
    public List<CampStationHistoryRow> history = new List<CampStationHistoryRow>();
    [Tooltip("Most recent UI backend action result.")]
    public CampStationUIActionResult lastResult;
}

[Serializable]
public class CampStationUIActionResult {
    [Tooltip("Kind of UI backend action that produced this result.")]
    public CampStationUIActionResultKind kind;
    [Tooltip("If enabled, the action succeeded.")]
    public bool success;
    [Tooltip("Readable result, failure or feedback text.")]
    public string message;
    [Tooltip("In-game day when the result was produced.")]
    public int day;
    [Tooltip("In-game hour when the result was produced.")]
    public int hour;
    [Tooltip("Absolute in-game hour when the result was produced.")]
    public int absoluteHour;
}

[Serializable]
public class CampStationHistoryRow {
    [Tooltip("Whether this record is a view, successful action or blocked attempt.")]
    public CampStationLogOperation operation;
    [Tooltip("Camp station definition id.")]
    public string stationId;
    [Tooltip("Camp station display name.")]
    public string stationName;
    [Tooltip("Action id selected by the player or source.")]
    public string actionId;
    [Tooltip("Action display name.")]
    public string actionName;
    [Tooltip("Action type saved for filters and future UI.")]
    public CampStationActionType actionType;
    [Tooltip("Source id used by the station action.")]
    public string sourceId;
    [Tooltip("Source display name used by the station action.")]
    public string sourceName;
    [Tooltip("Region id active when this record was written.")]
    public string regionId;
    [Tooltip("Region display name active when this record was written.")]
    public string regionName;
    [Tooltip("Activity zone id active when this record was written.")]
    public string zoneId;
    [Tooltip("Activity zone display name active when this record was written.")]
    public string zoneName;
    [Tooltip("If enabled, the operation succeeded.")]
    public bool success;
    [Tooltip("Result or failure message.")]
    public string message;
    [Tooltip("In-game day when this record was written.")]
    public int day;
    [Tooltip("In-game hour when this record was written.")]
    public int hour;
    [Tooltip("Absolute in-game hour when this record was written.")]
    public int absoluteHour;
    [Tooltip("Short text useful for placeholder UI rows.")]
    public string displayText;

    public static CampStationHistoryRow FromHistory(PlayerCampStationHistory history) {
        return new CampStationHistoryRow {
            operation = history != null ? history.operation : CampStationLogOperation.Blocked,
            stationId = history != null ? history.stationId : string.Empty,
            stationName = history != null ? history.stationName : string.Empty,
            actionId = history != null ? history.actionId : string.Empty,
            actionName = history != null ? history.actionName : string.Empty,
            actionType = history != null ? history.actionType : CampStationActionType.Activity,
            sourceId = history != null ? history.sourceId : string.Empty,
            sourceName = history != null ? history.sourceName : string.Empty,
            regionId = history != null ? history.regionId : string.Empty,
            regionName = history != null ? history.regionName : string.Empty,
            zoneId = history != null ? history.zoneId : string.Empty,
            zoneName = history != null ? history.zoneName : string.Empty,
            success = history != null && history.success,
            message = history != null ? history.message : string.Empty,
            day = history != null ? history.day : 0,
            hour = history != null ? history.hour : 0,
            absoluteHour = history != null ? history.absoluteHour : 0,
            displayText = history != null
                ? $"{history.stationName}: {(string.IsNullOrWhiteSpace(history.actionName) ? history.operation.ToString() : history.actionName)}"
                : string.Empty
        };
    }
}
