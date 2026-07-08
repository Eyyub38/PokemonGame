using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum ShopShelfSourceAction {
    None,
    ViewShelf,
    BeginBasket,
    AddDefaultOffer,
    AddFirstVisibleOffer,
    AddAllVisibleOffers
}

public class ShopShelfSource : MonoBehaviour, IPlayerTriggerable, Interactable {
    [Header("Identity")]
    [Tooltip("Stable source id written into shelf and basket logs. Empty uses shelf id or this GameObject name.")]
    [SerializeField] string sourceId = string.Empty;
    [Tooltip("Readable source name for debug/future UI. Empty uses shelf display name or this GameObject name.")]
    [SerializeField] string displayName = string.Empty;

    [Header("Shelf")]
    [Tooltip("Shop whose catalog, prices and stock this shelf reads from.")]
    [SerializeField] ShopCatalog shop;
    [Tooltip("Shelf definition that filters visible offers and controls default add behavior.")]
    [SerializeField] ShopShelfDefinition shelf;
    [Tooltip("Optional explicit player. Empty uses the triggering/interacting player or PlayerController.i.")]
    [SerializeField] PlayerController playerOverride;
    [Tooltip("If enabled, Begin Basket clears any existing basket for this shop.")]
    [SerializeField] bool clearExistingBasketOnBegin;

    [Header("Actions")]
    [Tooltip("Action applied when this source starts. UI can ignore this and call TryAddOffer or GetVisibleOffers directly.")]
    [SerializeField] ShopShelfSourceAction startAction = ShopShelfSourceAction.None;
    [Tooltip("Action applied when player trigger calls this source.")]
    [SerializeField] ShopShelfSourceAction triggerAction = ShopShelfSourceAction.ViewShelf;
    [Tooltip("Action applied when an Interactable flow calls Interact.")]
    [SerializeField] ShopShelfSourceAction interactAction = ShopShelfSourceAction.ViewShelf;
    [Tooltip("Bundle count used by Add All Visible Offers. 0 uses shelf Default Bundles.")]
    [Min(0)]
    [SerializeField] int addAllBundlesOverride;
    [Tooltip("If enabled, player trigger applies Trigger Action.")]
    [SerializeField] bool applyOnPlayerTrigger = true;
    [Tooltip("If enabled, this trigger can be called repeatedly by the player.")]
    [SerializeField] bool triggerRepeatedly = true;

    [Header("Feedback")]
    [Tooltip("If enabled, result text is shown through the existing DialogManager when available.")]
    [SerializeField] bool showDialogFeedback;
    [Tooltip("If enabled, blocked shelf actions are written to GameDebug.")]
    [SerializeField] bool logBlockedAttempts = true;
    [Tooltip("If enabled, successful shelf actions are written to GameDebug.")]
    [SerializeField] bool logSuccessfulAttempts;

    public string SourceId => ResolveSourceId();
    public string DisplayName => !string.IsNullOrWhiteSpace(displayName) ? displayName : shelf != null ? shelf.DisplayName : gameObject.name;
    public ShopCatalog Shop => shop;
    public ShopShelfDefinition Shelf => shelf;
    public ShopShelfSourceAction StartAction => startAction;
    public ShopShelfSourceAction TriggerAction => triggerAction;
    public ShopShelfSourceAction InteractAction => interactAction;
    public bool TriggerRepeatedly => triggerRepeatedly;

    void Start() {
        if(startAction != ShopShelfSourceAction.None) {
            ApplyAction(startAction, ResolvePlayer(null), out _);
        }
    }

    public void OnPlayerTriggered(PlayerController player) {
        if(!applyOnPlayerTrigger || triggerAction == ShopShelfSourceAction.None) {
            return;
        }

        ApplyAction(triggerAction, ResolvePlayer(player), out _);
    }

    public IEnumerator Interact(Transform initiator) {
        if(interactAction == ShopShelfSourceAction.None) {
            yield break;
        }

        var player = ResolvePlayer(initiator != null ? initiator.GetComponent<PlayerController>() : null);
        ApplyAction(interactAction, player, out var feedback);
        if(showDialogFeedback && DialogManager.i != null && !string.IsNullOrWhiteSpace(feedback)) {
            yield return DialogManager.i.ShowDialogText(feedback);
        }
    }

    public List<ShopCatalogEntry> GetVisibleOffers(PlayerController player) {
        player = ResolvePlayer(player);
        if(shelf == null || shop == null) {
            return new List<ShopCatalogEntry>();
        }

        return shelf.GetShelfOffers(shop, player, out _);
    }

    public bool TryViewShelf(PlayerController player, out string failureMessage) {
        player = ResolvePlayer(player);
        if(!ValidateShelf(player, out failureMessage)) {
            RecordBlocked(player, failureMessage);
            return false;
        }

        var offers = shelf.GetShelfOffers(shop, player, out failureMessage);
        if(!string.IsNullOrWhiteSpace(failureMessage)) {
            RecordBlocked(player, failureMessage);
            return false;
        }

        RecordInteraction(player, ShopShelfInteractionType.Viewed, null, 0, false, null);
        shelf.PublishViewed(player, shop, this);

        if(logSuccessfulAttempts) {
            GameDebug.Success($"{shelf.DisplayName} viewed with {offers.Count} offer(s).", GameDebugCategory.Shop, this, "ShopShelfSource");
        }

        failureMessage = null;
        return true;
    }

    public bool TryBeginBasket(PlayerController player, out string failureMessage) {
        player = ResolvePlayer(player);
        if(!ValidateShelf(player, out failureMessage)) {
            RecordBlocked(player, failureMessage);
            return false;
        }

        var log = player.GetComponent<PlayerShopBasketLog>() ?? player.gameObject.AddComponent<PlayerShopBasketLog>();
        if(!log.BeginBasket(shop, clearExistingBasketOnBegin, SourceId)) {
            failureMessage = "Could not begin a basket for this shelf.";
            RecordBlocked(player, failureMessage);
            return false;
        }

        RecordInteraction(player, ShopShelfInteractionType.Viewed, null, 0, false, null);
        failureMessage = null;
        return true;
    }

    public bool TryAddDefaultOffer(PlayerController player, out string failureMessage) {
        player = ResolvePlayer(player);
        if(!ValidateShelf(player, out failureMessage)) {
            RecordBlocked(player, failureMessage);
            return false;
        }

        var entry = shelf.GetDefaultOffer(shop, player, out failureMessage);
        if(entry == null) {
            RecordBlocked(player, failureMessage);
            return false;
        }

        return TryAddEntry(player, entry, shelf.DefaultBundles, ShopShelfInteractionType.AddedDefaultOffer, out failureMessage);
    }

    public bool TryAddFirstVisibleOffer(PlayerController player, out string failureMessage) {
        player = ResolvePlayer(player);
        if(!ValidateShelf(player, out failureMessage)) {
            RecordBlocked(player, failureMessage);
            return false;
        }

        var entry = shelf.GetShelfOffers(shop, player, out failureMessage).FirstOrDefault();
        if(entry == null) {
            failureMessage ??= "This shelf has no visible offers.";
            RecordBlocked(player, failureMessage);
            return false;
        }

        return TryAddEntry(player, entry, shelf.DefaultBundles, ShopShelfInteractionType.AddedFirstVisibleOffer, out failureMessage);
    }

    public bool TryAddOffer(PlayerController player, string offerId, int bundles, out string failureMessage) {
        player = ResolvePlayer(player);
        if(!ValidateShelf(player, out failureMessage)) {
            RecordBlocked(player, failureMessage);
            return false;
        }

        var entry = shelf.GetShelfOffers(shop, player, out failureMessage)
            .FirstOrDefault(offer => offer != null && offer.OfferId == offerId);
        if(entry == null) {
            failureMessage = $"Shelf does not contain offer '{offerId}'.";
            RecordBlocked(player, failureMessage);
            return false;
        }

        return TryAddEntry(player, entry, bundles, ShopShelfInteractionType.AddedOffer, out failureMessage);
    }

    public int TryAddAllVisibleOffers(PlayerController player, out string failureMessage) {
        player = ResolvePlayer(player);
        if(!ValidateShelf(player, out failureMessage)) {
            RecordBlocked(player, failureMessage);
            return 0;
        }

        var offers = shelf.GetShelfOffers(shop, player, out failureMessage);
        if(offers.Count == 0) {
            failureMessage ??= "This shelf has no visible offers.";
            RecordBlocked(player, failureMessage);
            return 0;
        }

        int added = 0;
        int failed = 0;
        string lastFailure = null;
        int bundles = addAllBundlesOverride > 0 ? addAllBundlesOverride : shelf.DefaultBundles;
        foreach(var entry in offers) {
            if(TryAddEntry(player, entry, bundles, ShopShelfInteractionType.AddedAllVisibleOffers, out var entryFailure)) {
                added++;
            } else {
                failed++;
                if(!string.IsNullOrWhiteSpace(entryFailure)) {
                    lastFailure = entryFailure;
                }
            }
        }

        if(failed > 0) {
            failureMessage = added > 0
                ? $"{added} shelf offer(s) added, {failed} failed. {lastFailure}"
                : lastFailure ?? "No visible shelf offers were added.";
        } else if(added == 0) {
            failureMessage = "No visible shelf offers were added.";
            RecordBlocked(player, failureMessage);
        } else {
            failureMessage = null;
        }

        return added;
    }

    bool TryAddEntry(PlayerController player, ShopCatalogEntry entry, int bundles, ShopShelfInteractionType interactionType, out string failureMessage) {
        failureMessage = null;
        if(player == null || entry == null) {
            failureMessage = "A player and shelf offer are required.";
            RecordBlocked(player, failureMessage);
            return false;
        }

        var basket = player.GetComponent<PlayerShopBasketLog>() ?? player.gameObject.AddComponent<PlayerShopBasketLog>();
        if(!basket.AddOffer(shop, entry, Mathf.Max(1, bundles), SourceId, out failureMessage)) {
            RecordBlocked(player, failureMessage, entry, bundles);
            return false;
        }

        RecordInteraction(player, interactionType, entry, bundles, false, null);
        shelf.PublishOfferAdded(player, shop, entry, Mathf.Max(1, bundles), this);

        if(logSuccessfulAttempts) {
            GameDebug.Success($"{entry.DisplayName} added from {shelf.DisplayName}.", GameDebugCategory.Shop, this, "ShopShelfSource");
        }

        return true;
    }

    bool ApplyAction(ShopShelfSourceAction action, PlayerController player, out string feedback) {
        feedback = null;
        switch(action) {
            case ShopShelfSourceAction.BeginBasket:
                if(TryBeginBasket(player, out feedback)) {
                    feedback = "Basket started.";
                    return true;
                }
                return false;
            case ShopShelfSourceAction.AddDefaultOffer:
                if(TryAddDefaultOffer(player, out feedback)) {
                    feedback = "Shelf offer added.";
                    return true;
                }
                return false;
            case ShopShelfSourceAction.AddFirstVisibleOffer:
                if(TryAddFirstVisibleOffer(player, out feedback)) {
                    feedback = "Shelf offer added.";
                    return true;
                }
                return false;
            case ShopShelfSourceAction.AddAllVisibleOffers:
                int added = TryAddAllVisibleOffers(player, out feedback);
                if(added > 0) {
                    feedback = string.IsNullOrWhiteSpace(feedback)
                        ? $"{added} shelf offer(s) added."
                        : feedback;
                    return true;
                }
                return false;
            case ShopShelfSourceAction.ViewShelf:
                if(TryViewShelf(player, out feedback)) {
                    feedback = "Shelf viewed.";
                    return true;
                }
                return false;
            default:
                return false;
        }
    }

    bool ValidateShelf(PlayerController player, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required to use shop shelves.";
            return false;
        }

        if(shop == null) {
            failureMessage = "Shop shelf source has no shop assigned.";
            return false;
        }

        if(shelf == null) {
            failureMessage = "Shop shelf source has no shelf definition assigned.";
            return false;
        }

        return shelf.CanBrowse(player, shop, out failureMessage);
    }

    void RecordInteraction(PlayerController player, ShopShelfInteractionType interactionType, ShopCatalogEntry entry, int bundles, bool blocked, string failureMessage) {
        var log = player != null ? player.GetComponent<PlayerShopShelfLog>() ?? player.gameObject.AddComponent<PlayerShopShelfLog>() : null;
        log?.RecordInteraction(shelf, shop, interactionType, SourceId, entry, bundles, blocked, failureMessage);
    }

    void RecordBlocked(PlayerController player, string failureMessage, ShopCatalogEntry entry = null, int bundles = 0) {
        RecordInteraction(player, ShopShelfInteractionType.Blocked, entry, bundles, true, failureMessage);
        shelf?.PublishBlocked(player, shop, failureMessage, this);
        if(logBlockedAttempts && !string.IsNullOrWhiteSpace(failureMessage)) {
            GameDebug.Warning(failureMessage, GameDebugCategory.Shop, player != null ? player : this, "ShopShelfSource");
        }
    }

    PlayerController ResolvePlayer(PlayerController player) {
        if(playerOverride != null) {
            return playerOverride;
        }

        if(player != null) {
            return player;
        }

        if(PlayerController.i != null) {
            return PlayerController.i;
        }

        return FindAnyObjectByType<PlayerController>();
    }

    string ResolveSourceId() {
        if(!string.IsNullOrWhiteSpace(sourceId)) {
            return sourceId;
        }

        if(shelf != null) {
            return $"shelf:{shelf.Id}";
        }

        return gameObject.name;
    }
}
