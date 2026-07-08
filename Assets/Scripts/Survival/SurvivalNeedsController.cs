using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SurvivalNeedsController : MonoBehaviour, ISavable {
    [Header("Definitions")]
    [Tooltip("All need definitions tracked by this controller.")]
    [SerializeField] List<SurvivalNeedDefinition> needDefinitions = new List<SurvivalNeedDefinition>();
    [Tooltip("Need increased when eating.")]
    [SerializeField] SurvivalNeedDefinition hungerNeed;
    [Tooltip("Need reduced by movement and restored by rest/sleep.")]
    [SerializeField] SurvivalNeedDefinition energyNeed;
    [Tooltip("Need representing sleep/rest state.")]
    [SerializeField] SurvivalNeedDefinition sleepNeed;
    [Tooltip("Need increased by companions and some positive activities.")]
    [SerializeField] SurvivalNeedDefinition moraleNeed;
    [Tooltip("Skill that reduces survival penalties/decay when leveled.")]
    [SerializeField] PlayerSkillDefinition survivalSupportSkill;

    [Header("Rules")]
    [Tooltip("If enabled, needs update from TimeSystem hour changes.")]
    [SerializeField] bool updateWithWorldTime = true;
    [Tooltip("Number of tile steps before movement energy is consumed.")]
    [Min(1)]
    [SerializeField] int movementEnergyCostEverySteps = 12;
    [Tooltip("Energy amount consumed when the step threshold is reached.")]
    [Min(1)]
    [SerializeField] int movementEnergyCost = 2;
    [Tooltip("Morale gained per hour while at least one companion is following.")]
    [SerializeField] int moraleGainFromCompanion = 1;

    [Header("Logging")]
    [Tooltip("If enabled, low, critical and recovered threshold transitions publish game events.")]
    [SerializeField] bool publishNeedEvents = true;
    [Tooltip("If enabled, survival need changes are written to the debug log.")]
    [SerializeField] bool writeDebugLogs;
    [Tooltip("Maximum number of recent survival need changes kept for UI/debug views.")]
    [Min(1)]
    [SerializeField] int maxRecentChangeRecords = 40;

    [Header("Runtime")]
    [Tooltip("Runtime values for tracked needs. Usually populated automatically from definitions.")]
    [SerializeField] List<SurvivalNeed> needs = new List<SurvivalNeed>();
    [Tooltip("Recent survival need changes. Saved for debugging and lightweight UI history.")]
    [SerializeField] List<SurvivalNeedChangeRecord> recentChanges = new List<SurvivalNeedChangeRecord>();

    int minuteBuffer;
    int moveStepBuffer;
    PlayerController player;
    PlayerProgression progression;

    public event Action<SurvivalNeed> OnNeedChanged;
    public event Action<SurvivalNeed, SurvivalNeedChangeRecord> OnNeedChangeRecorded;
    public IReadOnlyList<SurvivalNeedDefinition> NeedDefinitions => needDefinitions;
    public IReadOnlyList<SurvivalNeed> Needs => needs;
    public IReadOnlyList<SurvivalNeedChangeRecord> RecentChanges => recentChanges;
    public bool IsResting { get; private set; }
    public bool IsSleeping { get; private set; }

    void Awake() {
        player = GetComponent<PlayerController>();
        progression = GetComponent<PlayerProgression>();
        EnsureNeedsFromDefinitions();
    }

    void OnEnable() {
        if(updateWithWorldTime && TimeSystem.i != null) {
            TimeSystem.i.OnTimeChanged += HandleTimeChanged;
        }

        if(player != null) {
            player.OnMovedTile += HandlePlayerMovedTile;
        }
    }

    void OnDisable() {
        if(TimeSystem.i != null) {
            TimeSystem.i.OnTimeChanged -= HandleTimeChanged;
        }

        if(player != null) {
            player.OnMovedTile -= HandlePlayerMovedTile;
        }
    }

    public SurvivalNeed GetNeed(SurvivalNeedDefinition definition) {
        if(definition == null) {
            return null;
        }

        return needs.FirstOrDefault(n => n.Definition == definition || n.Id == definition.Id);
    }

    public SurvivalNeed GetNeed(string id) {
        return needs.FirstOrDefault(n => n.Id == id);
    }

    public SurvivalNeedState GetWorstState() {
        if(needs.Any(n => n.State == SurvivalNeedState.Critical)) return SurvivalNeedState.Critical;
        if(needs.Any(n => n.State == SurvivalNeedState.Low)) return SurvivalNeedState.Low;
        if(needs.Count > 0 && needs.All(n => n.State == SurvivalNeedState.High)) return SurvivalNeedState.High;
        return SurvivalNeedState.Normal;
    }

    public int GetActionPenalty() {
        int penalty = 0;
        penalty += needs.Count(n => n.State == SurvivalNeedState.Critical) * 2;
        penalty += needs.Count(n => n.State == SurvivalNeedState.Low);
        return Mathf.Max(0, penalty - GetCompanionSurvivalSupport() - GetProgressionSurvivalSupport());
    }

    public void Eat(int nutrition) {
        ChangeNeed(hungerNeed, Mathf.Abs(nutrition), "eat");
        ChangeNeed(moraleNeed, Mathf.Max(1, nutrition / 8), "eat");
    }

    public void Rest(int hours = 1) {
        int appliedHours = Mathf.Max(1, hours);
        IsResting = true;
        ApplyHourlyChange(appliedHours, allowDecay: false, sourceId: "rest");
        GetComponent<PokemonCareNeedsController>()?.ApplyRest(appliedHours);
        IsResting = false;
    }

    public void Sleep(int hours = 8) {
        int appliedHours = Mathf.Max(1, hours);
        IsSleeping = true;
        ApplyHourlyChange(appliedHours, allowDecay: true, sourceId: "sleep");
        GetComponent<PokemonCareNeedsController>()?.ApplySleep(appliedHours);
        IsSleeping = false;
    }

    public void ChangeNeed(SurvivalNeedDefinition definition, int amount) {
        ChangeNeed(definition, amount, "manual");
    }

    public void ChangeNeed(SurvivalNeedDefinition definition, int amount, string sourceId) {
        TryChangeNeed(definition, amount, sourceId, out _);
    }

    public bool TryChangeNeed(SurvivalNeedDefinition definition, int amount, string sourceId, out SurvivalNeedChangeRecord record) {
        record = null;
        var need = GetNeed(definition);
        if(need == null || amount == 0) {
            return false;
        }

        int before = need.CurrentValue;
        var beforeState = need.State;
        need.Change(amount);
        int after = need.CurrentValue;
        int applied = after - before;
        if(applied == 0) {
            return false;
        }

        record = new SurvivalNeedChangeRecord {
            needId = need.Id,
            needName = need.DisplayName,
            sourceId = sourceId,
            amountRequested = amount,
            amountApplied = applied,
            beforeValue = before,
            afterValue = after,
            beforeState = beforeState,
            afterState = need.State,
            day = GetCurrentDay(),
            absoluteHour = GetCurrentAbsoluteHour()
        };

        RecordChange(need, record);
        return true;
    }

    void RecordChange(SurvivalNeed need, SurvivalNeedChangeRecord record) {
        recentChanges.Add(record);
        while(recentChanges.Count > Mathf.Max(1, maxRecentChangeRecords)) {
            recentChanges.RemoveAt(0);
        }

        PublishThresholdEvent(need, record);
        if(writeDebugLogs) {
            GameDebug.Step(
                $"{record.needName}: {record.beforeValue} -> {record.afterValue} ({record.amountApplied:+#;-#;0})",
                GameDebugCategory.Activity,
                this,
                "SurvivalNeedsController");
        }

        OnNeedChanged?.Invoke(need);
        OnNeedChangeRecorded?.Invoke(need, record);
    }

    void HandleTimeChanged() {
        minuteBuffer++;
        if(minuteBuffer < 60) {
            return;
        }

        int hours = minuteBuffer / 60;
        minuteBuffer %= 60;
        ApplyHourlyChange(hours, allowDecay: true, sourceId: "world-time");
    }

    void HandlePlayerMovedTile(Vector3 position) {
        moveStepBuffer++;
        if(moveStepBuffer < Mathf.Max(1, movementEnergyCostEverySteps)) {
            return;
        }

        moveStepBuffer = 0;
        ChangeNeed(energyNeed, -Mathf.Max(1, movementEnergyCost - GetCompanionStaminaSupport()), "movement");
    }

    void ApplyHourlyChange(int hours, bool allowDecay, string sourceId) {
        int support = GetCompanionSurvivalSupport() + GetProgressionSurvivalSupport();

        foreach(var need in needs) {
            var definition = need.Definition;
            if(definition == null) {
                continue;
            }

            if(IsSleeping) {
                ApplyNeedChange(need, definition.HourlySleepGain * hours, sourceId);
                continue;
            }

            if(IsResting) {
                ApplyNeedChange(need, definition.HourlyRestGain * hours, sourceId);
                continue;
            }

            if(allowDecay && definition.HourlyDecay > 0) {
                ApplyNeedChange(need, -Mathf.Max(1, definition.HourlyDecay - support) * hours, sourceId);
            }
        }

        if(GetActiveCompanions().Length > 0) {
            ChangeNeed(moraleNeed, moraleGainFromCompanion * hours, "companion");
        }
    }

    void ApplyNeedChange(SurvivalNeed need, int amount, string sourceId) {
        if(need?.Definition == null) {
            return;
        }

        TryChangeNeed(need.Definition, amount, sourceId, out _);
    }

    void PublishThresholdEvent(SurvivalNeed need, SurvivalNeedChangeRecord record) {
        if(!publishNeedEvents || need?.Definition == null || record == null) {
            return;
        }

        if(record.afterState == SurvivalNeedState.Critical && record.beforeState != SurvivalNeedState.Critical) {
            PublishNeedEvent(need.Definition.CriticalEvent, "critical", need, record, GameEventImportance.Warning);
        } else if(record.afterState == SurvivalNeedState.Low && record.beforeState != SurvivalNeedState.Low && record.beforeState != SurvivalNeedState.Critical) {
            PublishNeedEvent(need.Definition.LowEvent, "low", need, record, GameEventImportance.Warning);
        } else if((record.beforeState == SurvivalNeedState.Low || record.beforeState == SurvivalNeedState.Critical)
            && (record.afterState == SurvivalNeedState.Normal || record.afterState == SurvivalNeedState.High)) {
            PublishNeedEvent(need.Definition.RecoveredEvent, "recovered", need, record, GameEventImportance.Info);
        }
    }

    void PublishNeedEvent(GameEventDefinition eventDefinition, string phase, SurvivalNeed need, SurvivalNeedChangeRecord record, GameEventImportance importance) {
        GameEventPublishing.PublishOptional(
            eventDefinition,
            $"survival.need.{phase}.{need.Id}",
            $"{need.DisplayName} is {phase}.",
            GameEventCategory.Activity,
            importance,
            this,
            "SurvivalNeedsController",
            GameEventScope.Player,
            showInFeed: true,
            writeToDebugLog: false,
            GameEventPublishing.Value("needId", need.Id),
            GameEventPublishing.Value("needName", need.DisplayName),
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("beforeValue", record.beforeValue),
            GameEventPublishing.Value("afterValue", record.afterValue),
            GameEventPublishing.Value("sourceId", record.sourceId));
    }

    int GetCompanionStaminaSupport() {
        return GetActiveCompanions().Sum(c => c.GetStaminaSupport());
    }

    int GetCompanionSurvivalSupport() {
        return GetActiveCompanions().Sum(c => c.GetSurvivalSupport());
    }

    int GetProgressionSurvivalSupport() {
        if(progression == null) {
            progression = GetComponent<PlayerProgression>();
        }

        return progression != null ? progression.GetSkillLevel(survivalSupportSkill) : 0;
    }

    CompanionController[] GetActiveCompanions() {
        return FindObjectsByType<CompanionController>(FindObjectsInactive.Exclude)
            .Where(c => c.IsFollowing)
            .ToArray();
    }

    void EnsureNeedsFromDefinitions() {
        foreach(var definition in needDefinitions) {
            if(definition != null && needs.All(n => n.Id != definition.Id)) {
                needs.Add(new SurvivalNeed(definition));
            }
        }
    }

    int GetCurrentDay() {
        return TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    public object CaptureState() {
        return new SurvivalNeedsSaveData() {
            needs = needs.Select(n => new SurvivalNeedSaveData() {
                needId = n.Id,
                value = n.CurrentValue
            }).ToList(),
            minuteBuffer = minuteBuffer,
            moveStepBuffer = moveStepBuffer,
            recentChanges = recentChanges != null ? new List<SurvivalNeedChangeRecord>(recentChanges) : new List<SurvivalNeedChangeRecord>()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as SurvivalNeedsSaveData;
        if(saveData == null) {
            return;
        }

        EnsureNeedsFromDefinitions();
        foreach(var needData in saveData.needs) {
            GetNeed(needData.needId)?.Set(needData.value);
        }

        minuteBuffer = saveData.minuteBuffer;
        moveStepBuffer = saveData.moveStepBuffer;
        recentChanges = saveData.recentChanges?.Where(record => record != null).ToList()
            ?? new List<SurvivalNeedChangeRecord>();
    }
}

[Serializable]
public class SurvivalNeedsSaveData {
    public List<SurvivalNeedSaveData> needs;
    public int minuteBuffer;
    public int moveStepBuffer;
    public List<SurvivalNeedChangeRecord> recentChanges;
}

[Serializable]
public class SurvivalNeedSaveData {
    public string needId;
    public int value;
}

[Serializable]
public class SurvivalNeedChangeRecord {
    [Tooltip("Survival need id affected by this change.")]
    public string needId;
    [Tooltip("Survival need display name affected by this change.")]
    public string needName;
    [Tooltip("Source id that caused this change, such as world-time, movement, eat, rest or sleep.")]
    public string sourceId;
    [Tooltip("Raw requested amount before clamping.")]
    public int amountRequested;
    [Tooltip("Actual applied amount after clamping.")]
    public int amountApplied;
    [Tooltip("Need value before the change.")]
    public int beforeValue;
    [Tooltip("Need value after the change.")]
    public int afterValue;
    [Tooltip("Need state before the change.")]
    public SurvivalNeedState beforeState;
    [Tooltip("Need state after the change.")]
    public SurvivalNeedState afterState;
    [Tooltip("In-game day when this change happened.")]
    public int day;
    [Tooltip("Absolute in-game hour when this change happened.")]
    public int absoluteHour;
}
