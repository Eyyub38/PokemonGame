using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum ProgressionAccessUIActionResultKind {
    None,
    Refreshed,
    TitleGranted,
    TitleRevoked,
    MilestoneCompleted,
    CareerJoined,
    CareerPointsAdded,
    ReputationChanged,
    AccessChecked,
    Blocked
}

public class ProgressionAccessUIManager : MonoBehaviour {
    [Header("Player")]
    [Tooltip("Player whose title, career, milestone, reputation and access state is shown. Empty uses PlayerController.i or the first PlayerController in the scene.")]
    [SerializeField] PlayerController playerOverride = null;
    [Tooltip("If enabled, missing player log components are created when UI actions need them.")]
    [SerializeField] bool createMissingLogsForActions = true;

    [Header("Title Pool")]
    [Tooltip("Titles, badges, permits or licenses explicitly shown by this UI. Empty can still read Resources when Include Resource Titles is enabled.")]
    [SerializeField] List<TitleDefinition> titlePool = new List<TitleDefinition>();
    [Tooltip("If enabled, all TitleDefinition assets in Resources are added to the visible pool.")]
    [SerializeField] bool includeResourceTitles = true;

    [Header("Career Pool")]
    [Tooltip("Career paths explicitly shown by this UI. Empty can still read Resources when Include Resource Careers is enabled.")]
    [SerializeField] List<CareerPathDefinition> careerPool = new List<CareerPathDefinition>();
    [Tooltip("If enabled, all CareerPathDefinition assets in Resources are added to the visible pool.")]
    [SerializeField] bool includeResourceCareers = true;

    [Header("Milestone Pool")]
    [Tooltip("Milestones explicitly shown by this UI. Empty can still read Resources when Include Resource Milestones is enabled.")]
    [SerializeField] List<MilestoneDefinition> milestonePool = new List<MilestoneDefinition>();
    [Tooltip("If enabled, all MilestoneDefinition assets in Resources are added to the visible pool.")]
    [SerializeField] bool includeResourceMilestones = true;

    [Header("Reputation Pool")]
    [Tooltip("Reputation factions explicitly shown by this UI. Empty can still read Resources when Include Resource Factions is enabled.")]
    [SerializeField] List<ReputationFactionDefinition> factionPool = new List<ReputationFactionDefinition>();
    [Tooltip("If enabled, all ReputationFactionDefinition assets in Resources are added to the visible pool.")]
    [SerializeField] bool includeResourceFactions = true;

    [Header("Access Pool")]
    [Tooltip("Access profiles explicitly shown by this UI. Empty can still read Resources when Include Resource Access Profiles is enabled.")]
    [SerializeField] List<AccessProfileDefinition> accessProfilePool = new List<AccessProfileDefinition>();
    [Tooltip("If enabled, all AccessProfileDefinition assets in Resources are added to the visible pool.")]
    [SerializeField] bool includeResourceAccessProfiles = true;

    [Header("Visibility")]
    [Tooltip("If enabled, known titles that the player does not currently have are shown as inactive rows.")]
    [SerializeField] bool includeInactiveTitles = true;
    [Tooltip("If enabled, locked or unavailable careers remain visible with their failure reason.")]
    [SerializeField] bool includeLockedCareers = true;
    [Tooltip("If enabled, hidden milestone definitions can be shown before completion.")]
    [SerializeField] bool includeHiddenMilestones;
    [Tooltip("If enabled, known milestones that are not completed yet are shown as incomplete rows.")]
    [SerializeField] bool includeIncompleteMilestones = true;
    [Tooltip("If enabled, access profiles with no history are shown as checkable rows.")]
    [SerializeField] bool includeUnusedAccessProfiles = true;
    [Tooltip("Maximum title rows copied into the snapshot. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxTitleRows = 30;
    [Tooltip("Maximum career rows copied into the snapshot. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxCareerRows = 30;
    [Tooltip("Maximum milestone rows copied into the snapshot. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxMilestoneRows = 40;
    [Tooltip("Maximum reputation rows copied into the snapshot. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxReputationRows = 30;
    [Tooltip("Maximum access rows copied into the snapshot. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxAccessRows = 40;

    [Header("Snapshot")]
    [Tooltip("If enabled, Refresh is called when this component starts.")]
    [SerializeField] bool refreshOnStart = true;
    [Tooltip("If enabled, Refresh is called after every successful or blocked action.")]
    [SerializeField] bool refreshAfterActions = true;
    [Tooltip("Source id stored in title/career/action logs when this UI backend applies a change.")]
    [SerializeField] string uiSourceId = "ui:progression-access";

    [Header("Debug")]
    [Tooltip("If enabled, successful UI backend actions are written to GameDebug.")]
    [SerializeField] bool logSuccessfulActions;
    [Tooltip("If enabled, blocked or denied UI backend actions are written to GameDebug.")]
    [SerializeField] bool logBlockedActions = true;

    ProgressionAccessUIScreenSnapshot currentSnapshot = new ProgressionAccessUIScreenSnapshot();
    ProgressionAccessUIActionResult lastResult = new ProgressionAccessUIActionResult();

    public ProgressionAccessUIScreenSnapshot CurrentSnapshot => currentSnapshot;
    public ProgressionAccessUIActionResult LastResult => lastResult;
    public PlayerController PlayerOverride => playerOverride;
    public bool CreateMissingLogsForActions => createMissingLogsForActions;
    public IReadOnlyList<TitleDefinition> TitlePool => titlePool;
    public bool IncludeResourceTitles => includeResourceTitles;
    public IReadOnlyList<CareerPathDefinition> CareerPool => careerPool;
    public bool IncludeResourceCareers => includeResourceCareers;
    public IReadOnlyList<MilestoneDefinition> MilestonePool => milestonePool;
    public bool IncludeResourceMilestones => includeResourceMilestones;
    public IReadOnlyList<ReputationFactionDefinition> FactionPool => factionPool;
    public bool IncludeResourceFactions => includeResourceFactions;
    public IReadOnlyList<AccessProfileDefinition> AccessProfilePool => accessProfilePool;
    public bool IncludeResourceAccessProfiles => includeResourceAccessProfiles;
    public bool IncludeInactiveTitles => includeInactiveTitles;
    public bool IncludeLockedCareers => includeLockedCareers;
    public bool IncludeIncompleteMilestones => includeIncompleteMilestones;
    public bool IncludeUnusedAccessProfiles => includeUnusedAccessProfiles;
    public event Action<ProgressionAccessUIScreenSnapshot> OnSnapshotChanged;
    public event Action<ProgressionAccessUIActionResult> OnActionResult;

    void Start() {
        if(refreshOnStart) {
            Refresh();
        }
    }

    [ContextMenu("Refresh Progression Access Snapshot")]
    public ProgressionAccessUIScreenSnapshot RefreshFromContextMenu() {
        return Refresh();
    }

    public ProgressionAccessUIScreenSnapshot Refresh() {
        var player = ResolvePlayer();
        var titles = player != null ? player.GetComponent<PlayerTitles>() : null;
        var careers = player != null ? player.GetComponent<PlayerCareerLog>() : null;
        var milestones = player != null ? player.GetComponent<PlayerMilestones>() : null;
        var reputation = player != null ? player.GetComponent<PlayerReputation>() : null;
        var accessLog = player != null ? player.GetComponent<PlayerAccessLog>() : null;

        var titleRows = BuildTitleRows(titles).ToList();
        var careerRows = BuildCareerRows(player, careers).ToList();
        var milestoneRows = BuildMilestoneRows(milestones).ToList();
        var reputationRows = BuildReputationRows(reputation).ToList();
        var accessRows = BuildAccessRows(player, accessLog).ToList();

        currentSnapshot = new ProgressionAccessUIScreenSnapshot {
            hasPlayer = player != null,
            playerName = player != null ? player.name : string.Empty,
            sourceId = ResolveSourceId(),
            hasTitleLog = titles != null,
            hasCareerLog = careers != null,
            hasMilestoneLog = milestones != null,
            hasReputationLog = reputation != null,
            hasAccessLog = accessLog != null,
            day = TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1,
            hour = TimeSystem.i != null ? Mathf.Clamp(TimeSystem.i.Hour, 0, 23) : 0,
            absoluteHour = GetCurrentAbsoluteHour(),
            activeTitleCount = titleRows.Count(row => row != null && row.active),
            permanentTitleCount = titleRows.Count(row => row != null && row.active && row.permanent),
            temporaryTitleCount = titleRows.Count(row => row != null && row.active && !row.permanent),
            joinedCareerCount = careerRows.Count(row => row != null && row.joined),
            unlockedCareerCount = careerRows.Count(row => row != null && row.unlocked),
            completedMilestoneCount = milestoneRows.Count(row => row != null && row.completed),
            knownReputationCount = reputationRows.Count(row => row != null && row.knownInLog),
            positiveReputationCount = reputationRows.Count(row => row != null && row.value > 0),
            negativeReputationCount = reputationRows.Count(row => row != null && row.value < 0),
            accessPassedCount = accessRows.Sum(row => row != null ? Mathf.Max(0, row.passedCount) : 0),
            accessDeniedCount = accessRows.Sum(row => row != null ? Mathf.Max(0, row.deniedCount) : 0),
            titleRows = titleRows,
            careerRows = careerRows,
            milestoneRows = milestoneRows,
            reputationRows = reputationRows,
            accessRows = accessRows,
            lastResult = lastResult
        };

        OnSnapshotChanged?.Invoke(currentSnapshot);
        return currentSnapshot;
    }

    public bool TryGrantTitle(TitleDefinition title, out string feedback) {
        int durationHours = title != null && !title.PermanentByDefault ? title.DefaultDurationHours : -1;
        return TryGrantTitle(title, durationHours, out feedback);
    }

    public bool TryGrantTitle(TitleDefinition title, int durationHours, out string feedback) {
        var player = ResolvePlayer();
        if(player == null) {
            return Block("A player is required to grant titles.", out feedback);
        }

        if(title == null) {
            return Block("No title was provided.", out feedback);
        }

        var log = GetPlayerComponent<PlayerTitles>(player, createMissingLogsForActions);
        if(log == null) {
            return Block("PlayerTitles is missing.", out feedback);
        }

        int resolvedDuration = ResolveTitleDuration(title, durationHours);
        if(log.Grant(title, resolvedDuration, ResolveSourceId(), refreshExisting: true, context: this)) {
            return Succeed(ProgressionAccessUIActionResultKind.TitleGranted, $"{title.DisplayName} granted.", out feedback);
        }

        return Block($"{title.DisplayName} could not be granted.", out feedback);
    }

    public bool TryGrantTitleById(string titleId, out string feedback) {
        return TryGrantTitle(FindTitle(titleId), out feedback);
    }

    public bool TryRevokeTitle(string titleId, out string feedback) {
        var player = ResolvePlayer();
        if(player == null) {
            return Block("A player is required to revoke titles.", out feedback);
        }

        if(string.IsNullOrWhiteSpace(titleId)) {
            return Block("No title id was provided.", out feedback);
        }

        var log = GetPlayerComponent<PlayerTitles>(player, createMissingLogsForActions);
        if(log == null) {
            return Block("PlayerTitles is missing.", out feedback);
        }

        if(log.Revoke(titleId, this)) {
            return Succeed(ProgressionAccessUIActionResultKind.TitleRevoked, $"{titleId} revoked.", out feedback);
        }

        return Block($"{titleId} is not active.", out feedback);
    }

    public bool TryJoinCareer(CareerPathDefinition career, bool viaMentor, out string feedback) {
        var player = ResolvePlayer();
        if(player == null) {
            return Block("A player is required to join careers.", out feedback);
        }

        if(career == null) {
            return Block("No career path was provided.", out feedback);
        }

        var log = GetPlayerComponent<PlayerCareerLog>(player, createMissingLogsForActions);
        if(log == null) {
            return Block("PlayerCareerLog is missing.", out feedback);
        }

        bool alreadyJoined = log.HasJoinedCareer(career);
        if(log.JoinCareer(career, viaMentor, ResolveSourceId(), out string failureMessage)) {
            return Succeed(ProgressionAccessUIActionResultKind.CareerJoined, alreadyJoined ? $"{career.DisplayName} is already joined." : $"{career.DisplayName} joined.", out feedback);
        }

        return Block(failureMessage, out feedback);
    }

    public bool TryAddCareerPoints(CareerPathDefinition career, int points, out string feedback) {
        var player = ResolvePlayer();
        if(player == null) {
            return Block("A player is required to add career points.", out feedback);
        }

        if(career == null) {
            return Block("No career path was provided.", out feedback);
        }

        points = Mathf.Max(0, points);
        if(points <= 0) {
            return Block("Career points must be greater than 0.", out feedback);
        }

        var log = GetPlayerComponent<PlayerCareerLog>(player, createMissingLogsForActions);
        if(log == null) {
            return Block("PlayerCareerLog is missing.", out feedback);
        }

        if(log.AddPoints(career, points, ResolveSourceId())) {
            return Succeed(ProgressionAccessUIActionResultKind.CareerPointsAdded, $"{points} point(s) added to {career.DisplayName}.", out feedback);
        }

        return Block($"{career.DisplayName} could not receive points. It may need to be joined first.", out feedback);
    }

    public bool TryCompleteMilestone(MilestoneDefinition milestone, out string feedback) {
        var player = ResolvePlayer();
        if(player == null) {
            return Block("A player is required to complete milestones.", out feedback);
        }

        if(milestone == null) {
            return Block("No milestone was provided.", out feedback);
        }

        var log = GetPlayerComponent<PlayerMilestones>(player, createMissingLogsForActions);
        if(log == null) {
            return Block("PlayerMilestones is missing.", out feedback);
        }

        if(log.CompleteMilestone(milestone)) {
            return Succeed(ProgressionAccessUIActionResultKind.MilestoneCompleted, $"{milestone.DisplayName} completed.", out feedback);
        }

        return Block($"{milestone.DisplayName} is already completed.", out feedback);
    }

    public bool TryAddReputation(ReputationFactionDefinition faction, int amount, out string feedback) {
        var player = ResolvePlayer();
        if(player == null) {
            return Block("A player is required to change reputation.", out feedback);
        }

        if(faction == null) {
            return Block("No reputation faction was provided.", out feedback);
        }

        if(amount == 0) {
            return Block("Reputation amount must not be 0.", out feedback);
        }

        var log = GetPlayerComponent<PlayerReputation>(player, createMissingLogsForActions);
        if(log == null) {
            return Block("PlayerReputation is missing.", out feedback);
        }

        int before = log.GetReputation(faction);
        log.AddReputation(faction, amount);
        int after = log.GetReputation(faction);
        return Succeed(ProgressionAccessUIActionResultKind.ReputationChanged, $"{faction.DisplayName} reputation changed from {before} to {after}.", out feedback);
    }

    public bool TryCheckAccess(AccessProfileDefinition profile, string contextId, out string feedback) {
        var player = ResolvePlayer();
        if(player == null) {
            return Block("A player is required to check access.", out feedback);
        }

        if(profile == null) {
            return Block("No access profile was provided.", out feedback);
        }

        var log = GetPlayerComponent<PlayerAccessLog>(player, createMissingLogsForActions);
        if(log == null) {
            return Block("PlayerAccessLog is missing.", out feedback);
        }

        bool passed = log.CheckAndRecord(profile, contextId, out string failureMessage, this);
        string message = passed ? profile.PassedMessage : failureMessage;
        SetLastResult(ProgressionAccessUIActionResultKind.AccessChecked, passed, message);

        if(passed && logSuccessfulActions) {
            GameDebug.Success(message, GameDebugCategory.Access, this, "ProgressionAccessUIManager");
        } else if(!passed && logBlockedActions) {
            GameDebug.Warning(message, GameDebugCategory.Access, this, "ProgressionAccessUIManager");
        }

        feedback = message;
        return passed;
    }

    public ProgressionTitleRow FindTitleRow(string titleId) {
        return currentSnapshot?.titleRows?
            .FirstOrDefault(row => row != null && string.Equals(row.titleId, titleId, StringComparison.OrdinalIgnoreCase));
    }

    public ProgressionCareerRow FindCareerRow(string careerId) {
        return currentSnapshot?.careerRows?
            .FirstOrDefault(row => row != null && string.Equals(row.careerId, careerId, StringComparison.OrdinalIgnoreCase));
    }

    public ProgressionMilestoneRow FindMilestoneRow(string milestoneId) {
        return currentSnapshot?.milestoneRows?
            .FirstOrDefault(row => row != null && string.Equals(row.milestoneId, milestoneId, StringComparison.OrdinalIgnoreCase));
    }

    public ProgressionReputationRow FindReputationRow(string factionId) {
        return currentSnapshot?.reputationRows?
            .FirstOrDefault(row => row != null && string.Equals(row.factionId, factionId, StringComparison.OrdinalIgnoreCase));
    }

    public ProgressionAccessRow FindAccessRow(string profileId, string contextId = null) {
        return currentSnapshot?.accessRows?
            .FirstOrDefault(row => row != null
                && string.Equals(row.profileId, profileId, StringComparison.OrdinalIgnoreCase)
                && (string.IsNullOrWhiteSpace(contextId) || string.Equals(row.contextId, contextId, StringComparison.Ordinal)));
    }

    IEnumerable<ProgressionTitleRow> BuildTitleRows(PlayerTitles log) {
        var rows = new List<ProgressionTitleRow>();
        var definitions = ResolveTitlePool().ToDictionarySafe(title => title.Id);
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if(log != null) {
            foreach(var state in log.Titles.Where(state => state != null)) {
                var definition = state.definition != null ? state.definition : definitions.GetValueOrDefaultSafe(state.titleId);
                rows.Add(ProgressionTitleRow.FromState(state, definition, GetCurrentAbsoluteHour()));
                seenIds.Add(state.titleId);
            }
        }

        if(includeInactiveTitles) {
            foreach(var title in definitions.Values.Where(title => title != null && !seenIds.Contains(title.Id))) {
                rows.Add(ProgressionTitleRow.FromDefinition(title));
            }
        }

        return LimitRows(rows
            .OrderByDescending(row => row.active)
            .ThenBy(row => row.kind)
            .ThenBy(row => row.displayName), maxTitleRows);
    }

    IEnumerable<ProgressionCareerRow> BuildCareerRows(PlayerController player, PlayerCareerLog log) {
        var rows = new List<ProgressionCareerRow>();
        var definitions = ResolveCareerPool().ToDictionarySafe(career => career.Id);
        var states = log != null
            ? log.Careers.Where(state => state != null).ToDictionarySafe(state => state.careerId)
            : new Dictionary<string, PlayerCareerState>(StringComparer.OrdinalIgnoreCase);
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach(var career in definitions.Values.Where(career => career != null)) {
            states.TryGetValue(career.Id, out var state);
            var row = ProgressionCareerRow.FromCareer(career, state, player, log);
            if(row.joined || row.unlocked || row.canJoin || includeLockedCareers) {
                rows.Add(row);
                seenIds.Add(career.Id);
            }
        }

        foreach(var state in states.Values.Where(state => state != null && !seenIds.Contains(state.careerId))) {
            rows.Add(ProgressionCareerRow.FromState(state));
        }

        return LimitRows(rows
            .OrderByDescending(row => row.joined)
            .ThenByDescending(row => row.unlocked)
            .ThenBy(row => row.category)
            .ThenBy(row => row.displayName), maxCareerRows);
    }

    IEnumerable<ProgressionMilestoneRow> BuildMilestoneRows(PlayerMilestones log) {
        var rows = new List<ProgressionMilestoneRow>();
        var definitions = ResolveMilestonePool().ToDictionarySafe(milestone => milestone.Id);
        var completedIds = log != null
            ? new HashSet<string>(log.CompletedMilestoneIds.Where(id => !string.IsNullOrWhiteSpace(id)), StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach(var milestone in definitions.Values.Where(milestone => milestone != null)) {
            bool completed = completedIds.Contains(milestone.Id);
            if(!completed && milestone.Hidden && !includeHiddenMilestones) {
                continue;
            }

            if(!completed && !includeIncompleteMilestones) {
                continue;
            }

            rows.Add(ProgressionMilestoneRow.FromDefinition(milestone, completed));
            seenIds.Add(milestone.Id);
        }

        foreach(string completedId in completedIds.Where(id => !seenIds.Contains(id))) {
            rows.Add(ProgressionMilestoneRow.FromCompletedId(completedId));
        }

        return LimitRows(rows
            .OrderByDescending(row => row.completed)
            .ThenBy(row => row.hidden)
            .ThenBy(row => row.displayName), maxMilestoneRows);
    }

    IEnumerable<ProgressionReputationRow> BuildReputationRows(PlayerReputation log) {
        var rows = new List<ProgressionReputationRow>();
        var definitions = ResolveFactionPool().ToDictionarySafe(faction => faction.Id);
        var savedValues = log != null
            ? log.Reputations.Where(value => value != null).ToDictionarySafe(value => value.factionId)
            : new Dictionary<string, ReputationValue>(StringComparer.OrdinalIgnoreCase);
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach(var faction in definitions.Values.Where(faction => faction != null)) {
            savedValues.TryGetValue(faction.Id, out var savedValue);
            rows.Add(ProgressionReputationRow.FromFaction(faction, log, savedValue));
            seenIds.Add(faction.Id);
        }

        foreach(var value in savedValues.Values.Where(value => value != null && !seenIds.Contains(value.factionId))) {
            rows.Add(ProgressionReputationRow.FromValue(value));
        }

        return LimitRows(rows
            .OrderByDescending(row => row.knownInLog)
            .ThenBy(row => row.displayName), maxReputationRows);
    }

    IEnumerable<ProgressionAccessRow> BuildAccessRows(PlayerController player, PlayerAccessLog log) {
        var rows = new List<ProgressionAccessRow>();
        var definitions = ResolveAccessProfilePool().ToDictionarySafe(profile => profile.Id);
        var seenProfileIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if(log != null) {
            foreach(var state in log.AccessStates.Where(state => state != null)) {
                var profile = definitions.GetValueOrDefaultSafe(state.profileId);
                rows.Add(ProgressionAccessRow.FromState(state, profile, player));
                seenProfileIds.Add(state.profileId);
            }
        }

        if(includeUnusedAccessProfiles) {
            foreach(var profile in definitions.Values.Where(profile => profile != null && !seenProfileIds.Contains(profile.Id))) {
                rows.Add(ProgressionAccessRow.FromProfile(profile, player));
            }
        }

        return LimitRows(rows
            .OrderByDescending(row => row.hasHistory)
            .ThenByDescending(row => row.priority)
            .ThenBy(row => row.category)
            .ThenBy(row => row.displayName), maxAccessRows);
    }

    IEnumerable<TitleDefinition> ResolveTitlePool() {
        return MergeDefinitions(titlePool, includeResourceTitles ? Resources.LoadAll<TitleDefinition>("") : Array.Empty<TitleDefinition>(), title => title.Id);
    }

    IEnumerable<CareerPathDefinition> ResolveCareerPool() {
        return MergeDefinitions(careerPool, includeResourceCareers ? Resources.LoadAll<CareerPathDefinition>("") : Array.Empty<CareerPathDefinition>(), career => career.Id);
    }

    IEnumerable<MilestoneDefinition> ResolveMilestonePool() {
        return MergeDefinitions(milestonePool, includeResourceMilestones ? Resources.LoadAll<MilestoneDefinition>("") : Array.Empty<MilestoneDefinition>(), milestone => milestone.Id);
    }

    IEnumerable<ReputationFactionDefinition> ResolveFactionPool() {
        return MergeDefinitions(factionPool, includeResourceFactions ? Resources.LoadAll<ReputationFactionDefinition>("") : Array.Empty<ReputationFactionDefinition>(), faction => faction.Id);
    }

    IEnumerable<AccessProfileDefinition> ResolveAccessProfilePool() {
        return MergeDefinitions(accessProfilePool, includeResourceAccessProfiles ? Resources.LoadAll<AccessProfileDefinition>("") : Array.Empty<AccessProfileDefinition>(), profile => profile.Id);
    }

    TitleDefinition FindTitle(string titleId) {
        if(string.IsNullOrWhiteSpace(titleId)) {
            return null;
        }

        return ResolveTitlePool()
            .FirstOrDefault(title => title != null && string.Equals(title.Id, titleId, StringComparison.OrdinalIgnoreCase));
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

    T GetPlayerComponent<T>(PlayerController player, bool createIfMissing) where T : Component {
        if(player == null) {
            return null;
        }

        var component = player.GetComponent<T>();
        return component != null || !createIfMissing ? component : player.gameObject.AddComponent<T>();
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

    int ResolveTitleDuration(TitleDefinition title, int durationHours) {
        if(title == null || durationHours < 0 || !title.CanBeTemporary) {
            return -1;
        }

        if(durationHours > 0) {
            return durationHours;
        }

        if(title.DefaultDurationHours > 0) {
            return title.DefaultDurationHours;
        }

        return title.PermanentByDefault ? -1 : 1;
    }

    string ResolveSourceId() {
        return string.IsNullOrWhiteSpace(uiSourceId) ? "ui:progression-access" : uiSourceId;
    }

    bool Succeed(ProgressionAccessUIActionResultKind kind, string message, out string feedback) {
        feedback = message;
        SetLastResult(kind, true, message);
        if(logSuccessfulActions) {
            GameDebug.Success(message, GameDebugCategory.RPG, this, "ProgressionAccessUIManager");
        }
        return true;
    }

    bool Block(string message, out string feedback) {
        feedback = string.IsNullOrWhiteSpace(message) ? "Progression/access action was blocked." : message;
        SetLastResult(ProgressionAccessUIActionResultKind.Blocked, false, feedback);
        if(logBlockedActions) {
            GameDebug.Warning(feedback, GameDebugCategory.RPG, this, "ProgressionAccessUIManager");
        }
        return false;
    }

    void SetLastResult(ProgressionAccessUIActionResultKind kind, bool success, string message) {
        lastResult = new ProgressionAccessUIActionResult {
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
public class ProgressionAccessUIScreenSnapshot {
    [Tooltip("If enabled, a player was resolved for this snapshot.")]
    public bool hasPlayer;
    [Tooltip("Resolved player object name.")]
    public string playerName;
    [Tooltip("Source id used by UI backend actions.")]
    public string sourceId;
    [Tooltip("If enabled, PlayerTitles was found on the player.")]
    public bool hasTitleLog;
    [Tooltip("If enabled, PlayerCareerLog was found on the player.")]
    public bool hasCareerLog;
    [Tooltip("If enabled, PlayerMilestones was found on the player.")]
    public bool hasMilestoneLog;
    [Tooltip("If enabled, PlayerReputation was found on the player.")]
    public bool hasReputationLog;
    [Tooltip("If enabled, PlayerAccessLog was found on the player.")]
    public bool hasAccessLog;
    [Tooltip("Current in-game day.")]
    public int day;
    [Tooltip("Current in-game hour.")]
    public int hour;
    [Tooltip("Current absolute in-game hour.")]
    public int absoluteHour;
    [Tooltip("Number of active titles, badges, permits or licenses.")]
    public int activeTitleCount;
    [Tooltip("Number of active permanent title rows.")]
    public int permanentTitleCount;
    [Tooltip("Number of active temporary title rows.")]
    public int temporaryTitleCount;
    [Tooltip("Number of joined career paths.")]
    public int joinedCareerCount;
    [Tooltip("Number of unlocked career paths.")]
    public int unlockedCareerCount;
    [Tooltip("Number of completed milestones.")]
    public int completedMilestoneCount;
    [Tooltip("Number of reputation rows that have saved player values.")]
    public int knownReputationCount;
    [Tooltip("Number of reputation rows above 0.")]
    public int positiveReputationCount;
    [Tooltip("Number of reputation rows below 0.")]
    public int negativeReputationCount;
    [Tooltip("Total successful access checks in visible rows.")]
    public int accessPassedCount;
    [Tooltip("Total denied access checks in visible rows.")]
    public int accessDeniedCount;
    [Tooltip("Visible title, badge, permit and license rows.")]
    public List<ProgressionTitleRow> titleRows = new List<ProgressionTitleRow>();
    [Tooltip("Visible career path rows.")]
    public List<ProgressionCareerRow> careerRows = new List<ProgressionCareerRow>();
    [Tooltip("Visible milestone rows.")]
    public List<ProgressionMilestoneRow> milestoneRows = new List<ProgressionMilestoneRow>();
    [Tooltip("Visible reputation rows.")]
    public List<ProgressionReputationRow> reputationRows = new List<ProgressionReputationRow>();
    [Tooltip("Visible access profile/history rows.")]
    public List<ProgressionAccessRow> accessRows = new List<ProgressionAccessRow>();
    [Tooltip("Most recent UI backend action result.")]
    public ProgressionAccessUIActionResult lastResult;
}

[Serializable]
public class ProgressionAccessUIActionResult {
    [Tooltip("Kind of UI backend action that produced this result.")]
    public ProgressionAccessUIActionResultKind kind;
    [Tooltip("If enabled, the action succeeded or an access check passed.")]
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
public class ProgressionTitleRow {
    [Tooltip("Title, badge, permit or license id.")]
    public string titleId;
    [Tooltip("Display name shown by UI.")]
    public string displayName;
    [Tooltip("Description shown by UI.")]
    public string description;
    [Tooltip("Kind of title represented by this row.")]
    public TitleKind kind;
    [Tooltip("Free-form tags copied from the title definition.")]
    public List<string> tags = new List<string>();
    [Tooltip("If enabled, the player currently has this title.")]
    public bool active;
    [Tooltip("If enabled, this active title never expires.")]
    public bool permanent;
    [Tooltip("If enabled, the definition allows temporary grants.")]
    public bool canBeTemporary;
    [Tooltip("If enabled, this title can be granted from this UI backend.")]
    public bool canGrant;
    [Tooltip("If enabled, this title can be revoked from this UI backend.")]
    public bool canRevoke;
    [Tooltip("In-game absolute hour when this title was acquired.")]
    public int acquiredAtHour;
    [Tooltip("In-game absolute hour when this title expires. -1 means permanent.")]
    public int expiresAtHour;
    [Tooltip("Remaining in-game hours. -1 means permanent, 0 means inactive or expired.")]
    public int remainingHours;
    [Tooltip("Short source/reason stored when the title was granted.")]
    public string source;
    [Tooltip("Short text useful for placeholder UI rows.")]
    public string displayText;

    public static ProgressionTitleRow FromState(PlayerTitleState state, TitleDefinition definition, int currentAbsoluteHour) {
        string id = !string.IsNullOrWhiteSpace(state?.titleId) ? state.titleId : definition != null ? definition.Id : string.Empty;
        string name = definition != null ? definition.DisplayName : !string.IsNullOrWhiteSpace(state?.displayName) ? state.displayName : id;
        bool permanent = state != null && state.permanent;
        return new ProgressionTitleRow {
            titleId = id,
            displayName = name,
            description = definition != null ? definition.Description : string.Empty,
            kind = definition != null ? definition.Kind : state != null ? state.kind : TitleKind.Title,
            tags = definition != null ? definition.Tags.ToList() : new List<string>(),
            active = state != null,
            permanent = permanent,
            canBeTemporary = definition != null && definition.CanBeTemporary,
            canGrant = definition != null,
            canRevoke = state != null,
            acquiredAtHour = state != null ? state.acquiredAtHour : -1,
            expiresAtHour = state != null ? state.expiresAtHour : -1,
            remainingHours = state == null ? 0 : permanent ? -1 : Mathf.Max(0, state.expiresAtHour - currentAbsoluteHour),
            source = state != null ? state.source : string.Empty,
            displayText = state != null ? $"{name} - active" : $"{name} - inactive"
        };
    }

    public static ProgressionTitleRow FromDefinition(TitleDefinition definition) {
        return new ProgressionTitleRow {
            titleId = definition != null ? definition.Id : string.Empty,
            displayName = definition != null ? definition.DisplayName : string.Empty,
            description = definition != null ? definition.Description : string.Empty,
            kind = definition != null ? definition.Kind : TitleKind.Title,
            tags = definition != null ? definition.Tags.ToList() : new List<string>(),
            active = false,
            permanent = definition == null || definition.PermanentByDefault,
            canBeTemporary = definition != null && definition.CanBeTemporary,
            canGrant = definition != null,
            canRevoke = false,
            acquiredAtHour = -1,
            expiresAtHour = -1,
            remainingHours = 0,
            source = string.Empty,
            displayText = definition != null ? $"{definition.DisplayName} - inactive" : string.Empty
        };
    }
}

[Serializable]
public class ProgressionCareerRow {
    [Tooltip("Career path id.")]
    public string careerId;
    [Tooltip("Display name shown by UI.")]
    public string displayName;
    [Tooltip("Description shown by UI.")]
    public string description;
    [Tooltip("Broad career category.")]
    public CareerCategory category;
    [Tooltip("Free-form career tags.")]
    public List<string> tags = new List<string>();
    [Tooltip("Career join rule used by this path.")]
    public CareerJoinMode joinMode;
    [Tooltip("If enabled, the career is unlocked or unlocked by default.")]
    public bool unlocked;
    [Tooltip("If enabled, the player has joined this career.")]
    public bool joined;
    [Tooltip("If enabled, the player can join this career right now through this UI action.")]
    public bool canJoin;
    [Tooltip("If enabled, this career can coexist with other joined careers.")]
    public bool canRunAlongsideOtherCareers;
    [Tooltip("Current career points.")]
    public int points;
    [Tooltip("Total career points earned over the save.")]
    public int totalPointsEarned;
    [Tooltip("Current rank index. -1 means no rank reached.")]
    public int currentRankIndex;
    [Tooltip("Current rank id.")]
    public string currentRankId;
    [Tooltip("Current rank display name.")]
    public string currentRankName;
    [Tooltip("Next rank id, if any.")]
    public string nextRankId;
    [Tooltip("Next rank display name, if any.")]
    public string nextRankName;
    [Tooltip("Career points needed to reach the next rank. 0 means no next rank or already eligible.")]
    public int pointsToNextRank;
    [Tooltip("Last career points gained in one action.")]
    public int lastPointGain;
    [Tooltip("Absolute in-game hour of the last career point gain.")]
    public int lastPointGainHour;
    [Tooltip("Short source id that last changed this career.")]
    public string lastSource;
    [Tooltip("Failure reason shown when Can Join is false.")]
    public string failureMessage;
    [Tooltip("Short text useful for placeholder UI rows.")]
    public string displayText;

    public static ProgressionCareerRow FromCareer(CareerPathDefinition career, PlayerCareerState state, PlayerController player, PlayerCareerLog log) {
        bool unlocked = career != null && (career.UnlockedByDefault || (log?.HasUnlockedCareer(career) ?? false));
        bool joined = state != null && state.joined;
        string failureMessage = "Career path could not be resolved.";
        bool canJoin = career != null && career.CanJoin(player, viaMentor: false, out failureMessage);
        int points = state != null ? Mathf.Max(0, state.points) : 0;
        var currentRank = career != null ? career.GetRankForPoints(points) : null;
        var nextRank = career != null ? career.GetNextRank(points) : null;

        return new ProgressionCareerRow {
            careerId = career != null ? career.Id : state != null ? state.careerId : string.Empty,
            displayName = career != null ? career.DisplayName : state != null ? state.careerName : string.Empty,
            description = career != null ? career.Description : string.Empty,
            category = career != null ? career.Category : state != null ? state.category : CareerCategory.Trainer,
            tags = career != null ? career.Tags.ToList() : new List<string>(),
            joinMode = career != null ? career.JoinMode : CareerJoinMode.FreeJoin,
            unlocked = unlocked,
            joined = joined,
            canJoin = canJoin || joined,
            canRunAlongsideOtherCareers = career == null || career.CanRunAlongsideOtherCareers,
            points = points,
            totalPointsEarned = state != null ? Mathf.Max(0, state.totalPointsEarned) : 0,
            currentRankIndex = currentRank != null && career != null ? career.GetRankIndex(currentRank) : state != null ? state.currentRankIndex : -1,
            currentRankId = currentRank != null ? currentRank.Id : state != null ? state.currentRankId : string.Empty,
            currentRankName = currentRank != null ? currentRank.DisplayName : state != null ? state.currentRankName : string.Empty,
            nextRankId = nextRank != null ? nextRank.Id : string.Empty,
            nextRankName = nextRank != null ? nextRank.DisplayName : string.Empty,
            pointsToNextRank = nextRank != null ? Mathf.Max(0, nextRank.MinPoints - points) : 0,
            lastPointGain = state != null ? Mathf.Max(0, state.lastPointGain) : 0,
            lastPointGainHour = state != null ? state.lastPointGainHour : -1,
            lastSource = state != null ? state.lastSource : string.Empty,
            failureMessage = joined || canJoin ? string.Empty : failureMessage,
            displayText = career != null ? $"{career.DisplayName} - {(joined ? "joined" : unlocked ? "unlocked" : "locked")}" : string.Empty
        };
    }

    public static ProgressionCareerRow FromState(PlayerCareerState state) {
        return new ProgressionCareerRow {
            careerId = state != null ? state.careerId : string.Empty,
            displayName = state != null ? state.careerName : string.Empty,
            description = string.Empty,
            category = state != null ? state.category : CareerCategory.Trainer,
            tags = new List<string>(),
            joinMode = CareerJoinMode.FreeJoin,
            unlocked = state != null,
            joined = state != null && state.joined,
            canJoin = false,
            canRunAlongsideOtherCareers = true,
            points = state != null ? Mathf.Max(0, state.points) : 0,
            totalPointsEarned = state != null ? Mathf.Max(0, state.totalPointsEarned) : 0,
            currentRankIndex = state != null ? state.currentRankIndex : -1,
            currentRankId = state != null ? state.currentRankId : string.Empty,
            currentRankName = state != null ? state.currentRankName : string.Empty,
            lastPointGain = state != null ? Mathf.Max(0, state.lastPointGain) : 0,
            lastPointGainHour = state != null ? state.lastPointGainHour : -1,
            lastSource = state != null ? state.lastSource : string.Empty,
            failureMessage = "Career definition could not be resolved.",
            displayText = state != null ? $"{state.careerName} - saved state" : string.Empty
        };
    }
}

[Serializable]
public class ProgressionMilestoneRow {
    [Tooltip("Milestone id.")]
    public string milestoneId;
    [Tooltip("Display name shown by UI.")]
    public string displayName;
    [Tooltip("Description shown by UI.")]
    public string description;
    [Tooltip("If enabled, this milestone is completed by the player.")]
    public bool completed;
    [Tooltip("If enabled, this milestone is hidden until completion unless the UI manager allows hidden milestones.")]
    public bool hidden;
    [Tooltip("If enabled, the milestone can be completed from this UI backend.")]
    public bool canComplete;
    [Tooltip("Short text useful for placeholder UI rows.")]
    public string displayText;

    public static ProgressionMilestoneRow FromDefinition(MilestoneDefinition milestone, bool completed) {
        return new ProgressionMilestoneRow {
            milestoneId = milestone != null ? milestone.Id : string.Empty,
            displayName = milestone != null ? milestone.DisplayName : string.Empty,
            description = milestone != null ? milestone.Description : string.Empty,
            completed = completed,
            hidden = milestone != null && milestone.Hidden,
            canComplete = milestone != null && !completed,
            displayText = milestone != null ? $"{milestone.DisplayName} - {(completed ? "completed" : "incomplete")}" : string.Empty
        };
    }

    public static ProgressionMilestoneRow FromCompletedId(string milestoneId) {
        return new ProgressionMilestoneRow {
            milestoneId = milestoneId,
            displayName = milestoneId,
            description = string.Empty,
            completed = true,
            hidden = false,
            canComplete = false,
            displayText = $"{milestoneId} - completed"
        };
    }
}

[Serializable]
public class ProgressionReputationRow {
    [Tooltip("Reputation faction id.")]
    public string factionId;
    [Tooltip("Display name shown by UI.")]
    public string displayName;
    [Tooltip("Description shown by UI.")]
    public string description;
    [Tooltip("Current reputation value.")]
    public int value;
    [Tooltip("Default reputation value from the faction definition.")]
    public int defaultValue;
    [Tooltip("Minimum reputation value.")]
    public int minValue;
    [Tooltip("Maximum reputation value.")]
    public int maxValue;
    [Tooltip("0-1 normalized reputation value useful for meters.")]
    public float normalizedValue;
    [Tooltip("If enabled, this faction has a saved value in PlayerReputation.")]
    public bool knownInLog;
    [Tooltip("If enabled, this row can be changed from this UI backend.")]
    public bool canChange;
    [Tooltip("Short text useful for placeholder UI rows.")]
    public string displayText;

    public static ProgressionReputationRow FromFaction(ReputationFactionDefinition faction, PlayerReputation log, ReputationValue savedValue) {
        int value = log != null ? log.GetReputation(faction) : faction != null ? faction.DefaultValue : 0;
        int min = faction != null ? faction.MinValue : -100;
        int max = faction != null ? faction.MaxValue : 100;
        return new ProgressionReputationRow {
            factionId = faction != null ? faction.Id : string.Empty,
            displayName = faction != null ? faction.DisplayName : string.Empty,
            description = faction != null ? faction.Description : string.Empty,
            value = value,
            defaultValue = faction != null ? faction.DefaultValue : 0,
            minValue = min,
            maxValue = max,
            normalizedValue = Mathf.InverseLerp(min, max, value),
            knownInLog = savedValue != null,
            canChange = faction != null,
            displayText = faction != null ? $"{faction.DisplayName}: {value}" : string.Empty
        };
    }

    public static ProgressionReputationRow FromValue(ReputationValue value) {
        int currentValue = value != null ? value.value : 0;
        return new ProgressionReputationRow {
            factionId = value != null ? value.factionId : string.Empty,
            displayName = value != null ? value.factionId : string.Empty,
            description = string.Empty,
            value = currentValue,
            defaultValue = 0,
            minValue = -100,
            maxValue = 100,
            normalizedValue = Mathf.InverseLerp(-100, 100, currentValue),
            knownInLog = value != null,
            canChange = false,
            displayText = value != null ? $"{value.factionId}: {currentValue}" : string.Empty
        };
    }
}

[Serializable]
public class ProgressionAccessRow {
    [Tooltip("Access profile id.")]
    public string profileId;
    [Tooltip("Display name shown by UI.")]
    public string displayName;
    [Tooltip("Description shown by UI.")]
    public string description;
    [Tooltip("Broad access category.")]
    public AccessProfileCategory category;
    [Tooltip("Free-form access tags.")]
    public List<string> tags = new List<string>();
    [Tooltip("Profile priority used by access/profile ordering.")]
    public int priority;
    [Tooltip("Optional source/gate/context id where this access profile was checked.")]
    public string contextId;
    [Tooltip("If enabled, this row has saved access history.")]
    public bool hasHistory;
    [Tooltip("If enabled, the player passes this access profile right now.")]
    public bool canAccessNow;
    [Tooltip("Number of successful checks.")]
    public int passedCount;
    [Tooltip("Number of denied checks.")]
    public int deniedCount;
    [Tooltip("In-game day of the most recent check.")]
    public int lastCheckedDay;
    [Tooltip("Absolute in-game hour of the most recent check.")]
    public int lastCheckedAbsoluteHour;
    [Tooltip("In-game day of the most recent successful check.")]
    public int lastPassedDay;
    [Tooltip("In-game day of the most recent denied check.")]
    public int lastDeniedDay;
    [Tooltip("Last message produced by this profile.")]
    public string lastMessage;
    [Tooltip("Last reason access was denied.")]
    public string lastDeniedReason;
    [Tooltip("Failure reason shown when Can Access Now is false.")]
    public string failureMessage;
    [Tooltip("Short text useful for placeholder UI rows.")]
    public string displayText;

    public static ProgressionAccessRow FromState(PlayerAccessState state, AccessProfileDefinition profile, PlayerController player) {
        string failureMessage = "Access profile could not be resolved.";
        bool canAccess = profile != null && profile.CanAccess(player, out failureMessage);
        string name = profile != null ? profile.DisplayName : !string.IsNullOrWhiteSpace(state?.profileName) ? state.profileName : state?.profileId;
        return new ProgressionAccessRow {
            profileId = profile != null ? profile.Id : state != null ? state.profileId : string.Empty,
            displayName = name,
            description = profile != null ? profile.Description : string.Empty,
            category = profile != null ? profile.Category : state != null ? state.category : AccessProfileCategory.General,
            tags = profile != null ? profile.Tags.ToList() : new List<string>(),
            priority = profile != null ? profile.Priority : 0,
            contextId = state != null ? state.contextId : string.Empty,
            hasHistory = state != null,
            canAccessNow = canAccess,
            passedCount = state != null ? Mathf.Max(0, state.passedCount) : 0,
            deniedCount = state != null ? Mathf.Max(0, state.deniedCount) : 0,
            lastCheckedDay = state != null ? state.lastCheckedDay : -1,
            lastCheckedAbsoluteHour = state != null ? state.lastCheckedAbsoluteHour : -1,
            lastPassedDay = state != null ? state.lastPassedDay : -1,
            lastDeniedDay = state != null ? state.lastDeniedDay : -1,
            lastMessage = state != null ? state.lastMessage : string.Empty,
            lastDeniedReason = state != null ? state.lastDeniedReason : string.Empty,
            failureMessage = canAccess ? string.Empty : failureMessage,
            displayText = !string.IsNullOrWhiteSpace(name) ? $"{name} - {(canAccess ? "allowed" : "locked")}" : string.Empty
        };
    }

    public static ProgressionAccessRow FromProfile(AccessProfileDefinition profile, PlayerController player) {
        string failureMessage = "Access profile could not be resolved.";
        bool canAccess = profile != null && profile.CanAccess(player, out failureMessage);
        return new ProgressionAccessRow {
            profileId = profile != null ? profile.Id : string.Empty,
            displayName = profile != null ? profile.DisplayName : string.Empty,
            description = profile != null ? profile.Description : string.Empty,
            category = profile != null ? profile.Category : AccessProfileCategory.General,
            tags = profile != null ? profile.Tags.ToList() : new List<string>(),
            priority = profile != null ? profile.Priority : 0,
            contextId = string.Empty,
            hasHistory = false,
            canAccessNow = canAccess,
            passedCount = 0,
            deniedCount = 0,
            lastCheckedDay = -1,
            lastCheckedAbsoluteHour = -1,
            lastPassedDay = -1,
            lastDeniedDay = -1,
            lastMessage = string.Empty,
            lastDeniedReason = string.Empty,
            failureMessage = canAccess ? string.Empty : failureMessage,
            displayText = profile != null ? $"{profile.DisplayName} - {(canAccess ? "allowed" : "locked")}" : string.Empty
        };
    }
}

static class ProgressionAccessUIEnumerableExtensions {
    public static Dictionary<string, T> ToDictionarySafe<T>(this IEnumerable<T> source, Func<T, string> idSelector) {
        var dictionary = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
        if(source == null || idSelector == null) {
            return dictionary;
        }

        foreach(var item in source) {
            if(item == null) {
                continue;
            }

            string id = idSelector(item);
            if(string.IsNullOrWhiteSpace(id) || dictionary.ContainsKey(id)) {
                continue;
            }

            dictionary.Add(id, item);
        }

        return dictionary;
    }

    public static T GetValueOrDefaultSafe<T>(this Dictionary<string, T> dictionary, string key) {
        if(dictionary == null || string.IsNullOrWhiteSpace(key)) {
            return default;
        }

        return dictionary.TryGetValue(key, out var value) ? value : default;
    }
}
