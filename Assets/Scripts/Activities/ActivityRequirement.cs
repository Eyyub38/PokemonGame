using UnityEngine;

public abstract class ActivityRequirement : ScriptableObject {
    [Tooltip("Message shown when this requirement prevents the activity.")]
    [SerializeField] string failureMessage = "You cannot do that right now.";

    public string FailureMessage => failureMessage;
    public abstract bool IsMet(PlayerController player);
}
