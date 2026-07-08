using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum RoleActivityBoardUIActionResultKind {
    None,
    Refreshed,
    Viewed,
    EntryRan,
    FirstAvailableRan,
    Blocked
}

public class RoleActivityBoardUIManager : MonoBehaviour {
    [Header("Player")]
    [Tooltip("Player whose role board state is shown. Empty uses PlayerController.i or the first PlayerController in the scene.")]
    [SerializeField] PlayerController playerOverride = null;
    [Tooltip("If enabled, missing PlayerRoleActivityBoardLog is created when UI actions need it.")]
    [SerializeField] bool createMissingLogForActions = true;

    [Header("Board")]
    [Tooltip("Optional overworld role board source used as the primary board/action context.")]
    [SerializeField] RoleActivityBoardSource source = null;
    [Tooltip("Role activity board shown when Source is empty or Source has no board.")]
    [SerializeField] RoleActivityBoardDefinition board = null;
    [Tooltip("Optional region context passed into board actions. Empty uses Source region context.")]
    [SerializeField] RegionInfoDefinition regionContext = null;
    [Tooltip("Optional activity zone context passed into board actions. Empty uses Source zone context or PlayerActivityContext.CurrentZone.")]
    [SerializeField] ActivityZoneDefinition zoneContext = null;
    [Tooltip("Source id used when no RoleActivityBoardSource is assigned.")]
    [SerializeField] string uiSourceId = "ui:role-activity-board";
    [Tooltip("Source name used when no RoleActivityBoardSource is assigned.")]
    [SerializeField] string uiSourceName = "Role Activity Board";

    [Header("Snapshot")]
    [Tooltip("If enabled, Refresh is called when this component starts.")]
    [SerializeField] bool refreshOnStart = true;
    [Tooltip("If enabled, Refresh is called after every successful or blocked action.")]
    [SerializeField] bool refreshAfterActions = true;
    [Tooltip("If enabled, locked board rows are included with a failure reason.")]
    [SerializeField] bool includeLockedRows = true;
    [Tooltip("Maximum board rows copied into the snapshot. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxBoardRows = 30;
    [Tooltip("Maximum history rows copied into the snapshot. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxHistoryRows = 30;

    [Header("Debug")]
    [Tooltip("If enabled, successful UI backend actions are written to GameDebug.")]
    [SerializeField] bool logSuccessfulActions;
    [Tooltip("If enabled, blocked UI backend actions are written to GameDebug.")]
    [SerializeField] bool logBlockedActions = true;

    RoleActivityBoardUIScreenSnapshot currentSnapshot = new RoleActivityBoardUIScreenSnapshot();
    RoleActivityBoardUIActionResult lastResult = new RoleActivityBoardUIActionResult();

    public RoleActivityBoardUIScreenSnapshot CurrentSnapshot => currentSnapshot;
    public RoleActivityBoardUIActionResult LastResult => lastResult;
    public RoleActivityBoardSource Source => source;
    public RoleActivityBoardDefinition Board => board;
    public RegionInfoDefinition RegionContext => regionContext;
    public ActivityZoneDefinition ZoneContext => zoneContext;
    public event Action<RoleActivityBoardUIScreenSnapshot> OnSnapshotChanged;
    public event Action<RoleActivityBoardUIActionResult> OnActionResult;

    void Start() {
        if(refreshOnStart) {
            Refresh();
        }
    }

    [ContextMenu("Refresh Role Activity Board Snapshot")]
    public RoleActivityBoardUIScreenSnapshot RefreshFromContextMenu() {
        return Refresh();
    }

    public RoleActivityBoardUIScreenSnapshot Refresh() {
        var player = ResolvePlayer();
        var log = player != null ? player.GetComponent<PlayerRoleActivityBoardLog>() : null;
        var region = ResolveRegion();
        var zone = ResolveZone();
        var boardSnapshot = BuildBoardSnapshot(player, region, zone);
        var historyRows = BuildHistoryRows(log).ToList();

        currentSnapshot = new RoleActivityBoardUIScreenSnapshot {
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
            board = boardSnapshot,
            rowCount = boardSnapshot != null && boardSnapshot.rows != null ? boardSnapshot.rows.Count : 0,
            availableRowCount = boardSnapshot != null && boardSnapshot.rows != null ? boardSnapshot.rows.Count(row => row != null && row.canRun) : 0,
            lockedRowCount = boardSnapshot != null && boardSnapshot.rows != null ? boardSnapshot.rows.Count(row => row != null && !row.canRun) : 0,
            historyCount = historyRows.Count,
            history = historyRows,
            lastResult = lastResult
        };

        OnSnapshotChanged?.Invoke(currentSnapshot);
        return currentSnapshot;
    }

    public bool TryRecordView(out string feedback) {
        var player = ResolvePlayer();
        var resolvedBoard = ResolveBoard();
        if(player == null) {
            return Block("A player is required to record role board views.", out feedback);
        }

        if(resolvedBoard == null) {
            return Block("No role activity board is assigned.", out feedback);
        }

        var log = GetLog(player, createMissingLogForActions);
        log?.RecordView(resolvedBoard, ResolveSourceId(), ResolveSourceName(), ResolveRegion(), ResolveZone());
        return Succeed(RoleActivityBoardUIActionResultKind.Viewed, $"{resolvedBoard.DisplayName} viewed.", out feedback);
    }

    public bool TryRunEntry(string entryId, out string feedback) {
        var player = ResolvePlayer();
        if(player == null) {
            return Block("A player is required to run role board entries.", out feedback);
        }

        if(string.IsNullOrWhiteSpace(entryId)) {
            return Block("No role board entry id was provided.", out feedback);
        }

        RoleActivityBoardRunResult result = null;
        bool success;
        if(source != null && source.Board != null) {
            success = source.TryRunEntry(entryId, player, out result);
        } else {
            var resolvedBoard = ResolveBoard();
            if(resolvedBoard == null) {
                return Block("No role activity board is assigned.", out feedback);
            }

            success = resolvedBoard.TryRunEntry(player, entryId, ResolveSourceId(), ResolveSourceName(), ResolveRegion(), ResolveZone(), this, out result);
            RecordRun(player, result);
        }

        feedback = result != null ? result.message : success ? "Role board entry completed." : "Role board entry failed.";
        return success
            ? Succeed(RoleActivityBoardUIActionResultKind.EntryRan, feedback, out feedback)
            : Block(feedback, out feedback);
    }

    public bool TryRunFirstAvailable(out string feedback) {
        var player = ResolvePlayer();
        if(player == null) {
            return Block("A player is required to run role board entries.", out feedback);
        }

        RoleActivityBoardRunResult result = null;
        bool success;
        if(source != null && source.Board != null) {
            success = source.TryRunFirstAvailable(player, out result);
        } else {
            var resolvedBoard = ResolveBoard();
            if(resolvedBoard == null) {
                return Block("No role activity board is assigned.", out feedback);
            }

            success = resolvedBoard.TryRunFirstAvailable(player, ResolveSourceId(), ResolveSourceName(), ResolveRegion(), ResolveZone(), this, out result);
            RecordRun(player, result);
        }

        feedback = result != null ? result.message : success ? "Role board entry completed." : "No available role board entry found.";
        return success
            ? Succeed(RoleActivityBoardUIActionResultKind.FirstAvailableRan, feedback, out feedback)
            : Block(feedback, out feedback);
    }

    public RoleActivityBoardRow FindRow(string entryId) {
        return currentSnapshot?.board?.rows?
            .FirstOrDefault(row => row != null && string.Equals(row.entryId, entryId, StringComparison.OrdinalIgnoreCase));
    }

    RoleActivityBoardSnapshot BuildBoardSnapshot(PlayerController player, RegionInfoDefinition region, ActivityZoneDefinition zone) {
        RoleActivityBoardSnapshot snapshot = null;
        if(source != null && source.Board != null) {
            snapshot = source.GetSnapshot(player);
        } else {
            var resolvedBoard = ResolveBoard();
            if(resolvedBoard != null) {
                snapshot = resolvedBoard.BuildSnapshot(player, ResolveSourceId(), ResolveSourceName(), region, zone, includeLockedRows, this);
            }
        }

        if(snapshot?.rows != null && maxBoardRows > 0 && snapshot.rows.Count > maxBoardRows) {
            snapshot.rows = snapshot.rows.Take(maxBoardRows).ToList();
        }

        return snapshot;
    }

    IEnumerable<RoleActivityBoardHistoryRow> BuildHistoryRows(PlayerRoleActivityBoardLog log) {
        var rows = log != null
            ? log.History
                .Where(record => record != null)
                .OrderByDescending(record => record.absoluteHour)
                .ThenByDescending(record => record.day)
                .Select(RoleActivityBoardHistoryRow.FromHistory)
            : Enumerable.Empty<RoleActivityBoardHistoryRow>();

        return maxHistoryRows > 0 ? rows.Take(maxHistoryRows) : rows;
    }

    void RecordRun(PlayerController player, RoleActivityBoardRunResult result) {
        if(player == null || result == null) {
            return;
        }

        var log = GetLog(player, createMissingLogForActions);
        log?.RecordRun(result, ResolveRegion(), ResolveZone());
    }

    PlayerRoleActivityBoardLog GetLog(PlayerController player, bool createIfMissing) {
        if(player == null) {
            return null;
        }

        var log = player.GetComponent<PlayerRoleActivityBoardLog>();
        return log != null || !createIfMissing ? log : player.gameObject.AddComponent<PlayerRoleActivityBoardLog>();
    }

    RoleActivityBoardDefinition ResolveBoard() {
        return board != null ? board : source != null ? source.Board : null;
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

        var resolvedBoard = ResolveBoard();
        return resolvedBoard != null ? resolvedBoard.ResolveBoardSourceId(null) : "ui:role-activity-board";
    }

    string ResolveSourceName() {
        if(source != null) {
            return source.DisplayName;
        }

        if(!string.IsNullOrWhiteSpace(uiSourceName)) {
            return uiSourceName;
        }

        var resolvedBoard = ResolveBoard();
        return resolvedBoard != null ? resolvedBoard.DisplayName : "Role Activity Board";
    }

    bool Succeed(RoleActivityBoardUIActionResultKind kind, string message, out string feedback) {
        feedback = message;
        SetLastResult(kind, true, message);
        if(logSuccessfulActions) {
            GameDebug.Success(message, GameDebugCategory.Activity, this, "RoleActivityBoardUIManager");
        }
        return true;
    }

    bool Block(string message, out string feedback) {
        feedback = string.IsNullOrWhiteSpace(message) ? "Role activity board action was blocked." : message;
        SetLastResult(RoleActivityBoardUIActionResultKind.Blocked, false, feedback);
        if(logBlockedActions) {
            GameDebug.Warning(feedback, GameDebugCategory.Activity, this, "RoleActivityBoardUIManager");
        }
        return false;
    }

    void SetLastResult(RoleActivityBoardUIActionResultKind kind, bool success, string message) {
        lastResult = new RoleActivityBoardUIActionResult {
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
public class RoleActivityBoardUIScreenSnapshot {
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
    [Tooltip("Role activity board snapshot used by board UI panels.")]
    public RoleActivityBoardSnapshot board;
    [Tooltip("Visible board row count.")]
    public int rowCount;
    [Tooltip("Rows that can run right now.")]
    public int availableRowCount;
    [Tooltip("Rows that are visible but locked.")]
    public int lockedRowCount;
    [Tooltip("History row count.")]
    public int historyCount;
    [Tooltip("Recent role board history rows.")]
    public List<RoleActivityBoardHistoryRow> history = new List<RoleActivityBoardHistoryRow>();
    [Tooltip("Most recent UI backend action result.")]
    public RoleActivityBoardUIActionResult lastResult;
}

[Serializable]
public class RoleActivityBoardUIActionResult {
    [Tooltip("Kind of UI backend action that produced this result.")]
    public RoleActivityBoardUIActionResultKind kind;
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
public class RoleActivityBoardHistoryRow {
    [Tooltip("Whether this record is a view, successful run or blocked attempt.")]
    public RoleActivityBoardLogOperation operation;
    [Tooltip("Board definition id.")]
    public string boardId;
    [Tooltip("Board display name saved for fallback/debug UI.")]
    public string boardName;
    [Tooltip("Entry id selected by the player or source.")]
    public string entryId;
    [Tooltip("Entry display name saved for fallback/debug UI.")]
    public string entryName;
    [Tooltip("Entry content type.")]
    public RoleActivityBoardEntryType entryType;
    [Tooltip("Scene/source id used by the board action.")]
    public string sourceId;
    [Tooltip("Scene/source display name used by the board action.")]
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

    public static RoleActivityBoardHistoryRow FromHistory(PlayerRoleActivityBoardHistory history) {
        return new RoleActivityBoardHistoryRow {
            operation = history != null ? history.operation : RoleActivityBoardLogOperation.Blocked,
            boardId = history != null ? history.boardId : string.Empty,
            boardName = history != null ? history.boardName : string.Empty,
            entryId = history != null ? history.entryId : string.Empty,
            entryName = history != null ? history.entryName : string.Empty,
            entryType = history != null ? history.entryType : RoleActivityBoardEntryType.Activity,
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
                ? $"{history.boardName}: {(string.IsNullOrWhiteSpace(history.entryName) ? history.operation.ToString() : history.entryName)}"
                : string.Empty
        };
    }
}
