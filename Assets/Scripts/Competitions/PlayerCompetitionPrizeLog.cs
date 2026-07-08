using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerCompetitionPrizeLog : MonoBehaviour, ISavable {
    [Tooltip("Runtime/save history of competition prize tables awarded to this player.")]
    [SerializeField] List<PlayerCompetitionPrizeAwardRecord> awardHistory = new List<PlayerCompetitionPrizeAwardRecord>();

    public IReadOnlyList<PlayerCompetitionPrizeAwardRecord> AwardHistory => awardHistory;
    public event Action<CompetitionPrizeTableDefinition, PlayerCompetitionPrizeAwardRecord> OnPrizeAwarded;
    public event Action OnCompetitionPrizeLogChanged;

    public bool CanAward(CompetitionPrizeTableDefinition prizeTable, CompetitionPrizeContext context, out string failureMessage) {
        if(prizeTable == null) {
            failureMessage = "A prize table is required.";
            return false;
        }

        var repeatMode = prizeTable.RepeatMode;
        if(repeatMode == CompetitionPrizeRepeatMode.Always) {
            failureMessage = null;
            return true;
        }

        string contextKey = context != null ? context.BuildContextKey(repeatMode) : prizeTable.Id;
        if(string.IsNullOrWhiteSpace(contextKey)) {
            contextKey = prizeTable.Id;
        }

        if(repeatMode == CompetitionPrizeRepeatMode.CooldownHours) {
            int remainingHours = GetRemainingCooldownHours(prizeTable, contextKey);
            if(remainingHours > 0) {
                failureMessage = $"{prizeTable.DisplayName} can be awarded again in {remainingHours} hour(s).";
                return false;
            }

            failureMessage = null;
            return true;
        }

        if(HasAwarded(prizeTable, contextKey)) {
            failureMessage = $"{prizeTable.DisplayName} was already awarded.";
            return false;
        }

        failureMessage = null;
        return true;
    }

    public PlayerCompetitionPrizeAwardRecord RecordAward(CompetitionPrizeTableDefinition prizeTable, CompetitionPrizeContext context, string sourceId = null) {
        if(prizeTable == null) {
            return null;
        }

        var record = new PlayerCompetitionPrizeAwardRecord {
            prizeTableId = prizeTable.Id,
            prizeTableName = prizeTable.DisplayName,
            trigger = context != null ? context.Trigger : CompetitionPrizeTrigger.BracketCompleted,
            contextKey = context != null ? context.BuildContextKey(prizeTable.RepeatMode) : prizeTable.Id,
            rosterId = context?.Roster != null ? context.Roster.Id : string.Empty,
            rosterName = context?.Roster != null ? context.Roster.DisplayName : string.Empty,
            competitionId = context?.Competition != null ? context.Competition.Id : string.Empty,
            competitionName = context?.Competition != null ? context.Competition.DisplayName : string.Empty,
            seasonId = context?.Season != null ? context.Season.Id : string.Empty,
            seasonName = context?.Season != null ? context.Season.DisplayName : string.Empty,
            rankingId = context?.Ranking != null ? context.Ranking.Id : string.Empty,
            rankingName = context?.Ranking != null ? context.Ranking.DisplayName : string.Empty,
            matchId = context?.Match != null ? context.Match.matchId : string.Empty,
            bracketSeed = context?.BracketState != null ? context.BracketState.seed : 0,
            won = context?.Won ?? false,
            awardedTotalHour = GetCurrentTotalHour(),
            sourceId = sourceId
        };

        awardHistory.Add(record);
        OnPrizeAwarded?.Invoke(prizeTable, record);
        OnCompetitionPrizeLogChanged?.Invoke();
        return record;
    }

    public bool HasAwarded(CompetitionPrizeTableDefinition prizeTable) {
        return GetAwardCount(prizeTable) > 0;
    }

    public bool HasAwarded(CompetitionPrizeTableDefinition prizeTable, string contextKey) {
        return GetAwardCount(prizeTable, contextKey) > 0;
    }

    public int GetAwardCount(CompetitionPrizeTableDefinition prizeTable) {
        string prizeTableId = prizeTable != null ? prizeTable.Id : string.Empty;
        return string.IsNullOrWhiteSpace(prizeTableId)
            ? 0
            : awardHistory.Count(record => record != null && record.prizeTableId == prizeTableId);
    }

    public int GetAwardCount(CompetitionPrizeTableDefinition prizeTable, string contextKey) {
        string prizeTableId = prizeTable != null ? prizeTable.Id : string.Empty;
        if(string.IsNullOrWhiteSpace(prizeTableId) || string.IsNullOrWhiteSpace(contextKey)) {
            return 0;
        }

        return awardHistory.Count(record => record != null && record.prizeTableId == prizeTableId && record.contextKey == contextKey);
    }

    public int GetRemainingCooldownHours(CompetitionPrizeTableDefinition prizeTable, string contextKey) {
        if(prizeTable == null || prizeTable.CooldownHours <= 0) {
            return 0;
        }

        string prizeTableId = prizeTable.Id;
        var lastAward = awardHistory
            .Where(record => record != null
                && record.prizeTableId == prizeTableId
                && (string.IsNullOrWhiteSpace(contextKey) || record.contextKey == contextKey))
            .OrderByDescending(record => record.awardedTotalHour)
            .FirstOrDefault();

        if(lastAward == null || lastAward.awardedTotalHour < 0) {
            return 0;
        }

        int readyAt = lastAward.awardedTotalHour + prizeTable.CooldownHours;
        return Mathf.Max(0, readyAt - GetCurrentTotalHour());
    }

    int GetCurrentTotalHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    public object CaptureState() {
        return new PlayerCompetitionPrizeLogSaveData {
            awardHistory = awardHistory.Where(record => record != null).Select(record => record.Clone()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerCompetitionPrizeLogSaveData;
        awardHistory = saveData?.awardHistory?.Where(record => record != null).Select(record => record.Clone()).ToList() ?? new List<PlayerCompetitionPrizeAwardRecord>();
        OnCompetitionPrizeLogChanged?.Invoke();
    }
}

[Serializable]
public class PlayerCompetitionPrizeAwardRecord {
    [Tooltip("Saved prize table id.")]
    public string prizeTableId;
    [Tooltip("Saved prize table display name.")]
    public string prizeTableName;
    [Tooltip("Prize trigger that caused this award.")]
    public CompetitionPrizeTrigger trigger;
    [Tooltip("Repeat/cooldown context key used by this award.")]
    public string contextKey;
    [Tooltip("Saved roster id.")]
    public string rosterId;
    [Tooltip("Saved roster display name.")]
    public string rosterName;
    [Tooltip("Saved competition id.")]
    public string competitionId;
    [Tooltip("Saved competition display name.")]
    public string competitionName;
    [Tooltip("Saved season id.")]
    public string seasonId;
    [Tooltip("Saved season display name.")]
    public string seasonName;
    [Tooltip("Saved ranking id.")]
    public string rankingId;
    [Tooltip("Saved ranking display name.")]
    public string rankingName;
    [Tooltip("Saved bracket match id, if the prize came from a match.")]
    public string matchId;
    [Tooltip("Saved generated bracket seed.")]
    public int bracketSeed;
    [Tooltip("Whether this prize was awarded after a win.")]
    public bool won;
    [Tooltip("In-game total hour when this prize was awarded.")]
    public int awardedTotalHour = -1;
    [Tooltip("Short source id that awarded this prize.")]
    public string sourceId;

    public PlayerCompetitionPrizeAwardRecord Clone() {
        return new PlayerCompetitionPrizeAwardRecord {
            prizeTableId = prizeTableId,
            prizeTableName = prizeTableName,
            trigger = trigger,
            contextKey = contextKey,
            rosterId = rosterId,
            rosterName = rosterName,
            competitionId = competitionId,
            competitionName = competitionName,
            seasonId = seasonId,
            seasonName = seasonName,
            rankingId = rankingId,
            rankingName = rankingName,
            matchId = matchId,
            bracketSeed = bracketSeed,
            won = won,
            awardedTotalHour = awardedTotalHour,
            sourceId = sourceId
        };
    }
}

[Serializable]
public class PlayerCompetitionPrizeLogSaveData {
    [Tooltip("Saved competition prize award history.")]
    public List<PlayerCompetitionPrizeAwardRecord> awardHistory = new List<PlayerCompetitionPrizeAwardRecord>();
}
