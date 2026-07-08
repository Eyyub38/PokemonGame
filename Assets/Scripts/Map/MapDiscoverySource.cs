using System.Collections.Generic;
using UnityEngine;

public class MapDiscoverySource : MonoBehaviour, IPlayerTriggerable {
    [Header("Discovery")]
    [Tooltip("Marker definitions discovered when this source is applied.")]
    [SerializeField] List<MapMarkerDefinition> markersToDiscover = new List<MapMarkerDefinition>();
    [Tooltip("Scene marker providers discovered when this source is applied.")]
    [SerializeField] List<MapMarkerProvider> markerProvidersToDiscover = new List<MapMarkerProvider>();
    [Tooltip("Short source id written into map logs. Empty uses this GameObject name.")]
    [SerializeField] string sourceId = "map-discovery-source";
    [Tooltip("If enabled, player trigger applies all assigned map discoveries.")]
    [SerializeField] bool discoverOnPlayerTrigger = true;
    [Tooltip("If enabled, related region, PokeNav entry and social post data are applied for marker definitions.")]
    [SerializeField] bool applyRelatedPokeNavInfo = true;

    [Header("Navigation")]
    [Tooltip("If enabled, this source also sets a map navigation target.")]
    [SerializeField] bool setNavigationTarget;
    [Tooltip("Preferred scene marker provider used as the navigation target.")]
    [SerializeField] MapMarkerProvider navigationTargetProvider;
    [Tooltip("Fallback marker definition used as the navigation target.")]
    [SerializeField] MapMarkerDefinition navigationTargetMarker;
    [Tooltip("If enabled, Navigation Target Position is used for the fallback marker definition.")]
    [SerializeField] bool useNavigationTargetPosition;
    [Tooltip("Fallback world position used when targeting a marker definition.")]
    [SerializeField] Vector3 navigationTargetPosition;
    [Tooltip("Optional view profile future UI can open after this discovery.")]
    [SerializeField] MapViewProfileDefinition suggestedViewProfile;
    [Tooltip("If enabled, the chosen navigation target is also discovered while setting the target.")]
    [SerializeField] bool discoverNavigationTarget = true;

    [Header("Trigger")]
    [Tooltip("If enabled, this trigger can be called repeatedly by the player.")]
    [SerializeField] bool triggerRepeatedly = true;

    [Header("Debug")]
    [Tooltip("If enabled, blocked discovery attempts are written to GameDebug.")]
    [SerializeField] bool logBlockedAttempts = true;
    [Tooltip("If enabled, successful discovery attempts are written to GameDebug.")]
    [SerializeField] bool logSuccessfulAttempts;

    public bool TriggerRepeatedly => triggerRepeatedly;
    public IReadOnlyList<MapMarkerDefinition> MarkersToDiscover => markersToDiscover;
    public IReadOnlyList<MapMarkerProvider> MarkerProvidersToDiscover => markerProvidersToDiscover;
    public bool SetNavigationTarget => setNavigationTarget;
    public MapMarkerProvider NavigationTargetProvider => navigationTargetProvider;
    public MapMarkerDefinition NavigationTargetMarker => navigationTargetMarker;
    public MapViewProfileDefinition SuggestedViewProfile => suggestedViewProfile;

    public void OnPlayerTriggered(PlayerController player) {
        if(!discoverOnPlayerTrigger) {
            return;
        }

        Apply(player);
    }

    public int Apply(PlayerController player) {
        if(player == null) {
            LogBlocked("Map discovery source has no player.");
            return 0;
        }

        int applied = 0;
        var mapLog = player.GetComponent<PlayerMapLog>();
        if(mapLog == null) {
            LogBlocked("Player is missing PlayerMapLog.");
        }

        if(markersToDiscover != null) {
            foreach(var marker in markersToDiscover) {
                if(marker == null) {
                    LogBlocked("Map discovery source has a null marker slot.");
                    continue;
                }

                if(mapLog != null && mapLog.DiscoverMarker(marker, ResolveSourceId())) {
                    applied++;
                }

                if(applyRelatedPokeNavInfo) {
                    ApplyRelatedPokeNavInfo(player, marker);
                }
            }
        }

        if(markerProvidersToDiscover != null) {
            foreach(var provider in markerProvidersToDiscover) {
                if(provider == null) {
                    LogBlocked("Map discovery source has a null provider slot.");
                    continue;
                }

                if(provider.Discover(player, ResolveSourceId())) {
                    applied++;
                }
            }
        }

        if(setNavigationTarget && TrySetNavigationTarget(player)) {
            applied++;
        }

        if(applied > 0 && logSuccessfulAttempts) {
            GameDebug.Success($"{applied} map discovery operation(s) applied.", GameDebugCategory.Map, this, "MapDiscoverySource");
        }

        return applied;
    }

    bool TrySetNavigationTarget(PlayerController player) {
        var navigationLog = player != null ? player.GetComponent<PlayerMapNavigationLog>() : null;
        if(navigationLog == null) {
            LogBlocked("Player is missing PlayerMapNavigationLog.");
            return false;
        }

        if(navigationTargetProvider != null) {
            return navigationLog.SetTarget(navigationTargetProvider, ResolveSourceId(), discoverNavigationTarget);
        }

        if(navigationTargetMarker != null) {
            return useNavigationTargetPosition
                ? navigationLog.SetTarget(navigationTargetMarker, navigationTargetPosition, ResolveSourceId(), discoverNavigationTarget)
                : navigationLog.SetTarget(navigationTargetMarker, ResolveSourceId(), discoverNavigationTarget);
        }

        if(markerProvidersToDiscover != null) {
            foreach(var provider in markerProvidersToDiscover) {
                if(provider != null) {
                    return navigationLog.SetTarget(provider, ResolveSourceId(), discoverNavigationTarget);
                }
            }
        }

        if(markersToDiscover != null) {
            foreach(var marker in markersToDiscover) {
                if(marker != null) {
                    return navigationLog.SetTarget(marker, ResolveSourceId(), discoverNavigationTarget);
                }
            }
        }

        LogBlocked("Map discovery source is set to navigation mode but has no target.");
        return false;
    }

    void ApplyRelatedPokeNavInfo(PlayerController player, MapMarkerDefinition marker) {
        if(player == null || marker == null) {
            return;
        }

        var pokeNav = player.GetComponent<PlayerPokeNavLog>();
        if(pokeNav == null) {
            return;
        }

        if(marker.Region != null) {
            pokeNav.DiscoverRegion(marker.Region, out _);
        }

        if(marker.PokeNavEntry != null) {
            pokeNav.DiscoverEntry(marker.PokeNavEntry, out _);
        }

        if(marker.SocialPost != null) {
            pokeNav.UnlockPost(marker.SocialPost);
        }
    }

    string ResolveSourceId() {
        return string.IsNullOrWhiteSpace(sourceId) ? name : sourceId;
    }

    void LogBlocked(string failureMessage) {
        if(!logBlockedAttempts) {
            return;
        }

        GameDebug.Warning(failureMessage, GameDebugCategory.Map, this, "MapDiscoverySource");
    }
}
