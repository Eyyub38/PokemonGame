using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum SituationEventSignalUIActionResultKind {
    None,
    Refreshed,
    Evaluated,
    RuleFilterChanged,
    Blocked
}

public class SituationEventSignalUIManager : MonoBehaviour {
    [Header("Player")]
    [Tooltip("Player whose situation signal log is shown. Empty uses PlayerController.i or the first PlayerController in the scene.")]
    [SerializeField] PlayerController playerOverride = null;
    [Tooltip("If enabled, missing PlayerSituationEventSignalLog is created when UI actions need it.")]
    [SerializeField] bool createMissingSignalLogForActions = true;

    [Header("Signal Source")]
    [Tooltip("Controller used by evaluate actions. Empty searches the scene for a controller using Profile Override.")]
    [SerializeField] SituationEventSignalController controller = null;
    [Tooltip("Profile shown by this UI backend. Empty uses Controller.Profile.")]
    [SerializeField] SituationEventSignalProfileDefinition profileOverride = null;
    [Tooltip("Trigger used to preview whether signal rules are currently selectable.")]
    [SerializeField] SituationEventSignalTrigger previewTrigger = SituationEventSignalTrigger.Manual;

    [Header("Filters")]
    [Tooltip("Optional rule id filter. Empty shows every rule in the selected profile.")]
    [SerializeField] string selectedRuleId = string.Empty;
    [Tooltip("If enabled, disabled rules are still included in the snapshot.")]
    [SerializeField] bool includeDisabledRules = true;
    [Tooltip("If enabled, blocked signal log entries remain visible.")]
    [SerializeField] bool includeBlockedHistory = true;
    [Tooltip("If enabled, history is limited to the selected profile.")]
    [SerializeField] bool showOnlySelectedProfileHistory = true;
    [Tooltip("If enabled, newest signal log entries are shown first.")]
    [SerializeField] bool newestHistoryFirst = true;

    [Header("Snapshot")]
    [Tooltip("If enabled, Refresh is called when this component starts.")]
    [SerializeField] bool refreshOnStart = true;
    [Tooltip("If enabled, Refresh is called when the player's signal log records a new entry.")]
    [SerializeField] bool refreshWhenLogChanges = true;
    [Tooltip("If enabled, Refresh is called after every successful or blocked action.")]
    [SerializeField] bool refreshAfterActions = true;
    [Tooltip("Maximum signal rule rows copied into the snapshot. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxRuleRows = 40;
    [Tooltip("Maximum history rows copied into the snapshot. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxHistoryRows = 50;

    [Header("Debug")]
    [Tooltip("If enabled, successful UI backend actions are written to GameDebug.")]
    [SerializeField] bool logSuccessfulActions;
    [Tooltip("If enabled, blocked UI backend actions are written to GameDebug.")]
    [SerializeField] bool logBlockedActions = true;

    SituationEventSignalUIScreenSnapshot currentSnapshot = new SituationEventSignalUIScreenSnapshot();
    SituationEventSignalUIActionResult lastResult = new SituationEventSignalUIActionResult();
    PlayerSituationEventSignalLog subscribedLog;

    public SituationEventSignalUIScreenSnapshot CurrentSnapshot => currentSnapshot;
    public SituationEventSignalUIActionResult LastResult => lastResult;
    public SituationEventSignalController Controller => controller;
    public SituationEventSignalProfileDefinition ProfileOverride => profileOverride;
    public string SelectedRuleId => selectedRuleId;
    public int MaxRuleRows => Mathf.Max(0, maxRuleRows);
    public int MaxHistoryRows => Mathf.Max(0, maxHistoryRows);
    public event Action<SituationEventSignalUIScreenSnapshot> OnSnapshotChanged;
    public event Action<SituationEventSignalUIActionResult> OnActionResult;

    void OnEnable() {
        SubscribeToLog();
    }

    void Start() {
        if(refreshOnStart) {
            Refresh();
        }
    }

    void OnDisable() {
        UnsubscribeFromLog();
    }

    [ContextMenu("Refresh Situation Signal Snapshot")]
    public SituationEventSignalUIScreenSnapshot RefreshFromContextMenu() {
        return Refresh();
    }

    [ContextMenu("Evaluate Manual Situation Signals")]
    public void EvaluateManualFromContextMenu() {
        TryEvaluate(SituationEventSignalTrigger.Manual, out _);
    }

    public SituationEventSignalUIScreenSnapshot Refresh() {
        SubscribeToLog();
        var player = ResolvePlayer();
        var resolvedController = ResolveController();
        var profile = ResolveProfile(resolvedController);
        var log = player != null ? player.GetComponent<PlayerSituationEventSignalLog>() : null;
        var rules = BuildRuleRows(player, profile, log).ToList();
        var history = BuildHistoryRows(log, profile).ToList();

        currentSnapshot = new SituationEventSignalUIScreenSnapshot {
            hasPlayer = player != null,
            playerName = player != null ? player.name : string.Empty,
            hasController = resolvedController != null,
            controllerName = resolvedController != null ? resolvedController.name : string.Empty,
            hasProfile = profile != null,
            profileId = profile != null ? profile.Id : string.Empty,
            profileName = profile != null ? profile.DisplayName : string.Empty,
            profileDescription = profile != null ? profile.Description : string.Empty,
            profileTags = profile != null ? profile.Tags.ToList() : new List<string>(),
            previewTrigger = previewTrigger,
            selectedRuleId = selectedRuleId,
            includeDisabledRules = includeDisabledRules,
            includeBlockedHistory = includeBlockedHistory,
            showOnlySelectedProfileHistory = showOnlySelectedProfileHistory,
            day = TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1,
            hour = TimeSystem.i != null ? Mathf.Clamp(TimeSystem.i.Hour, 0, 23) : 0,
            absoluteHour = GetCurrentAbsoluteHour(),
            ruleCount = rules.Count,
            evaluatableRuleCount = rules.Count(row => row != null && row.canEvaluatePreview),
            blockedRuleCount = rules.Count(row => row != null && !row.canEvaluatePreview),
            historyCount = history.Count,
            rules = rules,
            history = history,
            lastResult = lastResult
        };

        OnSnapshotChanged?.Invoke(currentSnapshot);
        return currentSnapshot;
    }

    public bool TryEvaluate(SituationEventSignalTrigger trigger, out string feedback) {
        var resolvedController = ResolveController();
        if(resolvedController == null) {
            return Block("No situation event signal controller is available.", out feedback);
        }

        GetSignalLog(ResolvePlayer(), createMissingSignalLogForActions);
        var results = resolvedController.Evaluate(trigger);
        int started = results.Sum(result => result != null ? result.startedEvents : 0);
        int rolled = results.Sum(result => result != null ? result.rolledPools : 0);
        int blocked = results.Count(result => result != null && result.blocked);
        feedback = results.Count > 0
            ? $"Evaluated {results.Count} rule(s): {rolled} pool(s), {started} event(s), {blocked} blocked."
            : "No signal rules evaluated.";

        return Succeed(SituationEventSignalUIActionResultKind.Evaluated, feedback, out feedback);
    }

    public bool TrySetSelectedRule(string ruleId, out string feedback) {
        selectedRuleId = ruleId ?? string.Empty;
        string label = string.IsNullOrWhiteSpace(selectedRuleId) ? "all rules" : selectedRuleId;
        return Succeed(SituationEventSignalUIActionResultKind.RuleFilterChanged, $"Showing {label}.", out feedback);
    }

    IEnumerable<SituationEventSignalRuleUIRow> BuildRuleRows(PlayerController player, SituationEventSignalProfileDefinition profile, PlayerSituationEventSignalLog log) {
        if(profile == null || profile.Rules == null) {
            yield break;
        }

        int emitted = 0;
        foreach(var rule in profile.Rules) {
            if(rule == null) {
                continue;
            }

            if(!includeDisabledRules && !rule.Enabled) {
                continue;
            }

            if(!string.IsNullOrWhiteSpace(selectedRuleId)
                && !string.Equals(rule.RuleId, selectedRuleId, StringComparison.OrdinalIgnoreCase)) {
                continue;
            }

            if(maxRuleRows > 0 && emitted >= maxRuleRows) {
                yield break;
            }

            var row = BuildRuleRow(player, profile, rule, log);
            emitted++;
            yield return row;
        }
    }

    SituationEventSignalRuleUIRow BuildRuleRow(PlayerController player, SituationEventSignalProfileDefinition profile, SituationEventSignalRule rule, PlayerSituationEventSignalLog log) {
        var latestAny = log != null ? log.GetLatest(profile.Id, rule.RuleId, includeBlocked: true) : null;
        var latestSuccess = log != null ? log.GetLatest(profile.Id, rule.RuleId, includeBlocked: false) : null;
        int remainingCooldownHours = CalculateRemainingCooldown(rule, latestSuccess);
        bool canEvaluate = CanPreviewEvaluate(player, rule, remainingCooldownHours, out var blockedReason);

        return new SituationEventSignalRuleUIRow {
            ruleId = rule.RuleId,
            displayName = rule.DisplayName,
            mode = rule.Mode,
            modeName = rule.Mode.ToString(),
            enabled = rule.Enabled,
            acceptsPreviewTrigger = rule.AcceptsTrigger(previewTrigger),
            evaluateChance = rule.EvaluateChance,
            cooldownHours = rule.CooldownHours,
            remainingCooldownHours = remainingCooldownHours,
            poolCount = rule.Pools != null ? rule.Pools.Count : 0,
            extraRequirementCount = rule.ExtraRequirements != null ? rule.ExtraRequirements.Count : 0,
            triggerNames = rule.Triggers != null ? rule.Triggers.Select(trigger => trigger.ToString()).ToList() : new List<string>(),
            sourceId = rule.ResolveSourceId(profile),
            sourceName = rule.ResolveSourceName(profile),
            canEvaluatePreview = canEvaluate,
            blockedReason = blockedReason,
            latestMessage = latestAny != null ? latestAny.message : string.Empty,
            latestWasBlocked = latestAny != null && latestAny.blocked,
            latestDay = latestAny != null ? latestAny.day : 0,
            latestAbsoluteHour = latestAny != null ? latestAny.absoluteHour : -1
        };
    }

    bool CanPreviewEvaluate(PlayerController player, SituationEventSignalRule rule, int remainingCooldownHours, out string blockedReason) {
        if(rule == null) {
            blockedReason = "Signal rule is missing.";
            return false;
        }

        if(!rule.Enabled) {
            blockedReason = "Signal rule is disabled.";
            return false;
        }

        if(!rule.AcceptsTrigger(previewTrigger)) {
            blockedReason = "Preview trigger does not match this rule.";
            return false;
        }

        if(player == null) {
            blockedReason = "A player is required.";
            return false;
        }

        if(rule.Pools == null || rule.Pools.Count == 0) {
            blockedReason = "Signal rule has no pools.";
            return false;
        }

        if(remainingCooldownHours > 0) {
            blockedReason = $"Rule is on cooldown for {remainingCooldownHours} hour(s).";
            return false;
        }

        if(!rule.SignalConditionMet(player, out blockedReason)) {
            return false;
        }

        foreach(var requirement in rule.ExtraRequirements) {
            if(requirement != null && !requirement.IsMet(player)) {
                blockedReason = requirement.FailureMessage;
                return false;
            }
        }

        blockedReason = string.Empty;
        return true;
    }

    IEnumerable<SituationEventSignalHistoryUIRow> BuildHistoryRows(PlayerSituationEventSignalLog log, SituationEventSignalProfileDefinition profile) {
        if(log == null || log.Records == null) {
            yield break;
        }

        IEnumerable<SituationEventSignalRecord> query = log.Records.Where(record => record != null);
        if(!includeBlockedHistory) {
            query = query.Where(record => !record.blocked);
        }

        if(showOnlySelectedProfileHistory && profile != null) {
            query = query.Where(record => string.Equals(record.profileId, profile.Id, StringComparison.OrdinalIgnoreCase));
        }

        if(!string.IsNullOrWhiteSpace(selectedRuleId)) {
            query = query.Where(record => string.Equals(record.ruleId, selectedRuleId, StringComparison.OrdinalIgnoreCase));
        }

        query = newestHistoryFirst
            ? query.OrderByDescending(record => record.absoluteHour)
            : query.OrderBy(record => record.absoluteHour);

        int emitted = 0;
        foreach(var record in query) {
            if(maxHistoryRows > 0 && emitted >= maxHistoryRows) {
                yield break;
            }

            emitted++;
            yield return SituationEventSignalHistoryUIRow.FromRecord(record);
        }
    }

    int CalculateRemainingCooldown(SituationEventSignalRule rule, SituationEventSignalRecord latestSuccess) {
        if(rule == null || latestSuccess == null || rule.CooldownHours <= 0) {
            return 0;
        }

        int elapsed = GetCurrentAbsoluteHour() - latestSuccess.absoluteHour;
        return Mathf.Max(0, rule.CooldownHours - elapsed);
    }

    void SubscribeToLog() {
        if(!refreshWhenLogChanges) {
            return;
        }

        var player = ResolvePlayer();
        var log = player != null ? player.GetComponent<PlayerSituationEventSignalLog>() : null;
        if(log == subscribedLog) {
            return;
        }

        UnsubscribeFromLog();
        subscribedLog = log;
        if(subscribedLog != null) {
            subscribedLog.OnSignalRecorded += HandleSignalRecorded;
        }
    }

    void UnsubscribeFromLog() {
        if(subscribedLog != null) {
            subscribedLog.OnSignalRecorded -= HandleSignalRecorded;
        }
        subscribedLog = null;
    }

    void HandleSignalRecorded(SituationEventSignalRecord record) {
        Refresh();
    }

    PlayerController ResolvePlayer() {
        if(playerOverride != null) {
            return playerOverride;
        }

        if(PlayerController.i != null) {
            return PlayerController.i;
        }

        return FindAnyObjectByType<PlayerController>(FindObjectsInactive.Include);
    }

    SituationEventSignalController ResolveController() {
        if(controller != null) {
            return controller;
        }

        var profile = profileOverride;
        var controllers = FindObjectsByType<SituationEventSignalController>(FindObjectsInactive.Include);
        if(profile == null) {
            return controllers.FirstOrDefault();
        }

        return controllers.FirstOrDefault(candidate => candidate != null && candidate.Profile == profile);
    }

    SituationEventSignalProfileDefinition ResolveProfile(SituationEventSignalController resolvedController) {
        return profileOverride != null ? profileOverride : resolvedController != null ? resolvedController.Profile : null;
    }

    PlayerSituationEventSignalLog GetSignalLog(PlayerController player, bool createIfMissing) {
        if(player == null) {
            return null;
        }

        var log = player.GetComponent<PlayerSituationEventSignalLog>();
        if(log == null && createIfMissing) {
            log = player.gameObject.AddComponent<PlayerSituationEventSignalLog>();
        }
        return log;
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    bool Succeed(SituationEventSignalUIActionResultKind kind, string message, out string feedback) {
        lastResult = new SituationEventSignalUIActionResult {
            kind = kind,
            success = true,
            message = message,
            day = TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1,
            absoluteHour = GetCurrentAbsoluteHour()
        };

        feedback = message;
        if(logSuccessfulActions) {
            GameDebugLogger.Ensure().Record(GameDebugSeverity.Info, GameDebugCategory.UI, message, this, "SituationEventSignalUIManager");
        }

        OnActionResult?.Invoke(lastResult);
        if(refreshAfterActions) {
            Refresh();
        }
        return true;
    }

    bool Block(string message, out string feedback) {
        lastResult = new SituationEventSignalUIActionResult {
            kind = SituationEventSignalUIActionResultKind.Blocked,
            success = false,
            message = message,
            day = TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1,
            absoluteHour = GetCurrentAbsoluteHour()
        };

        feedback = message;
        if(logBlockedActions) {
            GameDebugLogger.Ensure().Record(GameDebugSeverity.Warning, GameDebugCategory.UI, message, this, "SituationEventSignalUIManager");
        }

        OnActionResult?.Invoke(lastResult);
        if(refreshAfterActions) {
            Refresh();
        }
        return false;
    }
}

[Serializable]
public class SituationEventSignalUIScreenSnapshot {
    [Tooltip("True when a player is available for signal checks.")]
    public bool hasPlayer;
    [Tooltip("Resolved player object name.")]
    public string playerName;
    [Tooltip("True when a signal controller is available for evaluate actions.")]
    public bool hasController;
    [Tooltip("Resolved signal controller object name.")]
    public string controllerName;
    [Tooltip("True when a signal profile is available.")]
    public bool hasProfile;
    [Tooltip("Selected signal profile id.")]
    public string profileId;
    [Tooltip("Selected signal profile display name.")]
    public string profileName;
    [Tooltip("Selected signal profile description.")]
    public string profileDescription;
    [Tooltip("Selected signal profile tags.")]
    public List<string> profileTags = new List<string>();
    [Tooltip("Trigger used for preview checks.")]
    public SituationEventSignalTrigger previewTrigger;
    [Tooltip("Current rule id filter.")]
    public string selectedRuleId;
    [Tooltip("If enabled, disabled rules are included in Rows.")]
    public bool includeDisabledRules;
    [Tooltip("If enabled, blocked records are included in History.")]
    public bool includeBlockedHistory;
    [Tooltip("If enabled, history is limited to the selected profile.")]
    public bool showOnlySelectedProfileHistory;
    [Tooltip("Current in-game day.")]
    public int day;
    [Tooltip("Current in-game hour.")]
    public int hour;
    [Tooltip("Current absolute in-game hour.")]
    public int absoluteHour;
    [Tooltip("Number of visible rule rows.")]
    public int ruleCount;
    [Tooltip("Number of visible rule rows that can evaluate in preview.")]
    public int evaluatableRuleCount;
    [Tooltip("Number of visible rule rows blocked in preview.")]
    public int blockedRuleCount;
    [Tooltip("Number of visible history rows.")]
    public int historyCount;
    [Tooltip("Signal rules available to UI.")]
    public List<SituationEventSignalRuleUIRow> rules = new List<SituationEventSignalRuleUIRow>();
    [Tooltip("Signal evaluation history available to UI.")]
    public List<SituationEventSignalHistoryUIRow> history = new List<SituationEventSignalHistoryUIRow>();
    [Tooltip("Most recent UI action result.")]
    public SituationEventSignalUIActionResult lastResult = new SituationEventSignalUIActionResult();
}

[Serializable]
public class SituationEventSignalRuleUIRow {
    [Tooltip("Rule id.")]
    public string ruleId;
    [Tooltip("Rule display name.")]
    public string displayName;
    [Tooltip("Rule condition mode.")]
    public SituationEventSignalMode mode;
    [Tooltip("Readable rule condition mode.")]
    public string modeName;
    [Tooltip("True when this rule is enabled in its profile.")]
    public bool enabled;
    [Tooltip("True when this rule accepts the current preview trigger.")]
    public bool acceptsPreviewTrigger;
    [Tooltip("Chance used by the runtime rule evaluation.")]
    public float evaluateChance;
    [Tooltip("Rule cooldown in in-game hours.")]
    public int cooldownHours;
    [Tooltip("Remaining cooldown in in-game hours.")]
    public int remainingCooldownHours;
    [Tooltip("Number of event pools assigned to this rule.")]
    public int poolCount;
    [Tooltip("Number of extra requirements assigned to this rule.")]
    public int extraRequirementCount;
    [Tooltip("Trigger names accepted by this rule. Empty means every trigger.")]
    public List<string> triggerNames = new List<string>();
    [Tooltip("Source id resolved by this rule.")]
    public string sourceId;
    [Tooltip("Source name resolved by this rule.")]
    public string sourceName;
    [Tooltip("Preview result that ignores random chance but checks player, pools, trigger, cooldown and conditions.")]
    public bool canEvaluatePreview;
    [Tooltip("Reason why preview evaluation is blocked.")]
    public string blockedReason;
    [Tooltip("Latest log message for this rule.")]
    public string latestMessage;
    [Tooltip("True when the latest log entry for this rule was blocked.")]
    public bool latestWasBlocked;
    [Tooltip("In-game day of the latest log entry.")]
    public int latestDay;
    [Tooltip("Absolute in-game hour of the latest log entry.")]
    public int latestAbsoluteHour;
}

[Serializable]
public class SituationEventSignalHistoryUIRow {
    [Tooltip("Signal profile id.")]
    public string profileId;
    [Tooltip("Signal profile display name.")]
    public string profileName;
    [Tooltip("Signal rule id.")]
    public string ruleId;
    [Tooltip("Signal rule display name.")]
    public string ruleName;
    [Tooltip("Trigger that evaluated this signal.")]
    public SituationEventSignalTrigger trigger;
    [Tooltip("Source id used by this evaluation.")]
    public string sourceId;
    [Tooltip("Number of pools rolled by this evaluation.")]
    public int rolledPools;
    [Tooltip("Number of events started by this evaluation.")]
    public int startedEvents;
    [Tooltip("True when this evaluation was blocked.")]
    public bool blocked;
    [Tooltip("Readable result/failure message.")]
    public string message;
    [Tooltip("In-game day when this evaluation happened.")]
    public int day;
    [Tooltip("Absolute in-game hour when this evaluation happened.")]
    public int absoluteHour;

    public static SituationEventSignalHistoryUIRow FromRecord(SituationEventSignalRecord record) {
        if(record == null) {
            return new SituationEventSignalHistoryUIRow();
        }

        return new SituationEventSignalHistoryUIRow {
            profileId = record.profileId,
            profileName = record.profileName,
            ruleId = record.ruleId,
            ruleName = record.ruleName,
            trigger = record.trigger,
            sourceId = record.sourceId,
            rolledPools = record.rolledPools,
            startedEvents = record.startedEvents,
            blocked = record.blocked,
            message = record.message,
            day = record.day,
            absoluteHour = record.absoluteHour
        };
    }
}

[Serializable]
public class SituationEventSignalUIActionResult {
    [Tooltip("Kind of UI backend action.")]
    public SituationEventSignalUIActionResultKind kind;
    [Tooltip("True when the action succeeded.")]
    public bool success;
    [Tooltip("Readable action result message.")]
    public string message;
    [Tooltip("In-game day when this result was produced.")]
    public int day;
    [Tooltip("Absolute in-game hour when this result was produced.")]
    public int absoluteHour;
}
