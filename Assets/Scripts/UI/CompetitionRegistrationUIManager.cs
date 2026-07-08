using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum CompetitionRegistrationUIActionResultKind {
    None,
    Refreshed,
    Registered,
    InvitationGranted,
    VenueEntered,
    MatchPrepared,
    Blocked
}

public class CompetitionRegistrationUIManager : MonoBehaviour {
    [Header("Player")]
    [Tooltip("Player whose competition registration, invitation, venue and bracket state is shown. Empty uses PlayerController.i or the first PlayerController in the scene.")]
    [SerializeField] PlayerController playerOverride = null;
    [Tooltip("If enabled, missing player log components are created when UI actions need them.")]
    [SerializeField] bool createMissingLogsForActions = true;

    [Header("Sources")]
    [Tooltip("Optional scene/NPC registration source used by register actions and as a primary registration context.")]
    [SerializeField] CompetitionRegistrationSource registrationSource = null;
    [Tooltip("Optional bracket source used by Prepare Match actions. Empty uses the registration source bracket source when available.")]
    [SerializeField] CompetitionBracketSource bracketSource = null;
    [Tooltip("Optional registration used to evaluate venue compatibility when no registration source is assigned.")]
    [SerializeField] CompetitionRegistrationDefinition registrationContext = null;
    [Tooltip("If enabled, Register actions use the assigned registration source when the selected registration matches that source.")]
    [SerializeField] bool useRegistrationSourceForMatchingActions = true;
    [Tooltip("If enabled, a successful Register action also asks the resolved bracket source to prepare the next player match.")]
    [SerializeField] bool prepareMatchAfterRegistration;

    [Header("Registration Pool")]
    [Tooltip("Competition registrations explicitly shown by this UI. Empty can still read Resources when Include Resource Registrations is enabled.")]
    [SerializeField] List<CompetitionRegistrationDefinition> registrationPool = new List<CompetitionRegistrationDefinition>();
    [Tooltip("If enabled, all CompetitionRegistrationDefinition assets in Resources are added to the registration pool.")]
    [SerializeField] bool includeResourceRegistrations = true;

    [Header("Invitation Pool")]
    [Tooltip("Competition invitations, qualifier passes and wildcards explicitly shown by this UI. Empty can still read Resources when Include Resource Invitations is enabled.")]
    [SerializeField] List<CompetitionInvitationDefinition> invitationPool = new List<CompetitionInvitationDefinition>();
    [Tooltip("If enabled, all CompetitionInvitationDefinition assets in Resources are added to the invitation pool.")]
    [SerializeField] bool includeResourceInvitations = true;

    [Header("Venue Pool")]
    [Tooltip("Competition venues, gyms, stadiums or facilities explicitly shown by this UI. Empty can still read Resources when Include Resource Venues is enabled.")]
    [SerializeField] List<CompetitionVenueDefinition> venuePool = new List<CompetitionVenueDefinition>();
    [Tooltip("If enabled, all CompetitionVenueDefinition assets in Resources are added to the venue pool.")]
    [SerializeField] bool includeResourceVenues = true;

    [Header("Visibility")]
    [Tooltip("If enabled, registrations that are currently blocked remain visible with a failure reason.")]
    [SerializeField] bool includeBlockedRegistrations = true;
    [Tooltip("If enabled, invitations the player does not own remain visible if they are known in the pool.")]
    [SerializeField] bool includeUnownedInvitations = true;
    [Tooltip("If enabled, venues that cannot currently be entered or host the context registration remain visible with a failure reason.")]
    [SerializeField] bool includeBlockedVenues = true;
    [Tooltip("If enabled, registration history rows are included in the snapshot.")]
    [SerializeField] bool includeRegistrationHistory = true;
    [Tooltip("If enabled, active and recent bracket rows are included in the snapshot.")]
    [SerializeField] bool includeBracketSummary = true;
    [Tooltip("Maximum registration option rows copied into the snapshot. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxRegistrationRows = 30;
    [Tooltip("Maximum invitation rows copied into the snapshot. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxInvitationRows = 30;
    [Tooltip("Maximum venue rows copied into the snapshot. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxVenueRows = 30;
    [Tooltip("Maximum registration history rows copied into the snapshot. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxHistoryRows = 30;
    [Tooltip("Maximum bracket summary rows copied into the snapshot. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxBracketRows = 20;

    [Header("Snapshot")]
    [Tooltip("If enabled, Refresh is called when this component starts.")]
    [SerializeField] bool refreshOnStart = true;
    [Tooltip("If enabled, Refresh is called after every successful or blocked action.")]
    [SerializeField] bool refreshAfterActions = true;
    [Tooltip("Source id written into registration, invitation, venue and bracket logs when this UI performs actions.")]
    [SerializeField] string uiSourceId = "ui:competition-registration";

    [Header("Debug")]
    [Tooltip("If enabled, successful UI backend actions are written to GameDebug.")]
    [SerializeField] bool logSuccessfulActions;
    [Tooltip("If enabled, blocked UI backend actions are written to GameDebug.")]
    [SerializeField] bool logBlockedActions = true;

    CompetitionRegistrationUIScreenSnapshot currentSnapshot = new CompetitionRegistrationUIScreenSnapshot();
    CompetitionRegistrationUIActionResult lastResult = new CompetitionRegistrationUIActionResult();

    public CompetitionRegistrationUIScreenSnapshot CurrentSnapshot => currentSnapshot;
    public CompetitionRegistrationUIActionResult LastResult => lastResult;
    public PlayerController PlayerOverride => playerOverride;
    public bool CreateMissingLogsForActions => createMissingLogsForActions;
    public CompetitionRegistrationSource RegistrationSource => registrationSource;
    public CompetitionBracketSource BracketSource => bracketSource;
    public CompetitionRegistrationDefinition RegistrationContext => registrationContext;
    public bool UseRegistrationSourceForMatchingActions => useRegistrationSourceForMatchingActions;
    public bool PrepareMatchAfterRegistration => prepareMatchAfterRegistration;
    public IReadOnlyList<CompetitionRegistrationDefinition> RegistrationPool => registrationPool;
    public bool IncludeResourceRegistrations => includeResourceRegistrations;
    public IReadOnlyList<CompetitionInvitationDefinition> InvitationPool => invitationPool;
    public bool IncludeResourceInvitations => includeResourceInvitations;
    public IReadOnlyList<CompetitionVenueDefinition> VenuePool => venuePool;
    public bool IncludeResourceVenues => includeResourceVenues;
    public bool IncludeBlockedRegistrations => includeBlockedRegistrations;
    public bool IncludeUnownedInvitations => includeUnownedInvitations;
    public bool IncludeBlockedVenues => includeBlockedVenues;
    public event Action<CompetitionRegistrationUIScreenSnapshot> OnSnapshotChanged;
    public event Action<CompetitionRegistrationUIActionResult> OnActionResult;

    void Start() {
        if(refreshOnStart) {
            Refresh();
        }
    }

    [ContextMenu("Refresh Competition Registration Snapshot")]
    public CompetitionRegistrationUIScreenSnapshot RefreshFromContextMenu() {
        return Refresh();
    }

    public CompetitionRegistrationUIScreenSnapshot Refresh() {
        var player = ResolvePlayer();
        var registrationLog = player != null ? player.GetComponent<PlayerCompetitionRegistrationLog>() : null;
        var invitationLog = player != null ? player.GetComponent<PlayerCompetitionInvitationLog>() : null;
        var venueLog = player != null ? player.GetComponent<PlayerCompetitionVenueLog>() : null;
        var bracketLog = player != null ? player.GetComponent<PlayerCompetitionBracketLog>() : null;

        var registrationRows = BuildRegistrationRows(player, registrationLog, bracketLog).ToList();
        var invitationRows = BuildInvitationRows(player, invitationLog).ToList();
        var venueRows = BuildVenueRows(player, venueLog).ToList();
        var historyRows = includeRegistrationHistory ? BuildHistoryRows(registrationLog).ToList() : new List<CompetitionRegistrationHistoryRow>();
        var bracketRows = includeBracketSummary ? BuildBracketRows(bracketLog).ToList() : new List<CompetitionBracketSummaryRow>();

        currentSnapshot = new CompetitionRegistrationUIScreenSnapshot {
            hasPlayer = player != null,
            playerName = player != null ? player.name : string.Empty,
            sourceId = ResolveSourceId(),
            sourceName = registrationSource != null ? registrationSource.name : name,
            hasRegistrationSource = registrationSource != null,
            hasBracketSource = ResolveBracketSource() != null,
            hasRegistrationLog = registrationLog != null,
            hasInvitationLog = invitationLog != null,
            hasVenueLog = venueLog != null,
            hasBracketLog = bracketLog != null,
            day = TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1,
            hour = TimeSystem.i != null ? Mathf.Clamp(TimeSystem.i.Hour, 0, 23) : 0,
            absoluteHour = GetCurrentAbsoluteHour(),
            registrationCount = registrationRows.Count,
            availableRegistrationCount = registrationRows.Count(row => row != null && row.canRegister),
            blockedRegistrationCount = registrationRows.Count(row => row != null && !row.canRegister),
            ownedInvitationCount = invitationRows.Count(row => row != null && row.owned),
            usableInvitationCount = invitationRows.Count(row => row != null && row.usable),
            availableVenueCount = venueRows.Count(row => row != null && row.canEnter && row.canHostContextRegistration),
            usedVenueCount = venueRows.Count(row => row != null && row.successfulUseCount > 0),
            registrationHistoryCount = historyRows.Count,
            activeBracketCount = bracketRows.Count(row => row != null && row.active),
            completedBracketCount = bracketRows.Count(row => row != null && row.completed),
            registrationRows = registrationRows,
            invitationRows = invitationRows,
            venueRows = venueRows,
            historyRows = historyRows,
            bracketRows = bracketRows,
            lastResult = lastResult
        };

        OnSnapshotChanged?.Invoke(currentSnapshot);
        return currentSnapshot;
    }

    public bool TryRegister(CompetitionRegistrationDefinition registration, out string feedback) {
        var player = ResolvePlayer();
        if(player == null) {
            return Block("A player is required to register for competitions.", out feedback);
        }

        registration = registration != null ? registration : ResolvePrimaryRegistration();
        if(registration == null) {
            return Block("No competition registration was provided.", out feedback);
        }

        EnsureActionLogs(player);

        bool success;
        if(useRegistrationSourceForMatchingActions && registrationSource != null && registrationSource.Registration == registration) {
            success = registrationSource.TryRegister(player, out feedback);
        } else {
            success = registration.TryRegister(player, ResolveSourceId(), out _, out feedback);
        }

        if(!success) {
            return Block(feedback, out feedback);
        }

        if(prepareMatchAfterRegistration) {
            var resolvedBracketSource = ResolveBracketSource();
            if(resolvedBracketSource != null) {
                resolvedBracketSource.TryPrepareNextMatch(player, out _);
            }
        }

        return Succeed(CompetitionRegistrationUIActionResultKind.Registered, $"{registration.DisplayName} registered.", out feedback);
    }

    public bool TryRegisterById(string registrationId, out string feedback) {
        return TryRegister(FindRegistration(registrationId), out feedback);
    }

    public bool TryGrantInvitation(CompetitionInvitationDefinition invitation, out string feedback) {
        var player = ResolvePlayer();
        if(player == null) {
            return Block("A player is required to grant competition invitations.", out feedback);
        }

        if(invitation == null) {
            return Block("No competition invitation was provided.", out feedback);
        }

        EnsureActionLogs(player);
        if(invitation.TryGrant(player, ResolveSourceId(), out _, out feedback)) {
            return Succeed(CompetitionRegistrationUIActionResultKind.InvitationGranted, $"{invitation.DisplayName} granted.", out feedback);
        }

        return Block(feedback, out feedback);
    }

    public bool TryEnterVenue(CompetitionVenueDefinition venue, out string feedback) {
        var player = ResolvePlayer();
        if(player == null) {
            return Block("A player is required to enter competition venues.", out feedback);
        }

        if(venue == null) {
            return Block("No competition venue was provided.", out feedback);
        }

        EnsureActionLogs(player);
        bool canEnter = venue.CanEnter(player, out feedback);
        venue.RecordUse(player, CompetitionVenuePurpose.Enter, ResolvePrimaryRegistration(), ResolvePrimaryRegistration()?.Roster, ResolveSourceId(), this, !canEnter, feedback);

        return canEnter
            ? Succeed(CompetitionRegistrationUIActionResultKind.VenueEntered, $"{venue.DisplayName} entered.", out feedback)
            : Block(feedback, out feedback);
    }

    public bool TryPrepareNextMatch(out string feedback) {
        var player = ResolvePlayer();
        if(player == null) {
            return Block("A player is required to prepare competition matches.", out feedback);
        }

        var resolvedBracketSource = ResolveBracketSource();
        if(resolvedBracketSource == null) {
            return Block("No competition bracket source is assigned.", out feedback);
        }

        EnsureActionLogs(player);
        if(resolvedBracketSource.TryPrepareNextMatch(player, out feedback)) {
            return Succeed(CompetitionRegistrationUIActionResultKind.MatchPrepared, "Next competition match prepared.", out feedback);
        }

        return Block(feedback, out feedback);
    }

    public CompetitionRegistrationOptionRow FindRegistrationRow(string registrationId) {
        return currentSnapshot?.registrationRows?
            .FirstOrDefault(row => row != null && string.Equals(row.registrationId, registrationId, StringComparison.OrdinalIgnoreCase));
    }

    public CompetitionInvitationOptionRow FindInvitationRow(string invitationId) {
        return currentSnapshot?.invitationRows?
            .FirstOrDefault(row => row != null && string.Equals(row.invitationId, invitationId, StringComparison.OrdinalIgnoreCase));
    }

    public CompetitionVenueOptionRow FindVenueRow(string venueId) {
        return currentSnapshot?.venueRows?
            .FirstOrDefault(row => row != null && string.Equals(row.venueId, venueId, StringComparison.OrdinalIgnoreCase));
    }

    IEnumerable<CompetitionRegistrationOptionRow> BuildRegistrationRows(PlayerController player, PlayerCompetitionRegistrationLog registrationLog, PlayerCompetitionBracketLog bracketLog) {
        var rows = ResolveRegistrationPool()
            .Where(registration => registration != null)
            .Select(registration => CompetitionRegistrationOptionRow.FromRegistration(registration, player, registrationLog, bracketLog, ResolveSourceId()))
            .Where(row => row != null && (includeBlockedRegistrations || row.canRegister))
            .OrderByDescending(row => row.canRegister)
            .ThenBy(row => row.competitionName)
            .ThenBy(row => row.displayName);

        return LimitRows(rows, maxRegistrationRows);
    }

    IEnumerable<CompetitionInvitationOptionRow> BuildInvitationRows(PlayerController player, PlayerCompetitionInvitationLog invitationLog) {
        var definitions = ResolveInvitationPool().ToCompetitionDictionarySafe(invitation => invitation.Id);
        var records = invitationLog != null
            ? invitationLog.Invitations.Where(record => record != null).ToCompetitionDictionarySafe(record => record.invitationId)
            : new Dictionary<string, PlayerCompetitionInvitationRecord>(StringComparer.OrdinalIgnoreCase);

        var rows = new List<CompetitionInvitationOptionRow>();
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach(var definition in definitions.Values.Where(invitation => invitation != null)) {
            records.TryGetValue(definition.Id, out var record);
            var row = CompetitionInvitationOptionRow.FromDefinition(definition, record, player, GetCurrentAbsoluteHour());
            if(includeUnownedInvitations || row.owned) {
                rows.Add(row);
                seenIds.Add(definition.Id);
            }
        }

        foreach(var record in records.Values.Where(record => record != null && !seenIds.Contains(record.invitationId))) {
            rows.Add(CompetitionInvitationOptionRow.FromRecord(record, GetCurrentAbsoluteHour()));
        }

        return LimitRows(rows
            .OrderByDescending(row => row.usable)
            .ThenByDescending(row => row.owned)
            .ThenBy(row => row.kind)
            .ThenBy(row => row.displayName), maxInvitationRows);
    }

    IEnumerable<CompetitionVenueOptionRow> BuildVenueRows(PlayerController player, PlayerCompetitionVenueLog venueLog) {
        var contextRegistration = ResolvePrimaryRegistration();
        var rows = ResolveVenuePool()
            .Where(venue => venue != null)
            .Select(venue => CompetitionVenueOptionRow.FromVenue(venue, player, venueLog, contextRegistration, ResolveSourceId()))
            .Where(row => row != null && (includeBlockedVenues || (row.canEnter && row.canHostContextRegistration)))
            .OrderByDescending(row => row.canEnter && row.canHostContextRegistration)
            .ThenBy(row => row.kind)
            .ThenBy(row => row.displayName);

        return LimitRows(rows, maxVenueRows);
    }

    IEnumerable<CompetitionRegistrationHistoryRow> BuildHistoryRows(PlayerCompetitionRegistrationLog registrationLog) {
        var rows = registrationLog != null
            ? registrationLog.RegistrationHistory
                .Where(record => record != null)
                .OrderByDescending(record => record.registeredTotalHour)
                .Select(CompetitionRegistrationHistoryRow.FromRecord)
            : Enumerable.Empty<CompetitionRegistrationHistoryRow>();

        return LimitRows(rows, maxHistoryRows);
    }

    IEnumerable<CompetitionBracketSummaryRow> BuildBracketRows(PlayerCompetitionBracketLog bracketLog) {
        var rows = bracketLog != null
            ? bracketLog.BracketStates
                .Where(state => state != null)
                .OrderByDescending(state => state.active)
                .ThenByDescending(state => state.generatedTotalHour)
                .Select(CompetitionBracketSummaryRow.FromState)
            : Enumerable.Empty<CompetitionBracketSummaryRow>();

        return LimitRows(rows, maxBracketRows);
    }

    IEnumerable<CompetitionRegistrationDefinition> ResolveRegistrationPool() {
        var explicitItems = new List<CompetitionRegistrationDefinition>();
        if(registrationSource != null && registrationSource.Registration != null) {
            explicitItems.Add(registrationSource.Registration);
        }

        if(registrationContext != null) {
            explicitItems.Add(registrationContext);
        }

        if(registrationPool != null) {
            explicitItems.AddRange(registrationPool);
        }

        return MergeDefinitions(explicitItems, includeResourceRegistrations ? Resources.LoadAll<CompetitionRegistrationDefinition>("") : Array.Empty<CompetitionRegistrationDefinition>(), registration => registration.Id);
    }

    IEnumerable<CompetitionInvitationDefinition> ResolveInvitationPool() {
        return MergeDefinitions(invitationPool, includeResourceInvitations ? Resources.LoadAll<CompetitionInvitationDefinition>("") : Array.Empty<CompetitionInvitationDefinition>(), invitation => invitation.Id);
    }

    IEnumerable<CompetitionVenueDefinition> ResolveVenuePool() {
        return MergeDefinitions(venuePool, includeResourceVenues ? Resources.LoadAll<CompetitionVenueDefinition>("") : Array.Empty<CompetitionVenueDefinition>(), venue => venue.Id);
    }

    CompetitionRegistrationDefinition FindRegistration(string registrationId) {
        if(string.IsNullOrWhiteSpace(registrationId)) {
            return null;
        }

        return ResolveRegistrationPool()
            .FirstOrDefault(registration => registration != null && string.Equals(registration.Id, registrationId, StringComparison.OrdinalIgnoreCase));
    }

    CompetitionRegistrationDefinition ResolvePrimaryRegistration() {
        if(registrationContext != null) {
            return registrationContext;
        }

        return registrationSource != null ? registrationSource.Registration : null;
    }

    CompetitionBracketSource ResolveBracketSource() {
        return bracketSource != null ? bracketSource : registrationSource != null ? registrationSource.BracketSource : null;
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

    void EnsureActionLogs(PlayerController player) {
        if(player == null || !createMissingLogsForActions) {
            return;
        }

        AddMissingComponent<PlayerCompetitionRegistrationLog>(player);
        AddMissingComponent<PlayerCompetitionInvitationLog>(player);
        AddMissingComponent<PlayerCompetitionVenueLog>(player);
        AddMissingComponent<PlayerCompetitionBracketLog>(player);
    }

    T AddMissingComponent<T>(PlayerController player) where T : Component {
        var component = player.GetComponent<T>();
        return component != null ? component : player.gameObject.AddComponent<T>();
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

    string ResolveSourceId() {
        return string.IsNullOrWhiteSpace(uiSourceId) ? "ui:competition-registration" : uiSourceId;
    }

    bool Succeed(CompetitionRegistrationUIActionResultKind kind, string message, out string feedback) {
        feedback = message;
        SetLastResult(kind, true, message);
        if(logSuccessfulActions) {
            GameDebug.Success(message, GameDebugCategory.BattleRule, this, "CompetitionRegistrationUIManager");
        }
        return true;
    }

    bool Block(string message, out string feedback) {
        feedback = string.IsNullOrWhiteSpace(message) ? "Competition registration action was blocked." : message;
        SetLastResult(CompetitionRegistrationUIActionResultKind.Blocked, false, feedback);
        if(logBlockedActions) {
            GameDebug.Warning(feedback, GameDebugCategory.BattleRule, this, "CompetitionRegistrationUIManager");
        }
        return false;
    }

    void SetLastResult(CompetitionRegistrationUIActionResultKind kind, bool success, string message) {
        lastResult = new CompetitionRegistrationUIActionResult {
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
public class CompetitionRegistrationUIScreenSnapshot {
    [Tooltip("If enabled, a player was resolved for this snapshot.")]
    public bool hasPlayer;
    [Tooltip("Resolved player object name.")]
    public string playerName;
    [Tooltip("Source id used by UI backend actions.")]
    public string sourceId;
    [Tooltip("Display/source object name shown by placeholder UI.")]
    public string sourceName;
    [Tooltip("If enabled, a CompetitionRegistrationSource is assigned.")]
    public bool hasRegistrationSource;
    [Tooltip("If enabled, a CompetitionBracketSource is available for Prepare Match actions.")]
    public bool hasBracketSource;
    [Tooltip("If enabled, PlayerCompetitionRegistrationLog was found on the player.")]
    public bool hasRegistrationLog;
    [Tooltip("If enabled, PlayerCompetitionInvitationLog was found on the player.")]
    public bool hasInvitationLog;
    [Tooltip("If enabled, PlayerCompetitionVenueLog was found on the player.")]
    public bool hasVenueLog;
    [Tooltip("If enabled, PlayerCompetitionBracketLog was found on the player.")]
    public bool hasBracketLog;
    [Tooltip("Current in-game day.")]
    public int day;
    [Tooltip("Current in-game hour.")]
    public int hour;
    [Tooltip("Current absolute in-game hour.")]
    public int absoluteHour;
    [Tooltip("Visible registration option count.")]
    public int registrationCount;
    [Tooltip("Registrations the player can enter right now.")]
    public int availableRegistrationCount;
    [Tooltip("Registrations visible but blocked.")]
    public int blockedRegistrationCount;
    [Tooltip("Visible invitations owned by the player.")]
    public int ownedInvitationCount;
    [Tooltip("Visible invitations usable right now.")]
    public int usableInvitationCount;
    [Tooltip("Visible venues that can be entered and can host the context registration.")]
    public int availableVenueCount;
    [Tooltip("Visible venues with at least one successful use record.")]
    public int usedVenueCount;
    [Tooltip("Visible registration history row count.")]
    public int registrationHistoryCount;
    [Tooltip("Visible active bracket count.")]
    public int activeBracketCount;
    [Tooltip("Visible completed bracket count.")]
    public int completedBracketCount;
    [Tooltip("Visible competition registration rows.")]
    public List<CompetitionRegistrationOptionRow> registrationRows = new List<CompetitionRegistrationOptionRow>();
    [Tooltip("Visible invitation, qualifier pass and wildcard rows.")]
    public List<CompetitionInvitationOptionRow> invitationRows = new List<CompetitionInvitationOptionRow>();
    [Tooltip("Visible venue, arena, gym and stadium rows.")]
    public List<CompetitionVenueOptionRow> venueRows = new List<CompetitionVenueOptionRow>();
    [Tooltip("Recent competition registration history rows.")]
    public List<CompetitionRegistrationHistoryRow> historyRows = new List<CompetitionRegistrationHistoryRow>();
    [Tooltip("Active or recent bracket summary rows.")]
    public List<CompetitionBracketSummaryRow> bracketRows = new List<CompetitionBracketSummaryRow>();
    [Tooltip("Most recent UI backend action result.")]
    public CompetitionRegistrationUIActionResult lastResult;
}

[Serializable]
public class CompetitionRegistrationUIActionResult {
    [Tooltip("Kind of UI backend action that produced this result.")]
    public CompetitionRegistrationUIActionResultKind kind;
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
public class CompetitionRegistrationOptionRow {
    [Tooltip("Registration definition id.")]
    public string registrationId;
    [Tooltip("Display name shown by UI.")]
    public string displayName;
    [Tooltip("Description shown by UI.")]
    public string description;
    [Tooltip("Free-form registration tags.")]
    public List<string> tags = new List<string>();
    [Tooltip("Roster id this registration enters.")]
    public string rosterId;
    [Tooltip("Roster display name this registration enters.")]
    public string rosterName;
    [Tooltip("Competition id connected to this registration.")]
    public string competitionId;
    [Tooltip("Competition display name connected to this registration.")]
    public string competitionName;
    [Tooltip("Season id connected to this registration.")]
    public string seasonId;
    [Tooltip("Season display name connected to this registration.")]
    public string seasonName;
    [Tooltip("Ranking id connected to this registration.")]
    public string rankingId;
    [Tooltip("Ranking display name connected to this registration.")]
    public string rankingName;
    [Tooltip("Repeat rule used by this registration.")]
    public CompetitionRegistrationRepeatMode repeatMode;
    [Tooltip("Window rule used by this registration.")]
    public CompetitionRegistrationWindowMode windowMode;
    [Tooltip("Invitation rule used by this registration.")]
    public CompetitionRegistrationInvitationMode invitationMode;
    [Tooltip("Venue rule used by this registration.")]
    public CompetitionRegistrationVenueMode venueMode;
    [Tooltip("Money required for this registration.")]
    public float moneyCost;
    [Tooltip("Number of item cost entries with a valid item/count.")]
    public int itemCostCount;
    [Tooltip("If enabled, registration can be performed right now.")]
    public bool canRegister;
    [Tooltip("If enabled, the linked roster can generate a bracket right now.")]
    public bool canGenerateRoster;
    [Tooltip("If enabled, the registration has at least one open registration window or is always open.")]
    public bool hasOpenWindow;
    [Tooltip("Number of open windows found for this registration.")]
    public int openWindowCount;
    [Tooltip("Resolved open window id, if any.")]
    public string openWindowId;
    [Tooltip("Resolved open window display name, if any.")]
    public string openWindowName;
    [Tooltip("Resolved usable invitation id, if any.")]
    public string invitationId;
    [Tooltip("Resolved usable invitation display name, if any.")]
    public string invitationName;
    [Tooltip("Resolved venue id, if any.")]
    public string venueId;
    [Tooltip("Resolved venue display name, if any.")]
    public string venueName;
    [Tooltip("If enabled, successful registration can generate a bracket immediately.")]
    public bool generateBracketOnRegister;
    [Tooltip("If enabled, an active bracket already exists for this roster.")]
    public bool hasActiveBracket;
    [Tooltip("Number of previous registrations for this definition.")]
    public int registrationCount;
    [Tooltip("Repeat/cooldown context key that would be used for this registration.")]
    public string contextKey;
    [Tooltip("Remaining cooldown hours for this registration context.")]
    public int remainingCooldownHours;
    [Tooltip("Failure reason shown when Can Register is false.")]
    public string failureMessage;
    [Tooltip("Short text useful for placeholder UI rows.")]
    public string displayText;

    public static CompetitionRegistrationOptionRow FromRegistration(
        CompetitionRegistrationDefinition registration,
        PlayerController player,
        PlayerCompetitionRegistrationLog registrationLog,
        PlayerCompetitionBracketLog bracketLog,
        string sourceId) {
        string failureMessage = "Registration could not be resolved.";
        bool canRegister = registration != null && registration.CanRegister(player, out failureMessage);
        bool canGenerateRoster = registration != null && (registration.Roster == null || registration.Roster.CanGenerate(player, out _));
        bool hasActiveBracket = registration?.Roster != null && bracketLog != null && bracketLog.GetActiveBracket(registration.Roster) != null;
        var openWindows = registration != null ? registration.GetOpenWindows(player) : new List<CompetitionRegistrationWindowDefinition>();
        CompetitionRegistrationWindowDefinition resolvedWindow = null;
        CompetitionVenueDefinition resolvedVenue = null;
        CompetitionInvitationDefinition resolvedInvitation = null;
        if(registration != null) {
            registration.TryResolveOpenWindow(player, out resolvedWindow, out _);
            registration.TryResolveVenue(player, out resolvedVenue, out _);
            registration.TryResolveInvitation(player, resolvedWindow, out resolvedInvitation, out _);
        }

        var context = registration != null
            ? new CompetitionRegistrationContext(registration, player, sourceId, 0f, resolvedWindow, resolvedInvitation, resolvedVenue)
            : null;
        string contextKey = registration != null ? context.BuildContextKey(registration.RepeatMode) : string.Empty;

        return new CompetitionRegistrationOptionRow {
            registrationId = registration != null ? registration.Id : string.Empty,
            displayName = registration != null ? registration.DisplayName : string.Empty,
            description = registration != null ? registration.Description : string.Empty,
            tags = registration != null ? registration.Tags.ToList() : new List<string>(),
            rosterId = registration?.Roster != null ? registration.Roster.Id : string.Empty,
            rosterName = registration?.Roster != null ? registration.Roster.DisplayName : string.Empty,
            competitionId = registration?.Competition != null ? registration.Competition.Id : string.Empty,
            competitionName = registration?.Competition != null ? registration.Competition.DisplayName : string.Empty,
            seasonId = registration?.Season != null ? registration.Season.Id : string.Empty,
            seasonName = registration?.Season != null ? registration.Season.DisplayName : string.Empty,
            rankingId = registration?.Ranking != null ? registration.Ranking.Id : string.Empty,
            rankingName = registration?.Ranking != null ? registration.Ranking.DisplayName : string.Empty,
            repeatMode = registration != null ? registration.RepeatMode : CompetitionRegistrationRepeatMode.Always,
            windowMode = registration != null ? registration.WindowMode : CompetitionRegistrationWindowMode.AlwaysOpen,
            invitationMode = registration != null ? registration.InvitationMode : CompetitionRegistrationInvitationMode.NotRequired,
            venueMode = registration != null ? registration.VenueMode : CompetitionRegistrationVenueMode.NotRequired,
            moneyCost = registration != null ? registration.MoneyCost : 0f,
            itemCostCount = registration != null ? registration.ItemCosts.Count(cost => cost != null && cost.item != null && cost.count > 0) : 0,
            canRegister = canRegister,
            canGenerateRoster = canGenerateRoster,
            hasOpenWindow = registration == null || registration.WindowMode == CompetitionRegistrationWindowMode.AlwaysOpen || resolvedWindow != null,
            openWindowCount = openWindows.Count,
            openWindowId = resolvedWindow != null ? resolvedWindow.Id : string.Empty,
            openWindowName = resolvedWindow != null ? resolvedWindow.DisplayName : string.Empty,
            invitationId = resolvedInvitation != null ? resolvedInvitation.Id : string.Empty,
            invitationName = resolvedInvitation != null ? resolvedInvitation.DisplayName : string.Empty,
            venueId = resolvedVenue != null ? resolvedVenue.Id : string.Empty,
            venueName = resolvedVenue != null ? resolvedVenue.DisplayName : string.Empty,
            generateBracketOnRegister = registration != null && registration.GenerateBracketOnRegister,
            hasActiveBracket = hasActiveBracket,
            registrationCount = registrationLog != null && registration != null ? registrationLog.GetRegistrationCount(registration) : 0,
            contextKey = contextKey,
            remainingCooldownHours = registrationLog != null && registration != null ? registrationLog.GetRemainingCooldownHours(registration, contextKey) : 0,
            failureMessage = canRegister ? string.Empty : failureMessage,
            displayText = registration != null ? $"{registration.DisplayName} - {(canRegister ? "open" : "locked")}" : string.Empty
        };
    }
}

[Serializable]
public class CompetitionInvitationOptionRow {
    [Tooltip("Invitation, qualifier pass or wildcard id.")]
    public string invitationId;
    [Tooltip("Display name shown by UI.")]
    public string displayName;
    [Tooltip("Description shown by UI.")]
    public string description;
    [Tooltip("Broad invitation kind.")]
    public CompetitionInvitationKind kind;
    [Tooltip("Free-form invitation tags.")]
    public List<string> tags = new List<string>();
    [Tooltip("If enabled, the player owns this invitation.")]
    public bool owned;
    [Tooltip("If enabled, this invitation can be used right now.")]
    public bool usable;
    [Tooltip("If enabled, this invitation can be granted right now.")]
    public bool canGrant;
    [Tooltip("How many times this invitation has been granted.")]
    public int grantCount;
    [Tooltip("How many counted uses remain. -1 means unlimited.")]
    public int availableUses;
    [Tooltip("How many counted uses have been consumed.")]
    public int usedCount;
    [Tooltip("If enabled, this invitation has unlimited uses while active.")]
    public bool unlimitedUses;
    [Tooltip("If enabled, this invitation can expire.")]
    public bool expires;
    [Tooltip("Absolute in-game hour when this invitation expires. -1 means no expiration.")]
    public int expiresTotalHour;
    [Tooltip("Remaining in-game hours before expiration. -1 means no expiration.")]
    public int remainingHours;
    [Tooltip("Last source id that granted this invitation.")]
    public string sourceId;
    [Tooltip("Failure reason shown when Usable or Can Grant is false.")]
    public string failureMessage;
    [Tooltip("Short text useful for placeholder UI rows.")]
    public string displayText;

    public static CompetitionInvitationOptionRow FromDefinition(CompetitionInvitationDefinition invitation, PlayerCompetitionInvitationRecord record, PlayerController player, int currentTotalHour) {
        string grantFailure = "Invitation could not be granted.";
        bool canGrant = invitation != null && invitation.CanGrant(player, out grantFailure);
        string useFailure = "Invitation is not owned.";
        bool usable = record != null && record.IsUsable(currentTotalHour, out useFailure);
        return new CompetitionInvitationOptionRow {
            invitationId = invitation != null ? invitation.Id : record != null ? record.invitationId : string.Empty,
            displayName = invitation != null ? invitation.DisplayName : record != null ? record.invitationName : string.Empty,
            description = invitation != null ? invitation.Description : string.Empty,
            kind = invitation != null ? invitation.Kind : ParseKind(record != null ? record.kind : null),
            tags = invitation != null ? invitation.Tags.ToList() : new List<string>(),
            owned = record != null,
            usable = usable,
            canGrant = canGrant,
            grantCount = record != null ? Mathf.Max(0, record.grantCount) : 0,
            availableUses = record != null ? (record.unlimitedUses ? -1 : record.GetAvailableUseCount()) : 0,
            usedCount = record != null ? Mathf.Max(0, record.usedCount) : 0,
            unlimitedUses = record != null ? record.unlimitedUses : invitation != null && invitation.UnlimitedUses,
            expires = invitation != null ? invitation.Expires : record != null && record.expiresTotalHour >= 0,
            expiresTotalHour = record != null ? record.expiresTotalHour : -1,
            remainingHours = record == null || record.expiresTotalHour < 0 ? -1 : Mathf.Max(0, record.expiresTotalHour - currentTotalHour),
            sourceId = record != null ? record.sourceId : string.Empty,
            failureMessage = usable || canGrant ? string.Empty : !string.IsNullOrWhiteSpace(useFailure) ? useFailure : grantFailure,
            displayText = invitation != null ? $"{invitation.DisplayName} - {(usable ? "usable" : record != null ? "owned" : "unowned")}" : string.Empty
        };
    }

    public static CompetitionInvitationOptionRow FromRecord(PlayerCompetitionInvitationRecord record, int currentTotalHour) {
        string failureMessage = "Invitation record could not be resolved.";
        bool usable = record != null && record.IsUsable(currentTotalHour, out failureMessage);
        return new CompetitionInvitationOptionRow {
            invitationId = record != null ? record.invitationId : string.Empty,
            displayName = record != null ? record.invitationName : string.Empty,
            description = string.Empty,
            kind = ParseKind(record != null ? record.kind : null),
            tags = new List<string>(),
            owned = record != null,
            usable = usable,
            canGrant = false,
            grantCount = record != null ? Mathf.Max(0, record.grantCount) : 0,
            availableUses = record != null ? (record.unlimitedUses ? -1 : record.GetAvailableUseCount()) : 0,
            usedCount = record != null ? Mathf.Max(0, record.usedCount) : 0,
            unlimitedUses = record != null && record.unlimitedUses,
            expires = record != null && record.expiresTotalHour >= 0,
            expiresTotalHour = record != null ? record.expiresTotalHour : -1,
            remainingHours = record == null || record.expiresTotalHour < 0 ? -1 : Mathf.Max(0, record.expiresTotalHour - currentTotalHour),
            sourceId = record != null ? record.sourceId : string.Empty,
            failureMessage = usable ? string.Empty : failureMessage,
            displayText = record != null ? $"{record.invitationName} - saved invitation" : string.Empty
        };
    }

    static CompetitionInvitationKind ParseKind(string value) {
        return Enum.TryParse(value, out CompetitionInvitationKind parsed) ? parsed : CompetitionInvitationKind.Invitation;
    }
}

[Serializable]
public class CompetitionVenueOptionRow {
    [Tooltip("Venue, gym, stadium or facility id.")]
    public string venueId;
    [Tooltip("Display name shown by UI.")]
    public string displayName;
    [Tooltip("Description shown by UI.")]
    public string description;
    [Tooltip("Broad venue kind.")]
    public CompetitionVenueKind kind;
    [Tooltip("Free-form venue tags.")]
    public List<string> tags = new List<string>();
    [Tooltip("World region id connected to this venue.")]
    public string worldRegionId;
    [Tooltip("World region display name connected to this venue.")]
    public string worldRegionName;
    [Tooltip("Scene name resolved by this venue.")]
    public string sceneName;
    [Tooltip("Location key resolved by this venue.")]
    public string locationKey;
    [Tooltip("If enabled, the player can enter this venue right now.")]
    public bool canEnter;
    [Tooltip("If enabled, this venue can host the context registration or no context registration is assigned.")]
    public bool canHostContextRegistration;
    [Tooltip("Number of successful use records for this venue.")]
    public int successfulUseCount;
    [Tooltip("Number of blocked use records for this venue.")]
    public int blockedUseCount;
    [Tooltip("Absolute in-game hour of the last venue use.")]
    public int lastUsedTotalHour;
    [Tooltip("Failure reason shown when entry or hosting is blocked.")]
    public string failureMessage;
    [Tooltip("Short text useful for placeholder UI rows.")]
    public string displayText;

    public static CompetitionVenueOptionRow FromVenue(CompetitionVenueDefinition venue, PlayerController player, PlayerCompetitionVenueLog log, CompetitionRegistrationDefinition registrationContext, string sourceId) {
        string enterFailure = "Venue could not be resolved.";
        bool canEnter = venue != null && venue.CanEnter(player, out enterFailure);
        string hostFailure = string.Empty;
        bool canHost = registrationContext == null || (venue != null && venue.CanHost(player, registrationContext, out hostFailure));
        var lastUse = log != null ? log.GetLastUse(venue, sourceId, includeBlocked: true) : null;

        return new CompetitionVenueOptionRow {
            venueId = venue != null ? venue.Id : string.Empty,
            displayName = venue != null ? venue.DisplayName : string.Empty,
            description = venue != null ? venue.Description : string.Empty,
            kind = venue != null ? venue.Kind : CompetitionVenueKind.Arena,
            tags = venue != null ? venue.Tags.ToList() : new List<string>(),
            worldRegionId = venue?.WorldRegion != null ? venue.WorldRegion.Id : string.Empty,
            worldRegionName = venue?.WorldRegion != null ? venue.WorldRegion.DisplayName : string.Empty,
            sceneName = venue != null ? venue.ResolveSceneName() : string.Empty,
            locationKey = venue != null ? venue.ResolveLocationKey() : string.Empty,
            canEnter = canEnter,
            canHostContextRegistration = canHost,
            successfulUseCount = log != null ? log.GetUseCount(venue, includeBlocked: false) : 0,
            blockedUseCount = log != null ? Mathf.Max(0, log.GetUseCount(venue, includeBlocked: true) - log.GetUseCount(venue, includeBlocked: false)) : 0,
            lastUsedTotalHour = lastUse != null ? lastUse.usedTotalHour : -1,
            failureMessage = canEnter && canHost ? string.Empty : !canEnter ? enterFailure : hostFailure,
            displayText = venue != null ? $"{venue.DisplayName} - {(canEnter && canHost ? "available" : "locked")}" : string.Empty
        };
    }
}

[Serializable]
public class CompetitionRegistrationHistoryRow {
    [Tooltip("Registration definition id.")]
    public string registrationId;
    [Tooltip("Registration display name.")]
    public string registrationName;
    [Tooltip("Repeat/cooldown context key used by this registration.")]
    public string contextKey;
    [Tooltip("Roster id.")]
    public string rosterId;
    [Tooltip("Roster display name.")]
    public string rosterName;
    [Tooltip("Competition id.")]
    public string competitionId;
    [Tooltip("Competition display name.")]
    public string competitionName;
    [Tooltip("Season id.")]
    public string seasonId;
    [Tooltip("Season display name.")]
    public string seasonName;
    [Tooltip("Ranking id.")]
    public string rankingId;
    [Tooltip("Ranking display name.")]
    public string rankingName;
    [Tooltip("Registration window id, if any.")]
    public string windowId;
    [Tooltip("Registration window display name, if any.")]
    public string windowName;
    [Tooltip("Invitation id used by this registration, if any.")]
    public string invitationId;
    [Tooltip("Invitation display name used by this registration, if any.")]
    public string invitationName;
    [Tooltip("Venue id used by this registration, if any.")]
    public string venueId;
    [Tooltip("Venue display name used by this registration, if any.")]
    public string venueName;
    [Tooltip("If enabled, a bracket was generated by this registration.")]
    public bool generatedBracket;
    [Tooltip("Generated bracket seed, if any.")]
    public int bracketSeed;
    [Tooltip("Money paid for this registration.")]
    public float moneyPaid;
    [Tooltip("Absolute in-game hour when this registration was made.")]
    public int registeredTotalHour;
    [Tooltip("Short source id that recorded this registration.")]
    public string sourceId;
    [Tooltip("Short text useful for placeholder UI rows.")]
    public string displayText;

    public static CompetitionRegistrationHistoryRow FromRecord(PlayerCompetitionRegistrationRecord record) {
        return new CompetitionRegistrationHistoryRow {
            registrationId = record != null ? record.registrationId : string.Empty,
            registrationName = record != null ? record.registrationName : string.Empty,
            contextKey = record != null ? record.contextKey : string.Empty,
            rosterId = record != null ? record.rosterId : string.Empty,
            rosterName = record != null ? record.rosterName : string.Empty,
            competitionId = record != null ? record.competitionId : string.Empty,
            competitionName = record != null ? record.competitionName : string.Empty,
            seasonId = record != null ? record.seasonId : string.Empty,
            seasonName = record != null ? record.seasonName : string.Empty,
            rankingId = record != null ? record.rankingId : string.Empty,
            rankingName = record != null ? record.rankingName : string.Empty,
            windowId = record != null ? record.windowId : string.Empty,
            windowName = record != null ? record.windowName : string.Empty,
            invitationId = record != null ? record.invitationId : string.Empty,
            invitationName = record != null ? record.invitationName : string.Empty,
            venueId = record != null ? record.venueId : string.Empty,
            venueName = record != null ? record.venueName : string.Empty,
            generatedBracket = record != null && record.generatedBracket,
            bracketSeed = record != null ? record.bracketSeed : 0,
            moneyPaid = record != null ? record.moneyPaid : 0f,
            registeredTotalHour = record != null ? record.registeredTotalHour : -1,
            sourceId = record != null ? record.sourceId : string.Empty,
            displayText = record != null ? $"{record.registrationName} - hour {record.registeredTotalHour}" : string.Empty
        };
    }
}

[Serializable]
public class CompetitionBracketSummaryRow {
    [Tooltip("Roster id for this bracket.")]
    public string rosterId;
    [Tooltip("Roster display name for this bracket.")]
    public string rosterName;
    [Tooltip("Competition id for this bracket.")]
    public string competitionId;
    [Tooltip("Competition display name for this bracket.")]
    public string competitionName;
    [Tooltip("Season id for this bracket.")]
    public string seasonId;
    [Tooltip("Season display name for this bracket.")]
    public string seasonName;
    [Tooltip("Ranking id for this bracket.")]
    public string rankingId;
    [Tooltip("Ranking display name for this bracket.")]
    public string rankingName;
    [Tooltip("Generated bracket seed.")]
    public int seed;
    [Tooltip("If enabled, this bracket is active.")]
    public bool active;
    [Tooltip("If enabled, this bracket completed.")]
    public bool completed;
    [Tooltip("If enabled, the player won this bracket.")]
    public bool won;
    [Tooltip("If enabled, this bracket was abandoned.")]
    public bool abandoned;
    [Tooltip("Current round index.")]
    public int currentRoundIndex;
    [Tooltip("Player match attempt count.")]
    public int matchAttemptCount;
    [Tooltip("Player match win count.")]
    public int matchWinCount;
    [Tooltip("Player match loss count.")]
    public int matchLossCount;
    [Tooltip("Entrant count in this bracket.")]
    public int entrantCount;
    [Tooltip("Round count in this bracket.")]
    public int roundCount;
    [Tooltip("Total player match count in this bracket.")]
    public int playerMatchCount;
    [Tooltip("In-game total hour when this bracket was generated.")]
    public int generatedTotalHour;
    [Tooltip("In-game total hour when this bracket completed.")]
    public int completedTotalHour;
    [Tooltip("Last match id touched by this bracket.")]
    public string lastMatchId;
    [Tooltip("Short source id that generated this bracket.")]
    public string sourceId;
    [Tooltip("Short text useful for placeholder UI rows.")]
    public string displayText;

    public static CompetitionBracketSummaryRow FromState(PlayerCompetitionBracketState state) {
        return new CompetitionBracketSummaryRow {
            rosterId = state != null ? state.rosterId : string.Empty,
            rosterName = state != null ? state.rosterName : string.Empty,
            competitionId = state != null ? state.competitionId : string.Empty,
            competitionName = state != null ? state.competitionName : string.Empty,
            seasonId = state != null ? state.seasonId : string.Empty,
            seasonName = state != null ? state.seasonName : string.Empty,
            rankingId = state != null ? state.rankingId : string.Empty,
            rankingName = state != null ? state.rankingName : string.Empty,
            seed = state != null ? state.seed : 0,
            active = state != null && state.active,
            completed = state != null && state.completed,
            won = state != null && state.won,
            abandoned = state != null && state.abandoned,
            currentRoundIndex = state != null ? state.currentRoundIndex : 0,
            matchAttemptCount = state != null ? state.matchAttemptCount : 0,
            matchWinCount = state != null ? state.matchWinCount : 0,
            matchLossCount = state != null ? state.matchLossCount : 0,
            entrantCount = state != null && state.entrants != null ? state.entrants.Count : 0,
            roundCount = state != null && state.rounds != null ? state.rounds.Count : 0,
            playerMatchCount = state != null ? state.GetPlayerMatchCount() : 0,
            generatedTotalHour = state != null ? state.generatedTotalHour : -1,
            completedTotalHour = state != null ? state.completedTotalHour : -1,
            lastMatchId = state != null ? state.lastMatchId : string.Empty,
            sourceId = state != null ? state.sourceId : string.Empty,
            displayText = state != null ? $"{state.rosterName} - {(state.active ? "active" : state.completed ? "completed" : "inactive")}" : string.Empty
        };
    }
}

static class CompetitionRegistrationUIEnumerableExtensions {
    public static Dictionary<string, T> ToCompetitionDictionarySafe<T>(this IEnumerable<T> source, Func<T, string> idSelector) {
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
}
