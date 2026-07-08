using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerJourneyIncidentLog : MonoBehaviour, ISavable {
    [Tooltip("Maximum journey incident history records kept in save data. 0 keeps all records.")]
    [Min(0)]
    [SerializeField] int maxHistoryRecords = 240;
    [Tooltip("Runtime/save active journey incidents.")]
    [SerializeField] List<PlayerJourneyIncidentState> activeIncidents = new List<PlayerJourneyIncidentState>();
    [Tooltip("Runtime/save history of incident activation, resolution, expiration and blocked attempts.")]
    [SerializeField] List<PlayerJourneyIncidentRecord> records = new List<PlayerJourneyIncidentRecord>();

    public IReadOnlyList<PlayerJourneyIncidentState> ActiveIncidents => activeIncidents;
    public IReadOnlyList<PlayerJourneyIncidentRecord> Records => records;

    public event Action<PlayerJourneyIncidentState> OnJourneyIncidentActivated;
    public event Action<PlayerJourneyIncidentRecord> OnJourneyIncidentRecorded;
    public event Action OnJourneyIncidentsChanged;

    void OnEnable() {
        SubscribeTime();
    }

    void OnDisable() {
        UnsubscribeTime();
    }

    public bool CanActivate(
        JourneyIncidentDefinition definition,
        string sourceId,
        ConsequenceChainRepeatMode repeatMode,
        int cooldownHours,
        int maxActivationCount,
        bool allowMultipleActiveInstances,
        RegionInfoDefinition region,
        ActivityZoneDefinition zone,
        out string failureMessage) {
        if(definition == null) {
            failureMessage = "No journey incident selected.";
            return false;
        }

        PruneExpired();
        string normalizedSourceId = NormalizeSourceId(sourceId, definition);
        if(!allowMultipleActiveInstances && IsActive(definition, null, region, zone)) {
            failureMessage = $"{definition.DisplayName} is already active here.";
            return false;
        }

        int totalActivations = GetActivationCount(definition, includeBlocked: false);
        if(maxActivationCount > 0 && totalActivations >= maxActivationCount) {
            failureMessage = $"{definition.DisplayName} reached its maximum activation count.";
            return false;
        }

        if(repeatMode == ConsequenceChainRepeatMode.OnceEver && totalActivations > 0) {
            failureMessage = $"{definition.DisplayName} has already happened.";
            return false;
        }

        if(repeatMode == ConsequenceChainRepeatMode.OncePerSource && GetActivationCount(definition, normalizedSourceId, includeBlocked: false) > 0) {
            failureMessage = $"{definition.DisplayName} already happened from this source.";
            return false;
        }

        var lastSourceActivation = GetLastRecord(definition, normalizedSourceId, JourneyIncidentPhase.Activated, includeBlocked: false);
        if(repeatMode == ConsequenceChainRepeatMode.Daily && lastSourceActivation != null && lastSourceActivation.day == GetCurrentDay()) {
            failureMessage = $"{definition.DisplayName} can only activate once per day from this source.";
            return false;
        }

        if(repeatMode == ConsequenceChainRepeatMode.CooldownHours && lastSourceActivation != null) {
            int elapsed = GetCurrentAbsoluteHour() - lastSourceActivation.absoluteHour;
            int cooldown = Mathf.Max(0, cooldownHours);
            if(elapsed < cooldown) {
                failureMessage = $"{definition.DisplayName} can activate again in {cooldown - elapsed} hour(s).";
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    public PlayerJourneyIncidentState RecordActivated(
        JourneyIncidentDefinition definition,
        string sourceId,
        string sourceName,
        RegionInfoDefinition region,
        ActivityZoneDefinition zone,
        int durationHours,
        bool expireAutomatically) {
        if(definition == null) {
            return null;
        }

        string normalizedSourceId = NormalizeSourceId(sourceId, definition);
        var activeState = durationHours > 0
            ? CreateActiveState(definition, normalizedSourceId, sourceName, region, zone, durationHours, expireAutomatically)
            : null;

        if(activeState != null) {
            activeIncidents.Add(activeState);
            OnJourneyIncidentActivated?.Invoke(activeState);
        }

        Record(definition, JourneyIncidentPhase.Activated, normalizedSourceId, sourceName, region, zone, false, null);
        OnJourneyIncidentsChanged?.Invoke();
        return activeState;
    }

    public PlayerJourneyIncidentRecord RecordBlocked(
        JourneyIncidentDefinition definition,
        string sourceId,
        string sourceName,
        RegionInfoDefinition region,
        ActivityZoneDefinition zone,
        string failureMessage) {
        return Record(definition, JourneyIncidentPhase.Blocked, NormalizeSourceId(sourceId, definition), sourceName, region, zone, true, failureMessage);
    }

    public bool IsActive(JourneyIncidentDefinition definition, string sourceId = null, RegionInfoDefinition region = null, ActivityZoneDefinition zone = null) {
        if(definition == null) {
            return false;
        }

        PruneExpired();
        return activeIncidents.Any(state => MatchesState(state, definition, sourceId, region, zone));
    }

    public int Resolve(JourneyIncidentDefinition definition, string sourceId = null, RegionInfoDefinition region = null, ActivityZoneDefinition zone = null) {
        return RemoveActive(definition, sourceId, region, zone, JourneyIncidentPhase.Resolved);
    }

    public int Expire(JourneyIncidentDefinition definition, string sourceId = null, RegionInfoDefinition region = null, ActivityZoneDefinition zone = null) {
        return RemoveActive(definition, sourceId, region, zone, JourneyIncidentPhase.Expired);
    }

    public int PruneExpired() {
        int currentHour = GetCurrentAbsoluteHour();
        var expired = activeIncidents
            .Where(state => state != null && state.expireAutomatically && state.IsExpired(currentHour))
            .ToList();

        foreach(var state in expired) {
            activeIncidents.Remove(state);
            var definition = state.ResolveDefinition();
            if(definition != null) {
                Record(definition, JourneyIncidentPhase.Expired, state.sourceId, state.sourceName, state.ResolveRegion(), state.ResolveZone(), false, null);
                definition.ApplyExpiredEffects(GetComponent<PlayerController>(), state, this);
            }
        }

        if(expired.Count > 0) {
            OnJourneyIncidentsChanged?.Invoke();
        }

        return expired.Count;
    }

    public int GetActivationCount(JourneyIncidentDefinition definition, string sourceId = null, bool includeBlocked = false) {
        if(definition == null) {
            return 0;
        }

        string normalizedSourceId = string.IsNullOrWhiteSpace(sourceId) ? null : NormalizeSourceId(sourceId, definition);
        return records.Count(record => record != null
            && record.incidentId == definition.Id
            && record.phase == JourneyIncidentPhase.Activated
            && (includeBlocked || !record.blocked)
            && (string.IsNullOrWhiteSpace(normalizedSourceId) || record.sourceId == normalizedSourceId));
    }

    public PlayerJourneyIncidentRecord GetLastRecord(JourneyIncidentDefinition definition, string sourceId = null, JourneyIncidentPhase? phase = null, bool includeBlocked = true) {
        if(definition == null) {
            return null;
        }

        string normalizedSourceId = string.IsNullOrWhiteSpace(sourceId) ? null : NormalizeSourceId(sourceId, definition);
        return records
            .Where(record => record != null
                && record.incidentId == definition.Id
                && (!phase.HasValue || record.phase == phase.Value)
                && (includeBlocked || !record.blocked)
                && (string.IsNullOrWhiteSpace(normalizedSourceId) || record.sourceId == normalizedSourceId))
            .OrderByDescending(record => record.absoluteHour)
            .ThenByDescending(record => record.day)
            .FirstOrDefault();
    }

    int RemoveActive(JourneyIncidentDefinition definition, string sourceId, RegionInfoDefinition region, ActivityZoneDefinition zone, JourneyIncidentPhase phase) {
        if(definition == null) {
            return 0;
        }

        PruneExpired();
        var matches = activeIncidents
            .Where(state => MatchesState(state, definition, sourceId, region, zone))
            .ToList();

        foreach(var state in matches) {
            activeIncidents.Remove(state);
            Record(definition, phase, state.sourceId, state.sourceName, state.ResolveRegion(), state.ResolveZone(), false, null);
        }

        if(matches.Count > 0) {
            OnJourneyIncidentsChanged?.Invoke();
        }

        return matches.Count;
    }

    PlayerJourneyIncidentState CreateActiveState(
        JourneyIncidentDefinition definition,
        string sourceId,
        string sourceName,
        RegionInfoDefinition region,
        ActivityZoneDefinition zone,
        int durationHours,
        bool expireAutomatically) {
        int currentHour = GetCurrentAbsoluteHour();
        return new PlayerJourneyIncidentState {
            activeId = Guid.NewGuid().ToString("N"),
            incidentId = definition.Id,
            incidentName = definition.DisplayName,
            category = definition.Category,
            severity = definition.Severity,
            sourceId = sourceId,
            sourceName = sourceName ?? string.Empty,
            regionId = region != null ? region.Id : string.Empty,
            regionName = region != null ? region.DisplayName : string.Empty,
            zoneId = zone != null ? zone.Id : string.Empty,
            zoneName = zone != null ? zone.DisplayName : string.Empty,
            startedDay = GetCurrentDay(),
            startedAbsoluteHour = currentHour,
            expiresAbsoluteHour = currentHour + Mathf.Max(1, durationHours),
            expireAutomatically = expireAutomatically
        };
    }

    PlayerJourneyIncidentRecord Record(
        JourneyIncidentDefinition definition,
        JourneyIncidentPhase phase,
        string sourceId,
        string sourceName,
        RegionInfoDefinition region,
        ActivityZoneDefinition zone,
        bool blocked,
        string failureMessage) {
        if(definition == null) {
            return null;
        }

        var record = new PlayerJourneyIncidentRecord {
            recordId = Guid.NewGuid().ToString("N"),
            incidentId = definition.Id,
            incidentName = definition.DisplayName,
            category = definition.Category,
            severity = definition.Severity,
            phase = phase,
            sourceId = NormalizeSourceId(sourceId, definition),
            sourceName = sourceName ?? string.Empty,
            regionId = region != null ? region.Id : string.Empty,
            regionName = region != null ? region.DisplayName : string.Empty,
            zoneId = zone != null ? zone.Id : string.Empty,
            zoneName = zone != null ? zone.DisplayName : string.Empty,
            day = GetCurrentDay(),
            absoluteHour = GetCurrentAbsoluteHour(),
            blocked = blocked,
            failureMessage = failureMessage
        };

        records.Add(record);
        TrimHistory();
        OnJourneyIncidentRecorded?.Invoke(record);
        return record;
    }

    bool MatchesState(PlayerJourneyIncidentState state, JourneyIncidentDefinition definition, string sourceId, RegionInfoDefinition region, ActivityZoneDefinition zone) {
        if(state == null || definition == null || state.incidentId != definition.Id) {
            return false;
        }

        if(!string.IsNullOrWhiteSpace(sourceId) && state.sourceId != NormalizeSourceId(sourceId, definition)) {
            return false;
        }

        if(region != null && state.regionId != region.Id) {
            return false;
        }

        if(zone != null && state.zoneId != zone.Id) {
            return false;
        }

        return true;
    }

    void SubscribeTime() {
        if(TimeSystem.i == null) {
            return;
        }

        TimeSystem.i.OnTimeChanged -= HandleTimeChanged;
        TimeSystem.i.OnDayChanged -= HandleTimeChanged;
        TimeSystem.i.OnTimeChanged += HandleTimeChanged;
        TimeSystem.i.OnDayChanged += HandleTimeChanged;
    }

    void UnsubscribeTime() {
        if(TimeSystem.i == null) {
            return;
        }

        TimeSystem.i.OnTimeChanged -= HandleTimeChanged;
        TimeSystem.i.OnDayChanged -= HandleTimeChanged;
    }

    void HandleTimeChanged() {
        PruneExpired();
    }

    void TrimHistory() {
        if(maxHistoryRecords <= 0 || records.Count <= maxHistoryRecords) {
            return;
        }

        records = records
            .Where(record => record != null)
            .OrderByDescending(record => record.absoluteHour)
            .ThenByDescending(record => record.day)
            .Take(maxHistoryRecords)
            .OrderBy(record => record.absoluteHour)
            .ThenBy(record => record.day)
            .ToList();
    }

    string NormalizeSourceId(string sourceId, JourneyIncidentDefinition definition) {
        return string.IsNullOrWhiteSpace(sourceId) ? definition != null ? $"journey-incident:{definition.Id}" : "journey-incident" : sourceId;
    }

    int GetCurrentDay() {
        return TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    public object CaptureState() {
        PruneExpired();
        return new PlayerJourneyIncidentLogSaveData {
            activeIncidents = activeIncidents.Where(state => state != null).Select(state => new PlayerJourneyIncidentState(state)).ToList(),
            records = records.Where(record => record != null).Select(record => new PlayerJourneyIncidentRecord(record)).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerJourneyIncidentLogSaveData;
        activeIncidents = saveData?.activeIncidents?.Where(entry => entry != null).Select(entry => new PlayerJourneyIncidentState(entry)).ToList()
            ?? new List<PlayerJourneyIncidentState>();
        records = saveData?.records?.Where(entry => entry != null).Select(entry => new PlayerJourneyIncidentRecord(entry)).ToList()
            ?? new List<PlayerJourneyIncidentRecord>();
        OnJourneyIncidentsChanged?.Invoke();
    }
}

[Serializable]
public class PlayerJourneyIncidentState {
    [Tooltip("Unique active incident id.")]
    public string activeId;
    [Tooltip("Saved journey incident definition id.")]
    public string incidentId;
    [Tooltip("Saved journey incident display name.")]
    public string incidentName;
    [Tooltip("Saved journey incident category.")]
    public JourneyIncidentCategory category;
    [Tooltip("Saved journey incident severity.")]
    public JourneyIncidentSeverity severity;
    [Tooltip("Source id that activated this incident.")]
    public string sourceId;
    [Tooltip("Source display name saved for debug/fallback output.")]
    public string sourceName;
    [Tooltip("Region id this incident is active in. Empty means no region limit.")]
    public string regionId;
    [Tooltip("Region display name saved for debug/fallback output.")]
    public string regionName;
    [Tooltip("Activity zone id this incident is active in. Empty means no zone limit.")]
    public string zoneId;
    [Tooltip("Activity zone display name saved for debug/fallback output.")]
    public string zoneName;
    [Tooltip("In-game day when this incident started.")]
    public int startedDay;
    [Tooltip("Absolute in-game hour when this incident started.")]
    public int startedAbsoluteHour;
    [Tooltip("Absolute in-game hour when this incident expires.")]
    public int expiresAbsoluteHour;
    [Tooltip("If enabled, PlayerJourneyIncidentLog expires this incident when the expiry hour is reached.")]
    public bool expireAutomatically = true;

    public PlayerJourneyIncidentState() {
    }

    public PlayerJourneyIncidentState(PlayerJourneyIncidentState other) {
        activeId = other.activeId;
        incidentId = other.incidentId;
        incidentName = other.incidentName;
        category = other.category;
        severity = other.severity;
        sourceId = other.sourceId;
        sourceName = other.sourceName;
        regionId = other.regionId;
        regionName = other.regionName;
        zoneId = other.zoneId;
        zoneName = other.zoneName;
        startedDay = other.startedDay;
        startedAbsoluteHour = other.startedAbsoluteHour;
        expiresAbsoluteHour = other.expiresAbsoluteHour;
        expireAutomatically = other.expireAutomatically;
    }

    public bool IsExpired(int currentAbsoluteHour) {
        return expiresAbsoluteHour >= 0 && currentAbsoluteHour >= expiresAbsoluteHour;
    }

    public JourneyIncidentDefinition ResolveDefinition() {
        if(string.IsNullOrWhiteSpace(incidentId)) {
            return null;
        }

        return Resources.LoadAll<JourneyIncidentDefinition>("").FirstOrDefault(definition => definition != null && definition.Id == incidentId);
    }

    public RegionInfoDefinition ResolveRegion() {
        if(string.IsNullOrWhiteSpace(regionId)) {
            return null;
        }

        return Resources.LoadAll<RegionInfoDefinition>("").FirstOrDefault(region => region != null && region.Id == regionId);
    }

    public ActivityZoneDefinition ResolveZone() {
        if(string.IsNullOrWhiteSpace(zoneId)) {
            return null;
        }

        return Resources.LoadAll<ActivityZoneDefinition>("").FirstOrDefault(zone => zone != null && zone.Id == zoneId);
    }
}

[Serializable]
public class PlayerJourneyIncidentRecord {
    [Tooltip("Unique saved record id.")]
    public string recordId;
    [Tooltip("Saved journey incident definition id.")]
    public string incidentId;
    [Tooltip("Saved journey incident display name.")]
    public string incidentName;
    [Tooltip("Saved journey incident category.")]
    public JourneyIncidentCategory category;
    [Tooltip("Saved journey incident severity.")]
    public JourneyIncidentSeverity severity;
    [Tooltip("What happened to the incident.")]
    public JourneyIncidentPhase phase;
    [Tooltip("Source id that caused this record.")]
    public string sourceId;
    [Tooltip("Source display name saved for debug/fallback output.")]
    public string sourceName;
    [Tooltip("Region id related to this record.")]
    public string regionId;
    [Tooltip("Region display name saved for debug/fallback output.")]
    public string regionName;
    [Tooltip("Activity zone id related to this record.")]
    public string zoneId;
    [Tooltip("Activity zone display name saved for debug/fallback output.")]
    public string zoneName;
    [Tooltip("In-game day when this record was created.")]
    public int day;
    [Tooltip("Absolute in-game hour when this record was created.")]
    public int absoluteHour;
    [Tooltip("If enabled, this record represents a blocked attempt.")]
    public bool blocked;
    [Tooltip("Failure reason for blocked attempts.")]
    public string failureMessage;

    public PlayerJourneyIncidentRecord() {
    }

    public PlayerJourneyIncidentRecord(PlayerJourneyIncidentRecord other) {
        recordId = other.recordId;
        incidentId = other.incidentId;
        incidentName = other.incidentName;
        category = other.category;
        severity = other.severity;
        phase = other.phase;
        sourceId = other.sourceId;
        sourceName = other.sourceName;
        regionId = other.regionId;
        regionName = other.regionName;
        zoneId = other.zoneId;
        zoneName = other.zoneName;
        day = other.day;
        absoluteHour = other.absoluteHour;
        blocked = other.blocked;
        failureMessage = other.failureMessage;
    }
}

[Serializable]
public class PlayerJourneyIncidentLogSaveData {
    public List<PlayerJourneyIncidentState> activeIncidents = new List<PlayerJourneyIncidentState>();
    public List<PlayerJourneyIncidentRecord> records = new List<PlayerJourneyIncidentRecord>();
}
