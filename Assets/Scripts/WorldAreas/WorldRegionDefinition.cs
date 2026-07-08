using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum WorldRegionCategory {
    MajorRegion,
    Island,
    WildArea,
    LeagueArea,
    Special
}

[CreateAssetMenu(menuName = "World Regions/World Region Definition")]
public class WorldRegionDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this world region. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in future world map/PokeNav UI. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note or player-facing description for this world region.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Broad world region category used by filters and future UI.")]
    [SerializeField] WorldRegionCategory category = WorldRegionCategory.MajorRegion;
    [Tooltip("Optional icon used by future world map/PokeNav UI.")]
    [SerializeField] Sprite icon;
    [Tooltip("Free-form tags such as kanto, coastal, league, cold, beginner or postgame.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Default Entry")]
    [Tooltip("Default scene loaded when entering this world region if a route does not override it.")]
    [SerializeField] string defaultSceneName = string.Empty;
    [Tooltip("Default portal/spawn key used after entering this world region if a route does not override it.")]
    [SerializeField] string defaultSpawnPointId = string.Empty;
    [Tooltip("Optional PokeNav/Map region card that represents the default arrival area.")]
    [SerializeField] RegionInfoDefinition defaultRegionInfo;
    [Tooltip("Optional map marker discovered when this world region is discovered or entered.")]
    [SerializeField] MapMarkerDefinition defaultMapMarker;

    [Header("World Links")]
    [Tooltip("PokeNav/map region cards that belong to this world region.")]
    [SerializeField] List<RegionInfoDefinition> regionInfos = new List<RegionInfoDefinition>();
    [Tooltip("Map markers that belong to this world region.")]
    [SerializeField] List<MapMarkerDefinition> mapMarkers = new List<MapMarkerDefinition>();
    [Tooltip("Encounter tables associated with this world region.")]
    [SerializeField] List<EncounterTableDefinition> encounterTables = new List<EncounterTableDefinition>();
    [Tooltip("Activity zones associated with this world region.")]
    [SerializeField] List<ActivityZoneDefinition> activityZones = new List<ActivityZoneDefinition>();
    [Tooltip("Shop catalogs available in this world region.")]
    [SerializeField] List<ShopCatalogDefinition> shops = new List<ShopCatalogDefinition>();
    [Tooltip("Service definitions available in this world region.")]
    [SerializeField] List<ServiceDefinition> services = new List<ServiceDefinition>();
    [Tooltip("Transit stops inside this world region.")]
    [SerializeField] List<TransitStopDefinition> transitStops = new List<TransitStopDefinition>();
    [Tooltip("Calendar events connected to this world region, such as leagues or festivals.")]
    [SerializeField] List<CalendarEventDefinition> calendarEvents = new List<CalendarEventDefinition>();
    [Tooltip("Battle rule sets commonly used in this world region.")]
    [SerializeField] List<BattleRuleSetDefinition> battleRuleSets = new List<BattleRuleSetDefinition>();
    [Tooltip("Important Pokemon that future UI can feature for this world region.")]
    [SerializeField] List<PokemonBase> featuredPokemon = new List<PokemonBase>();

    [Header("Discovery")]
    [Tooltip("If enabled, this world region starts discovered/unlocked for travel systems.")]
    [SerializeField] bool discoveredByDefault;
    [Tooltip("If enabled, entering this world region discovers its PokeNav region cards and map markers.")]
    [SerializeField] bool discoverLinkedContentOnEntry = true;
    [Tooltip("If enabled, linked transit stops are unlocked when this world region is discovered.")]
    [SerializeField] bool unlockTransitStopsOnDiscovery;

    [Header("Access")]
    [Tooltip("Optional title, badge, permit or license required before this world region can be entered.")]
    [SerializeField] TitleDefinition requiredTitle;
    [Tooltip("Optional milestone required before this world region can be entered.")]
    [SerializeField] MilestoneDefinition requiredMilestone;
    [Tooltip("Optional faction whose reputation gates this world region.")]
    [SerializeField] ReputationFactionDefinition requiredFaction;
    [Tooltip("Minimum required reputation with the selected faction.")]
    [SerializeField] int requiredReputation;
    [Tooltip("Optional world event whose active state gates this world region.")]
    [SerializeField] WorldEventDefinition requiredWorldEvent;
    [Tooltip("Expected active state for the required world event.")]
    [SerializeField] bool requiredWorldEventActive = true;
    [Tooltip("How custom requirements are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode requirementMatchMode = ConsequenceRequirementMatchMode.All;
    [Tooltip("Additional requirements checked before this world region can be entered.")]
    [SerializeField] List<ActivityRequirement> requirements = new List<ActivityRequirement>();
    [Tooltip("Message shown when access is blocked and no more specific message is available.")]
    [TextArea]
    [SerializeField] string lockedMessage = "This region is not available yet.";

    [Header("Events")]
    [Tooltip("Optional event published when this world region is discovered. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition discoveredEvent;
    [Tooltip("Optional event published when this world region is entered. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition enteredEvent;
    [Tooltip("If enabled, generated world region events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed = true;
    [Tooltip("If enabled, generated world region events are written to GameDebugLogger.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public WorldRegionCategory Category => category;
    public Sprite Icon => icon;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public string DefaultSceneName => defaultSceneName;
    public string DefaultSpawnPointId => defaultSpawnPointId;
    public RegionInfoDefinition DefaultRegionInfo => defaultRegionInfo;
    public MapMarkerDefinition DefaultMapMarker => defaultMapMarker;
    public IReadOnlyList<RegionInfoDefinition> RegionInfos => regionInfos != null ? (IReadOnlyList<RegionInfoDefinition>)regionInfos : Array.Empty<RegionInfoDefinition>();
    public IReadOnlyList<MapMarkerDefinition> MapMarkers => mapMarkers != null ? (IReadOnlyList<MapMarkerDefinition>)mapMarkers : Array.Empty<MapMarkerDefinition>();
    public IReadOnlyList<EncounterTableDefinition> EncounterTables => encounterTables != null ? (IReadOnlyList<EncounterTableDefinition>)encounterTables : Array.Empty<EncounterTableDefinition>();
    public IReadOnlyList<ActivityZoneDefinition> ActivityZones => activityZones != null ? (IReadOnlyList<ActivityZoneDefinition>)activityZones : Array.Empty<ActivityZoneDefinition>();
    public IReadOnlyList<ShopCatalogDefinition> Shops => shops != null ? (IReadOnlyList<ShopCatalogDefinition>)shops : Array.Empty<ShopCatalogDefinition>();
    public IReadOnlyList<ServiceDefinition> Services => services != null ? (IReadOnlyList<ServiceDefinition>)services : Array.Empty<ServiceDefinition>();
    public IReadOnlyList<TransitStopDefinition> TransitStops => transitStops != null ? (IReadOnlyList<TransitStopDefinition>)transitStops : Array.Empty<TransitStopDefinition>();
    public IReadOnlyList<CalendarEventDefinition> CalendarEvents => calendarEvents != null ? (IReadOnlyList<CalendarEventDefinition>)calendarEvents : Array.Empty<CalendarEventDefinition>();
    public IReadOnlyList<BattleRuleSetDefinition> BattleRuleSets => battleRuleSets != null ? (IReadOnlyList<BattleRuleSetDefinition>)battleRuleSets : Array.Empty<BattleRuleSetDefinition>();
    public IReadOnlyList<PokemonBase> FeaturedPokemon => featuredPokemon != null ? (IReadOnlyList<PokemonBase>)featuredPokemon : Array.Empty<PokemonBase>();
    public bool DiscoveredByDefault => discoveredByDefault;
    public bool DiscoverLinkedContentOnEntry => discoverLinkedContentOnEntry;
    public bool UnlockTransitStopsOnDiscovery => unlockTransitStopsOnDiscovery;
    public IReadOnlyList<ActivityRequirement> Requirements => requirements != null ? (IReadOnlyList<ActivityRequirement>)requirements : Array.Empty<ActivityRequirement>();

    public bool CanEnter(PlayerController player, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required to enter a world region.";
            return false;
        }

        if(requiredTitle != null && !(player.GetComponent<PlayerTitles>()?.HasTitle(requiredTitle) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need {requiredTitle.DisplayName}." : lockedMessage;
            return false;
        }

        if(requiredMilestone != null && !(player.GetComponent<PlayerMilestones>()?.HasMilestone(requiredMilestone) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need {requiredMilestone.DisplayName} first." : lockedMessage;
            return false;
        }

        if(requiredFaction != null) {
            int reputation = player.GetComponent<PlayerReputation>()?.GetReputation(requiredFaction) ?? 0;
            if(reputation < requiredReputation) {
                failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need more reputation with {requiredFaction.DisplayName}." : lockedMessage;
                return false;
            }
        }

        if(requiredWorldEvent != null) {
            bool active = WorldEventManager.i != null && WorldEventManager.i.IsEventActive(requiredWorldEvent);
            if(active != requiredWorldEventActive) {
                failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{DisplayName} is not available right now." : lockedMessage;
                return false;
            }
        }

        if(!RequirementsMet(player, out failureMessage)) {
            return false;
        }

        failureMessage = null;
        return true;
    }

    public void ApplyDiscovery(PlayerController player, string source = null, bool publish = true) {
        if(player == null) {
            return;
        }

        var regionLog = player.GetComponent<PlayerWorldRegionLog>();
        regionLog?.DiscoverRegion(this, source, publish);

        if(!discoverLinkedContentOnEntry) {
            return;
        }

        var pokeNav = player.GetComponent<PlayerPokeNavLog>();
        DiscoverRegionInfo(pokeNav, defaultRegionInfo);
        foreach(var regionInfo in RegionInfos) {
            DiscoverRegionInfo(pokeNav, regionInfo);
        }

        var map = player.GetComponent<PlayerMapLog>();
        map?.DiscoverMarker(defaultMapMarker, source ?? Id);
        foreach(var marker in MapMarkers) {
            map?.DiscoverMarker(marker, source ?? Id);
        }

        if(unlockTransitStopsOnDiscovery) {
            var transit = player.GetComponent<PlayerTransitLog>();
            foreach(var stop in TransitStops) {
                transit?.UnlockStop(stop, source ?? Id);
            }
        }

        var calendar = player.GetComponent<PlayerCalendarLog>();
        foreach(var calendarEvent in CalendarEvents) {
            calendar?.UnlockEvent(calendarEvent, source ?? Id);
        }
    }

    public void PublishEntered(PlayerController player, UnityEngine.Object context = null) {
        GameEventPublishing.PublishOptional(
            enteredEvent,
            $"world-region.entered.{Id}",
            $"{DisplayName} entered.",
            GameEventCategory.Transit,
            GameEventImportance.Success,
            context != null ? context : player,
            "WorldRegionDefinition",
            GameEventScope.Player,
            showInFeed: showEventsInFeed,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("regionId", Id),
            GameEventPublishing.Value("regionName", DisplayName),
            GameEventPublishing.Value("category", category));
    }

    public void PublishDiscovered(PlayerController player, string source = null) {
        GameEventPublishing.PublishOptional(
            discoveredEvent,
            $"world-region.discovered.{Id}",
            $"{DisplayName} discovered.",
            GameEventCategory.PokeNav,
            GameEventImportance.Success,
            player,
            "WorldRegionDefinition",
            GameEventScope.Player,
            showInFeed: showEventsInFeed,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("regionId", Id),
            GameEventPublishing.Value("regionName", DisplayName),
            GameEventPublishing.Value("source", source));
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    bool RequirementsMet(PlayerController player, out string failureMessage) {
        var activeRequirements = requirements?.Where(requirement => requirement != null).ToList() ?? new List<ActivityRequirement>();
        if(activeRequirements.Count == 0) {
            failureMessage = null;
            return true;
        }

        if(requirementMatchMode == ConsequenceRequirementMatchMode.Any) {
            foreach(var requirement in activeRequirements) {
                if(requirement.IsMet(player)) {
                    failureMessage = null;
                    return true;
                }
            }

            failureMessage = activeRequirements.FirstOrDefault()?.FailureMessage ?? lockedMessage;
            return false;
        }

        foreach(var requirement in activeRequirements) {
            if(!requirement.IsMet(player)) {
                failureMessage = requirement.FailureMessage;
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    void DiscoverRegionInfo(PlayerPokeNavLog pokeNav, RegionInfoDefinition regionInfo) {
        if(pokeNav != null && regionInfo != null) {
            pokeNav.DiscoverRegion(regionInfo, out _);
        }
    }
}
