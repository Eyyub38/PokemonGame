using UnityEngine;

[System.Serializable]
public class ActivityItemCost {
    [Tooltip("Inventory item consumed by the activity.")]
    public ItemBase item;
    [Tooltip("Amount of the item consumed.")]
    [Min(1)]
    public int count = 1;
}

[System.Serializable]
public class ActivityToolCost {
    [Tooltip("Tool whose durability is consumed by the activity.")]
    public ToolDefinition tool;
    [Tooltip("Durability removed from the tool.")]
    [Min(1)]
    public int durabilityCost = 1;
}

[System.Serializable]
public class ActivityNeedCost {
    [Tooltip("Survival need consumed by the activity.")]
    public SurvivalNeedDefinition need;
    [Tooltip("Amount removed from the selected need.")]
    [Min(1)]
    public int amount = 1;
}
