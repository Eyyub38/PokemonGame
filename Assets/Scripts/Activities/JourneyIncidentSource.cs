using System.Collections;
using UnityEngine;

public enum JourneyIncidentSourceAction {
    ViewOnly,
    ActivateConfiguredIncident,
    RollBoard,
    ResolveConfiguredIncident,
    ExpireConfiguredIncident
}

public class JourneyIncidentSource : MonoBehaviour, Interactable, IPlayerTriggerable, IOverworldInteractionInfoProvider {
    [Header("Targets")]
    [Tooltip("Specific incident used by Activate, Resolve and Expire actions.")]
    [SerializeField] JourneyIncidentDefinition incident = null;
    [Tooltip("Incident board rolled by Roll Board actions or used for snapshots.")]
    [SerializeField] JourneyIncidentBoardDefinition board = null;
    [Tooltip("Optional stable source id for logs, repeat rules and event payloads. Empty uses board, incident or GameObject name.")]
    [SerializeField] string sourceId = string.Empty;
    [Tooltip("Name shown by prompts and saved in logs. Empty uses board, incident or GameObject name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Optional player override. Empty uses the interacting player or PlayerController.i.")]
    [SerializeField] PlayerController playerOverride = null;

    [Header("Context")]
    [Tooltip("Optional region context passed into incident filters and snapshots.")]
    [SerializeField] RegionInfoDefinition regionContext = null;
    [Tooltip("Optional activity zone context. Empty falls back to PlayerActivityContext.CurrentZone.")]
    [SerializeField] ActivityZoneDefinition zoneContext = null;
    [Tooltip("If enabled, locked board rows are included in snapshots unless their row hides locked state.")]
    [SerializeField] bool includeLockedRows = true;

    [Header("Access")]
    [Tooltip("Optional reusable access profile checked before this scene source can run its action.")]
    [SerializeField] AccessProfileDefinition accessProfile = null;
    [Tooltip("If enabled, source-level access checks are published to access logs/events.")]
    [SerializeField] bool publishAccessChecks = true;

    [Header("Interaction")]
    [Tooltip("Action performed when the player interacts with this source.")]
    [SerializeField] JourneyIncidentSourceAction interactAction = JourneyIncidentSourceAction.ViewOnly;
    [Tooltip("Action label shown by overworld prompts.")]
    [SerializeField] string actionName = "Investigate";
    [Tooltip("Short prompt text shown by overworld interaction UI. Empty uses board or incident description.")]
    [TextArea]
    [SerializeField] string promptText = string.Empty;

    [Header("Trigger")]
    [Tooltip("If enabled, entering this trigger runs Trigger Action.")]
    [SerializeField] bool runOnPlayerTrigger;
    [Tooltip("Action performed when this source is triggered.")]
    [SerializeField] JourneyIncidentSourceAction triggerAction = JourneyIncidentSourceAction.RollBoard;
    [Tooltip("If enabled, repeated player triggers can call this source more than once.")]
    [SerializeField] bool triggerRepeatedly = true;

    [Header("Feedback")]
    [Tooltip("If enabled, view/run results use DialogManager when available.")]
    [SerializeField] bool showDialogResult = true;
    [Tooltip("If enabled, source attempts are written to GameDebugLogger.")]
    [SerializeField] bool logAttempts;

    public JourneyIncidentDefinition Incident => incident;
    public JourneyIncidentBoardDefinition Board => board;
    public string SourceId => !string.IsNullOrWhiteSpace(sourceId)
        ? sourceId
        : board != null ? board.ResolveBoardSourceId(null) : incident != null ? $"journey-incident:{incident.Id}" : name;
    public string DisplayName => !string.IsNullOrWhiteSpace(displayName)
        ? displayName
        : board != null ? board.DisplayName : incident != null ? incident.DisplayName : name;
    public RegionInfoDefinition RegionContext => regionContext;
    public ActivityZoneDefinition ZoneContext => zoneContext;
    public bool IncludeLockedRows => includeLockedRows;
    public JourneyIncidentSourceAction InteractAction => interactAction;
    public JourneyIncidentSourceAction TriggerAction => triggerAction;
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

    [ContextMenu("View Snapshot")]
    public void ViewSnapshotFromContextMenu() {
        GetSnapshot(ResolvePlayer(null));
    }

    [ContextMenu("Activate Configured Incident")]
    public void ActivateIncidentFromContextMenu() {
        TryActivateIncident(ResolvePlayer(null), out _);
    }

    [ContextMenu("Roll Board")]
    public void RollBoardFromContextMenu() {
        TryRollBoard(ResolvePlayer(null), out _);
    }

    [ContextMenu("Resolve Configured Incident")]
    public void ResolveIncidentFromContextMenu() {
        ResolveIncident(ResolvePlayer(null));
    }

    public JourneyIncidentBoardSnapshot GetSnapshot(PlayerController player = null) {
        player = player != null ? player : ResolvePlayer(null);
        if(board == null) {
            return new JourneyIncidentBoardSnapshot {
                boardId = string.Empty,
                boardName = DisplayName,
                description = incident != null ? incident.Description : "No journey incident board assigned.",
                sourceId = SourceId,
                sourceName = DisplayName,
                usable = incident != null,
                failureMessage = incident != null ? null : "No journey incident board assigned."
            };
        }

        var log = player != null ? player.GetComponent<PlayerJourneyIncidentLog>() ?? player.gameObject.AddComponent<PlayerJourneyIncidentLog>() : null;
        return board.BuildSnapshot(player, log, SourceId, DisplayName, ResolveRegion(), ResolveZone(), includeLockedRows, this);
    }

    public bool TryActivateIncident(PlayerController player, out JourneyIncidentActivationResult result) {
        player = player != null ? player : ResolvePlayer(null);
        if(!CanUseSource(player, out var failureMessage)) {
            result = new JourneyIncidentActivationResult(incident, SourceId, ResolveRegion(), ResolveZone()) {
                blocked = true,
                failureMessage = failureMessage
            };
            Log(failureMessage, GameDebugSeverity.Warning);
            return false;
        }

        if(incident == null) {
            result = new JourneyIncidentActivationResult(null, SourceId, ResolveRegion(), ResolveZone()) {
                blocked = true,
                failureMessage = "No journey incident assigned."
            };
            Log(result.failureMessage, GameDebugSeverity.Warning);
            return false;
        }

        result = incident.Activate(player, ResolveRegion(), ResolveZone(), SourceId, DisplayName, this);
        Log(result != null && !result.blocked ? $"{incident.DisplayName} activated." : result != null ? result.failureMessage : "Journey incident activation failed.", result != null && !result.blocked ? GameDebugSeverity.Info : GameDebugSeverity.Warning);
        return result != null && !result.blocked;
    }

    public bool TryRollBoard(PlayerController player, out JourneyIncidentBoardRollResult result) {
        player = player != null ? player : ResolvePlayer(null);
        if(!CanUseSource(player, out var failureMessage)) {
            result = new JourneyIncidentBoardRollResult(board, SourceId, ResolveRegion(), ResolveZone()) {
                blocked = true,
                failureMessage = failureMessage
            };
            Log(failureMessage, GameDebugSeverity.Warning);
            return false;
        }

        if(board == null) {
            result = new JourneyIncidentBoardRollResult(null, SourceId, ResolveRegion(), ResolveZone()) {
                blocked = true,
                failureMessage = "No journey incident board assigned."
            };
            Log(result.failureMessage, GameDebugSeverity.Warning);
            return false;
        }

        result = board.Roll(player, ResolveRegion(), ResolveZone(), SourceId, DisplayName, this);
        Log(result != null && result.activatedIncidents > 0 ? $"{result.activatedIncidents} journey incident(s) activated." : result != null ? result.failureMessage : "Journey incident board failed.", result != null && result.activatedIncidents > 0 ? GameDebugSeverity.Info : GameDebugSeverity.Warning);
        return result != null && result.activatedIncidents > 0 && !result.blocked;
    }

    public int ResolveIncident(PlayerController player) {
        player = player != null ? player : ResolvePlayer(null);
        if(incident == null || player == null) {
            return 0;
        }

        int resolved = incident.ResolveActive(player, ResolveRegion(), ResolveZone(), SourceId, this);
        Log(resolved > 0 ? $"{incident.DisplayName} resolved." : $"{incident.DisplayName} was not active.", resolved > 0 ? GameDebugSeverity.Info : GameDebugSeverity.Warning);
        return resolved;
    }

    public int ExpireIncident(PlayerController player) {
        player = player != null ? player : ResolvePlayer(null);
        if(incident == null || player == null) {
            return 0;
        }

        int expired = incident.ExpireActive(player, ResolveRegion(), ResolveZone(), SourceId, this);
        Log(expired > 0 ? $"{incident.DisplayName} expired." : $"{incident.DisplayName} was not active.", expired > 0 ? GameDebugSeverity.Info : GameDebugSeverity.Warning);
        return expired;
    }

    public bool TryGetInteractionInfo(PlayerController player, out OverworldInteractionInfo info) {
        bool canUse = CanUseSource(player, out var blockedMessage);
        info = new OverworldInteractionInfo {
            TargetName = DisplayName,
            ActionName = string.IsNullOrWhiteSpace(actionName) ? "Investigate" : actionName,
            Description = ResolvePromptText(),
            PermissionHint = ResolveZone() != null ? ResolveZone().DisplayName : string.Empty,
            BlockedMessage = blockedMessage,
            CanInteract = canUse,
            Activity = null,
            Zone = ResolveZone(),
            Source = this
        };
        return true;
    }

    IEnumerator Apply(PlayerController player, JourneyIncidentSourceAction action) {
        switch(action) {
            case JourneyIncidentSourceAction.ActivateConfiguredIncident:
                TryActivateIncident(player, out var activationResult);
                yield return ShowFeedback(activationResult != null && !string.IsNullOrWhiteSpace(activationResult.failureMessage) ? activationResult.failureMessage : activationResult != null ? $"{activationResult.incidentName} activated." : "Journey incident finished.");
                break;
            case JourneyIncidentSourceAction.RollBoard:
                TryRollBoard(player, out var rollResult);
                yield return ShowFeedback(BuildRollMessage(rollResult));
                break;
            case JourneyIncidentSourceAction.ResolveConfiguredIncident:
                int resolved = ResolveIncident(player);
                yield return ShowFeedback(resolved > 0 ? $"{DisplayName} resolved." : $"{DisplayName} is not active.");
                break;
            case JourneyIncidentSourceAction.ExpireConfiguredIncident:
                int expired = ExpireIncident(player);
                yield return ShowFeedback(expired > 0 ? $"{DisplayName} expired." : $"{DisplayName} is not active.");
                break;
            default:
                var snapshot = GetSnapshot(player);
                Log($"{DisplayName} viewed with {snapshot.rows.Count} visible incident row(s).", GameDebugSeverity.Info);
                yield return ShowFeedback(BuildViewMessage(snapshot));
                break;
        }
    }

    bool CanUseSource(PlayerController player, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required.";
            return false;
        }

        if(accessProfile != null && !accessProfile.CanAccess(player, out failureMessage)) {
            PublishAccessCheck(player, false, SourceId, failureMessage);
            return false;
        }

        PublishAccessCheck(player, true, SourceId, accessProfile != null ? accessProfile.PassedMessage : null);
        failureMessage = null;
        return true;
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

    string ResolvePromptText() {
        if(!string.IsNullOrWhiteSpace(promptText)) {
            return promptText;
        }

        if(board != null) {
            return board.Description;
        }

        return incident != null ? incident.Description : string.Empty;
    }

    string BuildViewMessage(JourneyIncidentBoardSnapshot snapshot) {
        if(snapshot == null) {
            return "Journey incident snapshot is unavailable.";
        }

        if(!snapshot.usable && !string.IsNullOrWhiteSpace(snapshot.failureMessage)) {
            return snapshot.failureMessage;
        }

        if(board == null && incident != null) {
            return $"{incident.DisplayName} is available.";
        }

        return snapshot.rows.Count == 1
            ? $"{DisplayName} has 1 visible incident."
            : $"{DisplayName} has {snapshot.rows.Count} visible incident(s).";
    }

    string BuildRollMessage(JourneyIncidentBoardRollResult result) {
        if(result == null) {
            return "Journey incident board did not return a result.";
        }

        if(result.activatedIncidents > 0) {
            return result.activatedIncidents == 1
                ? "1 journey incident activated."
                : $"{result.activatedIncidents} journey incidents activated.";
        }

        return !string.IsNullOrWhiteSpace(result.failureMessage) ? result.failureMessage : "No journey incident activated.";
    }

    IEnumerator ShowFeedback(string message) {
        if(showDialogResult && DialogManager.i != null && !string.IsNullOrWhiteSpace(message)) {
            yield return DialogManager.i.ShowDialogText(message);
        }
    }

    void PublishAccessCheck(PlayerController player, bool passed, string resolvedSourceId, string message) {
        if(accessProfile == null || !publishAccessChecks) {
            return;
        }

        accessProfile.PublishChecked(player, passed, resolvedSourceId, message, this);
    }

    void Log(string message, GameDebugSeverity severity) {
        if(!logAttempts && severity < GameDebugSeverity.Warning) {
            return;
        }

        GameDebugLogger.Ensure().Record(severity, GameDebugCategory.Activity, message, this, "JourneyIncidentSource");
    }
}
