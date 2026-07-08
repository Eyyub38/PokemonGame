using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerCompetitionBracketLog : MonoBehaviour, ISavable {
    const string PlayerEntrantId = "player";

    [Tooltip("Runtime/save history for generated competition brackets, tournament rosters and challenge runs.")]
    [SerializeField] List<PlayerCompetitionBracketState> bracketStates = new List<PlayerCompetitionBracketState>();

    public IReadOnlyList<PlayerCompetitionBracketState> BracketStates => bracketStates;
    public event Action<PlayerCompetitionBracketState> OnBracketGenerated;
    public event Action<PlayerCompetitionBracketState, PlayerCompetitionBracketMatchRecord> OnBracketMatchRecorded;
    public event Action<PlayerCompetitionBracketState> OnBracketCompleted;
    public event Action OnCompetitionBracketLogChanged;

    public bool GenerateBracket(CompetitionRosterDefinition roster, int seed, string sourceId, out PlayerCompetitionBracketState state, out string failureMessage) {
        state = null;
        if(roster == null) {
            failureMessage = "A competition roster is required.";
            return false;
        }

        var player = GetComponent<PlayerController>();
        if(!roster.CanGenerate(player, out failureMessage)) {
            return false;
        }

        state = roster.GenerateBracket(player, seed, sourceId);
        if(state == null) {
            failureMessage = "Bracket generation failed.";
            return false;
        }

        bracketStates.Add(state);
        OnBracketGenerated?.Invoke(state);
        OnCompetitionBracketLogChanged?.Invoke();
        PublishLogEvent("generated", state, null, sourceId, GameEventImportance.Info);
        failureMessage = null;
        return true;
    }

    public bool GenerateBracket(CompetitionRosterDefinition roster, int seed, string sourceId, out string failureMessage) {
        return GenerateBracket(roster, seed, sourceId, out _, out failureMessage);
    }

    public bool GenerateBracket(CompetitionRosterDefinition roster, out string failureMessage) {
        return GenerateBracket(roster, 0, null, out _, out failureMessage);
    }

    public bool RecordMatchResult(CompetitionRosterDefinition roster, string matchId, bool playerWon, string sourceId = null) {
        var state = GetActiveBracket(roster);
        var match = state?.GetMatch(matchId);
        if(match == null || match.completed) {
            return false;
        }

        string winnerEntrantId = ResolvePlayerResultWinner(match, playerWon);
        return RecordMatchWinner(roster, matchId, winnerEntrantId, sourceId);
    }

    public bool RecordMatchWinner(
        CompetitionRosterDefinition roster,
        string matchId,
        string winnerEntrantId,
        string sourceId = null,
        bool resolvedAutomatically = false,
        string resolverId = null,
        int firstResolvedPower = 0,
        int secondResolvedPower = 0
    ) {
        var state = GetActiveBracket(roster);
        var match = state?.GetMatch(matchId);
        if(state == null || match == null || match.completed || string.IsNullOrWhiteSpace(winnerEntrantId)) {
            return false;
        }

        if(!match.ContainsEntrant(winnerEntrantId)) {
            return false;
        }

        string loserEntrantId = match.GetOpponentId(winnerEntrantId);
        match.completed = true;
        match.completedTotalHour = GetCurrentTotalHour();
        match.winnerEntrantId = winnerEntrantId;
        match.winnerEntrantName = state.GetEntrantName(winnerEntrantId);
        match.loserEntrantId = loserEntrantId;
        match.loserEntrantName = state.GetEntrantName(loserEntrantId);
        match.playerInMatch = match.ContainsEntrant(PlayerEntrantId);
        match.playerWon = match.playerInMatch && string.Equals(winnerEntrantId, PlayerEntrantId, StringComparison.OrdinalIgnoreCase);
        match.sourceId = sourceId;
        match.resolvedAutomatically = resolvedAutomatically;
        match.resolverId = resolverId;
        match.firstResolvedPower = firstResolvedPower;
        match.secondResolvedPower = secondResolvedPower;

        state.matchAttemptCount++;
        state.lastMatchId = match.matchId;
        state.lastSourceId = sourceId;
        state.lastMatchTotalHour = match.completedTotalHour;

        if(match.playerInMatch) {
            if(match.playerWon) {
                state.matchWinCount++;
                MarkEntrantDefeated(state, loserEntrantId);
            } else {
                state.matchLossCount++;
                MarkEntrantDefeated(state, PlayerEntrantId);
            }
        } else if(!string.IsNullOrWhiteSpace(loserEntrantId)) {
            MarkEntrantDefeated(state, loserEntrantId);
        }

        UpdateBracketProgress(state, sourceId);
        OnBracketMatchRecorded?.Invoke(state, match);
        OnCompetitionBracketLogChanged?.Invoke();
        PublishLogEvent("match", state, match, sourceId, match.playerWon ? GameEventImportance.Success : GameEventImportance.Info);
        return true;
    }

    public bool CompleteBracket(CompetitionRosterDefinition roster, bool won, string sourceId = null) {
        var state = GetActiveBracket(roster);
        if(state == null) {
            return false;
        }

        CompleteBracketState(state, won, sourceId);
        return true;
    }

    public bool AbandonBracket(CompetitionRosterDefinition roster, string sourceId = null) {
        var state = GetActiveBracket(roster);
        if(state == null) {
            return false;
        }

        state.active = false;
        state.completed = false;
        state.abandoned = true;
        state.completedTotalHour = GetCurrentTotalHour();
        state.lastSourceId = sourceId;
        OnCompetitionBracketLogChanged?.Invoke();
        PublishLogEvent("abandoned", state, null, sourceId, GameEventImportance.Warning);
        return true;
    }

    public PlayerCompetitionBracketState GetActiveBracket(CompetitionRosterDefinition roster) {
        return GetLatestState(roster, state => state.active && !state.completed && !state.abandoned);
    }

    public PlayerCompetitionBracketState GetLatestBracket(CompetitionRosterDefinition roster) {
        return GetLatestState(roster, _ => true);
    }

    public bool HasGenerated(CompetitionRosterDefinition roster) {
        return GetGeneratedCount(roster) > 0;
    }

    public bool HasActiveBracket(CompetitionRosterDefinition roster) {
        return GetActiveBracket(roster) != null;
    }

    public bool HasCompleted(CompetitionRosterDefinition roster) {
        return GetCompletedCount(roster) > 0;
    }

    public bool HasWon(CompetitionRosterDefinition roster) {
        return GetWinCount(roster) > 0;
    }

    public int GetGeneratedCount(CompetitionRosterDefinition roster) {
        string rosterId = roster != null ? roster.Id : string.Empty;
        return string.IsNullOrWhiteSpace(rosterId)
            ? 0
            : bracketStates.Count(state => state != null && state.rosterId == rosterId);
    }

    public int GetCompletedCount(CompetitionRosterDefinition roster) {
        string rosterId = roster != null ? roster.Id : string.Empty;
        return string.IsNullOrWhiteSpace(rosterId)
            ? 0
            : bracketStates.Count(state => state != null && state.rosterId == rosterId && state.completed);
    }

    public int GetWinCount(CompetitionRosterDefinition roster) {
        string rosterId = roster != null ? roster.Id : string.Empty;
        return string.IsNullOrWhiteSpace(rosterId)
            ? 0
            : bracketStates.Count(state => state != null && state.rosterId == rosterId && state.completed && state.won);
    }

    public int GetMatchWinCount(CompetitionRosterDefinition roster) {
        string rosterId = roster != null ? roster.Id : string.Empty;
        return string.IsNullOrWhiteSpace(rosterId)
            ? 0
            : bracketStates.Where(state => state != null && state.rosterId == rosterId).Sum(state => Mathf.Max(0, state.matchWinCount));
    }

    public int GetCurrentRoundIndex(CompetitionRosterDefinition roster) {
        return Mathf.Max(0, GetActiveBracket(roster)?.currentRoundIndex ?? 0);
    }

    PlayerCompetitionBracketState GetLatestState(CompetitionRosterDefinition roster, Func<PlayerCompetitionBracketState, bool> predicate) {
        string rosterId = roster != null ? roster.Id : string.Empty;
        if(string.IsNullOrWhiteSpace(rosterId)) {
            return null;
        }

        return bracketStates
            .Where(state => state != null && state.rosterId == rosterId && predicate(state))
            .OrderByDescending(state => state.generatedTotalHour)
            .ThenByDescending(state => bracketStates.IndexOf(state))
            .FirstOrDefault();
    }

    void UpdateBracketProgress(PlayerCompetitionBracketState state, string sourceId) {
        if(state == null || state.completed || state.abandoned) {
            return;
        }

        if(state.HasPlayerLoss()) {
            CompleteBracketState(state, false, sourceId);
            return;
        }

        if(state.bracketFormat == CompetitionBracketFormat.SingleElimination) {
            AdvanceSingleElimination(state);
        }

        state.currentRoundIndex = state.GetFirstIncompleteRoundIndex();
        if(state.HasIncompleteMatches()) {
            return;
        }

        bool won = state.GetPlayerMatchCount() > 0 && state.GetPlayerLossCount() == 0;
        CompleteBracketState(state, won, sourceId);
    }

    void AdvanceSingleElimination(PlayerCompetitionBracketState state) {
        while(state != null && !state.completed && !state.HasPlayerLoss()) {
            var latestRound = state.GetLatestRound();
            if(latestRound == null || latestRound.HasIncompleteMatches()) {
                return;
            }

            var winnerIds = latestRound.Matches
                .Where(match => match != null && !string.IsNullOrWhiteSpace(match.winnerEntrantId))
                .Select(match => match.winnerEntrantId)
                .Distinct()
                .ToList();

            if(winnerIds.Count <= 1) {
                CompleteBracketState(state, winnerIds.Count == 1 && winnerIds[0] == PlayerEntrantId, state.lastSourceId);
                return;
            }

            int nextRoundIndex = latestRound.roundIndex + 1;
            if(state.rounds.Any(round => round != null && round.roundIndex == nextRoundIndex)) {
                return;
            }

            var winnerRecords = winnerIds
                .Select(state.GetEntrant)
                .Where(entrant => entrant != null)
                .OrderBy(entrant => entrant.slotIndex)
                .ToList();

            state.rounds.Add(CreateEliminationRound(state, nextRoundIndex, winnerRecords));
        }
    }

    PlayerCompetitionBracketRoundRecord CreateEliminationRound(PlayerCompetitionBracketState state, int roundIndex, List<PlayerCompetitionBracketEntrantRecord> entrants) {
        var matches = new List<PlayerCompetitionBracketMatchRecord>();
        for(int i = 0; i < entrants.Count; i += 2) {
            var first = entrants[i];
            var second = i + 1 < entrants.Count ? entrants[i + 1] : null;
            matches.Add(CreateMatch(state, roundIndex, matches.Count, first, second));
        }

        return new PlayerCompetitionBracketRoundRecord {
            roundIndex = roundIndex,
            roundName = $"Round {roundIndex + 1}",
            matches = matches
        };
    }

    PlayerCompetitionBracketMatchRecord CreateMatch(PlayerCompetitionBracketState state, int roundIndex, int matchIndex, PlayerCompetitionBracketEntrantRecord first, PlayerCompetitionBracketEntrantRecord second) {
        return new PlayerCompetitionBracketMatchRecord {
            matchId = $"{state.rosterId}.r{roundIndex}.m{matchIndex}",
            roundIndex = roundIndex,
            matchIndex = matchIndex,
            firstEntrantId = first != null ? first.entrantId : string.Empty,
            firstEntrantName = first != null ? first.entrantName : "Bye",
            secondEntrantId = second != null ? second.entrantId : string.Empty,
            secondEntrantName = second != null ? second.entrantName : "Bye",
            challengeId = ResolveChallengeId(first, second),
            ruleSetId = ResolveRuleSetId(state, first, second),
            completed = second == null,
            winnerEntrantId = second == null && first != null ? first.entrantId : string.Empty,
            winnerEntrantName = second == null && first != null ? first.entrantName : string.Empty,
            playerInMatch = (first != null && first.isPlayer) || (second != null && second.isPlayer),
            playerWon = second == null && first != null && first.isPlayer
        };
    }

    string ResolveChallengeId(PlayerCompetitionBracketEntrantRecord first, PlayerCompetitionBracketEntrantRecord second) {
        var opponent = ResolveOpponent(first, second);
        if(opponent != null && !string.IsNullOrWhiteSpace(opponent.challengeId)) {
            return opponent.challengeId;
        }

        return first != null ? first.challengeId : second != null ? second.challengeId : string.Empty;
    }

    string ResolveRuleSetId(PlayerCompetitionBracketState state, PlayerCompetitionBracketEntrantRecord first, PlayerCompetitionBracketEntrantRecord second) {
        var opponent = ResolveOpponent(first, second);
        if(opponent != null && !string.IsNullOrWhiteSpace(opponent.ruleSetId)) {
            return opponent.ruleSetId;
        }

        if(first != null && !string.IsNullOrWhiteSpace(first.ruleSetId)) {
            return first.ruleSetId;
        }

        if(second != null && !string.IsNullOrWhiteSpace(second.ruleSetId)) {
            return second.ruleSetId;
        }

        return state != null ? state.defaultRuleSetId : string.Empty;
    }

    PlayerCompetitionBracketEntrantRecord ResolveOpponent(PlayerCompetitionBracketEntrantRecord first, PlayerCompetitionBracketEntrantRecord second) {
        if(first != null && first.isPlayer) {
            return second;
        }

        if(second != null && second.isPlayer) {
            return first;
        }

        return first ?? second;
    }

    void CompleteBracketState(PlayerCompetitionBracketState state, bool won, string sourceId) {
        if(state == null || state.completed) {
            return;
        }

        state.active = false;
        state.completed = true;
        state.won = won;
        state.completedTotalHour = GetCurrentTotalHour();
        state.lastSourceId = sourceId;
        OnBracketCompleted?.Invoke(state);
        OnCompetitionBracketLogChanged?.Invoke();
        PublishLogEvent("completed", state, null, sourceId, won ? GameEventImportance.Success : GameEventImportance.Info);
    }

    void MarkEntrantDefeated(PlayerCompetitionBracketState state, string entrantId) {
        var entrant = state?.GetEntrant(entrantId);
        if(entrant != null) {
            entrant.defeated = true;
        }
    }

    string ResolvePlayerResultWinner(PlayerCompetitionBracketMatchRecord match, bool playerWon) {
        if(match == null) {
            return string.Empty;
        }

        if(playerWon && match.ContainsEntrant(PlayerEntrantId)) {
            return PlayerEntrantId;
        }

        if(match.ContainsEntrant(PlayerEntrantId)) {
            return match.GetOpponentId(PlayerEntrantId);
        }

        return playerWon ? match.firstEntrantId : match.secondEntrantId;
    }

    int GetCurrentTotalHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    void PublishLogEvent(string phase, PlayerCompetitionBracketState state, PlayerCompetitionBracketMatchRecord match, string sourceId, GameEventImportance importance) {
        GameEventPublishing.PublishOptional(
            null,
            $"competition-bracket-log.{phase}.{state?.rosterId}.{match?.matchId}",
            $"Competition bracket {phase}.",
            GameEventCategory.BattleRule,
            importance,
            this,
            "PlayerCompetitionBracketLog",
            GameEventScope.Player,
            showInFeed: false,
            writeToDebugLog: false,
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("rosterId", state != null ? state.rosterId : string.Empty),
            GameEventPublishing.Value("rosterName", state != null ? state.rosterName : string.Empty),
            GameEventPublishing.Value("competitionId", state != null ? state.competitionId : string.Empty),
            GameEventPublishing.Value("matchId", match != null ? match.matchId : string.Empty),
            GameEventPublishing.Value("winnerEntrantId", match != null ? match.winnerEntrantId : string.Empty),
            GameEventPublishing.Value("sourceId", sourceId));
    }

    public object CaptureState() {
        return new PlayerCompetitionBracketLogSaveData {
            bracketStates = bracketStates.Where(state => state != null).Select(state => state.Clone()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerCompetitionBracketLogSaveData;
        bracketStates = saveData?.bracketStates?.Where(entry => entry != null).Select(entry => entry.Clone()).ToList() ?? new List<PlayerCompetitionBracketState>();
        OnCompetitionBracketLogChanged?.Invoke();
    }
}

[Serializable]
public class PlayerCompetitionBracketState {
    [Tooltip("Saved roster id.")]
    public string rosterId;
    [Tooltip("Saved roster display name for fallback/debug output.")]
    public string rosterName;
    [Tooltip("Saved competition id linked to this bracket.")]
    public string competitionId;
    [Tooltip("Saved competition display name linked to this bracket.")]
    public string competitionName;
    [Tooltip("Saved season id linked to this bracket.")]
    public string seasonId;
    [Tooltip("Saved season display name linked to this bracket.")]
    public string seasonName;
    [Tooltip("Saved ranking id linked to this bracket.")]
    public string rankingId;
    [Tooltip("Saved ranking display name linked to this bracket.")]
    public string rankingName;
    [Tooltip("Default rule set id used by generated matches when entrants do not provide one.")]
    public string defaultRuleSetId;
    [Tooltip("Default rule set display name used by generated matches.")]
    public string defaultRuleSetName;
    [Tooltip("Bracket format used when this run was generated.")]
    public CompetitionBracketFormat bracketFormat;
    [Tooltip("Deterministic seed used by roster generation.")]
    public int seed;
    [Tooltip("Whether this bracket is currently active.")]
    public bool active;
    [Tooltip("Whether this bracket has reached a final result.")]
    public bool completed;
    [Tooltip("Whether this bracket was won by the player.")]
    public bool won;
    [Tooltip("Whether this bracket was manually abandoned before completion.")]
    public bool abandoned;
    [Tooltip("Current round index for future UI focus.")]
    [Min(0)]
    public int currentRoundIndex;
    [Tooltip("How many matches were recorded in this bracket.")]
    [Min(0)]
    public int matchAttemptCount;
    [Tooltip("How many player matches were won in this bracket.")]
    [Min(0)]
    public int matchWinCount;
    [Tooltip("How many player matches were lost in this bracket.")]
    [Min(0)]
    public int matchLossCount;
    [Tooltip("In-game total hour when this bracket was generated.")]
    public int generatedTotalHour = -1;
    [Tooltip("In-game total hour when this bracket completed or was abandoned.")]
    public int completedTotalHour = -1;
    [Tooltip("In-game total hour of the last recorded match.")]
    public int lastMatchTotalHour = -1;
    [Tooltip("Last recorded match id.")]
    public string lastMatchId;
    [Tooltip("Short source id that last changed this bracket.")]
    public string sourceId;
    [Tooltip("Short source id that last changed this bracket after generation.")]
    public string lastSourceId;
    [Tooltip("Generated entrant records for this bracket.")]
    public List<PlayerCompetitionBracketEntrantRecord> entrants = new List<PlayerCompetitionBracketEntrantRecord>();
    [Tooltip("Generated round and match records for this bracket.")]
    public List<PlayerCompetitionBracketRoundRecord> rounds = new List<PlayerCompetitionBracketRoundRecord>();

    public IReadOnlyList<PlayerCompetitionBracketEntrantRecord> Entrants => entrants != null ? (IReadOnlyList<PlayerCompetitionBracketEntrantRecord>)entrants : Array.Empty<PlayerCompetitionBracketEntrantRecord>();
    public IReadOnlyList<PlayerCompetitionBracketRoundRecord> Rounds => rounds != null ? (IReadOnlyList<PlayerCompetitionBracketRoundRecord>)rounds : Array.Empty<PlayerCompetitionBracketRoundRecord>();

    public PlayerCompetitionBracketMatchRecord GetMatch(string matchId) {
        if(string.IsNullOrWhiteSpace(matchId)) {
            return null;
        }

        return Rounds
            .SelectMany(round => round?.Matches ?? Array.Empty<PlayerCompetitionBracketMatchRecord>())
            .FirstOrDefault(match => match != null && match.matchId == matchId);
    }

    public PlayerCompetitionBracketRoundRecord GetCurrentRound() {
        return Rounds.FirstOrDefault(round => round != null && round.roundIndex == currentRoundIndex)
            ?? Rounds.FirstOrDefault(round => round != null && round.HasIncompleteMatches())
            ?? Rounds.LastOrDefault(round => round != null);
    }

    public PlayerCompetitionBracketRoundRecord GetLatestRound() {
        return Rounds
            .Where(round => round != null)
            .OrderByDescending(round => round.roundIndex)
            .FirstOrDefault();
    }

    public int GetFirstIncompleteRoundIndex() {
        var round = Rounds
            .Where(entry => entry != null)
            .OrderBy(entry => entry.roundIndex)
            .FirstOrDefault(entry => entry.HasIncompleteMatches());

        return Mathf.Max(0, round?.roundIndex ?? currentRoundIndex);
    }

    public PlayerCompetitionBracketEntrantRecord GetEntrant(string entrantId) {
        return string.IsNullOrWhiteSpace(entrantId)
            ? null
            : Entrants.FirstOrDefault(entrant => entrant != null && entrant.entrantId == entrantId);
    }

    public string GetEntrantName(string entrantId) {
        return GetEntrant(entrantId)?.entrantName ?? string.Empty;
    }

    public bool HasIncompleteMatches() {
        return Rounds.Any(round => round != null && round.HasIncompleteMatches());
    }

    public bool HasPlayerLoss() {
        return Rounds
            .SelectMany(round => round?.Matches ?? Array.Empty<PlayerCompetitionBracketMatchRecord>())
            .Any(match => match != null && match.completed && match.playerInMatch && !match.playerWon);
    }

    public int GetPlayerMatchCount() {
        return Rounds
            .SelectMany(round => round?.Matches ?? Array.Empty<PlayerCompetitionBracketMatchRecord>())
            .Count(match => match != null && match.playerInMatch);
    }

    public int GetPlayerLossCount() {
        return Rounds
            .SelectMany(round => round?.Matches ?? Array.Empty<PlayerCompetitionBracketMatchRecord>())
            .Count(match => match != null && match.completed && match.playerInMatch && !match.playerWon);
    }

    public PlayerCompetitionBracketState Clone() {
        return new PlayerCompetitionBracketState {
            rosterId = rosterId,
            rosterName = rosterName,
            competitionId = competitionId,
            competitionName = competitionName,
            seasonId = seasonId,
            seasonName = seasonName,
            rankingId = rankingId,
            rankingName = rankingName,
            defaultRuleSetId = defaultRuleSetId,
            defaultRuleSetName = defaultRuleSetName,
            bracketFormat = bracketFormat,
            seed = seed,
            active = active,
            completed = completed,
            won = won,
            abandoned = abandoned,
            currentRoundIndex = currentRoundIndex,
            matchAttemptCount = matchAttemptCount,
            matchWinCount = matchWinCount,
            matchLossCount = matchLossCount,
            generatedTotalHour = generatedTotalHour,
            completedTotalHour = completedTotalHour,
            lastMatchTotalHour = lastMatchTotalHour,
            lastMatchId = lastMatchId,
            sourceId = sourceId,
            lastSourceId = lastSourceId,
            entrants = entrants?.Where(entry => entry != null).Select(entry => entry.Clone()).ToList() ?? new List<PlayerCompetitionBracketEntrantRecord>(),
            rounds = rounds?.Where(round => round != null).Select(round => round.Clone()).ToList() ?? new List<PlayerCompetitionBracketRoundRecord>()
        };
    }
}

[Serializable]
public class PlayerCompetitionBracketEntrantRecord {
    [Tooltip("Saved entrant id.")]
    public string entrantId;
    [Tooltip("Saved entrant display name.")]
    public string entrantName;
    [Tooltip("Saved entrant kind.")]
    public CompetitionEntrantKind kind;
    [Tooltip("Generated bracket slot index.")]
    [Min(0)]
    public int slotIndex;
    [Tooltip("Whether this record represents the player.")]
    public bool isPlayer;
    [Tooltip("Whether this entrant has been defeated in this bracket.")]
    public bool defeated;
    [Tooltip("Saved battle challenge id for this entrant.")]
    public string challengeId;
    [Tooltip("Saved battle challenge display name for this entrant.")]
    public string challengeName;
    [Tooltip("Saved rule set id for this entrant.")]
    public string ruleSetId;
    [Tooltip("Saved rule set display name for this entrant.")]
    public string ruleSetName;
    [Tooltip("Saved trainer party template id for this entrant.")]
    public string partyTemplateId;
    [Tooltip("Saved trainer party template display name for this entrant.")]
    public string partyTemplateName;
    [Tooltip("Deterministic party seed generated for this entrant.")]
    public int partySeed;
    [Tooltip("Seeded rank copied from the entrant definition. Lower values usually mean stronger seeding.")]
    public int seededRank;
    [Tooltip("Selection weight copied from the entrant definition.")]
    [Min(0)]
    public int selectionWeight;
    [Tooltip("Tags copied from the entrant definition for simulation and future UI filters.")]
    public List<string> tags = new List<string>();

    public PlayerCompetitionBracketEntrantRecord Clone() {
        return new PlayerCompetitionBracketEntrantRecord {
            entrantId = entrantId,
            entrantName = entrantName,
            kind = kind,
            slotIndex = slotIndex,
            isPlayer = isPlayer,
            defeated = defeated,
            challengeId = challengeId,
            challengeName = challengeName,
            ruleSetId = ruleSetId,
            ruleSetName = ruleSetName,
            partyTemplateId = partyTemplateId,
            partyTemplateName = partyTemplateName,
            partySeed = partySeed,
            seededRank = seededRank,
            selectionWeight = selectionWeight,
            tags = tags != null ? tags.Where(tag => !string.IsNullOrWhiteSpace(tag)).Distinct().ToList() : new List<string>()
        };
    }
}

[Serializable]
public class PlayerCompetitionBracketRoundRecord {
    [Tooltip("Round index inside the generated bracket.")]
    [Min(0)]
    public int roundIndex;
    [Tooltip("Round display name for future UI.")]
    public string roundName;
    [Tooltip("Generated matches in this round.")]
    public List<PlayerCompetitionBracketMatchRecord> matches = new List<PlayerCompetitionBracketMatchRecord>();

    public IReadOnlyList<PlayerCompetitionBracketMatchRecord> Matches => matches != null ? (IReadOnlyList<PlayerCompetitionBracketMatchRecord>)matches : Array.Empty<PlayerCompetitionBracketMatchRecord>();

    public bool HasIncompleteMatches() {
        return Matches.Any(match => match != null && !match.completed);
    }

    public PlayerCompetitionBracketRoundRecord Clone() {
        return new PlayerCompetitionBracketRoundRecord {
            roundIndex = roundIndex,
            roundName = roundName,
            matches = matches?.Where(match => match != null).Select(match => match.Clone()).ToList() ?? new List<PlayerCompetitionBracketMatchRecord>()
        };
    }
}

[Serializable]
public class PlayerCompetitionBracketMatchRecord {
    [Tooltip("Stable match id inside the generated bracket.")]
    public string matchId;
    [Tooltip("Round index that owns this match.")]
    [Min(0)]
    public int roundIndex;
    [Tooltip("Match index inside its round.")]
    [Min(0)]
    public int matchIndex;
    [Tooltip("First entrant id.")]
    public string firstEntrantId;
    [Tooltip("First entrant display name.")]
    public string firstEntrantName;
    [Tooltip("Second entrant id.")]
    public string secondEntrantId;
    [Tooltip("Second entrant display name.")]
    public string secondEntrantName;
    [Tooltip("Battle challenge id this match should launch when played by the player.")]
    public string challengeId;
    [Tooltip("Battle rule set id this match should use.")]
    public string ruleSetId;
    [Tooltip("Whether this match result has been recorded.")]
    public bool completed;
    [Tooltip("Whether this match includes the player.")]
    public bool playerInMatch;
    [Tooltip("Whether the player won this match, if the player participated.")]
    public bool playerWon;
    [Tooltip("Winner entrant id.")]
    public string winnerEntrantId;
    [Tooltip("Winner entrant display name.")]
    public string winnerEntrantName;
    [Tooltip("Loser entrant id.")]
    public string loserEntrantId;
    [Tooltip("Loser entrant display name.")]
    public string loserEntrantName;
    [Tooltip("In-game total hour when this match completed.")]
    public int completedTotalHour = -1;
    [Tooltip("Short source id that recorded this match.")]
    public string sourceId;
    [Tooltip("Whether this match was resolved by a bracket simulation resolver.")]
    public bool resolvedAutomatically;
    [Tooltip("Resolver id that simulated this match.")]
    public string resolverId;
    [Tooltip("Calculated power for the first entrant when this match was simulated.")]
    public int firstResolvedPower;
    [Tooltip("Calculated power for the second entrant when this match was simulated.")]
    public int secondResolvedPower;

    public bool ContainsEntrant(string entrantId) {
        return !string.IsNullOrWhiteSpace(entrantId)
            && (string.Equals(firstEntrantId, entrantId, StringComparison.OrdinalIgnoreCase)
                || string.Equals(secondEntrantId, entrantId, StringComparison.OrdinalIgnoreCase));
    }

    public string GetOpponentId(string entrantId) {
        if(string.IsNullOrWhiteSpace(entrantId)) {
            return string.Empty;
        }

        if(string.Equals(firstEntrantId, entrantId, StringComparison.OrdinalIgnoreCase)) {
            return secondEntrantId;
        }

        if(string.Equals(secondEntrantId, entrantId, StringComparison.OrdinalIgnoreCase)) {
            return firstEntrantId;
        }

        return string.Empty;
    }

    public PlayerCompetitionBracketMatchRecord Clone() {
        return new PlayerCompetitionBracketMatchRecord {
            matchId = matchId,
            roundIndex = roundIndex,
            matchIndex = matchIndex,
            firstEntrantId = firstEntrantId,
            firstEntrantName = firstEntrantName,
            secondEntrantId = secondEntrantId,
            secondEntrantName = secondEntrantName,
            challengeId = challengeId,
            ruleSetId = ruleSetId,
            completed = completed,
            playerInMatch = playerInMatch,
            playerWon = playerWon,
            winnerEntrantId = winnerEntrantId,
            winnerEntrantName = winnerEntrantName,
            loserEntrantId = loserEntrantId,
            loserEntrantName = loserEntrantName,
            completedTotalHour = completedTotalHour,
            sourceId = sourceId,
            resolvedAutomatically = resolvedAutomatically,
            resolverId = resolverId,
            firstResolvedPower = firstResolvedPower,
            secondResolvedPower = secondResolvedPower
        };
    }
}

[Serializable]
public class PlayerCompetitionBracketLogSaveData {
    [Tooltip("Saved generated bracket history.")]
    public List<PlayerCompetitionBracketState> bracketStates = new List<PlayerCompetitionBracketState>();
}
