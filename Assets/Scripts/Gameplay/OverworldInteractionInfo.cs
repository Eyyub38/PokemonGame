using UnityEngine;

public interface IOverworldInteractionInfoProvider {
    bool TryGetInteractionInfo(PlayerController player, out OverworldInteractionInfo info);
}

public class OverworldInteractionInfo {
    public string TargetName;
    public string ActionName;
    public string Description;
    public string KeyHint;
    public string ToolHint;
    public string PermissionHint;
    public string BlockedMessage;
    public bool CanInteract = true;
    public ActivityDefinition Activity;
    public ActivityZoneDefinition Zone;
    public Object Source;

    public static OverworldInteractionInfo Basic(string targetName, string actionName, string description, Object source = null) {
        return new OverworldInteractionInfo {
            TargetName = targetName,
            ActionName = actionName,
            Description = description,
            Source = source
        };
    }
}

public class InteractionPromptSource : MonoBehaviour, IOverworldInteractionInfoProvider {
    [Header("Prompt")]
    [Tooltip("Name shown by overworld interaction prompts. Empty uses this GameObject name.")]
    [SerializeField] string targetName;
    [Tooltip("Primary action label, such as Water, Mine, Talk or Feed.")]
    [SerializeField] string actionName = "Interact";
    [Tooltip("Short prompt text shown below the target name.")]
    [TextArea]
    [SerializeField] string description;
    [Tooltip("Optional tool/requirement text shown as a compact UI hint.")]
    [SerializeField] string toolHint;
    [Tooltip("Optional permission/area text shown as a compact UI hint.")]
    [SerializeField] string permissionHint;

    [Header("Activity")]
    [Tooltip("Optional activity checked before the prompt is marked available.")]
    [SerializeField] ActivityDefinition activity;
    [Tooltip("If enabled, ActivityDefinition.CanPerform controls whether this prompt is shown as blocked.")]
    [SerializeField] bool validateActivity = true;

    public bool TryGetInteractionInfo(PlayerController player, out OverworldInteractionInfo info) {
        bool canInteract = true;
        string blockedMessage = null;
        if(validateActivity && activity != null) {
            canInteract = activity.CanPerform(player, out blockedMessage);
        }

        info = new OverworldInteractionInfo {
            TargetName = string.IsNullOrWhiteSpace(targetName) ? name : targetName,
            ActionName = string.IsNullOrWhiteSpace(actionName) ? "Interact" : actionName,
            Description = description,
            ToolHint = toolHint,
            PermissionHint = permissionHint,
            BlockedMessage = blockedMessage,
            CanInteract = canInteract,
            Activity = activity,
            Zone = PlayerActivityContext.CurrentZone,
            Source = this
        };
        return true;
    }
}
