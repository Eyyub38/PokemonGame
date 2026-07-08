using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MapMarkerRegistry : MonoBehaviour {
    [Header("Lifetime")]
    [Tooltip("If enabled, this registry survives scene loads.")]
    [SerializeField] bool dontDestroyOnLoad = true;
    [Header("Runtime")]
    [Tooltip("Runtime/manual markers added by scripts. Provider markers are tracked statically.")]
    [SerializeField] List<MapMarkerRecord> runtimeMarkers = new List<MapMarkerRecord>();

    static readonly HashSet<MapMarkerProvider> providers = new HashSet<MapMarkerProvider>();

    public static MapMarkerRegistry i { get; private set; }
    public static IReadOnlyCollection<MapMarkerProvider> Providers => providers;
    public IReadOnlyList<MapMarkerRecord> RuntimeMarkers => runtimeMarkers;

    void Awake() {
        if(i != null && i != this) {
            Destroy(gameObject);
            return;
        }

        i = this;
        if(dontDestroyOnLoad) {
            DontDestroyOnLoad(gameObject);
        }
    }

    public static MapMarkerRegistry Ensure() {
        if(i != null) {
            return i;
        }

        var existing = FindAnyObjectByType<MapMarkerRegistry>();
        if(existing != null) {
            i = existing;
            return i;
        }

        var go = new GameObject("MapMarkerRegistry");
        return go.AddComponent<MapMarkerRegistry>();
    }

    public static void Register(MapMarkerProvider provider) {
        if(provider == null) {
            return;
        }

        providers.Add(provider);
        Ensure();
    }

    public static void Unregister(MapMarkerProvider provider) {
        if(provider != null) {
            providers.Remove(provider);
        }
    }

    public IReadOnlyList<MapMarkerRecord> GetVisibleMarkers(PlayerController player, bool forMinimap = false, bool forWorldMap = true, bool includeHiddenByPreference = false) {
        var result = providers
            .Where(provider => provider != null && provider.isActiveAndEnabled)
            .Where(provider => provider.IsVisible(player, forMinimap, forWorldMap, out _, includeHiddenByPreference))
            .Select(provider => provider.BuildRecord(player))
            .Concat(runtimeMarkers.Where(marker => marker != null))
            .Where(marker => includeHiddenByPreference || !marker.hidden)
            .OrderByDescending(marker => marker.favorite)
            .ThenByDescending(marker => marker.important)
            .ThenByDescending(marker => marker.priority)
            .ThenBy(marker => marker.displayName)
            .ToList();

        return result;
    }

    public void AddRuntimeMarker(MapMarkerRecord record) {
        if(record == null || string.IsNullOrWhiteSpace(record.id)) {
            return;
        }

        runtimeMarkers.RemoveAll(marker => marker != null && marker.id == record.id);
        runtimeMarkers.Add(record);
    }

    public bool RemoveRuntimeMarker(string markerId) {
        if(string.IsNullOrWhiteSpace(markerId)) {
            return false;
        }

        return runtimeMarkers.RemoveAll(marker => marker != null && marker.id == markerId) > 0;
    }

    public void ClearRuntimeMarkers() {
        runtimeMarkers.Clear();
    }
}
