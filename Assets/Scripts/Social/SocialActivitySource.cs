using System.Collections;
using UnityEngine;

public class SocialActivitySource : MonoBehaviour, Interactable, IPlayerTriggerable, IOverworldInteractionInfoProvider {
    [Header("Social Activity")]
    [Tooltip("Social activity definition this overworld source runs.")]
    [SerializeField] SocialActivityDefinition activity;
    [Tooltip("Stable scene/source id for history records. Empty uses this GameObject name.")]
    [SerializeField] string sourceId;
    [Tooltip("Name shown by interaction prompts. Empty uses the activity display name or this GameObject name.")]
    [SerializeField] string targetName;
    [Tooltip("Action label shown by prompts, such as Picnic, Hangout, Train or Join.")]
    [SerializeField] string actionName = "Socialize";
    [Tooltip("Short prompt text shown by the overworld interaction UI. Empty uses the activity description.")]
    [TextArea]
    [SerializeField] string promptText;

    [Header("Trigger")]
    [Tooltip("If enabled, entering this trigger runs the social activity.")]
    [SerializeField] bool runOnPlayerTrigger;
    [Tooltip("Controls IPlayerTriggerable repeat behavior.")]
    [SerializeField] bool triggerRepeatedly;

    [Header("Feedback")]
    [Tooltip("If enabled, DialogManager shows success and failure messages.")]
    [SerializeField] bool showDialogResult = true;
    [Tooltip("If enabled, failed attempts are written to the custom debug log.")]
    [SerializeField] bool logFailures = true;

    public bool TriggerRepeatedly => triggerRepeatedly;
    public SocialActivityDefinition Activity => activity;
    public string SourceId => string.IsNullOrWhiteSpace(sourceId) ? name : sourceId;

    public IEnumerator Interact(Transform initiator) {
        var player = initiator != null ? initiator.GetComponent<PlayerController>() : PlayerController.i;
        yield return Run(player);
    }

    public void OnPlayerTriggered(PlayerController player) {
        if(runOnPlayerTrigger) {
            StartCoroutine(Run(player));
        }
    }

    public bool TryGetInteractionInfo(PlayerController player, out OverworldInteractionInfo info) {
        string blockedMessage = null;
        bool canInteract = activity != null && activity.CanRun(player, out blockedMessage);

        info = new OverworldInteractionInfo {
            TargetName = BuildTargetName(),
            ActionName = string.IsNullOrWhiteSpace(actionName) ? "Socialize" : actionName,
            Description = !string.IsNullOrWhiteSpace(promptText) ? promptText : activity != null ? activity.Description : string.Empty,
            PermissionHint = PlayerActivityContext.CurrentZone != null ? PlayerActivityContext.CurrentZone.DisplayName : string.Empty,
            BlockedMessage = blockedMessage,
            CanInteract = canInteract,
            Activity = activity != null ? activity.BaseActivity : null,
            Zone = PlayerActivityContext.CurrentZone,
            Source = this
        };
        return true;
    }

    IEnumerator Run(PlayerController player) {
        if(activity == null) {
            yield return ShowFeedback("This social activity source is not configured.");
            yield break;
        }

        if(activity.TryRun(player, SourceId, this, out var result)) {
            yield return ShowFeedback(result.message);
            yield break;
        }

        if(logFailures) {
            GameDebug.Warning(result != null ? result.message : "Social activity failed.", GameDebugCategory.Activity, this, "SocialActivitySource");
        }

        yield return ShowFeedback(result != null ? result.message : "Social activity failed.");
    }

    IEnumerator ShowFeedback(string message) {
        if(showDialogResult && DialogManager.i != null && !string.IsNullOrWhiteSpace(message)) {
            yield return DialogManager.i.ShowDialogText(message);
        }
    }

    string BuildTargetName() {
        if(!string.IsNullOrWhiteSpace(targetName)) {
            return targetName;
        }

        return activity != null ? activity.DisplayName : name;
    }
}
