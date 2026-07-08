using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerLocationVisitLog : MonoBehaviour, ISavable {
    [Tooltip("Maximum visit records kept in memory/save data. Older records are trimmed first. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxHistoryRecords = 600;
    [Tooltip("Runtime/save history of location visits and optional blocked attempts.")]
    [SerializeField] List<PlayerLocationVisitRecord> records = new List<PlayerLocationVisitRecord>();

    public IReadOnlyList<PlayerLocationVisitRecord> Records => records;
    public event Action<LocationVisitDefinition, PlayerLocationVisitRecord> OnLocationVisitRecorded;

    public bool CanRecord(LocationVisitDefinition visit, string sourceId, ConsequenceChainRepeatMode repeatMode, int cooldownHours, int maxRecordCount, out string failureMessage) {
        if(visit == null) {
            failureMessage = "No location visit selected.";
            return false;
        }

        string normalizedSourceId = NormalizeSourceId(sourceId);
        int totalSuccessfulRecords = GetVisitCount(visit, includeBlocked: false);
        if(maxRecordCount > 0 && totalSuccessfulRecords >= maxRecordCount) {
            failureMessage = $"{visit.DisplayName} has reached its maximum visit count.";
            return false;
        }

        if(repeatMode == ConsequenceChainRepeatMode.OnceEver && totalSuccessfulRecords > 0) {
            failureMessage = $"{visit.DisplayName} has already been visited.";
            return false;
        }

        if(repeatMode == ConsequenceChainRepeatMode.OncePerSource && GetVisitCount(visit, sourceId: normalizedSourceId, includeBlocked: false) > 0) {
            failureMessage = $"{visit.DisplayName} has already been visited from this source.";
            return false;
        }

        var lastSourceRecord = GetLastVisit(visit, sourceId: normalizedSourceId, includeBlocked: false);
        if(repeatMode == ConsequenceChainRepeatMode.Daily && lastSourceRecord != null && lastSourceRecord.day == GetCurrentDay()) {
            failureMessage = $"{visit.DisplayName} can only be visited once per day from this source.";
            return false;
        }

        if(repeatMode == ConsequenceChainRepeatMode.CooldownHours && lastSourceRecord != null) {
            int elapsed = GetCurrentAbsoluteHour() - lastSourceRecord.absoluteHour;
            int cooldown = Mathf.Max(0, cooldownHours);
            if(elapsed < cooldown) {
                failureMessage = $"{visit.DisplayName} will be available again in {cooldown - elapsed} hour(s).";
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    public PlayerLocationVisitRecord RecordVisit(LocationVisitDefinition visit, string sourceId, string sourceName, LocationVisitResult result) {
        if(visit == null) {
            return null;
        }

        string normalizedSourceId = NormalizeSourceId(sourceId);
        var record = new PlayerLocationVisitRecord {
            recordId = Guid.NewGuid().ToString("N"),
            visitId = visit.Id,
            visitName = visit.DisplayName,
            kind = visit.Kind,
            sourceId = normalizedSourceId,
            sourceName = sourceName ?? string.Empty,
            regionId = visit.Region != null ? visit.Region.Id : string.Empty,
            regionName = visit.Region != null ? visit.Region.DisplayName : string.Empty,
            mapMarkerId = visit.MapMarker != null ? visit.MapMarker.Id : string.Empty,
            sceneName = result != null ? result.sceneName : visit.SceneName,
            locationKey = result != null ? result.locationKey : visit.LocationKey,
            activityZoneId = visit.ActivityZone != null ? visit.ActivityZone.Id : string.Empty,
            day = GetCurrentDay(),
            absoluteHour = GetCurrentAbsoluteHour(),
            visitNumber = GetVisitCount(visit, sourceId: normalizedSourceId, includeBlocked: false) + 1,
            regionDiscovered = result != null && result.regionDiscovered,
            mapMarkerDiscovered = result != null && result.mapMarkerDiscovered,
            appliedDiscoveries = result != null ? Mathf.Max(0, result.appliedDiscoveries) : 0,
            blockedDiscoveries = result != null ? Mathf.Max(0, result.blockedDiscoveries) : 0,
            skippedDiscoveries = result != null ? Mathf.Max(0, result.skippedDiscoveries) : 0,
            appliedChains = result != null ? Mathf.Max(0, result.appliedChains) : 0,
            blockedChains = result != null ? Mathf.Max(0, result.blockedChains) : 0,
            skippedChains = result != null ? Mathf.Max(0, result.skippedChains) : 0,
            blocked = result != null && result.blocked,
            failureMessage = result != null ? result.failureMessage : null,
            messages = result != null && result.messages != null ? result.messages.Where(message => !string.IsNullOrWhiteSpace(message)).ToList() : new List<string>()
        };

        records.Add(record);
        TrimHistory();
        OnLocationVisitRecorded?.Invoke(visit, record);
        return record;
    }

    public int GetVisitCount(
        LocationVisitDefinition visit = null,
        string sourceId = null,
        LocationVisitKind? kind = null,
        RegionInfoDefinition region = null,
        string sceneName = null,
        string locationKey = null,
        bool includeBlocked = false
    ) {
        string normalizedSourceId = string.IsNullOrWhiteSpace(sourceId) ? null : NormalizeSourceId(sourceId);
        string regionId = region != null ? region.Id : null;
        return records.Count(record => Matches(record, visit, normalizedSourceId, kind, regionId, sceneName, locationKey, includeBlocked));
    }

    public bool HasVisited(LocationVisitDefinition visit = null, string sourceId = null, bool includeBlocked = false) {
        return GetVisitCount(visit, sourceId: sourceId, includeBlocked: includeBlocked) > 0;
    }

    public PlayerLocationVisitRecord GetLastVisit(
        LocationVisitDefinition visit = null,
        string sourceId = null,
        LocationVisitKind? kind = null,
        RegionInfoDefinition region = null,
        string sceneName = null,
        string locationKey = null,
        bool includeBlocked = false
    ) {
        string normalizedSourceId = string.IsNullOrWhiteSpace(sourceId) ? null : NormalizeSourceId(sourceId);
        string regionId = region != null ? region.Id : null;
        return records
            .Where(record => Matches(record, visit, normalizedSourceId, kind, regionId, sceneName, locationKey, includeBlocked))
            .OrderByDescending(record => record.absoluteHour)
            .ThenByDescending(record => record.day)
            .FirstOrDefault();
    }

    public int GetHoursSinceLastVisit(LocationVisitDefinition visit = null, string sourceId = null, bool includeBlocked = false) {
        var lastRecord = GetLastVisit(visit, sourceId: sourceId, includeBlocked: includeBlocked);
        return lastRecord != null ? Mathf.Max(0, GetCurrentAbsoluteHour() - lastRecord.absoluteHour) : -1;
    }

    bool Matches(PlayerLocationVisitRecord record, LocationVisitDefinition visit, string sourceId, LocationVisitKind? kind, string regionId, string sceneName, string locationKey, bool includeBlocked) {
        return record != null
            && (includeBlocked || !record.blocked)
            && (visit == null || record.visitId == visit.Id)
            && (string.IsNullOrWhiteSpace(sourceId) || record.sourceId == sourceId)
            && (!kind.HasValue || record.kind == kind.Value)
            && (string.IsNullOrWhiteSpace(regionId) || record.regionId == regionId)
            && (string.IsNullOrWhiteSpace(sceneName) || record.sceneName == sceneName)
            && (string.IsNullOrWhiteSpace(locationKey) || record.locationKey == locationKey);
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

    static string NormalizeSourceId(string sourceId) {
        return string.IsNullOrWhiteSpace(sourceId) ? "location-visit" : sourceId;
    }

    int GetCurrentDay() {
        return TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    public object CaptureState() {
        TrimHistory();
        return new PlayerLocationVisitLogSaveData {
            records = records.Where(record => record != null).Select(record => record.ToSaveData()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerLocationVisitLogSaveData;
        records = saveData?.records?.Where(record => record != null).Select(record => new PlayerLocationVisitRecord(record)).ToList()
            ?? new List<PlayerLocationVisitRecord>();
        TrimHistory();
    }
}

[Serializable]
public class PlayerLocationVisitRecord {
    [Tooltip("Unique runtime/save id for this visit record.")]
    public string recordId;
    [Tooltip("Saved visit definition id.")]
    public string visitId;
    [Tooltip("Saved visit display name for fallback/debug output.")]
    public string visitName;
    [Tooltip("Kind of visit that was recorded.")]
    public LocationVisitKind kind;
    [Tooltip("Source id used by repeat rules.")]
    public string sourceId;
    [Tooltip("Source display name for debug output.")]
    public string sourceName;
    [Tooltip("Related region id.")]
    public string regionId;
    [Tooltip("Related region display name.")]
    public string regionName;
    [Tooltip("Related map marker id.")]
    public string mapMarkerId;
    [Tooltip("Scene name connected to this visit.")]
    public string sceneName;
    [Tooltip("Custom location key connected to this visit.")]
    public string locationKey;
    [Tooltip("Related activity zone id.")]
    public string activityZoneId;
    [Tooltip("In-game day when this visit was recorded.")]
    public int day;
    [Tooltip("Absolute in-game hour when this visit was recorded.")]
    public int absoluteHour;
    [Tooltip("Successful visit number for this visit/source pair.")]
    [Min(0)]
    public int visitNumber;
    [Tooltip("If enabled, this visit discovered its region.")]
    public bool regionDiscovered;
    [Tooltip("If enabled, this visit discovered its map marker.")]
    public bool mapMarkerDiscovered;
    [Tooltip("Number of linked world discoveries applied successfully.")]
    [Min(0)]
    public int appliedDiscoveries;
    [Tooltip("Number of linked world discoveries blocked by their own rules.")]
    [Min(0)]
    public int blockedDiscoveries;
    [Tooltip("Number of null discovery slots skipped.")]
    [Min(0)]
    public int skippedDiscoveries;
    [Tooltip("Number of consequence chains applied successfully.")]
    [Min(0)]
    public int appliedChains;
    [Tooltip("Number of consequence chains blocked by their own rules.")]
    [Min(0)]
    public int blockedChains;
    [Tooltip("Number of null chain slots skipped.")]
    [Min(0)]
    public int skippedChains;
    [Tooltip("If enabled, this visit attempt was blocked before applying linked systems.")]
    public bool blocked;
    [Tooltip("Failure reason saved for blocked attempts.")]
    public string failureMessage;
    [Tooltip("Extra debug messages saved with this record.")]
    public List<string> messages = new List<string>();

    public PlayerLocationVisitRecord() {
    }

    public PlayerLocationVisitRecord(PlayerLocationVisitRecordSaveData saveData) {
        if(saveData == null) {
            return;
        }

        recordId = saveData.recordId;
        visitId = saveData.visitId;
        visitName = saveData.visitName;
        kind = saveData.kind;
        sourceId = saveData.sourceId;
        sourceName = saveData.sourceName;
        regionId = saveData.regionId;
        regionName = saveData.regionName;
        mapMarkerId = saveData.mapMarkerId;
        sceneName = saveData.sceneName;
        locationKey = saveData.locationKey;
        activityZoneId = saveData.activityZoneId;
        day = saveData.day;
        absoluteHour = saveData.absoluteHour;
        visitNumber = Mathf.Max(0, saveData.visitNumber);
        regionDiscovered = saveData.regionDiscovered;
        mapMarkerDiscovered = saveData.mapMarkerDiscovered;
        appliedDiscoveries = Mathf.Max(0, saveData.appliedDiscoveries);
        blockedDiscoveries = Mathf.Max(0, saveData.blockedDiscoveries);
        skippedDiscoveries = Mathf.Max(0, saveData.skippedDiscoveries);
        appliedChains = Mathf.Max(0, saveData.appliedChains);
        blockedChains = Mathf.Max(0, saveData.blockedChains);
        skippedChains = Mathf.Max(0, saveData.skippedChains);
        blocked = saveData.blocked;
        failureMessage = saveData.failureMessage;
        messages = saveData.messages ?? new List<string>();
    }

    public LocationVisitDefinition ResolveDefinition() {
        if(string.IsNullOrWhiteSpace(visitId)) {
            return null;
        }

        return Resources.LoadAll<LocationVisitDefinition>("").FirstOrDefault(visit => visit != null && visit.Id == visitId);
    }

    public PlayerLocationVisitRecordSaveData ToSaveData() {
        return new PlayerLocationVisitRecordSaveData {
            recordId = recordId,
            visitId = visitId,
            visitName = visitName,
            kind = kind,
            sourceId = sourceId,
            sourceName = sourceName,
            regionId = regionId,
            regionName = regionName,
            mapMarkerId = mapMarkerId,
            sceneName = sceneName,
            locationKey = locationKey,
            activityZoneId = activityZoneId,
            day = day,
            absoluteHour = absoluteHour,
            visitNumber = visitNumber,
            regionDiscovered = regionDiscovered,
            mapMarkerDiscovered = mapMarkerDiscovered,
            appliedDiscoveries = appliedDiscoveries,
            blockedDiscoveries = blockedDiscoveries,
            skippedDiscoveries = skippedDiscoveries,
            appliedChains = appliedChains,
            blockedChains = blockedChains,
            skippedChains = skippedChains,
            blocked = blocked,
            failureMessage = failureMessage,
            messages = messages != null ? messages.ToList() : new List<string>()
        };
    }
}

[Serializable]
public class PlayerLocationVisitLogSaveData {
    public List<PlayerLocationVisitRecordSaveData> records;
}

[Serializable]
public class PlayerLocationVisitRecordSaveData {
    public string recordId;
    public string visitId;
    public string visitName;
    public LocationVisitKind kind;
    public string sourceId;
    public string sourceName;
    public string regionId;
    public string regionName;
    public string mapMarkerId;
    public string sceneName;
    public string locationKey;
    public string activityZoneId;
    public int day;
    public int absoluteHour;
    public int visitNumber;
    public bool regionDiscovered;
    public bool mapMarkerDiscovered;
    public int appliedDiscoveries;
    public int blockedDiscoveries;
    public int skippedDiscoveries;
    public int appliedChains;
    public int blockedChains;
    public int skippedChains;
    public bool blocked;
    public string failureMessage;
    public List<string> messages;
}
