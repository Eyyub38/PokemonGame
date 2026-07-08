using UnityEngine;
using UnityEngine.InputSystem;

public class InputMapController : MonoBehaviour {
    public static InputMapController Instance { get; private set; }

    [SerializeField] InputActionAsset actions;

    InputActionMap playerMap;
    InputActionMap uiMap;

    void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (actions == null) {
            Debug.LogError($"{name}: InputActionAsset is missing!", this);
            return;
        }

        playerMap = actions.FindActionMap("Player", true);
        uiMap     = actions.FindActionMap("UI", true);
    }

    void OnDisable() {
        DisableAll();
    }

    public void EnablePlayer() {
        if (playerMap == null || uiMap == null) return;
        uiMap.Disable();
        playerMap.Enable();
    }

    public void EnableUI() {
        if (playerMap == null || uiMap == null) return;
        playerMap.Disable();
        uiMap.Enable();
    }

    public void DisableAll() {
        if (playerMap == null || uiMap == null) return;
        playerMap.Disable();
        uiMap.Disable();
    }
}
