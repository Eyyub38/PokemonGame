using UnityEngine;
using UnityEngine.InputSystem;

public class InputMapController : MonoBehaviour {
    [SerializeField] InputActionAsset actions;

    InputActionMap playerMap;
    InputActionMap uiMap;

    void Awake() {
        playerMap = actions.FindActionMap("Player", true);
        uiMap     = actions.FindActionMap("UI", true);
    }

    public void EnablePlayer() {
        uiMap.Disable();
        playerMap.Enable();
    }

    public void EnableUI() {
        playerMap.Disable();
        uiMap.Enable();
    }
}
