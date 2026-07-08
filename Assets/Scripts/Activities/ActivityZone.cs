using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ActivityZone : MonoBehaviour {
    [Tooltip("Zone definition that decides which activities are allowed while the player is inside this trigger.")]
    [SerializeField] ActivityZoneDefinition definition;
    [Tooltip("If enabled, this component warns when it has no zone definition.")]
    [SerializeField] bool warnWhenMissingDefinition = true;

    public ActivityZoneDefinition Definition => definition;

    void Reset() {
        var zoneCollider = GetComponent<Collider2D>();
        zoneCollider.isTrigger = true;
    }

    void Awake() {
        if(definition == null && warnWhenMissingDefinition) {
            GameDebug.Warning($"{name} has no activity zone definition.", GameDebugCategory.Validation, this, "ActivityZone");
        }
    }

    void OnTriggerEnter2D(Collider2D other) {
        var player = other.GetComponentInParent<PlayerController>();
        if(player != null) {
            PlayerActivityContext.SetCurrentZone(definition);
        }
    }

    void OnTriggerExit2D(Collider2D other) {
        var player = other.GetComponentInParent<PlayerController>();
        if(player != null) {
            PlayerActivityContext.ClearCurrentZone(definition);
        }
    }
}
