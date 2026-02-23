using UnityEngine;

namespace GDEUtills.GenerciSelectionUI {
    public class InputRouter : MonoBehaviour {
        public static InputRouter i { get; private set; }

        SelectionInputFromActions uiInput;

        public ISelectionInput UI => uiInput;

        void Awake() {
            if(i != null && i != this) {
                Destroy(gameObject);
                return;
            }
            i = this;
            uiInput = GetComponent<SelectionInputFromActions>();

            if(uiInput == null) {
                Debug.LogError($"{name}: SelectionInputFromActions component is missing!", this);
            }
        }
    }

}
