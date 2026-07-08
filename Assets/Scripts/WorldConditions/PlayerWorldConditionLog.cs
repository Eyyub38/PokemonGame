using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerWorldConditionLog : MonoBehaviour, ISavable {
    [Tooltip("Runtime/save list of active world conditions affecting this player.")]
    [SerializeField] List<PlayerWorldConditionState> activeConditions = new List<PlayerWorldConditionState>();

    public IReadOnlyList<PlayerWorldConditionState> ActiveConditions => activeConditions;
    public event Action<PlayerWorldConditionState> OnConditionActivated;
    public event Action<PlayerWorldConditionState> OnConditionDeactivated;
    public event Action OnWorldConditionsChanged;

    void OnEnable() {
        SubscribeToTime();
    }

    void OnDisable() {
        UnsubscribeFromTime();
    }

    public PlayerWorldConditionState ActivateCondition(
        WorldConditionDefinition condition,
        string sourceId = null,
        string sourceName = null,
        RegionInfoDefinition region = null,
        ActivityZoneDefinition zone = null,
        int durationOverrideHours = -1,
        float intensity = 1f,
        int stacks = 1,
        bool refreshExisting = true,
        bool stackExisting = false
    ) {
        if(condition == null) {
            return null;
        }

        PruneExpired();
        string normalizedSourceId = sourceId ?? string.Empty;
        string normalizedRegionId = region != null ? region.Id : string.Empty;
        string normalizedZoneId = zone != null ? zone.Id : string.Empty;
        var existing = activeConditions.FirstOrDefault(state => state != null
            && state.conditionId == condition.Id
            && state.sourceId == normalizedSourceId
            && state.regionId == normalizedRegionId
            && state.zoneId == normalizedZoneId);

        int currentHour = GetCurrentAbsoluteHour();
        int durationHours = durationOverrideHours >= 0 ? durationOverrideHours : condition.DefaultDurationHours;
        int expiresAt = durationHours > 0 ? currentHour + durationHours : -1;

        if(existing != null) {
            bool changed = false;
            if(refreshExisting) {
                existing.startedDay = GetCurrentDay();
                existing.startedAbsoluteHour = currentHour;
                existing.expiresAbsoluteHour = expiresAt;
                existing.intensity = Mathf.Max(0f, intensity);
                changed = true;
            }

            if(stackExisting) {
                existing.stacks = Mathf.Max(1, existing.stacks + Mathf.Max(1, stacks));
                changed = true;
            }

            if(changed) {
                OnWorldConditionsChanged?.Invoke();
            }

            return existing;
        }

        var state = new PlayerWorldConditionState {
            conditionId = condition.Id,
            conditionName = condition.DisplayName,
            category = condition.Category,
            sourceId = normalizedSourceId,
            sourceName = sourceName ?? string.Empty,
            regionId = normalizedRegionId,
            regionName = region != null ? region.DisplayName : string.Empty,
            zoneId = normalizedZoneId,
            zoneName = zone != null ? zone.DisplayName : string.Empty,
            startedDay = GetCurrentDay(),
            startedAbsoluteHour = currentHour,
            expiresAbsoluteHour = expiresAt,
            intensity = Mathf.Max(0f, intensity),
            stacks = Mathf.Max(1, stacks)
        };

        activeConditions.Add(state);
        OnConditionActivated?.Invoke(state);
        OnWorldConditionsChanged?.Invoke();
        PublishConditionEvent(condition, state, active: true, "activated");
        return state;
    }

    public bool DeactivateCondition(WorldConditionDefinition condition, string sourceId = null, RegionInfoDefinition region = null, ActivityZoneDefinition zone = null) {
        if(condition == null) {
            return false;
        }

        PruneExpired();
        var removed = activeConditions
            .Where(state => MatchesState(state, condition, sourceId, region, zone))
            .ToList();

        foreach(var state in removed) {
            activeConditions.Remove(state);
            OnConditionDeactivated?.Invoke(state);
            PublishConditionEvent(condition, state, active: false, "deactivated");
        }

        if(removed.Count > 0) {
            OnWorldConditionsChanged?.Invoke();
            return true;
        }

        return false;
    }

    public bool IsConditionActive(WorldConditionDefinition condition, string sourceId = null) {
        if(condition == null) {
            return false;
        }

        PruneExpired();
        return activeConditions.Any(state => state != null
            && state.conditionId == condition.Id
            && (string.IsNullOrWhiteSpace(sourceId) || state.sourceId == sourceId));
    }

    public bool IsConditionActive(WorldConditionDefinition condition, string sourceId, RegionInfoDefinition region, ActivityZoneDefinition zone) {
        if(condition == null) {
            return false;
        }

        PruneExpired();
        return activeConditions.Any(state => MatchesState(state, condition, sourceId, region, zone));
    }

    public int GetActiveCount(WorldConditionDefinition condition = null, string tag = null, WorldConditionCategory? category = null) {
        PruneExpired();
        int count = 0;
        foreach(var state in activeConditions) {
            var definition = state?.ResolveDefinition();
            if(definition == null) {
                continue;
            }

            if(condition != null && definition.Id != condition.Id) {
                continue;
            }

            if(category.HasValue && definition.Category != category.Value) {
                continue;
            }

            if(!string.IsNullOrWhiteSpace(tag) && !definition.HasTag(tag)) {
                continue;
            }

            count++;
        }

        return count;
    }

    public bool HasConditionWithTag(string tag) {
        return GetActiveCount(tag: tag) > 0;
    }

    public List<WorldConditionDefinition> GetActiveConditions(ActivityDefinition activity = null, ActivityZoneDefinition activeZone = null, RegionInfoDefinition activeRegion = null) {
        return GetActiveConditionStates(activity, activeZone, activeRegion)
            .Select(state => state.ResolveDefinition())
            .Where(condition => condition != null)
            .Distinct()
            .OrderByDescending(condition => condition.Priority)
            .ThenBy(condition => condition.DisplayName)
            .ToList();
    }

    public List<PlayerWorldConditionState> GetActiveConditionStates(ActivityDefinition activity = null, ActivityZoneDefinition activeZone = null, RegionInfoDefinition activeRegion = null) {
        PruneExpired();
        return activeConditions
            .Where(state => state != null && StateAffects(state, activity, activeZone, activeRegion))
            .OrderByDescending(state => state.ResolveDefinition()?.Priority ?? 0)
            .ThenBy(state => state.conditionName)
            .ToList();
    }

    public bool IsActivityBlocked(ActivityDefinition activity, out string failureMessage) {
        foreach(var state in GetActiveConditionStates(activity, PlayerActivityContext.CurrentZone)) {
            var condition = state.ResolveDefinition();
            if(condition != null && condition.BlocksActivities) {
                failureMessage = condition.BlockedActivityMessage;
                return true;
            }
        }

        failureMessage = null;
        return false;
    }

    public int PruneExpired() {
        int currentHour = GetCurrentAbsoluteHour();
        var expired = activeConditions
            .Where(state => state != null && state.IsExpired(currentHour))
            .ToList();

        foreach(var state in expired) {
            activeConditions.Remove(state);
            var condition = state.ResolveDefinition();
            if(condition != null) {
                PublishConditionEvent(condition, state, active: false, "expired");
            }

            OnConditionDeactivated?.Invoke(state);
        }

        if(expired.Count > 0) {
            OnWorldConditionsChanged?.Invoke();
        }

        return expired.Count;
    }

    bool StateAffects(PlayerWorldConditionState state, ActivityDefinition activity, ActivityZoneDefinition activeZone, RegionInfoDefinition activeRegion) {
        var condition = state.ResolveDefinition();
        if(condition == null) {
            return false;
        }

        if(!StateMatchesLocation(state, activeZone, activeRegion)) {
            return false;
        }

        if(activity == null) {
            return condition.MatchesScope(activeRegion, activeZone);
        }

        return condition.Affects(activity, activeZone, activeRegion);
    }

    bool StateMatchesLocation(PlayerWorldConditionState state, ActivityZoneDefinition activeZone, RegionInfoDefinition activeRegion) {
        if(state == null) {
            return false;
        }

        if(!string.IsNullOrWhiteSpace(state.zoneId) && (activeZone == null || activeZone.Id != state.zoneId)) {
            return false;
        }

        if(!string.IsNullOrWhiteSpace(state.regionId)) {
            if(activeRegion != null && activeRegion.Id == state.regionId) {
                return true;
            }

            var stateRegion = state.ResolveRegion();
            if(stateRegion != null && activeZone != null && stateRegion.ActivityZones != null && stateRegion.ActivityZones.Contains(activeZone)) {
                return true;
            }

            return false;
        }

        return true;
    }

    bool MatchesState(PlayerWorldConditionState state, WorldConditionDefinition condition, string sourceId, RegionInfoDefinition region, ActivityZoneDefinition zone) {
        if(state == null || condition == null || state.conditionId != condition.Id) {
            return false;
        }

        if(!string.IsNullOrWhiteSpace(sourceId) && state.sourceId != sourceId) {
            return false;
        }

        if(region != null && state.regionId != region.Id) {
            return false;
        }

        if(zone != null && state.zoneId != zone.Id) {
            return false;
        }

        return true;
    }

    void SubscribeToTime() {
        if(TimeSystem.i == null) {
            return;
        }

        TimeSystem.i.OnTimeChanged -= HandleTimeChanged;
        TimeSystem.i.OnDayChanged -= HandleTimeChanged;
        TimeSystem.i.OnTimeChanged += HandleTimeChanged;
        TimeSystem.i.OnDayChanged += HandleTimeChanged;
    }

    void UnsubscribeFromTime() {
        if(TimeSystem.i == null) {
            return;
        }

        TimeSystem.i.OnTimeChanged -= HandleTimeChanged;
        TimeSystem.i.OnDayChanged -= HandleTimeChanged;
    }

    void HandleTimeChanged() {
        PruneExpired();
    }

    int GetCurrentDay() {
        return TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    void PublishConditionEvent(WorldConditionDefinition condition, PlayerWorldConditionState state, bool active, string phase) {
        GameEventPublishing.PublishOptional(
            active ? condition.ActivatedEvent : condition.DeactivatedEvent,
            $"world-condition.{phase}.{condition.Id}",
            active ? $"{condition.DisplayName} is active." : $"{condition.DisplayName} ended.",
            GameEventCategory.WorldEvent,
            active ? GameEventImportance.Info : GameEventImportance.Trace,
            this,
            "PlayerWorldConditionLog",
            GameEventScope.Player,
            showInFeed: condition.ShowConditionEventsInFeed,
            writeToDebugLog: condition.WriteConditionEventsToDebugLog,
            GameEventPublishing.Value("conditionId", condition.Id),
            GameEventPublishing.Value("conditionName", condition.DisplayName),
            GameEventPublishing.Value("category", condition.Category),
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("sourceId", state.sourceId),
            GameEventPublishing.Value("regionId", state.regionId),
            GameEventPublishing.Value("zoneId", state.zoneId),
            GameEventPublishing.Value("intensity", state.intensity),
            GameEventPublishing.Value("stacks", state.stacks));
    }

    public object CaptureState() {
        return new PlayerWorldConditionLogSaveData {
            activeConditions = activeConditions.Where(state => state != null).Select(state => state.ToSaveData()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerWorldConditionLogSaveData;
        activeConditions = saveData?.activeConditions?.Where(entry => entry != null).Select(entry => new PlayerWorldConditionState(entry)).ToList()
            ?? new List<PlayerWorldConditionState>();
        OnWorldConditionsChanged?.Invoke();
    }
}

[Serializable]
public class PlayerWorldConditionState {
    [Tooltip("Saved world condition definition id.")]
    public string conditionId;
    [Tooltip("Saved world condition display name.")]
    public string conditionName;
    [Tooltip("Saved world condition category.")]
    public WorldConditionCategory category;
    [Tooltip("Source id that activated this condition.")]
    public string sourceId;
    [Tooltip("Source display name saved for debug/fallback output.")]
    public string sourceName;
    [Tooltip("Region id this condition instance is limited to. Empty means no runtime region limit.")]
    public string regionId;
    [Tooltip("Region display name saved for debug/fallback output.")]
    public string regionName;
    [Tooltip("Activity zone id this condition instance is limited to. Empty means no runtime zone limit.")]
    public string zoneId;
    [Tooltip("Activity zone display name saved for debug/fallback output.")]
    public string zoneName;
    [Tooltip("In-game day when this condition became active.")]
    public int startedDay;
    [Tooltip("Absolute in-game hour when this condition became active.")]
    public int startedAbsoluteHour;
    [Tooltip("Absolute in-game hour when this condition expires. -1 means no automatic expiry.")]
    public int expiresAbsoluteHour = -1;
    [Tooltip("Strength of this condition instance. 1 uses definition values as-is.")]
    [Min(0f)]
    public float intensity = 1f;
    [Tooltip("Number of stacks applied to this condition instance.")]
    [Min(1)]
    public int stacks = 1;

    public PlayerWorldConditionState() {
    }

    public PlayerWorldConditionState(PlayerWorldConditionStateSaveData saveData) {
        if(saveData == null) {
            return;
        }

        conditionId = saveData.conditionId;
        conditionName = saveData.conditionName;
        category = saveData.category;
        sourceId = saveData.sourceId;
        sourceName = saveData.sourceName;
        regionId = saveData.regionId;
        regionName = saveData.regionName;
        zoneId = saveData.zoneId;
        zoneName = saveData.zoneName;
        startedDay = saveData.startedDay;
        startedAbsoluteHour = saveData.startedAbsoluteHour;
        expiresAbsoluteHour = saveData.expiresAbsoluteHour;
        intensity = Mathf.Max(0f, saveData.intensity);
        stacks = Mathf.Max(1, saveData.stacks);
    }

    public bool IsExpired(int currentAbsoluteHour) {
        return expiresAbsoluteHour >= 0 && currentAbsoluteHour >= expiresAbsoluteHour;
    }

    public float ScaleMultiplier(float multiplier) {
        float strength = Mathf.Max(0f, intensity) * Mathf.Max(1, stacks);
        return Mathf.Max(0f, 1f + (multiplier - 1f) * strength);
    }

    public int ScaleFlatBonus(int value) {
        return Mathf.RoundToInt(value * Mathf.Max(0f, intensity) * Mathf.Max(1, stacks));
    }

    public WorldConditionDefinition ResolveDefinition() {
        if(string.IsNullOrWhiteSpace(conditionId)) {
            return null;
        }

        return Resources.LoadAll<WorldConditionDefinition>("").FirstOrDefault(condition => condition != null && condition.Id == conditionId);
    }

    public RegionInfoDefinition ResolveRegion() {
        if(string.IsNullOrWhiteSpace(regionId)) {
            return null;
        }

        return Resources.LoadAll<RegionInfoDefinition>("").FirstOrDefault(region => region != null && region.Id == regionId);
    }

    public ActivityZoneDefinition ResolveZone() {
        if(string.IsNullOrWhiteSpace(zoneId)) {
            return null;
        }

        return Resources.LoadAll<ActivityZoneDefinition>("").FirstOrDefault(zone => zone != null && zone.Id == zoneId);
    }

    public PlayerWorldConditionStateSaveData ToSaveData() {
        return new PlayerWorldConditionStateSaveData {
            conditionId = conditionId,
            conditionName = conditionName,
            category = category,
            sourceId = sourceId,
            sourceName = sourceName,
            regionId = regionId,
            regionName = regionName,
            zoneId = zoneId,
            zoneName = zoneName,
            startedDay = startedDay,
            startedAbsoluteHour = startedAbsoluteHour,
            expiresAbsoluteHour = expiresAbsoluteHour,
            intensity = intensity,
            stacks = stacks
        };
    }
}

[Serializable]
public class PlayerWorldConditionLogSaveData {
    public List<PlayerWorldConditionStateSaveData> activeConditions;
}

[Serializable]
public class PlayerWorldConditionStateSaveData {
    public string conditionId;
    public string conditionName;
    public WorldConditionCategory category;
    public string sourceId;
    public string sourceName;
    public string regionId;
    public string regionName;
    public string zoneId;
    public string zoneName;
    public int startedDay;
    public int startedAbsoluteHour;
    public int expiresAbsoluteHour;
    public float intensity;
    public int stacks;
}
