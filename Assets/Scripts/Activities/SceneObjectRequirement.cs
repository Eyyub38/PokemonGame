using UnityEngine;

public enum SceneObjectRequirementMode {
    Available,
    Unavailable,
    StateEquals,
    InteractionCountAtLeast,
    HasInteracted,
    NeverInteracted,
    StateCountWithTagAtLeast,
    StateCountByCategoryAtLeast
}

[CreateAssetMenu(menuName = "Activities/Requirements/Scene Object Requirement")]
public class SceneObjectRequirement : ActivityRequirement {
    [Tooltip("Which scene object check this requirement performs.")]
    [SerializeField] SceneObjectRequirementMode mode = SceneObjectRequirementMode.Available;
    [Tooltip("Specific scene object checked by object/state/interact modes.")]
    [SerializeField] SceneObjectDefinition sceneObject = null;
    [Tooltip("State checked by state modes.")]
    [SerializeField] SceneObjectState requiredState = SceneObjectState.Available;
    [Tooltip("Optional source id filter for interaction count mode.")]
    [SerializeField] string sourceId = string.Empty;
    [Tooltip("Tag checked by State Count With Tag mode.")]
    [SerializeField] string tag = string.Empty;
    [Tooltip("Category checked by State Count By Category mode.")]
    [SerializeField] SceneObjectCategory category = SceneObjectCategory.General;
    [Tooltip("Minimum count required by count modes.")]
    [Min(0)]
    [SerializeField] int requiredCount = 1;
    [Tooltip("If enabled, the selected condition must be true. If disabled, it must be false.")]
    [SerializeField] bool mustBeMet = true;

    public override bool IsMet(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerSceneObjectLog>() : null;
        bool result = mode switch {
            SceneObjectRequirementMode.Unavailable => sceneObject != null && (log == null ? !sceneObject.IsAvailableState(sceneObject.DefaultState) : !log.IsAvailable(sceneObject)),
            SceneObjectRequirementMode.StateEquals => sceneObject != null && (log != null ? log.GetState(sceneObject) : sceneObject.DefaultState) == requiredState,
            SceneObjectRequirementMode.InteractionCountAtLeast => log != null && log.GetInteractionCount(sceneObject, sourceId) >= Mathf.Max(0, requiredCount),
            SceneObjectRequirementMode.HasInteracted => log != null && log.HasInteracted(sceneObject, sourceId),
            SceneObjectRequirementMode.NeverInteracted => log == null || !log.HasInteracted(sceneObject, sourceId),
            SceneObjectRequirementMode.StateCountWithTagAtLeast => log != null && log.GetStateCount(requiredState, tag: tag) >= Mathf.Max(0, requiredCount),
            SceneObjectRequirementMode.StateCountByCategoryAtLeast => log != null && log.GetStateCount(requiredState, category: category) >= Mathf.Max(0, requiredCount),
            _ => sceneObject == null || (log != null ? log.IsAvailable(sceneObject) : sceneObject.IsAvailableState(sceneObject.DefaultState))
        };

        return mustBeMet ? result : !result;
    }
}
