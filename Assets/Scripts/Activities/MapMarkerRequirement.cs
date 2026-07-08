using UnityEngine;

public enum MapMarkerRequirementMode {
    MarkerDiscovered,
    MarkerFavorite,
    MarkerHidden
}

[CreateAssetMenu(menuName = "Activities/Requirements/Map Marker Requirement")]
public class MapMarkerRequirement : ActivityRequirement {
    [Tooltip("Which map marker value this requirement checks.")]
    [SerializeField] MapMarkerRequirementMode mode = MapMarkerRequirementMode.MarkerDiscovered;
    [Tooltip("Marker definition checked by this requirement.")]
    [SerializeField] MapMarkerDefinition marker;
    [Tooltip("Optional marker id override. Empty uses Marker Definition id.")]
    [SerializeField] string markerId;
    [Tooltip("If enabled, the selected map marker condition must be true. If disabled, it must be false.")]
    [SerializeField] bool mustBeMet = true;

    public override bool IsMet(PlayerController player) {
        var mapLog = player != null ? player.GetComponent<PlayerMapLog>() : null;
        string id = !string.IsNullOrWhiteSpace(markerId) ? markerId : marker != null ? marker.Id : string.Empty;
        bool result = mode switch {
            MapMarkerRequirementMode.MarkerFavorite => mapLog != null && mapLog.IsMarkerFavorite(id),
            MapMarkerRequirementMode.MarkerHidden => mapLog != null && mapLog.IsMarkerHidden(id),
            _ => mapLog != null && mapLog.HasDiscoveredMarker(id)
        };

        return mustBeMet ? result : !result;
    }
}
