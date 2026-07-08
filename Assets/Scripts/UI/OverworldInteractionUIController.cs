using UnityEngine;
using UnityEngine.UI;

public class OverworldInteractionUIController : MonoBehaviour {
    [Header("Root")]
    [Tooltip("Root object enabled when an interactable target is available. Empty uses this GameObject.")]
    [SerializeField] GameObject root;
    [Tooltip("Optional object enabled when the current target is blocked.")]
    [SerializeField] GameObject blockedRoot;

    [Header("Text")]
    [Tooltip("Text showing the interaction key, such as E.")]
    [SerializeField] Text keyText;
    [Tooltip("Text showing the target name.")]
    [SerializeField] Text targetNameText;
    [Tooltip("Text showing the primary action name.")]
    [SerializeField] Text actionText;
    [Tooltip("Text showing a short target description.")]
    [SerializeField] Text descriptionText;
    [Tooltip("Text showing area/permission state.")]
    [SerializeField] Text permissionText;
    [Tooltip("Text showing required or active tool.")]
    [SerializeField] Text toolText;
    [Tooltip("Text showing why this interaction is blocked.")]
    [SerializeField] Text blockedText;

    [Header("Defaults")]
    [Tooltip("Default key hint shown when interaction info does not provide one.")]
    [SerializeField] string defaultKeyHint = "E";
    [Tooltip("Default action label shown when interaction info does not provide one.")]
    [SerializeField] string defaultActionName = "Interact";
    [Tooltip("If enabled, this prompt is hidden on Awake.")]
    [SerializeField] bool hideOnAwake = true;

    public OverworldInteractionInfo CurrentInfo { get; private set; }

    void Awake() {
        if(root == null) {
            root = gameObject;
        }

        if(hideOnAwake) {
            Hide();
        }
    }

    public void Show(OverworldInteractionInfo info) {
        CurrentInfo = info;
        if(root != null) {
            root.SetActive(true);
        }

        string actionName = !string.IsNullOrWhiteSpace(info?.ActionName) ? info.ActionName : defaultActionName;
        SetText(keyText, !string.IsNullOrWhiteSpace(info?.KeyHint) ? info.KeyHint : defaultKeyHint);
        SetText(targetNameText, !string.IsNullOrWhiteSpace(info?.TargetName) ? info.TargetName : "Interactable");
        SetText(actionText, actionName);
        SetText(descriptionText, info != null ? info.Description : string.Empty);
        SetText(permissionText, info != null ? info.PermissionHint : string.Empty);
        SetText(toolText, info != null ? info.ToolHint : string.Empty);

        bool blocked = info != null && !info.CanInteract;
        if(blockedRoot != null) {
            blockedRoot.SetActive(blocked);
        }
        SetText(blockedText, blocked ? info.BlockedMessage : string.Empty);
    }

    public void Hide() {
        CurrentInfo = null;
        if(root != null) {
            root.SetActive(false);
        }
        if(blockedRoot != null) {
            blockedRoot.SetActive(false);
        }
    }

    static void SetText(Text text, string value) {
        if(text != null) {
            text.text = value ?? string.Empty;
        }
    }
}
