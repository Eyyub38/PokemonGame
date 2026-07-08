using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum WorldDiscoveryKind {
    General,
    Region,
    PokemonSighting,
    NPCSighting,
    TrainerSighting,
    Shop,
    Transit,
    Resource,
    Farming,
    Activity,
    Event,
    Research,
    Police,
    Rumor,
    MapMarker,
    Social,
    Custom
}

[CreateAssetMenu(menuName = "World Discovery/World Discovery Definition")]
public class WorldDiscoveryDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this discovery. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in debug/future UI. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note explaining what the player learns or reveals.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Broad discovery kind used by validators, requirements and future UI filters.")]
    [SerializeField] WorldDiscoveryKind kind = WorldDiscoveryKind.General;
    [Tooltip("Free-form tags such as route, sighting, market, police, research or seasonal.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Repeat Rules")]
    [Tooltip("How often this discovery can be recorded for the current player.")]
    [SerializeField] ConsequenceChainRepeatMode repeatMode = ConsequenceChainRepeatMode.OncePerSource;
    [Tooltip("Cooldown in in-game hours when Repeat Mode is Cooldown Hours.")]
    [Min(0)]
    [SerializeField] int cooldownHours;
    [Tooltip("Maximum successful discovery records for this definition. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxRecordCount;
    [Tooltip("If enabled, successful discovery attempts are stored in PlayerWorldDiscoveryLog.")]
    [SerializeField] bool recordHistory = true;
    [Tooltip("If enabled, blocked discovery attempts are also stored in PlayerWorldDiscoveryLog.")]
    [SerializeField] bool recordBlockedAttempts;

    [Header("Requirements")]
    [Tooltip("How discovery-level requirements are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode requirementMatchMode = ConsequenceRequirementMatchMode.All;
    [Tooltip("Requirements checked before this discovery can apply.")]
    [SerializeField] List<ActivityRequirement> requirements = new List<ActivityRequirement>();

    [Header("Linked Knowledge")]
    [Tooltip("Pokemon whose PokeNav knowledge is updated by this discovery.")]
    [SerializeField] PokemonBase relatedPokemon = null;
    [Tooltip("Knowledge level granted to Related Pokemon.")]
    [SerializeField] PokemonKnowledgeLevel pokemonKnowledgeLevel = PokemonKnowledgeLevel.Seen;
    [Tooltip("Region discovered by this discovery.")]
    [SerializeField] RegionInfoDefinition relatedRegion = null;
    [Tooltip("PokeNav knowledge entry discovered by this discovery.")]
    [SerializeField] PokeNavEntryDefinition pokeNavEntry = null;
    [Tooltip("Social post unlocked or pushed by this discovery.")]
    [SerializeField] SocialPostDefinition socialPost = null;
    [Tooltip("Map marker discovered by this discovery.")]
    [SerializeField] MapMarkerDefinition mapMarker = null;

    [Header("Apply Options")]
    [Tooltip("If enabled, Related Pokemon knowledge is written to PlayerPokeNavLog.")]
    [SerializeField] bool recordPokemonKnowledge = true;
    [Tooltip("If enabled, Related Region is discovered in PlayerPokeNavLog.")]
    [SerializeField] bool discoverRegion = true;
    [Tooltip("If enabled, PokeNav Entry is discovered in PlayerPokeNavLog.")]
    [SerializeField] bool discoverPokeNavEntry = true;
    [Tooltip("If enabled, Social Post is unlocked in PlayerPokeNavLog.")]
    [SerializeField] bool unlockSocialPost = true;
    [Tooltip("If enabled, Map Marker is discovered in PlayerMapLog.")]
    [SerializeField] bool discoverMapMarker = true;

    [Header("Events")]
    [Tooltip("Optional event published when this discovery applies. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition discoveredEvent = null;
    [Tooltip("Optional event published when this discovery is blocked. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition blockedEvent = null;
    [Tooltip("If enabled, discovery events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed = true;
    [Tooltip("If enabled, discovery events are written to GameDebugLogger.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public WorldDiscoveryKind Kind => kind;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public ConsequenceChainRepeatMode RepeatMode => repeatMode;
    public int CooldownHours => Mathf.Max(0, cooldownHours);
    public int MaxRecordCount => Mathf.Max(0, maxRecordCount);
    public bool RecordHistory => recordHistory;
    public bool RecordBlockedAttempts => recordBlockedAttempts;
    public IReadOnlyList<ActivityRequirement> Requirements => requirements != null ? (IReadOnlyList<ActivityRequirement>)requirements : Array.Empty<ActivityRequirement>();
    public PokemonBase RelatedPokemon => relatedPokemon;
    public PokemonKnowledgeLevel PokemonKnowledgeLevel => pokemonKnowledgeLevel;
    public RegionInfoDefinition RelatedRegion => relatedRegion;
    public PokeNavEntryDefinition PokeNavEntry => pokeNavEntry;
    public SocialPostDefinition SocialPost => socialPost;
    public MapMarkerDefinition MapMarker => mapMarker;
    public bool RecordPokemonKnowledge => recordPokemonKnowledge;
    public bool DiscoverRegion => discoverRegion;
    public bool DiscoverPokeNavEntry => discoverPokeNavEntry;
    public bool UnlockSocialPost => unlockSocialPost;
    public bool DiscoverMapMarker => discoverMapMarker;

    public bool CanDiscover(PlayerController player, PlayerWorldDiscoveryLog log, string sourceId, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required for world discovery.";
            return false;
        }

        if(log != null && !log.CanRecord(this, sourceId, repeatMode, CooldownHours, MaxRecordCount, out failureMessage)) {
            return false;
        }

        if(!ConsequenceChainDefinition.RequirementsMet(player, requirements, requirementMatchMode, out failureMessage)) {
            return false;
        }

        if(discoverRegion && relatedRegion != null && !relatedRegion.CanDiscover(player, out failureMessage)) {
            return false;
        }

        if(discoverPokeNavEntry && pokeNavEntry != null && !pokeNavEntry.CanDiscover(player, out failureMessage)) {
            return false;
        }

        failureMessage = null;
        return true;
    }

    public WorldDiscoveryApplyResult Apply(PlayerController player, string sourceId = null, string sourceName = null, UnityEngine.Object context = null) {
        var result = new WorldDiscoveryApplyResult(Id, DisplayName, kind, NormalizeSourceId(sourceId), sourceName);
        var log = player != null ? player.GetComponent<PlayerWorldDiscoveryLog>() ?? player.gameObject.AddComponent<PlayerWorldDiscoveryLog>() : null;

        if(!CanDiscover(player, log, result.sourceId, out var failureMessage)) {
            result.blocked = true;
            result.failureMessage = failureMessage;
            if(recordHistory && recordBlockedAttempts) {
                log?.RecordDiscovery(this, result.sourceId, sourceName, result);
            }

            PublishDiscoveryEvent(blockedEvent, "blocked", result, player, context, GameEventImportance.Warning);
            return result;
        }

        ApplyLinkedKnowledge(player, result);
        if(recordHistory) {
            log?.RecordDiscovery(this, result.sourceId, sourceName, result);
        }

        PublishDiscoveryEvent(discoveredEvent, "discovered", result, player, context, GameEventImportance.Success);
        return result;
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    void ApplyLinkedKnowledge(PlayerController player, WorldDiscoveryApplyResult result) {
        var pokeNav = player.GetComponent<PlayerPokeNavLog>() ?? player.gameObject.AddComponent<PlayerPokeNavLog>();
        var mapLog = player.GetComponent<PlayerMapLog>() ?? player.gameObject.AddComponent<PlayerMapLog>();

        if(recordPokemonKnowledge && relatedPokemon != null && pokemonKnowledgeLevel > PokemonKnowledgeLevel.Unknown) {
            result.pokemonKnowledgeUpdated = pokeNav.RecordPokemonKnowledge(relatedPokemon, pokemonKnowledgeLevel, result.sourceId);
        }

        if(discoverRegion && relatedRegion != null) {
            result.regionDiscovered = pokeNav.DiscoverRegion(relatedRegion, out var regionFailure);
            if(!string.IsNullOrWhiteSpace(regionFailure)) {
                result.messages.Add(regionFailure);
            }
        }

        if(discoverPokeNavEntry && pokeNavEntry != null) {
            result.entryDiscovered = pokeNav.DiscoverEntry(pokeNavEntry, out var entryFailure);
            if(!string.IsNullOrWhiteSpace(entryFailure)) {
                result.messages.Add(entryFailure);
            }
        }

        if(unlockSocialPost && socialPost != null) {
            result.socialPostUnlocked = pokeNav.UnlockPost(socialPost);
        }

        if(discoverMapMarker && mapMarker != null) {
            result.mapMarkerDiscovered = mapLog.DiscoverMarker(mapMarker, result.sourceId);
        }
    }

    static string NormalizeSourceId(string sourceId) {
        return string.IsNullOrWhiteSpace(sourceId) ? "world-discovery" : sourceId;
    }

    void PublishDiscoveryEvent(GameEventDefinition eventDefinition, string phase, WorldDiscoveryApplyResult result, PlayerController player, UnityEngine.Object context, GameEventImportance importance) {
        GameEventPublishing.PublishOptional(
            eventDefinition,
            $"world-discovery.{phase}.{Id}",
            phase == "discovered" ? $"{DisplayName} discovered." : $"{DisplayName} blocked: {result?.failureMessage}",
            GameEventCategory.WorldDiscovery,
            importance,
            context != null ? context : player != null ? player : this,
            "WorldDiscoveryDefinition",
            GameEventScope.Player,
            showInFeed: showEventsInFeed,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("discoveryId", Id),
            GameEventPublishing.Value("discoveryName", DisplayName),
            GameEventPublishing.Value("kind", kind),
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("sourceId", result != null ? result.sourceId : string.Empty),
            GameEventPublishing.Value("pokemon", relatedPokemon != null ? relatedPokemon.name : string.Empty),
            GameEventPublishing.Value("region", relatedRegion != null ? relatedRegion.Id : string.Empty),
            GameEventPublishing.Value("pokeNavEntry", pokeNavEntry != null ? pokeNavEntry.Id : string.Empty),
            GameEventPublishing.Value("socialPost", socialPost != null ? socialPost.Id : string.Empty),
            GameEventPublishing.Value("mapMarker", mapMarker != null ? mapMarker.Id : string.Empty),
            GameEventPublishing.Value("blocked", result != null && result.blocked));
    }
}

public class WorldDiscoveryApplyResult {
    public readonly string discoveryId;
    public readonly string discoveryName;
    public readonly WorldDiscoveryKind kind;
    public readonly string sourceId;
    public readonly string sourceName;
    public bool pokemonKnowledgeUpdated;
    public bool regionDiscovered;
    public bool entryDiscovered;
    public bool socialPostUnlocked;
    public bool mapMarkerDiscovered;
    public bool blocked;
    public string failureMessage;
    public readonly List<string> messages = new List<string>();

    public WorldDiscoveryApplyResult(string discoveryId, string discoveryName, WorldDiscoveryKind kind, string sourceId, string sourceName) {
        this.discoveryId = discoveryId;
        this.discoveryName = discoveryName;
        this.kind = kind;
        this.sourceId = sourceId;
        this.sourceName = sourceName;
    }
}
