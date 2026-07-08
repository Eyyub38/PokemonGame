using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CompetitionBracketSource : MonoBehaviour, IPlayerTriggerable {
    const string PlayerEntrantId = "player";

    [Header("Roster")]
    [Tooltip("Roster used by this NPC, desk, gate or terminal to generate and run a bracket.")]
    [SerializeField] CompetitionRosterDefinition roster;
    [Tooltip("Optional rule set forced by this source. Empty uses the match, entrant, roster or challenge default.")]
    [SerializeField] BattleRuleSetDefinition forcedRuleSet;
    [Tooltip("Optional venue, arena, gym or stadium hosting this bracket source.")]
    [SerializeField] CompetitionVenueDefinition venue;
    [Tooltip("Short source id written into logs. Empty uses this GameObject name.")]
    [SerializeField] string sourceId = "competition-bracket-source";

    [Header("Trigger")]
    [Tooltip("If enabled, player trigger creates a bracket when no active bracket exists.")]
    [SerializeField] bool generateBracketOnPlayerTrigger = true;
    [Tooltip("If enabled, player trigger prepares the next available player match through BattleRuleManager.")]
    [SerializeField] bool prepareMatchOnPlayerTrigger = true;
    [Tooltip("If enabled, TryPrepareNextMatch creates a bracket when the player has no active bracket for this roster.")]
    [SerializeField] bool autoGenerateBracketWhenMissing = true;
    [Tooltip("If enabled, this trigger can be called repeatedly by the player.")]
    [SerializeField] bool triggerRepeatedly = true;

    [Header("Result Recording")]
    [Tooltip("If enabled, BattleRuleManager completion events from this source automatically record bracket results.")]
    [SerializeField] bool recordBracketResultOnBattleCompletion = true;
    [Tooltip("If enabled, match completion also attempts to record linked CompetitionDefinition progress.")]
    [SerializeField] bool recordCompetitionProgressOnMatchComplete = true;
    [Tooltip("If enabled, prize tables are evaluated after a match result is recorded.")]
    [SerializeField] bool applyPrizeTablesOnMatchResult = true;
    [Tooltip("If enabled, prize tables are evaluated when the bracket reaches a completed result.")]
    [SerializeField] bool applyPrizeTablesOnBracketCompletion = true;
    [Tooltip("Prize tables evaluated by this bracket source after match and bracket results.")]
    [SerializeField] List<CompetitionPrizeTableDefinition> prizeTables = new List<CompetitionPrizeTableDefinition>();
    [Tooltip("If enabled, pending match data is cleared after a battle completion event is processed.")]
    [SerializeField] bool clearPendingMatchAfterResult = true;

    [Header("Simulation")]
    [Tooltip("Resolver used to simulate NPC vs NPC bracket matches.")]
    [SerializeField] CompetitionMatchResolverDefinition matchResolver;
    [Tooltip("If enabled, NPC-only matches are resolved after a new bracket is generated.")]
    [SerializeField] bool resolveNpcMatchesOnBracketGenerated = true;
    [Tooltip("If enabled, NPC-only matches are resolved before preparing the next player match.")]
    [SerializeField] bool resolveNpcMatchesBeforePreparingPlayerMatch = true;
    [Tooltip("If enabled, NPC-only matches are resolved after recording a player match result.")]
    [SerializeField] bool resolveNpcMatchesAfterPlayerMatchResult = true;
    [Tooltip("Maximum automatic match resolutions attempted in one pass. Protects against broken bracket loops.")]
    [Min(1)]
    [SerializeField] int maxAutoResolveIterations = 32;

    [Header("Debug")]
    [Tooltip("If enabled, blocked trigger or battle preparation attempts are written to GameDebug.")]
    [SerializeField] bool logBlockedAttempts = true;
    [Tooltip("If enabled, successful bracket and match steps are written to GameDebug.")]
    [SerializeField] bool logSuccessfulAttempts;
    [Tooltip("Last match id prepared by this source.")]
    [SerializeField] string pendingMatchId;
    [Tooltip("Last BattleRuleContext source id prepared by this source.")]
    [SerializeField] string pendingBattleSourceId;

    BattleRuleManager subscribedManager;

    public bool TriggerRepeatedly => triggerRepeatedly;
    public CompetitionRosterDefinition Roster => roster;
    public BattleRuleSetDefinition ForcedRuleSet => forcedRuleSet;
    public CompetitionVenueDefinition Venue => venue;
    public IReadOnlyList<CompetitionPrizeTableDefinition> PrizeTables => prizeTables;
    public CompetitionMatchResolverDefinition MatchResolver => matchResolver;
    public string PendingMatchId => pendingMatchId;
    public string PendingBattleSourceId => pendingBattleSourceId;

    void OnDisable() {
        UnsubscribeFromBattleRuleManager();
    }

    public void OnPlayerTriggered(PlayerController player) {
        if(generateBracketOnPlayerTrigger) {
            TryGenerateBracket(player, out _);
        }

        if(prepareMatchOnPlayerTrigger) {
            TryPrepareNextMatch(player, out _);
        }
    }

    public bool TryGenerateBracket(PlayerController player, out string failureMessage) {
        return TryGenerateBracket(player, 0, out _, out failureMessage);
    }

    public bool TryGenerateBracket(PlayerController player, int seed, out PlayerCompetitionBracketState state, out string failureMessage) {
        state = null;
        if(roster == null) {
            failureMessage = "No competition roster is assigned.";
            LogBlocked(player, failureMessage);
            return false;
        }

        if(venue != null && !venue.CanHost(player, roster, out failureMessage)) {
            venue.RecordUse(player, CompetitionVenuePurpose.Bracket, null, roster, ResolveSourceId(), this, blocked: true, failureMessage);
            LogBlocked(player, failureMessage);
            return false;
        }

        var log = player != null ? player.GetComponent<PlayerCompetitionBracketLog>() : null;
        if(log == null) {
            failureMessage = "PlayerCompetitionBracketLog is missing.";
            LogBlocked(player, failureMessage);
            return false;
        }

        if(log.GetActiveBracket(roster) != null) {
            state = log.GetActiveBracket(roster);
            failureMessage = null;
            return true;
        }

        if(!log.GenerateBracket(roster, seed, ResolveSourceId(), out state, out failureMessage)) {
            LogBlocked(player, failureMessage);
            return false;
        }

        venue?.RecordUse(player, CompetitionVenuePurpose.Bracket, null, roster, ResolveSourceId(), this, blocked: false, null);

        if(resolveNpcMatchesOnBracketGenerated) {
            AutoResolveNpcMatches(player, state);
        }

        if(logSuccessfulAttempts) {
            GameDebug.Success($"{roster.DisplayName} bracket generated.", GameDebugCategory.BattleRule, this, "CompetitionBracketSource");
        }

        return true;
    }

    public bool TryPrepareNextMatch(PlayerController player, out string failureMessage) {
        if(!TryGetOrCreateActiveBracket(player, out var state, out failureMessage)) {
            return false;
        }

        if(resolveNpcMatchesBeforePreparingPlayerMatch) {
            AutoResolveNpcMatches(player, state);
        }

        if(venue != null && !venue.CanHost(player, roster, out failureMessage)) {
            venue.RecordUse(player, CompetitionVenuePurpose.Match, null, roster, ResolveSourceId(), this, blocked: true, failureMessage);
            LogBlocked(player, failureMessage);
            return false;
        }

        var match = GetNextPlayerMatch(state);
        if(match == null) {
            failureMessage = "No available player match was found in this bracket.";
            LogBlocked(player, failureMessage);
            return false;
        }

        var challenge = ResolveChallenge(match);
        if(challenge == null) {
            failureMessage = $"Match '{match.matchId}' has no battle challenge. Assign a challenge to its entrant.";
            LogBlocked(player, failureMessage);
            return false;
        }

        var ruleSet = ResolveRuleSet(player, match, challenge);
        var manager = BattleRuleManager.Ensure();
        if(manager.HasActiveRule) {
            failureMessage = "Another battle rule context is already active.";
            LogBlocked(player, failureMessage);
            return false;
        }

        string battleSourceId = ResolveBattleSourceId(match);
        if(!manager.PrepareChallenge(player, challenge, ruleSet, battleSourceId, out failureMessage)) {
            LogBlocked(player, failureMessage);
            return false;
        }

        pendingMatchId = match.matchId;
        pendingBattleSourceId = battleSourceId;
        SubscribeToBattleRuleManager(manager);
        venue?.RecordUse(player, CompetitionVenuePurpose.Match, null, roster, battleSourceId, this, blocked: false, null);

        if(logSuccessfulAttempts) {
            GameDebug.Success($"Prepared bracket match {match.matchId}.", GameDebugCategory.BattleRule, this, "CompetitionBracketSource");
        }

        return true;
    }

    public bool TryRecordPreparedMatchResult(PlayerController player, bool won, out string failureMessage) {
        if(string.IsNullOrWhiteSpace(pendingMatchId)) {
            failureMessage = "No pending bracket match is prepared.";
            LogBlocked(player, failureMessage);
            return false;
        }

        bool recorded = TryRecordMatchResult(player, pendingMatchId, won, out failureMessage);
        if(recorded && clearPendingMatchAfterResult) {
            ClearPendingMatch();
        }

        return recorded;
    }

    public bool TryRecordMatchResult(PlayerController player, string matchId, bool won, out string failureMessage) {
        if(roster == null) {
            failureMessage = "No competition roster is assigned.";
            LogBlocked(player, failureMessage);
            return false;
        }

        var log = player != null ? player.GetComponent<PlayerCompetitionBracketLog>() : null;
        if(log == null) {
            failureMessage = "PlayerCompetitionBracketLog is missing.";
            LogBlocked(player, failureMessage);
            return false;
        }

        var state = log.GetActiveBracket(roster);
        var match = state?.GetMatch(matchId);
        if(match == null) {
            failureMessage = $"Bracket match '{matchId}' was not found.";
            LogBlocked(player, failureMessage);
            return false;
        }

        var challenge = ResolveChallenge(match);
        var ruleSet = ResolveRuleSet(player, match, challenge);
        if(!log.RecordMatchResult(roster, match.matchId, won, ResolveBattleSourceId(match))) {
            failureMessage = $"Bracket match '{match.matchId}' could not be recorded.";
            LogBlocked(player, failureMessage);
            return false;
        }

        if(resolveNpcMatchesAfterPlayerMatchResult) {
            AutoResolveNpcMatches(player, state);
        }

        TryRecordCompetitionProgress(player, match, challenge, ruleSet, won);
        ApplyPrizeTables(player, state, match, challenge, ruleSet, won);
        failureMessage = null;
        return true;
    }

    public PlayerCompetitionBracketMatchRecord GetNextPlayerMatch(PlayerController player) {
        var state = player != null ? player.GetComponent<PlayerCompetitionBracketLog>()?.GetActiveBracket(roster) : null;
        return GetNextPlayerMatch(state);
    }

    PlayerCompetitionBracketMatchRecord GetNextPlayerMatch(PlayerCompetitionBracketState state) {
        if(state == null) {
            return null;
        }

        var currentRoundMatch = state.GetCurrentRound()?.Matches
            .FirstOrDefault(match => match != null && !match.completed && match.ContainsEntrant(PlayerEntrantId));
        if(currentRoundMatch != null) {
            return currentRoundMatch;
        }

        return state.Rounds
            .SelectMany(round => round?.Matches ?? new List<PlayerCompetitionBracketMatchRecord>())
            .FirstOrDefault(match => match != null && !match.completed && match.ContainsEntrant(PlayerEntrantId));
    }

    bool TryGetOrCreateActiveBracket(PlayerController player, out PlayerCompetitionBracketState state, out string failureMessage) {
        state = null;
        if(roster == null) {
            failureMessage = "No competition roster is assigned.";
            LogBlocked(player, failureMessage);
            return false;
        }

        var log = player != null ? player.GetComponent<PlayerCompetitionBracketLog>() : null;
        state = log != null ? log.GetActiveBracket(roster) : null;
        if(state != null) {
            failureMessage = null;
            return true;
        }

        if(!autoGenerateBracketWhenMissing) {
            failureMessage = "No active bracket exists for this roster.";
            LogBlocked(player, failureMessage);
            return false;
        }

        return TryGenerateBracket(player, 0, out state, out failureMessage);
    }

    void TryRecordCompetitionProgress(PlayerController player, PlayerCompetitionBracketMatchRecord match, BattleChallengeDefinition challenge, BattleRuleSetDefinition ruleSet, bool won) {
        if(!recordCompetitionProgressOnMatchComplete || player == null || roster?.Competition == null || challenge == null) {
            return;
        }

        if(!roster.Competition.TryRecordChallengeResult(player, challenge, ruleSet, won, ResolveBattleSourceId(match), out string failureMessage)) {
            GameDebug.Warning(failureMessage, GameDebugCategory.BattleRule, this, "CompetitionBracketSource");
        }
    }

    void ApplyPrizeTables(PlayerController player, PlayerCompetitionBracketState state, PlayerCompetitionBracketMatchRecord match, BattleChallengeDefinition challenge, BattleRuleSetDefinition ruleSet, bool won) {
        if(player == null || prizeTables == null || prizeTables.Count == 0) {
            return;
        }

        if(applyPrizeTablesOnMatchResult) {
            ApplyPrizeTables(player, CompetitionPrizeTrigger.MatchCompleted, state, match, challenge, ruleSet, won);
            ApplyPrizeTables(player, won ? CompetitionPrizeTrigger.MatchWon : CompetitionPrizeTrigger.MatchLost, state, match, challenge, ruleSet, won);
        }

        if(applyPrizeTablesOnBracketCompletion && state != null && state.completed) {
            ApplyPrizeTables(player, CompetitionPrizeTrigger.BracketCompleted, state, match, challenge, ruleSet, state.won);
            ApplyPrizeTables(player, state.won ? CompetitionPrizeTrigger.BracketWon : CompetitionPrizeTrigger.BracketLost, state, match, challenge, ruleSet, state.won);
        }
    }

    void ApplyPrizeTables(PlayerController player, CompetitionPrizeTrigger trigger, PlayerCompetitionBracketState state, PlayerCompetitionBracketMatchRecord match, BattleChallengeDefinition challenge, BattleRuleSetDefinition ruleSet, bool won) {
        var context = new CompetitionPrizeContext(trigger, roster, state, match, challenge, ruleSet, won, ResolveBattleSourceId(match));
        foreach(var prizeTable in prizeTables) {
            if(prizeTable == null) {
                continue;
            }

            if(!prizeTable.TryApply(player, context, out string failureMessage) && logBlockedAttempts && !string.IsNullOrWhiteSpace(failureMessage)) {
                GameDebug.Step(failureMessage, GameDebugCategory.BattleRule, this, "CompetitionBracketSource");
            }
        }
    }

    public int AutoResolveNpcMatches(PlayerController player) {
        var state = player != null ? player.GetComponent<PlayerCompetitionBracketLog>()?.GetActiveBracket(roster) : null;
        return AutoResolveNpcMatches(player, state);
    }

    int AutoResolveNpcMatches(PlayerController player, PlayerCompetitionBracketState state) {
        if(player == null || roster == null || matchResolver == null || state == null) {
            return 0;
        }

        var log = player.GetComponent<PlayerCompetitionBracketLog>();
        if(log == null) {
            return 0;
        }

        int resolvedCount = 0;
        int limit = Mathf.Max(1, maxAutoResolveIterations);
        for(int i = 0; i < limit && state != null && state.active && !state.completed && !state.abandoned; i++) {
            var match = GetNextNpcMatch(state);
            if(match == null) {
                break;
            }

            string simulationSourceId = ResolveSimulationSourceId(match);
            if(!matchResolver.TryResolve(roster, state, match, simulationSourceId, out var result, out string failureMessage)) {
                if(logBlockedAttempts && !string.IsNullOrWhiteSpace(failureMessage)) {
                    GameDebug.Step(failureMessage, GameDebugCategory.BattleRule, this, "CompetitionBracketSource");
                }
                break;
            }

            if(result == null || !log.RecordMatchWinner(roster, match.matchId, result.WinnerEntrantId, simulationSourceId, true, result.ResolverId, result.FirstPower, result.SecondPower)) {
                break;
            }

            resolvedCount++;
            state = log.GetActiveBracket(roster) ?? state;
        }

        if(resolvedCount == limit && logBlockedAttempts) {
            GameDebug.Warning($"Auto-resolver reached the iteration limit ({limit}) for {roster.DisplayName}.", GameDebugCategory.BattleRule, this, "CompetitionBracketSource");
        }

        return resolvedCount;
    }

    PlayerCompetitionBracketMatchRecord GetNextNpcMatch(PlayerCompetitionBracketState state) {
        if(state == null) {
            return null;
        }

        return state.Rounds
            .Where(round => round != null)
            .OrderBy(round => round.roundIndex)
            .SelectMany(round => round.Matches)
            .FirstOrDefault(match => match != null && !match.completed && !match.playerInMatch);
    }

    BattleChallengeDefinition ResolveChallenge(PlayerCompetitionBracketMatchRecord match) {
        BattleChallengeDefinition resolvedChallenge = null;
        if(match != null && !string.IsNullOrWhiteSpace(match.challengeId)) {
            resolvedChallenge = GetMatchEntrants(match)
                .Select(entrant => entrant != null ? entrant.Challenge : null)
                .FirstOrDefault(challenge => challenge != null && challenge.Id == match.challengeId);

            if(resolvedChallenge == null) {
                resolvedChallenge = Resources.LoadAll<BattleChallengeDefinition>("")
                    .FirstOrDefault(challenge => challenge != null && challenge.Id == match.challengeId);
            }
        }

        return venue != null ? venue.ResolveChallenge(resolvedChallenge) : resolvedChallenge;
    }

    BattleRuleSetDefinition ResolveRuleSet(PlayerController player, PlayerCompetitionBracketMatchRecord match, BattleChallengeDefinition challenge) {
        if(forcedRuleSet != null) {
            return forcedRuleSet;
        }

        BattleRuleSetDefinition resolvedRuleSet = null;
        if(match != null && !string.IsNullOrWhiteSpace(match.ruleSetId)) {
            if(roster?.DefaultRuleSet != null && roster.DefaultRuleSet.Id == match.ruleSetId) {
                resolvedRuleSet = roster.DefaultRuleSet;
            }

            if(resolvedRuleSet == null) {
                resolvedRuleSet = GetMatchEntrants(match)
                    .Select(entrant => entrant != null ? entrant.DefaultRuleSet : null)
                    .FirstOrDefault(rule => rule != null && rule.Id == match.ruleSetId);
            }

            if(resolvedRuleSet == null) {
                resolvedRuleSet = Resources.LoadAll<BattleRuleSetDefinition>("")
                    .FirstOrDefault(rule => rule != null && rule.Id == match.ruleSetId);
            }
        }

        resolvedRuleSet ??= roster?.DefaultRuleSet != null ? roster.DefaultRuleSet : challenge?.DefaultRuleSet;
        return venue != null ? venue.ResolveRuleSet(player, resolvedRuleSet, out _) : resolvedRuleSet;
    }

    List<CompetitionEntrantDefinition> GetMatchEntrants(PlayerCompetitionBracketMatchRecord match) {
        if(match == null || roster == null) {
            return new List<CompetitionEntrantDefinition>();
        }

        return roster.Entrants
            .Where(entrant => entrant != null
                && (entrant.Id == match.firstEntrantId || entrant.Id == match.secondEntrantId))
            .ToList();
    }

    void HandleBattleContextCompleted(BattleRuleContext context, bool won) {
        if(!recordBracketResultOnBattleCompletion || context == null) {
            return;
        }

        if(string.IsNullOrWhiteSpace(pendingBattleSourceId)
            || !string.Equals(context.SourceId, pendingBattleSourceId, System.StringComparison.OrdinalIgnoreCase)) {
            return;
        }

        TryRecordPreparedMatchResult(context.Player, won, out _);
    }

    void SubscribeToBattleRuleManager(BattleRuleManager manager) {
        if(manager == null || subscribedManager == manager) {
            return;
        }

        UnsubscribeFromBattleRuleManager();
        subscribedManager = manager;
        subscribedManager.OnRuleContextCompleted += HandleBattleContextCompleted;
    }

    void UnsubscribeFromBattleRuleManager() {
        if(subscribedManager == null) {
            return;
        }

        subscribedManager.OnRuleContextCompleted -= HandleBattleContextCompleted;
        subscribedManager = null;
    }

    void ClearPendingMatch() {
        pendingMatchId = null;
        pendingBattleSourceId = null;
    }

    string ResolveSourceId() {
        return string.IsNullOrWhiteSpace(sourceId) ? name : sourceId;
    }

    string ResolveBattleSourceId(PlayerCompetitionBracketMatchRecord match) {
        return $"{ResolveSourceId()}:{roster?.Id}:{match?.matchId}";
    }

    string ResolveSimulationSourceId(PlayerCompetitionBracketMatchRecord match) {
        return $"{ResolveSourceId()}:simulation:{roster?.Id}:{match?.matchId}";
    }

    void LogBlocked(PlayerController player, string failureMessage) {
        if(!logBlockedAttempts) {
            return;
        }

        GameDebug.Warning(failureMessage, GameDebugCategory.BattleRule, player != null ? player : this, "CompetitionBracketSource");
    }
}
