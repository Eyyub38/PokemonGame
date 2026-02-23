using UnityEngine;
using UnityEngine.InputSystem;

public class SelectionInputFromActions : MonoBehaviour, ISelectionInput {
    [SerializeField] InputActionAsset actions;
    [SerializeField] string mapName = "UI";
    [SerializeField] string navigateName = "Navigate";
    [SerializeField] string selectName = "Select";
    [SerializeField] string backName = "Back";
    [SerializeField] string nextName = "Next";
    [SerializeField] string previousName = "Previous";

    InputActionMap map;
    InputAction navigate;
    InputAction select;
    InputAction back;
    InputAction next;
    InputAction previous;
    Vector2 lastNav;

    const float TH = 0.2f; // Lower threshold for more responsive menu navigation

    public Vector2 Navigate => navigate != null ? navigate.ReadValue<Vector2>() : Vector2.zero;
    public bool SelectPressedThisFrame => select != null && select.WasPressedThisFrame();
    public bool BackPressedThisFrame => back != null && back.WasPressedThisFrame();
    public bool NextPressedThisFrame => next != null && next.WasPressedThisFrame();
    public bool PreviousPressedThisFrame => previous != null && previous.WasPressedThisFrame();
    public bool UpPressedThisFrame { get; private set; }
    public bool DownPressedThisFrame { get; private set; }
    public bool RightPressedThisFrame { get; private set; }
    public bool LeftPressedThisFrame { get; private set; }

    void Awake() {
        if(actions == null) {
            Debug.LogError("SelectionInputFromActions: actions not set");
            enabled = false;
            return;
        }

        map = actions.FindActionMap(mapName, true);
        navigate = map.FindAction(navigateName, true);
        select = map.FindAction(selectName, true);
        back = map.FindAction(backName, true);
        next = map.FindAction(nextName, true);
        previous = map.FindAction(previousName, true);
    }

    void Update() {
        var nav = Navigate;

        RightPressedThisFrame = nav.x > TH && lastNav.x <= TH;
        LeftPressedThisFrame = nav.x < -TH && lastNav.x >= -TH;
        UpPressedThisFrame = nav.y > TH && lastNav.y <= TH;
        DownPressedThisFrame = nav.y < -TH && lastNav.y >= -TH;

        lastNav = nav;
    }

    void OnEnable() {
        // Do not Enable/Disable the map here. Let InputMapController manage the state.
        lastNav = Navigate;
    }
}
