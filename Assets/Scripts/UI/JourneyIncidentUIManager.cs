using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum JourneyIncidentUIActionResultKind {
    None,
    Refreshed,
    IncidentActivated,
    BoardRolled,
    IncidentResolved,
    IncidentExpired,
    Blocked
}

public class JourneyIncidentUIManager : MonoBehaviour {
    [Header("Player")]
    [Tooltip("Player whose journey incident state is shown. Empty uses PlayerController.i or the first PlayerController in the scene.")]
    [SerializeField] PlayerController playerOverride = null;
    [Tooltip("If enabled, missing PlayerJourneyIncidentLog is created when UI actions need it.")]
    [SerializeField] bool createMissingLogForActions = true;

    [Header("Sources")]
    [Tooltip("Optional overworld incident source used as the primary source for board snapshots and actions.")]
    [SerializeField] JourneyIncidentSource source = null;
    [Tooltip("Journey incident board shown when Source is empty or Source has no board.")]
    [SerializeField] JourneyIncidentBoardDefinition board = null;
    [Tooltip("Main direct incident shown by this UI backend when no board row is selected.")]
    [SerializeField] JourneyIncidentDefinition directIncident = null;
    [Tooltip("Additional direct incidents shown by this UI backend.")]
    [SerializeField] List<JourneyIncidentDefinition> directIncidents = new List<JourneyIncidentDefinition>();
    [Tooltip("Optional region context passed into incident and board checks. Empty uses Source region context.")]
    [SerializeField] RegionInfoDefinition regionContext = null;
    [Tooltip("Optional activity zone context passed into incident and board checks. Empty uses Source zone context or PlayerActivityContext.CurrentZone.")]
    [SerializeField] ActivityZoneDefinition zoneContext = null;
    [Tooltip("Source id used when no JourneyIncidentSource is assigned.")]
    [SerializeField] string uiSourceId = "ui:journey-incident";
    [Tooltip("Source name used when no JourneyIncidentSource is assigned.")]
    [SerializeField] string uiSourceName = "Journey Incidents";

    [Header("Snapshot")]
    [Tooltip("If enabled, Refresh is called when this component starts.")]
    [SerializeField] bool refreshOnStart = true;
    [Tooltip("If enabled, Refresh is called after every successful or blocked action.")]
    [SerializeField] bool refreshAfterActions = true;
    [Tooltip("If enabled, locked board and direct incident rows are included with a failure reason.")]
    [SerializeField] bool includeLockedRows = true;
    [Tooltip("Maximum board rows copied into the snapshot. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxBoardRows = 30;
    [Tooltip("Maximum direct incident rows copied into the snapshot. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxDirectRows = 20;
    [Tooltip("Maximum active incident rows copied into the snapshot. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxActiveRows = 20;
    [Tooltip("Maximum history rows copied into the snapshot. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxHistoryRows = 30;

    [Header("Debug")]
    [Tooltip("If enabled, successful UI backend actions are written to GameDebug.")]
    [SerializeField] bool logSuccessfulActions;
    [Tooltip("If enabled, blocked UI backend actions are written to GameDebug.")]
    [SerializeField] bool logBlockedActions = true;

    JourneyIncidentUIScreenSnapshot currentSnapshot = new JourneyIncidentUIScreenSnapshot();
    JourneyIncidentUIActionResult lastResult = new JourneyIncidentUIActionResult();

    public JourneyIncidentUIScreenSnapshot CurrentSnapshot => currentSnapshot;
    public JourneyIncidentUIActionResult LastResult => lastResult;
    public JourneyIncidentSource Source => source;
    public JourneyIncidentBoardDefinition Board => board;
    public JourneyIncidentDefinition DirectIncident => directIncident;
    public IReadOnlyList<JourneyIncidentDefinition> DirectIncidents => directIncidents;
    public RegionInfoDefinition RegionContext => regionContext;
    public ActivityZoneDefinition ZoneContext => zoneContext;
    public event Action<JourneyIncidentUIScreenSnapshot> OnSnapshotChanged;
    public event Action<JourneyIncidentUIActionResult> OnActionResult;

    void Start() {
        if(refreshOnStart) {
            Refresh();
        }
    }

    [ContextMenu("Refresh Journey Incident Snapshot")]
    public JourneyIncidentUIScreenSnapshot RefreshFromContextMenu() {
        return Refresh();
    }

    public JourneyIncidentUIScreenSnapshot Refresh() {
        var player = ResolvePlayer();
        var log = player != null ? player.GetComponent<PlayerJourneyIncidentLog>() : null;
        var region = ResolveRegion();
        var zone = ResolveZone();
        var boardSnapshot = BuildBoardSnapshot(player, log, region, zone);
        var directRows = BuildDirectRows(player, log, region, zone).ToList();
        var activeRows = BuildActiveRows(log).ToList();
        var historyRows = BuildHistoryRows(log).ToList();

        currentSnapshot = new JourneyIncidentUIScreenSnapshot {
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
            boardRowCount = boardSnapshot != null && boardSnapshot.rows != null ? boardSnapshot.rows.Count : 0,
            directRowCount = directRows.Count,
            activeCount = activeRows.Count,
            historyCount = historyRows.Count,
            directIncidents = directRows,
            activeIncidents = activeRows,
            history = historyRows,
            lastResult = lastResult
        };

        OnSnapshotChanged?.Invoke(currentSnapshot);
        return currentSnapshot;
    }

    public bool TryRollBoard(out string feedback) {
        var player = ResolvePlayer();
        if(player == null) {
            return Block("A player is required to roll journey incidents.", out feedback);
        }

        JourneyIncidentBoardRollResult result = null;
        bool success = false;
        if(source != null && source.Board != null) {
            success = source.TryRollBoard(player, out result);
        } else {
            var targetBoard = ResolveBoard();
            if(targetBoard == null) {
                return Block("No journey incident board is assigned.", out feedback);
            }

            result = targetBoard.Roll(player, ResolveRegion(), ResolveZone(), ResolveSourceId(), ResolveSourceName(), this);
            success = result != null && result.activatedIncidents > 0 && !result.blocked;
        }

        feedback = result != null && result.activatedIncidents > 0
            ? $"{result.activatedIncidents} journey incident(s) activated."
            : result != null && !string.IsNullOrWhiteSpace(result.failureMessage) ? result.failureMessage : "No journey incident activated.";

        return success
            ? Succeed(JourneyIncidentUIActionResultKind.BoardRolled, feedback, out feedback)
            : Block(feedback, out feedback);
    }

    public bool TryActivateDirectIncident(string incidentId, out string feedback) {
        var player = ResolvePlayer();
        if(player == null) {
            return Block("A player is required to activate journey incidents.", out feedback);
        }

        var incident = FindDirectIncident(incidentId);
        if(incident == null) {
            return Block($"Journey incident '{incidentId}' could not be found.", out feedback);
        }

        var result = incident.Activate(player, ResolveRegion(), ResolveZone(), ResolveSourceId(), ResolveSourceName(), this);
        feedback = result != null && !result.blocked
            ? $"{incident.DisplayName} activated."
            : result != null ? result.failureMessage : "Journey incident activation failed.";

        return result != null && !result.blocked
            ? Succeed(JourneyIncidentUIActionResultKind.IncidentActivated, feedback, out feedback)
            : Block(feedback, out feedback);
    }

    public bool TryActivateBoardRow(string entryId, out string feedback) {
        var player = ResolvePlayer();
        if(player == null) {
            return Block("A player is required to activate journey incidents.", out feedback);
        }

        var targetBoard = ResolveBoard();
        if(targetBoard == null) {
            return Block("No journey incident board is assigned.", out feedback);
        }

        var entry = targetBoard.GetOrderedEntries()
            .FirstOrDefault(row => row != null && string.Equals(row.ResolveEntryId(), entryId, StringComparison.OrdinalIgnoreCase));
        if(entry == null || entry.Incident == null) {
            return Block($"Journey incident board row '{entryId}' could not be found.", out feedback);
        }

        var log = GetLog(player, createMissingLogForActions);
        if(!entry.CanActivate(player, log, targetBoard, ResolveSourceId(), ResolveRegion(), ResolveZone(), out feedback)) {
            return Block(feedback, out feedback);
        }

        string rowSourceId = entry.ResolveSourceId(targetBoard, ResolveSourceId());
        var result = entry.Incident.Activate(player, ResolveRegion(), ResolveZone(), rowSourceId, ResolveSourceName(), this);
        feedback = result != null && !result.blocked
            ? $"{entry.ResolveDisplayName()} activated."
            : result != null ? result.failureMessage : "Journey incident board row failed.";

        return result != null && !result.blocked
            ? Succeed(JourneyIncidentUIActionResultKind.IncidentActivated, feedback, out feedback)
            : Block(feedback, out feedback);
    }

    public bool TryResolveIncident(string incidentId, out string feedback) {
        var player = ResolvePlayer();
        if(player == null) {
            return Block("A player is required to resolve journey incidents.", out feedback);
        }

        var incident = FindKnownIncident(incidentId);
        if(incident == null) {
            return Block($"Journey incident '{incidentId}' could not be found.", out feedback);
        }

        int resolved = incident.ResolveActive(player, ResolveRegion(), ResolveZone(), ResolveSourceId(), this);
        feedback = resolved > 0 ? $"{incident.DisplayName} resolved." : $"{incident.DisplayName} is not active.";
        return resolved > 0
            ? Succeed(JourneyIncidentUIActionResultKind.IncidentResolved, feedback, out feedback)
            : Block(feedback, out feedback);
    }

    public bool TryExpireIncident(string incidentId, out string feedback) {
        var player = ResolvePlayer();
        if(player == null) {
            return Block("A player is required to expire journey incidents.", out feedback);
        }

        var incident = FindKnownIncident(incidentId);
        if(incident == null) {
            return Block($"Journey incident '{incidentId}' could not be found.", out feedback);
        }

        int expired = incident.ExpireActive(player, ResolveRegion(), ResolveZone(), ResolveSourceId(), this);
        feedback = expired > 0 ? $"{incident.DisplayName} expired." : $"{incident.DisplayName} is not active.";
        return expired > 0
            ? Succeed(JourneyIncidentUIActionResultKind.IncidentExpired, feedback, out feedback)
            : Block(feedback, out feedback);
    }

    JourneyIncidentBoardSnapshot BuildBoardSnapshot(PlayerController player, PlayerJourneyIncidentLog log, RegionInfoDefinition region, ActivityZoneDefinition zone) {
        JourneyIncidentBoardSnapshot snapshot = null;
        if(source != null && source.Board != null) {
            snapshot = source.GetSnapshot(player);
        } else {
            var targetBoard = ResolveBoard();
            if(targetBoard != null) {
                var actionLog = player != null ? log ?? GetLog(player, createMissingLogForActions) : null;
                snapshot = targetBoard.BuildSnapshot(player, actionLog, ResolveSourceId(), ResolveSourceName(), region, zone, includeLockedRows, this);
            }
        }

        if(snapshot?.rows != null && maxBoardRows > 0 && snapshot.rows.Count > maxBoardRows) {
            snapshot.rows = snapshot.rows.Take(maxBoardRows).ToList();
        }

        return snapshot;
    }

    IEnumerable<JourneyIncidentDirectRow> BuildDirectRows(PlayerController player, PlayerJourneyIncidentLog log, RegionInfoDefinition region, ActivityZoneDefinition zone) {
        var actionLog = player != null ? log ?? GetLog(player, createMissingLogForActions) : null;
        var rows = ResolveDirectIncidents()
            .Where(incident => incident != null)
            .Distinct()
            .OrderByDescending(incident => incident.Priority)
            .ThenBy(incident => incident.DisplayName)
            .Select(incident => JourneyIncidentDirectRow.FromIncident(
                incident,
                player,
                actionLog,
                ResolveSourceId(),
                ResolveSourceName(),
                region,
                zone));

        rows = includeLockedRows ? rows : rows.Where(row => row != null && row.canActivate);
        return Limit(rows, maxDirectRows);
    }

    IEnumerable<JourneyIncidentActiveRow> BuildActiveRows(PlayerJourneyIncidentLog log) {
        var rows = log != null
            ? log.ActiveIncidents
                .Where(state => state != null)
                .OrderBy(state => state.expiresAbsoluteHour < 0 ? int.MaxValue : state.expiresAbsoluteHour)
                .ThenByDescending(state => state.startedAbsoluteHour)
                .Select(JourneyIncidentActiveRow.FromState)
            : Enumerable.Empty<JourneyIncidentActiveRow>();

        return Limit(rows, maxActiveRows);
    }

    IEnumerable<JourneyIncidentHistoryRow> BuildHistoryRows(PlayerJourneyIncidentLog log) {
        var rows = log != null
            ? log.Records
                .Where(record => record != null)
                .OrderByDescending(record => record.absoluteHour)
                .ThenByDescending(record => record.day)
                .Select(JourneyIncidentHistoryRow.FromRecord)
            : Enumerable.Empty<JourneyIncidentHistoryRow>();

        return Limit(rows, maxHistoryRows);
    }

    IEnumerable<JourneyIncidentDefinition> ResolveDirectIncidents() {
        if(directIncident != null) {
            yield return directIncident;
        }

        foreach(var incident in directIncidents) {
            if(incident != null) {
                yield return incident;
            }
        }
    }

    JourneyIncidentDefinition FindDirectIncident(string incidentId) {
        return ResolveDirectIncidents()
            .FirstOrDefault(incident => incident != null
                && (string.Equals(incident.Id, incidentId, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(incident.name, incidentId, StringComparison.OrdinalIgnoreCase)));
    }

    JourneyIncidentDefinition FindKnownIncident(string incidentId) {
        var direct = FindDirectIncident(incidentId);
        if(direct != null) {
            return direct;
        }

        var boardEntry = ResolveBoard()?.GetOrderedEntries()
            .FirstOrDefault(entry => entry != null && entry.Incident != null
                && (string.Equals(entry.Incident.Id, incidentId, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(entry.ResolveEntryId(), incidentId, StringComparison.OrdinalIgnoreCase)));
        if(boardEntry != null) {
            return boardEntry.Incident;
        }

        var activeState = ResolvePlayer()?.GetComponent<PlayerJourneyIncidentLog>()?.ActiveIncidents
            .FirstOrDefault(state => state != null && string.Equals(state.incidentId, incidentId, StringComparison.OrdinalIgnoreCase));
        return activeState?.ResolveDefinition();
    }

    JourneyIncidentBoardDefinition ResolveBoard() {
        return board != null ? board : source != null ? source.Board : null;
    }

    PlayerJourneyIncidentLog GetLog(PlayerController player, bool createIfMissing) {
        if(player == null) {
            return null;
        }

        var log = player.GetComponent<PlayerJourneyIncidentLog>();
        return log != null || !createIfMissing ? log : player.gameObject.AddComponent<PlayerJourneyIncidentLog>();
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

        return string.IsNullOrWhiteSpace(uiSourceId) ? "ui:journey-incident" : uiSourceId;
    }

    string ResolveSourceName() {
        if(source != null) {
            return source.DisplayName;
        }

        return string.IsNullOrWhiteSpace(uiSourceName) ? "Journey Incidents" : uiSourceName;
    }

    bool Succeed(JourneyIncidentUIActionResultKind kind, string message, out string feedback) {
        feedback = message;
        SetLastResult(kind, true, message);
        if(logSuccessfulActions) {
            GameDebug.Success(message, GameDebugCategory.Activity, this, "JourneyIncidentUIManager");
        }
        return true;
    }

    bool Block(string message, out string feedback) {
        feedback = string.IsNullOrWhiteSpace(message) ? "Journey incident action was blocked." : message;
        SetLastResult(JourneyIncidentUIActionResultKind.Blocked, false, feedback);
        if(logBlockedActions) {
            GameDebug.Warning(feedback, GameDebugCategory.Activity, this, "JourneyIncidentUIManager");
        }
        return false;
    }

    void SetLastResult(JourneyIncidentUIActionResultKind kind, bool success, string message) {
        lastResult = new JourneyIncidentUIActionResult {
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

    static IEnumerable<T> Limit<T>(IEnumerable<T> query, int limit) {
        return limit > 0 ? query.Take(limit) : query;
    }
}

[Serializable]
public class JourneyIncidentUIScreenSnapshot {
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
    [Tooltip("Optional board snapshot used by board-like UI panels.")]
    public JourneyIncidentBoardSnapshot board;
    [Tooltip("Visible board row count.")]
    public int boardRowCount;
    [Tooltip("Visible direct incident row count.")]
    public int directRowCount;
    [Tooltip("Active incident row count.")]
    public int activeCount;
    [Tooltip("History row count.")]
    public int historyCount;
    [Tooltip("Direct incident rows.")]
    public List<JourneyIncidentDirectRow> directIncidents = new List<JourneyIncidentDirectRow>();
    [Tooltip("Active incident rows.")]
    public List<JourneyIncidentActiveRow> activeIncidents = new List<JourneyIncidentActiveRow>();
    [Tooltip("Recent incident history rows.")]
    public List<JourneyIncidentHistoryRow> history = new List<JourneyIncidentHistoryRow>();
    [Tooltip("Most recent UI backend action result.")]
    public JourneyIncidentUIActionResult lastResult;
}

[Serializable]
public class JourneyIncidentUIActionResult {
    [Tooltip("Kind of UI backend action that produced this result.")]
    public JourneyIncidentUIActionResultKind kind;
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
public class JourneyIncidentDirectRow {
    [Tooltip("Incident definition id.")]
    public string incidentId;
    [Tooltip("Display name shown for this incident.")]
    public string displayName;
    [Tooltip("Description shown for this incident.")]
    public string description;
    [Tooltip("Incident category used by filters.")]
    public JourneyIncidentCategory category;
    [Tooltip("Incident severity used by filters and future UI color.")]
    public JourneyIncidentSeverity severity;
    [Tooltip("Sort priority copied from the incident.")]
    public int priority;
    [Tooltip("If enabled, this incident can activate right now.")]
    public bool canActivate;
    [Tooltip("If enabled, this incident is already active in this context.")]
    public bool isActive;
    [Tooltip("Failure reason shown when the incident is locked.")]
    public string failureMessage;
    [Tooltip("Resolved source id used when the incident activates.")]
    public string sourceId;
    [Tooltip("Resolved source name used when the incident activates.")]
    public string sourceName;
    [Tooltip("Region id used by this row.")]
    public string regionId;
    [Tooltip("Region name used by this row.")]
    public string regionName;
    [Tooltip("Activity zone id used by this row.")]
    public string zoneId;
    [Tooltip("Activity zone name used by this row.")]
    public string zoneName;
    [Tooltip("Free-form incident tags.")]
    public List<string> tags = new List<string>();
    [Tooltip("Short text useful for placeholder UI rows.")]
    public string displayText;

    public static JourneyIncidentDirectRow FromIncident(
        JourneyIncidentDefinition incident,
        PlayerController player,
        PlayerJourneyIncidentLog log,
        string sourceId,
        string sourceName,
        RegionInfoDefinition region,
        ActivityZoneDefinition zone) {
        string failureMessage = null;
        bool canActivate = incident != null && incident.CanActivate(player, log, region, zone, sourceId, out failureMessage);
        bool active = incident != null && (log?.IsActive(incident, null, region, zone) ?? false);
        return new JourneyIncidentDirectRow {
            incidentId = incident != null ? incident.Id : string.Empty,
            displayName = incident != null ? incident.DisplayName : string.Empty,
            description = incident != null ? incident.Description : string.Empty,
            category = incident != null ? incident.Category : JourneyIncidentCategory.General,
            severity = incident != null ? incident.Severity : JourneyIncidentSeverity.Info,
            priority = incident != null ? incident.Priority : 0,
            canActivate = canActivate,
            isActive = active,
            failureMessage = failureMessage,
            sourceId = sourceId,
            sourceName = sourceName,
            regionId = region != null ? region.Id : string.Empty,
            regionName = region != null ? region.DisplayName : string.Empty,
            zoneId = zone != null ? zone.Id : string.Empty,
            zoneName = zone != null ? zone.DisplayName : string.Empty,
            tags = incident != null ? incident.Tags.ToList() : new List<string>(),
            displayText = incident != null ? $"{incident.DisplayName} - {(canActivate ? "available" : active ? "active" : "locked")}" : "Missing incident"
        };
    }
}

[Serializable]
public class JourneyIncidentActiveRow {
    [Tooltip("Unique active incident id.")]
    public string activeId;
    [Tooltip("Incident definition id.")]
    public string incidentId;
    [Tooltip("Incident display name.")]
    public string incidentName;
    [Tooltip("Incident category.")]
    public JourneyIncidentCategory category;
    [Tooltip("Incident severity.")]
    public JourneyIncidentSeverity severity;
    [Tooltip("Source id that activated this incident.")]
    public string sourceId;
    [Tooltip("Source display name saved for fallback/debug UI.")]
    public string sourceName;
    [Tooltip("Region id this incident is active in.")]
    public string regionId;
    [Tooltip("Region display name.")]
    public string regionName;
    [Tooltip("Activity zone id this incident is active in.")]
    public string zoneId;
    [Tooltip("Activity zone display name.")]
    public string zoneName;
    [Tooltip("Start absolute in-game hour.")]
    public int startedAbsoluteHour;
    [Tooltip("Expiry absolute in-game hour.")]
    public int expiresAbsoluteHour;
    [Tooltip("Remaining hours until expiry. 0 means expired or no timer.")]
    public int hoursRemaining;
    [Tooltip("If enabled, the incident is set to expire automatically.")]
    public bool expireAutomatically;
    [Tooltip("Short text useful for placeholder UI rows.")]
    public string displayText;

    public static JourneyIncidentActiveRow FromState(PlayerJourneyIncidentState state) {
        int currentHour = TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
        int remaining = state != null && state.expiresAbsoluteHour >= 0 ? Mathf.Max(0, state.expiresAbsoluteHour - currentHour) : 0;
        return new JourneyIncidentActiveRow {
            activeId = state != null ? state.activeId : string.Empty,
            incidentId = state != null ? state.incidentId : string.Empty,
            incidentName = state != null ? state.incidentName : string.Empty,
            category = state != null ? state.category : JourneyIncidentCategory.General,
            severity = state != null ? state.severity : JourneyIncidentSeverity.Info,
            sourceId = state != null ? state.sourceId : string.Empty,
            sourceName = state != null ? state.sourceName : string.Empty,
            regionId = state != null ? state.regionId : string.Empty,
            regionName = state != null ? state.regionName : string.Empty,
            zoneId = state != null ? state.zoneId : string.Empty,
            zoneName = state != null ? state.zoneName : string.Empty,
            startedAbsoluteHour = state != null ? state.startedAbsoluteHour : 0,
            expiresAbsoluteHour = state != null ? state.expiresAbsoluteHour : -1,
            hoursRemaining = remaining,
            expireAutomatically = state != null && state.expireAutomatically,
            displayText = state != null ? $"{state.incidentName} {(remaining > 0 ? remaining + "h" : "active")}" : string.Empty
        };
    }
}

[Serializable]
public class JourneyIncidentHistoryRow {
    [Tooltip("Unique saved record id.")]
    public string recordId;
    [Tooltip("Incident definition id.")]
    public string incidentId;
    [Tooltip("Incident display name.")]
    public string incidentName;
    [Tooltip("Incident category.")]
    public JourneyIncidentCategory category;
    [Tooltip("Incident severity.")]
    public JourneyIncidentSeverity severity;
    [Tooltip("What happened to the incident.")]
    public JourneyIncidentPhase phase;
    [Tooltip("Source id that caused this record.")]
    public string sourceId;
    [Tooltip("Source display name saved for fallback/debug UI.")]
    public string sourceName;
    [Tooltip("Region id related to this record.")]
    public string regionId;
    [Tooltip("Region display name.")]
    public string regionName;
    [Tooltip("Activity zone id related to this record.")]
    public string zoneId;
    [Tooltip("Activity zone display name.")]
    public string zoneName;
    [Tooltip("In-game day when this record was created.")]
    public int day;
    [Tooltip("Absolute in-game hour when this record was created.")]
    public int absoluteHour;
    [Tooltip("If enabled, this record represents a blocked attempt.")]
    public bool blocked;
    [Tooltip("Failure reason for blocked attempts.")]
    public string failureMessage;
    [Tooltip("Short text useful for placeholder UI rows.")]
    public string displayText;

    public static JourneyIncidentHistoryRow FromRecord(PlayerJourneyIncidentRecord record) {
        return new JourneyIncidentHistoryRow {
            recordId = record != null ? record.recordId : string.Empty,
            incidentId = record != null ? record.incidentId : string.Empty,
            incidentName = record != null ? record.incidentName : string.Empty,
            category = record != null ? record.category : JourneyIncidentCategory.General,
            severity = record != null ? record.severity : JourneyIncidentSeverity.Info,
            phase = record != null ? record.phase : JourneyIncidentPhase.Blocked,
            sourceId = record != null ? record.sourceId : string.Empty,
            sourceName = record != null ? record.sourceName : string.Empty,
            regionId = record != null ? record.regionId : string.Empty,
            regionName = record != null ? record.regionName : string.Empty,
            zoneId = record != null ? record.zoneId : string.Empty,
            zoneName = record != null ? record.zoneName : string.Empty,
            day = record != null ? record.day : 0,
            absoluteHour = record != null ? record.absoluteHour : 0,
            blocked = record != null && record.blocked,
            failureMessage = record != null ? record.failureMessage : string.Empty,
            displayText = record != null ? $"{record.incidentName} - {record.phase}" : string.Empty
        };
    }
}
