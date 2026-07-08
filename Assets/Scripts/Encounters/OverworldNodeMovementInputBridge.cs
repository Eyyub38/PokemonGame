using UnityEngine;
using UnityEngine.InputSystem;

public class OverworldNodeMovementInputBridge : MonoBehaviour {
    [Header("References")]
    [Tooltip("Node movement adapter that receives input directions. Empty uses the component on this GameObject.")]
    [SerializeField] OverworldNodeMovementAdapter movementAdapter = null;
    [Tooltip("Input action asset that contains the movement action.")]
    [SerializeField] InputActionAsset actions = null;
    [Tooltip("Action map name that contains the movement action.")]
    [SerializeField] string actionMapName = "Player";
    [Tooltip("Move action name that returns a Vector2.")]
    [SerializeField] string moveActionName = "Move";

    [Header("Input")]
    [Tooltip("If enabled, this bridge enables the move action on OnEnable and disables it on OnDisable.")]
    [SerializeField] bool manageActionLifecycle = true;
    [Tooltip("If enabled, movement is attempted when the action is pressed this frame.")]
    [SerializeField] bool moveOnPressedThisFrame = true;
    [Tooltip("If enabled, holding a direction repeats move attempts after the repeat delay.")]
    [SerializeField] bool repeatWhileHeld = false;
    [Tooltip("Seconds between repeated held-direction move attempts.")]
    [Min(0.01f)]
    [SerializeField] float repeatDelaySeconds = 0.18f;
    [Tooltip("If enabled, diagonal input is collapsed before it reaches the adapter. The adapter can also apply its own cardinal preference.")]
    [SerializeField] bool preferCardinalDirection = true;

    [Header("Debug")]
    [Tooltip("If enabled, missing input setup and blocked bridge attempts are written to GameDebug.")]
    [SerializeField] bool logBlockedAttempts = false;

    InputAction moveAction;
    Vector2 lastMoveDirection;
    float nextRepeatTime;

    public OverworldNodeMovementAdapter MovementAdapter => movementAdapter;
    public InputActionAsset Actions => actions;
    public string ActionMapName => actionMapName;
    public string MoveActionName => moveActionName;
    public bool RepeatWhileHeld => repeatWhileHeld;
    public float RepeatDelaySeconds => Mathf.Max(0.01f, repeatDelaySeconds);

    void Awake() {
        if(movementAdapter == null) {
            movementAdapter = GetComponent<OverworldNodeMovementAdapter>();
        }

        ResolveMoveAction();
    }

    void OnEnable() {
        if(moveAction == null) {
            ResolveMoveAction();
        }

        if(manageActionLifecycle) {
            moveAction?.Enable();
        }
    }

    void OnDisable() {
        if(manageActionLifecycle) {
            moveAction?.Disable();
        }
    }

    void Update() {
        if(moveAction == null || movementAdapter == null) {
            return;
        }

        Vector2 direction = NormalizeInput(moveAction.ReadValue<Vector2>());
        if(direction.sqrMagnitude <= 0f) {
            lastMoveDirection = Vector2.zero;
            return;
        }

        bool pressed = moveOnPressedThisFrame && moveAction.WasPressedThisFrame();
        bool directionChanged = lastMoveDirection.sqrMagnitude <= 0f || Vector2.Dot(lastMoveDirection.normalized, direction.normalized) < 0.99f;
        bool repeat = repeatWhileHeld && Time.time >= nextRepeatTime;

        if(pressed || directionChanged || repeat) {
            AttemptMove(direction);
        }

        lastMoveDirection = direction;
    }

    [ContextMenu("Resolve Move Action")]
    public void ResolveMoveActionFromContext() {
        ResolveMoveAction();
    }

    public void SetActions(InputActionAsset inputActions) {
        actions = inputActions;
        ResolveMoveAction();
    }

    public void SetMovementAdapter(OverworldNodeMovementAdapter adapter) {
        movementAdapter = adapter;
    }

    void AttemptMove(Vector2 direction) {
        nextRepeatTime = Time.time + RepeatDelaySeconds;
        if(!movementAdapter.TryMove(direction)) {
            LogBlocked("Node movement input bridge move attempt was blocked.");
        }
    }

    void ResolveMoveAction() {
        moveAction = null;
        if(actions == null) {
            LogBlocked("Node movement input bridge has no InputActionAsset.");
            return;
        }

        var map = actions.FindActionMap(actionMapName, throwIfNotFound: false);
        if(map == null) {
            LogBlocked($"Node movement input bridge could not find action map '{actionMapName}'.");
            return;
        }

        moveAction = map.FindAction(moveActionName, throwIfNotFound: false);
        if(moveAction == null) {
            LogBlocked($"Node movement input bridge could not find action '{moveActionName}'.");
        }
    }

    Vector2 NormalizeInput(Vector2 direction) {
        if(direction.sqrMagnitude <= 0f || !preferCardinalDirection) {
            return direction;
        }

        return Mathf.Abs(direction.x) >= Mathf.Abs(direction.y)
            ? new Vector2(Mathf.Sign(direction.x), 0f)
            : new Vector2(0f, Mathf.Sign(direction.y));
    }

    void LogBlocked(string message) {
        if(logBlockedAttempts) {
            GameDebug.Warning(message, GameDebugCategory.Encounter, this, "OverworldNodeMovementInputBridge");
        }
    }
}
