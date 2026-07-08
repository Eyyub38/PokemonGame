using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Farming/Farmable Definition")]
public class FarmableDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this farmable. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in UI/debug messages. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer note or player-facing description for this farmable.")]
    [TextArea][SerializeField] string description;
    [Header("Activity")]
    [Tooltip("Activity definition that gates costs, requirements, XP and rewards for this farmable.")]
    [SerializeField] ActivityDefinition activity;
    [Header("Growth")]
    [Tooltip("In-game hours required before this farmable can be harvested.")]
    [Min(1)]
    [SerializeField] int growthHours = 8;
    [Tooltip("If enabled, harvesting restarts growth. If disabled, it stays harvested.")]
    [SerializeField] bool repeatable = true;
    [Header("Rewards")]
    [Tooltip("Items produced when this farmable is harvested.")]
    [SerializeField] List<FarmYield> yields = new List<FarmYield>();
    [Tooltip("Visual stages used while the farmable grows.")]
    [SerializeField] List<FarmGrowthStage> growthStages = new List<FarmGrowthStage>();
    [Header("Events")]
    [Tooltip("Optional event published when this farmable is harvested. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition harvestEvent;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public ActivityDefinition Activity => activity;
    public int GrowthHours => Mathf.Max(1, growthHours);
    public bool Repeatable => repeatable;
    public IReadOnlyList<FarmYield> Yields => yields;
    public IReadOnlyList<FarmGrowthStage> GrowthStages => growthStages;
    public GameEventDefinition HarvestEvent => harvestEvent;

    public FarmGrowthStage GetStage(float normalizedGrowth) {
        if(growthStages == null || growthStages.Count == 0) {
            return null;
        }

        FarmGrowthStage best = growthStages[0];
        foreach(var stage in growthStages) {
            if(stage != null && normalizedGrowth >= stage.normalizedAt) {
                best = stage;
            }
        }
        return best;
    }
}

[System.Serializable]
public class FarmYield {
    [Tooltip("Item produced by this farmable.")]
    public ItemBase item;
    [Tooltip("Minimum amount produced before skill bonuses.")]
    [Min(0)]
    public int minCount = 1;
    [Tooltip("Maximum amount produced before skill bonuses.")]
    [Min(0)]
    public int maxCount = 1;

    public int RollCount(int bonus = 0) {
        int min = Mathf.Max(0, minCount + bonus);
        int max = Mathf.Max(min, maxCount + bonus);
        return Random.Range(min, max + 1);
    }
}

[System.Serializable]
public class FarmGrowthStage {
    [Tooltip("Growth percentage where this stage starts. 0 is planted, 1 is fully grown.")]
    [Range(0f, 1f)] public float normalizedAt;
    [Tooltip("Sprite displayed while this stage is active.")]
    public Sprite sprite;
    [Tooltip("Short stage label used in messages, such as sprouting or blooming.")]
    public string label;
}
