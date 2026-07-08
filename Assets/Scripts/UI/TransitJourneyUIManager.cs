using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum TransitJourneyUIActionKind {
    None,
    Refreshed,
    Started,
    TimeAdvanced,
    Continued,
    Disembarked,
    Cancelled,
    OnboardActivityRecorded,
    Blocked
}

public class TransitJourneyUIManager : MonoBehaviour {
    [Header("Player")]
    [Tooltip("Player whose transit journey state is shown. Empty uses PlayerController.i or the first PlayerController in the scene.")]
    [SerializeField] PlayerController playerOverride;
    [Tooltip("If enabled, missing PlayerTransitLog and PlayerTransitJourneyLog components are created when UI actions need them.")]
    [SerializeField] bool createMissingLogsForActions = true;

    [Header("Sources")]
    [Tooltip("Optional scene source used as the primary journey/origin context.")]
    [SerializeField] TransitJourneySource source;
    [Tooltip("Optional station used to resolve origin stop and station routes when no source is assigned.")]
    [SerializeField] TransitStation station;
    [Tooltip("Optional origin stop id override used by journey option checks. Empty uses source/station/first leg fallback.")]
    [SerializeField] string originStopId = string.Empty;
    [Tooltip("Source id written into journey logs/events when this UI performs actions.")]
    [SerializeField] string uiSourceId = "ui:transit-journey";

    [Header("Journey Pool")]
    [Tooltip("Journeys explicitly shown by this UI. Empty can still read Resources when Include Resource Journeys is enabled.")]
    [SerializeField] List<TransitJourneyDefinition> journeyPool = new List<TransitJourneyDefinition>();
    [Tooltip("If enabled, the assigned source journey is inserted into the visible journey pool.")]
    [SerializeField] bool includeSourceJourney = true;
    [Tooltip("If enabled, all TransitJourneyDefinition assets in Resources are added to the journey pool.")]
    [SerializeField] bool includeResourceJourneys = true;

    [Header("Visibility")]
    [Tooltip("Optional lowercase/uppercase-insensitive text filter applied to journey, leg, history and activity rows.")]
    [SerializeField] string searchText = string.Empty;
    [Tooltip("Optional journey tag filter.")]
    [SerializeField] string tagFilter = string.Empty;
    [Tooltip("If enabled, blocked journeys remain visible with failure text.")]
    [SerializeField] bool includeBlockedJourneys = true;
    [Tooltip("If enabled, completed/cancelled journey history rows are included in the snapshot.")]
    [SerializeField] bool includeJourneyHistory = true;
    [Tooltip("If enabled, onboard activity rows are included in the snapshot.")]
    [SerializeField] bool includeOnboardActivityHistory = true;
    [Tooltip("Maximum journey option rows copied into the snapshot. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxJourneyRows = 30;
    [Tooltip("Maximum leg rows copied into the snapshot. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxLegRows = 20;
    [Tooltip("Maximum journey history rows copied into the snapshot. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxHistoryRows = 20;
    [Tooltip("Maximum onboard activity rows copied into the snapshot. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxOnboardActivityRows = 20;

    [Header("Action Defaults")]
    [Tooltip("Default hours spent when the UI calls Advance Time without an explicit value.")]
    [Min(0)]
    [SerializeField] int defaultAdvanceHours = 1;
    [Tooltip("Default onboard activity id used by Record Default Onboard Activity.")]
    [SerializeField] string defaultOnboardActivityId = "wait";
    [Tooltip("Default onboard activity display name used by Record Default Onboard Activity.")]
    [SerializeField] string defaultOnboardActivityName = "Wait";
    [Tooltip("Default cancellation reason used by Cancel Active Journey.")]
    [SerializeField] string defaultCancelReason = "cancelled-from-ui";

    [Header("Snapshot")]
    [Tooltip("If enabled, Refresh is called when this component starts.")]
    [SerializeField] bool refreshOnStart = true;
    [Tooltip("If enabled, Refresh is called after every successful or blocked action.")]
    [SerializeField] bool refreshAfterActions = true;
    [Tooltip("If enabled, this manager subscribes to PlayerTransitJourneyLog changes while active.")]
    [SerializeField] bool refreshWhenLogChanges = true;

    [Header("Debug")]
    [Tooltip("If enabled, successful UI backend actions are written to GameDebug.")]
    [SerializeField] bool logSuccessfulActions;
    [Tooltip("If enabled, blocked UI backend actions are written to GameDebug.")]
    [SerializeField] bool logBlockedActions = true;

    TransitJourneyUIScreenSnapshot currentSnapshot = new TransitJourneyUIScreenSnapshot();
    TransitJourneyUIActionResult lastResult = new TransitJourneyUIActionResult();
    PlayerController subscribedPlayer;
    PlayerTransitJourneyLog subscribedJourneyLog;

    public TransitJourneyUIScreenSnapshot CurrentSnapshot => currentSnapshot;
    public TransitJourneyUIActionResult LastResult => lastResult;
    public PlayerController PlayerOverride => playerOverride;
    public bool CreateMissingLogsForActions => createMissingLogsForActions;
    public TransitJourneySource Source => source;
    public TransitStation Station => station;
    public string OriginStopId => originStopId;
    public string UISourceId => uiSourceId;
    public IReadOnlyList<TransitJourneyDefinition> JourneyPool => journeyPool;
    public bool IncludeSourceJourney => includeSourceJourney;
    public bool IncludeResourceJourneys => includeResourceJourneys;
    public string SearchText => searchText;
    public string TagFilter => tagFilter;
    public bool IncludeBlockedJourneys => includeBlockedJourneys;
    public bool IncludeJourneyHistory => includeJourneyHistory;
    public bool IncludeOnboardActivityHistory => includeOnboardActivityHistory;
    public event Action<TransitJourneyUIScreenSnapshot> OnSnapshotChanged;
    public event Action<TransitJourneyUIActionResult> OnActionResult;

    void OnEnable() {
        SubscribeToLog();
    }

    void Start() {
        if(refreshOnStart) {
            Refresh();
        }
    }

    void OnDisable() {
        UnsubscribeFromLog();
    }

    [ContextMenu("Refresh Transit Journey Snapshot")]
    public TransitJourneyUIScreenSnapshot RefreshFromContextMenu() {
        return Refresh();
    }

    public TransitJourneyUIScreenSnapshot Refresh() {
        SubscribeToLog();

        var player = ResolvePlayer();
        var transitLog = player != null ? player.GetComponent<PlayerTransitLog>() : null;
        var journeyLog = player != null ? player.GetComponent<PlayerTransitJourneyLog>() : null;
        var journeyRows = BuildJourneyRows(player, transitLog).ToList();
        var activeRow = TransitJourneyActiveRow.FromState(journeyLog?.ActiveJourney);
        var legRows = BuildLegRows(activeRow?.journeyId).ToList();
        var historyRows = includeJourneyHistory ? BuildHistoryRows(journeyLog).ToList() : new List<TransitJourneyHistoryRow>();
        var activityRows = includeOnboardActivityHistory ? BuildOnboardActivityRows(journeyLog).ToList() : new List<TransitOnboardActivityRow>();

        currentSnapshot = new TransitJourneyUIScreenSnapshot {
            hasPlayer = player != null,
            playerName = player != null ? player.name : string.Empty,
            hasTransitLog = transitLog != null,
            hasJourneyLog = journeyLog != null,
            hasActiveJourney = journeyLog != null && journeyLog.HasActiveJourney,
            sourceId = ResolveSourceId(),
            originStopId = ResolveOriginStopId(),
            day = TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1,
            hour = TimeSystem.i != null ? Mathf.Clamp(TimeSystem.i.Hour, 0, 23) : 0,
            absoluteHour = GetCurrentAbsoluteHour(),
            journeyCount = journeyRows.Count,
            availableJourneyCount = journeyRows.Count(row => row != null && row.canStart),
            blockedJourneyCount = journeyRows.Count(row => row != null && !row.canStart),
            historyCount = historyRows.Count,
            onboardActivityCount = activityRows.Count,
            activeJourney = activeRow,
            journeyRows = journeyRows,
            legRows = legRows,
            historyRows = historyRows,
            onboardActivityRows = activityRows,
            lastResult = lastResult
        };

        OnSnapshotChanged?.Invoke(currentSnapshot);
        return currentSnapshot;
    }

    public bool TryStartJourney(TransitJourneyDefinition journey, out string feedback) {
        var player = ResolvePlayer();
        if(player == null) {
            return Block("A player is required to start transit journeys.", out feedback);
        }

        if(journey == null) {
            journey = ResolvePrimaryJourney();
        }

        if(journey == null) {
            return Block("No transit journey was selected.", out feedback);
        }

        var log = GetJourneyLog(player, createMissingLogsForActions);
        if(log == null) {
            return Block("PlayerTransitJourneyLog is missing.", out feedback);
        }

        GetTransitLog(player, createMissingLogsForActions);
        if(log.TryStartJourney(player, journey, ResolveOriginStopId(), ResolveSourceId(), out feedback)) {
            return Succeed(TransitJourneyUIActionKind.Started, $"{journey.DisplayName} started.", out feedback);
        }

        return Block(feedback, out feedback);
    }

    public bool TryStartJourneyById(string journeyId, out string feedback) {
        return TryStartJourney(FindJourney(journeyId), out feedback);
    }

    public bool TryStartPrimaryJourney(out string feedback) {
        if(source != null) {
            var player = ResolvePlayer();
            if(player == null) {
                return Block("A player is required to start transit journeys.", out feedback);
            }

            GetTransitLog(player, createMissingLogsForActions);
            GetJourneyLog(player, createMissingLogsForActions);
            if(source.TryStartJourney(player, out feedback)) {
                return Succeed(TransitJourneyUIActionKind.Started, "Transit journey started.", out feedback);
            }

            return Block(feedback, out feedback);
        }

        return TryStartJourney(ResolvePrimaryJourney(), out feedback);
    }

    public bool TryAdvanceTime(out string feedback) {
        return TryAdvanceTime(defaultAdvanceHours, out feedback);
    }

    public bool TryAdvanceTime(int hours, out string feedback) {
        var player = ResolvePlayer();
        var log = player != null ? player.GetComponent<PlayerTransitJourneyLog>() : null;
        if(log == null) {
            return Block("PlayerTransitJourneyLog is missing.", out feedback);
        }

        int clampedHours = Mathf.Max(0, hours);
        if(log.TryAdvanceTime(player, clampedHours, out feedback)) {
            return Succeed(TransitJourneyUIActionKind.TimeAdvanced, $"Transit journey advanced by {clampedHours} hour(s).", out feedback);
        }

        return Block(feedback, out feedback);
    }

    public bool TryContinueJourney(out string feedback) {
        var player = ResolvePlayer();
        var log = player != null ? player.GetComponent<PlayerTransitJourneyLog>() : null;
        if(log == null) {
            return Block("PlayerTransitJourneyLog is missing.", out feedback);
        }

        if(log.TryContinueJourney(player, out feedback)) {
            return Succeed(TransitJourneyUIActionKind.Continued, "Transit journey continued.", out feedback);
        }

        return Block(feedback, out feedback);
    }

    public bool TryDisembark(out string feedback) {
        var player = ResolvePlayer();
        var log = player != null ? player.GetComponent<PlayerTransitJourneyLog>() : null;
        if(log == null) {
            return Block("PlayerTransitJourneyLog is missing.", out feedback);
        }

        if(log.TryDisembark(player, out feedback)) {
            return Succeed(TransitJourneyUIActionKind.Disembarked, "Disembarked from transit journey.", out feedback);
        }

        return Block(feedback, out feedback);
    }

    public bool TryCancelJourney(out string feedback) {
        return TryCancelJourney(defaultCancelReason, out feedback);
    }

    public bool TryCancelJourney(string reason, out string feedback) {
        var player = ResolvePlayer();
        var log = player != null ? player.GetComponent<PlayerTransitJourneyLog>() : null;
        if(log == null) {
            return Block("PlayerTransitJourneyLog is missing.", out feedback);
        }

        if(log.TryCancelJourney(player, reason, out feedback)) {
            return Succeed(TransitJourneyUIActionKind.Cancelled, "Transit journey cancelled.", out feedback);
        }

        return Block(feedback, out feedback);
    }

    public bool TryRecordDefaultOnboardActivity(out string feedback) {
        return TryRecordOnboardActivity(defaultOnboardActivityId, defaultOnboardActivityName, defaultAdvanceHours, out feedback);
    }

    public bool TryRecordOnboardActivity(string activityId, string displayName, int hoursSpent, out string feedback) {
        var player = ResolvePlayer();
        var log = player != null ? player.GetComponent<PlayerTransitJourneyLog>() : null;
        if(log == null || !log.HasActiveJourney) {
            return Block("No active transit journey.", out feedback);
        }

        int clampedHours = Mathf.Max(0, hoursSpent);
        log.RecordOnboardActivity(activityId, displayName, clampedHours, ResolveSourceId());
        if(clampedHours > 0) {
            log.TryAdvanceTime(player, clampedHours, out _);
        }

        return Succeed(TransitJourneyUIActionKind.OnboardActivityRecorded, $"{displayName} recorded.", out feedback);
    }

    public void SetSearchText(string value) {
        searchText = value ?? string.Empty;
        Refresh();
    }

    public void SetTagFilter(string value) {
        tagFilter = value ?? string.Empty;
        Refresh();
    }

    IEnumerable<TransitJourneyOptionRow> BuildJourneyRows(PlayerController player, PlayerTransitLog transitLog) {
        var rows = ResolveJourneyPool()
            .Where(journey => journey != null)
            .Select(journey => TransitJourneyOptionRow.FromDefinition(journey, player, transitLog, ResolveOriginStopId()))
            .Where(row => row != null)
            .Where(row => includeBlockedJourneys || row.canStart)
            .Where(RowPassesFilters)
            .OrderByDescending(row => row.canStart)
            .ThenBy(row => row.journeyType)
            .ThenBy(row => row.displayName);

        return LimitRows(rows, maxJourneyRows);
    }

    IEnumerable<TransitJourneyLegRow> BuildLegRows(string activeJourneyId) {
        var journey = !string.IsNullOrWhiteSpace(activeJourneyId) ? FindJourney(activeJourneyId) : ResolvePrimaryJourney();
        var rows = journey != null
            ? journey.Legs.Where(leg => leg != null).Select((leg, index) => TransitJourneyLegRow.FromLeg(journey, leg, index))
            : Enumerable.Empty<TransitJourneyLegRow>();

        return LimitRows(rows.Where(RowPassesFilters), maxLegRows);
    }

    IEnumerable<TransitJourneyHistoryRow> BuildHistoryRows(PlayerTransitJourneyLog log) {
        var rows = log != null
            ? log.JourneyHistory
                .Where(record => record != null)
                .OrderByDescending(record => record.endedAbsoluteHour)
                .Select(TransitJourneyHistoryRow.FromRecord)
            : Enumerable.Empty<TransitJourneyHistoryRow>();

        return LimitRows(rows.Where(RowPassesFilters), maxHistoryRows);
    }

    IEnumerable<TransitOnboardActivityRow> BuildOnboardActivityRows(PlayerTransitJourneyLog log) {
        var rows = log != null
            ? log.OnboardActivityHistory
                .Where(record => record != null)
                .OrderByDescending(record => record.absoluteHour)
                .Select(TransitOnboardActivityRow.FromRecord)
            : Enumerable.Empty<TransitOnboardActivityRow>();

        return LimitRows(rows.Where(RowPassesFilters), maxOnboardActivityRows);
    }

    IEnumerable<TransitJourneyDefinition> ResolveJourneyPool() {
        var explicitItems = new List<TransitJourneyDefinition>();
        if(includeSourceJourney && source != null && source.Journey != null) {
            explicitItems.Add(source.Journey);
        }

        if(journeyPool != null) {
            explicitItems.AddRange(journeyPool);
        }

        return MergeDefinitions(explicitItems, includeResourceJourneys ? Resources.LoadAll<TransitJourneyDefinition>("") : Array.Empty<TransitJourneyDefinition>(), journey => journey.Id);
    }

    IEnumerable<T> MergeDefinitions<T>(IEnumerable<T> explicitItems, IEnumerable<T> resourceItems, Func<T, string> idSelector) where T : UnityEngine.Object {
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach(var item in explicitItems ?? Enumerable.Empty<T>()) {
            if(item == null) continue;
            string id = idSelector(item);
            if(seenIds.Add(string.IsNullOrWhiteSpace(id) ? item.name : id)) {
                yield return item;
            }
        }

        foreach(var item in resourceItems ?? Enumerable.Empty<T>()) {
            if(item == null) continue;
            string id = idSelector(item);
            if(seenIds.Add(string.IsNullOrWhiteSpace(id) ? item.name : id)) {
                yield return item;
            }
        }
    }

    IEnumerable<T> LimitRows<T>(IEnumerable<T> rows, int maxRows) {
        if(rows == null) {
            return Enumerable.Empty<T>();
        }

        return maxRows > 0 ? rows.Take(maxRows) : rows;
    }

    bool RowPassesFilters(ITransitJourneyFilterable row) {
        if(row == null) {
            return false;
        }

        if(!string.IsNullOrWhiteSpace(searchText) && !row.MatchesSearch(searchText)) {
            return false;
        }

        if(!string.IsNullOrWhiteSpace(tagFilter) && !row.HasTag(tagFilter)) {
            return false;
        }

        return true;
    }

    TransitJourneyDefinition ResolvePrimaryJourney() {
        if(source != null && source.Journey != null) {
            return source.Journey;
        }

        return ResolveJourneyPool().FirstOrDefault();
    }

    TransitJourneyDefinition FindJourney(string journeyId) {
        if(string.IsNullOrWhiteSpace(journeyId)) {
            return null;
        }

        return ResolveJourneyPool()
            .FirstOrDefault(journey => journey != null && string.Equals(journey.Id, journeyId, StringComparison.OrdinalIgnoreCase));
    }

    PlayerTransitLog GetTransitLog(PlayerController player, bool createIfMissing) {
        if(player == null) {
            return null;
        }

        var log = player.GetComponent<PlayerTransitLog>();
        return log != null || !createIfMissing ? log : player.gameObject.AddComponent<PlayerTransitLog>();
    }

    PlayerTransitJourneyLog GetJourneyLog(PlayerController player, bool createIfMissing) {
        if(player == null) {
            return null;
        }

        var log = player.GetComponent<PlayerTransitJourneyLog>();
        return log != null || !createIfMissing ? log : player.gameObject.AddComponent<PlayerTransitJourneyLog>();
    }

    string ResolveOriginStopId() {
        if(!string.IsNullOrWhiteSpace(originStopId)) {
            return originStopId;
        }

        if(source != null && !string.IsNullOrWhiteSpace(source.OriginStopId)) {
            return source.OriginStopId;
        }

        if(station != null) {
            return station.StationId;
        }

        var primary = ResolvePrimaryJourney();
        return primary?.GetLeg(0)?.OriginStopId ?? string.Empty;
    }

    string ResolveSourceId() {
        if(!string.IsNullOrWhiteSpace(uiSourceId)) {
            return uiSourceId;
        }

        return source != null ? source.SourceId : "ui:transit-journey";
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

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    void SubscribeToLog() {
        if(!refreshWhenLogChanges) {
            return;
        }

        var player = ResolvePlayer();
        if(player == subscribedPlayer) {
            return;
        }

        UnsubscribeFromLog();
        subscribedPlayer = player;
        subscribedJourneyLog = subscribedPlayer != null ? subscribedPlayer.GetComponent<PlayerTransitJourneyLog>() : null;
        if(subscribedJourneyLog != null) {
            subscribedJourneyLog.OnTransitJourneyChanged += HandleLogChanged;
        }
    }

    void UnsubscribeFromLog() {
        if(subscribedJourneyLog != null) {
            subscribedJourneyLog.OnTransitJourneyChanged -= HandleLogChanged;
        }

        subscribedPlayer = null;
        subscribedJourneyLog = null;
    }

    void HandleLogChanged() {
        Refresh();
    }

    bool Succeed(TransitJourneyUIActionKind kind, string message, out string feedback) {
        feedback = message;
        SetLastResult(kind, true, message);
        if(logSuccessfulActions) {
            GameDebug.Success(message, GameDebugCategory.Transit, this, "TransitJourneyUIManager");
        }
        return true;
    }

    bool Block(string message, out string feedback) {
        feedback = string.IsNullOrWhiteSpace(message) ? "Transit journey action was blocked." : message;
        SetLastResult(TransitJourneyUIActionKind.Blocked, false, feedback);
        if(logBlockedActions) {
            GameDebug.Warning(feedback, GameDebugCategory.Transit, this, "TransitJourneyUIManager");
        }
        return false;
    }

    void SetLastResult(TransitJourneyUIActionKind kind, bool success, string message) {
        lastResult = new TransitJourneyUIActionResult {
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
}

public interface ITransitJourneyFilterable {
    bool MatchesSearch(string search);
    bool HasTag(string tag);
}

[Serializable]
public class TransitJourneyUIScreenSnapshot {
    [Tooltip("If enabled, a player was resolved for this snapshot.")]
    public bool hasPlayer;
    [Tooltip("Resolved player object name.")]
    public string playerName;
    [Tooltip("If enabled, PlayerTransitLog was found on the player.")]
    public bool hasTransitLog;
    [Tooltip("If enabled, PlayerTransitJourneyLog was found on the player.")]
    public bool hasJourneyLog;
    [Tooltip("If enabled, the player has an active transit journey.")]
    public bool hasActiveJourney;
    [Tooltip("Source id used by UI backend actions.")]
    public string sourceId;
    [Tooltip("Origin stop id used when evaluating journey options.")]
    public string originStopId;
    [Tooltip("Current in-game day.")]
    public int day;
    [Tooltip("Current in-game hour.")]
    public int hour;
    [Tooltip("Current absolute in-game hour.")]
    public int absoluteHour;
    [Tooltip("Visible journey option count.")]
    public int journeyCount;
    [Tooltip("Visible journeys that can start right now.")]
    public int availableJourneyCount;
    [Tooltip("Visible journeys blocked by access/cost/origin rules.")]
    public int blockedJourneyCount;
    [Tooltip("Visible journey history count.")]
    public int historyCount;
    [Tooltip("Visible onboard activity count.")]
    public int onboardActivityCount;
    [Tooltip("Active transit journey row.")]
    public TransitJourneyActiveRow activeJourney;
    [Tooltip("Visible journey option rows.")]
    public List<TransitJourneyOptionRow> journeyRows = new List<TransitJourneyOptionRow>();
    [Tooltip("Visible leg rows for the active or primary journey.")]
    public List<TransitJourneyLegRow> legRows = new List<TransitJourneyLegRow>();
    [Tooltip("Recent completed/cancelled journey rows.")]
    public List<TransitJourneyHistoryRow> historyRows = new List<TransitJourneyHistoryRow>();
    [Tooltip("Recent onboard activity rows.")]
    public List<TransitOnboardActivityRow> onboardActivityRows = new List<TransitOnboardActivityRow>();
    [Tooltip("Most recent UI backend action result.")]
    public TransitJourneyUIActionResult lastResult;
}

[Serializable]
public class TransitJourneyUIActionResult {
    [Tooltip("Kind of UI backend action that produced this result.")]
    public TransitJourneyUIActionKind kind;
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
public class TransitJourneyOptionRow : ITransitJourneyFilterable {
    [Tooltip("Journey definition id.")]
    public string journeyId;
    [Tooltip("Display name shown by UI.")]
    public string displayName;
    [Tooltip("Description shown by UI.")]
    public string description;
    [Tooltip("Broad journey vehicle type.")]
    public TransitRouteType journeyType;
    [Tooltip("Free-form journey tags.")]
    public List<string> tags = new List<string>();
    [Tooltip("If enabled, this journey can start right now.")]
    public bool canStart;
    [Tooltip("If enabled, this journey uses a vehicle interior scene hint.")]
    public bool useVehicleInterior;
    [Tooltip("Vehicle interior scene name, if configured.")]
    public string vehicleInteriorSceneName;
    [Tooltip("Vehicle interior spawn id, if configured.")]
    public string vehicleInteriorSpawnId;
    [Tooltip("Number of legs in this journey.")]
    public int legCount;
    [Tooltip("Total estimated travel hours across all legs.")]
    public int totalTravelHours;
    [Tooltip("Total dwell hours across all stops.")]
    public int totalDwellHours;
    [Tooltip("First origin stop id.")]
    public string originStopId;
    [Tooltip("Final destination stop id.")]
    public string finalStopId;
    [Tooltip("Final destination display name.")]
    public string finalStopName;
    [Tooltip("Failure reason shown when Can Start is false.")]
    public string failureMessage;
    [Tooltip("Short text useful for placeholder UI rows.")]
    public string displayText;

    public bool MatchesSearch(string search) {
        return SearchUtility(search, journeyId, displayName, description, journeyType.ToString(), originStopId, finalStopId, finalStopName, displayText);
    }

    public bool HasTag(string tag) {
        return HasTagUtility(tags, tag);
    }

    public static TransitJourneyOptionRow FromDefinition(TransitJourneyDefinition journey, PlayerController player, PlayerTransitLog log, string originStopId) {
        string failure = "Journey could not be resolved.";
        bool canStart = journey != null && journey.CanStart(player, log, originStopId, out failure);
        var legs = journey?.Legs?.Where(leg => leg != null).ToList() ?? new List<TransitJourneyLeg>();
        var first = legs.FirstOrDefault();
        var last = legs.LastOrDefault();
        return new TransitJourneyOptionRow {
            journeyId = journey != null ? journey.Id : string.Empty,
            displayName = journey != null ? journey.DisplayName : string.Empty,
            description = journey != null ? journey.Description : string.Empty,
            journeyType = journey != null ? journey.JourneyType : TransitRouteType.Special,
            tags = journey != null ? journey.Tags.ToList() : new List<string>(),
            canStart = canStart,
            useVehicleInterior = journey != null && journey.UseVehicleInterior,
            vehicleInteriorSceneName = journey != null ? journey.VehicleInteriorSceneName : string.Empty,
            vehicleInteriorSpawnId = journey != null ? journey.VehicleInteriorSpawnId : string.Empty,
            legCount = legs.Count,
            totalTravelHours = legs.Sum(leg => Mathf.Max(0, leg.TravelHours)),
            totalDwellHours = legs.Sum(leg => Mathf.Max(0, leg.DwellHours)),
            originStopId = first != null ? first.OriginStopId : string.Empty,
            finalStopId = last != null ? last.DestinationStopId : string.Empty,
            finalStopName = last != null ? last.DestinationDisplayName : string.Empty,
            failureMessage = canStart ? string.Empty : failure,
            displayText = journey != null ? $"{journey.DisplayName} - {(canStart ? "available" : "blocked")}" : string.Empty
        };
    }

    static bool SearchUtility(string search, params string[] values) {
        if(string.IsNullOrWhiteSpace(search)) return true;
        return values.Any(value => !string.IsNullOrWhiteSpace(value) && value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    static bool HasTagUtility(IEnumerable<string> values, string tag) {
        return !string.IsNullOrWhiteSpace(tag) && values != null && values.Any(value => string.Equals(value, tag, StringComparison.OrdinalIgnoreCase));
    }
}

[Serializable]
public class TransitJourneyActiveRow : ITransitJourneyFilterable {
    [Tooltip("Active journey id.")]
    public string journeyId;
    [Tooltip("Active journey display name.")]
    public string journeyName;
    [Tooltip("Current route id.")]
    public string routeId;
    [Tooltip("Current route display name.")]
    public string routeName;
    [Tooltip("Current journey phase.")]
    public TransitJourneyPhase phase;
    [Tooltip("Current leg index.")]
    public int currentLegIndex;
    [Tooltip("Current stop id or last reached stop id.")]
    public string currentStopId;
    [Tooltip("Current leg origin stop id.")]
    public string originStopId;
    [Tooltip("Current leg destination stop id.")]
    public string destinationStopId;
    [Tooltip("Current leg destination display name.")]
    public string destinationDisplayName;
    [Tooltip("Remaining travel hours before reaching the next stop.")]
    public int remainingTravelHours;
    [Tooltip("Remaining dwell hours before the vehicle can auto-continue.")]
    public int remainingDwellHours;
    [Tooltip("Total in-game hours spent on this journey.")]
    public int totalHoursSpent;
    [Tooltip("If enabled, the player can disembark at the current stop.")]
    public bool canDisembark;
    [Tooltip("If enabled, the player can continue to the next leg.")]
    public bool canContinue;
    [Tooltip("If enabled, the journey can auto-continue after dwell time.")]
    public bool autoContinueAfterDwell;
    [Tooltip("Short text useful for placeholder UI rows.")]
    public string displayText;

    public bool MatchesSearch(string search) {
        return string.IsNullOrWhiteSpace(search) || new[] { journeyId, journeyName, routeId, routeName, currentStopId, originStopId, destinationStopId, destinationDisplayName, displayText }
            .Any(value => !string.IsNullOrWhiteSpace(value) && value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    public bool HasTag(string tag) {
        return false;
    }

    public static TransitJourneyActiveRow FromState(PlayerTransitJourneyState state) {
        if(state == null) {
            return null;
        }

        return new TransitJourneyActiveRow {
            journeyId = state.journeyId,
            journeyName = state.journeyName,
            routeId = state.routeId,
            routeName = state.routeName,
            phase = state.phase,
            currentLegIndex = state.currentLegIndex,
            currentStopId = state.currentStopId,
            originStopId = state.originStopId,
            destinationStopId = state.destinationStopId,
            destinationDisplayName = state.destinationDisplayName,
            remainingTravelHours = state.remainingTravelHours,
            remainingDwellHours = state.remainingDwellHours,
            totalHoursSpent = state.totalHoursSpent,
            canDisembark = state.canDisembark,
            canContinue = state.canContinue,
            autoContinueAfterDwell = state.autoContinueAfterDwell,
            displayText = $"{state.journeyName} - {state.phase}"
        };
    }
}

[Serializable]
public class TransitJourneyLegRow : ITransitJourneyFilterable {
    [Tooltip("Journey id this leg belongs to.")]
    public string journeyId;
    [Tooltip("Journey display name this leg belongs to.")]
    public string journeyName;
    [Tooltip("Leg index inside the journey.")]
    public int legIndex;
    [Tooltip("Route id used by this leg.")]
    public string routeId;
    [Tooltip("Route display name used by this leg.")]
    public string routeName;
    [Tooltip("Origin stop id for this leg.")]
    public string originStopId;
    [Tooltip("Destination stop id for this leg.")]
    public string destinationStopId;
    [Tooltip("Destination stop display name for this leg.")]
    public string destinationDisplayName;
    [Tooltip("How this stop behaves when reached.")]
    public TransitJourneyStopRule stopRule;
    [Tooltip("Travel hours for this leg.")]
    public int travelHours;
    [Tooltip("Dwell hours for this stop.")]
    public int dwellHours;
    [Tooltip("If enabled, the player can leave at this leg destination.")]
    public bool canDisembark;
    [Tooltip("Short text useful for placeholder UI rows.")]
    public string displayText;

    public bool MatchesSearch(string search) {
        return string.IsNullOrWhiteSpace(search) || new[] { journeyId, journeyName, routeId, routeName, originStopId, destinationStopId, destinationDisplayName, stopRule.ToString(), displayText }
            .Any(value => !string.IsNullOrWhiteSpace(value) && value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    public bool HasTag(string tag) {
        return false;
    }

    public static TransitJourneyLegRow FromLeg(TransitJourneyDefinition journey, TransitJourneyLeg leg, int index) {
        return new TransitJourneyLegRow {
            journeyId = journey != null ? journey.Id : string.Empty,
            journeyName = journey != null ? journey.DisplayName : string.Empty,
            legIndex = index,
            routeId = leg?.Route != null ? leg.Route.Id : string.Empty,
            routeName = leg?.Route != null ? leg.Route.DisplayName : string.Empty,
            originStopId = leg != null ? leg.OriginStopId : string.Empty,
            destinationStopId = leg != null ? leg.DestinationStopId : string.Empty,
            destinationDisplayName = leg != null ? leg.DestinationDisplayName : string.Empty,
            stopRule = leg != null ? leg.StopRule : TransitJourneyStopRule.PassThrough,
            travelHours = leg != null ? leg.TravelHours : 0,
            dwellHours = leg != null ? leg.DwellHours : 0,
            canDisembark = leg != null && leg.CanDisembark,
            displayText = leg != null ? $"{index + 1}. {leg.OriginStopId} -> {leg.DestinationDisplayName}" : string.Empty
        };
    }
}

[Serializable]
public class TransitJourneyHistoryRow : ITransitJourneyFilterable {
    [Tooltip("Journey id.")]
    public string journeyId;
    [Tooltip("Journey display name.")]
    public string journeyName;
    [Tooltip("Final stop id.")]
    public string finalStopId;
    [Tooltip("Final stop display name.")]
    public string finalStopName;
    [Tooltip("Final journey phase.")]
    public TransitJourneyPhase finalPhase;
    [Tooltip("If enabled, the player manually disembarked.")]
    public bool disembarked;
    [Tooltip("If enabled, the journey was cancelled.")]
    public bool cancelled;
    [Tooltip("Recorded cancel/block reason.")]
    public string reason;
    [Tooltip("Total in-game hours spent.")]
    public int totalHoursSpent;
    [Tooltip("Absolute in-game hour when ended.")]
    public int endedAbsoluteHour;
    [Tooltip("Short text useful for placeholder UI rows.")]
    public string displayText;

    public bool MatchesSearch(string search) {
        return string.IsNullOrWhiteSpace(search) || new[] { journeyId, journeyName, finalStopId, finalStopName, finalPhase.ToString(), reason, displayText }
            .Any(value => !string.IsNullOrWhiteSpace(value) && value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    public bool HasTag(string tag) {
        return false;
    }

    public static TransitJourneyHistoryRow FromRecord(PlayerTransitJourneyHistoryRecord record) {
        return new TransitJourneyHistoryRow {
            journeyId = record != null ? record.journeyId : string.Empty,
            journeyName = record != null ? record.journeyName : string.Empty,
            finalStopId = record != null ? record.finalStopId : string.Empty,
            finalStopName = record != null ? record.finalStopName : string.Empty,
            finalPhase = record != null ? record.finalPhase : TransitJourneyPhase.None,
            disembarked = record != null && record.disembarked,
            cancelled = record != null && record.cancelled,
            reason = record != null ? record.reason : string.Empty,
            totalHoursSpent = record != null ? record.totalHoursSpent : 0,
            endedAbsoluteHour = record != null ? record.endedAbsoluteHour : -1,
            displayText = record != null ? $"{record.journeyName} -> {record.finalStopName}" : string.Empty
        };
    }
}

[Serializable]
public class TransitOnboardActivityRow : ITransitJourneyFilterable {
    [Tooltip("Journey id active when recorded.")]
    public string journeyId;
    [Tooltip("Journey display name active when recorded.")]
    public string journeyName;
    [Tooltip("Route id active when recorded.")]
    public string routeId;
    [Tooltip("Route display name active when recorded.")]
    public string routeName;
    [Tooltip("Activity id such as sleep, research, talk or wait.")]
    public string activityId;
    [Tooltip("Activity display name.")]
    public string displayName;
    [Tooltip("In-game hours spent.")]
    public int hoursSpent;
    [Tooltip("Absolute in-game hour when recorded.")]
    public int absoluteHour;
    [Tooltip("Source id that recorded this activity.")]
    public string sourceId;
    [Tooltip("Short text useful for placeholder UI rows.")]
    public string displayText;

    public bool MatchesSearch(string search) {
        return string.IsNullOrWhiteSpace(search) || new[] { journeyId, journeyName, routeId, routeName, activityId, displayName, sourceId, displayText }
            .Any(value => !string.IsNullOrWhiteSpace(value) && value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    public bool HasTag(string tag) {
        return false;
    }

    public static TransitOnboardActivityRow FromRecord(PlayerTransitOnboardActivityRecord record) {
        return new TransitOnboardActivityRow {
            journeyId = record != null ? record.journeyId : string.Empty,
            journeyName = record != null ? record.journeyName : string.Empty,
            routeId = record != null ? record.routeId : string.Empty,
            routeName = record != null ? record.routeName : string.Empty,
            activityId = record != null ? record.activityId : string.Empty,
            displayName = record != null ? record.displayName : string.Empty,
            hoursSpent = record != null ? record.hoursSpent : 0,
            absoluteHour = record != null ? record.absoluteHour : -1,
            sourceId = record != null ? record.sourceId : string.Empty,
            displayText = record != null ? $"{record.displayName} ({record.hoursSpent}h)" : string.Empty
        };
    }
}
