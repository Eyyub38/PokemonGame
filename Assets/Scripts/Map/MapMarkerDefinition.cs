using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum MapMarkerCategory {
    General,
    Player,
    Region,
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
    Police,
    Research,
    Contest,
    Housing,
    Event,
    Social,
    Custom
}

public enum MapMarkerVisibilityMode {
    AlwaysVisible,
    VisibleAfterDiscovery,
    HiddenUntilPokeNavUnlock,
    HiddenUntilRegionDiscovery
}

[CreateAssetMenu(menuName = "Map/Marker Definition")]
public class MapMarkerDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this marker. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in minimap/world map UI. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer note or player-facing description for this marker.")]
    [TextArea]
    [SerializeField] string description;
    [Tooltip("Broad marker category used by filters and UI icons.")]
    [SerializeField] MapMarkerCategory category = MapMarkerCategory.General;
    [Tooltip("Free-form tags used by filters, PokeNav, jobs and debug tools.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Display")]
    [Tooltip("Icon used by future minimap/world map UI.")]
    [SerializeField] Sprite icon;
    [Tooltip("Tint color used by future minimap/world map UI.")]
    [SerializeField] Color color = Color.white;
    [Tooltip("Higher priority markers can be drawn above lower priority markers.")]
    [SerializeField] int priority;
    [Tooltip("If enabled, this marker may appear on the minimap.")]
    [SerializeField] bool showOnMinimap = true;
    [Tooltip("If enabled, this marker may appear on the full world map.")]
    [SerializeField] bool showOnWorldMap = true;
    [Tooltip("If enabled, future UI should treat this marker as important/pinned.")]
    [SerializeField] bool important;

    [Header("Visibility")]
    [Tooltip("How this marker becomes visible.")]
    [SerializeField] MapMarkerVisibilityMode visibilityMode = MapMarkerVisibilityMode.VisibleAfterDiscovery;
    [Tooltip("If enabled, this marker is automatically discovered the first time a provider registers it.")]
    [SerializeField] bool discoverOnRegister;
    [Tooltip("Message/debug reason used when this marker is hidden.")]
    [SerializeField] string hiddenMessage = "This map marker has not been discovered yet.";

    [Header("Related Data")]
    [Tooltip("Region this marker belongs to.")]
    [SerializeField] RegionInfoDefinition region;
    [Tooltip("PokeNav entry that can unlock or explain this marker.")]
    [SerializeField] PokeNavEntryDefinition pokeNavEntry;
    [Tooltip("Social post that can unlock or highlight this marker.")]
    [SerializeField] SocialPostDefinition socialPost;
    [Tooltip("Pokemon connected to this marker, such as a sighting or habitat.")]
    [SerializeField] PokemonBase relatedPokemon;
    [Tooltip("Minimum Pokemon knowledge required when Related Pokemon is assigned.")]
    [SerializeField] PokemonKnowledgeLevel requiredPokemonKnowledge = PokemonKnowledgeLevel.Unknown;

    [Header("Access")]
    [Tooltip("Optional title, badge, permit or license required before this marker can be visible.")]
    [SerializeField] TitleDefinition requiredTitle;
    [Tooltip("Optional milestone required before this marker can be visible.")]
    [SerializeField] MilestoneDefinition requiredMilestone;
    [Tooltip("Optional faction whose reputation gates this marker.")]
    [SerializeField] ReputationFactionDefinition requiredFaction;
    [Tooltip("Minimum required reputation with the selected faction.")]
    [SerializeField] int requiredReputation;
    [Tooltip("Optional world event whose active state gates this marker.")]
    [SerializeField] WorldEventDefinition requiredWorldEvent;
    [Tooltip("Expected active state for the required world event.")]
    [SerializeField] bool requiredWorldEventActive = true;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public MapMarkerCategory Category => category;
    public IReadOnlyList<string> Tags => tags;
    public Sprite Icon => icon;
    public Color Color => color;
    public int Priority => priority;
    public bool ShowOnMinimap => showOnMinimap;
    public bool ShowOnWorldMap => showOnWorldMap;
    public bool Important => important;
    public MapMarkerVisibilityMode VisibilityMode => visibilityMode;
    public bool DiscoverOnRegister => discoverOnRegister;
    public RegionInfoDefinition Region => region;
    public PokeNavEntryDefinition PokeNavEntry => pokeNavEntry;
    public SocialPostDefinition SocialPost => socialPost;
    public PokemonBase RelatedPokemon => relatedPokemon;

    public bool CanShow(PlayerController player, PlayerMapLog mapLog, out string failureMessage) {
        if(!PassesVisibilityMode(player, mapLog, out failureMessage)) {
            return false;
        }

        if(requiredTitle != null && !(player?.GetComponent<PlayerTitles>()?.HasTitle(requiredTitle) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(hiddenMessage) ? $"You need {requiredTitle.DisplayName}." : hiddenMessage;
            return false;
        }

        if(requiredMilestone != null && !(player?.GetComponent<PlayerMilestones>()?.HasMilestone(requiredMilestone) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(hiddenMessage) ? $"You need {requiredMilestone.DisplayName} first." : hiddenMessage;
            return false;
        }

        if(requiredFaction != null) {
            int reputation = player?.GetComponent<PlayerReputation>()?.GetReputation(requiredFaction) ?? 0;
            if(reputation < requiredReputation) {
                failureMessage = string.IsNullOrWhiteSpace(hiddenMessage) ? $"You need more reputation with {requiredFaction.DisplayName}." : hiddenMessage;
                return false;
            }
        }

        if(requiredWorldEvent != null) {
            bool active = WorldEventManager.i != null && WorldEventManager.i.IsEventActive(requiredWorldEvent);
            if(active != requiredWorldEventActive) {
                failureMessage = string.IsNullOrWhiteSpace(hiddenMessage) ? $"{DisplayName} is not active right now." : hiddenMessage;
                return false;
            }
        }

        if(relatedPokemon != null && requiredPokemonKnowledge > PokemonKnowledgeLevel.Unknown) {
            var pokeNav = player != null ? player.GetComponent<PlayerPokeNavLog>() : null;
            if(pokeNav == null || pokeNav.GetPokemonKnowledgeLevel(relatedPokemon) < requiredPokemonKnowledge) {
                failureMessage = string.IsNullOrWhiteSpace(hiddenMessage) ? $"You need more information about {relatedPokemon.Name}." : hiddenMessage;
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && tags != null
            && tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    bool PassesVisibilityMode(PlayerController player, PlayerMapLog mapLog, out string failureMessage) {
        switch(visibilityMode) {
            case MapMarkerVisibilityMode.AlwaysVisible:
                failureMessage = null;
                return true;
            case MapMarkerVisibilityMode.HiddenUntilPokeNavUnlock:
                if(pokeNavEntry == null || (player?.GetComponent<PlayerPokeNavLog>()?.HasDiscoveredEntry(pokeNavEntry) ?? false)) {
                    failureMessage = null;
                    return true;
                }
                break;
            case MapMarkerVisibilityMode.HiddenUntilRegionDiscovery:
                if(region == null || (player?.GetComponent<PlayerPokeNavLog>()?.HasDiscoveredRegion(region) ?? region.VisibleByDefault)) {
                    failureMessage = null;
                    return true;
                }
                break;
            default:
                if(mapLog != null && mapLog.HasDiscoveredMarker(Id)) {
                    failureMessage = null;
                    return true;
                }
                break;
        }

        failureMessage = hiddenMessage;
        return false;
    }
}
