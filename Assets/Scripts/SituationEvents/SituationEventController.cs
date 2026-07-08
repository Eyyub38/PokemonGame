using System.Collections.Generic;
using UnityEngine;

public class SituationEventController : MonoBehaviour {
    [Header("Pools")]
    [Tooltip("Situation event pools evaluated by this controller.")]
    [SerializeField] List<SituationEventPoolDefinition> pools = new List<SituationEventPoolDefinition>();
    [Tooltip("Optional player override. Empty uses PlayerController.i or the first loaded PlayerController.")]
    [SerializeField] PlayerController playerOverride = null;

    [Header("Context")]
    [Tooltip("Optional region context passed into pool/event filters.")]
    [SerializeField] RegionInfoDefinition regionContext = null;
    [Tooltip("Optional activity zone context passed into pool/event filters. Empty can fall back to PlayerActivityContext.CurrentZone.")]
    [SerializeField] ActivityZoneDefinition zoneContext = null;
    [Tooltip("Source id used by repeat/cooldown records when this controller rolls pools.")]
    [SerializeField] string sourceId = string.Empty;
    [Tooltip("Source name saved in event history when this controller rolls pools.")]
    [SerializeField] string sourceName = string.Empty;

    [Header("Signals")]
    [Tooltip("If enabled, pools are rolled once at Start.")]
    [SerializeField] bool rollOnStart;
    [Tooltip("If enabled, pools are rolled on TimeSystem.OnTimeChanged.")]
    [SerializeField] bool rollOnTimeChanged = true;
    [Tooltip("If enabled, pools are rolled on TimeSystem.OnDayChanged.")]
    [SerializeField] bool rollOnDayChanged = true;

    [Header("Debug")]
    [Tooltip("If enabled, roll summaries are written to GameDebugLogger.")]
    [SerializeField] bool logRolls;

    bool timeSubscribed;

    public IReadOnlyList<SituationEventPoolDefinition> Pools => pools;
    public RegionInfoDefinition RegionContext => regionContext;
    public ActivityZoneDefinition ZoneContext => zoneContext;

    void OnEnable() {
        SubscribeTime();
    }

    void Start() {
        SubscribeTime();
        if(rollOnStart) {
            RollPools("situation-controller:start", name);
        }
    }

    void OnDisable() {
        UnsubscribeTime();
    }

    [ContextMenu("Roll Situation Event Pools")]
    public void RollPoolsFromContextMenu() {
        RollPools("situation-controller:context-menu", name);
    }

    public List<SituationEventPoolRollResult> RollPools(string overrideSourceId = null, string overrideSourceName = null) {
        var results = new List<SituationEventPoolRollResult>();
        var player = ResolvePlayer();
        var region = ResolveRegion();
        var zone = ResolveZone();
        string resolvedSourceId = string.IsNullOrWhiteSpace(overrideSourceId) ? SourceId : overrideSourceId;
        string resolvedSourceName = string.IsNullOrWhiteSpace(overrideSourceName) ? SourceName : overrideSourceName;

        player?.GetComponent<PlayerSituationEventLog>()?.PruneExpired();

        foreach(var pool in pools) {
            if(pool == null) {
                continue;
            }

            var result = pool.Roll(player, region, zone, resolvedSourceId, resolvedSourceName, this);
            if(result != null) {
                results.Add(result);
            }
        }

        if(logRolls) {
            int started = 0;
            foreach(var result in results) {
                started += result != null ? result.startedEvents : 0;
            }

            GameDebugLogger.Ensure().Record(
                started > 0 ? GameDebugSeverity.Info : GameDebugSeverity.Trace,
                GameDebugCategory.WorldTrigger,
                $"{name} rolled {results.Count} situation pool(s), started {started} event(s).",
                this,
                "SituationEventController");
        }

        return results;
    }

    void HandleTimeChanged() {
        string id = TimeSystem.i != null ? $"situation-time:{TimeSystem.i.Day}:{TimeSystem.i.Hour}" : "situation-time:unknown";
        RollPools(id, "Situation Time Tick");
    }

    void HandleDayChanged() {
        string id = TimeSystem.i != null ? $"situation-day:{TimeSystem.i.Day}" : "situation-day:unknown";
        RollPools(id, "Situation Day Tick");
    }

    void SubscribeTime() {
        if(timeSubscribed || TimeSystem.i == null) {
            return;
        }

        if(rollOnTimeChanged) {
            TimeSystem.i.OnTimeChanged += HandleTimeChanged;
        }

        if(rollOnDayChanged) {
            TimeSystem.i.OnDayChanged += HandleDayChanged;
        }

        timeSubscribed = rollOnTimeChanged || rollOnDayChanged;
    }

    void UnsubscribeTime() {
        if(!timeSubscribed || TimeSystem.i == null) {
            timeSubscribed = false;
            return;
        }

        TimeSystem.i.OnTimeChanged -= HandleTimeChanged;
        TimeSystem.i.OnDayChanged -= HandleDayChanged;
        timeSubscribed = false;
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

    RegionInfoDefinition ResolveRegion() {
        return regionContext;
    }

    ActivityZoneDefinition ResolveZone() {
        return zoneContext != null ? zoneContext : PlayerActivityContext.CurrentZone;
    }

    string SourceId => string.IsNullOrWhiteSpace(sourceId) ? name : sourceId;
    string SourceName => string.IsNullOrWhiteSpace(sourceName) ? name : sourceName;
}
