using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerLawLog : MonoBehaviour, ISavable {
    [Header("Wanted Levels")]
    [Tooltip("Wanted score required for wanted level 1.")]
    [Min(1)]
    [SerializeField] int levelOneThreshold = 1;
    [Tooltip("Wanted score required for wanted level 2.")]
    [Min(1)]
    [SerializeField] int levelTwoThreshold = 3;
    [Tooltip("Wanted score required for wanted level 3.")]
    [Min(1)]
    [SerializeField] int levelThreeThreshold = 6;
    [Tooltip("Wanted score required for wanted level 4.")]
    [Min(1)]
    [SerializeField] int levelFourThreshold = 10;

    [Header("State")]
    [Tooltip("Runtime/save history of reported law violations.")]
    [SerializeField] List<PlayerLawIncident> incidents = new List<PlayerLawIncident>();
    [Tooltip("Runtime/save wanted and fine state per authority.")]
    [SerializeField] List<PlayerWantedState> wantedStates = new List<PlayerWantedState>();

    public IReadOnlyList<PlayerLawIncident> Incidents => incidents;
    public IReadOnlyList<PlayerWantedState> WantedStates => wantedStates;
    public event Action<LawViolationDefinition, PlayerLawIncident> OnViolationRecorded;
    public event Action<PlayerWantedState> OnWantedStateChanged;
    public event Action OnLawLogChanged;

    public PlayerLawIncident RecordViolation(LawViolationDefinition violation, string sourceId = null, string reporterId = null, bool applyConsequences = true, UnityEngine.Object context = null) {
        if(violation == null) {
            return null;
        }

        var incident = new PlayerLawIncident {
            incidentId = Guid.NewGuid().ToString("N"),
            violationId = violation.Id,
            violationName = violation.DisplayName,
            category = violation.Category,
            severity = violation.Severity,
            authorityId = violation.AuthorityId,
            authorityName = violation.AuthorityName,
            sourceId = sourceId,
            reporterId = reporterId,
            wantedPoints = violation.WantedPoints,
            fineAmount = violation.FineAmount,
            day = GetCurrentDay(),
            absoluteHour = GetCurrentAbsoluteHour()
        };
        incidents.Add(incident);

        var wantedState = GetOrCreateWantedState(violation.AuthorityId, violation.AuthorityName);
        wantedState.wantedScore += violation.WantedPoints;
        wantedState.fineOwed += violation.FineAmount;
        wantedState.incidentCount++;
        wantedState.lastIncidentId = incident.incidentId;
        wantedState.lastViolationId = violation.Id;
        wantedState.lastViolationName = violation.DisplayName;
        wantedState.lastUpdatedDay = incident.day;
        wantedState.lastUpdatedAbsoluteHour = incident.absoluteHour;

        if(applyConsequences) {
            violation.ApplyConsequences(GetComponent<PlayerController>());
        }

        OnViolationRecorded?.Invoke(violation, incident);
        OnWantedStateChanged?.Invoke(wantedState);
        OnLawLogChanged?.Invoke();
        violation.PublishReported(GetComponent<PlayerController>(), sourceId, reporterId, context != null ? context : this);
        return incident;
    }

    public bool PayFine(ReputationFactionDefinition authority, float amount, out string failureMessage, UnityEngine.Object context = null) {
        return PayFine(authority != null ? authority.Id : null, amount, out failureMessage, context);
    }

    public bool PayFine(string authorityId, float amount, out string failureMessage, UnityEngine.Object context = null) {
        var state = GetWantedState(authorityId);
        if(state == null || state.fineOwed <= 0f) {
            failureMessage = "No fine is owed.";
            return false;
        }

        float payment = amount <= 0f ? state.fineOwed : Mathf.Min(amount, state.fineOwed);
        if(Wallet.i == null || !Wallet.i.HasMoney(payment)) {
            failureMessage = $"You need {payment:0} money to pay this fine.";
            return false;
        }

        Wallet.i.TakeMoney(payment);
        state.fineOwed = Mathf.Max(0f, state.fineOwed - payment);
        state.lastUpdatedDay = GetCurrentDay();
        state.lastUpdatedAbsoluteHour = GetCurrentAbsoluteHour();
        OnWantedStateChanged?.Invoke(state);
        OnLawLogChanged?.Invoke();
        PublishFinePaid(state, payment, context);
        failureMessage = null;
        return true;
    }

    public bool Pardon(ReputationFactionDefinition authority, bool clearWantedScore = true, bool clearFine = true, UnityEngine.Object context = null) {
        return Pardon(authority != null ? authority.Id : null, clearWantedScore, clearFine, context);
    }

    public bool Pardon(string authorityId, bool clearWantedScore = true, bool clearFine = true, UnityEngine.Object context = null) {
        var state = GetWantedState(authorityId);
        if(state == null) {
            return false;
        }

        if(clearWantedScore) {
            state.wantedScore = 0;
        }

        if(clearFine) {
            state.fineOwed = 0f;
        }

        state.pardonCount++;
        state.lastUpdatedDay = GetCurrentDay();
        state.lastUpdatedAbsoluteHour = GetCurrentAbsoluteHour();
        OnWantedStateChanged?.Invoke(state);
        OnLawLogChanged?.Invoke();
        PublishPardoned(state, context);
        return true;
    }

    public int GetWantedScore(ReputationFactionDefinition authority = null) {
        return GetWantedScore(authority != null ? authority.Id : null);
    }

    public int GetWantedScore(string authorityId = null) {
        if(string.IsNullOrWhiteSpace(authorityId)) {
            return wantedStates.Sum(state => state != null ? Mathf.Max(0, state.wantedScore) : 0);
        }

        return Mathf.Max(0, GetWantedState(authorityId)?.wantedScore ?? 0);
    }

    public int GetWantedLevel(ReputationFactionDefinition authority = null) {
        return GetWantedLevel(authority != null ? authority.Id : null);
    }

    public int GetWantedLevel(string authorityId = null) {
        int score = GetWantedScore(authorityId);
        if(score >= Mathf.Max(levelFourThreshold, levelThreeThreshold, levelTwoThreshold, levelOneThreshold)) return 4;
        if(score >= Mathf.Max(levelThreeThreshold, levelTwoThreshold, levelOneThreshold)) return 3;
        if(score >= Mathf.Max(levelTwoThreshold, levelOneThreshold)) return 2;
        return score >= Mathf.Max(1, levelOneThreshold) ? 1 : 0;
    }

    public float GetFineOwed(ReputationFactionDefinition authority = null) {
        return GetFineOwed(authority != null ? authority.Id : null);
    }

    public float GetFineOwed(string authorityId = null) {
        if(string.IsNullOrWhiteSpace(authorityId)) {
            return wantedStates.Sum(state => state != null ? Mathf.Max(0f, state.fineOwed) : 0f);
        }

        return Mathf.Max(0f, GetWantedState(authorityId)?.fineOwed ?? 0f);
    }

    public int GetViolationCount(LawViolationDefinition violation = null, string authorityId = null, string sourceId = null) {
        return incidents.Count(incident => incident != null
            && (violation == null || incident.violationId == violation.Id)
            && (string.IsNullOrWhiteSpace(authorityId) || incident.authorityId == authorityId)
            && (string.IsNullOrWhiteSpace(sourceId) || incident.sourceId == sourceId));
    }

    public int GetViolationCountWithTag(string tag, string authorityId = null) {
        if(string.IsNullOrWhiteSpace(tag)) {
            return 0;
        }

        int count = 0;
        foreach(var incident in incidents) {
            if(incident == null || (!string.IsNullOrWhiteSpace(authorityId) && incident.authorityId != authorityId)) {
                continue;
            }

            var violation = ResolveViolation(incident.violationId);
            if(violation != null && violation.HasTag(tag)) {
                count++;
            }
        }

        return count;
    }

    public int GetViolationCountByCategory(LawViolationCategory category, string authorityId = null) {
        return incidents.Count(incident => incident != null
            && incident.category == category
            && (string.IsNullOrWhiteSpace(authorityId) || incident.authorityId == authorityId));
    }

    PlayerWantedState GetOrCreateWantedState(string authorityId, string authorityName) {
        authorityId = string.IsNullOrWhiteSpace(authorityId) ? "global" : authorityId;
        var state = wantedStates.FirstOrDefault(entry => entry != null && entry.authorityId == authorityId);
        if(state != null) {
            return state;
        }

        state = new PlayerWantedState {
            authorityId = authorityId,
            authorityName = string.IsNullOrWhiteSpace(authorityName) ? authorityId : authorityName
        };
        wantedStates.Add(state);
        return state;
    }

    PlayerWantedState GetWantedState(string authorityId) {
        if(string.IsNullOrWhiteSpace(authorityId)) {
            return wantedStates
                .Where(state => state != null)
                .OrderByDescending(state => state.wantedScore)
                .ThenByDescending(state => state.fineOwed)
                .FirstOrDefault();
        }

        return wantedStates.FirstOrDefault(state => state != null && state.authorityId == authorityId);
    }

    LawViolationDefinition ResolveViolation(string violationId) {
        if(string.IsNullOrWhiteSpace(violationId)) {
            return null;
        }

        return Resources.LoadAll<LawViolationDefinition>("").FirstOrDefault(violation => violation != null && violation.Id == violationId);
    }

    void PublishFinePaid(PlayerWantedState state, float amount, UnityEngine.Object context) {
        GameEventPublishing.PublishOptional(
            null,
            $"law.fine-paid.{state.authorityId}",
            $"Paid {amount:0} fine to {state.authorityName}.",
            GameEventCategory.Law,
            GameEventImportance.Success,
            context != null ? context : this,
            "PlayerLawLog",
            GameEventScope.Player,
            showInFeed: true,
            writeToDebugLog: false,
            GameEventPublishing.Value("authorityId", state.authorityId),
            GameEventPublishing.Value("authorityName", state.authorityName),
            GameEventPublishing.Value("amount", amount),
            GameEventPublishing.Value("remainingFine", state.fineOwed));
    }

    void PublishPardoned(PlayerWantedState state, UnityEngine.Object context) {
        GameEventPublishing.PublishOptional(
            null,
            $"law.pardoned.{state.authorityId}",
            $"{state.authorityName} record cleared.",
            GameEventCategory.Law,
            GameEventImportance.Success,
            context != null ? context : this,
            "PlayerLawLog",
            GameEventScope.Player,
            showInFeed: true,
            writeToDebugLog: false,
            GameEventPublishing.Value("authorityId", state.authorityId),
            GameEventPublishing.Value("authorityName", state.authorityName),
            GameEventPublishing.Value("wantedScore", state.wantedScore),
            GameEventPublishing.Value("fineOwed", state.fineOwed));
    }

    int GetCurrentDay() {
        return TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    public object CaptureState() {
        return new PlayerLawLogSaveData {
            incidents = incidents.Where(incident => incident != null).Select(incident => incident.ToSaveData()).ToList(),
            wantedStates = wantedStates.Where(state => state != null).Select(state => state.ToSaveData()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerLawLogSaveData;
        incidents = saveData?.incidents?.Where(incident => incident != null).Select(incident => new PlayerLawIncident(incident)).ToList() ?? new List<PlayerLawIncident>();
        wantedStates = saveData?.wantedStates?.Where(entry => entry != null).Select(entry => new PlayerWantedState(entry)).ToList() ?? new List<PlayerWantedState>();
        OnLawLogChanged?.Invoke();
    }
}

[Serializable]
public class PlayerLawIncident {
    [Tooltip("Unique runtime/save id for this incident.")]
    public string incidentId;
    [Tooltip("Saved violation id.")]
    public string violationId;
    [Tooltip("Saved violation display name for fallback/debug output.")]
    public string violationName;
    [Tooltip("Saved violation category.")]
    public LawViolationCategory category;
    [Tooltip("Saved violation severity.")]
    public LawViolationSeverity severity;
    [Tooltip("Authority id that owns this incident.")]
    public string authorityId;
    [Tooltip("Authority display name for fallback/debug output.")]
    public string authorityName;
    [Tooltip("Optional source id that reported this incident.")]
    public string sourceId;
    [Tooltip("Optional reporter id, such as NPC, zone or system id.")]
    public string reporterId;
    [Tooltip("Wanted points added by this incident.")]
    [Min(0)]
    public int wantedPoints;
    [Tooltip("Fine added by this incident.")]
    [Min(0f)]
    public float fineAmount;
    [Tooltip("In-game day when this incident was recorded.")]
    public int day;
    [Tooltip("Absolute in-game hour when this incident was recorded.")]
    public int absoluteHour;

    public PlayerLawIncident() {
    }

    public PlayerLawIncident(PlayerLawIncidentSaveData saveData) {
        if(saveData == null) {
            return;
        }

        incidentId = saveData.incidentId;
        violationId = saveData.violationId;
        violationName = saveData.violationName;
        category = saveData.category;
        severity = saveData.severity;
        authorityId = saveData.authorityId;
        authorityName = saveData.authorityName;
        sourceId = saveData.sourceId;
        reporterId = saveData.reporterId;
        wantedPoints = Mathf.Max(0, saveData.wantedPoints);
        fineAmount = Mathf.Max(0f, saveData.fineAmount);
        day = saveData.day;
        absoluteHour = saveData.absoluteHour;
    }

    public PlayerLawIncidentSaveData ToSaveData() {
        return new PlayerLawIncidentSaveData {
            incidentId = incidentId,
            violationId = violationId,
            violationName = violationName,
            category = category,
            severity = severity,
            authorityId = authorityId,
            authorityName = authorityName,
            sourceId = sourceId,
            reporterId = reporterId,
            wantedPoints = wantedPoints,
            fineAmount = fineAmount,
            day = day,
            absoluteHour = absoluteHour
        };
    }
}

[Serializable]
public class PlayerWantedState {
    [Tooltip("Authority id that owns this wanted state.")]
    public string authorityId;
    [Tooltip("Authority display name for fallback/debug output.")]
    public string authorityName;
    [Tooltip("Current wanted score.")]
    [Min(0)]
    public int wantedScore;
    [Tooltip("Current unpaid fine.")]
    [Min(0f)]
    public float fineOwed;
    [Tooltip("Total incidents recorded for this authority.")]
    [Min(0)]
    public int incidentCount;
    [Tooltip("Total pardons or manual clears for this authority.")]
    [Min(0)]
    public int pardonCount;
    [Tooltip("Last incident id for this authority.")]
    public string lastIncidentId;
    [Tooltip("Last violation id for this authority.")]
    public string lastViolationId;
    [Tooltip("Last violation display name for this authority.")]
    public string lastViolationName;
    [Tooltip("In-game day when this authority state last changed.")]
    public int lastUpdatedDay = -1;
    [Tooltip("Absolute in-game hour when this authority state last changed.")]
    public int lastUpdatedAbsoluteHour = -1;

    public PlayerWantedState() {
    }

    public PlayerWantedState(PlayerWantedStateSaveData saveData) {
        if(saveData == null) {
            return;
        }

        authorityId = saveData.authorityId;
        authorityName = saveData.authorityName;
        wantedScore = Mathf.Max(0, saveData.wantedScore);
        fineOwed = Mathf.Max(0f, saveData.fineOwed);
        incidentCount = Mathf.Max(0, saveData.incidentCount);
        pardonCount = Mathf.Max(0, saveData.pardonCount);
        lastIncidentId = saveData.lastIncidentId;
        lastViolationId = saveData.lastViolationId;
        lastViolationName = saveData.lastViolationName;
        lastUpdatedDay = saveData.lastUpdatedDay;
        lastUpdatedAbsoluteHour = saveData.lastUpdatedAbsoluteHour;
    }

    public PlayerWantedStateSaveData ToSaveData() {
        return new PlayerWantedStateSaveData {
            authorityId = authorityId,
            authorityName = authorityName,
            wantedScore = wantedScore,
            fineOwed = fineOwed,
            incidentCount = incidentCount,
            pardonCount = pardonCount,
            lastIncidentId = lastIncidentId,
            lastViolationId = lastViolationId,
            lastViolationName = lastViolationName,
            lastUpdatedDay = lastUpdatedDay,
            lastUpdatedAbsoluteHour = lastUpdatedAbsoluteHour
        };
    }
}

[Serializable]
public class PlayerLawLogSaveData {
    public List<PlayerLawIncidentSaveData> incidents;
    public List<PlayerWantedStateSaveData> wantedStates;
}

[Serializable]
public class PlayerLawIncidentSaveData {
    public string incidentId;
    public string violationId;
    public string violationName;
    public LawViolationCategory category;
    public LawViolationSeverity severity;
    public string authorityId;
    public string authorityName;
    public string sourceId;
    public string reporterId;
    public int wantedPoints;
    public float fineAmount;
    public int day;
    public int absoluteHour;
}

[Serializable]
public class PlayerWantedStateSaveData {
    public string authorityId;
    public string authorityName;
    public int wantedScore;
    public float fineOwed;
    public int incidentCount;
    public int pardonCount;
    public string lastIncidentId;
    public string lastViolationId;
    public string lastViolationName;
    public int lastUpdatedDay;
    public int lastUpdatedAbsoluteHour;
}
