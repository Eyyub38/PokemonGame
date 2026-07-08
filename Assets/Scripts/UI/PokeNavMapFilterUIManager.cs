using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum PokeNavMapFilterActionKind {
    None,
    Refreshed,
    ProfileSelected,
    CategoryChanged,
    TagChanged,
    RegionChanged,
    SceneChanged,
    SearchChanged,
    FlagsChanged,
    Cleared,
    Blocked
}

public class PokeNavMapFilterUIManager : MonoBehaviour {
    [Header("Player")]
    [Tooltip("Player whose map state is shown. Empty uses PlayerController.i or the first PlayerController in the scene.")]
    [SerializeField] PlayerController playerOverride;

    [Header("Profiles")]
    [Tooltip("Default map profile used before the player selects a preset.")]
    [SerializeField] MapViewProfileDefinition defaultProfile;
    [Tooltip("Map view presets exposed as filter tabs/buttons.")]
    [SerializeField] List<MapViewProfileDefinition> profilePresets = new List<MapViewProfileDefinition>();
    [Tooltip("If enabled, all MapViewProfileDefinition assets in Resources are exposed after explicit presets.")]
    [SerializeField] bool includeResourceProfiles = true;

    [Header("Active Filters")]
    [Tooltip("If enabled, only discovered markers are returned.")]
    [SerializeField] bool onlyDiscovered;
    [Tooltip("If enabled, only favorited markers are returned.")]
    [SerializeField] bool onlyFavorites;
    [Tooltip("If enabled, only important markers are returned.")]
    [SerializeField] bool onlyImportant;
    [Tooltip("If enabled, markers hidden by the player can still appear in filtered results.")]
    [SerializeField] bool includeHiddenMarkers;
    [Tooltip("If enabled, markers must be eligible for minimap display.")]
    [SerializeField] bool requireMinimapEligible;
    [Tooltip("If enabled, markers must be eligible for world map display.")]
    [SerializeField] bool requireWorldMapEligible = true;
    [Tooltip("Optional active category filter. Empty means all categories.")]
    [SerializeField] List<MapMarkerCategory> activeCategories = new List<MapMarkerCategory>();
    [Tooltip("Optional active tag filter. Empty means all tags.")]
    [SerializeField] List<string> activeTags = new List<string>();
    [Tooltip("Required tag matching mode when Active Tags has entries.")]
    [SerializeField] MapMarkerFilterMatchMode activeTagMatchMode = MapMarkerFilterMatchMode.Any;
    [Tooltip("Optional active region id filter. Empty means all regions.")]
    [SerializeField] string activeRegionId = string.Empty;
    [Tooltip("Optional active scene name filter. Empty means all scenes.")]
    [SerializeField] string activeSceneName = string.Empty;
    [Tooltip("Case-insensitive search text matched against marker name, description, scene, region and tags.")]
    [SerializeField] string searchText = string.Empty;

    [Header("Distance")]
    [Tooltip("Optional origin used by distance-aware rows. Empty uses the player transform.")]
    [SerializeField] Transform distanceOriginOverride;
    [Tooltip("If enabled, markers farther than Max Distance are filtered out.")]
    [SerializeField] bool useMaxDistance;
    [Tooltip("Maximum marker distance from origin when Use Max Distance is enabled.")]
    [Min(0f)]
    [SerializeField] float maxDistance = 50f;

    [Header("Snapshot")]
    [Tooltip("If enabled, Refresh is called when this component starts.")]
    [SerializeField] bool refreshOnStart = true;
    [Tooltip("If enabled, Refresh is called after every filter action.")]
    [SerializeField] bool refreshAfterActions = true;
    [Tooltip("Maximum marker rows copied to the snapshot. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxMarkerRows = 80;

    [Header("Debug")]
    [Tooltip("If enabled, successful filter actions are written to GameDebug.")]
    [SerializeField] bool logSuccessfulActions;
    [Tooltip("If enabled, blocked filter actions are written to GameDebug.")]
    [SerializeField] bool logBlockedActions = true;

    MapViewProfileDefinition selectedProfile;
    PokeNavMapFilterSnapshot currentSnapshot = new PokeNavMapFilterSnapshot();
    PokeNavMapFilterActionResult lastResult = new PokeNavMapFilterActionResult();

    public PokeNavMapFilterSnapshot CurrentSnapshot => currentSnapshot;
    public PokeNavMapFilterActionResult LastResult => lastResult;
    public MapViewProfileDefinition DefaultProfile => defaultProfile;
    public MapViewProfileDefinition SelectedProfile => selectedProfile != null ? selectedProfile : defaultProfile;
    public IReadOnlyList<MapViewProfileDefinition> ProfilePresets => profilePresets;
    public bool IncludeResourceProfiles => includeResourceProfiles;
    public IReadOnlyList<MapMarkerCategory> ActiveCategories => activeCategories;
    public IReadOnlyList<string> ActiveTags => activeTags;
    public string ActiveRegionId => activeRegionId;
    public string ActiveSceneName => activeSceneName;
    public string SearchText => searchText;
    public int MaxMarkerRows => Mathf.Max(0, maxMarkerRows);
    public event Action<PokeNavMapFilterSnapshot> OnSnapshotChanged;
    public event Action<PokeNavMapFilterActionResult> OnActionResult;

    void Start() {
        selectedProfile = defaultProfile;
        if(refreshOnStart) {
            Refresh();
        }
    }

    [ContextMenu("Refresh PokeNav Map Filter Snapshot")]
    public PokeNavMapFilterSnapshot RefreshFromContextMenu() {
        return Refresh();
    }

    public PokeNavMapFilterSnapshot Refresh() {
        var player = ResolvePlayer();
        var origin = ResolveOrigin(player);
        var navigationLog = player != null ? player.GetComponent<PlayerMapNavigationLog>() : null;
        var allMarkers = ResolveBaseMarkers(player, origin).Where(marker => marker != null).ToList();
        var filtered = ApplyRuntimeFilters(allMarkers, origin).ToList();

        currentSnapshot = new PokeNavMapFilterSnapshot {
            hasPlayer = player != null,
            playerName = player != null ? player.name : string.Empty,
            selectedProfileId = SelectedProfile != null ? SelectedProfile.Id : string.Empty,
            selectedProfileName = SelectedProfile != null ? SelectedProfile.DisplayName : "Runtime Map",
            onlyDiscovered = onlyDiscovered,
            onlyFavorites = onlyFavorites,
            onlyImportant = onlyImportant,
            includeHiddenMarkers = includeHiddenMarkers,
            requireMinimapEligible = requireMinimapEligible,
            requireWorldMapEligible = requireWorldMapEligible,
            activeRegionId = activeRegionId,
            activeSceneName = activeSceneName,
            searchText = searchText,
            totalMarkerCount = allMarkers.Count,
            filteredMarkerCount = filtered.Count,
            favoriteCount = filtered.Count(marker => marker.favorite),
            importantCount = filtered.Count(marker => marker.important),
            hiddenCount = filtered.Count(marker => marker.hidden),
            categoryRows = BuildCategoryRows(allMarkers, filtered),
            tagRows = BuildTagRows(allMarkers, filtered),
            regionRows = BuildRegionRows(allMarkers, filtered),
            sceneRows = BuildSceneRows(allMarkers, filtered),
            profileRows = BuildProfileRows(),
            markerRows = Limit(filtered.Select(marker => PokeNavMapMarkerRow.FromRecord(marker, navigationLog, origin))).ToList(),
            lastResult = lastResult
        };

        OnSnapshotChanged?.Invoke(currentSnapshot);
        return currentSnapshot;
    }

    public bool SelectProfile(string profileId, out string feedback) {
        var profile = ResolveProfiles().FirstOrDefault(item => item != null && string.Equals(item.Id, profileId, StringComparison.OrdinalIgnoreCase));
        if(profile == null && !string.IsNullOrWhiteSpace(profileId)) {
            return Block($"Map filter profile '{profileId}' could not be found.", out feedback);
        }

        selectedProfile = profile;
        bool success = Succeed(PokeNavMapFilterActionKind.ProfileSelected, profile != null ? $"{profile.DisplayName} selected." : "Runtime map profile selected.", out feedback);
        RefreshIfNeeded();
        return success;
    }

    public bool SetSearchText(string value, out string feedback) {
        searchText = value ?? string.Empty;
        bool success = Succeed(PokeNavMapFilterActionKind.SearchChanged, "Map marker search changed.", out feedback);
        RefreshIfNeeded();
        return success;
    }

    public bool SetOnlyDiscovered(bool value, out string feedback) {
        onlyDiscovered = value;
        return SetFlagsChanged(out feedback);
    }

    public bool SetOnlyFavorites(bool value, out string feedback) {
        onlyFavorites = value;
        return SetFlagsChanged(out feedback);
    }

    public bool SetOnlyImportant(bool value, out string feedback) {
        onlyImportant = value;
        return SetFlagsChanged(out feedback);
    }

    public bool SetIncludeHiddenMarkers(bool value, out string feedback) {
        includeHiddenMarkers = value;
        return SetFlagsChanged(out feedback);
    }

    public bool ToggleCategory(MapMarkerCategory category, out string feedback) {
        if(activeCategories.Contains(category)) {
            activeCategories.Remove(category);
        } else {
            activeCategories.Add(category);
        }

        bool success = Succeed(PokeNavMapFilterActionKind.CategoryChanged, $"{category} category filter changed.", out feedback);
        RefreshIfNeeded();
        return success;
    }

    public bool ClearCategories(out string feedback) {
        activeCategories.Clear();
        bool success = Succeed(PokeNavMapFilterActionKind.CategoryChanged, "Category filters cleared.", out feedback);
        RefreshIfNeeded();
        return success;
    }

    public bool ToggleTag(string tag, out string feedback) {
        if(string.IsNullOrWhiteSpace(tag)) {
            return Block("No tag was selected.", out feedback);
        }

        int index = activeTags.FindIndex(item => string.Equals(item, tag, StringComparison.OrdinalIgnoreCase));
        if(index >= 0) {
            activeTags.RemoveAt(index);
        } else {
            activeTags.Add(tag);
        }

        bool success = Succeed(PokeNavMapFilterActionKind.TagChanged, $"Tag filter '{tag}' changed.", out feedback);
        RefreshIfNeeded();
        return success;
    }

    public bool SetRegionFilter(string regionId, out string feedback) {
        activeRegionId = regionId ?? string.Empty;
        bool success = Succeed(PokeNavMapFilterActionKind.RegionChanged, string.IsNullOrWhiteSpace(activeRegionId) ? "Region filter cleared." : $"Region filter set to {activeRegionId}.", out feedback);
        RefreshIfNeeded();
        return success;
    }

    public bool SetSceneFilter(string sceneName, out string feedback) {
        activeSceneName = sceneName ?? string.Empty;
        bool success = Succeed(PokeNavMapFilterActionKind.SceneChanged, string.IsNullOrWhiteSpace(activeSceneName) ? "Scene filter cleared." : $"Scene filter set to {activeSceneName}.", out feedback);
        RefreshIfNeeded();
        return success;
    }

    public bool ClearFilters(out string feedback) {
        activeCategories.Clear();
        activeTags.Clear();
        activeRegionId = string.Empty;
        activeSceneName = string.Empty;
        searchText = string.Empty;
        onlyDiscovered = false;
        onlyFavorites = false;
        onlyImportant = false;
        useMaxDistance = false;
        bool success = Succeed(PokeNavMapFilterActionKind.Cleared, "Map marker filters cleared.", out feedback);
        RefreshIfNeeded();
        return success;
    }

    bool SetFlagsChanged(out string feedback) {
        bool success = Succeed(PokeNavMapFilterActionKind.FlagsChanged, "Map marker filter flags changed.", out feedback);
        RefreshIfNeeded();
        return success;
    }

    IReadOnlyList<MapMarkerRecord> ResolveBaseMarkers(PlayerController player, Vector3? origin) {
        if(SelectedProfile != null) {
            return SelectedProfile.GetVisibleMarkers(player, origin);
        }

        if(MapMarkerRegistry.i == null) {
            return Array.Empty<MapMarkerRecord>();
        }

        return MapMarkerRegistry.i.GetVisibleMarkers(player, requireMinimapEligible, requireWorldMapEligible, includeHiddenMarkers);
    }

    IEnumerable<MapMarkerRecord> ApplyRuntimeFilters(IEnumerable<MapMarkerRecord> markers, Vector3? origin) {
        var filtered = markers ?? Enumerable.Empty<MapMarkerRecord>();
        if(!includeHiddenMarkers) {
            filtered = filtered.Where(marker => !marker.hidden);
        }

        if(onlyDiscovered) {
            filtered = filtered.Where(marker => marker.discovered);
        }

        if(onlyFavorites) {
            filtered = filtered.Where(marker => marker.favorite);
        }

        if(onlyImportant) {
            filtered = filtered.Where(marker => marker.important);
        }

        if(requireMinimapEligible) {
            filtered = filtered.Where(marker => marker.showOnMinimap);
        }

        if(requireWorldMapEligible) {
            filtered = filtered.Where(marker => marker.showOnWorldMap);
        }

        if(activeCategories != null && activeCategories.Count > 0) {
            filtered = filtered.Where(marker => activeCategories.Contains(marker.category));
        }

        if(!string.IsNullOrWhiteSpace(activeRegionId)) {
            filtered = filtered.Where(marker => string.Equals(marker.regionId, activeRegionId, StringComparison.OrdinalIgnoreCase));
        }

        if(!string.IsNullOrWhiteSpace(activeSceneName)) {
            filtered = filtered.Where(marker => string.Equals(marker.sceneName, activeSceneName, StringComparison.OrdinalIgnoreCase));
        }

        if(activeTags != null && activeTags.Any(tag => !string.IsNullOrWhiteSpace(tag))) {
            var tags = activeTags.Where(tag => !string.IsNullOrWhiteSpace(tag)).ToList();
            filtered = activeTagMatchMode == MapMarkerFilterMatchMode.All
                ? filtered.Where(marker => tags.All(tag => HasTag(marker, tag)))
                : filtered.Where(marker => tags.Any(tag => HasTag(marker, tag)));
        }

        if(!string.IsNullOrWhiteSpace(searchText)) {
            filtered = filtered.Where(MatchesSearch);
        }

        if(useMaxDistance && origin.HasValue) {
            filtered = filtered.Where(marker => Vector3.Distance(origin.Value, marker.worldPosition) <= Mathf.Max(0f, maxDistance));
        }

        return filtered
            .OrderByDescending(marker => marker.favorite)
            .ThenByDescending(marker => marker.important)
            .ThenByDescending(marker => marker.priority)
            .ThenBy(marker => marker.displayName);
    }

    List<PokeNavMapFilterCategoryRow> BuildCategoryRows(List<MapMarkerRecord> allMarkers, List<MapMarkerRecord> filteredMarkers) {
        return Enum.GetValues(typeof(MapMarkerCategory))
            .Cast<MapMarkerCategory>()
            .Select(category => new PokeNavMapFilterCategoryRow {
                category = category,
                selected = activeCategories != null && activeCategories.Contains(category),
                totalCount = allMarkers.Count(marker => marker.category == category),
                filteredCount = filteredMarkers.Count(marker => marker.category == category),
                displayText = $"{category} ({filteredMarkers.Count(marker => marker.category == category)})"
            })
            .Where(row => row.totalCount > 0 || row.selected)
            .ToList();
    }

    List<PokeNavMapFilterTagRow> BuildTagRows(List<MapMarkerRecord> allMarkers, List<MapMarkerRecord> filteredMarkers) {
        var tags = allMarkers
            .Where(marker => marker.tags != null)
            .SelectMany(marker => marker.tags)
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(tag => tag);

        return tags.Select(tag => new PokeNavMapFilterTagRow {
            tag = tag,
            selected = activeTags != null && activeTags.Any(item => string.Equals(item, tag, StringComparison.OrdinalIgnoreCase)),
            totalCount = allMarkers.Count(marker => HasTag(marker, tag)),
            filteredCount = filteredMarkers.Count(marker => HasTag(marker, tag)),
            displayText = $"{tag} ({filteredMarkers.Count(marker => HasTag(marker, tag))})"
        }).ToList();
    }

    List<PokeNavMapFilterRegionRow> BuildRegionRows(List<MapMarkerRecord> allMarkers, List<MapMarkerRecord> filteredMarkers) {
        var ids = allMarkers
            .Select(marker => marker.regionId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(id => id);

        return ids.Select(id => {
            var region = FindResourceById<RegionInfoDefinition>(id, item => item.Id);
            return new PokeNavMapFilterRegionRow {
                regionId = id,
                displayName = region != null ? region.DisplayName : id,
                selected = string.Equals(activeRegionId, id, StringComparison.OrdinalIgnoreCase),
                totalCount = allMarkers.Count(marker => string.Equals(marker.regionId, id, StringComparison.OrdinalIgnoreCase)),
                filteredCount = filteredMarkers.Count(marker => string.Equals(marker.regionId, id, StringComparison.OrdinalIgnoreCase)),
                displayText = $"{(region != null ? region.DisplayName : id)} ({filteredMarkers.Count(marker => string.Equals(marker.regionId, id, StringComparison.OrdinalIgnoreCase))})"
            };
        }).ToList();
    }

    List<PokeNavMapFilterSceneRow> BuildSceneRows(List<MapMarkerRecord> allMarkers, List<MapMarkerRecord> filteredMarkers) {
        return allMarkers
            .Select(marker => marker.sceneName)
            .Where(scene => !string.IsNullOrWhiteSpace(scene))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(scene => scene)
            .Select(scene => new PokeNavMapFilterSceneRow {
                sceneName = scene,
                selected = string.Equals(activeSceneName, scene, StringComparison.OrdinalIgnoreCase),
                totalCount = allMarkers.Count(marker => string.Equals(marker.sceneName, scene, StringComparison.OrdinalIgnoreCase)),
                filteredCount = filteredMarkers.Count(marker => string.Equals(marker.sceneName, scene, StringComparison.OrdinalIgnoreCase)),
                displayText = $"{scene} ({filteredMarkers.Count(marker => string.Equals(marker.sceneName, scene, StringComparison.OrdinalIgnoreCase))})"
            }).ToList();
    }

    List<PokeNavMapFilterProfileRow> BuildProfileRows() {
        return ResolveProfiles().Select(profile => new PokeNavMapFilterProfileRow {
            profileId = profile != null ? profile.Id : string.Empty,
            displayName = profile != null ? profile.DisplayName : "Runtime Map",
            description = profile != null ? profile.Description : string.Empty,
            mode = profile != null ? profile.Mode : MapViewMode.Custom,
            selected = profile == SelectedProfile,
            displayText = profile != null ? $"{profile.DisplayName} [{profile.Mode}]" : "Runtime Map"
        }).ToList();
    }

    IEnumerable<MapViewProfileDefinition> ResolveProfiles() {
        var profiles = new List<MapViewProfileDefinition>();
        if(defaultProfile != null) {
            profiles.Add(defaultProfile);
        }

        if(profilePresets != null) {
            profiles.AddRange(profilePresets.Where(profile => profile != null));
        }

        if(includeResourceProfiles) {
            profiles.AddRange(Resources.LoadAll<MapViewProfileDefinition>("").Where(profile => profile != null));
        }

        return profiles.Distinct().OrderBy(profile => profile.DisplayName);
    }

    IEnumerable<T> Limit<T>(IEnumerable<T> source) {
        if(source == null) {
            return Enumerable.Empty<T>();
        }

        return MaxMarkerRows > 0 ? source.Take(MaxMarkerRows) : source;
    }

    bool MatchesSearch(MapMarkerRecord marker) {
        if(marker == null || string.IsNullOrWhiteSpace(searchText)) {
            return true;
        }

        string needle = searchText.Trim();
        return Contains(marker.displayName, needle)
            || Contains(marker.description, needle)
            || Contains(marker.sceneName, needle)
            || Contains(marker.regionId, needle)
            || (marker.tags != null && marker.tags.Any(tag => Contains(tag, needle)));
    }

    static bool Contains(string value, string needle) {
        return !string.IsNullOrWhiteSpace(value)
            && !string.IsNullOrWhiteSpace(needle)
            && value.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    static bool HasTag(MapMarkerRecord marker, string tag) {
        return marker != null
            && marker.tags != null
            && !string.IsNullOrWhiteSpace(tag)
            && marker.tags.Any(item => string.Equals(item, tag, StringComparison.OrdinalIgnoreCase));
    }

    T FindResourceById<T>(string id, Func<T, string> getId) where T : UnityEngine.Object {
        if(string.IsNullOrWhiteSpace(id) || getId == null) {
            return null;
        }

        return Resources.LoadAll<T>("").FirstOrDefault(asset => asset != null && string.Equals(getId(asset), id, StringComparison.OrdinalIgnoreCase));
    }

    PlayerController ResolvePlayer() {
        if(playerOverride != null) {
            return playerOverride;
        }

        if(PlayerController.i != null) {
            return PlayerController.i;
        }

        return FindAnyObjectByType<PlayerController>();
    }

    Vector3? ResolveOrigin(PlayerController player) {
        if(distanceOriginOverride != null) {
            return distanceOriginOverride.position;
        }

        return player != null ? player.transform.position : null;
    }

    void RefreshIfNeeded() {
        if(refreshAfterActions) {
            Refresh();
        }
    }

    bool Succeed(PokeNavMapFilterActionKind kind, string message, out string feedback) {
        feedback = message;
        lastResult = BuildResult(kind, true, message);
        if(logSuccessfulActions) {
            GameDebug.Step(message, GameDebugCategory.UI, this, "PokeNavMapFilterUI");
        }

        OnActionResult?.Invoke(lastResult);
        return true;
    }

    bool Block(string message, out string feedback) {
        feedback = message;
        lastResult = BuildResult(PokeNavMapFilterActionKind.Blocked, false, message);
        if(logBlockedActions) {
            GameDebug.Warning(message, GameDebugCategory.UI, this, "PokeNavMapFilterUI");
        }

        OnActionResult?.Invoke(lastResult);
        RefreshIfNeeded();
        return false;
    }

    PokeNavMapFilterActionResult BuildResult(PokeNavMapFilterActionKind kind, bool success, string message) {
        return new PokeNavMapFilterActionResult {
            kind = kind,
            success = success,
            message = message
        };
    }
}

[Serializable]
public class PokeNavMapFilterSnapshot {
    [Tooltip("If enabled, a player was found for this snapshot.")]
    public bool hasPlayer;
    [Tooltip("Player GameObject name used by this snapshot.")]
    public string playerName;
    [Tooltip("Selected map profile id.")]
    public string selectedProfileId;
    [Tooltip("Selected map profile display name.")]
    public string selectedProfileName;
    [Tooltip("If enabled, only discovered markers are shown.")]
    public bool onlyDiscovered;
    [Tooltip("If enabled, only favorited markers are shown.")]
    public bool onlyFavorites;
    [Tooltip("If enabled, only important markers are shown.")]
    public bool onlyImportant;
    [Tooltip("If enabled, hidden markers can appear.")]
    public bool includeHiddenMarkers;
    [Tooltip("If enabled, markers must be minimap eligible.")]
    public bool requireMinimapEligible;
    [Tooltip("If enabled, markers must be world-map eligible.")]
    public bool requireWorldMapEligible;
    [Tooltip("Active region id filter.")]
    public string activeRegionId;
    [Tooltip("Active scene name filter.")]
    public string activeSceneName;
    [Tooltip("Active search text.")]
    public string searchText;
    [Tooltip("Marker count before runtime filters.")]
    public int totalMarkerCount;
    [Tooltip("Marker count after runtime filters.")]
    public int filteredMarkerCount;
    [Tooltip("Favorited marker count in filtered rows.")]
    public int favoriteCount;
    [Tooltip("Important marker count in filtered rows.")]
    public int importantCount;
    [Tooltip("Hidden marker count in filtered rows.")]
    public int hiddenCount;
    [Tooltip("Available category filter rows.")]
    public List<PokeNavMapFilterCategoryRow> categoryRows = new List<PokeNavMapFilterCategoryRow>();
    [Tooltip("Available tag filter rows.")]
    public List<PokeNavMapFilterTagRow> tagRows = new List<PokeNavMapFilterTagRow>();
    [Tooltip("Available region filter rows.")]
    public List<PokeNavMapFilterRegionRow> regionRows = new List<PokeNavMapFilterRegionRow>();
    [Tooltip("Available scene filter rows.")]
    public List<PokeNavMapFilterSceneRow> sceneRows = new List<PokeNavMapFilterSceneRow>();
    [Tooltip("Available profile preset rows.")]
    public List<PokeNavMapFilterProfileRow> profileRows = new List<PokeNavMapFilterProfileRow>();
    [Tooltip("Filtered marker rows.")]
    public List<PokeNavMapMarkerRow> markerRows = new List<PokeNavMapMarkerRow>();
    [Tooltip("Most recent filter backend action result.")]
    public PokeNavMapFilterActionResult lastResult;
}

[Serializable]
public class PokeNavMapFilterActionResult {
    [Tooltip("Kind of filter action that produced this result.")]
    public PokeNavMapFilterActionKind kind;
    [Tooltip("If enabled, the action succeeded.")]
    public bool success;
    [Tooltip("Readable result, failure or feedback text.")]
    public string message;
}

[Serializable]
public class PokeNavMapFilterCategoryRow {
    [Tooltip("Marker category represented by this row.")]
    public MapMarkerCategory category;
    [Tooltip("If enabled, this category is currently selected.")]
    public bool selected;
    [Tooltip("Count before runtime filters.")]
    public int totalCount;
    [Tooltip("Count after runtime filters.")]
    public int filteredCount;
    [Tooltip("Short text useful for placeholder UI.")]
    public string displayText;
}

[Serializable]
public class PokeNavMapFilterTagRow {
    [Tooltip("Marker tag represented by this row.")]
    public string tag;
    [Tooltip("If enabled, this tag is currently selected.")]
    public bool selected;
    [Tooltip("Count before runtime filters.")]
    public int totalCount;
    [Tooltip("Count after runtime filters.")]
    public int filteredCount;
    [Tooltip("Short text useful for placeholder UI.")]
    public string displayText;
}

[Serializable]
public class PokeNavMapFilterRegionRow {
    [Tooltip("Region id represented by this row.")]
    public string regionId;
    [Tooltip("Region display name.")]
    public string displayName;
    [Tooltip("If enabled, this region is currently selected.")]
    public bool selected;
    [Tooltip("Count before runtime filters.")]
    public int totalCount;
    [Tooltip("Count after runtime filters.")]
    public int filteredCount;
    [Tooltip("Short text useful for placeholder UI.")]
    public string displayText;
}

[Serializable]
public class PokeNavMapFilterSceneRow {
    [Tooltip("Scene name represented by this row.")]
    public string sceneName;
    [Tooltip("If enabled, this scene is currently selected.")]
    public bool selected;
    [Tooltip("Count before runtime filters.")]
    public int totalCount;
    [Tooltip("Count after runtime filters.")]
    public int filteredCount;
    [Tooltip("Short text useful for placeholder UI.")]
    public string displayText;
}

[Serializable]
public class PokeNavMapFilterProfileRow {
    [Tooltip("Map view profile id.")]
    public string profileId;
    [Tooltip("Map view profile display name.")]
    public string displayName;
    [Tooltip("Map view profile description.")]
    public string description;
    [Tooltip("Map view mode.")]
    public MapViewMode mode;
    [Tooltip("If enabled, this profile is currently selected.")]
    public bool selected;
    [Tooltip("Short text useful for placeholder UI.")]
    public string displayText;
}
