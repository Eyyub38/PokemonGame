using System.Linq;
using UnityEngine;

public class OverworldInteractionSensor : MonoBehaviour {
    [Header("Player")]
    [Tooltip("Player used to calculate facing interaction position. Empty uses PlayerController.i or a parent PlayerController.")]
    [SerializeField] PlayerController player;
    [Tooltip("If enabled, the sensor uses the tile in front of the player. If disabled, it scans around the player transform.")]
    [SerializeField] bool useFacingTile = true;
    [Tooltip("Radius used for interactable detection.")]
    [Min(0.05f)]
    [SerializeField] float detectionRadius = 0.5f;
    [Tooltip("Layer mask used to find interactable objects. Empty uses GameLayers.i interactable and water layers.")]
    [SerializeField] LayerMask interactionMask;

    [Header("UI")]
    [Tooltip("Prompt UI updated by this sensor.")]
    [SerializeField] OverworldInteractionUIController promptUI;
    [Tooltip("If enabled, prompt UI is hidden when no target is found.")]
    [SerializeField] bool hidePromptWhenNoTarget = true;

    [Header("Refresh")]
    [Tooltip("Seconds between prompt refreshes. 0 checks every frame.")]
    [Min(0f)]
    [SerializeField] float refreshInterval = 0.05f;

    float nextRefreshTime;
    Interactable currentInteractable;

    public Interactable CurrentInteractable => currentInteractable;
    public OverworldInteractionInfo CurrentInfo { get; private set; }

    void Awake() {
        if(player == null) {
            player = GetComponentInParent<PlayerController>() ?? PlayerController.i;
        }

        if(interactionMask == 0 && GameLayers.i != null) {
            interactionMask = GameLayers.i.InteractableLayer | GameLayers.i.WaterLayer;
        }
    }

    void Update() {
        if(refreshInterval > 0f && Time.unscaledTime < nextRefreshTime) {
            return;
        }

        nextRefreshTime = Time.unscaledTime + refreshInterval;
        Refresh();
    }

    public void Refresh() {
        var collider = FindTargetCollider();
        if(collider == null) {
            ClearTarget();
            return;
        }

        var interactable = collider.GetComponent<Interactable>();
        if(interactable == null) {
            ClearTarget();
            return;
        }

        currentInteractable = interactable;
        CurrentInfo = BuildInfo(collider, interactable);
        promptUI?.Show(CurrentInfo);
    }

    Collider2D FindTargetCollider() {
        if(player == null) {
            return null;
        }

        Vector3 origin = player.transform.position;
        if(useFacingTile) {
            origin += player.GetLastFacingDirection();
        }

        return Physics2D.OverlapCircle(origin, detectionRadius, interactionMask);
    }

    OverworldInteractionInfo BuildInfo(Collider2D targetCollider, Interactable interactable) {
        var provider = targetCollider.GetComponents<MonoBehaviour>().OfType<IOverworldInteractionInfoProvider>().FirstOrDefault();
        if(provider != null && provider.TryGetInteractionInfo(player, out var providedInfo) && providedInfo != null) {
            return providedInfo;
        }

        var targetObject = targetCollider.gameObject;
        return OverworldInteractionInfo.Basic(
            targetObject.name,
            "Interact",
            "Press the interaction key to use this object.",
            targetObject);
    }

    void ClearTarget() {
        currentInteractable = null;
        CurrentInfo = null;
        if(hidePromptWhenNoTarget) {
            promptUI?.Hide();
        }
    }
}
