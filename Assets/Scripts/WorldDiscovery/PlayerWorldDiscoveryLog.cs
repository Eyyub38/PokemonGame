using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerWorldDiscoveryLog : MonoBehaviour, ISavable {
    [Tooltip("Maximum discovery records kept in memory/save data. Older records are trimmed first. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxHistoryRecords = 500;
    [Tooltip("Runtime/save history of world discoveries and optional blocked attempts.")]
    [SerializeField] List<PlayerWorldDiscoveryRecord> records = new List<PlayerWorldDiscoveryRecord>();

    public IReadOnlyList<PlayerWorldDiscoveryRecord> Records => records;
    public event Action<WorldDiscoveryDefinition, PlayerWorldDiscoveryRecord> OnWorldDiscoveryRecorded;

    public bool CanRecord(WorldDiscoveryDefinition discovery, string sourceId, ConsequenceChainRepeatMode repeatMode, int cooldownHours, int maxRecordCount, out string failureMessage) {
        if(discovery == null) {
            failureMessage = "No world discovery selected.";
            return false;
        }

        string normalizedSourceId = NormalizeSourceId(sourceId);
        int totalSuccessfulRecords = GetDiscoveryCount(discovery, includeBlocked: false);
        if(maxRecordCount > 0 && totalSuccessfulRecords >= maxRecordCount) {
            failureMessage = $"{discovery.DisplayName} has reached its maximum discovery count.";
            return false;
        }

        if(repeatMode == ConsequenceChainRepeatMode.OnceEver && totalSuccessfulRecords > 0) {
            failureMessage = $"{discovery.DisplayName} has already been discovered.";
            return false;
        }

        if(repeatMode == ConsequenceChainRepeatMode.OncePerSource && GetDiscoveryCount(discovery, normalizedSourceId, includeBlocked: false) > 0) {
            failureMessage = $"{discovery.DisplayName} has already been discovered from this source.";
            return false;
        }

        var lastSourceRecord = GetLastDiscovery(discovery, normalizedSourceId, includeBlocked: false);
        if(repeatMode == ConsequenceChainRepeatMode.Daily && lastSourceRecord != null && lastSourceRecord.day == GetCurrentDay()) {
            failureMessage = $"{discovery.DisplayName} can only be discovered once per day from this source.";
            return false;
        }

        if(repeatMode == ConsequenceChainRepeatMode.CooldownHours && lastSourceRecord != null) {
            int elapsed = GetCurrentAbsoluteHour() - lastSourceRecord.absoluteHour;
            int cooldown = Mathf.Max(0, cooldownHours);
            if(elapsed < cooldown) {
                failureMessage = $"{discovery.DisplayName} will be available again in {cooldown - elapsed} hour(s).";
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    public PlayerWorldDiscoveryRecord RecordDiscovery(WorldDiscoveryDefinition discovery, string sourceId, string sourceName, WorldDiscoveryApplyResult result) {
        if(discovery == null) {
            return null;
        }

        var record = new PlayerWorldDiscoveryRecord {
            recordId = Guid.NewGuid().ToString("N"),
            discoveryId = discovery.Id,
            discoveryName = discovery.DisplayName,
            kind = discovery.Kind,
            sourceId = NormalizeSourceId(sourceId),
            sourceName = sourceName ?? string.Empty,
            pokemonId = discovery.RelatedPokemon != null ? discovery.RelatedPokemon.name : string.Empty,
            pokemonName = discovery.RelatedPokemon != null ? discovery.RelatedPokemon.Name : string.Empty,
            regionId = discovery.RelatedRegion != null ? discovery.RelatedRegion.Id : string.Empty,
            pokeNavEntryId = discovery.PokeNavEntry != null ? discovery.PokeNavEntry.Id : string.Empty,
            socialPostId = discovery.SocialPost != null ? discovery.SocialPost.Id : string.Empty,
            mapMarkerId = discovery.MapMarker != null ? discovery.MapMarker.Id : string.Empty,
            day = GetCurrentDay(),
            absoluteHour = GetCurrentAbsoluteHour(),
            pokemonKnowledgeUpdated = result != null && result.pokemonKnowledgeUpdated,
            regionDiscovered = result != null && result.regionDiscovered,
            entryDiscovered = result != null && result.entryDiscovered,
            socialPostUnlocked = result != null && result.socialPostUnlocked,
            mapMarkerDiscovered = result != null && result.mapMarkerDiscovered,
            blocked = result != null && result.blocked,
            failureMessage = result != null ? result.failureMessage : null,
            messages = result != null && result.messages != null ? result.messages.Where(message => !string.IsNullOrWhiteSpace(message)).ToList() : new List<string>()
        };

        records.Add(record);
        TrimHistory();
        OnWorldDiscoveryRecorded?.Invoke(discovery, record);
        return record;
    }

    public int GetDiscoveryCount(
        WorldDiscoveryDefinition discovery = null,
        string sourceId = null,
        WorldDiscoveryKind? kind = null,
        PokemonBase pokemon = null,
        RegionInfoDefinition region = null,
        MapMarkerDefinition mapMarker = null,
        bool includeBlocked = false
    ) {
        string normalizedSourceId = string.IsNullOrWhiteSpace(sourceId) ? null : NormalizeSourceId(sourceId);
        string pokemonId = pokemon != null ? pokemon.name : null;
        string regionId = region != null ? region.Id : null;
        string mapMarkerId = mapMarker != null ? mapMarker.Id : null;

        return records.Count(record => Matches(record, discovery, normalizedSourceId, kind, pokemonId, regionId, mapMarkerId, includeBlocked));
    }

    public bool HasDiscovered(WorldDiscoveryDefinition discovery = null, string sourceId = null, bool includeBlocked = false) {
        return GetDiscoveryCount(discovery, sourceId, includeBlocked: includeBlocked) > 0;
    }

    public PlayerWorldDiscoveryRecord GetLastDiscovery(
        WorldDiscoveryDefinition discovery = null,
        string sourceId = null,
        WorldDiscoveryKind? kind = null,
        PokemonBase pokemon = null,
        RegionInfoDefinition region = null,
        MapMarkerDefinition mapMarker = null,
        bool includeBlocked = false
    ) {
        string normalizedSourceId = string.IsNullOrWhiteSpace(sourceId) ? null : NormalizeSourceId(sourceId);
        string pokemonId = pokemon != null ? pokemon.name : null;
        string regionId = region != null ? region.Id : null;
        string mapMarkerId = mapMarker != null ? mapMarker.Id : null;

        return records
            .Where(record => Matches(record, discovery, normalizedSourceId, kind, pokemonId, regionId, mapMarkerId, includeBlocked))
            .OrderByDescending(record => record.absoluteHour)
            .ThenByDescending(record => record.day)
            .FirstOrDefault();
    }

    public int GetHoursSinceLastDiscovery(WorldDiscoveryDefinition discovery = null, string sourceId = null, bool includeBlocked = false) {
        var lastRecord = GetLastDiscovery(discovery, sourceId, includeBlocked: includeBlocked);
        return lastRecord != null ? Mathf.Max(0, GetCurrentAbsoluteHour() - lastRecord.absoluteHour) : -1;
    }

    bool Matches(PlayerWorldDiscoveryRecord record, WorldDiscoveryDefinition discovery, string sourceId, WorldDiscoveryKind? kind, string pokemonId, string regionId, string mapMarkerId, bool includeBlocked) {
        return record != null
            && (includeBlocked || !record.blocked)
            && (discovery == null || record.discoveryId == discovery.Id)
            && (string.IsNullOrWhiteSpace(sourceId) || record.sourceId == sourceId)
            && (!kind.HasValue || record.kind == kind.Value)
            && (string.IsNullOrWhiteSpace(pokemonId) || record.pokemonId == pokemonId)
            && (string.IsNullOrWhiteSpace(regionId) || record.regionId == regionId)
            && (string.IsNullOrWhiteSpace(mapMarkerId) || record.mapMarkerId == mapMarkerId);
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
        return string.IsNullOrWhiteSpace(sourceId) ? "world-discovery" : sourceId;
    }

    int GetCurrentDay() {
        return TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    public object CaptureState() {
        TrimHistory();
        return new PlayerWorldDiscoveryLogSaveData {
            records = records.Where(record => record != null).Select(record => record.ToSaveData()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerWorldDiscoveryLogSaveData;
        records = saveData?.records?.Where(record => record != null).Select(record => new PlayerWorldDiscoveryRecord(record)).ToList()
            ?? new List<PlayerWorldDiscoveryRecord>();
        TrimHistory();
    }
}

[Serializable]
public class PlayerWorldDiscoveryRecord {
    [Tooltip("Unique runtime/save id for this discovery record.")]
    public string recordId;
    [Tooltip("Saved discovery definition id.")]
    public string discoveryId;
    [Tooltip("Saved discovery display name for fallback/debug output.")]
    public string discoveryName;
    [Tooltip("Kind of discovery that was recorded.")]
    public WorldDiscoveryKind kind;
    [Tooltip("Source id used by repeat rules.")]
    public string sourceId;
    [Tooltip("Source display name for debug output.")]
    public string sourceName;
    [Tooltip("Related Pokemon asset id/name.")]
    public string pokemonId;
    [Tooltip("Related Pokemon display name.")]
    public string pokemonName;
    [Tooltip("Related region id.")]
    public string regionId;
    [Tooltip("Related PokeNav entry id.")]
    public string pokeNavEntryId;
    [Tooltip("Related social post id.")]
    public string socialPostId;
    [Tooltip("Related map marker id.")]
    public string mapMarkerId;
    [Tooltip("In-game day when this discovery was recorded.")]
    public int day;
    [Tooltip("Absolute in-game hour when this discovery was recorded.")]
    public int absoluteHour;
    [Tooltip("If enabled, this discovery changed Pokemon knowledge.")]
    public bool pokemonKnowledgeUpdated;
    [Tooltip("If enabled, this discovery added a region to PokeNav.")]
    public bool regionDiscovered;
    [Tooltip("If enabled, this discovery added a knowledge entry to PokeNav.")]
    public bool entryDiscovered;
    [Tooltip("If enabled, this discovery unlocked a social post.")]
    public bool socialPostUnlocked;
    [Tooltip("If enabled, this discovery added a map marker.")]
    public bool mapMarkerDiscovered;
    [Tooltip("If enabled, this discovery attempt was blocked before applying linked knowledge.")]
    public bool blocked;
    [Tooltip("Failure reason saved for blocked attempts.")]
    public string failureMessage;
    [Tooltip("Extra debug messages saved with this record.")]
    public List<string> messages = new List<string>();

    public PlayerWorldDiscoveryRecord() {
    }

    public PlayerWorldDiscoveryRecord(PlayerWorldDiscoveryRecordSaveData saveData) {
        if(saveData == null) {
            return;
        }

        recordId = saveData.recordId;
        discoveryId = saveData.discoveryId;
        discoveryName = saveData.discoveryName;
        kind = saveData.kind;
        sourceId = saveData.sourceId;
        sourceName = saveData.sourceName;
        pokemonId = saveData.pokemonId;
        pokemonName = saveData.pokemonName;
        regionId = saveData.regionId;
        pokeNavEntryId = saveData.pokeNavEntryId;
        socialPostId = saveData.socialPostId;
        mapMarkerId = saveData.mapMarkerId;
        day = saveData.day;
        absoluteHour = saveData.absoluteHour;
        pokemonKnowledgeUpdated = saveData.pokemonKnowledgeUpdated;
        regionDiscovered = saveData.regionDiscovered;
        entryDiscovered = saveData.entryDiscovered;
        socialPostUnlocked = saveData.socialPostUnlocked;
        mapMarkerDiscovered = saveData.mapMarkerDiscovered;
        blocked = saveData.blocked;
        failureMessage = saveData.failureMessage;
        messages = saveData.messages ?? new List<string>();
    }

    public WorldDiscoveryDefinition ResolveDefinition() {
        if(string.IsNullOrWhiteSpace(discoveryId)) {
            return null;
        }

        return Resources.LoadAll<WorldDiscoveryDefinition>("").FirstOrDefault(discovery => discovery != null && discovery.Id == discoveryId);
    }

    public PlayerWorldDiscoveryRecordSaveData ToSaveData() {
        return new PlayerWorldDiscoveryRecordSaveData {
            recordId = recordId,
            discoveryId = discoveryId,
            discoveryName = discoveryName,
            kind = kind,
            sourceId = sourceId,
            sourceName = sourceName,
            pokemonId = pokemonId,
            pokemonName = pokemonName,
            regionId = regionId,
            pokeNavEntryId = pokeNavEntryId,
            socialPostId = socialPostId,
            mapMarkerId = mapMarkerId,
            day = day,
            absoluteHour = absoluteHour,
            pokemonKnowledgeUpdated = pokemonKnowledgeUpdated,
            regionDiscovered = regionDiscovered,
            entryDiscovered = entryDiscovered,
            socialPostUnlocked = socialPostUnlocked,
            mapMarkerDiscovered = mapMarkerDiscovered,
            blocked = blocked,
            failureMessage = failureMessage,
            messages = messages != null ? messages.ToList() : new List<string>()
        };
    }
}

[Serializable]
public class PlayerWorldDiscoveryLogSaveData {
    public List<PlayerWorldDiscoveryRecordSaveData> records;
}

[Serializable]
public class PlayerWorldDiscoveryRecordSaveData {
    public string recordId;
    public string discoveryId;
    public string discoveryName;
    public WorldDiscoveryKind kind;
    public string sourceId;
    public string sourceName;
    public string pokemonId;
    public string pokemonName;
    public string regionId;
    public string pokeNavEntryId;
    public string socialPostId;
    public string mapMarkerId;
    public int day;
    public int absoluteHour;
    public bool pokemonKnowledgeUpdated;
    public bool regionDiscovered;
    public bool entryDiscovered;
    public bool socialPostUnlocked;
    public bool mapMarkerDiscovered;
    public bool blocked;
    public string failureMessage;
    public List<string> messages;
}
