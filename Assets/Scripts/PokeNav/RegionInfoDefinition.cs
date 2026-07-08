using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum RegionInfoType {
    Town,
    Route,
    WildArea,
    Cave,
    Forest,
    Mountain,
    Sea,
    Facility,
    District,
    Special
}

[CreateAssetMenu(menuName = "PokeNav/Region Info Definition")]
public class RegionInfoDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this region. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in PokeNav/map UI. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer note or player-facing description for this region.")]
    [TextArea]
    [SerializeField] string description;
    [Tooltip("Broad region type used by filters and future map UI.")]
    [SerializeField] RegionInfoType regionType = RegionInfoType.Route;
    [Tooltip("Optional scene name connected to this region.")]
    [SerializeField] string sceneName;
    [Tooltip("Optional icon used by future PokeNav/map UI.")]
    [SerializeField] Sprite icon;
    [Tooltip("Free-form tags used by filters, social posts and future map UI.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("World Links")]
    [Tooltip("Encounter tables known for this region.")]
    [SerializeField] List<EncounterTableDefinition> encounterTables = new List<EncounterTableDefinition>();
    [Tooltip("Activity zones associated with this region.")]
    [SerializeField] List<ActivityZoneDefinition> activityZones = new List<ActivityZoneDefinition>();
    [Tooltip("Shop catalogs available in this region.")]
    [SerializeField] List<ShopCatalogDefinition> shops = new List<ShopCatalogDefinition>();
    [Tooltip("Transit stops available in this region.")]
    [SerializeField] List<TransitStopDefinition> transitStops = new List<TransitStopDefinition>();
    [Tooltip("Job boards available in this region.")]
    [SerializeField] List<JobBoardDefinition> jobBoards = new List<JobBoardDefinition>();
    [Tooltip("Important Pokemon that future UI can feature even if they are not directly listed in encounter tables.")]
    [SerializeField] List<PokemonBase> featuredPokemon = new List<PokemonBase>();

    [Header("Access")]
    [Tooltip("If enabled, this region appears in PokeNav before the player discovers it.")]
    [SerializeField] bool visibleByDefault;
    [Tooltip("Optional title, badge, permit or license required before this region info can be discovered.")]
    [SerializeField] TitleDefinition requiredTitle;
    [Tooltip("Optional milestone required before this region info can be discovered.")]
    [SerializeField] MilestoneDefinition requiredMilestone;
    [Tooltip("Optional world event whose active state gates this region info.")]
    [SerializeField] WorldEventDefinition requiredWorldEvent;
    [Tooltip("Expected active state for the required world event.")]
    [SerializeField] bool requiredWorldEventActive = true;
    [Tooltip("Message shown when discovery is blocked.")]
    [SerializeField] string lockedMessage = "This region has not been discovered yet.";

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public RegionInfoType RegionType => regionType;
    public string SceneName => sceneName;
    public Sprite Icon => icon;
    public IReadOnlyList<string> Tags => tags;
    public IReadOnlyList<EncounterTableDefinition> EncounterTables => encounterTables;
    public IReadOnlyList<ActivityZoneDefinition> ActivityZones => activityZones;
    public IReadOnlyList<ShopCatalogDefinition> Shops => shops;
    public IReadOnlyList<TransitStopDefinition> TransitStops => transitStops;
    public IReadOnlyList<JobBoardDefinition> JobBoards => jobBoards;
    public IReadOnlyList<PokemonBase> FeaturedPokemon => featuredPokemon;
    public bool VisibleByDefault => visibleByDefault;

    public bool CanDiscover(PlayerController player, out string failureMessage) {
        if(requiredTitle != null && !(player?.GetComponent<PlayerTitles>()?.HasTitle(requiredTitle) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need {requiredTitle.DisplayName}." : lockedMessage;
            return false;
        }

        if(requiredMilestone != null && !(player?.GetComponent<PlayerMilestones>()?.HasMilestone(requiredMilestone) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need {requiredMilestone.DisplayName} first." : lockedMessage;
            return false;
        }

        if(requiredWorldEvent != null) {
            bool active = WorldEventManager.i != null && WorldEventManager.i.IsEventActive(requiredWorldEvent);
            if(active != requiredWorldEventActive) {
                failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{DisplayName} is not available right now." : lockedMessage;
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    public List<PokemonBase> GetListedPokemon() {
        var pokemon = new List<PokemonBase>();
        if(featuredPokemon != null) {
            pokemon.AddRange(featuredPokemon.Where(p => p != null));
        }

        foreach(var table in encounterTables) {
            if(table?.Entries == null) {
                continue;
            }

            pokemon.AddRange(table.Entries.Where(entry => entry != null && entry.Pokemon != null).Select(entry => entry.Pokemon));
        }

        return pokemon.Distinct().OrderBy(p => p.Name).ToList();
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && tags != null
            && tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }
}
