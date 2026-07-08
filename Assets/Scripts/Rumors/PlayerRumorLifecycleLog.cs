using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerRumorLifecycleLog : MonoBehaviour, ISavable {
    [Tooltip("Runtime/save lifecycle states for rumors that have started spreading.")]
    [SerializeField] List<PlayerRumorLifecycleState> activeRumors = new List<PlayerRumorLifecycleState>();

    public IReadOnlyList<PlayerRumorLifecycleState> ActiveRumors => activeRumors;
    public event Action<RumorDefinition> OnRumorSeeded;
    public event Action<RumorDefinition, RumorLifecycleStage> OnRumorStageChanged;
    public event Action OnRumorLifecycleChanged;

    public bool HasActiveRumor(RumorDefinition rumor) {
        return GetState(rumor) != null;
    }

    public PlayerRumorLifecycleState GetState(RumorDefinition rumor) {
        return rumor != null ? GetState(rumor.Id) : null;
    }

    public PlayerRumorLifecycleState GetState(string rumorId) {
        if(string.IsNullOrWhiteSpace(rumorId)) {
            return null;
        }

        return activeRumors.FirstOrDefault(state => state != null && state.rumorId == rumorId);
    }

    public bool SeedRumor(RumorDefinition rumor, RumorSource source, string reason = null, bool refreshExisting = false) {
        if(rumor == null || rumor.SpreadProfile == null || source == null || !rumor.SpreadProfile.CanSeedFrom(source)) {
            return false;
        }

        var state = GetState(rumor);
        if(state != null) {
            if(refreshExisting) {
                state.seededDay = GetCurrentDay();
                state.seededAbsoluteHour = GetCurrentAbsoluteHour();
                state.originSourceId = source.SourceId;
                state.originSourceName = source.DisplayName;
                state.originRegionId = source.Region != null ? source.Region.Id : rumor.OriginRegion != null ? rumor.OriginRegion.Id : string.Empty;
                state.originRegionName = source.Region != null ? source.Region.DisplayName : rumor.OriginRegion != null ? rumor.OriginRegion.DisplayName : string.Empty;
                state.lastKnownStage = RumorLifecycleStage.Fresh;
                OnRumorLifecycleChanged?.Invoke();
            }
            return false;
        }

        state = new PlayerRumorLifecycleState {
            rumorId = rumor.Id,
            rumorTitle = rumor.Title,
            profileId = rumor.SpreadProfile.Id,
            importance = rumor.Importance,
            originSourceId = source.SourceId,
            originSourceName = source.DisplayName,
            originRegionId = source.Region != null ? source.Region.Id : rumor.OriginRegion != null ? rumor.OriginRegion.Id : string.Empty,
            originRegionName = source.Region != null ? source.Region.DisplayName : rumor.OriginRegion != null ? rumor.OriginRegion.DisplayName : string.Empty,
            seededDay = GetCurrentDay(),
            seededAbsoluteHour = GetCurrentAbsoluteHour(),
            lastKnownStage = RumorLifecycleStage.Fresh,
            seedReason = reason
        };

        activeRumors.Add(state);
        OnRumorSeeded?.Invoke(rumor);
        OnRumorLifecycleChanged?.Invoke();
        PublishLifecycleEvent(rumor, state, "seeded", GameEventImportance.Info);
        return true;
    }

    public bool CanHear(RumorDefinition rumor, RumorSource source, out string failureMessage) {
        if(rumor == null || rumor.SpreadProfile == null) {
            failureMessage = null;
            return true;
        }

        var state = GetState(rumor);
        if(state == null) {
            failureMessage = $"{rumor.Title} has not started spreading.";
            return false;
        }

        int elapsedHours = GetElapsedHours(state);
        UpdateStage(rumor, state, elapsedHours);
        if(!rumor.SpreadProfile.CanReachSource(state, source, elapsedHours)) {
            failureMessage = $"{rumor.Title} has not reached this source.";
            return false;
        }

        failureMessage = null;
        return true;
    }

    public RumorLifecycleStage GetStage(RumorDefinition rumor) {
        var state = GetState(rumor);
        if(rumor == null || rumor.SpreadProfile == null || state == null) {
            return RumorLifecycleStage.Fresh;
        }

        int elapsedHours = GetElapsedHours(state);
        UpdateStage(rumor, state, elapsedHours);
        return state.lastKnownStage;
    }

    public int GetElapsedHours(PlayerRumorLifecycleState state) {
        if(state == null || state.seededAbsoluteHour < 0) {
            return 0;
        }

        return Mathf.Max(0, GetCurrentAbsoluteHour() - state.seededAbsoluteHour);
    }

    void UpdateStage(RumorDefinition rumor, PlayerRumorLifecycleState state, int elapsedHours) {
        if(rumor == null || rumor.SpreadProfile == null || state == null) {
            return;
        }

        var stage = rumor.SpreadProfile.GetStage(elapsedHours);
        if(stage == state.lastKnownStage) {
            return;
        }

        state.lastKnownStage = stage;
        OnRumorStageChanged?.Invoke(rumor, stage);
        OnRumorLifecycleChanged?.Invoke();
        PublishLifecycleEvent(rumor, state, $"stage-{stage}", stage == RumorLifecycleStage.Forgotten ? GameEventImportance.Trace : GameEventImportance.Info);
    }

    int GetCurrentDay() {
        return TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    void PublishLifecycleEvent(RumorDefinition rumor, PlayerRumorLifecycleState state, string phase, GameEventImportance importance) {
        GameEventPublishing.PublishOptional(
            null,
            $"rumor.lifecycle.{phase}.{rumor.Id}",
            $"{rumor.Title} lifecycle {phase}.",
            GameEventCategory.Rumor,
            importance,
            this,
            "PlayerRumorLifecycleLog",
            GameEventScope.Player,
            showInFeed: false,
            writeToDebugLog: false,
            GameEventPublishing.Value("rumorId", rumor.Id),
            GameEventPublishing.Value("rumorTitle", rumor.Title),
            GameEventPublishing.Value("profileId", state.profileId),
            GameEventPublishing.Value("importance", state.importance),
            GameEventPublishing.Value("stage", state.lastKnownStage),
            GameEventPublishing.Value("originSourceId", state.originSourceId),
            GameEventPublishing.Value("originRegionId", state.originRegionId));
    }

    public object CaptureState() {
        return activeRumors.Where(state => state != null).Select(state => state.ToSaveData()).ToList();
    }

    public void RestoreState(object state) {
        activeRumors = state as List<PlayerRumorLifecycleState> ?? new List<PlayerRumorLifecycleState>();
        OnRumorLifecycleChanged?.Invoke();
    }
}

[Serializable]
public class PlayerRumorLifecycleState {
    [Tooltip("Saved rumor id.")]
    public string rumorId;
    [Tooltip("Saved rumor title for fallback/debug output.")]
    public string rumorTitle;
    [Tooltip("Saved spread profile id.")]
    public string profileId;
    [Tooltip("Saved importance level at seed time.")]
    public RumorImportanceLevel importance;
    [Tooltip("Source id where this rumor started.")]
    public string originSourceId;
    [Tooltip("Source name where this rumor started.")]
    public string originSourceName;
    [Tooltip("Region id where this rumor started.")]
    public string originRegionId;
    [Tooltip("Region name where this rumor started.")]
    public string originRegionName;
    [Tooltip("In-game day when this rumor started spreading.")]
    public int seededDay = -1;
    [Tooltip("Absolute in-game hour when this rumor started spreading.")]
    public int seededAbsoluteHour = -1;
    [Tooltip("Last lifecycle stage calculated for this rumor.")]
    public RumorLifecycleStage lastKnownStage = RumorLifecycleStage.Fresh;
    [Tooltip("Optional debug/source reason used when the rumor was seeded.")]
    public string seedReason;

    public PlayerRumorLifecycleState() {
    }

    public PlayerRumorLifecycleState(PlayerRumorLifecycleState saveData) {
        if(saveData == null) {
            return;
        }

        rumorId = saveData.rumorId;
        rumorTitle = saveData.rumorTitle;
        profileId = saveData.profileId;
        importance = saveData.importance;
        originSourceId = saveData.originSourceId;
        originSourceName = saveData.originSourceName;
        originRegionId = saveData.originRegionId;
        originRegionName = saveData.originRegionName;
        seededDay = saveData.seededDay;
        seededAbsoluteHour = saveData.seededAbsoluteHour;
        lastKnownStage = saveData.lastKnownStage;
        seedReason = saveData.seedReason;
    }

    public RegionInfoDefinition ResolveOriginRegion() {
        if(string.IsNullOrWhiteSpace(originRegionId)) {
            return null;
        }

        return Resources.LoadAll<RegionInfoDefinition>("").FirstOrDefault(region => region != null && region.Id == originRegionId);
    }

    public PlayerRumorLifecycleState ToSaveData() {
        return new PlayerRumorLifecycleState(this);
    }
}
