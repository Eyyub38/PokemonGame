using UnityEngine;

[CreateAssetMenu(menuName = "Activities/Requirements/Tool Requirement")]
public class ToolRequirement : ActivityRequirement {
    [Tooltip("Tool the player must own.")]
    [SerializeField] ToolDefinition tool;
    [Tooltip("Minimum tool level required.")]
    [Min(1)]
    [SerializeField] int requiredLevel = 1;
    [Tooltip("Minimum remaining durability required.")]
    [Min(0)]
    [SerializeField] int requiredDurability = 1;

    public ToolDefinition Tool => tool;
    public int RequiredLevel => Mathf.Max(1, requiredLevel);
    public int RequiredDurability => Mathf.Max(0, requiredDurability);

    public override bool IsMet(PlayerController player) {
        if(tool == null || player == null) {
            return false;
        }

        var inventory = player.GetComponent<PlayerToolInventory>();
        return inventory != null && inventory.HasTool(tool, RequiredLevel, RequiredDurability);
    }
}
