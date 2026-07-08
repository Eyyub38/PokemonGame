using UnityEngine;

public enum ShopShelfRequirementMode {
    ShelfViewed,
    ShelfInteractionCountAtLeast,
    ShelfTaggedInteractionCountAtLeast,
    OfferAddedFromShelf,
    HoursSinceLastShelfInteractionAtLeast,
    CanBrowseShelf,
    ShelfHasVisibleOffer,
    ShelfVisibleOfferCountAtLeast
}

[CreateAssetMenu(menuName = "Activities/Requirements/Shop Shelf Requirement")]
public class ShopShelfRequirement : ActivityRequirement {
    [Header("Target")]
    [Tooltip("How shelf state or history should be checked.")]
    [SerializeField] ShopShelfRequirementMode mode = ShopShelfRequirementMode.ShelfViewed;
    [Tooltip("Specific shelf checked by shelf-specific modes.")]
    [SerializeField] ShopShelfDefinition shelf;
    [Tooltip("Optional shop used by visibility/browse checks.")]
    [SerializeField] ShopCatalog shop;
    [Tooltip("Optional shop id filter for history checks. Empty uses assigned Shop or accepts any shop.")]
    [SerializeField] string shopId = string.Empty;
    [Tooltip("Interaction type checked by Shelf Interaction Count At Least and Shelf Tagged Interaction Count At Least.")]
    [SerializeField] ShopShelfInteractionType interactionType = ShopShelfInteractionType.Viewed;
    [Tooltip("Tag checked by Shelf Tagged Interaction Count At Least mode.")]
    [SerializeField] string requiredTag = string.Empty;
    [Tooltip("Offer id checked by Offer Added From Shelf and Shelf Has Visible Offer modes.")]
    [SerializeField] string offerId = string.Empty;

    [Header("Threshold")]
    [Tooltip("Required count used by count-based modes.")]
    [Min(0)]
    [SerializeField] int requiredCount = 1;
    [Tooltip("Required in-game hours since last interaction for Hours Since Last Shelf Interaction At Least.")]
    [Min(0)]
    [SerializeField] int requiredHours;
    [Tooltip("If enabled, blocked shelf attempts also count.")]
    [SerializeField] bool includeBlockedAttempts;
    [Tooltip("If enabled, the final result is inverted.")]
    [SerializeField] bool invertResult;

    public ShopShelfRequirementMode Mode => mode;
    public ShopShelfDefinition Shelf => shelf;
    public ShopCatalog Shop => shop;
    public string ShopId => shopId;
    public ShopShelfInteractionType InteractionType => interactionType;
    public string RequiredTag => requiredTag;
    public string OfferId => offerId;
    public int RequiredCount => Mathf.Max(0, requiredCount);
    public int RequiredHours => Mathf.Max(0, requiredHours);
    public bool IncludeBlockedAttempts => includeBlockedAttempts;
    public bool InvertResult => invertResult;

    public override bool IsMet(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerShopShelfLog>() : null;
        bool met = Evaluate(player, log);
        return invertResult ? !met : met;
    }

    bool Evaluate(PlayerController player, PlayerShopShelfLog log) {
        switch(mode) {
            case ShopShelfRequirementMode.ShelfInteractionCountAtLeast:
                return log != null && log.GetInteractionCount(shelf, ResolveShopId(), interactionType, includeBlockedAttempts) >= RequiredCount;
            case ShopShelfRequirementMode.ShelfTaggedInteractionCountAtLeast:
                return log != null && log.GetTaggedInteractionCount(requiredTag, ResolveShopId(), interactionType, includeBlockedAttempts) >= RequiredCount;
            case ShopShelfRequirementMode.OfferAddedFromShelf:
                return log != null && log.GetOfferAddCount(shelf, offerId, ResolveShopId(), includeBlockedAttempts) >= Mathf.Max(1, RequiredCount);
            case ShopShelfRequirementMode.HoursSinceLastShelfInteractionAtLeast:
                if(log == null) {
                    return false;
                }
                int hours = log.GetHoursSinceLastInteraction(shelf, ResolveShopId(), includeBlockedAttempts);
                return hours >= 0 && hours >= RequiredHours;
            case ShopShelfRequirementMode.CanBrowseShelf:
                return shelf != null && shop != null && shelf.CanBrowse(player, shop, out _);
            case ShopShelfRequirementMode.ShelfHasVisibleOffer:
                return HasVisibleOffer(player);
            case ShopShelfRequirementMode.ShelfVisibleOfferCountAtLeast:
                return GetVisibleOfferCount(player) >= RequiredCount;
            default:
                return log != null && log.HasViewed(shelf, ResolveShopId(), includeBlockedAttempts);
        }
    }

    bool HasVisibleOffer(PlayerController player) {
        if(shelf == null || shop == null) {
            return false;
        }

        var offers = shelf.GetShelfOffers(shop, player, out _);
        if(string.IsNullOrWhiteSpace(offerId)) {
            return offers.Count > 0;
        }

        foreach(var offer in offers) {
            if(offer != null && offer.OfferId == offerId) {
                return true;
            }
        }

        return false;
    }

    int GetVisibleOfferCount(PlayerController player) {
        if(shelf == null || shop == null) {
            return 0;
        }

        return shelf.GetShelfOffers(shop, player, out _).Count;
    }

    string ResolveShopId() {
        if(!string.IsNullOrWhiteSpace(shopId)) {
            return shopId;
        }

        return shop != null ? shop.ShopId : string.Empty;
    }
}
