using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum LocationVisitKind {
    General,
    Region,
    Town,
    Route,
    Building,
    Facility,
    Dungeon,
    Cave,
    Forest,
    WildArea,
    Shop,
    Transit,
    ActivityZone,
    Event,
    Custom
}

[CreateAssetMenu(menuName = "Location Visits/Location Visit Definition")]
public class LocationVisitDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this location visit. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in debug/future UI. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note explaining what this visit represents.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Broad visit kind used by validators, requirements and future UI filters.")]
    [SerializeField] LocationVisitKind kind = LocationVisitKind.General;
    [Tooltip("Free-form tags such as town, route, cave, market, lab, first-visit or story.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Location")]
    [Tooltip("Region connected to this visit.")]
    [SerializeField] RegionInfoDefinition region = null;
    [Tooltip("Map marker discovered or highlighted by this visit.")]
    [SerializeField] MapMarkerDefinition mapMarker = null;
    [Tooltip("Optional scene name connected to this visit. Empty uses the active scene when recording.")]
    [SerializeField] string sceneName = string.Empty;
    [Tooltip("Optional custom location key such as portal id, route segment, building room or area id.")]
    [SerializeField] string locationKey = string.Empty;
    [Tooltip("Optional activity zone connected to this visit.")]
    [SerializeField] ActivityZoneDefinition activityZone = null;

    [Header("Repeat Rules")]
    [Tooltip("How often this visit can be recorded for the current player.")]
    [SerializeField] ConsequenceChainRepeatMode repeatMode = ConsequenceChainRepeatMode.OncePerSource;
    [Tooltip("Cooldown in in-game hours when Repeat Mode is Cooldown Hours.")]
    [Min(0)]
    [SerializeField] int cooldownHours;
    [Tooltip("Maximum successful visit records for this definition. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxRecordCount;
    [Tooltip("If enabled, successful visits are stored in PlayerLocationVisitLog.")]
    [SerializeField] bool recordHistory = true;
    [Tooltip("If enabled, blocked visit attempts are also stored in PlayerLocationVisitLog.")]
    [SerializeField] bool recordBlockedAttempts;

    [Header("Requirements")]
    [Tooltip("How visit-level requirements are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode requirementMatchMode = ConsequenceRequirementMatchMode.All;
    [Tooltip("Requirements checked before this visit can apply.")]
    [SerializeField] List<ActivityRequirement> requirements = new List<ActivityRequirement>();

    [Header("Linked Discoveries")]
    [Tooltip("If enabled, Region is discovered in PlayerPokeNavLog when this visit applies.")]
    [SerializeField] bool discoverRegion = true;
    [Tooltip("If enabled, Map Marker is discovered in PlayerMapLog when this visit applies.")]
    [SerializeField] bool discoverMapMarker = true;
    [Tooltip("World discoveries applied when this visit succeeds.")]
    [SerializeField] List<WorldDiscoveryDefinition> worldDiscoveries = new List<WorldDiscoveryDefinition>();

    [Header("Consequences")]
    [Tooltip("Consequence chains applied after this visit succeeds. These are skipped when a consequence chain records the visit to avoid accidental loops.")]
    [SerializeField] List<ConsequenceChainDefinition> visitChains = new List<ConsequenceChainDefinition>();

    [Header("Events")]
    [Tooltip("Optional event published when this visit applies. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition visitedEvent = null;
    [Tooltip("Optional event published when this visit is blocked. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition blockedEvent = null;
    [Tooltip("If enabled, visit events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed = true;
    [Tooltip("If enabled, visit events are written to GameDebugLogger.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public LocationVisitKind Kind => kind;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public RegionInfoDefinition Region => region;
    public MapMarkerDefinition MapMarker => mapMarker;
    public string SceneName => sceneName;
    public string LocationKey => locationKey;
    public ActivityZoneDefinition ActivityZone => activityZone;
    public ConsequenceChainRepeatMode RepeatMode => repeatMode;
    public int CooldownHours => Mathf.Max(0, cooldownHours);
    public int MaxRecordCount => Mathf.Max(0, maxRecordCount);
    public bool RecordHistory => recordHistory;
    public bool RecordBlockedAttempts => recordBlockedAttempts;
    public IReadOnlyList<ActivityRequirement> Requirements => requirements != null ? (IReadOnlyList<ActivityRequirement>)requirements : Array.Empty<ActivityRequirement>();
    public bool DiscoverRegion => discoverRegion;
    public bool DiscoverMapMarker => discoverMapMarker;
    public IReadOnlyList<WorldDiscoveryDefinition> WorldDiscoveries => worldDiscoveries != null ? (IReadOnlyList<WorldDiscoveryDefinition>)worldDiscoveries : Array.Empty<WorldDiscoveryDefinition>();
    public IReadOnlyList<ConsequenceChainDefinition> VisitChains => visitChains != null ? (IReadOnlyList<ConsequenceChainDefinition>)visitChains : Array.Empty<ConsequenceChainDefinition>();

    public bool CanVisit(PlayerController player, PlayerLocationVisitLog log, string sourceId, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required for location visits.";
            return false;
        }

        if(log != null && !log.CanRecord(this, sourceId, repeatMode, CooldownHours, MaxRecordCount, out failureMessage)) {
            return false;
        }

        if(!ConsequenceChainDefinition.RequirementsMet(player, requirements, requirementMatchMode, out failureMessage)) {
            return false;
        }

        if(discoverRegion && region != null && !region.CanDiscover(player, out failureMessage)) {
            return false;
        }

        failureMessage = null;
        return true;
    }

    public LocationVisitResult Apply(PlayerController player, string sourceId = null, string sourceName = null, UnityEngine.Object context = null, bool applyConsequences = true) {
        string resolvedSceneName = ResolveSceneName();
        string resolvedLocationKey = ResolveLocationKey(resolvedSceneName);
        var result = new LocationVisitResult(Id, DisplayName, kind, NormalizeSourceId(sourceId), sourceName, resolvedSceneName, resolvedLocationKey);
        var log = player != null ? player.GetComponent<PlayerLocationVisitLog>() ?? player.gameObject.AddComponent<PlayerLocationVisitLog>() : null;

        if(!CanVisit(player, log, result.sourceId, out var failureMessage)) {
            result.blocked = true;
            result.failureMessage = failureMessage;
            if(recordHistory && recordBlockedAttempts) {
                log?.RecordVisit(this, result.sourceId, sourceName, result);
            }

            PublishVisitEvent(blockedEvent, "blocked", result, player, context, GameEventImportance.Warning);
            return result;
        }

        ApplyLinkedSystems(player, result, context);
        if(recordHistory) {
            log?.RecordVisit(this, result.sourceId, sourceName, result);
        }

        if(applyConsequences) {
            ApplyVisitChains(player, result, context);
        }

        PublishVisitEvent(visitedEvent, "visited", result, player, context, GameEventImportance.Success);
        return result;
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    void ApplyLinkedSystems(PlayerController player, LocationVisitResult result, UnityEngine.Object context) {
        var pokeNav = player.GetComponent<PlayerPokeNavLog>() ?? player.gameObject.AddComponent<PlayerPokeNavLog>();
        var mapLog = player.GetComponent<PlayerMapLog>() ?? player.gameObject.AddComponent<PlayerMapLog>();

        if(discoverRegion && region != null) {
            result.regionDiscovered = pokeNav.DiscoverRegion(region, out var regionFailure);
            if(!string.IsNullOrWhiteSpace(regionFailure)) {
                result.messages.Add(regionFailure);
            }
        }

        if(discoverMapMarker && mapMarker != null) {
            result.mapMarkerDiscovered = mapLog.DiscoverMarker(mapMarker, result.sourceId);
        }

        foreach(var discovery in WorldDiscoveries) {
            if(discovery == null) {
                result.skippedDiscoveries++;
                continue;
            }

            var discoveryResult = discovery.Apply(player, result.sourceId, result.sourceName, context != null ? context : this);
            if(discoveryResult != null && !discoveryResult.blocked) {
                result.appliedDiscoveries++;
            } else {
                result.blockedDiscoveries++;
                if(discoveryResult != null && !string.IsNullOrWhiteSpace(discoveryResult.failureMessage)) {
                    result.messages.Add($"{discovery.DisplayName}: {discoveryResult.failureMessage}");
                }
            }
        }
    }

    void ApplyVisitChains(PlayerController player, LocationVisitResult result, UnityEngine.Object context) {
        if(player == null) {
            return;
        }

        var chainContext = new ConsequenceChainContext {
            SourceId = result.sourceId,
            SourceName = result.sourceName,
            Region = region,
            Zone = activityZone,
            ContextObject = context != null ? context : this
        };

        foreach(var chain in VisitChains) {
            if(chain == null) {
                result.skippedChains++;
                continue;
            }

            var chainResult = chain.Apply(player, chainContext, context != null ? context : this);
            if(chainResult != null && !chainResult.blocked) {
                result.appliedChains++;
            } else {
                result.blockedChains++;
                if(chainResult != null && !string.IsNullOrWhiteSpace(chainResult.failureMessage)) {
                    result.messages.Add($"{chain.DisplayName}: {chainResult.failureMessage}");
                }
            }
        }
    }

    string ResolveSceneName() {
        if(!string.IsNullOrWhiteSpace(sceneName)) {
            return sceneName;
        }

        if(region != null && !string.IsNullOrWhiteSpace(region.SceneName)) {
            return region.SceneName;
        }

        return SceneManager.GetActiveScene().name;
    }

    string ResolveLocationKey(string resolvedSceneName) {
        if(!string.IsNullOrWhiteSpace(locationKey)) {
            return locationKey;
        }

        if(region != null) {
            return region.Id;
        }

        return string.IsNullOrWhiteSpace(resolvedSceneName) ? Id : resolvedSceneName;
    }

    static string NormalizeSourceId(string sourceId) {
        return string.IsNullOrWhiteSpace(sourceId) ? "location-visit" : sourceId;
    }

    void PublishVisitEvent(GameEventDefinition eventDefinition, string phase, LocationVisitResult result, PlayerController player, UnityEngine.Object context, GameEventImportance importance) {
        GameEventPublishing.PublishOptional(
            eventDefinition,
            $"location-visit.{phase}.{Id}",
            phase == "visited" ? $"{DisplayName} visited." : $"{DisplayName} blocked: {result?.failureMessage}",
            GameEventCategory.LocationVisit,
            importance,
            context != null ? context : player != null ? player : this,
            "LocationVisitDefinition",
            GameEventScope.Player,
            showInFeed: showEventsInFeed,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("visitId", Id),
            GameEventPublishing.Value("visitName", DisplayName),
            GameEventPublishing.Value("kind", kind),
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("sourceId", result != null ? result.sourceId : string.Empty),
            GameEventPublishing.Value("sceneName", result != null ? result.sceneName : string.Empty),
            GameEventPublishing.Value("locationKey", result != null ? result.locationKey : string.Empty),
            GameEventPublishing.Value("region", region != null ? region.Id : string.Empty),
            GameEventPublishing.Value("mapMarker", mapMarker != null ? mapMarker.Id : string.Empty),
            GameEventPublishing.Value("blocked", result != null && result.blocked));
    }
}

public class LocationVisitResult {
    public readonly string visitId;
    public readonly string visitName;
    public readonly LocationVisitKind kind;
    public readonly string sourceId;
    public readonly string sourceName;
    public readonly string sceneName;
    public readonly string locationKey;
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
    public readonly List<string> messages = new List<string>();

    public LocationVisitResult(string visitId, string visitName, LocationVisitKind kind, string sourceId, string sourceName, string sceneName, string locationKey) {
        this.visitId = visitId;
        this.visitName = visitName;
        this.kind = kind;
        this.sourceId = sourceId;
        this.sourceName = sourceName;
        this.sceneName = sceneName;
        this.locationKey = locationKey;
    }
}
