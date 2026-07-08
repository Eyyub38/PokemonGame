using System.Collections;
using UnityEngine;

public enum CampStationSourceAction {
    ViewOnly,
    RunConfiguredAction,
    RunFirstAvailableAction
}

public class CampStationSource : MonoBehaviour, Interactable, IPlayerTriggerable, IOverworldInteractionInfoProvider {
    [Header("Station")]
    [Tooltip("Camp station definition exposed by this scene source.")]
    [SerializeField] CampStationDefinition station = null;
    [Tooltip("Optional stable source id for logs, repeat rules and event payloads. Empty uses station id or GameObject name.")]
    [SerializeField] string sourceId = string.Empty;
    [Tooltip("Name shown by prompts and saved in logs. Empty uses station display name or GameObject name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Optional player override. Empty uses the interacting player or PlayerController.i.")]
    [SerializeField] PlayerController playerOverride = null;

    [Header("Context")]
    [Tooltip("Optional region context passed into situation events, pools and station snapshots.")]
    [SerializeField] RegionInfoDefinition regionContext = null;
    [Tooltip("Optional activity zone context. Empty falls back to PlayerActivityContext.CurrentZone.")]
    [SerializeField] ActivityZoneDefinition zoneContext = null;
    [Tooltip("If enabled, locked actions are included in snapshots unless their row hides locked state.")]
    [SerializeField] bool includeLockedActions = true;

    [Header("Interaction")]
    [Tooltip("Action performed when the player interacts with this source.")]
    [SerializeField] CampStationSourceAction interactAction = CampStationSourceAction.ViewOnly;
    [Tooltip("Action id used when Interact Action is Run Configured Action.")]
    [SerializeField] string actionIdToRun = string.Empty;
    [Tooltip("Action label shown by overworld prompts.")]
    [SerializeField] string actionName = "Camp";
    [Tooltip("Short prompt text shown by overworld interaction UI. Empty uses the station description.")]
    [TextArea]
    [SerializeField] string promptText = string.Empty;

    [Header("Trigger")]
    [Tooltip("If enabled, entering this trigger runs Trigger Action.")]
    [SerializeField] bool runOnPlayerTrigger;
    [Tooltip("Action performed when this source is triggered.")]
    [SerializeField] CampStationSourceAction triggerAction = CampStationSourceAction.ViewOnly;
    [Tooltip("If enabled, repeated player triggers can call this source more than once.")]
    [SerializeField] bool triggerRepeatedly = true;

    [Header("Feedback")]
    [Tooltip("If enabled, view/run results use DialogManager when available.")]
    [SerializeField] bool showDialogResult = true;
    [Tooltip("If enabled, source views and run attempts are recorded in PlayerCampStationLog.")]
    [SerializeField] bool recordHistory = true;
    [Tooltip("If enabled, source attempts are written to GameDebugLogger.")]
    [SerializeField] bool logAttempts;

    public CampStationDefinition Station => station;
    public string SourceId => !string.IsNullOrWhiteSpace(sourceId)
        ? sourceId
        : station != null ? station.ResolveStationSourceId(null) : name;
    public string DisplayName => !string.IsNullOrWhiteSpace(displayName)
        ? displayName
        : station != null ? station.DisplayName : name;
    public RegionInfoDefinition RegionContext => regionContext;
    public ActivityZoneDefinition ZoneContext => zoneContext;
    public bool IncludeLockedActions => includeLockedActions;
    public CampStationSourceAction InteractAction => interactAction;
    public CampStationSourceAction TriggerAction => triggerAction;
    public string ActionIdToRun => actionIdToRun;
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

    [ContextMenu("Run Configured Action")]
    public void RunConfiguredActionFromContextMenu() {
        TryRunAction(actionIdToRun, ResolvePlayer(null), out _);
    }

    [ContextMenu("Run First Available Action")]
    public void RunFirstAvailableFromContextMenu() {
        TryRunFirstAvailable(ResolvePlayer(null), out _);
    }

    public CampStationSnapshot GetSnapshot(PlayerController player = null) {
        player = player != null ? player : ResolvePlayer(null);
        if(station == null) {
            return new CampStationSnapshot {
                stationId = string.Empty,
                stationName = DisplayName,
                description = "No camp station assigned.",
                sourceId = SourceId,
                sourceName = DisplayName,
                usable = false,
                failureMessage = "No camp station assigned."
            };
        }

        return station.BuildSnapshot(player, SourceId, DisplayName, ResolveRegion(), ResolveZone(), includeLockedActions, this);
    }

    public bool TryRunAction(string actionId, PlayerController player, out CampStationRunResult result) {
        player = player != null ? player : ResolvePlayer(null);
        if(station == null) {
            result = CampStationRunResult.Blocked(null, null, SourceId, "No camp station assigned.");
            RecordRun(player, result);
            Log(result.message, GameDebugSeverity.Warning);
            return false;
        }

        bool success = station.TryRunAction(player, actionId, SourceId, DisplayName, ResolveRegion(), ResolveZone(), this, out result);
        RecordRun(player, result);
        Log(result != null ? result.message : "Camp station action finished.", success ? GameDebugSeverity.Info : GameDebugSeverity.Warning);
        return success;
    }

    public bool TryRunFirstAvailable(PlayerController player, out CampStationRunResult result) {
        player = player != null ? player : ResolvePlayer(null);
        if(station == null) {
            result = CampStationRunResult.Blocked(null, null, SourceId, "No camp station assigned.");
            RecordRun(player, result);
            Log(result.message, GameDebugSeverity.Warning);
            return false;
        }

        bool success = station.TryRunFirstAvailable(player, SourceId, DisplayName, ResolveRegion(), ResolveZone(), this, out result);
        RecordRun(player, result);
        Log(result != null ? result.message : "Camp station action finished.", success ? GameDebugSeverity.Info : GameDebugSeverity.Warning);
        return success;
    }

    public bool TryGetInteractionInfo(PlayerController player, out OverworldInteractionInfo info) {
        string blockedMessage = null;
        bool canInteract = station != null && station.CanUse(player, ResolveZone(), out blockedMessage);

        info = new OverworldInteractionInfo {
            TargetName = DisplayName,
            ActionName = string.IsNullOrWhiteSpace(actionName) ? "Camp" : actionName,
            Description = !string.IsNullOrWhiteSpace(promptText) ? promptText : station != null ? station.Description : string.Empty,
            PermissionHint = ResolveZone() != null ? ResolveZone().DisplayName : string.Empty,
            BlockedMessage = blockedMessage,
            CanInteract = canInteract,
            Activity = null,
            Zone = ResolveZone(),
            Source = this
        };
        return true;
    }

    IEnumerator Apply(PlayerController player, CampStationSourceAction action) {
        if(station == null) {
            yield return ShowFeedback("No camp station assigned.");
            yield break;
        }

        switch(action) {
            case CampStationSourceAction.RunConfiguredAction:
                TryRunAction(actionIdToRun, player, out var configuredResult);
                yield return ShowFeedback(configuredResult != null ? configuredResult.message : "Camp station action finished.");
                break;
            case CampStationSourceAction.RunFirstAvailableAction:
                TryRunFirstAvailable(player, out var firstResult);
                yield return ShowFeedback(firstResult != null ? firstResult.message : "No available camp station action found.");
                break;
            default:
                var snapshot = GetSnapshot(player);
                RecordView(player);
                Log($"{DisplayName} viewed with {snapshot.rows.Count} visible action(s).", GameDebugSeverity.Info);
                yield return ShowFeedback(BuildViewMessage(snapshot));
                break;
        }
    }

    void RecordView(PlayerController player) {
        if(!recordHistory || player == null || station == null) {
            return;
        }

        var log = player.GetComponent<PlayerCampStationLog>() ?? player.gameObject.AddComponent<PlayerCampStationLog>();
        log.RecordView(station, SourceId, DisplayName, ResolveRegion(), ResolveZone());
    }

    void RecordRun(PlayerController player, CampStationRunResult result) {
        if(!recordHistory || player == null || result == null) {
            return;
        }

        var log = player.GetComponent<PlayerCampStationLog>() ?? player.gameObject.AddComponent<PlayerCampStationLog>();
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

    string BuildViewMessage(CampStationSnapshot snapshot) {
        if(snapshot == null) {
            return "Camp station snapshot is unavailable.";
        }

        if(!snapshot.usable && !string.IsNullOrWhiteSpace(snapshot.failureMessage)) {
            return snapshot.failureMessage;
        }

        return snapshot.rows.Count == 1
            ? $"{DisplayName} has 1 available action."
            : $"{DisplayName} has {snapshot.rows.Count} visible action(s).";
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

        GameDebugLogger.Ensure().Record(severity, GameDebugCategory.Activity, message, this, "CampStationSource");
    }
}
