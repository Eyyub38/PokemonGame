using UnityEngine;

[CreateAssetMenu(menuName = "Tools/Tool Definition")]
public class ToolDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this tool. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in UI/debug messages. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer note or player-facing description for this tool.")]
    [TextArea][SerializeField] string description;
    [Tooltip("Optional icon for menus or future UI.")]
    [SerializeField] Sprite icon;
    [Header("Durability")]
    [Tooltip("Maximum durability when the tool is fully repaired.")]
    [Min(1)]
    [SerializeField] int maxDurability = 100;
    [Tooltip("Highest upgrade level this tool can reach.")]
    [Min(1)]
    [SerializeField] int maxLevel = 5;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public int MaxDurability => Mathf.Max(1, maxDurability);
    public int MaxLevel => Mathf.Max(1, maxLevel);
}
