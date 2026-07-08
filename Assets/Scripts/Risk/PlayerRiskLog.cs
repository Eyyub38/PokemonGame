using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerRiskLog : MonoBehaviour, ISavable {
    [Header("Risk Level Thresholds")]
    [Tooltip("Risk score required for level 1.")]
    [Min(1)]
    [SerializeField] int levelOneThreshold = 3;
    [Tooltip("Risk score required for level 2.")]
    [Min(1)]
    [SerializeField] int levelTwoThreshold = 6;
    [Tooltip("Risk score required for level 3.")]
    [Min(1)]
    [SerializeField] int levelThreeThreshold = 10;
    [Tooltip("Risk score required for level 4.")]
    [Min(1)]
    [SerializeField] int levelFourThreshold = 15;

    [Header("State")]
    [Tooltip("Runtime/save history of risk incidents. Active risk is calculated from records that have not expired or been cleared.")]
    [SerializeField] List<PlayerRiskIncidentRecord> incidents = new List<PlayerRiskIncidentRecord>();

    public IReadOnlyList<PlayerRiskIncidentRecord> Incidents => incidents;
    public event Action<RiskIncidentDefinition, PlayerRiskIncidentRecord> OnRiskIncidentRecorded;
    public event Action<PlayerRiskIncidentRecord> OnRiskIncidentExpired;
    public event Action<PlayerRiskIncidentRecord> OnRiskIncidentCleared;
    public event Action OnRiskChanged;

    void OnEnable() {
        SubscribeToTime();
    }

    void OnDisable() {
        UnsubscribeFromTime();
    }

    public PlayerRiskIncidentRecord RecordIncident(
        RiskIncidentDefinition incident,
        string sourceId = null,
        string reporterId = null,
        RegionInfoDefinition regionOverride = null,
        string authorityIdOverride = null,
        string authorityNameOverride = null,
        bool applyConsequences = true,
        UnityEngine.Object context = null
    ) {
        if(incident == null) {
            return null;
        }

        PruneExpired();
        var region = regionOverride != null ? regionOverride : incident.DefaultRegion;
        string authorityId = !string.IsNullOrWhiteSpace(authorityIdOverride) ? authorityIdOverride : incident.AuthorityId;
        string authorityName = !string.IsNullOrWhiteSpace(authorityNameOverride) ? authorityNameOverride : incident.AuthorityName;
        int currentHour = GetCurrentAbsoluteHour();
        int activeDurationHours = incident.ActiveDurationHours;

        var record = new PlayerRiskIncidentRecord {
            recordId = Guid.NewGuid().ToString("N"),
            incidentId = incident.Id,
            incidentName = incident.DisplayName,
            category = incident.Category,
            severity = incident.Severity,
            authorityId = string.IsNullOrWhiteSpace(authorityId) ? "global" : authorityId,
            authorityName = string.IsNullOrWhiteSpace(authorityName) ? "global" : authorityName,
            regionId = region != null ? region.Id : string.Empty,
            regionName = region != null ? region.DisplayName : string.Empty,
            sourceId = sourceId ?? string.Empty,
            reporterId = reporterId ?? string.Empty,
            contributesToGlobalRisk = incident.ContributesToGlobalRisk,
            heatPoints = incident.HeatPoints,
            suspicionPoints = incident.SuspicionPoints,
            evidencePoints = incident.EvidencePoints,
            day = GetCurrentDay(),
            absoluteHour = currentHour,
            expiresAbsoluteHour = activeDurationHours > 0 ? currentHour + activeDurationHours : -1
        };

        incidents.Add(record);
        if(applyConsequences) {
            incident.ApplyConsequences(GetComponent<PlayerController>(), sourceId, reporterId, context != null ? context : this);
        }

        OnRiskIncidentRecorded?.Invoke(incident, record);
        OnRiskChanged?.Invoke();
        PublishRiskEvent(incident, record, "recorded", GameEventImportance.Warning, context);
        return record;
    }

    public int GetHeat(string authorityId = null, string regionId = null) {
        return GetActiveIncidents(authorityId, regionId).Sum(record => Mathf.Max(0, record.heatPoints));
    }

    public int GetSuspicion(string authorityId = null, string regionId = null) {
        return GetActiveIncidents(authorityId, regionId).Sum(record => Mathf.Max(0, record.suspicionPoints));
    }

    public int GetEvidence(string authorityId = null, string regionId = null) {
        return GetActiveIncidents(authorityId, regionId).Sum(record => Mathf.Max(0, record.evidencePoints));
    }

    public int GetHeatLevel(string authorityId = null, string regionId = null) {
        return GetLevel(GetHeat(authorityId, regionId));
    }

    public int GetSuspicionLevel(string authorityId = null, string regionId = null) {
        return GetLevel(GetSuspicion(authorityId, regionId));
    }

    public int GetEvidenceLevel(string authorityId = null, string regionId = null) {
        return GetLevel(GetEvidence(authorityId, regionId));
    }

    public int GetIncidentCount(RiskIncidentDefinition incident = null, string authorityId = null, string regionId = null, string sourceId = null, bool activeOnly = false) {
        return GetMatchingIncidents(incident, authorityId, regionId, sourceId, activeOnly).Count();
    }

    public int GetIncidentCountWithTag(string tag, string authorityId = null, string regionId = null, bool activeOnly = false) {
        if(string.IsNullOrWhiteSpace(tag)) {
            return 0;
        }

        int count = 0;
        foreach(var record in GetMatchingIncidents(null, authorityId, regionId, null, activeOnly)) {
            var incident = record.ResolveDefinition();
            if(incident != null && incident.HasTag(tag)) {
                count++;
            }
        }

        return count;
    }

    public int GetIncidentCountByCategory(RiskIncidentCategory category, string authorityId = null, string regionId = null, bool activeOnly = false) {
        return GetMatchingIncidents(null, authorityId, regionId, null, activeOnly).Count(record => record.category == category);
    }

    public List<PlayerRiskIncidentRecord> GetActiveIncidents(string authorityId = null, string regionId = null, string sourceId = null) {
        PruneExpired();
        int currentHour = GetCurrentAbsoluteHour();
        return incidents
            .Where(record => record != null && record.IsActive(currentHour) && MatchesFilters(record, authorityId, regionId, sourceId))
            .OrderByDescending(record => record.absoluteHour)
            .ToList();
    }

    public int ClearRisk(string authorityId = null, string regionId = null, string sourceId = null, UnityEngine.Object context = null) {
        int currentHour = GetCurrentAbsoluteHour();
        var records = incidents
            .Where(record => record != null && record.IsActive(currentHour) && MatchesFilters(record, authorityId, regionId, sourceId))
            .ToList();

        foreach(var record in records) {
            record.cleared = true;
            record.clearedAbsoluteHour = currentHour;
            OnRiskIncidentCleared?.Invoke(record);
            var incident = record.ResolveDefinition();
            if(incident != null) {
                PublishRiskEvent(incident, record, "cleared", GameEventImportance.Success, context);
            }
        }

        if(records.Count > 0) {
            OnRiskChanged?.Invoke();
        }

        return records.Count;
    }

    public int PruneExpired() {
        int currentHour = GetCurrentAbsoluteHour();
        int expiredCount = 0;
        foreach(var record in incidents) {
            if(record == null || record.expiryNotified || record.cleared || !record.IsExpired(currentHour)) {
                continue;
            }

            record.expiryNotified = true;
            expiredCount++;
            OnRiskIncidentExpired?.Invoke(record);
            var incident = record.ResolveDefinition();
            if(incident != null) {
                PublishRiskEvent(incident, record, "expired", GameEventImportance.Trace, this);
            }
        }

        if(expiredCount > 0) {
            OnRiskChanged?.Invoke();
        }

        return expiredCount;
    }

    IEnumerable<PlayerRiskIncidentRecord> GetMatchingIncidents(RiskIncidentDefinition incident, string authorityId, string regionId, string sourceId, bool activeOnly) {
        PruneExpired();
        int currentHour = GetCurrentAbsoluteHour();
        return incidents.Where(record => record != null
            && (!activeOnly || record.IsActive(currentHour))
            && (incident == null || record.incidentId == incident.Id)
            && MatchesFilters(record, authorityId, regionId, sourceId));
    }

    bool MatchesFilters(PlayerRiskIncidentRecord record, string authorityId, string regionId, string sourceId) {
        if(record == null) {
            return false;
        }

        if(!string.IsNullOrWhiteSpace(authorityId) && record.authorityId != authorityId) {
            return false;
        }

        if(!string.IsNullOrWhiteSpace(sourceId) && record.sourceId != sourceId) {
            return false;
        }

        if(!string.IsNullOrWhiteSpace(regionId)) {
            return record.regionId == regionId || (string.IsNullOrWhiteSpace(record.regionId) && record.contributesToGlobalRisk);
        }

        return true;
    }

    int GetLevel(int score) {
        if(score >= Mathf.Max(levelFourThreshold, levelThreeThreshold, levelTwoThreshold, levelOneThreshold)) return 4;
        if(score >= Mathf.Max(levelThreeThreshold, levelTwoThreshold, levelOneThreshold)) return 3;
        if(score >= Mathf.Max(levelTwoThreshold, levelOneThreshold)) return 2;
        return score >= Mathf.Max(1, levelOneThreshold) ? 1 : 0;
    }

    void SubscribeToTime() {
        if(TimeSystem.i == null) {
            return;
        }

        TimeSystem.i.OnTimeChanged -= HandleTimeChanged;
        TimeSystem.i.OnDayChanged -= HandleTimeChanged;
        TimeSystem.i.OnTimeChanged += HandleTimeChanged;
        TimeSystem.i.OnDayChanged += HandleTimeChanged;
    }

    void UnsubscribeFromTime() {
        if(TimeSystem.i == null) {
            return;
        }

        TimeSystem.i.OnTimeChanged -= HandleTimeChanged;
        TimeSystem.i.OnDayChanged -= HandleTimeChanged;
    }

    void HandleTimeChanged() {
        PruneExpired();
    }

    int GetCurrentDay() {
        return TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    void PublishRiskEvent(RiskIncidentDefinition incident, PlayerRiskIncidentRecord record, string phase, GameEventImportance importance, UnityEngine.Object context) {
        GameEventPublishing.PublishOptional(
            phase == "recorded" ? incident.RecordedEvent : incident.ClearedEvent,
            $"risk.{phase}.{incident.Id}",
            phase == "recorded" ? $"{incident.DisplayName} risk recorded." : $"{incident.DisplayName} risk {phase}.",
            GameEventCategory.Risk,
            importance,
            context != null ? context : this,
            "PlayerRiskLog",
            GameEventScope.Player,
            showInFeed: incident.ShowEventsInFeed,
            writeToDebugLog: incident.WriteEventsToDebugLog,
            GameEventPublishing.Value("riskIncidentId", incident.Id),
            GameEventPublishing.Value("riskIncidentName", incident.DisplayName),
            GameEventPublishing.Value("category", incident.Category),
            GameEventPublishing.Value("severity", incident.Severity),
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("recordId", record.recordId),
            GameEventPublishing.Value("authorityId", record.authorityId),
            GameEventPublishing.Value("regionId", record.regionId),
            GameEventPublishing.Value("sourceId", record.sourceId),
            GameEventPublishing.Value("reporterId", record.reporterId),
            GameEventPublishing.Value("heat", record.heatPoints),
            GameEventPublishing.Value("suspicion", record.suspicionPoints),
            GameEventPublishing.Value("evidence", record.evidencePoints));
    }

    public object CaptureState() {
        return new PlayerRiskLogSaveData {
            incidents = incidents.Where(record => record != null).Select(record => record.ToSaveData()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerRiskLogSaveData;
        incidents = saveData?.incidents?.Where(record => record != null).Select(record => new PlayerRiskIncidentRecord(record)).ToList()
            ?? new List<PlayerRiskIncidentRecord>();
        OnRiskChanged?.Invoke();
    }
}

[Serializable]
public class PlayerRiskIncidentRecord {
    [Tooltip("Unique runtime/save id for this risk record.")]
    public string recordId;
    [Tooltip("Saved risk incident definition id.")]
    public string incidentId;
    [Tooltip("Saved incident display name for fallback/debug output.")]
    public string incidentName;
    [Tooltip("Saved incident category.")]
    public RiskIncidentCategory category;
    [Tooltip("Saved incident severity.")]
    public RiskIncidentSeverity severity;
    [Tooltip("Authority id affected by this incident.")]
    public string authorityId;
    [Tooltip("Authority display name saved for fallback/debug output.")]
    public string authorityName;
    [Tooltip("Region id affected by this incident. Empty means no specific region.")]
    public string regionId;
    [Tooltip("Region display name saved for fallback/debug output.")]
    public string regionName;
    [Tooltip("Source id that created this incident.")]
    public string sourceId;
    [Tooltip("Reporter id, such as NPC, camera, shop or system id.")]
    public string reporterId;
    [Tooltip("If enabled, this incident also counts when querying all-region risk.")]
    public bool contributesToGlobalRisk = true;
    [Tooltip("Active heat points contributed by this incident.")]
    [Min(0)]
    public int heatPoints;
    [Tooltip("Active suspicion points contributed by this incident.")]
    [Min(0)]
    public int suspicionPoints;
    [Tooltip("Active evidence points contributed by this incident.")]
    [Min(0)]
    public int evidencePoints;
    [Tooltip("In-game day when this incident was recorded.")]
    public int day;
    [Tooltip("Absolute in-game hour when this incident was recorded.")]
    public int absoluteHour;
    [Tooltip("Absolute in-game hour when this incident stops contributing. -1 means no automatic expiry.")]
    public int expiresAbsoluteHour = -1;
    [Tooltip("If enabled, this incident was cleared before expiry.")]
    public bool cleared;
    [Tooltip("Absolute in-game hour when this incident was cleared.")]
    public int clearedAbsoluteHour = -1;
    [Tooltip("If enabled, expiry events have already been emitted for this record.")]
    public bool expiryNotified;

    public PlayerRiskIncidentRecord() {
    }

    public PlayerRiskIncidentRecord(PlayerRiskIncidentRecordSaveData saveData) {
        if(saveData == null) {
            return;
        }

        recordId = saveData.recordId;
        incidentId = saveData.incidentId;
        incidentName = saveData.incidentName;
        category = saveData.category;
        severity = saveData.severity;
        authorityId = saveData.authorityId;
        authorityName = saveData.authorityName;
        regionId = saveData.regionId;
        regionName = saveData.regionName;
        sourceId = saveData.sourceId;
        reporterId = saveData.reporterId;
        contributesToGlobalRisk = saveData.contributesToGlobalRisk;
        heatPoints = Mathf.Max(0, saveData.heatPoints);
        suspicionPoints = Mathf.Max(0, saveData.suspicionPoints);
        evidencePoints = Mathf.Max(0, saveData.evidencePoints);
        day = saveData.day;
        absoluteHour = saveData.absoluteHour;
        expiresAbsoluteHour = saveData.expiresAbsoluteHour;
        cleared = saveData.cleared;
        clearedAbsoluteHour = saveData.clearedAbsoluteHour;
        expiryNotified = saveData.expiryNotified;
    }

    public bool IsActive(int currentAbsoluteHour) {
        return !cleared && !IsExpired(currentAbsoluteHour);
    }

    public bool IsExpired(int currentAbsoluteHour) {
        return expiresAbsoluteHour >= 0 && currentAbsoluteHour >= expiresAbsoluteHour;
    }

    public RiskIncidentDefinition ResolveDefinition() {
        if(string.IsNullOrWhiteSpace(incidentId)) {
            return null;
        }

        return Resources.LoadAll<RiskIncidentDefinition>("").FirstOrDefault(incident => incident != null && incident.Id == incidentId);
    }

    public RegionInfoDefinition ResolveRegion() {
        if(string.IsNullOrWhiteSpace(regionId)) {
            return null;
        }

        return Resources.LoadAll<RegionInfoDefinition>("").FirstOrDefault(region => region != null && region.Id == regionId);
    }

    public PlayerRiskIncidentRecordSaveData ToSaveData() {
        return new PlayerRiskIncidentRecordSaveData {
            recordId = recordId,
            incidentId = incidentId,
            incidentName = incidentName,
            category = category,
            severity = severity,
            authorityId = authorityId,
            authorityName = authorityName,
            regionId = regionId,
            regionName = regionName,
            sourceId = sourceId,
            reporterId = reporterId,
            contributesToGlobalRisk = contributesToGlobalRisk,
            heatPoints = heatPoints,
            suspicionPoints = suspicionPoints,
            evidencePoints = evidencePoints,
            day = day,
            absoluteHour = absoluteHour,
            expiresAbsoluteHour = expiresAbsoluteHour,
            cleared = cleared,
            clearedAbsoluteHour = clearedAbsoluteHour,
            expiryNotified = expiryNotified
        };
    }
}

[Serializable]
public class PlayerRiskLogSaveData {
    public List<PlayerRiskIncidentRecordSaveData> incidents;
}

[Serializable]
public class PlayerRiskIncidentRecordSaveData {
    public string recordId;
    public string incidentId;
    public string incidentName;
    public RiskIncidentCategory category;
    public RiskIncidentSeverity severity;
    public string authorityId;
    public string authorityName;
    public string regionId;
    public string regionName;
    public string sourceId;
    public string reporterId;
    public bool contributesToGlobalRisk;
    public int heatPoints;
    public int suspicionPoints;
    public int evidencePoints;
    public int day;
    public int absoluteHour;
    public int expiresAbsoluteHour;
    public bool cleared;
    public int clearedAbsoluteHour;
    public bool expiryNotified;
}
