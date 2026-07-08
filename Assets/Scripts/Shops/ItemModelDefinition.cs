using System.Collections.Generic;
using UnityEngine;

public enum ItemModelCategory {
    General,
    Medicine,
    Pokeball,
    Bait,
    Food,
    Tool,
    Care,
    CraftingMaterial,
    Collectible
}

public enum ItemModelModifierType {
    Potency,
    Capture,
    Friendship,
    Mood,
    Freshness,
    Durability,
    Yield,
    Research,
    Custom
}

[CreateAssetMenu(menuName = "Shops/Item Model Definition")]
public class ItemModelDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this model. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in shop UI/debug messages. Empty uses brand + item name or asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer note or player-facing description for this model.")]
    [TextArea]
    [SerializeField] string description;
    [Tooltip("Broad model category used by filters and shop UI.")]
    [SerializeField] ItemModelCategory category = ItemModelCategory.General;
    [Tooltip("Free-form tags used by shops, recipes and future UI filters.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Product")]
    [Tooltip("Actual inventory item granted when this model is purchased or crafted.")]
    [SerializeField] ItemBase item;
    [Tooltip("Brand/manufacturer attached to this model.")]
    [SerializeField] ItemBrandDefinition brand;
    [Tooltip("Optional model line text, such as Basic, Plus, Max, Field Blend or Prototype.")]
    [SerializeField] string modelLine;
    [Tooltip("Generic model quality tier used by sorting and future UI.")]
    [Min(0)]
    [SerializeField] int qualityTier;

    [Header("Balance Metadata")]
    [Tooltip("Effectiveness as a percent. Example: 20 means 20% potency, 80 means 80% potency.")]
    [Min(0f)]
    [SerializeField] float effectivenessPercent = 100f;
    [Tooltip("Optional price override. 0 uses the referenced item's price.")]
    [Min(0f)]
    [SerializeField] float buyPriceOverride;
    [Tooltip("Additional buy price multiplier applied after brand multiplier.")]
    [Min(0f)]
    [SerializeField] float buyPriceMultiplier = 1f;
    [Tooltip("Additional sell price multiplier applied after brand multiplier.")]
    [Min(0f)]
    [SerializeField] float sellPriceMultiplier = 1f;
    [Tooltip("Extra typed modifiers that future systems/UI can read without changing this class.")]
    [SerializeField] List<ItemModelModifier> modifiers = new List<ItemModelModifier>();

    [Header("Unlock Links")]
    [Tooltip("Optional recipe that teaches or crafts this model.")]
    [SerializeField] RecipeDefinition linkedRecipe;
    [Tooltip("Optional title, badge or permit suggested before this model appears in advanced shops.")]
    [SerializeField] TitleDefinition suggestedTitle;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName {
        get {
            if(!string.IsNullOrWhiteSpace(displayName)) {
                return displayName;
            }

            if(brand != null && item != null) {
                return string.IsNullOrWhiteSpace(modelLine)
                    ? $"{brand.DisplayName} {item.Name}"
                    : $"{brand.DisplayName} {modelLine} {item.Name}";
            }

            return name;
        }
    }
    public string Description => description;
    public ItemModelCategory Category => category;
    public IReadOnlyList<string> Tags => tags;
    public ItemBase Item => item;
    public ItemBrandDefinition Brand => brand;
    public string ModelLine => modelLine;
    public int QualityTier => Mathf.Max(0, qualityTier) + (brand != null ? brand.QualityBonus : 0);
    public float EffectivenessMultiplier => (Mathf.Max(0f, effectivenessPercent) / 100f) * (brand != null ? brand.EffectivenessMultiplier : 1f);
    public float BuyPriceMultiplier => Mathf.Max(0f, buyPriceMultiplier) * (brand != null ? brand.BuyPriceMultiplier : 1f);
    public float SellPriceMultiplier => Mathf.Max(0f, sellPriceMultiplier) * (brand != null ? brand.SellPriceMultiplier : 1f);
    public IReadOnlyList<ItemModelModifier> Modifiers => modifiers;
    public RecipeDefinition LinkedRecipe => linkedRecipe;
    public TitleDefinition SuggestedTitle => suggestedTitle;

    public float GetBaseBuyPrice() {
        if(buyPriceOverride > 0f) {
            return buyPriceOverride;
        }

        return item != null ? item.Price : 0f;
    }

    public float GetBuyPrice(float shopMultiplier = 1f) {
        return Mathf.Max(0f, Mathf.Ceil(GetBaseBuyPrice() * BuyPriceMultiplier * Mathf.Max(0f, shopMultiplier)));
    }

    public float GetSellPrice(float shopMultiplier = 1f) {
        float basePrice = item != null ? item.Price * 0.5f : GetBaseBuyPrice() * 0.5f;
        return Mathf.Max(0f, Mathf.Floor(basePrice * SellPriceMultiplier * Mathf.Max(0f, shopMultiplier)));
    }

    public bool HasTag(string tag) {
        if(string.IsNullOrWhiteSpace(tag) || tags == null) {
            return false;
        }

        foreach(var entry in tags) {
            if(string.Equals(entry, tag, System.StringComparison.OrdinalIgnoreCase)) {
                return true;
            }
        }

        return brand != null && brand.HasTag(tag);
    }

    public float GetModifier(ItemModelModifierType type, string customKey = null, float fallback = 0f) {
        if(modifiers == null) {
            return fallback;
        }

        foreach(var modifier in modifiers) {
            if(modifier == null || modifier.type != type) {
                continue;
            }

            if(type == ItemModelModifierType.Custom && !string.Equals(modifier.customKey, customKey, System.StringComparison.OrdinalIgnoreCase)) {
                continue;
            }

            return modifier.value;
        }

        return fallback;
    }
}

[System.Serializable]
public class ItemModelModifier {
    [Tooltip("What gameplay value this modifier describes.")]
    public ItemModelModifierType type = ItemModelModifierType.Potency;
    [Tooltip("Optional custom key used when type is Custom.")]
    public string customKey;
    [Tooltip("Modifier value. Meaning is decided by the system/UI that reads it.")]
    public float value;
    [Tooltip("If enabled, UI may display this value as a percent.")]
    public bool displayAsPercent;
}
