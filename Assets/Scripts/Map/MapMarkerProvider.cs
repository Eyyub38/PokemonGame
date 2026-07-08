using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MapMarkerProvider : MonoBehaviour, IPlayerTriggerable {
    [Header("Marker")]
    [Tooltip("Marker definition used by minimap/world map UI.")]
    [SerializeField] MapMarkerDefinition markerDefinition;
    [Tooltip("Optional save/id override for this marker instance. Empty uses marker definition id or GameObject name.")]
    [SerializeField] string markerInstanceId;
    [Tooltip("If enabled, GameObject name is used when no explicit marker id exists.")]
    [SerializeField] bool fallbackToGameObjectName = true;

    [Header("Position")]
    [Tooltip("Optional transform used as marker position. Empty uses this transform.")]
    [SerializeField] Transform positionOverride;
    [Tooltip("Optional offset added to the marker world position.")]
    [SerializeField] Vector3 positionOffset;

    [Header("Fallback Display")]
    [Tooltip("Category used when no marker definition is assigned.")]
    [SerializeField] MapMarkerCategory fallbackCategory = MapMarkerCategory.General;
    [Tooltip("Display name used when no marker definition is assigned. Empty tries known components or GameObject name.")]
    [SerializeField] string fallbackDisplayName;
    [Tooltip("Description used when no marker definition is assigned.")]
    [TextArea]
    [SerializeField] string fallbackDescription;
    [Tooltip("Icon used when no marker definition is assigned.")]
    [SerializeField] Sprite fallbackIcon;
    [Tooltip("Color used when no marker definition is assigned.")]
    [SerializeField] Color fallbackColor = Color.white;
    [Tooltip("Priority used when no marker definition is assigned.")]
    [SerializeField] int fallbackPriority;
    [Tooltip("If enabled, fallback marker can appear on minimap.")]
    [SerializeField] bool fallbackShowOnMinimap = true;
    [Tooltip("If enabled, fallback marker can appear on world map.")]
    [SerializeField] bool fallbackShowOnWorldMap = true;

    [Header("Discovery")]
    [Tooltip("If enabled, this provider registers itself with MapMarkerRegistry while active.")]
    [SerializeField] bool registerWhileEnabled = true;
    [Tooltip("If enabled, player trigger discovery marks this marker as discovered.")]
    [SerializeField] bool discoverOnPlayerTrigger = true;
    [Tooltip("If enabled, this marker is discovered when the provider registers.")]
    [SerializeField] bool discoverOnRegister;
    [Tooltip("If enabled, related region and PokeNav entry are discovered when the marker is discovered.")]
    [SerializeField] bool discoverRelatedPokeNavInfo = true;
    [Tooltip("If enabled, repeated player triggers can call OnPlayerTriggered more than once.")]
    [SerializeField] bool triggerRepeatedly = false;

    public MapMarkerDefinition MarkerDefinition => markerDefinition;
    public bool TriggerRepeatedly => triggerRepeatedly;
    public string MarkerId {
        get {
            if(!string.IsNullOrWhiteSpace(markerInstanceId)) {
                return markerInstanceId;
            }

            if(markerDefinition != null) {
                return markerDefinition.Id;
            }

            return fallbackToGameObjectName ? name : "map-marker";
        }
    }

    void OnEnable() {
        if(registerWhileEnabled) {
            MapMarkerRegistry.Register(this);
        }

        if(discoverOnRegister || (markerDefinition != null && markerDefinition.DiscoverOnRegister)) {
            Discover(PlayerController.i, "register");
        }
    }

    void OnDisable() {
        if(registerWhileEnabled) {
            MapMarkerRegistry.Unregister(this);
        }
    }

    public void OnPlayerTriggered(PlayerController player) {
        if(discoverOnPlayerTrigger) {
            Discover(player, "trigger");
        }
    }

    public bool Discover(PlayerController player, string source = null) {
        if(player == null) {
            return false;
        }

        var mapLog = player.GetComponent<PlayerMapLog>();
        bool changed = mapLog != null && mapLog.DiscoverMarker(MarkerId, ResolveDisplayName(), source);

        if(discoverRelatedPokeNavInfo && markerDefinition != null) {
            var pokeNav = player.GetComponent<PlayerPokeNavLog>();
            if(markerDefinition.Region != null) {
                pokeNav?.DiscoverRegion(markerDefinition.Region, out _);
            }

            if(markerDefinition.PokeNavEntry != null) {
                pokeNav?.DiscoverEntry(markerDefinition.PokeNavEntry, out _);
            }

            if(markerDefinition.SocialPost != null) {
                pokeNav?.UnlockPost(markerDefinition.SocialPost);
            }
        }

        return changed;
    }

    public bool IsVisible(PlayerController player, bool forMinimap, bool forWorldMap, out string failureMessage, bool includeHiddenByPreference = false) {
        if(forMinimap && !ShowOnMinimap()) {
            failureMessage = "Marker is disabled for minimap.";
            return false;
        }

        if(forWorldMap && !ShowOnWorldMap()) {
            failureMessage = "Marker is disabled for world map.";
            return false;
        }

        var mapLog = player != null ? player.GetComponent<PlayerMapLog>() : null;
        if(mapLog != null && mapLog.IsMarkerHidden(MarkerId) && !includeHiddenByPreference) {
            failureMessage = "Marker is hidden by player preference.";
            return false;
        }

        if(markerDefinition != null) {
            return markerDefinition.CanShow(player, mapLog, out failureMessage);
        }

        failureMessage = null;
        return true;
    }

    public MapMarkerRecord BuildRecord(PlayerController player) {
        var mapLog = player != null ? player.GetComponent<PlayerMapLog>() : null;
        return new MapMarkerRecord {
            id = MarkerId,
            displayName = ResolveDisplayName(),
            description = markerDefinition != null ? markerDefinition.Description : fallbackDescription,
            category = markerDefinition != null ? markerDefinition.Category : fallbackCategory,
            worldPosition = ResolvePosition(),
            sceneName = gameObject.scene.IsValid() ? gameObject.scene.name : SceneManager.GetActiveScene().name,
            icon = markerDefinition != null ? markerDefinition.Icon : fallbackIcon,
            color = markerDefinition != null ? markerDefinition.Color : fallbackColor,
            priority = markerDefinition != null ? markerDefinition.Priority : fallbackPriority,
            showOnMinimap = ShowOnMinimap(),
            showOnWorldMap = ShowOnWorldMap(),
            important = markerDefinition != null && markerDefinition.Important,
            discovered = mapLog != null && mapLog.HasDiscoveredMarker(MarkerId),
            hidden = mapLog != null && mapLog.IsMarkerHidden(MarkerId),
            favorite = mapLog != null && mapLog.IsMarkerFavorite(MarkerId),
            regionId = markerDefinition != null && markerDefinition.Region != null ? markerDefinition.Region.Id : string.Empty,
            pokeNavEntryId = markerDefinition != null && markerDefinition.PokeNavEntry != null ? markerDefinition.PokeNavEntry.Id : string.Empty,
            socialPostId = markerDefinition != null && markerDefinition.SocialPost != null ? markerDefinition.SocialPost.Id : string.Empty,
            pokemonId = markerDefinition != null && markerDefinition.RelatedPokemon != null ? markerDefinition.RelatedPokemon.name : string.Empty,
            source = GetType().Name,
            tags = markerDefinition != null && markerDefinition.Tags != null ? new List<string>(markerDefinition.Tags) : new List<string>()
        };
    }

    Vector3 ResolvePosition() {
        var source = positionOverride != null ? positionOverride : transform;
        return source.position + positionOffset;
    }

    string ResolveDisplayName() {
        if(markerDefinition != null) {
            return markerDefinition.DisplayName;
        }

        if(!string.IsNullOrWhiteSpace(fallbackDisplayName)) {
            return fallbackDisplayName;
        }

        var shop = GetComponent<ShopCatalog>();
        if(shop != null && shop.Catalog != null) {
            return shop.Catalog.DisplayName;
        }

        var transit = GetComponent<TransitStation>();
        if(transit != null && transit.StopDefinition != null) {
            return transit.StopDefinition.DisplayName;
        }

        var board = GetComponent<JobBoard>();
        if(board != null && board.BoardDefinition != null) {
            return board.BoardDefinition.DisplayName;
        }

        var activityZone = GetComponent<ActivityZone>();
        if(activityZone != null && activityZone.Definition != null) {
            return activityZone.Definition.DisplayName;
        }

        return name;
    }

    bool ShowOnMinimap() {
        return markerDefinition != null ? markerDefinition.ShowOnMinimap : fallbackShowOnMinimap;
    }

    bool ShowOnWorldMap() {
        return markerDefinition != null ? markerDefinition.ShowOnWorldMap : fallbackShowOnWorldMap;
    }
}
