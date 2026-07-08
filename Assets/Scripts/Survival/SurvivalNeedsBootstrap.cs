using UnityEngine;

public class SurvivalNeedsBootstrap : MonoBehaviour {
    [Tooltip("If enabled, adds SurvivalNeedsController to the player on scene start when missing.")]
    [SerializeField] bool addToPlayerOnStart = true;

    void Start() {
        if(!addToPlayerOnStart || PlayerController.i == null) {
            return;
        }

        if(PlayerController.i.GetComponent<SurvivalNeedsController>() == null) {
            PlayerController.i.gameObject.AddComponent<SurvivalNeedsController>();
        }
    }
}
