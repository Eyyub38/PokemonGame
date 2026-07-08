using UnityEngine;

public enum ShopBasketRequirementMode {
    HasActiveBasket,
    BasketEmpty,
    BasketFromShop,
    ContainsOffer,
    BundleCountAtLeast,
    LineCountAtLeast,
    SnapshotTotalAtLeast,
    CanAffordBasket
}

[CreateAssetMenu(menuName = "Activities/Requirements/Shop Basket Requirement")]
public class ShopBasketRequirement : ActivityRequirement {
    [Tooltip("Which shop basket value this requirement checks.")]
    [SerializeField] ShopBasketRequirementMode mode = ShopBasketRequirementMode.HasActiveBasket;
    [Tooltip("Optional shop used by shop-specific modes and current price checks.")]
    [SerializeField] ShopCatalog shop;
    [Tooltip("Optional shop id override for Basket From Shop mode. Empty uses assigned Shop.")]
    [SerializeField] string shopId = string.Empty;
    [Tooltip("Offer id checked by Contains Offer and Bundle Count modes.")]
    [SerializeField] string offerId = string.Empty;
    [Tooltip("Required count used by line and bundle count modes.")]
    [Min(0)]
    [SerializeField] int requiredCount = 1;
    [Tooltip("Required total used by Snapshot Total At Least mode.")]
    [Min(0f)]
    [SerializeField] float requiredTotal = 1f;
    [Tooltip("If enabled, the selected basket condition must be true. If disabled, it must be false.")]
    [SerializeField] bool mustBeMet = true;

    public override bool IsMet(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerShopBasketLog>() : null;
        bool result = mode switch {
            ShopBasketRequirementMode.BasketEmpty => log == null || !log.HasActiveBasket || log.GetLineCount() == 0,
            ShopBasketRequirementMode.BasketFromShop => log != null && log.HasActiveBasket && log.ActiveBasket.shopId == ResolveShopId(),
            ShopBasketRequirementMode.ContainsOffer => log != null && log.ContainsOffer(offerId),
            ShopBasketRequirementMode.BundleCountAtLeast => log != null && log.GetBundleCount(offerId) >= Mathf.Max(0, requiredCount),
            ShopBasketRequirementMode.LineCountAtLeast => log != null && log.GetLineCount() >= Mathf.Max(0, requiredCount),
            ShopBasketRequirementMode.SnapshotTotalAtLeast => log != null && log.GetSnapshotTotal() >= Mathf.Max(0f, requiredTotal),
            ShopBasketRequirementMode.CanAffordBasket => log != null && log.HasActiveBasket && log.CanAffordCurrentBasket(shop),
            _ => log != null && log.HasActiveBasket
        };

        return mustBeMet ? result : !result;
    }

    string ResolveShopId() {
        if(!string.IsNullOrWhiteSpace(shopId)) {
            return shopId;
        }

        return shop != null ? shop.ShopId : string.Empty;
    }
}
