using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum CompetitionBracketRankingTab {
    Rankings,
    Brackets,
    Seasons,
    MatchHistory
}

public class CompetitionBracketRankingUIManager : MonoBehaviour {
    [Header("Player")]
    [Tooltip("Player whose competition ranking, season and bracket records are shown. Empty uses PlayerController.i or the first PlayerController in the scene.")]
    [SerializeField] PlayerController playerOverride = null;

    [Header("Definition Pools")]
    [Tooltip("Ranking tracks explicitly shown by this UI backend. Empty can still read Resources when Include Resource Rankings is enabled.")]
    [SerializeField] List<CompetitionRankingDefinition> rankingPool = new List<CompetitionRankingDefinition>();
    [Tooltip("If enabled, all CompetitionRankingDefinition assets in Resources are added to the ranking pool.")]
    [SerializeField] bool includeResourceRankings = true;
    [Tooltip("Roster/bracket definitions explicitly shown by this UI backend. Empty can still read Resources when Include Resource Rosters is enabled.")]
    [SerializeField] List<CompetitionRosterDefinition> rosterPool = new List<CompetitionRosterDefinition>();
    [Tooltip("If enabled, all CompetitionRosterDefinition assets in Resources are added to the roster pool.")]
    [SerializeField] bool includeResourceRosters = true;
    [Tooltip("Season definitions explicitly shown by this UI backend. Empty can still read Resources when Include Resource Seasons is enabled.")]
    [SerializeField] List<CompetitionSeasonDefinition> seasonPool = new List<CompetitionSeasonDefinition>();
    [Tooltip("If enabled, all CompetitionSeasonDefinition assets in Resources are added to the season pool.")]
    [SerializeField] bool includeResourceSeasons = true;

    [Header("Visibility")]
    [Tooltip("Current logical tab the UI can use to decide which snapshot rows to render first.")]
    [SerializeField] CompetitionBracketRankingTab activeTab = CompetitionBracketRankingTab.Rankings;
    [Tooltip("Optional lowercase/uppercase-insensitive text filter applied to ranking, bracket, season and match rows.")]
    [SerializeField] string searchText = string.Empty;
    [Tooltip("Optional tag filter. Rows remain visible when their definition or saved state contains this tag.")]
    [SerializeField] string tagFilter = string.Empty;
    [Tooltip("Optional region id filter for ranking and season rows.")]
    [SerializeField] string worldRegionFilter = string.Empty;
    [Tooltip("If enabled, locked or unavailable ranking tracks are still shown with failure text.")]
    [SerializeField] bool includeLockedRankings = true;
    [Tooltip("If enabled, rosters that cannot currently generate a bracket are still shown with failure text.")]
    [SerializeField] bool includeBlockedRosters = true;
    [Tooltip("If enabled, seasons that cannot currently start are still shown with failure text.")]
    [SerializeField] bool includeBlockedSeasons = true;
    [Tooltip("If enabled, completed and abandoned brackets are included, not only active brackets.")]
    [SerializeField] bool includeInactiveBrackets = true;
    [Tooltip("If enabled, match rows from all bracket states are included. If disabled, only active/current bracket match rows are included.")]
    [SerializeField] bool includeMatchHistory = true;
    [Tooltip("If enabled, only rows connected to the selected ranking id are included when a ranking is selected.")]
    [SerializeField] bool filterBySelectedRanking;
    [Tooltip("Maximum ranking rows copied into the snapshot. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxRankingRows = 30;
    [Tooltip("Maximum bracket rows copied into the snapshot. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxBracketRows = 20;
    [Tooltip("Maximum season rows copied into the snapshot. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxSeasonRows = 20;
    [Tooltip("Maximum match rows copied into the snapshot. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxMatchRows = 40;
    [Tooltip("Maximum point history rows copied into each ranking row. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxPointHistoryRowsPerRanking = 5;

    [Header("Selection")]
    [Tooltip("Selected ranking id used by optional filtering and detail panes.")]
    [SerializeField] string selectedRankingId = string.Empty;
    [Tooltip("Selected roster id used by optional detail panes.")]
    [SerializeField] string selectedRosterId = string.Empty;
    [Tooltip("Selected season id used by optional detail panes.")]
    [SerializeField] string selectedSeasonId = string.Empty;
    [Tooltip("Selected bracket roster id used by optional match detail panes.")]
    [SerializeField] string selectedBracketRosterId = string.Empty;
    [Tooltip("Selected match id used by optional detail panes.")]
    [SerializeField] string selectedMatchId = string.Empty;

    [Header("Snapshot")]
    [Tooltip("If enabled, Refresh is called when this component starts.")]
    [SerializeField] bool refreshOnStart = true;
    [Tooltip("If enabled, this manager subscribes to player competition log change events while active.")]
    [SerializeField] bool refreshWhenLogsChange = true;

    [Header("Debug")]
    [Tooltip("If enabled, Refresh writes a short success message to GameDebug.")]
    [SerializeField] bool logRefresh;

    CompetitionBracketRankingUIScreenSnapshot currentSnapshot = new CompetitionBracketRankingUIScreenSnapshot();
    PlayerController subscribedPlayer;
    PlayerCompetitionRankingLog subscribedRankingLog;
    PlayerCompetitionBracketLog subscribedBracketLog;
    PlayerCompetitionSeasonLog subscribedSeasonLog;

    public CompetitionBracketRankingUIScreenSnapshot CurrentSnapshot => currentSnapshot;
    public PlayerController PlayerOverride => playerOverride;
    public IReadOnlyList<CompetitionRankingDefinition> RankingPool => rankingPool;
    public bool IncludeResourceRankings => includeResourceRankings;
    public IReadOnlyList<CompetitionRosterDefinition> RosterPool => rosterPool;
    public bool IncludeResourceRosters => includeResourceRosters;
    public IReadOnlyList<CompetitionSeasonDefinition> SeasonPool => seasonPool;
    public bool IncludeResourceSeasons => includeResourceSeasons;
    public CompetitionBracketRankingTab ActiveTab => activeTab;
    public string SearchText => searchText;
    public string TagFilter => tagFilter;
    public string WorldRegionFilter => worldRegionFilter;
    public bool IncludeLockedRankings => includeLockedRankings;
    public bool IncludeBlockedRosters => includeBlockedRosters;
    public bool IncludeBlockedSeasons => includeBlockedSeasons;
    public bool IncludeInactiveBrackets => includeInactiveBrackets;
    public bool IncludeMatchHistory => includeMatchHistory;
    public bool FilterBySelectedRanking => filterBySelectedRanking;
    public string SelectedRankingId => selectedRankingId;
    public string SelectedRosterId => selectedRosterId;
    public string SelectedSeasonId => selectedSeasonId;
    public string SelectedBracketRosterId => selectedBracketRosterId;
    public string SelectedMatchId => selectedMatchId;
    public event Action<CompetitionBracketRankingUIScreenSnapshot> OnSnapshotChanged;

    void OnEnable() {
        SubscribeToLogs();
    }

    void Start() {
        if(refreshOnStart) {
            Refresh();
        }
    }

    void OnDisable() {
        UnsubscribeFromLogs();
    }

    [ContextMenu("Refresh Competition Bracket Ranking Snapshot")]
    public CompetitionBracketRankingUIScreenSnapshot RefreshFromContextMenu() {
        return Refresh();
    }

    public CompetitionBracketRankingUIScreenSnapshot Refresh() {
        SubscribeToLogs();

        var player = ResolvePlayer();
        var rankingLog = player != null ? player.GetComponent<PlayerCompetitionRankingLog>() : null;
        var bracketLog = player != null ? player.GetComponent<PlayerCompetitionBracketLog>() : null;
        var seasonLog = player != null ? player.GetComponent<PlayerCompetitionSeasonLog>() : null;

        var rankingRows = BuildRankingRows(player, rankingLog).ToList();
        var bracketRows = BuildBracketRows(player, bracketLog).ToList();
        var seasonRows = BuildSeasonRows(player, seasonLog).ToList();
        var matchRows = BuildMatchRows(bracketLog).ToList();

        currentSnapshot = new CompetitionBracketRankingUIScreenSnapshot {
            hasPlayer = player != null,
            playerName = player != null ? player.name : string.Empty,
            hasRankingLog = rankingLog != null,
            hasBracketLog = bracketLog != null,
            hasSeasonLog = seasonLog != null,
            activeTab = activeTab,
            searchText = searchText,
            tagFilter = tagFilter,
            worldRegionFilter = worldRegionFilter,
            selectedRankingId = selectedRankingId,
            selectedRosterId = selectedRosterId,
            selectedSeasonId = selectedSeasonId,
            selectedBracketRosterId = selectedBracketRosterId,
            selectedMatchId = selectedMatchId,
            day = TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1,
            hour = TimeSystem.i != null ? Mathf.Clamp(TimeSystem.i.Hour, 0, 23) : 0,
            absoluteHour = GetCurrentAbsoluteHour(),
            rankingCount = rankingRows.Count,
            unlockedRankingCount = rankingRows.Count(row => row != null && row.unlocked),
            blockedRankingCount = rankingRows.Count(row => row != null && !row.canScore),
            activeBracketCount = bracketRows.Count(row => row != null && row.active),
            completedBracketCount = bracketRows.Count(row => row != null && row.completed),
            activeSeasonCount = seasonRows.Count(row => row != null && row.active),
            availableSeasonCount = seasonRows.Count(row => row != null && row.canStart),
            pendingMatchCount = matchRows.Count(row => row != null && !row.completed),
            playerMatchCount = matchRows.Count(row => row != null && row.playerInMatch),
            rankingRows = rankingRows,
            bracketRows = bracketRows,
            seasonRows = seasonRows,
            matchRows = matchRows,
            selectedRanking = rankingRows.FirstOrDefault(row => row != null && MatchesId(row.rankingId, selectedRankingId)),
            selectedBracket = bracketRows.FirstOrDefault(row => row != null && MatchesId(row.rosterId, selectedBracketRosterId)),
            selectedSeason = seasonRows.FirstOrDefault(row => row != null && MatchesId(row.seasonId, selectedSeasonId)),
            selectedMatch = matchRows.FirstOrDefault(row => row != null && MatchesId(row.matchId, selectedMatchId))
        };

        if(logRefresh) {
            GameDebug.Success($"Competition bracket/ranking snapshot refreshed: {rankingRows.Count} rankings, {bracketRows.Count} brackets, {matchRows.Count} matches.", GameDebugCategory.BattleRule, this, "CompetitionBracketRankingUIManager");
        }

        OnSnapshotChanged?.Invoke(currentSnapshot);
        return currentSnapshot;
    }

    public void SetActiveTab(CompetitionBracketRankingTab tab) {
        activeTab = tab;
        Refresh();
    }

    public void SetSearchText(string value) {
        searchText = value ?? string.Empty;
        Refresh();
    }

    public void SetTagFilter(string value) {
        tagFilter = value ?? string.Empty;
        Refresh();
    }

    public void SetWorldRegionFilter(string value) {
        worldRegionFilter = value ?? string.Empty;
        Refresh();
    }

    public void SelectRanking(string rankingId) {
        selectedRankingId = rankingId ?? string.Empty;
        activeTab = CompetitionBracketRankingTab.Rankings;
        Refresh();
    }

    public void SelectRoster(string rosterId) {
        selectedRosterId = rosterId ?? string.Empty;
        selectedBracketRosterId = rosterId ?? string.Empty;
        activeTab = CompetitionBracketRankingTab.Brackets;
        Refresh();
    }

    public void SelectSeason(string seasonId) {
        selectedSeasonId = seasonId ?? string.Empty;
        activeTab = CompetitionBracketRankingTab.Seasons;
        Refresh();
    }

    public void SelectMatch(string matchId) {
        selectedMatchId = matchId ?? string.Empty;
        activeTab = CompetitionBracketRankingTab.MatchHistory;
        Refresh();
    }

    public void ClearSelection() {
        selectedRankingId = string.Empty;
        selectedRosterId = string.Empty;
        selectedSeasonId = string.Empty;
        selectedBracketRosterId = string.Empty;
        selectedMatchId = string.Empty;
        Refresh();
    }

    IEnumerable<CompetitionRankingUIRow> BuildRankingRows(PlayerController player, PlayerCompetitionRankingLog rankingLog) {
        var definitions = ResolveRankingPool().Where(ranking => ranking != null).ToList();
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var rows = new List<CompetitionRankingUIRow>();
        foreach(var ranking in definitions) {
            if(!seenIds.Add(ranking.Id)) {
                continue;
            }

            var row = CompetitionRankingUIRow.FromDefinition(ranking, player, rankingLog, maxPointHistoryRowsPerRanking);
            if(RowPassesCommonFilters(row) && (includeLockedRankings || row.canScore)) {
                rows.Add(row);
            }
        }

        if(rankingLog != null) {
            foreach(var state in rankingLog.RankingStates.Where(state => state != null && seenIds.Add(state.rankingId))) {
                var row = CompetitionRankingUIRow.FromStateOnly(state, maxPointHistoryRowsPerRanking);
                if(RowPassesCommonFilters(row) && (includeLockedRankings || row.canScore)) {
                    rows.Add(row);
                }
            }
        }

        return LimitRows(rows
            .OrderByDescending(row => row.unlocked)
            .ThenByDescending(row => row.currentPoints)
            .ThenBy(row => row.scope)
            .ThenBy(row => row.displayName), maxRankingRows);
    }

    IEnumerable<CompetitionBracketUIRow> BuildBracketRows(PlayerController player, PlayerCompetitionBracketLog bracketLog) {
        var rows = new List<CompetitionBracketUIRow>();
        var seenRosterIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if(bracketLog != null) {
            foreach(var state in bracketLog.BracketStates.Where(state => state != null)) {
                if(!includeInactiveBrackets && !state.active) {
                    continue;
                }

                var row = CompetitionBracketUIRow.FromState(state);
                if(RowPassesCommonFilters(row) && RowPassesSelectedRanking(row)) {
                    rows.Add(row);
                    seenRosterIds.Add(state.rosterId);
                }
            }
        }

        foreach(var roster in ResolveRosterPool().Where(roster => roster != null)) {
            if(!seenRosterIds.Add(roster.Id)) {
                continue;
            }

            var row = CompetitionBracketUIRow.FromRoster(roster, player, bracketLog);
            if(RowPassesCommonFilters(row) && RowPassesSelectedRanking(row) && (includeBlockedRosters || row.canGenerate)) {
                rows.Add(row);
            }
        }

        return LimitRows(rows
            .OrderByDescending(row => row.active)
            .ThenByDescending(row => row.completed)
            .ThenByDescending(row => row.generatedTotalHour)
            .ThenBy(row => row.competitionName)
            .ThenBy(row => row.displayName), maxBracketRows);
    }

    IEnumerable<CompetitionSeasonUIRow> BuildSeasonRows(PlayerController player, PlayerCompetitionSeasonLog seasonLog) {
        var definitions = ResolveSeasonPool().Where(season => season != null).ToList();
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var rows = new List<CompetitionSeasonUIRow>();

        foreach(var season in definitions) {
            if(!seenIds.Add(season.Id)) {
                continue;
            }

            var row = CompetitionSeasonUIRow.FromDefinition(season, player, seasonLog);
            if(RowPassesCommonFilters(row) && (includeBlockedSeasons || row.canStart)) {
                rows.Add(row);
            }
        }

        if(seasonLog != null) {
            foreach(var state in seasonLog.SeasonStates.Where(state => state != null && seenIds.Add(state.seasonId))) {
                var row = CompetitionSeasonUIRow.FromStateOnly(state);
                if(RowPassesCommonFilters(row) && (includeBlockedSeasons || row.canStart)) {
                    rows.Add(row);
                }
            }
        }

        return LimitRows(rows
            .OrderByDescending(row => row.active)
            .ThenByDescending(row => row.canStart)
            .ThenBy(row => row.kind)
            .ThenBy(row => row.displayName), maxSeasonRows);
    }

    IEnumerable<CompetitionMatchUIRow> BuildMatchRows(PlayerCompetitionBracketLog bracketLog) {
        if(bracketLog == null) {
            return Enumerable.Empty<CompetitionMatchUIRow>();
        }

        var rows = bracketLog.BracketStates
            .Where(state => state != null && (includeMatchHistory || state.active))
            .Where(state => includeInactiveBrackets || state.active)
            .Where(state => !filterBySelectedRanking || string.IsNullOrWhiteSpace(selectedRankingId) || MatchesId(state.rankingId, selectedRankingId))
            .SelectMany(state => state.Rounds.SelectMany(round => round.Matches.Select(match => CompetitionMatchUIRow.FromMatch(state, round, match))))
            .Where(row => row != null && RowPassesCommonFilters(row))
            .OrderByDescending(row => row.bracketActive)
            .ThenBy(row => row.completed)
            .ThenByDescending(row => row.completedTotalHour)
            .ThenBy(row => row.roundIndex)
            .ThenBy(row => row.matchIndex);

        return LimitRows(rows, maxMatchRows);
    }

    IEnumerable<CompetitionRankingDefinition> ResolveRankingPool() {
        return MergeDefinitions(rankingPool, includeResourceRankings ? Resources.LoadAll<CompetitionRankingDefinition>("") : Array.Empty<CompetitionRankingDefinition>(), ranking => ranking.Id);
    }

    IEnumerable<CompetitionRosterDefinition> ResolveRosterPool() {
        return MergeDefinitions(rosterPool, includeResourceRosters ? Resources.LoadAll<CompetitionRosterDefinition>("") : Array.Empty<CompetitionRosterDefinition>(), roster => roster.Id);
    }

    IEnumerable<CompetitionSeasonDefinition> ResolveSeasonPool() {
        return MergeDefinitions(seasonPool, includeResourceSeasons ? Resources.LoadAll<CompetitionSeasonDefinition>("") : Array.Empty<CompetitionSeasonDefinition>(), season => season.Id);
    }

    IEnumerable<T> MergeDefinitions<T>(IEnumerable<T> explicitItems, IEnumerable<T> resourceItems, Func<T, string> idSelector) where T : UnityEngine.Object {
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach(var item in explicitItems ?? Enumerable.Empty<T>()) {
            if(item == null) {
                continue;
            }

            string id = idSelector(item);
            if(seenIds.Add(string.IsNullOrWhiteSpace(id) ? item.name : id)) {
                yield return item;
            }
        }

        foreach(var item in resourceItems ?? Enumerable.Empty<T>()) {
            if(item == null) {
                continue;
            }

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

    bool RowPassesCommonFilters(ICompetitionBracketRankingFilterable row) {
        if(row == null) {
            return false;
        }

        if(!string.IsNullOrWhiteSpace(searchText) && !row.MatchesSearch(searchText)) {
            return false;
        }

        if(!string.IsNullOrWhiteSpace(tagFilter) && !row.HasTag(tagFilter)) {
            return false;
        }

        if(!string.IsNullOrWhiteSpace(worldRegionFilter) && !string.IsNullOrWhiteSpace(row.WorldRegionId) && !MatchesId(row.WorldRegionId, worldRegionFilter)) {
            return false;
        }

        return true;
    }

    bool RowPassesSelectedRanking(ICompetitionRankingLinkedRow row) {
        return !filterBySelectedRanking
            || string.IsNullOrWhiteSpace(selectedRankingId)
            || MatchesId(row?.RankingId, selectedRankingId);
    }

    void SubscribeToLogs() {
        if(!refreshWhenLogsChange) {
            return;
        }

        var player = ResolvePlayer();
        if(player == subscribedPlayer) {
            return;
        }

        UnsubscribeFromLogs();
        subscribedPlayer = player;
        if(subscribedPlayer == null) {
            return;
        }

        subscribedRankingLog = subscribedPlayer.GetComponent<PlayerCompetitionRankingLog>();
        subscribedBracketLog = subscribedPlayer.GetComponent<PlayerCompetitionBracketLog>();
        subscribedSeasonLog = subscribedPlayer.GetComponent<PlayerCompetitionSeasonLog>();

        if(subscribedRankingLog != null) subscribedRankingLog.OnCompetitionRankingChanged += HandleLogChanged;
        if(subscribedBracketLog != null) subscribedBracketLog.OnCompetitionBracketLogChanged += HandleLogChanged;
        if(subscribedSeasonLog != null) subscribedSeasonLog.OnCompetitionSeasonLogChanged += HandleLogChanged;
    }

    void UnsubscribeFromLogs() {
        if(subscribedRankingLog != null) subscribedRankingLog.OnCompetitionRankingChanged -= HandleLogChanged;
        if(subscribedBracketLog != null) subscribedBracketLog.OnCompetitionBracketLogChanged -= HandleLogChanged;
        if(subscribedSeasonLog != null) subscribedSeasonLog.OnCompetitionSeasonLogChanged -= HandleLogChanged;

        subscribedPlayer = null;
        subscribedRankingLog = null;
        subscribedBracketLog = null;
        subscribedSeasonLog = null;
    }

    void HandleLogChanged() {
        Refresh();
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

    static bool MatchesId(string value, string candidate) {
        return !string.IsNullOrWhiteSpace(value)
            && !string.IsNullOrWhiteSpace(candidate)
            && string.Equals(value, candidate, StringComparison.OrdinalIgnoreCase);
    }
}

public interface ICompetitionBracketRankingFilterable {
    string WorldRegionId { get; }
    bool MatchesSearch(string search);
    bool HasTag(string tag);
}

public interface ICompetitionRankingLinkedRow {
    string RankingId { get; }
}

[Serializable]
public class CompetitionBracketRankingUIScreenSnapshot {
    [Tooltip("If enabled, a player was resolved for this snapshot.")]
    public bool hasPlayer;
    [Tooltip("Resolved player object name.")]
    public string playerName;
    [Tooltip("If enabled, PlayerCompetitionRankingLog was found on the player.")]
    public bool hasRankingLog;
    [Tooltip("If enabled, PlayerCompetitionBracketLog was found on the player.")]
    public bool hasBracketLog;
    [Tooltip("If enabled, PlayerCompetitionSeasonLog was found on the player.")]
    public bool hasSeasonLog;
    [Tooltip("Current logical tab selected by the backend.")]
    public CompetitionBracketRankingTab activeTab;
    [Tooltip("Current text filter copied from the manager.")]
    public string searchText;
    [Tooltip("Current tag filter copied from the manager.")]
    public string tagFilter;
    [Tooltip("Current world region id filter copied from the manager.")]
    public string worldRegionFilter;
    [Tooltip("Selected ranking id copied from the manager.")]
    public string selectedRankingId;
    [Tooltip("Selected roster id copied from the manager.")]
    public string selectedRosterId;
    [Tooltip("Selected season id copied from the manager.")]
    public string selectedSeasonId;
    [Tooltip("Selected bracket roster id copied from the manager.")]
    public string selectedBracketRosterId;
    [Tooltip("Selected match id copied from the manager.")]
    public string selectedMatchId;
    [Tooltip("Current in-game day.")]
    public int day;
    [Tooltip("Current in-game hour.")]
    public int hour;
    [Tooltip("Current absolute in-game hour.")]
    public int absoluteHour;
    [Tooltip("Visible ranking row count.")]
    public int rankingCount;
    [Tooltip("Visible ranking rows that are unlocked or unlocked by default.")]
    public int unlockedRankingCount;
    [Tooltip("Visible ranking rows that cannot score right now.")]
    public int blockedRankingCount;
    [Tooltip("Visible active bracket count.")]
    public int activeBracketCount;
    [Tooltip("Visible completed bracket count.")]
    public int completedBracketCount;
    [Tooltip("Visible active season count.")]
    public int activeSeasonCount;
    [Tooltip("Visible seasons that can start right now.")]
    public int availableSeasonCount;
    [Tooltip("Visible match rows that are not completed yet.")]
    public int pendingMatchCount;
    [Tooltip("Visible match rows involving the player.")]
    public int playerMatchCount;
    [Tooltip("Ranking rows shown by ranking/league UI.")]
    public List<CompetitionRankingUIRow> rankingRows = new List<CompetitionRankingUIRow>();
    [Tooltip("Bracket rows shown by tournament/bracket UI.")]
    public List<CompetitionBracketUIRow> bracketRows = new List<CompetitionBracketUIRow>();
    [Tooltip("Season rows shown by league season UI.")]
    public List<CompetitionSeasonUIRow> seasonRows = new List<CompetitionSeasonUIRow>();
    [Tooltip("Match rows shown by bracket detail or match history UI.")]
    public List<CompetitionMatchUIRow> matchRows = new List<CompetitionMatchUIRow>();
    [Tooltip("Resolved selected ranking row, if any.")]
    public CompetitionRankingUIRow selectedRanking;
    [Tooltip("Resolved selected bracket row, if any.")]
    public CompetitionBracketUIRow selectedBracket;
    [Tooltip("Resolved selected season row, if any.")]
    public CompetitionSeasonUIRow selectedSeason;
    [Tooltip("Resolved selected match row, if any.")]
    public CompetitionMatchUIRow selectedMatch;
}

[Serializable]
public class CompetitionRankingUIRow : ICompetitionBracketRankingFilterable {
    [Tooltip("Ranking definition or saved state id.")]
    public string rankingId;
    [Tooltip("Display name shown by ranking UI.")]
    public string displayName;
    [Tooltip("Description shown by detail UI.")]
    public string description;
    [Tooltip("Ranking scope such as local, regional, world or special.")]
    public CompetitionRankingScope scope;
    [Tooltip("Connected world region id, if any.")]
    public string worldRegionId;
    [Tooltip("Connected world region display name, if any.")]
    public string worldRegionName;
    [Tooltip("Free-form ranking tags.")]
    public List<string> tags = new List<string>();
    [Tooltip("If enabled, this row came from a definition asset.")]
    public bool hasDefinition;
    [Tooltip("If enabled, this ranking is unlocked or unlocked by default.")]
    public bool unlocked;
    [Tooltip("If enabled, this ranking can score right now.")]
    public bool canScore;
    [Tooltip("Current point total.")]
    public int currentPoints;
    [Tooltip("Best point total ever reached.")]
    public int bestPoints;
    [Tooltip("Lifetime point total.")]
    public int lifetimePoints;
    [Tooltip("Last point delta.")]
    public int lastDelta;
    [Tooltip("Current tier id.")]
    public string currentTierId;
    [Tooltip("Current tier display name.")]
    public string currentTierName;
    [Tooltip("Next tier id, if any.")]
    public string nextTierId;
    [Tooltip("Next tier display name, if any.")]
    public string nextTierName;
    [Tooltip("Points needed to reach the next tier. 0 means no next tier or already reached.")]
    public int pointsToNextTier;
    [Tooltip("Reached tier ids copied from the player log.")]
    public List<string> reachedTierIds = new List<string>();
    [Tooltip("Recent point history rows for this ranking.")]
    public List<CompetitionRankingPointHistoryUIRow> pointHistoryRows = new List<CompetitionRankingPointHistoryUIRow>();
    [Tooltip("Failure reason shown when Can Score is false.")]
    public string failureMessage;
    [Tooltip("Short text useful for placeholder UI rows.")]
    public string displayText;

    public string WorldRegionId => worldRegionId;

    public bool MatchesSearch(string search) {
        return SearchUtility(search, rankingId, displayName, description, scope.ToString(), worldRegionName, currentTierName, nextTierName, displayText);
    }

    public bool HasTag(string tag) {
        return HasTagUtility(tags, tag);
    }

    public static CompetitionRankingUIRow FromDefinition(CompetitionRankingDefinition ranking, PlayerController player, PlayerCompetitionRankingLog rankingLog, int maxHistoryRows) {
        var state = rankingLog != null && ranking != null ? rankingLog.GetState(ranking) : null;
        string failure = "Ranking could not be resolved.";
        bool canScore = ranking != null && ranking.CanScore(player, rankingLog, out failure);
        int currentPoints = state != null ? state.currentPoints : 0;
        var currentTier = ranking != null ? ranking.GetTierForPoints(currentPoints) : null;
        var nextTier = ranking?.RankTiers
            .Where(tier => tier != null && tier.MinimumPoints > currentPoints)
            .OrderBy(tier => tier.MinimumPoints)
            .FirstOrDefault();

        return new CompetitionRankingUIRow {
            rankingId = ranking != null ? ranking.Id : string.Empty,
            displayName = ranking != null ? ranking.DisplayName : string.Empty,
            description = ranking != null ? ranking.Description : string.Empty,
            scope = ranking != null ? ranking.Scope : CompetitionRankingScope.Special,
            worldRegionId = ranking?.WorldRegion != null ? ranking.WorldRegion.Id : string.Empty,
            worldRegionName = ranking?.WorldRegion != null ? ranking.WorldRegion.DisplayName : string.Empty,
            tags = ranking != null ? ranking.Tags.ToList() : new List<string>(),
            hasDefinition = ranking != null,
            unlocked = ranking != null && (ranking.UnlockedByDefault || (rankingLog?.HasUnlocked(ranking) ?? false)),
            canScore = canScore,
            currentPoints = currentPoints,
            bestPoints = state != null ? state.bestPoints : 0,
            lifetimePoints = state != null ? state.lifetimePoints : 0,
            lastDelta = state != null ? state.lastDelta : 0,
            currentTierId = currentTier != null ? currentTier.TierId : state != null ? state.currentTierId : string.Empty,
            currentTierName = currentTier != null ? currentTier.DisplayName : state != null ? state.currentTierName : string.Empty,
            nextTierId = nextTier != null ? nextTier.TierId : string.Empty,
            nextTierName = nextTier != null ? nextTier.DisplayName : string.Empty,
            pointsToNextTier = nextTier != null ? Mathf.Max(0, nextTier.MinimumPoints - currentPoints) : 0,
            reachedTierIds = state != null ? state.reachedTierIds.Distinct().ToList() : new List<string>(),
            pointHistoryRows = BuildPointHistoryRows(state, maxHistoryRows),
            failureMessage = canScore ? string.Empty : failure,
            displayText = ranking != null ? $"{ranking.DisplayName} - {currentPoints} pts" : string.Empty
        };
    }

    public static CompetitionRankingUIRow FromStateOnly(PlayerCompetitionRankingState state, int maxHistoryRows) {
        return new CompetitionRankingUIRow {
            rankingId = state != null ? state.rankingId : string.Empty,
            displayName = state != null ? state.rankingName : string.Empty,
            scope = CompetitionRankingScope.Special,
            hasDefinition = false,
            unlocked = true,
            canScore = false,
            currentPoints = state != null ? state.currentPoints : 0,
            bestPoints = state != null ? state.bestPoints : 0,
            lifetimePoints = state != null ? state.lifetimePoints : 0,
            lastDelta = state != null ? state.lastDelta : 0,
            currentTierId = state != null ? state.currentTierId : string.Empty,
            currentTierName = state != null ? state.currentTierName : string.Empty,
            reachedTierIds = state != null ? state.reachedTierIds.Distinct().ToList() : new List<string>(),
            pointHistoryRows = BuildPointHistoryRows(state, maxHistoryRows),
            failureMessage = "This ranking was found in save data but no definition is assigned to this UI pool.",
            displayText = state != null ? $"{state.rankingName} - {state.currentPoints} pts" : string.Empty
        };
    }

    static List<CompetitionRankingPointHistoryUIRow> BuildPointHistoryRows(PlayerCompetitionRankingState state, int maxRows) {
        var rows = state?.pointHistory?
            .Where(record => record != null)
            .OrderByDescending(record => record.totalHour)
            .Select(CompetitionRankingPointHistoryUIRow.FromRecord)
            ?? Enumerable.Empty<CompetitionRankingPointHistoryUIRow>();

        return (maxRows > 0 ? rows.Take(maxRows) : rows).ToList();
    }

    static bool SearchUtility(string search, params string[] values) {
        if(string.IsNullOrWhiteSpace(search)) {
            return true;
        }

        return values.Any(value => !string.IsNullOrWhiteSpace(value) && value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    static bool HasTagUtility(IEnumerable<string> values, string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && values != null
            && values.Any(value => string.Equals(value, tag, StringComparison.OrdinalIgnoreCase));
    }
}

[Serializable]
public class CompetitionRankingPointHistoryUIRow {
    [Tooltip("Related ranking id.")]
    public string rankingId;
    [Tooltip("Related competition id.")]
    public string competitionId;
    [Tooltip("Related competition display name.")]
    public string competitionName;
    [Tooltip("Related stage id.")]
    public string stageId;
    [Tooltip("Related challenge id.")]
    public string challengeId;
    [Tooltip("Point delta applied.")]
    public int delta;
    [Tooltip("Point total after the delta.")]
    public int totalPoints;
    [Tooltip("In-game total hour when the point change happened.")]
    public int totalHour;
    [Tooltip("Short source id that caused the point change.")]
    public string sourceId;
    [Tooltip("Short text useful for placeholder UI rows.")]
    public string displayText;

    public static CompetitionRankingPointHistoryUIRow FromRecord(PlayerCompetitionRankingPointRecord record) {
        string sign = record != null && record.delta >= 0 ? "+" : string.Empty;
        return new CompetitionRankingPointHistoryUIRow {
            rankingId = record != null ? record.rankingId : string.Empty,
            competitionId = record != null ? record.competitionId : string.Empty,
            competitionName = record != null ? record.competitionName : string.Empty,
            stageId = record != null ? record.stageId : string.Empty,
            challengeId = record != null ? record.challengeId : string.Empty,
            delta = record != null ? record.delta : 0,
            totalPoints = record != null ? record.totalPoints : 0,
            totalHour = record != null ? record.totalHour : -1,
            sourceId = record != null ? record.sourceId : string.Empty,
            displayText = record != null ? $"{record.competitionName}: {sign}{record.delta} ({record.totalPoints})" : string.Empty
        };
    }
}

[Serializable]
public class CompetitionBracketUIRow : ICompetitionBracketRankingFilterable, ICompetitionRankingLinkedRow {
    [Tooltip("Roster id linked to this bracket row.")]
    public string rosterId;
    [Tooltip("Display name shown by bracket UI.")]
    public string displayName;
    [Tooltip("Description shown by detail UI.")]
    public string description;
    [Tooltip("Competition id connected to this bracket.")]
    public string competitionId;
    [Tooltip("Competition display name connected to this bracket.")]
    public string competitionName;
    [Tooltip("Season id connected to this bracket.")]
    public string seasonId;
    [Tooltip("Season display name connected to this bracket.")]
    public string seasonName;
    [Tooltip("Ranking id connected to this bracket.")]
    public string rankingId;
    [Tooltip("Ranking display name connected to this bracket.")]
    public string rankingName;
    [Tooltip("Bracket format.")]
    public CompetitionBracketFormat bracketFormat;
    [Tooltip("Free-form bracket or roster tags.")]
    public List<string> tags = new List<string>();
    [Tooltip("If enabled, this row came from a generated bracket state.")]
    public bool hasState;
    [Tooltip("If enabled, this row came from a roster definition asset.")]
    public bool hasDefinition;
    [Tooltip("If enabled, a new bracket can be generated from this roster right now.")]
    public bool canGenerate;
    [Tooltip("If enabled, this bracket is active.")]
    public bool active;
    [Tooltip("If enabled, this bracket has completed.")]
    public bool completed;
    [Tooltip("If enabled, the player won this bracket.")]
    public bool won;
    [Tooltip("If enabled, the bracket was abandoned.")]
    public bool abandoned;
    [Tooltip("Current round index.")]
    public int currentRoundIndex;
    [Tooltip("Generated entrant count.")]
    public int entrantCount;
    [Tooltip("Generated round count.")]
    public int roundCount;
    [Tooltip("Generated match count.")]
    public int matchCount;
    [Tooltip("Completed match count.")]
    public int completedMatchCount;
    [Tooltip("Player match win count.")]
    public int matchWinCount;
    [Tooltip("Player match loss count.")]
    public int matchLossCount;
    [Tooltip("In-game total hour when generated.")]
    public int generatedTotalHour;
    [Tooltip("In-game total hour when completed or abandoned.")]
    public int completedTotalHour;
    [Tooltip("Last recorded match id.")]
    public string lastMatchId;
    [Tooltip("Failure reason shown when Can Generate is false.")]
    public string failureMessage;
    [Tooltip("Short text useful for placeholder UI rows.")]
    public string displayText;

    public string WorldRegionId => string.Empty;
    public string RankingId => rankingId;

    public bool MatchesSearch(string search) {
        return SearchUtility(search, rosterId, displayName, description, competitionName, seasonName, rankingName, bracketFormat.ToString(), displayText);
    }

    public bool HasTag(string tag) {
        return HasTagUtility(tags, tag);
    }

    public static CompetitionBracketUIRow FromRoster(CompetitionRosterDefinition roster, PlayerController player, PlayerCompetitionBracketLog bracketLog) {
        string failure = "Roster could not be resolved.";
        bool canGenerate = roster != null && roster.CanGenerate(player, out failure);
        var activeState = bracketLog != null && roster != null ? bracketLog.GetActiveBracket(roster) : null;
        return new CompetitionBracketUIRow {
            rosterId = roster != null ? roster.Id : string.Empty,
            displayName = roster != null ? roster.DisplayName : string.Empty,
            description = roster != null ? roster.Description : string.Empty,
            competitionId = roster?.Competition != null ? roster.Competition.Id : string.Empty,
            competitionName = roster?.Competition != null ? roster.Competition.DisplayName : string.Empty,
            seasonId = roster?.Season != null ? roster.Season.Id : string.Empty,
            seasonName = roster?.Season != null ? roster.Season.DisplayName : string.Empty,
            rankingId = roster?.Ranking != null ? roster.Ranking.Id : string.Empty,
            rankingName = roster?.Ranking != null ? roster.Ranking.DisplayName : string.Empty,
            bracketFormat = roster != null ? roster.BracketFormat : CompetitionBracketFormat.FreeRun,
            tags = roster != null ? roster.Tags.ToList() : new List<string>(),
            hasState = activeState != null,
            hasDefinition = roster != null,
            canGenerate = canGenerate,
            active = activeState != null && activeState.active,
            completed = activeState != null && activeState.completed,
            won = activeState != null && activeState.won,
            abandoned = activeState != null && activeState.abandoned,
            currentRoundIndex = activeState != null ? activeState.currentRoundIndex : 0,
            entrantCount = activeState != null ? activeState.Entrants.Count : 0,
            roundCount = activeState != null ? activeState.Rounds.Count : 0,
            matchCount = activeState != null ? activeState.Rounds.Sum(round => round.Matches.Count) : 0,
            completedMatchCount = activeState != null ? activeState.Rounds.Sum(round => round.Matches.Count(match => match.completed)) : 0,
            matchWinCount = activeState != null ? activeState.matchWinCount : 0,
            matchLossCount = activeState != null ? activeState.matchLossCount : 0,
            generatedTotalHour = activeState != null ? activeState.generatedTotalHour : -1,
            completedTotalHour = activeState != null ? activeState.completedTotalHour : -1,
            lastMatchId = activeState != null ? activeState.lastMatchId : string.Empty,
            failureMessage = canGenerate ? string.Empty : failure,
            displayText = roster != null ? $"{roster.DisplayName} - {(canGenerate ? "available" : "locked")}" : string.Empty
        };
    }

    public static CompetitionBracketUIRow FromState(PlayerCompetitionBracketState state) {
        return new CompetitionBracketUIRow {
            rosterId = state != null ? state.rosterId : string.Empty,
            displayName = state != null ? state.rosterName : string.Empty,
            competitionId = state != null ? state.competitionId : string.Empty,
            competitionName = state != null ? state.competitionName : string.Empty,
            seasonId = state != null ? state.seasonId : string.Empty,
            seasonName = state != null ? state.seasonName : string.Empty,
            rankingId = state != null ? state.rankingId : string.Empty,
            rankingName = state != null ? state.rankingName : string.Empty,
            bracketFormat = state != null ? state.bracketFormat : CompetitionBracketFormat.FreeRun,
            hasState = state != null,
            hasDefinition = false,
            canGenerate = false,
            active = state != null && state.active,
            completed = state != null && state.completed,
            won = state != null && state.won,
            abandoned = state != null && state.abandoned,
            currentRoundIndex = state != null ? state.currentRoundIndex : 0,
            entrantCount = state != null ? state.Entrants.Count : 0,
            roundCount = state != null ? state.Rounds.Count : 0,
            matchCount = state != null ? state.Rounds.Sum(round => round.Matches.Count) : 0,
            completedMatchCount = state != null ? state.Rounds.Sum(round => round.Matches.Count(match => match.completed)) : 0,
            matchWinCount = state != null ? state.matchWinCount : 0,
            matchLossCount = state != null ? state.matchLossCount : 0,
            generatedTotalHour = state != null ? state.generatedTotalHour : -1,
            completedTotalHour = state != null ? state.completedTotalHour : -1,
            lastMatchId = state != null ? state.lastMatchId : string.Empty,
            failureMessage = state != null && state.active ? string.Empty : "This row is a saved/generated bracket state, not a roster generation option.",
            displayText = state != null ? $"{state.rosterName} - {(state.active ? "active" : state.completed ? "completed" : "inactive")}" : string.Empty
        };
    }

    static bool SearchUtility(string search, params string[] values) {
        if(string.IsNullOrWhiteSpace(search)) {
            return true;
        }

        return values.Any(value => !string.IsNullOrWhiteSpace(value) && value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    static bool HasTagUtility(IEnumerable<string> values, string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && values != null
            && values.Any(value => string.Equals(value, tag, StringComparison.OrdinalIgnoreCase));
    }
}

[Serializable]
public class CompetitionSeasonUIRow : ICompetitionBracketRankingFilterable {
    [Tooltip("Season definition or saved state id.")]
    public string seasonId;
    [Tooltip("Display name shown by season UI.")]
    public string displayName;
    [Tooltip("Description shown by detail UI.")]
    public string description;
    [Tooltip("Broad season kind.")]
    public CompetitionSeasonKind kind;
    [Tooltip("Connected world region id, if any.")]
    public string worldRegionId;
    [Tooltip("Connected world region display name, if any.")]
    public string worldRegionName;
    [Tooltip("Free-form season tags.")]
    public List<string> tags = new List<string>();
    [Tooltip("If enabled, this row came from a definition asset.")]
    public bool hasDefinition;
    [Tooltip("If enabled, this season is unlocked or unlocked by default.")]
    public bool unlocked;
    [Tooltip("If enabled, this season is active for the player.")]
    public bool active;
    [Tooltip("If enabled, this season can start right now.")]
    public bool canStart;
    [Tooltip("If enabled, this season can complete right now.")]
    public bool canComplete;
    [Tooltip("Number of times this season started.")]
    public int startedCount;
    [Tooltip("Number of times this season completed.")]
    public int completedCount;
    [Tooltip("Last in-game total hour this season started.")]
    public int lastStartedHour;
    [Tooltip("Last in-game total hour this season completed.")]
    public int lastCompletedHour;
    [Tooltip("Failure reason shown when Can Start is false.")]
    public string failureMessage;
    [Tooltip("Short text useful for placeholder UI rows.")]
    public string displayText;

    public string WorldRegionId => worldRegionId;

    public bool MatchesSearch(string search) {
        return SearchUtility(search, seasonId, displayName, description, kind.ToString(), worldRegionName, displayText);
    }

    public bool HasTag(string tag) {
        return HasTagUtility(tags, tag);
    }

    public static CompetitionSeasonUIRow FromDefinition(CompetitionSeasonDefinition season, PlayerController player, PlayerCompetitionSeasonLog seasonLog) {
        var state = seasonLog != null && season != null ? seasonLog.GetState(season) : null;
        string failure = "Season could not be resolved.";
        bool canStart = season != null && season.CanStart(player, out failure);
        string completionFailure;
        bool canComplete = season != null && season.CanComplete(player, out completionFailure);
        return new CompetitionSeasonUIRow {
            seasonId = season != null ? season.Id : string.Empty,
            displayName = season != null ? season.DisplayName : string.Empty,
            description = season != null ? season.Description : string.Empty,
            kind = season != null ? season.Kind : CompetitionSeasonKind.Custom,
            worldRegionId = season?.WorldRegion != null ? season.WorldRegion.Id : string.Empty,
            worldRegionName = season?.WorldRegion != null ? season.WorldRegion.DisplayName : string.Empty,
            tags = season != null ? season.Tags.ToList() : new List<string>(),
            hasDefinition = season != null,
            unlocked = season != null && (season.UnlockedByDefault || (seasonLog?.HasUnlocked(season) ?? false)),
            active = seasonLog != null && season != null && seasonLog.IsActive(season),
            canStart = canStart,
            canComplete = canComplete,
            startedCount = state != null ? state.startedCount : 0,
            completedCount = state != null ? state.completedCount : 0,
            lastStartedHour = state != null ? state.lastStartedHour : -1,
            lastCompletedHour = state != null ? state.lastCompletedHour : -1,
            failureMessage = canStart ? string.Empty : failure,
            displayText = season != null ? $"{season.DisplayName} - {(canStart ? "available" : state != null && state.active ? "active" : "locked")}" : string.Empty
        };
    }

    public static CompetitionSeasonUIRow FromStateOnly(PlayerCompetitionSeasonState state) {
        return new CompetitionSeasonUIRow {
            seasonId = state != null ? state.seasonId : string.Empty,
            displayName = state != null ? state.seasonName : string.Empty,
            kind = CompetitionSeasonKind.Custom,
            hasDefinition = false,
            unlocked = true,
            active = state != null && state.active,
            canStart = false,
            canComplete = false,
            startedCount = state != null ? state.startedCount : 0,
            completedCount = state != null ? state.completedCount : 0,
            lastStartedHour = state != null ? state.lastStartedHour : -1,
            lastCompletedHour = state != null ? state.lastCompletedHour : -1,
            failureMessage = "This season was found in save data but no definition is assigned to this UI pool.",
            displayText = state != null ? $"{state.seasonName} - {(state.active ? "active" : "saved")}" : string.Empty
        };
    }

    static bool SearchUtility(string search, params string[] values) {
        if(string.IsNullOrWhiteSpace(search)) {
            return true;
        }

        return values.Any(value => !string.IsNullOrWhiteSpace(value) && value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    static bool HasTagUtility(IEnumerable<string> values, string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && values != null
            && values.Any(value => string.Equals(value, tag, StringComparison.OrdinalIgnoreCase));
    }
}

[Serializable]
public class CompetitionMatchUIRow : ICompetitionBracketRankingFilterable, ICompetitionRankingLinkedRow {
    [Tooltip("Roster id that owns this match.")]
    public string rosterId;
    [Tooltip("Roster display name that owns this match.")]
    public string rosterName;
    [Tooltip("Competition id connected to this match.")]
    public string competitionId;
    [Tooltip("Competition display name connected to this match.")]
    public string competitionName;
    [Tooltip("Season id connected to this match.")]
    public string seasonId;
    [Tooltip("Season display name connected to this match.")]
    public string seasonName;
    [Tooltip("Ranking id connected to this match.")]
    public string rankingId;
    [Tooltip("Ranking display name connected to this match.")]
    public string rankingName;
    [Tooltip("Match id inside the generated bracket.")]
    public string matchId;
    [Tooltip("Round index that owns this match.")]
    public int roundIndex;
    [Tooltip("Round display name that owns this match.")]
    public string roundName;
    [Tooltip("Match index inside its round.")]
    public int matchIndex;
    [Tooltip("First entrant display name.")]
    public string firstEntrantName;
    [Tooltip("Second entrant display name.")]
    public string secondEntrantName;
    [Tooltip("Winner entrant display name.")]
    public string winnerEntrantName;
    [Tooltip("Loser entrant display name.")]
    public string loserEntrantName;
    [Tooltip("Battle challenge id this match should launch when played by the player.")]
    public string challengeId;
    [Tooltip("Battle rule set id this match should use.")]
    public string ruleSetId;
    [Tooltip("If enabled, the owning bracket is active.")]
    public bool bracketActive;
    [Tooltip("If enabled, this match result has been recorded.")]
    public bool completed;
    [Tooltip("If enabled, this match includes the player.")]
    public bool playerInMatch;
    [Tooltip("If enabled, the player won this match.")]
    public bool playerWon;
    [Tooltip("If enabled, this match was resolved by a bracket simulation resolver.")]
    public bool resolvedAutomatically;
    [Tooltip("In-game total hour when this match completed.")]
    public int completedTotalHour;
    [Tooltip("Short source id that recorded this match.")]
    public string sourceId;
    [Tooltip("Short text useful for placeholder UI rows.")]
    public string displayText;

    public string WorldRegionId => string.Empty;
    public string RankingId => rankingId;

    public bool MatchesSearch(string search) {
        return SearchUtility(search, rosterName, competitionName, seasonName, rankingName, matchId, roundName, firstEntrantName, secondEntrantName, winnerEntrantName, challengeId, ruleSetId, displayText);
    }

    public bool HasTag(string tag) {
        return false;
    }

    public static CompetitionMatchUIRow FromMatch(PlayerCompetitionBracketState state, PlayerCompetitionBracketRoundRecord round, PlayerCompetitionBracketMatchRecord match) {
        if(state == null || round == null || match == null) {
            return null;
        }

        return new CompetitionMatchUIRow {
            rosterId = state.rosterId,
            rosterName = state.rosterName,
            competitionId = state.competitionId,
            competitionName = state.competitionName,
            seasonId = state.seasonId,
            seasonName = state.seasonName,
            rankingId = state.rankingId,
            rankingName = state.rankingName,
            matchId = match.matchId,
            roundIndex = round.roundIndex,
            roundName = round.roundName,
            matchIndex = match.matchIndex,
            firstEntrantName = match.firstEntrantName,
            secondEntrantName = match.secondEntrantName,
            winnerEntrantName = match.winnerEntrantName,
            loserEntrantName = match.loserEntrantName,
            challengeId = match.challengeId,
            ruleSetId = match.ruleSetId,
            bracketActive = state.active,
            completed = match.completed,
            playerInMatch = match.playerInMatch,
            playerWon = match.playerWon,
            resolvedAutomatically = match.resolvedAutomatically,
            completedTotalHour = match.completedTotalHour,
            sourceId = match.sourceId,
            displayText = $"{round.roundName}: {match.firstEntrantName} vs {match.secondEntrantName}"
        };
    }

    static bool SearchUtility(string search, params string[] values) {
        if(string.IsNullOrWhiteSpace(search)) {
            return true;
        }

        return values.Any(value => !string.IsNullOrWhiteSpace(value) && value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
    }
}
