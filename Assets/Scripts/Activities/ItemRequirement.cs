using UnityEngine;

[CreateAssetMenu(menuName = "Activities/Requirements/Item Requirement")]
public class ItemRequirement : ActivityRequirement {
    [Tooltip("Item the player must have.")]
    [SerializeField] ItemBase item;
    [Tooltip("Minimum amount required in the inventory.")]
    [Min(1)]
    [SerializeField] int count = 1;

    public override bool IsMet(PlayerController player) {
        if(item == null || player == null) {
            return false;
        }

        var inventory = player.GetComponent<Inventory>();
        return inventory != null && inventory.HasItemEnough(item, Mathf.Max(1, count));
    }
}
