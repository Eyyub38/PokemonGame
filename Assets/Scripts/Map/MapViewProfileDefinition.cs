using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum MapViewMode {
    Minimap,
    WorldMap,
    RegionMap,
    PokeNavMap,
    Compass,
    Custom
}

public enum MapMarkerSortMode {
    FavoriteImportantPriorityName,
    DistanceThenPriority,
    PriorityThenName,
    Name,
    CategoryThenName,
    DiscoveryThenName
}

public enum MapMarkerFilterMatchMode {
    All,
    Any
}

[CreateAssetMenu(menuName = "Map/View Profile Definition")]
public class MapViewProfileDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable id for this map view profile. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in future map/minimap UI. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note explaining where this view profile should be used.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Free-form tags such as minimap, pokenav, town, route, event or debug.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Scope")]
    [Tooltip("Main view mode. This decides the default minimap/world-map visibility checks.")]
    [SerializeField] MapViewMode mode = MapViewMode.WorldMap;
    [Tooltip("If enabled, markers hidden by the player's preference are still returned.")]
    [SerializeField] bool includeHiddenByPreference;
    [Tooltip("If enabled, only markers the player has discovered are returned.")]
    [SerializeField] bool requireDiscovered;
    [Tooltip("If enabled, only player-favorited markers are returned.")]
    [SerializeField] bool requireFavorite;
    [Tooltip("If enabled, only markers flagged as important are returned.")]
    [SerializeField] bool requireImportant;
    [Tooltip("If enabled, returned records must be eligible for minimap display.")]
    [SerializeField] bool requireMinimapEligible;
    [Tooltip("If enabled, returned records must be eligible for full world map display.")]
    [SerializeField] bool requireWorldMapEligible;
    [Tooltip("Maximum markers returned after filtering and sorting. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxMarkers;

    [Header("Distance")]
    [Tooltip("If enabled, markers farther than Max Distance are filtered out. Player position is used when no origin is passed.")]
    [SerializeField] bool useMaxDistance;
    [Tooltip("Maximum allowed distance from the provided origin or player position.")]
    [Min(0f)]
    [SerializeField] float maxDistance = 30f;

    [Header("Category Filters")]
    [Tooltip("If any categories are listed, only those categories can pass.")]
    [SerializeField] List<MapMarkerCategory> allowedCategories = new List<MapMarkerCategory>();
    [Tooltip("Categories that are always filtered out.")]
    [SerializeField] List<MapMarkerCategory> blockedCategories = new List<MapMarkerCategory>();

    [Header("Tag Filters")]
    [Tooltip("Tags that must match before a marker is returned.")]
    [SerializeField] List<string> requiredTags = new List<string>();
    [Tooltip("Required tag matching mode. All requires every required tag; Any requires at least one.")]
    [SerializeField] MapMarkerFilterMatchMode requiredTagMatchMode = MapMarkerFilterMatchMode.All;
    [Tooltip("Markers with any of these tags are filtered out.")]
    [SerializeField] List<string> blockedTags = new List<string>();

    [Header("Location Filters")]
    [Tooltip("Optional region filter. Empty accepts any region.")]
    [SerializeField] RegionInfoDefinition region;
    [Tooltip("Optional exact scene name filter. Empty accepts any scene.")]
    [SerializeField] string sceneName = string.Empty;

    [Header("Sorting")]
    [Tooltip("How matching markers are sorted for the map UI.")]
    [SerializeField] MapMarkerSortMode sortMode = MapMarkerSortMode.FavoriteImportantPriorityName;
    [Tooltip("If enabled, the final sorted result is reversed.")]
    [SerializeField] bool reverseSort;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public MapViewMode Mode => mode;
    public bool IncludeHiddenByPreference => includeHiddenByPreference;
    public bool RequireDiscovered => requireDiscovered;
    public bool RequireFavorite => requireFavorite;
    public bool RequireImportant => requireImportant;
    public bool RequireMinimapEligible => requireMinimapEligible;
    public bool RequireWorldMapEligible => requireWorldMapEligible;
    public int MaxMarkers => Mathf.Max(0, maxMarkers);
    public bool UseMaxDistance => useMaxDistance;
    public float MaxDistance => Mathf.Max(0f, maxDistance);
    public IReadOnlyList<MapMarkerCategory> AllowedCategories => allowedCategories != null ? (IReadOnlyList<MapMarkerCategory>)allowedCategories : Array.Empty<MapMarkerCategory>();
    public IReadOnlyList<MapMarkerCategory> BlockedCategories => blockedCategories != null ? (IReadOnlyList<MapMarkerCategory>)blockedCategories : Array.Empty<MapMarkerCategory>();
    public IReadOnlyList<string> RequiredTags => requiredTags != null ? (IReadOnlyList<string>)requiredTags : Array.Empty<string>();
    public MapMarkerFilterMatchMode RequiredTagMatchMode => requiredTagMatchMode;
    public IReadOnlyList<string> BlockedTags => blockedTags != null ? (IReadOnlyList<string>)blockedTags : Array.Empty<string>();
    public RegionInfoDefinition Region => region;
    public string SceneName => sceneName;
    public MapMarkerSortMode SortMode => sortMode;
    public bool ReverseSort => reverseSort;

    public IReadOnlyList<MapMarkerRecord> GetVisibleMarkers(PlayerController player) {
        return GetVisibleMarkers(player, ResolveDefaultOrigin(player));
    }

    public IReadOnlyList<MapMarkerRecord> GetVisibleMarkers(PlayerController player, Transform origin) {
        return GetVisibleMarkers(player, origin != null ? origin.position : ResolveDefaultOrigin(player));
    }

    public IReadOnlyList<MapMarkerRecord> GetVisibleMarkers(PlayerController player, Vector3? origin) {
        var registry = MapMarkerRegistry.Ensure();
        bool forMinimap = mode == MapViewMode.Minimap || mode == MapViewMode.Compass || (mode == MapViewMode.Custom && requireMinimapEligible);
        bool forWorldMap = mode == MapViewMode.WorldMap || mode == MapViewMode.RegionMap || mode == MapViewMode.PokeNavMap || (mode == MapViewMode.Custom && requireWorldMapEligible);

        var markers = registry.GetVisibleMarkers(player, forMinimap, forWorldMap, includeHiddenByPreference)
            .Where(marker => Matches(marker, player, origin, out _));

        markers = SortMarkers(markers, origin);

        if(reverseSort) {
            markers = markers.Reverse();
        }

        if(MaxMarkers > 0) {
            markers = markers.Take(MaxMarkers);
        }

        return markers.ToList();
    }

    public bool Matches(MapMarkerRecord marker, PlayerController player, out string failureMessage) {
        return Matches(marker, player, ResolveDefaultOrigin(player), out failureMessage);
    }

    public bool Matches(MapMarkerRecord marker, PlayerController player, Vector3? origin, out string failureMessage) {
        if(marker == null) {
            failureMessage = "Marker record is null.";
            return false;
        }

        if(requireDiscovered && !marker.discovered) {
            failureMessage = "Marker is not discovered.";
            return false;
        }

        if(requireFavorite && !marker.favorite) {
            failureMessage = "Marker is not favorited.";
            return false;
        }

        if(requireImportant && !marker.important) {
            failureMessage = "Marker is not important.";
            return false;
        }

        if(requireMinimapEligible && !marker.showOnMinimap) {
            failureMessage = "Marker is not eligible for minimap.";
            return false;
        }

        if(requireWorldMapEligible && !marker.showOnWorldMap) {
            failureMessage = "Marker is not eligible for world map.";
            return false;
        }

        if(allowedCategories != null && allowedCategories.Count > 0 && !allowedCategories.Contains(marker.category)) {
            failureMessage = "Marker category is not allowed.";
            return false;
        }

        if(blockedCategories != null && blockedCategories.Contains(marker.category)) {
            failureMessage = "Marker category is blocked.";
            return false;
        }

        if(region != null && !string.Equals(marker.regionId, region.Id, StringComparison.OrdinalIgnoreCase)) {
            failureMessage = "Marker is in a different region.";
            return false;
        }

        if(!string.IsNullOrWhiteSpace(sceneName) && !string.Equals(marker.sceneName, sceneName, StringComparison.OrdinalIgnoreCase)) {
            failureMessage = "Marker is in a different scene.";
            return false;
        }

        if(!PassesRequiredTags(marker)) {
            failureMessage = "Marker does not match required tags.";
            return false;
        }

        if(blockedTags != null && blockedTags.Any(tag => HasMarkerTag(marker, tag))) {
            failureMessage = "Marker has a blocked tag.";
            return false;
        }

        if(useMaxDistance) {
            if(!origin.HasValue) {
                failureMessage = "No origin was available for distance filtering.";
                return false;
            }

            if(Vector3.Distance(origin.Value, marker.worldPosition) > MaxDistance) {
                failureMessage = "Marker is farther than max distance.";
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    IEnumerable<MapMarkerRecord> SortMarkers(IEnumerable<MapMarkerRecord> markers, Vector3? origin) {
        return sortMode switch {
            MapMarkerSortMode.DistanceThenPriority when origin.HasValue => markers
                .OrderBy(marker => Vector3.SqrMagnitude(marker.worldPosition - origin.Value))
                .ThenByDescending(marker => marker.priority)
                .ThenBy(marker => marker.displayName),
            MapMarkerSortMode.PriorityThenName => markers
                .OrderByDescending(marker => marker.priority)
                .ThenBy(marker => marker.displayName),
            MapMarkerSortMode.Name => markers
                .OrderBy(marker => marker.displayName),
            MapMarkerSortMode.CategoryThenName => markers
                .OrderBy(marker => marker.category)
                .ThenBy(marker => marker.displayName),
            MapMarkerSortMode.DiscoveryThenName => markers
                .OrderByDescending(marker => marker.discovered)
                .ThenBy(marker => marker.displayName),
            _ => markers
                .OrderByDescending(marker => marker.favorite)
                .ThenByDescending(marker => marker.important)
                .ThenByDescending(marker => marker.priority)
                .ThenBy(marker => marker.displayName)
        };
    }

    bool PassesRequiredTags(MapMarkerRecord marker) {
        if(requiredTags == null || requiredTags.Count == 0) {
            return true;
        }

        var validTags = requiredTags.Where(tag => !string.IsNullOrWhiteSpace(tag)).ToList();
        if(validTags.Count == 0) {
            return true;
        }

        return requiredTagMatchMode == MapMarkerFilterMatchMode.Any
            ? validTags.Any(tag => HasMarkerTag(marker, tag))
            : validTags.All(tag => HasMarkerTag(marker, tag));
    }

    static bool HasMarkerTag(MapMarkerRecord marker, string tag) {
        return marker != null
            && !string.IsNullOrWhiteSpace(tag)
            && marker.tags != null
            && marker.tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    static Vector3? ResolveDefaultOrigin(PlayerController player) {
        return player != null ? player.transform.position : (Vector3?)null;
    }
}
