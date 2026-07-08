using System.Collections;
using UnityEngine;

public enum RoleActivityBoardSourceAction {
    ViewOnly,
    RunConfiguredEntry,
    RunFirstAvailableEntry
}

public class RoleActivityBoardSource : MonoBehaviour, Interactable, IPlayerTriggerable, IOverworldInteractionInfoProvider {
    [Header("Board")]
    [Tooltip("Role activity board definition exposed by this scene source.")]
    [SerializeField] RoleActivityBoardDefinition board = null;
    [Tooltip("Optional stable source id for logs, repeat rules and event payloads. Empty uses board id or GameObject name.")]
    [SerializeField] string sourceId = string.Empty;
    [Tooltip("Name shown by prompts and saved in logs. Empty uses board display name or GameObject name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Optional player override. Empty uses the interacting player or PlayerController.i.")]
    [SerializeField] PlayerController playerOverride = null;

    [Header("Context")]
    [Tooltip("Optional region context passed into situation events, pools and board snapshots.")]
    [SerializeField] RegionInfoDefinition regionContext = null;
    [Tooltip("Optional activity zone context. Empty falls back to PlayerActivityContext.CurrentZone.")]
    [SerializeField] ActivityZoneDefinition zoneContext = null;
    [Tooltip("If enabled, locked entries are included in snapshots unless their row hides locked state.")]
    [SerializeField] bool includeLockedEntries = true;

    [Header("Interaction")]
    [Tooltip("Action performed when the player interacts with this source.")]
    [SerializeField] RoleActivityBoardSourceAction interactAction = RoleActivityBoardSourceAction.ViewOnly;
    [Tooltip("Entry id used when Interact Action is Run Configured Entry.")]
    [SerializeField] string entryIdToRun = string.Empty;
    [Tooltip("Action label shown by overworld prompts.")]
    [SerializeField] string actionName = "Check";
    [Tooltip("Short prompt text shown by overworld interaction UI. Empty uses the board description.")]
    [TextArea]
    [SerializeField] string promptText = string.Empty;

    [Header("Trigger")]
    [Tooltip("If enabled, entering this trigger runs Trigger Action.")]
    [SerializeField] bool runOnPlayerTrigger;
    [Tooltip("Action performed when this source is triggered.")]
    [SerializeField] RoleActivityBoardSourceAction triggerAction = RoleActivityBoardSourceAction.ViewOnly;
    [Tooltip("If enabled, repeated player triggers can call this source more than once.")]
    [SerializeField] bool triggerRepeatedly = true;

    [Header("Feedback")]
    [Tooltip("If enabled, view/run results use DialogManager when available.")]
    [SerializeField] bool showDialogResult = true;
    [Tooltip("If enabled, source views and run attempts are recorded in PlayerRoleActivityBoardLog.")]
    [SerializeField] bool recordHistory = true;
    [Tooltip("If enabled, source attempts are written to GameDebugLogger.")]
    [SerializeField] bool logAttempts;

    public RoleActivityBoardDefinition Board => board;
    public string SourceId => !string.IsNullOrWhiteSpace(sourceId)
        ? sourceId
        : board != null ? board.ResolveBoardSourceId(null) : name;
    public string DisplayName => !string.IsNullOrWhiteSpace(displayName)
        ? displayName
        : board != null ? board.DisplayName : name;
    public RegionInfoDefinition RegionContext => regionContext;
    public ActivityZoneDefinition ZoneContext => zoneContext;
    public bool IncludeLockedEntries => includeLockedEntries;
    public RoleActivityBoardSourceAction InteractAction => interactAction;
    public RoleActivityBoardSourceAction TriggerAction => triggerAction;
    public string EntryIdToRun => entryIdToRun;
    public bool RunOnPlayerTrigger => runOnPlayerTrigger;
    public bool TriggerRepeatedly => triggerRepeatedly;

    public IEnumerator Interact(Transform initiator) {
        var player = ResolvePlayer(initiator);
        yield return Apply(player, interactAction);
    }

    public void OnPlayerTriggered(PlayerController player) {
        if(runOnPlayerTrigger) {
            StartCoroutine(Apply(ResolvePlayer(player != null ? player.transform : null), triggerAction));
        }
    }

    [ContextMenu("Record View")]
    public void RecordViewFromContextMenu() {
        RecordView(ResolvePlayer(null));
    }

    [ContextMenu("Run Configured Entry")]
    public void RunConfiguredEntryFromContextMenu() {
        TryRunEntry(entryIdToRun, ResolvePlayer(null), out _);
    }

    [ContextMenu("Run First Available Entry")]
    public void RunFirstAvailableFromContextMenu() {
        TryRunFirstAvailable(ResolvePlayer(null), out _);
    }

    public RoleActivityBoardSnapshot GetSnapshot(PlayerController player = null) {
        player = player != null ? player : ResolvePlayer(null);
        if(board == null) {
            return new RoleActivityBoardSnapshot {
                boardId = string.Empty,
                boardName = DisplayName,
                description = "No role activity board assigned.",
                sourceId = SourceId,
                sourceName = DisplayName,
                usable = false,
                failureMessage = "No role activity board assigned."
            };
        }

        return board.BuildSnapshot(player, SourceId, DisplayName, ResolveRegion(), ResolveZone(), includeLockedEntries, this);
    }

    public bool TryRunEntry(string entryId, PlayerController player, out RoleActivityBoardRunResult result) {
        player = player != null ? player : ResolvePlayer(null);
        if(board == null) {
            result = RoleActivityBoardRunResult.Blocked(null, null, SourceId, "No role activity board assigned.");
            RecordRun(player, result);
            Log(result.message, GameDebugSeverity.Warning);
            return false;
        }

        bool success = board.TryRunEntry(player, entryId, SourceId, DisplayName, ResolveRegion(), ResolveZone(), this, out result);
        RecordRun(player, result);
        Log(result != null ? result.message : "Role activity board entry finished.", success ? GameDebugSeverity.Info : GameDebugSeverity.Warning);
        return success;
    }

    public bool TryRunFirstAvailable(PlayerController player, out RoleActivityBoardRunResult result) {
        player = player != null ? player : ResolvePlayer(null);
        if(board == null) {
            result = RoleActivityBoardRunResult.Blocked(null, null, SourceId, "No role activity board assigned.");
            RecordRun(player, result);
            Log(result.message, GameDebugSeverity.Warning);
            return false;
        }

        bool success = board.TryRunFirstAvailable(player, SourceId, DisplayName, ResolveRegion(), ResolveZone(), this, out result);
        RecordRun(player, result);
        Log(result != null ? result.message : "Role activity board entry finished.", success ? GameDebugSeverity.Info : GameDebugSeverity.Warning);
        return success;
    }

    public bool TryGetInteractionInfo(PlayerController player, out OverworldInteractionInfo info) {
        string blockedMessage = null;
        bool canInteract = board != null && board.CanUse(player, out blockedMessage);

        info = new OverworldInteractionInfo {
            TargetName = DisplayName,
            ActionName = string.IsNullOrWhiteSpace(actionName) ? "Check" : actionName,
            Description = !string.IsNullOrWhiteSpace(promptText) ? promptText : board != null ? board.Description : string.Empty,
            PermissionHint = ResolveZone() != null ? ResolveZone().DisplayName : string.Empty,
            BlockedMessage = blockedMessage,
            CanInteract = canInteract,
            Activity = null,
            Zone = ResolveZone(),
            Source = this
        };
        return true;
    }

    IEnumerator Apply(PlayerController player, RoleActivityBoardSourceAction action) {
        if(board == null) {
            yield return ShowFeedback("No role activity board assigned.");
            yield break;
        }

        switch(action) {
            case RoleActivityBoardSourceAction.RunConfiguredEntry:
                TryRunEntry(entryIdToRun, player, out var configuredResult);
                yield return ShowFeedback(configuredResult != null ? configuredResult.message : "Board entry finished.");
                break;
            case RoleActivityBoardSourceAction.RunFirstAvailableEntry:
                TryRunFirstAvailable(player, out var firstResult);
                yield return ShowFeedback(firstResult != null ? firstResult.message : "No available board entry found.");
                break;
            default:
                var snapshot = GetSnapshot(player);
                RecordView(player);
                Log($"{DisplayName} viewed with {snapshot.rows.Count} visible row(s).", GameDebugSeverity.Info);
                yield return ShowFeedback(BuildViewMessage(snapshot));
                break;
        }
    }

    void RecordView(PlayerController player) {
        if(!recordHistory || player == null || board == null) {
            return;
        }

        var log = player.GetComponent<PlayerRoleActivityBoardLog>() ?? player.gameObject.AddComponent<PlayerRoleActivityBoardLog>();
        log.RecordView(board, SourceId, DisplayName, ResolveRegion(), ResolveZone());
    }

    void RecordRun(PlayerController player, RoleActivityBoardRunResult result) {
        if(!recordHistory || player == null || result == null) {
            return;
        }

        var log = player.GetComponent<PlayerRoleActivityBoardLog>() ?? player.gameObject.AddComponent<PlayerRoleActivityBoardLog>();
        log.RecordRun(result, ResolveRegion(), ResolveZone());
    }

    PlayerController ResolvePlayer(Transform initiator) {
        if(playerOverride != null) {
            return playerOverride;
        }

        var player = initiator != null ? initiator.GetComponent<PlayerController>() : null;
        return player != null ? player : PlayerController.i;
    }

    RegionInfoDefinition ResolveRegion() {
        return regionContext;
    }

    ActivityZoneDefinition ResolveZone() {
        return zoneContext != null ? zoneContext : PlayerActivityContext.CurrentZone;
    }

    string BuildViewMessage(RoleActivityBoardSnapshot snapshot) {
        if(snapshot == null) {
            return "Board snapshot is unavailable.";
        }

        if(!snapshot.usable && !string.IsNullOrWhiteSpace(snapshot.failureMessage)) {
            return snapshot.failureMessage;
        }

        return snapshot.rows.Count == 1
            ? $"{DisplayName} has 1 available option."
            : $"{DisplayName} has {snapshot.rows.Count} visible option(s).";
    }

    IEnumerator ShowFeedback(string message) {
        if(showDialogResult && DialogManager.i != null && !string.IsNullOrWhiteSpace(message)) {
            yield return DialogManager.i.ShowDialogText(message);
        }
    }

    void Log(string message, GameDebugSeverity severity) {
        if(!logAttempts && severity < GameDebugSeverity.Warning) {
            return;
        }

        GameDebugLogger.Ensure().Record(severity, GameDebugCategory.Activity, message, this, "RoleActivityBoardSource");
    }
}
