using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SurvivalNeedsUIManager : MonoBehaviour {
    [Header("Source")]
    [Tooltip("Survival controller read by this UI manager. Empty uses the player controller at runtime.")]
    [SerializeField] SurvivalNeedsController controller;
    [Tooltip("If enabled, Refresh is called automatically when this object is enabled.")]
    [SerializeField] bool refreshOnEnable = true;
    [Tooltip("If enabled, snapshots update when the controller records a need change.")]
    [SerializeField] bool listenForNeedChanges = true;

    [Header("Rows")]
    [Tooltip("If enabled, high/healthy needs are included in the visible need rows.")]
    [SerializeField] bool includeHighNeeds = true;
    [Tooltip("If enabled, normal needs are included in the visible need rows.")]
    [SerializeField] bool includeNormalNeeds = true;
    [Tooltip("If enabled, rows are sorted by severity before display name.")]
    [SerializeField] bool sortBySeverity = true;
    [Tooltip("Maximum recent change rows returned to UI. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxRecentRows = 12;

    [Header("Default Actions")]
    [Tooltip("Default nutrition value used by EatDefault.")]
    [Min(0)]
    [SerializeField] int defaultNutrition = 20;
    [Tooltip("Default rest hours used by RestDefault.")]
    [Min(1)]
    [SerializeField] int defaultRestHours = 1;
    [Tooltip("Default sleep hours used by SleepDefault.")]
    [Min(1)]
    [SerializeField] int defaultSleepHours = 8;

    [Header("Debug")]
    [Tooltip("If enabled, refresh and action calls write debug messages.")]
    [SerializeField] bool logDebugMessages;

    SurvivalNeedsUIScreenSnapshot currentSnapshot = new SurvivalNeedsUIScreenSnapshot();

    public SurvivalNeedsController Controller => controller;
    public SurvivalNeedsUIScreenSnapshot CurrentSnapshot => currentSnapshot;
    public int MaxRecentRows => maxRecentRows;
    public event Action<SurvivalNeedsUIScreenSnapshot> OnSnapshotChanged;
    public event Action<SurvivalNeedActionResult> OnActionRan;

    void OnEnable() {
        var resolvedController = ResolveController();
        if(listenForNeedChanges && resolvedController != null) {
            resolvedController.OnNeedChangeRecorded += HandleNeedChangeRecorded;
        }

        if(refreshOnEnable) {
            Refresh();
        }
    }

    void OnDisable() {
        if(controller != null) {
            controller.OnNeedChangeRecorded -= HandleNeedChangeRecorded;
        }
    }

    [ContextMenu("Refresh Survival Needs Snapshot")]
    public SurvivalNeedsUIScreenSnapshot RefreshFromContextMenu() {
        return Refresh();
    }

    public SurvivalNeedsUIScreenSnapshot Refresh() {
        var resolvedController = ResolveController();
        if(resolvedController == null) {
            currentSnapshot = new SurvivalNeedsUIScreenSnapshot {
                isAvailable = false,
                unavailableReason = "No SurvivalNeedsController was found."
            };
            OnSnapshotChanged?.Invoke(currentSnapshot);
            return currentSnapshot;
        }

        var needRows = BuildNeedRows(resolvedController).ToList();
        var recentRows = BuildRecentRows(resolvedController).ToList();

        currentSnapshot = new SurvivalNeedsUIScreenSnapshot {
            isAvailable = true,
            unavailableReason = string.Empty,
            worstState = resolvedController.GetWorstState(),
            actionPenalty = resolvedController.GetActionPenalty(),
            isResting = resolvedController.IsResting,
            isSleeping = resolvedController.IsSleeping,
            needCount = needRows.Count,
            criticalCount = needRows.Count(row => row.state == SurvivalNeedState.Critical),
            lowCount = needRows.Count(row => row.state == SurvivalNeedState.Low),
            normalCount = needRows.Count(row => row.state == SurvivalNeedState.Normal),
            highCount = needRows.Count(row => row.state == SurvivalNeedState.High),
            recentChangeCount = recentRows.Count,
            needs = needRows,
            recentChanges = recentRows
        };

        if(logDebugMessages) {
            GameDebug.Step($"Survival needs snapshot refreshed: {currentSnapshot.needCount} rows.", GameDebugCategory.UI, this, "SurvivalNeedsUIManager");
        }

        OnSnapshotChanged?.Invoke(currentSnapshot);
        return currentSnapshot;
    }

    public SurvivalNeedActionResult EatDefault() {
        return Eat(defaultNutrition);
    }

    public SurvivalNeedActionResult RestDefault() {
        return Rest(defaultRestHours);
    }

    public SurvivalNeedActionResult SleepDefault() {
        return Sleep(defaultSleepHours);
    }

    public SurvivalNeedActionResult Eat(int nutrition) {
        var resolvedController = ResolveController();
        if(resolvedController == null) {
            return FinishAction(SurvivalNeedActionResult.Blocked("eat", "No SurvivalNeedsController was found."));
        }

        resolvedController.Eat(Mathf.Max(0, nutrition));
        return FinishAction(SurvivalNeedActionResult.Success("eat", $"Ate food for {Mathf.Max(0, nutrition)} nutrition."));
    }

    public SurvivalNeedActionResult Rest(int hours) {
        var resolvedController = ResolveController();
        if(resolvedController == null) {
            return FinishAction(SurvivalNeedActionResult.Blocked("rest", "No SurvivalNeedsController was found."));
        }

        int appliedHours = Mathf.Max(1, hours);
        resolvedController.Rest(appliedHours);
        return FinishAction(SurvivalNeedActionResult.Success("rest", $"Rested for {appliedHours} hour(s)."));
    }

    public SurvivalNeedActionResult Sleep(int hours) {
        var resolvedController = ResolveController();
        if(resolvedController == null) {
            return FinishAction(SurvivalNeedActionResult.Blocked("sleep", "No SurvivalNeedsController was found."));
        }

        int appliedHours = Mathf.Max(1, hours);
        resolvedController.Sleep(appliedHours);
        return FinishAction(SurvivalNeedActionResult.Success("sleep", $"Slept for {appliedHours} hour(s)."));
    }

    public SurvivalNeedActionResult ChangeNeed(SurvivalNeedDefinition definition, int amount, string sourceId = "ui") {
        var resolvedController = ResolveController();
        if(resolvedController == null) {
            return FinishAction(SurvivalNeedActionResult.Blocked("change", "No SurvivalNeedsController was found."));
        }

        if(definition == null) {
            return FinishAction(SurvivalNeedActionResult.Blocked("change", "No survival need definition was provided."));
        }

        bool changed = resolvedController.TryChangeNeed(definition, amount, sourceId, out var record);
        return FinishAction(changed
            ? SurvivalNeedActionResult.Success("change", $"{definition.DisplayName} changed by {record.amountApplied}.")
            : SurvivalNeedActionResult.Blocked("change", $"{definition.DisplayName} did not change."));
    }

    IEnumerable<SurvivalNeedUIRow> BuildNeedRows(SurvivalNeedsController source) {
        var rows = source.Needs
            .Where(need => need != null && need.Definition != null)
            .Where(ShouldIncludeNeed)
            .Select(SurvivalNeedUIRow.FromNeed);

        if(sortBySeverity) {
            rows = rows
                .OrderBy(row => GetSeveritySort(row.state))
                .ThenBy(row => row.displayName);
        } else {
            rows = rows.OrderBy(row => row.displayName);
        }

        return rows;
    }

    IEnumerable<SurvivalNeedChangeUIRow> BuildRecentRows(SurvivalNeedsController source) {
        var rows = source.RecentChanges
            .Where(record => record != null)
            .Reverse()
            .Select(SurvivalNeedChangeUIRow.FromRecord);

        return maxRecentRows > 0 ? rows.Take(maxRecentRows) : rows;
    }

    bool ShouldIncludeNeed(SurvivalNeed need) {
        if(need == null) {
            return false;
        }

        return need.State switch {
            SurvivalNeedState.High => includeHighNeeds,
            SurvivalNeedState.Normal => includeNormalNeeds,
            _ => true
        };
    }

    int GetSeveritySort(SurvivalNeedState state) {
        return state switch {
            SurvivalNeedState.Critical => 0,
            SurvivalNeedState.Low => 1,
            SurvivalNeedState.Normal => 2,
            SurvivalNeedState.High => 3,
            _ => 4
        };
    }

    void HandleNeedChangeRecorded(SurvivalNeed need, SurvivalNeedChangeRecord record) {
        Refresh();
    }

    SurvivalNeedActionResult FinishAction(SurvivalNeedActionResult result) {
        Refresh();
        if(logDebugMessages && result != null) {
            var severity = result.success ? GameDebugSeverity.Success : GameDebugSeverity.Warning;
            GameDebugLogger.Ensure().Record(severity, GameDebugCategory.UI, result.message, this, "SurvivalNeedsUIManager");
        }

        OnActionRan?.Invoke(result);
        return result;
    }

    SurvivalNeedsController ResolveController() {
        if(controller != null) {
            return controller;
        }

        controller = PlayerController.i != null
            ? PlayerController.i.GetComponent<SurvivalNeedsController>()
            : FindAnyObjectByType<SurvivalNeedsController>();
        return controller;
    }
}

public class SurvivalNeedsUIScreenSnapshot {
    [Tooltip("If false, no survival controller was available.")]
    public bool isAvailable;
    [Tooltip("Reason shown when the snapshot is unavailable.")]
    public string unavailableReason;
    [Tooltip("Worst current survival need state.")]
    public SurvivalNeedState worstState;
    [Tooltip("Action penalty reported by the survival controller.")]
    public int actionPenalty;
    [Tooltip("If enabled, the player is currently in a rest action.")]
    public bool isResting;
    [Tooltip("If enabled, the player is currently in a sleep action.")]
    public bool isSleeping;
    [Tooltip("Visible need row count.")]
    public int needCount;
    [Tooltip("Critical visible need count.")]
    public int criticalCount;
    [Tooltip("Low visible need count.")]
    public int lowCount;
    [Tooltip("Normal visible need count.")]
    public int normalCount;
    [Tooltip("High visible need count.")]
    public int highCount;
    [Tooltip("Visible recent change count.")]
    public int recentChangeCount;
    [Tooltip("Need rows available to UI.")]
    public List<SurvivalNeedUIRow> needs = new List<SurvivalNeedUIRow>();
    [Tooltip("Recent change rows available to UI.")]
    public List<SurvivalNeedChangeUIRow> recentChanges = new List<SurvivalNeedChangeUIRow>();
}

public class SurvivalNeedUIRow {
    [Tooltip("Need id.")]
    public string needId;
    [Tooltip("Need display name.")]
    public string displayName;
    [Tooltip("Need description.")]
    public string description;
    [Tooltip("Current need value.")]
    public int currentValue;
    [Tooltip("Maximum need value.")]
    public int maxValue;
    [Tooltip("Normalized current value from 0 to 1.")]
    public float normalized;
    [Tooltip("Current need state.")]
    public SurvivalNeedState state;
    [Tooltip("If enabled, this need is at or below low threshold.")]
    public bool isLow;
    [Tooltip("If enabled, this need is at or below critical threshold.")]
    public bool isCritical;

    public static SurvivalNeedUIRow FromNeed(SurvivalNeed need) {
        return new SurvivalNeedUIRow {
            needId = need.Id,
            displayName = need.DisplayName,
            description = need.Definition != null ? need.Definition.Description : string.Empty,
            currentValue = need.CurrentValue,
            maxValue = need.MaxValue,
            normalized = need.Normalized,
            state = need.State,
            isLow = need.State == SurvivalNeedState.Low || need.State == SurvivalNeedState.Critical,
            isCritical = need.State == SurvivalNeedState.Critical
        };
    }
}

public class SurvivalNeedChangeUIRow {
    [Tooltip("Need id affected by this change.")]
    public string needId;
    [Tooltip("Need display name affected by this change.")]
    public string needName;
    [Tooltip("Source id that caused this change.")]
    public string sourceId;
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

    public static SurvivalNeedChangeUIRow FromRecord(SurvivalNeedChangeRecord record) {
        return new SurvivalNeedChangeUIRow {
            needId = record.needId,
            needName = record.needName,
            sourceId = record.sourceId,
            amountApplied = record.amountApplied,
            beforeValue = record.beforeValue,
            afterValue = record.afterValue,
            beforeState = record.beforeState,
            afterState = record.afterState,
            day = record.day,
            absoluteHour = record.absoluteHour
        };
    }
}

public class SurvivalNeedActionResult {
    [Tooltip("Action id such as eat, rest, sleep or change.")]
    public string actionId;
    [Tooltip("If enabled, the action ran successfully.")]
    public bool success;
    [Tooltip("If enabled, the action was blocked.")]
    public bool blocked;
    [Tooltip("Human-readable result message.")]
    public string message;

    public static SurvivalNeedActionResult Success(string actionId, string message) {
        return new SurvivalNeedActionResult {
            actionId = actionId,
            success = true,
            blocked = false,
            message = message
        };
    }

    public static SurvivalNeedActionResult Blocked(string actionId, string message) {
        return new SurvivalNeedActionResult {
            actionId = actionId,
            success = false,
            blocked = true,
            message = message
        };
    }
}
