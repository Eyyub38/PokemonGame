using UnityEngine;

[CreateAssetMenu(menuName = "Activities/Requirements/World Event Requirement")]
public class WorldEventRequirement : ActivityRequirement {
    [Tooltip("World event checked by this requirement.")]
    [SerializeField] WorldEventDefinition requiredEvent;
    [Tooltip("If enabled, the event must be active. If disabled, it must be inactive.")]
    [SerializeField] bool mustBeActive = true;

    public override bool IsMet(PlayerController player) {
        if(requiredEvent == null) {
            return false;
        }

        bool isActive = WorldEventManager.i != null && WorldEventManager.i.IsEventActive(requiredEvent);
        return mustBeActive ? isActive : !isActive;
    }
}
