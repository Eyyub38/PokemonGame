using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerTransitLog : MonoBehaviour, ISavable {
    [Tooltip("Runtime/save ids for routes the player has unlocked.")]
    [SerializeField] List<string> unlockedRouteIds = new List<string>();
    [Tooltip("Runtime/save ids for stops the player has discovered or unlocked.")]
    [SerializeField] List<string> unlockedStopIds = new List<string>();
    [Tooltip("Runtime/save travel history grouped by route and stop pair.")]
    [SerializeField] List<PlayerTransitTravelState> travelHistory = new List<PlayerTransitTravelState>();

    public IReadOnlyList<string> UnlockedRouteIds => unlockedRouteIds;
    public IReadOnlyList<string> UnlockedStopIds => unlockedStopIds;
    public IReadOnlyList<PlayerTransitTravelState> TravelHistory => travelHistory;
    public event Action<TransitRouteDefinition> OnRouteUnlocked;
    public event Action<TransitStopDefinition> OnStopUnlocked;
    public event Action<TransitRouteDefinition> OnTravelRecorded;

    public bool HasUnlockedRoute(TransitRouteDefinition route) {
        return route != null && (route.UnlockedByDefault || HasUnlockedRoute(route.Id));
    }

    public bool HasUnlockedRoute(string routeId) {
        return !string.IsNullOrWhiteSpace(routeId) && unlockedRouteIds.Contains(routeId);
    }

    public bool UnlockRoute(TransitRouteDefinition route, string source = null) {
        if(route == null || HasUnlockedRoute(route.Id)) {
            return false;
        }

        unlockedRouteIds.Add(route.Id);
        OnRouteUnlocked?.Invoke(route);
        PublishUnlockEvent("route", route.Id, route.DisplayName, source);
        return true;
    }

    public bool HasUnlockedStop(TransitStopDefinition stop) {
        return stop != null && (stop.UnlockedByDefault || HasUnlockedStop(stop.Id));
    }

    public bool HasUnlockedStop(string stopId) {
        return !string.IsNullOrWhiteSpace(stopId) && unlockedStopIds.Contains(stopId);
    }

    public bool UnlockStop(TransitStopDefinition stop, string source = null) {
        if(stop == null || HasUnlockedStop(stop.Id)) {
            return false;
        }

        unlockedStopIds.Add(stop.Id);
        OnStopUnlocked?.Invoke(stop);
        PublishUnlockEvent("stop", stop.Id, stop.DisplayName, source);
        return true;
    }

    public void RecordTravel(TransitRouteDefinition route, string originStopId, string destinationStopId, int estimatedTravelHours) {
        if(route == null) {
            return;
        }

        var state = travelHistory.FirstOrDefault(t => t != null
            && t.routeId == route.Id
            && t.originStopId == Normalize(originStopId)
            && t.destinationStopId == Normalize(destinationStopId));

        if(state == null) {
            state = new PlayerTransitTravelState {
                routeId = route.Id,
                routeName = route.DisplayName,
                originStopId = Normalize(originStopId),
                destinationStopId = Normalize(destinationStopId)
            };
            travelHistory.Add(state);
        }

        state.travelCount++;
        state.lastTravelDay = GetCurrentDay();
        state.lastTravelAbsoluteHour = GetCurrentAbsoluteHour();
        state.lastEstimatedTravelHours = Mathf.Max(0, estimatedTravelHours);
        OnTravelRecorded?.Invoke(route);
    }

    public int GetTravelCount(TransitRouteDefinition route, string originStopId = null, string destinationStopId = null) {
        return route != null ? GetTravelCount(route.Id, originStopId, destinationStopId) : 0;
    }

    public int GetTravelCount(string routeId, string originStopId = null, string destinationStopId = null) {
        if(string.IsNullOrWhiteSpace(routeId)) {
            return 0;
        }

        return travelHistory
            .Where(t => t != null && t.routeId == routeId)
            .Where(t => string.IsNullOrWhiteSpace(originStopId) || t.originStopId == Normalize(originStopId))
            .Where(t => string.IsNullOrWhiteSpace(destinationStopId) || t.destinationStopId == Normalize(destinationStopId))
            .Sum(t => Mathf.Max(0, t.travelCount));
    }

    public int GetTravelCountWithTag(string tag) {
        if(string.IsNullOrWhiteSpace(tag)) {
            return 0;
        }

        int count = 0;
        foreach(var state in travelHistory) {
            if(state == null || state.travelCount <= 0) {
                continue;
            }

            var route = ResolveRoute(state.routeId);
            if(route != null && route.HasTag(tag)) {
                count += state.travelCount;
            }
        }

        return count;
    }

    public int GetTotalTravelCount() {
        return travelHistory.Where(t => t != null).Sum(t => Mathf.Max(0, t.travelCount));
    }

    int GetCurrentDay() {
        return TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    string Normalize(string value) {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    TransitRouteDefinition ResolveRoute(string routeId) {
        if(string.IsNullOrWhiteSpace(routeId)) {
            return null;
        }

        return Resources.LoadAll<TransitRouteDefinition>("").FirstOrDefault(route => route != null && route.Id == routeId);
    }

    void PublishUnlockEvent(string unlockType, string unlockId, string unlockName, string source) {
        GameEventPublishing.PublishOptional(
            null,
            $"transit.unlocked.{unlockType}.{unlockId}",
            $"{unlockName} unlocked.",
            GameEventCategory.Transit,
            GameEventImportance.Success,
            this,
            "PlayerTransitLog",
            GameEventScope.Player,
            showInFeed: true,
            writeToDebugLog: false,
            GameEventPublishing.Value("unlockType", unlockType),
            GameEventPublishing.Value("unlockId", unlockId),
            GameEventPublishing.Value("unlockName", unlockName),
            GameEventPublishing.Value("source", source));
    }

    public object CaptureState() {
        return new PlayerTransitLogSaveData {
            unlockedRouteIds = unlockedRouteIds.Distinct().ToList(),
            unlockedStopIds = unlockedStopIds.Distinct().ToList(),
            travelHistory = travelHistory.Where(t => t != null).Select(t => t.ToSaveData()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerTransitLogSaveData;
        if(saveData == null) {
            unlockedRouteIds = new List<string>();
            unlockedStopIds = new List<string>();
            travelHistory = new List<PlayerTransitTravelState>();
            return;
        }

        unlockedRouteIds = saveData.unlockedRouteIds?.Distinct().ToList() ?? new List<string>();
        unlockedStopIds = saveData.unlockedStopIds?.Distinct().ToList() ?? new List<string>();
        travelHistory = saveData.travelHistory?.Where(t => t != null).Select(t => new PlayerTransitTravelState(t)).ToList() ?? new List<PlayerTransitTravelState>();
    }
}

[Serializable]
public class PlayerTransitTravelState {
    [Tooltip("Saved route id.")]
    public string routeId;
    [Tooltip("Saved route display name for fallback/debug output.")]
    public string routeName;
    [Tooltip("Origin stop id used for this grouped travel record.")]
    public string originStopId;
    [Tooltip("Destination stop id used for this grouped travel record.")]
    public string destinationStopId;
    [Tooltip("Number of times this route/stop pair has been used.")]
    [Min(0)]
    public int travelCount;
    [Tooltip("In-game day when this route was last used.")]
    public int lastTravelDay = -1;
    [Tooltip("Absolute in-game hour when this route was last used.")]
    public int lastTravelAbsoluteHour = -1;
    [Tooltip("Estimated route duration recorded on the last travel.")]
    [Min(0)]
    public int lastEstimatedTravelHours;

    public PlayerTransitTravelState() {
    }

    public PlayerTransitTravelState(PlayerTransitTravelStateSaveData saveData) {
        if(saveData == null) {
            return;
        }

        routeId = saveData.routeId;
        routeName = saveData.routeName;
        originStopId = saveData.originStopId;
        destinationStopId = saveData.destinationStopId;
        travelCount = Mathf.Max(0, saveData.travelCount);
        lastTravelDay = saveData.lastTravelDay;
        lastTravelAbsoluteHour = saveData.lastTravelAbsoluteHour;
        lastEstimatedTravelHours = Mathf.Max(0, saveData.lastEstimatedTravelHours);
    }

    public PlayerTransitTravelStateSaveData ToSaveData() {
        return new PlayerTransitTravelStateSaveData {
            routeId = routeId,
            routeName = routeName,
            originStopId = originStopId,
            destinationStopId = destinationStopId,
            travelCount = travelCount,
            lastTravelDay = lastTravelDay,
            lastTravelAbsoluteHour = lastTravelAbsoluteHour,
            lastEstimatedTravelHours = lastEstimatedTravelHours
        };
    }
}

[Serializable]
public class PlayerTransitLogSaveData {
    public List<string> unlockedRouteIds;
    public List<string> unlockedStopIds;
    public List<PlayerTransitTravelStateSaveData> travelHistory;
}

[Serializable]
public class PlayerTransitTravelStateSaveData {
    public string routeId;
    public string routeName;
    public string originStopId;
    public string destinationStopId;
    public int travelCount;
    public int lastTravelDay;
    public int lastTravelAbsoluteHour;
    public int lastEstimatedTravelHours;
}
