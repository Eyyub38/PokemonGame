using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum MapViewportMode {
    MinimapFollowPlayer,
    WorldBounds,
    AutoBounds
}

public enum MapViewportProjection {
    XY,
    XZ
}

public enum MapViewportRefreshMode {
    Manual,
    UpdateInterval
}

public class MapViewportUIController : MonoBehaviour {
    [Header("Player")]
    [Tooltip("Player whose position, map log and navigation target drive this viewport. Empty uses PlayerController.i or the first player in the scene.")]
    [SerializeField] PlayerController playerOverride;
    [Tooltip("Optional world transform used as the center for minimap mode. Empty follows the player.")]
    [SerializeField] Transform centerOverride;

    [Header("Data")]
    [Tooltip("Optional map profile used to filter/sort visible markers. Empty reads directly from MapMarkerRegistry.")]
    [SerializeField] MapViewProfileDefinition mapViewProfile;
    [Tooltip("If enabled, markers hidden by player preference can still be drawn.")]
    [SerializeField] bool includeHiddenMarkers;
    [Tooltip("If enabled, only markers from the active scene are shown. Empty marker scene names are allowed.")]
    [SerializeField] bool requireSameScene = true;
    [Tooltip("Maximum marker views drawn at once. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxMarkerViews = 80;

    [Header("Viewport")]
    [Tooltip("How world positions are projected into this UI viewport.")]
    [SerializeField] MapViewportMode viewportMode = MapViewportMode.MinimapFollowPlayer;
    [Tooltip("World plane used by the map. XY is best for this 2D project; XZ is useful for 3D maps.")]
    [SerializeField] MapViewportProjection projection = MapViewportProjection.XY;
    [Tooltip("RectTransform that receives marker instances. Empty uses this object's RectTransform.")]
    [SerializeField] RectTransform markerRoot;
    [Tooltip("Optional prefab for marker entries. If empty, a simple Image/Text marker is created at runtime.")]
    [SerializeField] MapViewportMarkerView markerPrefab;
    [Tooltip("Optional dedicated UI marker for the player position.")]
    [SerializeField] RectTransform playerMarker;
    [Tooltip("Optional dedicated UI marker for the active navigation target.")]
    [SerializeField] RectTransform navigationTargetMarker;

    [Header("Minimap")]
    [Tooltip("World-unit radius covered by the minimap before zoom is applied.")]
    [Min(0.1f)]
    [SerializeField] float minimapWorldRadius = 14f;
    [Tooltip("Current zoom multiplier. Higher values show a smaller world area.")]
    [Min(0.1f)]
    [SerializeField] float zoom = 1f;
    [Tooltip("Minimum zoom accepted by SetZoom and ZoomOut.")]
    [Min(0.1f)]
    [SerializeField] float minZoom = 0.5f;
    [Tooltip("Maximum zoom accepted by SetZoom and ZoomIn.")]
    [Min(0.1f)]
    [SerializeField] float maxZoom = 3f;
    [Tooltip("Padding kept inside the viewport edge before clamped markers are placed.")]
    [Min(0f)]
    [SerializeField] float viewportPadding = 8f;
    [Tooltip("If enabled, out-of-range minimap markers are pinned to the viewport edge.")]
    [SerializeField] bool clampOutOfRangeMarkers = true;
    [Tooltip("If enabled and clamping is disabled, out-of-range minimap markers are hidden.")]
    [SerializeField] bool hideOutOfRangeMarkers = true;
    [Tooltip("If enabled, marker positions are rotated around the player by the player's Z rotation.")]
    [SerializeField] bool rotateWithPlayer;

    [Header("World Bounds")]
    [Tooltip("Center of the world map rectangle used by World Bounds mode.")]
    [SerializeField] Vector2 worldBoundsCenter = Vector2.zero;
    [Tooltip("Size of the world map rectangle used by World Bounds mode.")]
    [SerializeField] Vector2 worldBoundsSize = new Vector2(80f, 60f);
    [Tooltip("Extra world-space padding added around markers in Auto Bounds mode.")]
    [Min(0f)]
    [SerializeField] float autoBoundsPadding = 8f;
    [Tooltip("Smallest Auto Bounds rectangle allowed after fitting visible markers.")]
    [SerializeField] Vector2 minimumAutoBoundsSize = new Vector2(24f, 16f);

    [Header("Text")]
    [Tooltip("Optional label updated with the viewport title.")]
    [SerializeField] Text titleText;
    [Tooltip("Optional label updated with the current scene/profile name.")]
    [SerializeField] Text subtitleText;
    [Tooltip("Optional label updated with the active navigation target.")]
    [SerializeField] Text targetText;
    [Tooltip("Optional label updated with the current zoom.")]
    [SerializeField] Text zoomText;
    [Tooltip("Optional label updated with nearby/visible marker names.")]
    [SerializeField] Text nearbyText;

    [Header("Runtime")]
    [Tooltip("How this viewport refreshes after startup.")]
    [SerializeField] MapViewportRefreshMode refreshMode = MapViewportRefreshMode.UpdateInterval;
    [Tooltip("Seconds between automatic refreshes when Refresh Mode is Update Interval.")]
    [Min(0.02f)]
    [SerializeField] float refreshInterval = 0.15f;
    [Tooltip("If enabled, Refresh is called when this component becomes active.")]
    [SerializeField] bool refreshOnEnable = true;
    [Tooltip("If enabled, marker labels are shown when marker views have a Text reference.")]
    [SerializeField] bool showMarkerLabels;
    [Tooltip("Default marker size used when a marker prefab is not assigned.")]
    [SerializeField] Vector2 defaultMarkerSize = new Vector2(22f, 22f);
    [Tooltip("Font size used by runtime-created marker labels.")]
    [Min(1)]
    [SerializeField] int defaultMarkerLabelSize = 11;

    readonly List<MapViewportMarkerView> markerPool = new List<MapViewportMarkerView>();
    MapViewportSnapshot currentSnapshot = new MapViewportSnapshot();
    float refreshTimer;

    public MapViewportSnapshot CurrentSnapshot => currentSnapshot;
    public float Zoom => zoom;
    public event Action<MapViewportSnapshot> OnSnapshotChanged;

    void OnEnable() {
        refreshTimer = 0f;
        if(refreshOnEnable) {
            Refresh();
        }
    }

    void Update() {
        if(refreshMode != MapViewportRefreshMode.UpdateInterval) {
            return;
        }

        refreshTimer -= Time.unscaledDeltaTime;
        if(refreshTimer > 0f) {
            return;
        }

        refreshTimer = Mathf.Max(0.02f, refreshInterval);
        Refresh();
    }

    public void BindUI(
        RectTransform markerContainer,
        RectTransform playerIndicator,
        RectTransform navigationTargetIndicator,
        Text title,
        Text subtitle,
        Text target,
        Text zoomLabel,
        Text nearbyLabel) {
        markerRoot = markerContainer;
        playerMarker = playerIndicator;
        navigationTargetMarker = navigationTargetIndicator;
        titleText = title;
        subtitleText = subtitle;
        targetText = target;
        zoomText = zoomLabel;
        nearbyText = nearbyLabel;
    }

    public void ConfigureView(MapViewportMode mode, bool sameSceneOnly, bool labelsVisible, float minimapRadius) {
        viewportMode = mode;
        requireSameScene = sameSceneOnly;
        showMarkerLabels = labelsVisible;
        minimapWorldRadius = Mathf.Max(0.1f, minimapRadius);
    }

    public void SetZoom(float value) {
        zoom = Mathf.Clamp(value, Mathf.Min(minZoom, maxZoom), Mathf.Max(minZoom, maxZoom));
        Refresh();
    }

    public void ZoomIn(float amount = 0.25f) {
        SetZoom(zoom + Mathf.Abs(amount));
    }

    public void ZoomOut(float amount = 0.25f) {
        SetZoom(zoom - Mathf.Abs(amount));
    }

    [ContextMenu("Refresh Map Viewport")]
    public MapViewportSnapshot RefreshFromContextMenu() {
        return Refresh();
    }

    public MapViewportSnapshot Refresh() {
        var root = ResolveMarkerRoot();
        if(root == null) {
            currentSnapshot = new MapViewportSnapshot {
                message = "Map viewport has no RectTransform marker root."
            };
            OnSnapshotChanged?.Invoke(currentSnapshot);
            return currentSnapshot;
        }

        var player = ResolvePlayer();
        var centerWorld = ResolveCenter(player);
        var markerRecords = ResolveMarkerRecords(player, centerWorld);
        var bounds = ResolveWorldBounds(markerRecords, player, centerWorld);
        var navigationLog = player != null ? player.GetComponent<PlayerMapNavigationLog>() : null;
        var visibleRecords = LimitMarkers(markerRecords).ToList();

        EnsureMarkerPool(visibleRecords.Count, root);

        var snapshot = new MapViewportSnapshot {
            hasPlayer = player != null,
            playerName = player != null ? player.name : string.Empty,
            mode = viewportMode,
            centerWorldPosition = centerWorld ?? Vector3.zero,
            worldBounds = bounds,
            activeSceneName = SceneManager.GetActiveScene().name,
            mapViewProfileId = mapViewProfile != null ? mapViewProfile.Id : string.Empty,
            mapViewProfileName = mapViewProfile != null ? mapViewProfile.DisplayName : string.Empty,
            activeTargetId = navigationLog != null && navigationLog.HasActiveTarget ? navigationLog.ActiveTarget.markerId : string.Empty,
            activeTargetName = navigationLog != null && navigationLog.HasActiveTarget ? navigationLog.ActiveTarget.markerName : string.Empty
        };

        int viewIndex = 0;
        foreach(var record in visibleRecords) {
            bool clamped;
            Vector2 anchoredPosition;
            float distance;
            if(!TryProject(record.worldPosition, centerWorld, bounds, root, out anchoredPosition, out distance, out clamped)) {
                continue;
            }

            var markerView = markerPool[viewIndex];
            PositionRect(markerView.RectTransform, anchoredPosition);
            bool isTarget = navigationLog != null && navigationLog.IsTarget(record.id);
            markerView.Apply(record, showMarkerLabels, isTarget);
            markerView.gameObject.SetActive(true);

            snapshot.visibleMarkers.Add(new MapViewportMarkerSnapshot {
                markerId = record.id,
                displayName = record.displayName,
                category = record.category,
                worldPosition = record.worldPosition,
                viewportPosition = anchoredPosition,
                distance = distance,
                clamped = clamped,
                isNavigationTarget = isTarget
            });

            viewIndex++;
        }

        for(int i = viewIndex; i < markerPool.Count; i++) {
            if(markerPool[i] != null) {
                markerPool[i].gameObject.SetActive(false);
            }
        }

        UpdatePlayerMarker(player, centerWorld, bounds, root);
        UpdateNavigationTargetMarker(navigationLog, centerWorld, bounds, root);

        snapshot.visibleMarkerCount = snapshot.visibleMarkers.Count;
        snapshot.totalMarkerCount = markerRecords.Count;
        snapshot.message = BuildSnapshotMessage(snapshot);
        currentSnapshot = snapshot;

        UpdateText(snapshot);
        OnSnapshotChanged?.Invoke(currentSnapshot);
        return currentSnapshot;
    }

    RectTransform ResolveMarkerRoot() {
        if(markerRoot != null) {
            return markerRoot;
        }

        markerRoot = transform as RectTransform;
        return markerRoot;
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

    Vector3? ResolveCenter(PlayerController player) {
        if(centerOverride != null) {
            return centerOverride.position;
        }

        return player != null ? player.transform.position : (Vector3?)null;
    }

    List<MapMarkerRecord> ResolveMarkerRecords(PlayerController player, Vector3? centerWorld) {
        IEnumerable<MapMarkerRecord> records;
        if(mapViewProfile != null) {
            records = mapViewProfile.GetVisibleMarkers(player, centerWorld);
        } else {
            bool forMinimap = viewportMode == MapViewportMode.MinimapFollowPlayer;
            bool forWorldMap = viewportMode != MapViewportMode.MinimapFollowPlayer;
            records = MapMarkerRegistry.Ensure().GetVisibleMarkers(player, forMinimap, forWorldMap, includeHiddenMarkers);
        }

        string activeScene = SceneManager.GetActiveScene().name;
        return records
            .Where(record => record != null && !string.IsNullOrWhiteSpace(record.id))
            .Where(record => !requireSameScene || string.IsNullOrWhiteSpace(record.sceneName) || string.Equals(record.sceneName, activeScene, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    IEnumerable<MapMarkerRecord> LimitMarkers(IEnumerable<MapMarkerRecord> records) {
        return maxMarkerViews > 0 ? records.Take(maxMarkerViews) : records;
    }

    Rect ResolveWorldBounds(IReadOnlyList<MapMarkerRecord> records, PlayerController player, Vector3? centerWorld) {
        if(viewportMode == MapViewportMode.WorldBounds) {
            return BuildRect(worldBoundsCenter, worldBoundsSize);
        }

        if(viewportMode == MapViewportMode.MinimapFollowPlayer) {
            var minimapCenter = centerWorld.HasValue ? Project(centerWorld.Value) : Vector2.zero;
            var minimapSize = Vector2.one * Mathf.Max(0.1f, minimapWorldRadius * 2f / Mathf.Max(0.1f, zoom));
            return BuildRect(minimapCenter, minimapSize);
        }

        var points = records.Select(record => Project(record.worldPosition)).ToList();
        if(player != null) {
            points.Add(Project(player.transform.position));
        }

        if(centerWorld.HasValue) {
            points.Add(Project(centerWorld.Value));
        }

        if(points.Count == 0) {
            return BuildRect(worldBoundsCenter, worldBoundsSize);
        }

        float minX = points.Min(point => point.x);
        float maxX = points.Max(point => point.x);
        float minY = points.Min(point => point.y);
        float maxY = points.Max(point => point.y);
        float padding = Mathf.Max(0f, autoBoundsPadding);

        var center = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
        var size = new Vector2(maxX - minX + padding * 2f, maxY - minY + padding * 2f);
        size.x = Mathf.Max(Mathf.Abs(minimumAutoBoundsSize.x), size.x);
        size.y = Mathf.Max(Mathf.Abs(minimumAutoBoundsSize.y), size.y);
        return BuildRect(center, size);
    }

    bool TryProject(Vector3 worldPosition, Vector3? centerWorld, Rect worldBounds, RectTransform root, out Vector2 anchoredPosition, out float distance, out bool clamped) {
        anchoredPosition = Vector2.zero;
        clamped = false;

        var rootSize = ResolveRootSize(root);
        if(rootSize.x <= 0.01f || rootSize.y <= 0.01f) {
            distance = -1f;
            return false;
        }

        if(viewportMode == MapViewportMode.MinimapFollowPlayer) {
            if(!centerWorld.HasValue) {
                distance = -1f;
                return false;
            }

            var center = Project(centerWorld.Value);
            var point = Project(worldPosition);
            var delta = point - center;
            distance = delta.magnitude;

            if(rotateWithPlayer) {
                var player = ResolvePlayer();
                if(player != null) {
                    delta = Quaternion.Euler(0f, 0f, -player.transform.eulerAngles.z) * delta;
                }
            }

            var halfExtents = rootSize * 0.5f - Vector2.one * Mathf.Max(0f, viewportPadding);
            halfExtents.x = Mathf.Max(1f, halfExtents.x);
            halfExtents.y = Mathf.Max(1f, halfExtents.y);
            float pixelsPerWorldUnit = Mathf.Min(halfExtents.x, halfExtents.y) / Mathf.Max(0.1f, minimapWorldRadius) * Mathf.Max(0.1f, zoom);
            var pixelDelta = delta * pixelsPerWorldUnit;
            float outsideRatio = Mathf.Max(Mathf.Abs(pixelDelta.x) / halfExtents.x, Mathf.Abs(pixelDelta.y) / halfExtents.y);

            if(outsideRatio > 1f) {
                if(clampOutOfRangeMarkers) {
                    pixelDelta /= outsideRatio;
                    clamped = true;
                } else if(hideOutOfRangeMarkers) {
                    return false;
                }
            }

            anchoredPosition = CenterPixelsToAnchored(pixelDelta, rootSize);
            return true;
        }

        var projected = Project(worldPosition);
        distance = centerWorld.HasValue ? Vector2.Distance(Project(centerWorld.Value), projected) : -1f;

        float normalizedX = Mathf.InverseLerp(worldBounds.xMin, worldBounds.xMax, projected.x);
        float normalizedY = Mathf.InverseLerp(worldBounds.yMin, worldBounds.yMax, projected.y);
        bool outside = normalizedX < 0f || normalizedX > 1f || normalizedY < 0f || normalizedY > 1f;
        if(outside && hideOutOfRangeMarkers && !clampOutOfRangeMarkers) {
            return false;
        }

        normalizedX = Mathf.Clamp01(normalizedX);
        normalizedY = Mathf.Clamp01(normalizedY);
        clamped = outside;
        anchoredPosition = TopLeftPixelsToAnchored(new Vector2(normalizedX * rootSize.x, (1f - normalizedY) * rootSize.y));
        return true;
    }

    void UpdatePlayerMarker(PlayerController player, Vector3? centerWorld, Rect worldBounds, RectTransform root) {
        if(playerMarker == null) {
            return;
        }

        if(player == null) {
            playerMarker.gameObject.SetActive(false);
            return;
        }

        bool clamped;
        Vector2 position;
        float distance;
        if(TryProject(player.transform.position, centerWorld, worldBounds, root, out position, out distance, out clamped)) {
            PositionRect(playerMarker, position);
            playerMarker.gameObject.SetActive(true);
        } else {
            playerMarker.gameObject.SetActive(false);
        }
    }

    void UpdateNavigationTargetMarker(PlayerMapNavigationLog navigationLog, Vector3? centerWorld, Rect worldBounds, RectTransform root) {
        if(navigationTargetMarker == null) {
            return;
        }

        if(navigationLog == null || !navigationLog.HasActiveTarget || !navigationLog.ActiveTarget.hasWorldPosition) {
            navigationTargetMarker.gameObject.SetActive(false);
            return;
        }

        bool clamped;
        Vector2 position;
        float distance;
        if(TryProject(navigationLog.ActiveTarget.worldPosition, centerWorld, worldBounds, root, out position, out distance, out clamped)) {
            PositionRect(navigationTargetMarker, position);
            navigationTargetMarker.gameObject.SetActive(true);
        } else {
            navigationTargetMarker.gameObject.SetActive(false);
        }
    }

    void EnsureMarkerPool(int neededCount, RectTransform root) {
        while(markerPool.Count < neededCount) {
            markerPool.Add(CreateMarkerView(root));
        }
    }

    MapViewportMarkerView CreateMarkerView(RectTransform root) {
        MapViewportMarkerView view;
        if(markerPrefab != null) {
            view = Instantiate(markerPrefab, root);
        } else {
            view = CreateDefaultMarkerView(root);
        }

        var rect = view.RectTransform;
        if(rect != null) {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = defaultMarkerSize;
        }

        view.gameObject.SetActive(false);
        return view;
    }

    MapViewportMarkerView CreateDefaultMarkerView(RectTransform root) {
        var markerObject = new GameObject("Map_Marker_View", typeof(RectTransform), typeof(Image), typeof(MapViewportMarkerView));
        markerObject.transform.SetParent(root, false);

        var rect = markerObject.GetComponent<RectTransform>();
        rect.sizeDelta = defaultMarkerSize;

        var image = markerObject.GetComponent<Image>();
        image.color = new Color32(67, 123, 133, 255);

        var labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
        labelObject.transform.SetParent(markerObject.transform, false);
        var labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 0f);
        labelRect.anchorMax = new Vector2(0.5f, 0f);
        labelRect.pivot = new Vector2(0.5f, 1f);
        labelRect.anchoredPosition = new Vector2(0f, -4f);
        labelRect.sizeDelta = new Vector2(120f, 18f);

        var label = labelObject.GetComponent<Text>();
        label.font = ResolveDefaultFont();
        label.fontSize = defaultMarkerLabelSize;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = new Color32(35, 38, 42, 255);
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Truncate;

        var view = markerObject.GetComponent<MapViewportMarkerView>();
        view.Bind(image, label);
        return view;
    }

    void PositionRect(RectTransform rect, Vector2 anchoredPosition) {
        if(rect == null) {
            return;
        }

        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
    }

    void UpdateText(MapViewportSnapshot snapshot) {
        if(titleText != null) {
            titleText.text = viewportMode == MapViewportMode.MinimapFollowPlayer ? "Minimap" : "Map";
        }

        if(subtitleText != null) {
            subtitleText.text = !string.IsNullOrWhiteSpace(snapshot.mapViewProfileName) ? snapshot.mapViewProfileName : snapshot.activeSceneName;
        }

        if(targetText != null) {
            targetText.text = !string.IsNullOrWhiteSpace(snapshot.activeTargetName)
                ? $"Target: {snapshot.activeTargetName}"
                : "Target: none";
        }

        if(zoomText != null) {
            zoomText.text = $"Zoom {zoom:0.0}x";
        }

        if(nearbyText != null) {
            nearbyText.text = snapshot.visibleMarkers.Count > 0
                ? "Nearby: " + string.Join(", ", snapshot.visibleMarkers.Take(3).Select(marker => marker.displayName))
                : "Nearby: none";
        }
    }

    string BuildSnapshotMessage(MapViewportSnapshot snapshot) {
        if(snapshot == null) {
            return string.Empty;
        }

        return $"{snapshot.visibleMarkerCount}/{snapshot.totalMarkerCount} markers visible";
    }

    Vector2 Project(Vector3 worldPosition) {
        return projection == MapViewportProjection.XZ
            ? new Vector2(worldPosition.x, worldPosition.z)
            : new Vector2(worldPosition.x, worldPosition.y);
    }

    static Vector2 ResolveRootSize(RectTransform root) {
        if(root == null) {
            return Vector2.zero;
        }

        var size = root.rect.size;
        if(size.x <= 0.01f || size.y <= 0.01f) {
            size = root.sizeDelta;
        }

        return new Vector2(Mathf.Abs(size.x), Mathf.Abs(size.y));
    }

    static Vector2 CenterPixelsToAnchored(Vector2 centerPixels, Vector2 rootSize) {
        return TopLeftPixelsToAnchored(new Vector2(rootSize.x * 0.5f + centerPixels.x, rootSize.y * 0.5f - centerPixels.y));
    }

    static Vector2 TopLeftPixelsToAnchored(Vector2 topLeftPixels) {
        return new Vector2(topLeftPixels.x, -topLeftPixels.y);
    }

    static Rect BuildRect(Vector2 center, Vector2 size) {
        size.x = Mathf.Max(0.1f, Mathf.Abs(size.x));
        size.y = Mathf.Max(0.1f, Mathf.Abs(size.y));
        return new Rect(center - size * 0.5f, size);
    }

    static Font ResolveDefaultFont() {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if(font != null) {
            return font;
        }

        font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return font != null ? font : Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Arial" }, 12);
    }
}

[Serializable]
public class MapViewportSnapshot {
    [Tooltip("If enabled, a player was found for this viewport.")]
    public bool hasPlayer;
    [Tooltip("Resolved player name.")]
    public string playerName;
    [Tooltip("Viewport mode used for this snapshot.")]
    public MapViewportMode mode;
    [Tooltip("World-space center used by this snapshot.")]
    public Vector3 centerWorldPosition;
    [Tooltip("Projected world bounds used by world-map modes.")]
    public Rect worldBounds;
    [Tooltip("Active scene name when the snapshot was built.")]
    public string activeSceneName;
    [Tooltip("Map view profile id, if one is assigned.")]
    public string mapViewProfileId;
    [Tooltip("Map view profile name, if one is assigned.")]
    public string mapViewProfileName;
    [Tooltip("Active navigation target id, if any.")]
    public string activeTargetId;
    [Tooltip("Active navigation target display name, if any.")]
    public string activeTargetName;
    [Tooltip("Visible marker count after projection.")]
    public int visibleMarkerCount;
    [Tooltip("Marker count before projection and max view trimming.")]
    public int totalMarkerCount;
    [Tooltip("Readable debug/status message for this snapshot.")]
    public string message;
    [Tooltip("Marker rows drawn by this viewport.")]
    public List<MapViewportMarkerSnapshot> visibleMarkers = new List<MapViewportMarkerSnapshot>();
}

[Serializable]
public class MapViewportMarkerSnapshot {
    [Tooltip("Marker id.")]
    public string markerId;
    [Tooltip("Marker display name.")]
    public string displayName;
    [Tooltip("Marker category.")]
    public MapMarkerCategory category;
    [Tooltip("Marker world position.")]
    public Vector3 worldPosition;
    [Tooltip("Marker anchored UI position.")]
    public Vector2 viewportPosition;
    [Tooltip("Distance from viewport center. -1 means unavailable.")]
    public float distance;
    [Tooltip("If enabled, the marker was clamped to the viewport edge.")]
    public bool clamped;
    [Tooltip("If enabled, this marker is the active navigation target.")]
    public bool isNavigationTarget;
}
