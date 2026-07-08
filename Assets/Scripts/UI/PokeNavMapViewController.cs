using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class PokeNavMapViewController : MonoBehaviour {
    [Header("Data")]
    [Tooltip("Backend manager that builds the PokeNav map snapshot and performs map actions. Empty tries this object, parents, then scene lookup.")]
    [SerializeField] PokeNavMapUIManager mapManager;
    [Tooltip("Optional viewport renderer refreshed after map actions such as setting a navigation target.")]
    [SerializeField] MapViewportUIController mapViewport;

    [Header("Selected Marker Text")]
    [Tooltip("Text that receives the selected marker name.")]
    [SerializeField] Text selectedNameText;
    [Tooltip("Text that receives category, scene, discovery and favorite status.")]
    [SerializeField] Text selectedMetaText;
    [Tooltip("Text that receives the selected marker description.")]
    [SerializeField] Text selectedDescriptionText;
    [Tooltip("Text that receives distance or position information.")]
    [SerializeField] Text selectedDistanceText;
    [Tooltip("Text that receives the active navigation target summary.")]
    [SerializeField] Text activeTargetText;
    [Tooltip("Text that receives marker counts and unread PokeNav counters.")]
    [SerializeField] Text summaryText;
    [Tooltip("Text that receives the last action feedback.")]
    [SerializeField] Text feedbackText;

    [Header("Marker Rows")]
    [Tooltip("Optional fixed Text rows used to show the visible marker list. Buttons on these objects are wired automatically.")]
    [SerializeField] List<Text> markerRowTexts = new List<Text>();
    [Tooltip("Optional Text prefab used when row texts need to be created at runtime.")]
    [SerializeField] Text markerRowPrefab = null;
    [Tooltip("Parent for runtime marker rows. Empty uses Marker Row Prefab parent or this transform.")]
    [SerializeField] Transform markerRowRoot;
    [Tooltip("Maximum rows shown in the marker list. 0 means all rows from the snapshot.")]
    [Min(0)]
    [SerializeField] int maxMarkerRows = 8;

    [Header("Runtime")]
    [Tooltip("If enabled, the map snapshot is refreshed when this controller becomes active.")]
    [SerializeField] bool refreshOnEnable = true;
    [Tooltip("If enabled, the first visible marker is selected when no previous selection exists.")]
    [SerializeField] bool selectFirstMarkerOnRefresh = true;
    [Tooltip("If enabled, the previous selected marker id is kept after refresh when it still exists.")]
    [SerializeField] bool preserveSelectionOnRefresh = true;
    [Tooltip("If enabled, Map Viewport is refreshed after successful or blocked actions.")]
    [SerializeField] bool refreshViewportAfterActions = true;

    PokeNavMapMarkerRow selectedMarker;
    PokeNavMapUIScreenSnapshot currentSnapshot;

    public PokeNavMapMarkerRow SelectedMarker => selectedMarker;
    public string SelectedMarkerId => selectedMarker != null ? selectedMarker.markerId : string.Empty;
    public PokeNavMapUIScreenSnapshot CurrentSnapshot => currentSnapshot;
    public event Action<PokeNavMapMarkerRow> OnSelectedMarkerChanged;

    void OnEnable() {
        ResolveReferences();
        Subscribe();

        if(refreshOnEnable) {
            RefreshView();
        }
    }

    void OnDisable() {
        Unsubscribe();
    }

    public void BindUI(
        PokeNavMapUIManager manager,
        MapViewportUIController viewport,
        Text selectedName,
        Text selectedMeta,
        Text selectedDescription,
        Text selectedDistance,
        Text activeTarget,
        Text summary,
        Text feedback,
        params Text[] markerRows) {
        mapManager = manager;
        mapViewport = viewport;
        selectedNameText = selectedName;
        selectedMetaText = selectedMeta;
        selectedDescriptionText = selectedDescription;
        selectedDistanceText = selectedDistance;
        activeTargetText = activeTarget;
        summaryText = summary;
        feedbackText = feedback;
        markerRowTexts = markerRows != null ? markerRows.Where(row => row != null).ToList() : new List<Text>();
        WireMarkerRowButtons();
    }

    [ContextMenu("Refresh PokeNav Map View")]
    public void RefreshViewFromContextMenu() {
        RefreshView();
    }

    public void RefreshView() {
        ResolveReferences();
        if(mapManager == null) {
            currentSnapshot = null;
            selectedMarker = null;
            UpdateEmptyState("PokeNav map manager is missing.");
            return;
        }

        string previousSelectionId = preserveSelectionOnRefresh ? SelectedMarkerId : string.Empty;
        currentSnapshot = mapManager.Refresh();
        selectedMarker = ResolveSelection(previousSelectionId, currentSnapshot);
        UpdateMarkerRows();
        UpdateSelectedMarkerTexts();
        OnSelectedMarkerChanged?.Invoke(selectedMarker);
    }

    public bool SelectMarker(string markerId) {
        if(currentSnapshot == null) {
            RefreshView();
        }

        var marker = FindMarker(markerId);
        if(marker == null) {
            SetFeedback($"Marker '{markerId}' was not found.");
            return false;
        }

        selectedMarker = marker;
        UpdateSelectedMarkerTexts();
        OnSelectedMarkerChanged?.Invoke(selectedMarker);
        return true;
    }

    public bool SelectMarkerAtIndex(int index) {
        if(currentSnapshot == null) {
            RefreshView();
        }

        var markers = GetVisibleMarkers();
        if(index < 0 || index >= markers.Count) {
            SetFeedback("No marker row is available at that index.");
            return false;
        }

        selectedMarker = markers[index];
        UpdateSelectedMarkerTexts();
        OnSelectedMarkerChanged?.Invoke(selectedMarker);
        return true;
    }

    public void SelectNextMarker() {
        SelectRelative(1);
    }

    public void SelectPreviousMarker() {
        SelectRelative(-1);
    }

    public bool SetSelectedAsNavigationTarget() {
        if(!CanUseSelectedMarker(out var marker)) {
            return false;
        }

        bool success = mapManager.TrySetNavigationTarget(marker.markerId, out var feedback);
        AfterAction(feedback);
        return success;
    }

    public bool ClearNavigationTarget() {
        ResolveReferences();
        if(mapManager == null) {
            SetFeedback("PokeNav map manager is missing.");
            return false;
        }

        bool success = mapManager.TryClearNavigationTarget(out var feedback);
        AfterAction(feedback);
        return success;
    }

    public bool MarkNavigationTargetReached() {
        ResolveReferences();
        if(mapManager == null) {
            SetFeedback("PokeNav map manager is missing.");
            return false;
        }

        bool success = mapManager.TryMarkNavigationTargetReached(out var feedback);
        AfterAction(feedback);
        return success;
    }

    public bool ToggleSelectedFavorite() {
        if(!CanUseSelectedMarker(out var marker)) {
            return false;
        }

        bool success = mapManager.TrySetMarkerFavorite(marker.markerId, !marker.favorite, out var feedback);
        AfterAction(feedback);
        return success;
    }

    public bool ToggleSelectedHidden() {
        if(!CanUseSelectedMarker(out var marker)) {
            return false;
        }

        bool success = mapManager.TrySetMarkerHidden(marker.markerId, !marker.hidden, out var feedback);
        AfterAction(feedback);
        return success;
    }

    void ResolveReferences() {
        if(mapManager == null) {
            mapManager = GetComponent<PokeNavMapUIManager>() ?? GetComponentInParent<PokeNavMapUIManager>() ?? FindAnyObjectByType<PokeNavMapUIManager>();
        }

        if(mapViewport == null) {
            mapViewport = GetComponent<MapViewportUIController>() ?? GetComponentInChildren<MapViewportUIController>() ?? GetComponentInParent<MapViewportUIController>();
        }

        if(markerRowRoot == null && markerRowPrefab != null) {
            markerRowRoot = markerRowPrefab.transform.parent;
        }

        if(markerRowRoot == null) {
            markerRowRoot = transform;
        }
    }

    void Subscribe() {
        if(mapManager == null) {
            return;
        }

        mapManager.OnSnapshotChanged -= HandleSnapshotChanged;
        mapManager.OnActionResult -= HandleActionResult;
        mapManager.OnSnapshotChanged += HandleSnapshotChanged;
        mapManager.OnActionResult += HandleActionResult;
    }

    void Unsubscribe() {
        if(mapManager == null) {
            return;
        }

        mapManager.OnSnapshotChanged -= HandleSnapshotChanged;
        mapManager.OnActionResult -= HandleActionResult;
    }

    void HandleSnapshotChanged(PokeNavMapUIScreenSnapshot snapshot) {
        currentSnapshot = snapshot;
        selectedMarker = ResolveSelection(SelectedMarkerId, currentSnapshot);
        UpdateMarkerRows();
        UpdateSelectedMarkerTexts();
        OnSelectedMarkerChanged?.Invoke(selectedMarker);
    }

    void HandleActionResult(PokeNavMapUIActionResult result) {
        if(result != null) {
            SetFeedback(result.message);
        }
    }

    PokeNavMapMarkerRow ResolveSelection(string previousSelectionId, PokeNavMapUIScreenSnapshot snapshot) {
        var markers = snapshot?.markers;
        if(markers == null || markers.Count == 0) {
            return null;
        }

        if(!string.IsNullOrWhiteSpace(previousSelectionId)) {
            var previous = markers.FirstOrDefault(marker => marker != null && string.Equals(marker.markerId, previousSelectionId, StringComparison.OrdinalIgnoreCase));
            if(previous != null) {
                return previous;
            }
        }

        return selectFirstMarkerOnRefresh ? markers.FirstOrDefault(marker => marker != null) : null;
    }

    PokeNavMapMarkerRow FindMarker(string markerId) {
        if(string.IsNullOrWhiteSpace(markerId) || currentSnapshot?.markers == null) {
            return null;
        }

        return currentSnapshot.markers.FirstOrDefault(marker => marker != null && string.Equals(marker.markerId, markerId, StringComparison.OrdinalIgnoreCase));
    }

    List<PokeNavMapMarkerRow> GetVisibleMarkers() {
        if(currentSnapshot?.markers == null) {
            return new List<PokeNavMapMarkerRow>();
        }

        var markers = currentSnapshot.markers.Where(marker => marker != null).ToList();
        return maxMarkerRows > 0 ? markers.Take(maxMarkerRows).ToList() : markers;
    }

    void SelectRelative(int offset) {
        if(currentSnapshot == null) {
            RefreshView();
        }

        var markers = GetVisibleMarkers();
        if(markers.Count == 0) {
            SetFeedback("No map markers are visible.");
            return;
        }

        int currentIndex = string.IsNullOrWhiteSpace(SelectedMarkerId)
            ? -1
            : markers.FindIndex(marker => string.Equals(marker.markerId, SelectedMarkerId, StringComparison.OrdinalIgnoreCase));
        int nextIndex = currentIndex < 0 ? 0 : (currentIndex + offset + markers.Count) % markers.Count;
        selectedMarker = markers[nextIndex];
        UpdateSelectedMarkerTexts();
        OnSelectedMarkerChanged?.Invoke(selectedMarker);
    }

    void UpdateMarkerRows() {
        var markers = GetVisibleMarkers();
        EnsureRuntimeRows(markers.Count);

        for(int i = 0; i < markerRowTexts.Count; i++) {
            var row = markerRowTexts[i];
            if(row == null) {
                continue;
            }

            if(i >= markers.Count) {
                row.text = string.Empty;
                row.gameObject.SetActive(false);
                continue;
            }

            var marker = markers[i];
            bool selected = selectedMarker != null && string.Equals(selectedMarker.markerId, marker.markerId, StringComparison.OrdinalIgnoreCase);
            string prefix = selected ? "> " : "  ";
            string target = marker.isNavigationTarget ? " [Target]" : string.Empty;
            string favorite = marker.favorite ? " [Fav]" : string.Empty;
            string hidden = marker.hidden ? " [Hidden]" : string.Empty;
            row.text = $"{prefix}{marker.displayName} - {marker.category}{target}{favorite}{hidden}";
            row.gameObject.SetActive(true);
        }

        WireMarkerRowButtons();
    }

    void EnsureRuntimeRows(int neededCount) {
        if(markerRowPrefab == null || markerRowRoot == null) {
            return;
        }

        int targetCount = maxMarkerRows > 0 ? Mathf.Min(maxMarkerRows, neededCount) : neededCount;
        while(markerRowTexts.Count < targetCount) {
            var row = Instantiate(markerRowPrefab, markerRowRoot);
            row.name = $"PokeNav_Marker_Row_{markerRowTexts.Count + 1:00}";
            markerRowTexts.Add(row);
        }
    }

    void WireMarkerRowButtons() {
        for(int i = 0; i < markerRowTexts.Count; i++) {
            var row = markerRowTexts[i];
            if(row == null) {
                continue;
            }

            var button = row.GetComponent<Button>() ?? row.GetComponentInParent<Button>();
            if(button == null) {
                continue;
            }

            int capturedIndex = i;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => SelectMarkerAtIndex(capturedIndex));
        }
    }

    void UpdateSelectedMarkerTexts() {
        if(currentSnapshot == null) {
            UpdateEmptyState("PokeNav map snapshot is empty.");
            return;
        }

        if(selectedMarker == null) {
            UpdateEmptyState("No marker selected.");
            UpdateSummaryText();
            UpdateActiveTargetText();
            return;
        }

        if(selectedNameText != null) {
            selectedNameText.text = selectedMarker.displayName;
        }

        if(selectedMetaText != null) {
            selectedMetaText.text = BuildMarkerMeta(selectedMarker);
        }

        if(selectedDescriptionText != null) {
            selectedDescriptionText.text = string.IsNullOrWhiteSpace(selectedMarker.description)
                ? selectedMarker.displayText
                : selectedMarker.description;
        }

        if(selectedDistanceText != null) {
            selectedDistanceText.text = selectedMarker.distance >= 0f
                ? $"Distance: {selectedMarker.distance:0.0}"
                : $"Position: {selectedMarker.worldPosition.x:0.0}, {selectedMarker.worldPosition.y:0.0}";
        }

        UpdateSummaryText();
        UpdateActiveTargetText();
    }

    void UpdateSummaryText() {
        if(summaryText == null || currentSnapshot == null) {
            return;
        }

        summaryText.text = $"Markers {currentSnapshot.markers.Count} / Favorites {currentSnapshot.favoriteMarkerCount} / Feed {currentSnapshot.unreadFeedCount}";
    }

    void UpdateActiveTargetText() {
        if(activeTargetText == null || currentSnapshot == null) {
            return;
        }

        activeTargetText.text = currentSnapshot.activeTarget != null
            ? currentSnapshot.activeTarget.displayText
            : "Target: none";
    }

    string BuildMarkerMeta(PokeNavMapMarkerRow marker) {
        var flags = new List<string> {
            marker.category.ToString()
        };

        if(!string.IsNullOrWhiteSpace(marker.sceneName)) {
            flags.Add(marker.sceneName);
        }

        if(marker.discovered) flags.Add("Discovered");
        if(marker.favorite) flags.Add("Favorite");
        if(marker.important) flags.Add("Important");
        if(marker.hidden) flags.Add("Hidden");
        if(marker.isNavigationTarget) flags.Add("Target");
        return string.Join(" / ", flags);
    }

    bool CanUseSelectedMarker(out PokeNavMapMarkerRow marker) {
        ResolveReferences();
        marker = selectedMarker;
        if(mapManager == null) {
            SetFeedback("PokeNav map manager is missing.");
            return false;
        }

        if(marker == null || string.IsNullOrWhiteSpace(marker.markerId)) {
            SetFeedback("No marker is selected.");
            return false;
        }

        return true;
    }

    void AfterAction(string feedback) {
        SetFeedback(feedback);
        RefreshView();
        if(refreshViewportAfterActions && mapViewport != null) {
            mapViewport.Refresh();
        }
    }

    void SetFeedback(string message) {
        if(feedbackText != null) {
            feedbackText.text = string.IsNullOrWhiteSpace(message) ? string.Empty : message;
        }
    }

    void UpdateEmptyState(string message) {
        if(selectedNameText != null) selectedNameText.text = "No marker";
        if(selectedMetaText != null) selectedMetaText.text = string.Empty;
        if(selectedDescriptionText != null) selectedDescriptionText.text = message;
        if(selectedDistanceText != null) selectedDistanceText.text = string.Empty;
        if(activeTargetText != null) activeTargetText.text = "Target: none";
        if(summaryText != null) summaryText.text = "Markers 0";
        SetFeedback(message);
    }
}
