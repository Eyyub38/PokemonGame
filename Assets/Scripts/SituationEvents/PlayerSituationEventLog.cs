using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerSituationEventLog : MonoBehaviour, ISavable {
    [Tooltip("Maximum situation event history records kept in save data. 0 keeps all records.")]
    [Min(0)]
    [SerializeField] int maxHistoryRecords = 240;
    [Tooltip("Situation event ids unlocked for this player.")]
    [SerializeField] List<string> unlockedEventIds = new List<string>();
    [Tooltip("Runtime/save active situation events.")]
    [SerializeField] List<PlayerSituationEventState> activeEvents = new List<PlayerSituationEventState>();
    [Tooltip("Runtime/save history of situation event starts, resolutions, expirations and blocked attempts.")]
    [SerializeField] List<PlayerSituationEventRecord> records = new List<PlayerSituationEventRecord>();

    public IReadOnlyList<string> UnlockedEventIds => unlockedEventIds;
    public IReadOnlyList<PlayerSituationEventState> ActiveEvents => activeEvents;
    public IReadOnlyList<PlayerSituationEventRecord> Records => records;

    public event Action<PlayerSituationEventState> OnSituationEventStarted;
    public event Action<PlayerSituationEventRecord> OnSituationEventRecorded;
    public event Action OnSituationEventsChanged;

    void OnEnable() {
        SubscribeTime();
    }

    void OnDisable() {
        UnsubscribeTime();
    }

    public bool HasUnlocked(SituationEventDefinition definition) {
        return definition != null && HasUnlocked(definition.Id);
    }

    public bool HasUnlocked(string eventId) {
        return !string.IsNullOrWhiteSpace(eventId) && unlockedEventIds.Contains(eventId);
    }

    public bool Unlock(SituationEventDefinition definition, string sourceId = null) {
        if(definition == null || HasUnlocked(definition)) {
            return false;
        }

        unlockedEventIds.Add(definition.Id);
        Record(definition, SituationEventPhase.Started, sourceId, definition.DisplayName, null, null, false, "unlocked");
        return true;
    }

    public bool CanStart(SituationEventDefinition definition, string sourceId, ConsequenceChainRepeatMode repeatMode, int cooldownHours, int maxStartCount, out string failureMessage) {
        if(definition == null) {
            failureMessage = "No situation event selected.";
            return false;
        }

        string normalizedSourceId = NormalizeSourceId(sourceId, definition);
        int totalStarts = GetStartCount(definition, includeBlocked: false);
        if(maxStartCount > 0 && totalStarts >= maxStartCount) {
            failureMessage = $"{definition.DisplayName} reached its maximum start count.";
            return false;
        }

        if(repeatMode == ConsequenceChainRepeatMode.OnceEver && totalStarts > 0) {
            failureMessage = $"{definition.DisplayName} has already happened.";
            return false;
        }

        if(repeatMode == ConsequenceChainRepeatMode.OncePerSource && GetStartCount(definition, normalizedSourceId, includeBlocked: false) > 0) {
            failureMessage = $"{definition.DisplayName} already happened from this source.";
            return false;
        }

        var lastSourceStart = GetLastRecord(definition, normalizedSourceId, SituationEventPhase.Started, includeBlocked: false);
        if(repeatMode == ConsequenceChainRepeatMode.Daily && lastSourceStart != null && lastSourceStart.day == GetCurrentDay()) {
            failureMessage = $"{definition.DisplayName} can only start once per day from this source.";
            return false;
        }

        if(repeatMode == ConsequenceChainRepeatMode.CooldownHours && lastSourceStart != null) {
            int elapsed = GetCurrentAbsoluteHour() - lastSourceStart.absoluteHour;
            int cooldown = Mathf.Max(0, cooldownHours);
            if(elapsed < cooldown) {
                failureMessage = $"{definition.DisplayName} can start again in {cooldown - elapsed} hour(s).";
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    public PlayerSituationEventState RecordStarted(SituationEventDefinition definition, string sourceId, string sourceName, RegionInfoDefinition region, ActivityZoneDefinition zone, int durationHours, bool expireAutomatically) {
        if(definition == null) {
            return null;
        }

        string normalizedSourceId = NormalizeSourceId(sourceId, definition);
        var activeState = durationHours > 0
            ? CreateActiveState(definition, normalizedSourceId, sourceName, region, zone, durationHours, expireAutomatically)
            : null;

        if(activeState != null) {
            activeEvents.Add(activeState);
            OnSituationEventStarted?.Invoke(activeState);
        }

        Record(definition, SituationEventPhase.Started, normalizedSourceId, sourceName, region, zone, false, null);
        OnSituationEventsChanged?.Invoke();
        return activeState;
    }

    public PlayerSituationEventRecord RecordBlocked(SituationEventDefinition definition, string sourceId, string sourceName, RegionInfoDefinition region, ActivityZoneDefinition zone, string failureMessage) {
        return Record(definition, SituationEventPhase.Blocked, NormalizeSourceId(sourceId, definition), sourceName, region, zone, true, failureMessage);
    }

    public bool IsActive(SituationEventDefinition definition, RegionInfoDefinition region = null, ActivityZoneDefinition zone = null) {
        if(definition == null) {
            return false;
        }

        PruneExpired();
        return activeEvents.Any(state => MatchesState(state, definition, null, region, zone));
    }

    public int Resolve(SituationEventDefinition definition, string sourceId = null, RegionInfoDefinition region = null, ActivityZoneDefinition zone = null) {
        if(definition == null) {
            return 0;
        }

        PruneExpired();
        var matches = activeEvents
            .Where(state => MatchesState(state, definition, sourceId, region, zone))
            .ToList();

        foreach(var state in matches) {
            activeEvents.Remove(state);
            Record(definition, SituationEventPhase.Resolved, state.sourceId, state.sourceName, state.ResolveRegion(), state.ResolveZone(), false, null);
        }

        if(matches.Count > 0) {
            OnSituationEventsChanged?.Invoke();
        }

        return matches.Count;
    }

    public int PruneExpired() {
        int currentHour = GetCurrentAbsoluteHour();
        var expired = activeEvents
            .Where(state => state != null && state.expireAutomatically && state.IsExpired(currentHour))
            .ToList();

        foreach(var state in expired) {
            activeEvents.Remove(state);
            var definition = state.ResolveDefinition();
            if(definition != null) {
                Record(definition, SituationEventPhase.Expired, state.sourceId, state.sourceName, state.ResolveRegion(), state.ResolveZone(), false, null);
                definition.ApplyExpiredEffects(GetComponent<PlayerController>(), state, this);
            }
        }

        if(expired.Count > 0) {
            OnSituationEventsChanged?.Invoke();
        }

        return expired.Count;
    }

    public int GetStartCount(SituationEventDefinition definition, string sourceId = null, bool includeBlocked = false) {
        if(definition == null) {
            return 0;
        }

        string normalizedSourceId = string.IsNullOrWhiteSpace(sourceId) ? null : NormalizeSourceId(sourceId, definition);
        return records.Count(record => record != null
            && record.eventId == definition.Id
            && record.phase == SituationEventPhase.Started
            && (includeBlocked || !record.blocked)
            && (string.IsNullOrWhiteSpace(normalizedSourceId) || record.sourceId == normalizedSourceId));
    }

    public PlayerSituationEventRecord GetLastRecord(SituationEventDefinition definition, string sourceId = null, SituationEventPhase? phase = null, bool includeBlocked = true) {
        if(definition == null) {
            return null;
        }

        string normalizedSourceId = string.IsNullOrWhiteSpace(sourceId) ? null : NormalizeSourceId(sourceId, definition);
        return records
            .Where(record => record != null
                && record.eventId == definition.Id
                && (!phase.HasValue || record.phase == phase.Value)
                && (includeBlocked || !record.blocked)
                && (string.IsNullOrWhiteSpace(normalizedSourceId) || record.sourceId == normalizedSourceId))
            .OrderByDescending(record => record.absoluteHour)
            .ThenByDescending(record => record.day)
            .FirstOrDefault();
    }

    PlayerSituationEventState CreateActiveState(SituationEventDefinition definition, string sourceId, string sourceName, RegionInfoDefinition region, ActivityZoneDefinition zone, int durationHours, bool expireAutomatically) {
        int currentHour = GetCurrentAbsoluteHour();
        return new PlayerSituationEventState {
            activeId = Guid.NewGuid().ToString("N"),
            eventId = definition.Id,
            eventName = definition.DisplayName,
            category = definition.Category,
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

    PlayerSituationEventRecord Record(SituationEventDefinition definition, SituationEventPhase phase, string sourceId, string sourceName, RegionInfoDefinition region, ActivityZoneDefinition zone, bool blocked, string failureMessage) {
        if(definition == null) {
            return null;
        }

        var record = new PlayerSituationEventRecord {
            recordId = Guid.NewGuid().ToString("N"),
            eventId = definition.Id,
            eventName = definition.DisplayName,
            category = definition.Category,
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
        OnSituationEventRecorded?.Invoke(record);
        return record;
    }

    bool MatchesState(PlayerSituationEventState state, SituationEventDefinition definition, string sourceId, RegionInfoDefinition region, ActivityZoneDefinition zone) {
        if(state == null || definition == null || state.eventId != definition.Id) {
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

    string NormalizeSourceId(string sourceId, SituationEventDefinition definition) {
        return string.IsNullOrWhiteSpace(sourceId) ? definition != null ? $"situation:{definition.Id}" : "situation" : sourceId;
    }

    int GetCurrentDay() {
        return TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    public object CaptureState() {
        PruneExpired();
        return new PlayerSituationEventLogSaveData {
            unlockedEventIds = unlockedEventIds != null ? unlockedEventIds.ToList() : new List<string>(),
            activeEvents = activeEvents.Where(state => state != null).Select(state => new PlayerSituationEventState(state)).ToList(),
            records = records.Where(record => record != null).Select(record => new PlayerSituationEventRecord(record)).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerSituationEventLogSaveData;
        unlockedEventIds = saveData?.unlockedEventIds?.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList() ?? new List<string>();
        activeEvents = saveData?.activeEvents?.Where(entry => entry != null).Select(entry => new PlayerSituationEventState(entry)).ToList()
            ?? new List<PlayerSituationEventState>();
        records = saveData?.records?.Where(entry => entry != null).Select(entry => new PlayerSituationEventRecord(entry)).ToList()
            ?? new List<PlayerSituationEventRecord>();
        OnSituationEventsChanged?.Invoke();
    }
}

[Serializable]
public class PlayerSituationEventState {
    [Tooltip("Unique active event id.")]
    public string activeId;
    [Tooltip("Saved situation event definition id.")]
    public string eventId;
    [Tooltip("Saved situation event display name.")]
    public string eventName;
    [Tooltip("Saved situation event category.")]
    public SituationEventCategory category;
    [Tooltip("Source id that started this event.")]
    public string sourceId;
    [Tooltip("Source display name saved for debug/fallback output.")]
    public string sourceName;
    [Tooltip("Region id this event is active in. Empty means no region limit.")]
    public string regionId;
    [Tooltip("Region display name saved for debug/fallback output.")]
    public string regionName;
    [Tooltip("Activity zone id this event is active in. Empty means no zone limit.")]
    public string zoneId;
    [Tooltip("Activity zone display name saved for debug/fallback output.")]
    public string zoneName;
    [Tooltip("In-game day when this event started.")]
    public int startedDay;
    [Tooltip("Absolute in-game hour when this event started.")]
    public int startedAbsoluteHour;
    [Tooltip("Absolute in-game hour when this event expires.")]
    public int expiresAbsoluteHour;
    [Tooltip("If enabled, PlayerSituationEventLog expires this event when the expiry hour is reached.")]
    public bool expireAutomatically = true;

    public PlayerSituationEventState() {
    }

    public PlayerSituationEventState(PlayerSituationEventState other) {
        activeId = other.activeId;
        eventId = other.eventId;
        eventName = other.eventName;
        category = other.category;
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

    public SituationEventDefinition ResolveDefinition() {
        if(string.IsNullOrWhiteSpace(eventId)) {
            return null;
        }

        return Resources.LoadAll<SituationEventDefinition>("").FirstOrDefault(definition => definition != null && definition.Id == eventId);
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
public class PlayerSituationEventRecord {
    [Tooltip("Unique saved record id.")]
    public string recordId;
    [Tooltip("Saved situation event definition id.")]
    public string eventId;
    [Tooltip("Saved situation event display name.")]
    public string eventName;
    [Tooltip("Saved situation event category.")]
    public SituationEventCategory category;
    [Tooltip("What happened to the event.")]
    public SituationEventPhase phase;
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

    public PlayerSituationEventRecord() {
    }

    public PlayerSituationEventRecord(PlayerSituationEventRecord other) {
        recordId = other.recordId;
        eventId = other.eventId;
        eventName = other.eventName;
        category = other.category;
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
public class PlayerSituationEventLogSaveData {
    public List<string> unlockedEventIds = new List<string>();
    public List<PlayerSituationEventState> activeEvents = new List<PlayerSituationEventState>();
    public List<PlayerSituationEventRecord> records = new List<PlayerSituationEventRecord>();
}
