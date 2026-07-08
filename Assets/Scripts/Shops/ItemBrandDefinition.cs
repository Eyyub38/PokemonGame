using System.Collections.Generic;
using UnityEngine;

public enum ItemBrandTier {
    Budget,
    Standard,
    Premium,
    Specialist,
    Rare,
    Experimental
}

[CreateAssetMenu(menuName = "Shops/Item Brand Definition")]
public class ItemBrandDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this item brand. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in shop UI/debug messages. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer note or player-facing description for this brand.")]
    [TextArea]
    [SerializeField] string description;
    [Tooltip("Broad market tier used by filters, shop catalogs and future UI.")]
    [SerializeField] ItemBrandTier tier = ItemBrandTier.Standard;
    [Tooltip("Free-form tags used by shops, access rules and future UI filters.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Default Modifiers")]
    [Tooltip("Default buy price multiplier applied to models from this brand.")]
    [Min(0f)]
    [SerializeField] float buyPriceMultiplier = 1f;
    [Tooltip("Default sell price multiplier applied to models from this brand.")]
    [Min(0f)]
    [SerializeField] float sellPriceMultiplier = 1f;
    [Tooltip("Default effectiveness multiplier applied to models from this brand.")]
    [Min(0f)]
    [SerializeField] float effectivenessMultiplier = 1f;
    [Tooltip("Generic quality score used by sorting, shop UI and future balance logic.")]
    [Min(0)]
    [SerializeField] int qualityBonus;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public ItemBrandTier Tier => tier;
    public IReadOnlyList<string> Tags => tags;
    public float BuyPriceMultiplier => Mathf.Max(0f, buyPriceMultiplier);
    public float SellPriceMultiplier => Mathf.Max(0f, sellPriceMultiplier);
    public float EffectivenessMultiplier => Mathf.Max(0f, effectivenessMultiplier);
    public int QualityBonus => Mathf.Max(0, qualityBonus);

    public bool HasTag(string tag) {
        if(string.IsNullOrWhiteSpace(tag) || tags == null) {
            return false;
        }

        foreach(var entry in tags) {
            if(string.Equals(entry, tag, System.StringComparison.OrdinalIgnoreCase)) {
                return true;
            }
        }

        return false;
    }
}
