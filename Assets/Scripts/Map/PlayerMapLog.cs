using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerMapLog : MonoBehaviour, ISavable {
    [Tooltip("Runtime/save ids for map markers the player has discovered.")]
    [SerializeField] List<string> discoveredMarkerIds = new List<string>();
    [Tooltip("Runtime/save ids for markers the player has manually hidden in future UI.")]
    [SerializeField] List<string> hiddenMarkerIds = new List<string>();
    [Tooltip("Runtime/save ids for markers the player has favorited/pinned in future UI.")]
    [SerializeField] List<string> favoriteMarkerIds = new List<string>();

    public IReadOnlyList<string> DiscoveredMarkerIds => discoveredMarkerIds;
    public IReadOnlyList<string> HiddenMarkerIds => hiddenMarkerIds;
    public IReadOnlyList<string> FavoriteMarkerIds => favoriteMarkerIds;
    public event Action<string> OnMarkerDiscovered;
    public event Action OnMapLogChanged;

    public bool HasDiscoveredMarker(MapMarkerDefinition marker) {
        return marker != null && HasDiscoveredMarker(marker.Id);
    }

    public bool HasDiscoveredMarker(string markerId) {
        return !string.IsNullOrWhiteSpace(markerId) && discoveredMarkerIds.Contains(markerId);
    }

    public bool DiscoverMarker(MapMarkerDefinition marker, string source = null) {
        return marker != null && DiscoverMarker(marker.Id, marker.DisplayName, source);
    }

    public bool DiscoverMarker(string markerId, string markerName = null, string source = null) {
        if(string.IsNullOrWhiteSpace(markerId) || discoveredMarkerIds.Contains(markerId)) {
            return false;
        }

        discoveredMarkerIds.Add(markerId);
        OnMarkerDiscovered?.Invoke(markerId);
        OnMapLogChanged?.Invoke();
        PublishMapEvent("marker-discovered", markerId, string.IsNullOrWhiteSpace(markerName) ? markerId : markerName, source);
        return true;
    }

    public bool IsMarkerHidden(string markerId) {
        return !string.IsNullOrWhiteSpace(markerId) && hiddenMarkerIds.Contains(markerId);
    }

    public void SetMarkerHidden(string markerId, bool hidden) {
        if(string.IsNullOrWhiteSpace(markerId)) {
            return;
        }

        if(hidden && !hiddenMarkerIds.Contains(markerId)) {
            hiddenMarkerIds.Add(markerId);
        } else if(!hidden) {
            hiddenMarkerIds.Remove(markerId);
        }

        OnMapLogChanged?.Invoke();
    }

    public bool IsMarkerFavorite(string markerId) {
        return !string.IsNullOrWhiteSpace(markerId) && favoriteMarkerIds.Contains(markerId);
    }

    public void SetMarkerFavorite(string markerId, bool favorite) {
        if(string.IsNullOrWhiteSpace(markerId)) {
            return;
        }

        if(favorite && !favoriteMarkerIds.Contains(markerId)) {
            favoriteMarkerIds.Add(markerId);
        } else if(!favorite) {
            favoriteMarkerIds.Remove(markerId);
        }

        OnMapLogChanged?.Invoke();
    }

    void PublishMapEvent(string phase, string markerId, string markerName, string source) {
        GameEventPublishing.PublishOptional(
            null,
            $"map.{phase}.{markerId}",
            $"{markerName} marked on map.",
            GameEventCategory.Map,
            GameEventImportance.Success,
            this,
            "PlayerMapLog",
            GameEventScope.Player,
            showInFeed: true,
            writeToDebugLog: false,
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("markerId", markerId),
            GameEventPublishing.Value("markerName", markerName),
            GameEventPublishing.Value("source", source));
    }

    public object CaptureState() {
        return new PlayerMapLogSaveData {
            discoveredMarkerIds = discoveredMarkerIds.Distinct().ToList(),
            hiddenMarkerIds = hiddenMarkerIds.Distinct().ToList(),
            favoriteMarkerIds = favoriteMarkerIds.Distinct().ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerMapLogSaveData;
        discoveredMarkerIds = saveData?.discoveredMarkerIds?.Distinct().ToList() ?? new List<string>();
        hiddenMarkerIds = saveData?.hiddenMarkerIds?.Distinct().ToList() ?? new List<string>();
        favoriteMarkerIds = saveData?.favoriteMarkerIds?.Distinct().ToList() ?? new List<string>();
        OnMapLogChanged?.Invoke();
    }
}

[Serializable]
public class PlayerMapLogSaveData {
    public List<string> discoveredMarkerIds;
    public List<string> hiddenMarkerIds;
    public List<string> favoriteMarkerIds;
}
