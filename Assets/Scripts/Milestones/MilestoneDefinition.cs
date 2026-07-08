using UnityEngine;

[CreateAssetMenu(menuName = "Milestones/Milestone Definition")]
public class MilestoneDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this milestone. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in UI/debug messages. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer note or player-facing description for this milestone.")]
    [TextArea][SerializeField] string description;
    [Tooltip("If enabled, future UI can hide this milestone until completed.")]
    [SerializeField] bool hidden;
    [Header("Events")]
    [Tooltip("Optional event published when this milestone is completed. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition completedEvent;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public bool Hidden => hidden;
    public GameEventDefinition CompletedEvent => completedEvent;
}
