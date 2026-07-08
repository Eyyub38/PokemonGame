using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum NavigationHintCategory {
    General,
    Story,
    Quest,
    Map,
    Pokemon,
    NPC,
    Trainer,
    Shop,
    Transit,
    Job,
    Activity,
    Encounter,
    Resource,
    Farming,
    Research,
    PokemonCare,
    Police,
    Contest,
    Event,
    Custom
}

[CreateAssetMenu(menuName = "Navigation/Hints/Hint Definition")]
public class NavigationHintDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this navigation hint. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in debug/future UI. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note or player-facing explanation for this hint.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Broad category used by future map/minimap filters.")]
    [SerializeField] NavigationHintCategory category = NavigationHintCategory.General;
    [Tooltip("Free-form tags such as main, side, lab, market, route, hidden, daily or urgent.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Target")]
    [Tooltip("Short target label shown by future UI. Empty uses Display Name or Map Marker name.")]
    [SerializeField] string targetLabel = string.Empty;
    [Tooltip("Region connected to this target.")]
    [SerializeField] RegionInfoDefinition region = null;
    [Tooltip("Map marker connected to this target.")]
    [SerializeField] MapMarkerDefinition mapMarker = null;
    [Tooltip("Activity zone connected to this target.")]
    [SerializeField] ActivityZoneDefinition activityZone = null;
    [Tooltip("Optional PokeNav entry connected to this target.")]
    [SerializeField] PokeNavEntryDefinition pokeNavEntry = null;
    [Tooltip("Optional social post connected to this target.")]
    [SerializeField] SocialPostDefinition socialPost = null;
    [Tooltip("Optional location visit applied or tracked by this target.")]
    [SerializeField] LocationVisitDefinition locationVisit = null;
    [Tooltip("Optional scene name connected to this target. Empty uses region scene or the active scene when recording.")]
    [SerializeField] string sceneName = string.Empty;
    [Tooltip("Optional custom location key such as route id, room id, marker id or portal id.")]
    [SerializeField] string locationKey = string.Empty;
    [Tooltip("If enabled, Target World Position is saved when no runtime position is supplied by the source.")]
    [SerializeField] bool useStoredWorldPosition;
    [Tooltip("Optional fallback world position used by future minimap/world map UI.")]
    [SerializeField] Vector3 targetWorldPosition;

    [Header("Display")]
    [Tooltip("Icon used by future minimap/world map UI.")]
    [SerializeField] Sprite icon = null;
    [Tooltip("Tint color used by future minimap/world map UI.")]
    [SerializeField] Color color = Color.white;
    [Tooltip("Higher priority hints can be sorted or drawn above lower priority hints.")]
    [SerializeField] int priority;
    [Tooltip("If enabled, this hint may appear on the minimap.")]
    [SerializeField] bool showOnMinimap = true;
    [Tooltip("If enabled, this hint may appear on the full world map.")]
    [SerializeField] bool showOnWorldMap = true;
    [Tooltip("If enabled, this hint may appear in future PokeNav guidance lists.")]
    [SerializeField] bool showInPokeNav = true;
    [Tooltip("If enabled, future UI should pin this hint by default.")]
    [SerializeField] bool pinnedByDefault;
    [Tooltip("If enabled, activating an already active hint refreshes its source, time and target data instead of creating a duplicate active hint.")]
    [SerializeField] bool refreshExistingHint = true;

    [Header("Repeat Rules")]
    [Tooltip("How often this hint can be activated for the current player.")]
    [SerializeField] ConsequenceChainRepeatMode repeatMode = ConsequenceChainRepeatMode.Unlimited;
    [Tooltip("Cooldown in in-game hours when Repeat Mode is Cooldown Hours.")]
    [Min(0)]
    [SerializeField] int cooldownHours;
    [Tooltip("Maximum successful activation records for this definition. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxActivationCount;
    [Tooltip("If enabled, successful hint operations are stored in PlayerNavigationHintLog.")]
    [SerializeField] bool recordHistory = true;
    [Tooltip("If enabled, blocked hint attempts are also stored in PlayerNavigationHintLog.")]
    [SerializeField] bool recordBlockedAttempts;

    [Header("Requirements")]
    [Tooltip("How hint-level requirements are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode requirementMatchMode = ConsequenceRequirementMatchMode.All;
    [Tooltip("Requirements checked before this hint can activate.")]
    [SerializeField] List<ActivityRequirement> requirements = new List<ActivityRequirement>();

    [Header("Linked Unlocks")]
    [Tooltip("If enabled, Region is discovered in PlayerPokeNavLog when this hint activates.")]
    [SerializeField] bool discoverRegion;
    [Tooltip("If enabled, Map Marker is discovered in PlayerMapLog when this hint activates.")]
    [SerializeField] bool discoverMapMarker = true;
    [Tooltip("If enabled, PokeNav Entry is discovered when this hint activates.")]
    [SerializeField] bool discoverPokeNavEntry;
    [Tooltip("If enabled, Social Post is unlocked when this hint activates.")]
    [SerializeField] bool unlockSocialPost;
    [Tooltip("World discoveries applied when this hint activates.")]
    [SerializeField] List<WorldDiscoveryDefinition> worldDiscoveries = new List<WorldDiscoveryDefinition>();

    [Header("Linked Consequences")]
    [Tooltip("Consequence chains applied after this hint activates. These are skipped when a consequence chain activates the hint to avoid accidental loops.")]
    [SerializeField] List<ConsequenceChainDefinition> activatedChains = new List<ConsequenceChainDefinition>();
    [Tooltip("Consequence chains applied after this hint completes.")]
    [SerializeField] List<ConsequenceChainDefinition> completedChains = new List<ConsequenceChainDefinition>();
    [Tooltip("Consequence chains applied after this hint is cleared/cancelled.")]
    [SerializeField] List<ConsequenceChainDefinition> clearedChains = new List<ConsequenceChainDefinition>();

    [Header("Events")]
    [Tooltip("Optional event published when this hint activates. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition activatedEvent = null;
    [Tooltip("Optional event published when this hint completes. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition completedEvent = null;
    [Tooltip("Optional event published when this hint is cleared. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition clearedEvent = null;
    [Tooltip("Optional event published when this hint is blocked. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition blockedEvent = null;
    [Tooltip("If enabled, hint events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed = true;
    [Tooltip("If enabled, hint events are written to GameDebugLogger.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public NavigationHintCategory Category => category;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public string TargetLabel => ResolveTargetLabel();
    public RegionInfoDefinition Region => region;
    public MapMarkerDefinition MapMarker => mapMarker;
    public ActivityZoneDefinition ActivityZone => activityZone;
    public PokeNavEntryDefinition PokeNavEntry => pokeNavEntry;
    public SocialPostDefinition SocialPost => socialPost;
    public LocationVisitDefinition LocationVisit => locationVisit;
    public string SceneName => sceneName;
    public string LocationKey => locationKey;
    public bool UseStoredWorldPosition => useStoredWorldPosition;
    public Vector3 TargetWorldPosition => targetWorldPosition;
    public Sprite Icon => icon != null ? icon : mapMarker != null ? mapMarker.Icon : null;
    public Color Color => color;
    public int Priority => priority;
    public bool ShowOnMinimap => showOnMinimap;
    public bool ShowOnWorldMap => showOnWorldMap;
    public bool ShowInPokeNav => showInPokeNav;
    public bool PinnedByDefault => pinnedByDefault;
    public bool RefreshExistingHint => refreshExistingHint;
    public ConsequenceChainRepeatMode RepeatMode => repeatMode;
    public int CooldownHours => Mathf.Max(0, cooldownHours);
    public int MaxActivationCount => Mathf.Max(0, maxActivationCount);
    public bool RecordHistory => recordHistory;
    public bool RecordBlockedAttempts => recordBlockedAttempts;
    public IReadOnlyList<ActivityRequirement> Requirements => requirements != null ? (IReadOnlyList<ActivityRequirement>)requirements : Array.Empty<ActivityRequirement>();
    public bool DiscoverRegion => discoverRegion;
    public bool DiscoverMapMarker => discoverMapMarker;
    public bool DiscoverPokeNavEntry => discoverPokeNavEntry;
    public bool UnlockSocialPost => unlockSocialPost;
    public IReadOnlyList<WorldDiscoveryDefinition> WorldDiscoveries => worldDiscoveries != null ? (IReadOnlyList<WorldDiscoveryDefinition>)worldDiscoveries : Array.Empty<WorldDiscoveryDefinition>();
    public IReadOnlyList<ConsequenceChainDefinition> ActivatedChains => activatedChains != null ? (IReadOnlyList<ConsequenceChainDefinition>)activatedChains : Array.Empty<ConsequenceChainDefinition>();
    public IReadOnlyList<ConsequenceChainDefinition> CompletedChains => completedChains != null ? (IReadOnlyList<ConsequenceChainDefinition>)completedChains : Array.Empty<ConsequenceChainDefinition>();
    public IReadOnlyList<ConsequenceChainDefinition> ClearedChains => clearedChains != null ? (IReadOnlyList<ConsequenceChainDefinition>)clearedChains : Array.Empty<ConsequenceChainDefinition>();

    public bool CanActivate(PlayerController player, PlayerNavigationHintLog log, string sourceId, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required for navigation hints.";
            return false;
        }

        if(log != null && !log.CanActivate(this, sourceId, repeatMode, CooldownHours, MaxActivationCount, out failureMessage)) {
            return false;
        }

        if(!ConsequenceChainDefinition.RequirementsMet(player, requirements, requirementMatchMode, out failureMessage)) {
            return false;
        }

        failureMessage = null;
        return true;
    }

    public NavigationHintResult Activate(PlayerController player, string sourceId = null, string sourceName = null, UnityEngine.Object context = null, Vector3? runtimeTargetPosition = null, bool applyConsequences = true) {
        var result = CreateResult(NavigationHintOperation.Activate, sourceId, sourceName, runtimeTargetPosition);
        var log = player != null ? player.GetComponent<PlayerNavigationHintLog>() ?? player.gameObject.AddComponent<PlayerNavigationHintLog>() : null;

        if(!CanActivate(player, log, result.sourceId, out var failureMessage)) {
            result.blocked = true;
            result.failureMessage = failureMessage;
            if(recordHistory && recordBlockedAttempts) {
                log?.RecordBlocked(this, result);
            }

            PublishHintEvent(blockedEvent, "blocked", result, player, context, GameEventImportance.Warning);
            return result;
        }

        ApplyLinkedUnlocks(player, result, context);
        if(recordHistory) {
            result.state = log?.ActivateHint(this, result);
        }

        if(applyConsequences) {
            ApplyHintChains(player, activatedChains, result, context, "activated");
        }

        PublishHintEvent(activatedEvent, "activated", result, player, context, GameEventImportance.Success);
        return result;
    }

    public NavigationHintResult Complete(PlayerController player, string sourceId = null, string sourceName = null, UnityEngine.Object context = null, bool applyConsequences = true) {
        var result = CreateResult(NavigationHintOperation.Complete, sourceId, sourceName, null);
        var log = player != null ? player.GetComponent<PlayerNavigationHintLog>() ?? player.gameObject.AddComponent<PlayerNavigationHintLog>() : null;
        if(log == null || !log.CompleteHint(this, result)) {
            result.blocked = true;
            result.failureMessage = $"{DisplayName} is not active.";
            PublishHintEvent(blockedEvent, "blocked", result, player, context, GameEventImportance.Warning);
            return result;
        }

        if(applyConsequences) {
            ApplyHintChains(player, completedChains, result, context, "completed");
        }

        PublishHintEvent(completedEvent, "completed", result, player, context, GameEventImportance.Success);
        return result;
    }

    public NavigationHintResult Clear(PlayerController player, string sourceId = null, string sourceName = null, UnityEngine.Object context = null, bool applyConsequences = true) {
        var result = CreateResult(NavigationHintOperation.Clear, sourceId, sourceName, null);
        var log = player != null ? player.GetComponent<PlayerNavigationHintLog>() ?? player.gameObject.AddComponent<PlayerNavigationHintLog>() : null;
        if(log == null || !log.ClearHint(this, result)) {
            result.blocked = true;
            result.failureMessage = $"{DisplayName} is not active.";
            PublishHintEvent(blockedEvent, "blocked", result, player, context, GameEventImportance.Warning);
            return result;
        }

        if(applyConsequences) {
            ApplyHintChains(player, clearedChains, result, context, "cleared");
        }

        PublishHintEvent(clearedEvent, "cleared", result, player, context, GameEventImportance.Info);
        return result;
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    NavigationHintResult CreateResult(NavigationHintOperation operation, string sourceId, string sourceName, Vector3? runtimeTargetPosition) {
        return new NavigationHintResult(
            Id,
            DisplayName,
            category,
            operation,
            NormalizeSourceId(sourceId),
            sourceName,
            ResolveTargetLabel(),
            ResolveSceneName(),
            ResolveLocationKey(),
            ResolveTargetPosition(runtimeTargetPosition));
    }

    void ApplyLinkedUnlocks(PlayerController player, NavigationHintResult result, UnityEngine.Object context) {
        if(player == null) {
            return;
        }

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

        if(discoverPokeNavEntry && pokeNavEntry != null) {
            result.pokeNavEntryDiscovered = pokeNav.DiscoverEntry(pokeNavEntry, out var entryFailure);
            if(!string.IsNullOrWhiteSpace(entryFailure)) {
                result.messages.Add(entryFailure);
            }
        }

        if(unlockSocialPost && socialPost != null) {
            result.socialPostUnlocked = pokeNav.UnlockPost(socialPost);
        }

        if(locationVisit != null) {
            var visitResult = locationVisit.Apply(player, result.sourceId, result.sourceName, context != null ? context : this, applyConsequences: false);
            if(visitResult != null && !visitResult.blocked) {
                result.locationVisitRecorded = true;
            } else if(visitResult != null && !string.IsNullOrWhiteSpace(visitResult.failureMessage)) {
                result.messages.Add($"{locationVisit.DisplayName}: {visitResult.failureMessage}");
            }
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

    void ApplyHintChains(PlayerController player, IEnumerable<ConsequenceChainDefinition> chains, NavigationHintResult result, UnityEngine.Object context, string phase) {
        if(player == null || chains == null) {
            return;
        }

        var chainContext = new ConsequenceChainContext {
            SourceId = $"{result.sourceId}:{phase}",
            SourceName = result.sourceName,
            Region = region,
            Zone = activityZone,
            ContextObject = context != null ? context : this
        };

        foreach(var chain in chains) {
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

    string ResolveTargetLabel() {
        if(!string.IsNullOrWhiteSpace(targetLabel)) {
            return targetLabel;
        }

        if(mapMarker != null) {
            return mapMarker.DisplayName;
        }

        if(locationVisit != null) {
            return locationVisit.DisplayName;
        }

        if(region != null) {
            return region.DisplayName;
        }

        return DisplayName;
    }

    string ResolveSceneName() {
        if(!string.IsNullOrWhiteSpace(sceneName)) {
            return sceneName;
        }

        if(locationVisit != null && !string.IsNullOrWhiteSpace(locationVisit.SceneName)) {
            return locationVisit.SceneName;
        }

        if(region != null && !string.IsNullOrWhiteSpace(region.SceneName)) {
            return region.SceneName;
        }

        return SceneManager.GetActiveScene().name;
    }

    string ResolveLocationKey() {
        if(!string.IsNullOrWhiteSpace(locationKey)) {
            return locationKey;
        }

        if(locationVisit != null && !string.IsNullOrWhiteSpace(locationVisit.LocationKey)) {
            return locationVisit.LocationKey;
        }

        if(mapMarker != null) {
            return mapMarker.Id;
        }

        if(activityZone != null) {
            return activityZone.Id;
        }

        if(region != null) {
            return region.Id;
        }

        return Id;
    }

    Vector3? ResolveTargetPosition(Vector3? runtimeTargetPosition) {
        if(runtimeTargetPosition.HasValue) {
            return runtimeTargetPosition.Value;
        }

        return useStoredWorldPosition ? targetWorldPosition : null;
    }

    static string NormalizeSourceId(string sourceId) {
        return string.IsNullOrWhiteSpace(sourceId) ? "navigation-hint" : sourceId;
    }

    void PublishHintEvent(GameEventDefinition eventDefinition, string phase, NavigationHintResult result, PlayerController player, UnityEngine.Object context, GameEventImportance importance) {
        GameEventPublishing.PublishOptional(
            eventDefinition,
            $"navigation-hint.{phase}.{Id}",
            phase == "blocked" ? $"{DisplayName} blocked: {result?.failureMessage}" : $"{DisplayName} {phase}.",
            GameEventCategory.Navigation,
            importance,
            context != null ? context : player != null ? player : this,
            "NavigationHintDefinition",
            GameEventScope.Player,
            showInFeed: showEventsInFeed,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("hintId", Id),
            GameEventPublishing.Value("hintName", DisplayName),
            GameEventPublishing.Value("category", category),
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("sourceId", result != null ? result.sourceId : string.Empty),
            GameEventPublishing.Value("sceneName", result != null ? result.sceneName : string.Empty),
            GameEventPublishing.Value("locationKey", result != null ? result.locationKey : string.Empty),
            GameEventPublishing.Value("region", region != null ? region.Id : string.Empty),
            GameEventPublishing.Value("mapMarker", mapMarker != null ? mapMarker.Id : string.Empty),
            GameEventPublishing.Value("blocked", result != null && result.blocked));
    }
}

public enum NavigationHintOperation {
    Activate,
    Complete,
    Clear
}

public class NavigationHintResult {
    public readonly string hintId;
    public readonly string hintName;
    public readonly NavigationHintCategory category;
    public readonly NavigationHintOperation operation;
    public readonly string sourceId;
    public readonly string sourceName;
    public readonly string targetLabel;
    public readonly string sceneName;
    public readonly string locationKey;
    public readonly Vector3? targetWorldPosition;
    public PlayerNavigationHintState state;
    public bool regionDiscovered;
    public bool mapMarkerDiscovered;
    public bool pokeNavEntryDiscovered;
    public bool socialPostUnlocked;
    public bool locationVisitRecorded;
    public int appliedDiscoveries;
    public int blockedDiscoveries;
    public int skippedDiscoveries;
    public int appliedChains;
    public int blockedChains;
    public int skippedChains;
    public bool blocked;
    public string failureMessage;
    public readonly List<string> messages = new List<string>();

    public NavigationHintResult(string hintId, string hintName, NavigationHintCategory category, NavigationHintOperation operation, string sourceId, string sourceName, string targetLabel, string sceneName, string locationKey, Vector3? targetWorldPosition) {
        this.hintId = hintId;
        this.hintName = hintName;
        this.category = category;
        this.operation = operation;
        this.sourceId = sourceId;
        this.sourceName = sourceName;
        this.targetLabel = targetLabel;
        this.sceneName = sceneName;
        this.locationKey = locationKey;
        this.targetWorldPosition = targetWorldPosition;
    }
}
