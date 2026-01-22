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
            
            InputAction navigate;
            InputAction select;
            InputAction back;
            InputAction next;
            InputAction previous;
            
            public Vector2 Navigate => navigate != null ? navigate.ReadValue<Vector2>() : Vector2.zero;
            public bool SubmitPressedThisFrame => select != null && select.WasPressedThisFrame();
            public bool BackPressedThisFrame => back != null && back.WasPressedThisFrame();
            public float Horizontal => Navigate.x;
            public bool NextPressedThisFrame => select != null && next.WasPressedThisFrame();
            public bool PreviousPressedThisFrame => previous != null && previous.WasPressedThisFrame();

            void Awake() {
                if (actions == null) {
                    Debug.LogError("SelectionInputFromActions: actions not set");
                    enabled = false;
                    return;
                }
                
                var map = actions.FindActionMap(mapName,true);
                navigate = map.FindAction("Navigate");
                select = map.FindAction("Select");
                back = map.FindAction("Back");
                next = map.FindAction("Next");
                previous = map.FindAction("Previous");
            }

            void OnEnable() {
                navigate?.Enable();
                select?.Enable();
                back?.Enable();
                next?.Enable();
                previous?.Enable();
            }
            void OnDisable() {
                navigate?.Disable();
                select?.Disable();
                back?.Disable();
                next?.Disable();
                previous?.Disable();
            }
            
}

