using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum PokemonAssignmentUIActionResultKind {
    None,
    Refreshed,
    AssignmentStarted,
    AssignmentClaimed,
    AssignmentCancelled,
    Blocked
}

public class PokemonAssignmentUIManager : MonoBehaviour {
    [Header("Player")]
    [Tooltip("Player whose Pokemon assignment state is shown. Empty uses PlayerController.i or the first PlayerController in the scene.")]
    [SerializeField] PlayerController playerOverride = null;
    [Tooltip("If enabled, missing PlayerPokemonAssignmentLog is created when UI actions need it.")]
    [SerializeField] bool createMissingLogForActions = true;

    [Header("Board")]
    [Tooltip("Assignment board shown by this UI backend. Empty uses Direct Assignments.")]
    [SerializeField] PokemonAssignmentBoardDefinition board = null;
    [Tooltip("Assignments shown when no board definition is assigned.")]
    [SerializeField] List<PokemonAssignmentDefinition> directAssignments = new List<PokemonAssignmentDefinition>();
    [Tooltip("Optional activity zone context passed into assignment checks. Empty falls back to PlayerActivityContext.CurrentZone.")]
    [SerializeField] ActivityZoneDefinition zoneContext = null;
    [Tooltip("Source id used when Direct Assignments are shown or a board entry has no source override.")]
    [SerializeField] string uiSourceId = "ui:pokemon-assignment";

    [Header("Snapshot")]
    [Tooltip("If enabled, Refresh is called when this component starts.")]
    [SerializeField] bool refreshOnStart = true;
    [Tooltip("If enabled, Refresh is called after every successful or blocked action.")]
    [SerializeField] bool refreshAfterActions = true;
    [Tooltip("If enabled, locked offers are included with a failure reason.")]
    [SerializeField] bool includeLockedOffers = true;
    [Tooltip("If enabled, Pokemon that cannot start a listed assignment still appear in option rows.")]
    [SerializeField] bool includeIneligiblePokemonOptions;
    [Tooltip("Maximum offer rows copied into the snapshot. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxOfferRows = 30;
    [Tooltip("Maximum Pokemon option rows copied per offer. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxPokemonOptionsPerOffer = 6;
    [Tooltip("Maximum active assignment rows copied into the snapshot. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxActiveRows = 20;

    [Header("Debug")]
    [Tooltip("If enabled, successful UI backend actions are written to GameDebug.")]
    [SerializeField] bool logSuccessfulActions;
    [Tooltip("If enabled, blocked UI backend actions are written to GameDebug.")]
    [SerializeField] bool logBlockedActions = true;

    PokemonAssignmentUIScreenSnapshot currentSnapshot = new PokemonAssignmentUIScreenSnapshot();
    PokemonAssignmentUIActionResult lastResult = new PokemonAssignmentUIActionResult();

    public PokemonAssignmentUIScreenSnapshot CurrentSnapshot => currentSnapshot;
    public PokemonAssignmentUIActionResult LastResult => lastResult;
    public PokemonAssignmentBoardDefinition Board => board;
    public IReadOnlyList<PokemonAssignmentDefinition> DirectAssignments => directAssignments;
    public ActivityZoneDefinition ZoneContext => zoneContext;
    public event Action<PokemonAssignmentUIScreenSnapshot> OnSnapshotChanged;
    public event Action<PokemonAssignmentUIActionResult> OnActionResult;

    void Start() {
        if(refreshOnStart) {
            Refresh();
        }
    }

    [ContextMenu("Refresh Pokemon Assignment Snapshot")]
    public PokemonAssignmentUIScreenSnapshot RefreshFromContextMenu() {
        return Refresh();
    }

    public PokemonAssignmentUIScreenSnapshot Refresh() {
        var player = ResolvePlayer();
        var log = player != null ? player.GetComponent<PlayerPokemonAssignmentLog>() : null;
        var party = player != null ? player.GetComponent<PokemonParty>() : null;
        var zone = ResolveZone();
        var entries = ResolveEntries().ToList();
        var offerRows = BuildOfferRows(player, log, party, zone, entries).ToList();
        var activeRows = BuildActiveRows(log, entries).ToList();

        currentSnapshot = new PokemonAssignmentUIScreenSnapshot {
            hasPlayer = player != null,
            playerName = player != null ? player.name : string.Empty,
            boardId = board != null ? board.Id : string.Empty,
            boardName = board != null ? board.DisplayName : "Pokemon Assignments",
            boardDescription = board != null ? board.Description : string.Empty,
            zoneId = zone != null ? zone.Id : string.Empty,
            zoneName = zone != null ? zone.DisplayName : string.Empty,
            day = TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1,
            hour = TimeSystem.i != null ? Mathf.Clamp(TimeSystem.i.Hour, 0, 23) : 0,
            absoluteHour = GetCurrentAbsoluteHour(),
            offerCount = offerRows.Count,
            activeCount = activeRows.Count,
            readyCount = activeRows.Count(row => row != null && row.isReady),
            offers = offerRows,
            activeAssignments = activeRows,
            lastResult = lastResult
        };

        OnSnapshotChanged?.Invoke(currentSnapshot);
        return currentSnapshot;
    }

    public bool TryStartAssignment(string offerIdOrAssignmentId, int partyIndex, out string feedback) {
        var player = ResolvePlayer();
        if(player == null) {
            return Block("A player is required to start Pokemon assignments.", out feedback);
        }

        var entry = FindEntry(offerIdOrAssignmentId);
        if(entry == null || entry.Assignment == null) {
            return Block($"Assignment offer '{offerIdOrAssignmentId}' could not be found.", out feedback);
        }

        var party = player.GetComponent<PokemonParty>();
        if(party?.Pokemons == null || partyIndex < 0 || partyIndex >= party.Pokemons.Count) {
            return Block("Selected party Pokemon could not be found.", out feedback);
        }

        var pokemon = party.Pokemons[partyIndex];
        return TryStartEntry(player, entry, pokemon, out feedback);
    }

    public bool TryStartWithFirstEligiblePokemon(string offerIdOrAssignmentId, out string feedback) {
        var player = ResolvePlayer();
        if(player == null) {
            return Block("A player is required to start Pokemon assignments.", out feedback);
        }

        var entry = FindEntry(offerIdOrAssignmentId);
        if(entry == null || entry.Assignment == null) {
            return Block($"Assignment offer '{offerIdOrAssignmentId}' could not be found.", out feedback);
        }

        var log = GetLog(player, createMissingLogForActions);
        var party = player.GetComponent<PokemonParty>();
        var zone = entry.ResolveZone(ResolveZone());
        string sourceId = entry.ResolveSourceId(board, ResolveSourceId());
        var pokemon = party?.Pokemons?
            .Where(candidate => candidate != null)
            .OrderByDescending(candidate => entry.Assignment.GetSuccessChance(candidate))
            .FirstOrDefault(candidate => entry.RequirementsMet(player, out _)
                && entry.Assignment.CanStart(player, candidate, log, zone, sourceId, out _));

        if(pokemon == null) {
            return Block("No eligible party Pokemon was found for this assignment.", out feedback);
        }

        return TryStartEntry(player, entry, pokemon, out feedback);
    }

    public bool TryClaimFirstReady(string offerIdOrAssignmentId, out string feedback) {
        var player = ResolvePlayer();
        if(player == null) {
            return Block("A player is required to claim Pokemon assignments.", out feedback);
        }

        var entry = FindEntry(offerIdOrAssignmentId);
        if(entry == null || entry.Assignment == null) {
            return Block($"Assignment offer '{offerIdOrAssignmentId}' could not be found.", out feedback);
        }

        var log = GetLog(player, createMissingLogForActions);
        string sourceId = entry.ResolveSourceId(board, ResolveSourceId());
        var state = log?.GetReadyAssignments(entry.Assignment, sourceId).FirstOrDefault();
        if(state == null) {
            return Block("No ready assignment was found for this offer.", out feedback);
        }

        if(log.TryClaim(player, entry.Assignment, state, out feedback)) {
            return Succeed(PokemonAssignmentUIActionResultKind.AssignmentClaimed, feedback ?? $"{entry.DisplayName} claimed.", out feedback);
        }

        return Block(feedback, out feedback);
    }

    public bool TryCancelFirstActive(string offerIdOrAssignmentId, out string feedback) {
        var entry = FindEntry(offerIdOrAssignmentId);
        var player = ResolvePlayer();
        var log = player != null ? player.GetComponent<PlayerPokemonAssignmentLog>() : null;
        if(entry == null || entry.Assignment == null || log == null) {
            return Block("No matching active assignment was found.", out feedback);
        }

        string sourceId = entry.ResolveSourceId(board, ResolveSourceId());
        var state = log.ActiveAssignments.FirstOrDefault(active => active != null
            && active.assignmentId == entry.Assignment.Id
            && active.sourceId == sourceId);

        if(state != null && log.TryCancel(state)) {
            return Succeed(PokemonAssignmentUIActionResultKind.AssignmentCancelled, $"{entry.DisplayName} cancelled.", out feedback);
        }

        return Block("No matching active assignment was cancelled.", out feedback);
    }

    bool TryStartEntry(PlayerController player, PokemonAssignmentBoardEntry entry, Pokemon pokemon, out string feedback) {
        if(entry == null || entry.Assignment == null) {
            return Block("No assignment selected.", out feedback);
        }

        if(!entry.RequirementsMet(player, out feedback)) {
            return Block(feedback, out feedback);
        }

        var log = GetLog(player, createMissingLogForActions);
        string sourceId = entry.ResolveSourceId(board, ResolveSourceId());
        var zone = entry.ResolveZone(ResolveZone());

        if(log != null && log.TryStart(player, entry.Assignment, pokemon, zone, sourceId, entry.DisplayName, out feedback)) {
            return Succeed(PokemonAssignmentUIActionResultKind.AssignmentStarted, $"{pokemon.NickName} started {entry.DisplayName}.", out feedback);
        }

        return Block(feedback, out feedback);
    }

    IEnumerable<PokemonAssignmentOfferRow> BuildOfferRows(PlayerController player, PlayerPokemonAssignmentLog log, PokemonParty party, ActivityZoneDefinition zone, IReadOnlyList<PokemonAssignmentBoardEntry> entries) {
        var rows = new List<PokemonAssignmentOfferRow>();
        foreach(var entry in entries) {
            if(entry == null || entry.Assignment == null) {
                continue;
            }

            string sourceId = entry.ResolveSourceId(board, ResolveSourceId());
            var entryZone = entry.ResolveZone(zone);
            bool entryRequirementsMet = entry.RequirementsMet(player, out var entryFailure);
            var pokemonRows = BuildPokemonRows(player, log, party, entry, entryZone, sourceId, entryRequirementsMet, entryFailure).ToList();
            int eligibleCount = pokemonRows.Count(row => row != null && row.canStart);
            var best = pokemonRows.Where(row => row != null && row.canStart).OrderByDescending(row => row.successChance).FirstOrDefault();
            bool active = log != null && log.HasActiveAssignment(entry.Assignment, sourceId);
            int ready = log != null ? log.GetReadyAssignments(entry.Assignment, sourceId).Count : 0;
            bool canStart = entryRequirementsMet && eligibleCount > 0;
            string failure = canStart ? null : !string.IsNullOrWhiteSpace(entryFailure) ? entryFailure : pokemonRows.FirstOrDefault(row => row != null && !row.canStart)?.failureMessage;
            bool include = canStart || active || ready > 0 || includeLockedOffers || (board != null && board.ShowLockedEntriesByDefault);
            if(entry.HideWhenLocked && !canStart && !active && ready <= 0 && !includeLockedOffers) {
                include = false;
            }

            if(!include) {
                continue;
            }

            rows.Add(new PokemonAssignmentOfferRow {
                offerId = entry.OfferId,
                assignmentId = entry.Assignment.Id,
                displayName = entry.DisplayName,
                description = entry.Description,
                category = entry.Assignment.Category,
                priority = entry.Priority,
                sourceId = sourceId,
                zoneId = entryZone != null ? entryZone.Id : string.Empty,
                zoneName = entryZone != null ? entryZone.DisplayName : string.Empty,
                durationHours = entry.Assignment.DurationHours,
                canStart = canStart,
                isActive = active,
                readyCount = ready,
                completedCount = log != null ? log.GetCompletedCount(entry.Assignment, sourceId) : 0,
                eligiblePokemonCount = eligibleCount,
                bestPokemonKey = best != null ? best.pokemonKey : string.Empty,
                bestPokemonName = best != null ? best.pokemonName : string.Empty,
                bestPartyIndex = best != null ? best.partyIndex : -1,
                bestSuccessChance = best != null ? best.successChance : entry.Assignment.BaseSuccessChance,
                failureMessage = failure,
                tags = entry.Assignment.Tags != null ? entry.Assignment.Tags.ToList() : new List<string>(),
                pokemonOptions = Limit(pokemonRows, maxPokemonOptionsPerOffer).ToList(),
                displayText = $"{entry.DisplayName} - {(canStart ? eligibleCount + " eligible" : "locked")}"
            });
        }

        return Limit(rows.OrderByDescending(row => row.priority).ThenBy(row => row.displayName), maxOfferRows);
    }

    IEnumerable<PokemonAssignmentPokemonOptionRow> BuildPokemonRows(PlayerController player, PlayerPokemonAssignmentLog log, PokemonParty party, PokemonAssignmentBoardEntry entry, ActivityZoneDefinition zone, string sourceId, bool entryRequirementsMet, string entryFailure) {
        if(party?.Pokemons == null || entry?.Assignment == null) {
            yield break;
        }

        for(int i = 0; i < party.Pokemons.Count; i++) {
            var pokemon = party.Pokemons[i];
            if(pokemon == null) {
                continue;
            }

            string failure = null;
            bool canStart = entryRequirementsMet && entry.Assignment.CanStart(player, pokemon, log, zone, sourceId, out failure);
            if(!entryRequirementsMet) {
                failure = entryFailure;
            }

            if(!includeIneligiblePokemonOptions && !canStart) {
                continue;
            }

            yield return PokemonAssignmentPokemonOptionRow.FromPokemon(pokemon, log, party, i, entry.Assignment, canStart, failure);
        }
    }

    IEnumerable<PokemonAssignmentActiveRow> BuildActiveRows(PlayerPokemonAssignmentLog log, IReadOnlyList<PokemonAssignmentBoardEntry> entries) {
        if(log == null) {
            return Array.Empty<PokemonAssignmentActiveRow>();
        }

        var knownIds = new HashSet<string>(entries.Where(entry => entry?.Assignment != null).Select(entry => entry.Assignment.Id));
        var rows = log.ActiveAssignments
            .Where(state => state != null && (knownIds.Count == 0 || knownIds.Contains(state.assignmentId)))
            .Select(PokemonAssignmentActiveRow.FromState)
            .OrderByDescending(row => row.isReady)
            .ThenBy(row => row.readyAbsoluteHour)
            .ThenBy(row => row.assignmentName);

        return Limit(rows, maxActiveRows);
    }

    IEnumerable<PokemonAssignmentBoardEntry> ResolveEntries() {
        if(board != null) {
            return board.GetOrderedEntries();
        }

        return (directAssignments ?? new List<PokemonAssignmentDefinition>())
            .Where(assignment => assignment != null)
            .Select(assignment => new PokemonAssignmentBoardEntryAdapter(assignment))
            .Cast<PokemonAssignmentBoardEntry>();
    }

    PokemonAssignmentBoardEntry FindEntry(string offerIdOrAssignmentId) {
        if(string.IsNullOrWhiteSpace(offerIdOrAssignmentId)) {
            return null;
        }

        return ResolveEntries().FirstOrDefault(entry => entry != null
            && entry.Assignment != null
            && (string.Equals(entry.OfferId, offerIdOrAssignmentId, StringComparison.OrdinalIgnoreCase)
                || string.Equals(entry.Assignment.Id, offerIdOrAssignmentId, StringComparison.OrdinalIgnoreCase)));
    }

    PlayerController ResolvePlayer() {
        if(playerOverride != null) {
            return playerOverride;
        }

        if(PlayerController.i != null) {
            return PlayerController.i;
        }

        return FindAnyObjectByType<PlayerController>(FindObjectsInactive.Include);
    }

    PlayerPokemonAssignmentLog GetLog(PlayerController player, bool createIfMissing) {
        if(player == null) {
            return null;
        }

        var log = player.GetComponent<PlayerPokemonAssignmentLog>();
        if(log == null && createIfMissing) {
            log = player.gameObject.AddComponent<PlayerPokemonAssignmentLog>();
        }

        return log;
    }

    ActivityZoneDefinition ResolveZone() {
        return zoneContext != null ? zoneContext : PlayerActivityContext.CurrentZone;
    }

    string ResolveSourceId() {
        if(!string.IsNullOrWhiteSpace(uiSourceId)) {
            return uiSourceId;
        }

        return board != null ? $"ui:pokemon-assignment:{board.Id}" : "ui:pokemon-assignment";
    }

    bool Succeed(PokemonAssignmentUIActionResultKind kind, string message, out string feedback) {
        feedback = message;
        lastResult = BuildResult(kind, true, message);
        OnActionResult?.Invoke(lastResult);
        if(logSuccessfulActions) {
            GameDebug.Success(message, GameDebugCategory.PokemonCare, this, "PokemonAssignmentUIManager");
        }

        if(refreshAfterActions) {
            Refresh();
        }

        return true;
    }

    bool Block(string message, out string feedback) {
        feedback = message;
        lastResult = BuildResult(PokemonAssignmentUIActionResultKind.Blocked, false, message);
        OnActionResult?.Invoke(lastResult);
        if(logBlockedActions) {
            GameDebug.Warning(message, GameDebugCategory.PokemonCare, this, "PokemonAssignmentUIManager");
        }

        if(refreshAfterActions) {
            Refresh();
        }

        return false;
    }

    PokemonAssignmentUIActionResult BuildResult(PokemonAssignmentUIActionResultKind kind, bool success, string message) {
        return new PokemonAssignmentUIActionResult {
            kind = kind,
            success = success,
            message = message,
            day = TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1,
            hour = TimeSystem.i != null ? Mathf.Clamp(TimeSystem.i.Hour, 0, 23) : 0,
            absoluteHour = GetCurrentAbsoluteHour()
        };
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    static IEnumerable<T> Limit<T>(IEnumerable<T> query, int limit) {
        return limit > 0 ? query.Take(limit) : query;
    }

    class PokemonAssignmentBoardEntryAdapter : PokemonAssignmentBoardEntry {
        readonly PokemonAssignmentDefinition assignmentDefinition;

        public PokemonAssignmentBoardEntryAdapter(PokemonAssignmentDefinition assignmentDefinition) {
            this.assignmentDefinition = assignmentDefinition;
        }

        public override PokemonAssignmentDefinition Assignment => assignmentDefinition;
        public override string OfferId => assignmentDefinition != null ? assignmentDefinition.Id : string.Empty;
        public override string DisplayName => assignmentDefinition != null ? assignmentDefinition.DisplayName : string.Empty;
        public override string Description => assignmentDefinition != null ? assignmentDefinition.Description : string.Empty;
        public override int Priority => assignmentDefinition != null ? assignmentDefinition.Priority : 0;
        public override bool HideWhenLocked => false;
        public override IReadOnlyList<ActivityRequirement> ExtraRequirements => Array.Empty<ActivityRequirement>();
        public override string ResolveSourceId(PokemonAssignmentBoardDefinition board, string fallbackSourceId) {
            return !string.IsNullOrWhiteSpace(fallbackSourceId) ? fallbackSourceId : assignmentDefinition != null ? $"pokemon-assignment:{assignmentDefinition.Id}" : "pokemon-assignment";
        }
        public override ActivityZoneDefinition ResolveZone(ActivityZoneDefinition fallbackZone) => fallbackZone;
        public override bool RequirementsMet(PlayerController player, out string failureMessage) {
            failureMessage = null;
            return true;
        }
    }
}

[Serializable]
public class PokemonAssignmentUIScreenSnapshot {
    [Tooltip("If enabled, a player was resolved for this snapshot.")]
    public bool hasPlayer;
    [Tooltip("Resolved player object name.")]
    public string playerName;
    [Tooltip("Assignment board id.")]
    public string boardId;
    [Tooltip("Assignment board display name.")]
    public string boardName;
    [Tooltip("Assignment board description.")]
    public string boardDescription;
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
    [Tooltip("Visible offer row count.")]
    public int offerCount;
    [Tooltip("Active assignment row count.")]
    public int activeCount;
    [Tooltip("Ready active assignment count.")]
    public int readyCount;
    [Tooltip("Visible assignment offers.")]
    public List<PokemonAssignmentOfferRow> offers = new List<PokemonAssignmentOfferRow>();
    [Tooltip("Active assignment rows.")]
    public List<PokemonAssignmentActiveRow> activeAssignments = new List<PokemonAssignmentActiveRow>();
    [Tooltip("Most recent UI backend action result.")]
    public PokemonAssignmentUIActionResult lastResult;
}

[Serializable]
public class PokemonAssignmentUIActionResult {
    [Tooltip("Kind of UI backend action that produced this result.")]
    public PokemonAssignmentUIActionResultKind kind;
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
public class PokemonAssignmentOfferRow {
    [Tooltip("Offer id used by UI actions.")]
    public string offerId;
    [Tooltip("Assignment definition id.")]
    public string assignmentId;
    [Tooltip("Readable assignment name.")]
    public string displayName;
    [Tooltip("Readable assignment description.")]
    public string description;
    [Tooltip("Assignment category.")]
    public PokemonAssignmentCategory category;
    [Tooltip("Offer priority used for sorting.")]
    public int priority;
    [Tooltip("Source id written into assignment logs.")]
    public string sourceId;
    [Tooltip("Activity zone id used by this offer.")]
    public string zoneId;
    [Tooltip("Activity zone name used by this offer.")]
    public string zoneName;
    [Tooltip("In-game hours until the assignment can be claimed.")]
    public int durationHours;
    [Tooltip("If enabled, at least one Pokemon can start this assignment now.")]
    public bool canStart;
    [Tooltip("If enabled, this assignment is already active for this source.")]
    public bool isActive;
    [Tooltip("Ready assignment count for this offer/source.")]
    public int readyCount;
    [Tooltip("Completed claim count for this offer/source.")]
    public int completedCount;
    [Tooltip("Eligible Pokemon count.")]
    public int eligiblePokemonCount;
    [Tooltip("Best eligible Pokemon key.")]
    public string bestPokemonKey;
    [Tooltip("Best eligible Pokemon display name.")]
    public string bestPokemonName;
    [Tooltip("Best eligible party index.")]
    public int bestPartyIndex = -1;
    [Tooltip("Best success chance among eligible Pokemon.")]
    [Range(0f, 1f)]
    public float bestSuccessChance;
    [Tooltip("Failure/reason when this offer cannot start.")]
    public string failureMessage;
    [Tooltip("Free-form assignment tags.")]
    public List<string> tags = new List<string>();
    [Tooltip("Pokemon option rows for this offer.")]
    public List<PokemonAssignmentPokemonOptionRow> pokemonOptions = new List<PokemonAssignmentPokemonOptionRow>();
    [Tooltip("Short text useful for placeholder UI rows.")]
    public string displayText;
}

[Serializable]
public class PokemonAssignmentPokemonOptionRow {
    [Tooltip("Assignment id this option belongs to.")]
    public string assignmentId;
    [Tooltip("Runtime key used by PlayerPokemonAssignmentLog.")]
    public string pokemonKey;
    [Tooltip("Party slot index.")]
    public int partyIndex = -1;
    [Tooltip("Pokemon display/nickname.")]
    public string pokemonName;
    [Tooltip("Pokemon base/species name.")]
    public string pokemonBaseName;
    [Tooltip("Pokemon level.")]
    public int level;
    [Tooltip("Current HP.")]
    public int hp;
    [Tooltip("Maximum HP.")]
    public int maxHp;
    [Tooltip("Current friendship.")]
    public int friendship;
    [Tooltip("If enabled, this Pokemon can start the assignment now.")]
    public bool canStart;
    [Tooltip("Failure/reason when this Pokemon cannot start.")]
    public string failureMessage;
    [Tooltip("Estimated success chance for this Pokemon.")]
    [Range(0f, 1f)]
    public float successChance;
    [Tooltip("Short text useful for placeholder UI rows.")]
    public string displayText;

    public static PokemonAssignmentPokemonOptionRow FromPokemon(Pokemon pokemon, PlayerPokemonAssignmentLog log, PokemonParty party, int partyIndex, PokemonAssignmentDefinition assignment, bool canStart, string failureMessage) {
        string baseName = pokemon != null && pokemon.Base != null ? pokemon.Base.Name : string.Empty;
        float chance = assignment != null ? assignment.GetSuccessChance(pokemon) : 0f;
        return new PokemonAssignmentPokemonOptionRow {
            assignmentId = assignment != null ? assignment.Id : string.Empty,
            pokemonKey = log != null ? log.BuildPokemonKey(pokemon, party) : string.Empty,
            partyIndex = partyIndex,
            pokemonName = pokemon != null ? pokemon.NickName : string.Empty,
            pokemonBaseName = baseName,
            level = pokemon != null ? pokemon.Level : 0,
            hp = pokemon != null ? pokemon.HP : 0,
            maxHp = pokemon != null ? pokemon.MaxHp : 0,
            friendship = pokemon != null ? pokemon.Friendship : 0,
            canStart = canStart,
            failureMessage = failureMessage,
            successChance = chance,
            displayText = $"{(pokemon != null ? pokemon.NickName : "Pokemon")} - {(canStart ? $"{chance:P0}" : "locked")}"
        };
    }
}

[Serializable]
public class PokemonAssignmentActiveRow {
    [Tooltip("Assignment id.")]
    public string assignmentId;
    [Tooltip("Assignment display name.")]
    public string assignmentName;
    [Tooltip("Assignment source id.")]
    public string sourceId;
    [Tooltip("Assignment source name.")]
    public string sourceName;
    [Tooltip("Activity zone id.")]
    public string zoneId;
    [Tooltip("Pokemon key.")]
    public string pokemonKey;
    [Tooltip("Pokemon display name.")]
    public string pokemonName;
    [Tooltip("Pokemon base/species name.")]
    public string pokemonBaseName;
    [Tooltip("Party index captured at assignment start.")]
    public int partyIndex;
    [Tooltip("Start absolute hour.")]
    public int startedAbsoluteHour;
    [Tooltip("Ready absolute hour.")]
    public int readyAbsoluteHour;
    [Tooltip("Remaining hours until ready. 0 means ready now.")]
    public int hoursRemaining;
    [Tooltip("If enabled, assignment is ready to claim.")]
    public bool isReady;
    [Tooltip("Captured success chance.")]
    [Range(0f, 1f)]
    public float successChance;
    [Tooltip("Short text useful for placeholder UI rows.")]
    public string displayText;

    public static PokemonAssignmentActiveRow FromState(PlayerPokemonAssignmentState state) {
        int currentHour = TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
        int remaining = Mathf.Max(0, state.readyAbsoluteHour - currentHour);
        return new PokemonAssignmentActiveRow {
            assignmentId = state.assignmentId,
            assignmentName = state.assignmentName,
            sourceId = state.sourceId,
            sourceName = state.sourceName,
            zoneId = state.zoneId,
            pokemonKey = state.pokemonKey,
            pokemonName = state.pokemonName,
            pokemonBaseName = state.pokemonBaseName,
            partyIndex = state.partyIndex,
            startedAbsoluteHour = state.startedAbsoluteHour,
            readyAbsoluteHour = state.readyAbsoluteHour,
            hoursRemaining = remaining,
            isReady = remaining <= 0,
            successChance = state.successChance,
            displayText = $"{state.pokemonName}: {state.assignmentName} {(remaining <= 0 ? "ready" : remaining + "h")}"
        };
    }
}
