using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerWorldRegionLog : MonoBehaviour, ISavable {
    [Tooltip("Maximum region travel history records kept in memory/save data. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxHistoryRecords = 300;
    [Tooltip("Current world region id used by regional travel checks.")]
    [SerializeField] string currentRegionId = string.Empty;
    [Tooltip("Current world region display name stored for debug/future UI.")]
    [SerializeField] string currentRegionName = string.Empty;
    [Tooltip("Runtime/save ids for discovered world regions.")]
    [SerializeField] List<string> discoveredRegionIds = new List<string>();
    [Tooltip("Runtime/save ids for unlocked region travel routes.")]
    [SerializeField] List<string> unlockedRouteIds = new List<string>();
    [Tooltip("Runtime/save ids for completed region challenges.")]
    [SerializeField] List<string> completedChallengeIds = new List<string>();
    [Tooltip("Runtime/save history of regional travel attempts and successes.")]
    [SerializeField] List<RegionTravelRecord> travelRecords = new List<RegionTravelRecord>();
    [Tooltip("Runtime/save state for an active regional challenge or roster lock.")]
    [SerializeField] RegionChallengeState activeChallenge = new RegionChallengeState();

    public string CurrentRegionId => currentRegionId;
    public string CurrentRegionName => currentRegionName;
    public IReadOnlyList<string> DiscoveredRegionIds => discoveredRegionIds;
    public IReadOnlyList<string> UnlockedRouteIds => unlockedRouteIds;
    public IReadOnlyList<string> CompletedChallengeIds => completedChallengeIds;
    public IReadOnlyList<RegionTravelRecord> TravelRecords => travelRecords;
    public RegionChallengeState ActiveChallenge => activeChallenge;
    public bool HasActiveChallenge => activeChallenge != null && activeChallenge.active;

    public event Action<WorldRegionDefinition> OnRegionDiscovered;
    public event Action<WorldRegionDefinition> OnCurrentRegionChanged;
    public event Action<RegionTravelRouteDefinition> OnRouteUnlocked;
    public event Action<RegionTravelRecord> OnTravelRecorded;
    public event Action<RegionChallengeState> OnChallengeStateChanged;

    void Awake() {
        DiscoverDefaultRegions();
    }

    public bool IsCurrentRegion(WorldRegionDefinition region) {
        return region != null && IsCurrentRegion(region.Id);
    }

    public bool IsCurrentRegion(string regionId) {
        return !string.IsNullOrWhiteSpace(regionId)
            && string.Equals(currentRegionId, regionId, StringComparison.OrdinalIgnoreCase);
    }

    public void SetCurrentRegion(WorldRegionDefinition region, string source = null, bool discover = true) {
        if(region == null) {
            return;
        }

        currentRegionId = region.Id;
        currentRegionName = region.DisplayName;
        if(discover) {
            DiscoverRegion(region, source);
        }

        OnCurrentRegionChanged?.Invoke(region);
    }

    public bool HasDiscoveredRegion(WorldRegionDefinition region) {
        return region != null && (region.DiscoveredByDefault || HasDiscoveredRegion(region.Id));
    }

    public bool HasDiscoveredRegion(string regionId) {
        return !string.IsNullOrWhiteSpace(regionId) && discoveredRegionIds.Contains(regionId);
    }

    public bool DiscoverRegion(WorldRegionDefinition region, string source = null, bool publish = true) {
        if(region == null || HasDiscoveredRegion(region.Id)) {
            return false;
        }

        discoveredRegionIds.Add(region.Id);
        OnRegionDiscovered?.Invoke(region);
        if(publish) {
            region.PublishDiscovered(GetComponent<PlayerController>(), source);
        }
        return true;
    }

    public bool HasUnlockedRoute(RegionTravelRouteDefinition route) {
        return route != null && (route.UnlockedByDefault || HasUnlockedRoute(route.Id));
    }

    public bool HasUnlockedRoute(string routeId) {
        return !string.IsNullOrWhiteSpace(routeId) && unlockedRouteIds.Contains(routeId);
    }

    public bool UnlockRoute(RegionTravelRouteDefinition route, string source = null) {
        if(route == null || HasUnlockedRoute(route.Id)) {
            return false;
        }

        unlockedRouteIds.Add(route.Id);
        OnRouteUnlocked?.Invoke(route);
        PublishLogEvent("route-unlocked", route.Id, route.DisplayName, GameEventImportance.Success, source);
        return true;
    }

    public bool CanTravel(
        RegionTravelRouteDefinition route,
        string sourceId,
        ConsequenceChainRepeatMode repeatMode,
        int cooldownHours,
        int maxTravelCount,
        out string failureMessage
    ) {
        if(route == null) {
            failureMessage = "No regional route selected.";
            return false;
        }

        string normalizedSourceId = NormalizeSourceId(sourceId);
        int totalSuccessfulTravels = GetTravelCount(route, includeBlocked: false);
        if(maxTravelCount > 0 && totalSuccessfulTravels >= maxTravelCount) {
            failureMessage = $"{route.DisplayName} has reached its maximum travel count.";
            return false;
        }

        if(repeatMode == ConsequenceChainRepeatMode.OnceEver && totalSuccessfulTravels > 0) {
            failureMessage = $"{route.DisplayName} has already been used.";
            return false;
        }

        if(repeatMode == ConsequenceChainRepeatMode.OncePerSource && GetTravelCount(route, normalizedSourceId, includeBlocked: false) > 0) {
            failureMessage = $"{route.DisplayName} has already been used from this source.";
            return false;
        }

        var lastSourceTravel = GetLastTravel(route, normalizedSourceId, includeBlocked: false);
        if(repeatMode == ConsequenceChainRepeatMode.Daily && lastSourceTravel != null && lastSourceTravel.day == GetCurrentDay()) {
            failureMessage = $"{route.DisplayName} can only be used once per day from this source.";
            return false;
        }

        if(repeatMode == ConsequenceChainRepeatMode.CooldownHours && lastSourceTravel != null) {
            int elapsed = GetCurrentAbsoluteHour() - lastSourceTravel.absoluteHour;
            int cooldown = Mathf.Max(0, cooldownHours);
            if(elapsed < cooldown) {
                failureMessage = $"{route.DisplayName} will be available again in {cooldown - elapsed} hour(s).";
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    public RegionTravelRecord RecordTravel(RegionTravelRouteDefinition route, RegionTravelResult result) {
        if(route == null) {
            return null;
        }

        var record = new RegionTravelRecord {
            recordId = Guid.NewGuid().ToString("N"),
            routeId = route.Id,
            routeName = route.DisplayName,
            sourceId = NormalizeSourceId(result != null ? result.sourceId : null),
            sourceName = result != null ? result.sourceName : string.Empty,
            originRegionId = result != null ? result.originRegionId : route.OriginRegion != null ? route.OriginRegion.Id : string.Empty,
            originRegionName = result != null ? result.originRegionName : route.OriginRegion != null ? route.OriginRegion.DisplayName : string.Empty,
            destinationRegionId = result != null ? result.destinationRegionId : route.DestinationRegion != null ? route.DestinationRegion.Id : string.Empty,
            destinationRegionName = result != null ? result.destinationRegionName : route.DestinationRegion != null ? route.DestinationRegion.DisplayName : string.Empty,
            destinationSceneName = result != null ? result.destinationSceneName : route.DestinationSceneName,
            destinationSpawnPointId = result != null ? result.destinationSpawnPointId : route.DestinationSpawnPointId,
            travelMode = route.TravelMode,
            day = GetCurrentDay(),
            absoluteHour = GetCurrentAbsoluteHour(),
            blocked = result != null && result.blocked,
            failureMessage = result != null ? result.failureMessage : null,
            costsPaid = result != null && result.costsPaid,
            destinationDiscovered = result != null && result.destinationDiscovered,
            challengeStarted = result != null && result.challengeStarted,
            challengeId = result != null ? result.challengeId : string.Empty,
            challengeName = result != null ? result.challengeName : string.Empty,
            policyId = result != null ? result.policyId : string.Empty,
            policyName = result != null ? result.policyName : string.Empty,
            policyOptionId = result != null ? result.policyOptionId : string.Empty,
            policyOptionName = result != null ? result.policyOptionName : string.Empty,
            selectedPokemonId = result != null ? result.selectedPokemonId : string.Empty,
            selectedPokemonName = result != null ? result.selectedPokemonName : string.Empty,
            partyTransferMode = result != null ? result.partyTransferMode : RegionPartyTransferMode.KeepCurrentParty,
            estimatedTravelHours = result != null ? Mathf.Max(0, result.estimatedTravelHours) : route.EstimatedTravelHours,
            messages = result != null && result.messages != null ? result.messages.Where(message => !string.IsNullOrWhiteSpace(message)).ToList() : new List<string>()
        };

        travelRecords.Add(record);
        TrimHistory();
        OnTravelRecorded?.Invoke(record);
        return record;
    }

    public int GetTravelCount(RegionTravelRouteDefinition route = null, string sourceId = null, bool includeBlocked = false) {
        string routeId = route != null ? route.Id : null;
        string normalizedSourceId = string.IsNullOrWhiteSpace(sourceId) ? null : NormalizeSourceId(sourceId);
        return travelRecords.Count(record => record != null
            && (includeBlocked || !record.blocked)
            && (string.IsNullOrWhiteSpace(routeId) || record.routeId == routeId)
            && (string.IsNullOrWhiteSpace(normalizedSourceId) || record.sourceId == normalizedSourceId));
    }

    public RegionTravelRecord GetLastTravel(RegionTravelRouteDefinition route = null, string sourceId = null, bool includeBlocked = false) {
        string routeId = route != null ? route.Id : null;
        string normalizedSourceId = string.IsNullOrWhiteSpace(sourceId) ? null : NormalizeSourceId(sourceId);
        return travelRecords
            .Where(record => record != null
                && (includeBlocked || !record.blocked)
                && (string.IsNullOrWhiteSpace(routeId) || record.routeId == routeId)
                && (string.IsNullOrWhiteSpace(normalizedSourceId) || record.sourceId == normalizedSourceId))
            .OrderByDescending(record => record.absoluteHour)
            .ThenByDescending(record => record.day)
            .FirstOrDefault();
    }

    public RegionChallengeState StartChallenge(
        WorldRegionDefinition region,
        RegionChallengeProfileDefinition challenge,
        PokemonParty party,
        Pokemon selectedPokemon,
        string source = null,
        RegionPartyTransferMode? partyTransferModeOverride = null,
        List<string> allowedPokemonIdsOverride = null
    ) {
        if(challenge == null) {
            ClearActiveChallenge();
            return activeChallenge;
        }

        var partyTransferMode = partyTransferModeOverride ?? challenge.PartyTransferMode;
        activeChallenge = new RegionChallengeState {
            active = true,
            regionId = region != null ? region.Id : string.Empty,
            regionName = region != null ? region.DisplayName : string.Empty,
            challengeId = challenge.Id,
            challengeName = challenge.DisplayName,
            partyTransferMode = partyTransferMode,
            levelCap = challenge.LevelCap,
            storageLocked = challenge.LockStorageUntilCompleted,
            onlyLocalPokemonAllowed = challenge.OnlyLocalPokemonAllowed,
            startedDay = GetCurrentDay(),
            startedAbsoluteHour = GetCurrentAbsoluteHour(),
            source = source,
            allowedPokemonInstanceIds = allowedPokemonIdsOverride != null
                ? allowedPokemonIdsOverride.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList()
                : challenge.BuildAllowedPokemonIds(party, selectedPokemon)
        };

        OnChallengeStateChanged?.Invoke(activeChallenge);
        return activeChallenge;
    }

    public bool CompleteActiveChallenge(PlayerController player = null, bool applyRewards = true, UnityEngine.Object context = null) {
        if(!HasActiveChallenge) {
            return false;
        }

        var challenge = ResolveChallenge(activeChallenge.challengeId);
        var region = ResolveRegion(activeChallenge.regionId);
        if(applyRewards) {
            challenge?.ApplyCompletionRewards(player != null ? player : GetComponent<PlayerController>(), context != null ? context : this);
        }

        if(challenge != null && !completedChallengeIds.Contains(challenge.Id)) {
            completedChallengeIds.Add(challenge.Id);
            challenge.PublishCompleted(player != null ? player : GetComponent<PlayerController>(), region, context != null ? context : this);
        }

        ClearActiveChallenge();
        return true;
    }

    public void ClearActiveChallenge() {
        activeChallenge = new RegionChallengeState();
        OnChallengeStateChanged?.Invoke(activeChallenge);
    }

    public bool HasCompletedChallenge(RegionChallengeProfileDefinition challenge) {
        return challenge != null && completedChallengeIds.Contains(challenge.Id);
    }

    public bool IsPokemonAllowedByActiveChallenge(Pokemon pokemon) {
        if(!HasActiveChallenge || pokemon == null) {
            return true;
        }

        if(activeChallenge.allowedPokemonInstanceIds == null || activeChallenge.allowedPokemonInstanceIds.Count == 0) {
            return true;
        }

        return activeChallenge.allowedPokemonInstanceIds.Contains(pokemon.InstanceId);
    }

    void DiscoverDefaultRegions() {
        foreach(var region in Resources.LoadAll<WorldRegionDefinition>("")) {
            if(region != null && region.DiscoveredByDefault && !discoveredRegionIds.Contains(region.Id)) {
                discoveredRegionIds.Add(region.Id);
            }
        }
    }

    void TrimHistory() {
        if(maxHistoryRecords <= 0 || travelRecords.Count <= maxHistoryRecords) {
            return;
        }

        travelRecords = travelRecords
            .Where(record => record != null)
            .OrderByDescending(record => record.absoluteHour)
            .ThenByDescending(record => record.day)
            .Take(maxHistoryRecords)
            .OrderBy(record => record.absoluteHour)
            .ThenBy(record => record.day)
            .ToList();
    }

    WorldRegionDefinition ResolveRegion(string regionId) {
        return string.IsNullOrWhiteSpace(regionId)
            ? null
            : Resources.LoadAll<WorldRegionDefinition>("").FirstOrDefault(region => region != null && region.Id == regionId);
    }

    RegionChallengeProfileDefinition ResolveChallenge(string challengeId) {
        return string.IsNullOrWhiteSpace(challengeId)
            ? null
            : Resources.LoadAll<RegionChallengeProfileDefinition>("").FirstOrDefault(challenge => challenge != null && challenge.Id == challengeId);
    }

    static string NormalizeSourceId(string sourceId) {
        return string.IsNullOrWhiteSpace(sourceId) ? "region-travel" : sourceId;
    }

    int GetCurrentDay() {
        return TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    void PublishLogEvent(string phase, string valueId, string valueName, GameEventImportance importance, string source) {
        GameEventPublishing.PublishOptional(
            null,
            $"world-region-log.{phase}.{valueId}",
            $"{valueName} {phase}.",
            GameEventCategory.Transit,
            importance,
            this,
            "PlayerWorldRegionLog",
            GameEventScope.Player,
            showInFeed: true,
            writeToDebugLog: false,
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("id", valueId),
            GameEventPublishing.Value("name", valueName),
            GameEventPublishing.Value("source", source));
    }

    public object CaptureState() {
        TrimHistory();
        return new PlayerWorldRegionLogSaveData {
            currentRegionId = currentRegionId,
            currentRegionName = currentRegionName,
            discoveredRegionIds = discoveredRegionIds.Distinct().ToList(),
            unlockedRouteIds = unlockedRouteIds.Distinct().ToList(),
            completedChallengeIds = completedChallengeIds.Distinct().ToList(),
            activeChallenge = activeChallenge != null ? activeChallenge.ToSaveData() : new RegionChallengeStateSaveData(),
            travelRecords = travelRecords.Where(record => record != null).Select(record => record.ToSaveData()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerWorldRegionLogSaveData;
        if(saveData == null) {
            discoveredRegionIds = new List<string>();
            unlockedRouteIds = new List<string>();
            completedChallengeIds = new List<string>();
            travelRecords = new List<RegionTravelRecord>();
            activeChallenge = new RegionChallengeState();
            DiscoverDefaultRegions();
            return;
        }

        currentRegionId = saveData.currentRegionId;
        currentRegionName = saveData.currentRegionName;
        discoveredRegionIds = saveData.discoveredRegionIds?.Distinct().ToList() ?? new List<string>();
        unlockedRouteIds = saveData.unlockedRouteIds?.Distinct().ToList() ?? new List<string>();
        completedChallengeIds = saveData.completedChallengeIds?.Distinct().ToList() ?? new List<string>();
        activeChallenge = saveData.activeChallenge != null ? new RegionChallengeState(saveData.activeChallenge) : new RegionChallengeState();
        travelRecords = saveData.travelRecords?.Where(record => record != null).Select(record => new RegionTravelRecord(record)).ToList() ?? new List<RegionTravelRecord>();
        DiscoverDefaultRegions();
        TrimHistory();
    }
}

[Serializable]
public class RegionChallengeState {
    [Tooltip("Whether a region challenge is currently active.")]
    public bool active;
    [Tooltip("World region id connected to the active challenge.")]
    public string regionId;
    [Tooltip("World region display name connected to the active challenge.")]
    public string regionName;
    [Tooltip("Active challenge id.")]
    public string challengeId;
    [Tooltip("Active challenge display name.")]
    public string challengeName;
    [Tooltip("Party transfer mode used when the challenge started.")]
    public RegionPartyTransferMode partyTransferMode;
    [Tooltip("Level cap stored for the active challenge. 0 means none.")]
    public int levelCap;
    [Tooltip("If enabled, future storage UI should block PC/storage access until completion.")]
    public bool storageLocked;
    [Tooltip("If enabled, future checks can require local-region Pokemon.")]
    public bool onlyLocalPokemonAllowed;
    [Tooltip("Allowed Pokemon instance ids for roster locked challenges.")]
    public List<string> allowedPokemonInstanceIds = new List<string>();
    [Tooltip("In-game day when the challenge started.")]
    public int startedDay = -1;
    [Tooltip("Absolute in-game hour when the challenge started.")]
    public int startedAbsoluteHour = -1;
    [Tooltip("Source that started the challenge, such as route or event id.")]
    public string source;

    public RegionChallengeState() {
    }

    public RegionChallengeState(RegionChallengeStateSaveData saveData) {
        if(saveData == null) {
            return;
        }

        active = saveData.active;
        regionId = saveData.regionId;
        regionName = saveData.regionName;
        challengeId = saveData.challengeId;
        challengeName = saveData.challengeName;
        partyTransferMode = saveData.partyTransferMode;
        levelCap = Mathf.Max(0, saveData.levelCap);
        storageLocked = saveData.storageLocked;
        onlyLocalPokemonAllowed = saveData.onlyLocalPokemonAllowed;
        allowedPokemonInstanceIds = saveData.allowedPokemonInstanceIds ?? new List<string>();
        startedDay = saveData.startedDay;
        startedAbsoluteHour = saveData.startedAbsoluteHour;
        source = saveData.source;
    }

    public RegionChallengeStateSaveData ToSaveData() {
        return new RegionChallengeStateSaveData {
            active = active,
            regionId = regionId,
            regionName = regionName,
            challengeId = challengeId,
            challengeName = challengeName,
            partyTransferMode = partyTransferMode,
            levelCap = levelCap,
            storageLocked = storageLocked,
            onlyLocalPokemonAllowed = onlyLocalPokemonAllowed,
            allowedPokemonInstanceIds = allowedPokemonInstanceIds != null ? new List<string>(allowedPokemonInstanceIds) : new List<string>(),
            startedDay = startedDay,
            startedAbsoluteHour = startedAbsoluteHour,
            source = source
        };
    }
}

[Serializable]
public class RegionTravelRecord {
    [Tooltip("Unique runtime/save id for this travel record.")]
    public string recordId;
    [Tooltip("Regional route id.")]
    public string routeId;
    [Tooltip("Regional route display name.")]
    public string routeName;
    [Tooltip("Source id used by repeat rules.")]
    public string sourceId;
    [Tooltip("Source display name for debug/future UI.")]
    public string sourceName;
    [Tooltip("Origin world region id.")]
    public string originRegionId;
    [Tooltip("Origin world region display name.")]
    public string originRegionName;
    [Tooltip("Destination world region id.")]
    public string destinationRegionId;
    [Tooltip("Destination world region display name.")]
    public string destinationRegionName;
    [Tooltip("Destination scene name used by the route.")]
    public string destinationSceneName;
    [Tooltip("Destination spawn/portal id used by the route.")]
    public string destinationSpawnPointId;
    [Tooltip("Travel mode used by this route.")]
    public RegionTravelMode travelMode;
    [Tooltip("In-game day when this travel happened or was blocked.")]
    public int day;
    [Tooltip("Absolute in-game hour when this travel happened or was blocked.")]
    public int absoluteHour;
    [Tooltip("Whether this record is a blocked travel attempt.")]
    public bool blocked;
    [Tooltip("Failure message stored for blocked attempts.")]
    public string failureMessage;
    [Tooltip("Whether route costs were paid.")]
    public bool costsPaid;
    [Tooltip("Whether destination discovery was applied.")]
    public bool destinationDiscovered;
    [Tooltip("Whether a regional challenge started after travel.")]
    public bool challengeStarted;
    [Tooltip("Challenge id started by this travel, if any.")]
    public string challengeId;
    [Tooltip("Challenge display name started by this travel, if any.")]
    public string challengeName;
    [Tooltip("Travel policy id used by this route, if any.")]
    public string policyId;
    [Tooltip("Travel policy display name used by this route, if any.")]
    public string policyName;
    [Tooltip("Travel policy option id selected for this travel, if any.")]
    public string policyOptionId;
    [Tooltip("Travel policy option display name selected for this travel, if any.")]
    public string policyOptionName;
    [Tooltip("Selected Pokemon instance id used by this travel option, if any.")]
    public string selectedPokemonId;
    [Tooltip("Selected Pokemon display/nickname used by this travel option, if any.")]
    public string selectedPokemonName;
    [Tooltip("Party transfer mode applied or recorded for this travel.")]
    public RegionPartyTransferMode partyTransferMode;
    [Tooltip("Estimated in-game travel hours recorded for this travel.")]
    public int estimatedTravelHours;
    [Tooltip("Extra debug messages collected while applying this travel.")]
    public List<string> messages = new List<string>();

    public RegionTravelRecord() {
    }

    public RegionTravelRecord(RegionTravelRecordSaveData saveData) {
        if(saveData == null) {
            return;
        }

        recordId = saveData.recordId;
        routeId = saveData.routeId;
        routeName = saveData.routeName;
        sourceId = saveData.sourceId;
        sourceName = saveData.sourceName;
        originRegionId = saveData.originRegionId;
        originRegionName = saveData.originRegionName;
        destinationRegionId = saveData.destinationRegionId;
        destinationRegionName = saveData.destinationRegionName;
        destinationSceneName = saveData.destinationSceneName;
        destinationSpawnPointId = saveData.destinationSpawnPointId;
        travelMode = saveData.travelMode;
        day = saveData.day;
        absoluteHour = saveData.absoluteHour;
        blocked = saveData.blocked;
        failureMessage = saveData.failureMessage;
        costsPaid = saveData.costsPaid;
        destinationDiscovered = saveData.destinationDiscovered;
        challengeStarted = saveData.challengeStarted;
        challengeId = saveData.challengeId;
        challengeName = saveData.challengeName;
        policyId = saveData.policyId;
        policyName = saveData.policyName;
        policyOptionId = saveData.policyOptionId;
        policyOptionName = saveData.policyOptionName;
        selectedPokemonId = saveData.selectedPokemonId;
        selectedPokemonName = saveData.selectedPokemonName;
        partyTransferMode = saveData.partyTransferMode;
        estimatedTravelHours = Mathf.Max(0, saveData.estimatedTravelHours);
        messages = saveData.messages ?? new List<string>();
    }

    public RegionTravelRecordSaveData ToSaveData() {
        return new RegionTravelRecordSaveData {
            recordId = recordId,
            routeId = routeId,
            routeName = routeName,
            sourceId = sourceId,
            sourceName = sourceName,
            originRegionId = originRegionId,
            originRegionName = originRegionName,
            destinationRegionId = destinationRegionId,
            destinationRegionName = destinationRegionName,
            destinationSceneName = destinationSceneName,
            destinationSpawnPointId = destinationSpawnPointId,
            travelMode = travelMode,
            day = day,
            absoluteHour = absoluteHour,
            blocked = blocked,
            failureMessage = failureMessage,
            costsPaid = costsPaid,
            destinationDiscovered = destinationDiscovered,
            challengeStarted = challengeStarted,
            challengeId = challengeId,
            challengeName = challengeName,
            policyId = policyId,
            policyName = policyName,
            policyOptionId = policyOptionId,
            policyOptionName = policyOptionName,
            selectedPokemonId = selectedPokemonId,
            selectedPokemonName = selectedPokemonName,
            partyTransferMode = partyTransferMode,
            estimatedTravelHours = estimatedTravelHours,
            messages = messages != null ? new List<string>(messages) : new List<string>()
        };
    }
}

[Serializable]
public class PlayerWorldRegionLogSaveData {
    public string currentRegionId;
    public string currentRegionName;
    public List<string> discoveredRegionIds;
    public List<string> unlockedRouteIds;
    public List<string> completedChallengeIds;
    public RegionChallengeStateSaveData activeChallenge;
    public List<RegionTravelRecordSaveData> travelRecords;
}

[Serializable]
public class RegionChallengeStateSaveData {
    public bool active;
    public string regionId;
    public string regionName;
    public string challengeId;
    public string challengeName;
    public RegionPartyTransferMode partyTransferMode;
    public int levelCap;
    public bool storageLocked;
    public bool onlyLocalPokemonAllowed;
    public List<string> allowedPokemonInstanceIds;
    public int startedDay;
    public int startedAbsoluteHour;
    public string source;
}

[Serializable]
public class RegionTravelRecordSaveData {
    public string recordId;
    public string routeId;
    public string routeName;
    public string sourceId;
    public string sourceName;
    public string originRegionId;
    public string originRegionName;
    public string destinationRegionId;
    public string destinationRegionName;
    public string destinationSceneName;
    public string destinationSpawnPointId;
    public RegionTravelMode travelMode;
    public int day;
    public int absoluteHour;
    public bool blocked;
    public string failureMessage;
    public bool costsPaid;
    public bool destinationDiscovered;
    public bool challengeStarted;
    public string challengeId;
    public string challengeName;
    public string policyId;
    public string policyName;
    public string policyOptionId;
    public string policyOptionName;
    public string selectedPokemonId;
    public string selectedPokemonName;
    public RegionPartyTransferMode partyTransferMode;
    public int estimatedTravelHours;
    public List<string> messages;
}
