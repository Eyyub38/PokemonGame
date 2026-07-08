using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Resources/Resource Node Definition")]
public class ResourceNodeDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this resource. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in UI/debug messages. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer note or player-facing description for this resource.")]
    [TextArea][SerializeField] string description;
    [Header("Activity")]
    [Tooltip("Activity definition that gates costs, requirements, XP and rewards for gathering.")]
    [SerializeField] ActivityDefinition activity;
    [Header("Tools")]
    [Tooltip("Legacy inventory item required as a tool. Prefer Required Tool Definition for new content.")]
    [SerializeField] ItemBase requiredTool;
    [Tooltip("Tool inventory definition required to gather from this node.")]
    [SerializeField] ToolDefinition requiredToolDefinition;
    [Tooltip("Durability consumed from the required tool definition on gather.")]
    [Min(0)]
    [SerializeField] int toolDurabilityCost = 1;
    [Header("Respawn")]
    [Tooltip("In-game hours before a depleted node becomes available again. 0 means no timed respawn.")]
    [Min(0)]
    [SerializeField] int respawnHours = 12;
    [Tooltip("If enabled, the node becomes depleted after gathering.")]
    [SerializeField] bool depleteAfterGather = true;
    [Header("Rewards")]
    [Tooltip("Items that can be gathered from this node.")]
    [SerializeField] List<ResourceYield> yields = new List<ResourceYield>();
    [Header("Visuals")]
    [Tooltip("Sprite shown while this resource can be gathered.")]
    [SerializeField] Sprite availableSprite;
    [Tooltip("Sprite shown while this resource is depleted.")]
    [SerializeField] Sprite depletedSprite;
    [Header("Events")]
    [Tooltip("Optional event published when this resource is gathered. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition gatherEvent;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public ActivityDefinition Activity => activity;
    public ItemBase RequiredTool => requiredTool;
    public ToolDefinition RequiredToolDefinition => requiredToolDefinition;
    public int ToolDurabilityCost => Mathf.Max(0, toolDurabilityCost);
    public int RespawnHours => Mathf.Max(0, respawnHours);
    public bool DepleteAfterGather => depleteAfterGather;
    public IReadOnlyList<ResourceYield> Yields => yields;
    public Sprite AvailableSprite => availableSprite;
    public Sprite DepletedSprite => depletedSprite;
    public GameEventDefinition GatherEvent => gatherEvent;
}

[System.Serializable]
public class ResourceYield {
    [Tooltip("Item produced by this resource roll.")]
    public ItemBase item;
    [Tooltip("Minimum amount produced before skill bonuses.")]
    [Min(0)]
    public int minCount = 1;
    [Tooltip("Maximum amount produced before skill bonuses.")]
    [Min(0)]
    public int maxCount = 1;
    [Tooltip("Chance for this yield line to produce anything.")]
    [Range(0f, 1f)] public float chance = 1f;

    public bool TryRoll(out int count, int bonus = 0) {
        count = 0;
        if(item == null || Random.value > chance) {
            return false;
        }

        int min = Mathf.Max(0, minCount + bonus);
        int max = Mathf.Max(min, maxCount + bonus);
        count = Random.Range(min, max + 1);
        return count > 0;
    }
}
