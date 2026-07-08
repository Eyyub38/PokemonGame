using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum ShopCheckoutTerminalKind {
    SelfCheckout,
    Cashier,
    ServiceCounter,
    Kiosk,
    Custom
}

public enum ShopPaymentMode {
    Money,
    Free,
    ManualApproval
}

public enum ShopCheckoutTerminalAction {
    PreviewQuote,
    CheckoutBasket,
    ClearBasket
}

[CreateAssetMenu(menuName = "Shops/Payment Rule Definition")]
public class ShopPaymentRuleDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this payment rule. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in debug/future checkout UI. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note or player-facing description for this payment rule.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Default checkout terminal kind this rule represents.")]
    [SerializeField] ShopCheckoutTerminalKind terminalKind = ShopCheckoutTerminalKind.SelfCheckout;
    [Tooltip("Optional icon used by future checkout UI.")]
    [SerializeField] Sprite icon;
    [Tooltip("Free-form tags such as self-checkout, cashier, premium, airport, mart, police or clinic.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Catalog Filters")]
    [Tooltip("If assigned, this payment rule only works with this exact shop catalog.")]
    [SerializeField] ShopCatalogDefinition requiredCatalog;
    [Tooltip("If not empty, this payment rule only works with these catalog types.")]
    [SerializeField] List<ShopCatalogType> allowedCatalogTypes = new List<ShopCatalogType>();
    [Tooltip("Catalog tags required before this payment rule can be used. Empty means no catalog tag filter.")]
    [SerializeField] List<string> requiredCatalogTags = new List<string>();
    [Tooltip("How required catalog tags are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode catalogTagMatchMode = ConsequenceRequirementMatchMode.All;

    [Header("Basket Limits")]
    [Tooltip("Maximum basket line count accepted by this payment rule. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxLineCount;
    [Tooltip("Maximum total bundle count accepted by this payment rule. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxBundleCount;
    [Tooltip("Minimum item subtotal required before checkout is allowed. 0 means no minimum.")]
    [Min(0f)]
    [SerializeField] float minimumSubtotal;
    [Tooltip("Maximum item subtotal accepted by this payment rule. 0 means unlimited.")]
    [Min(0f)]
    [SerializeField] float maximumSubtotal;

    [Header("Payment")]
    [Tooltip("How the checkout is paid. Money charges Wallet, Free waives payment, Manual Approval records the quote without charging Wallet.")]
    [SerializeField] ShopPaymentMode paymentMode = ShopPaymentMode.Money;
    [Tooltip("Flat fee added to the basket item subtotal before payment. 0 means no flat fee.")]
    [Min(0f)]
    [SerializeField] float flatServiceFee;
    [Tooltip("Percentage fee added to the item subtotal. 0.10 means +10%.")]
    [Min(0f)]
    [SerializeField] float percentageServiceFee;
    [Tooltip("Percentage discount removed from item subtotal plus fee. 0.10 means -10%.")]
    [Min(0f)]
    [SerializeField] float discountPercent;
    [Tooltip("If enabled, final amount due is rounded up with Mathf.Ceil.")]
    [SerializeField] bool roundAmountUp = true;
    [Tooltip("If enabled, Money mode checks Wallet before checkout mutates inventory or stock.")]
    [SerializeField] bool requireWalletFunds = true;

    [Header("Access")]
    [Tooltip("Optional title, badge, permit or license required before this payment rule can be used.")]
    [SerializeField] TitleDefinition requiredTitle;
    [Tooltip("Optional milestone required before this payment rule can be used.")]
    [SerializeField] MilestoneDefinition requiredMilestone;
    [Tooltip("Optional faction whose reputation gates this payment rule.")]
    [SerializeField] ReputationFactionDefinition requiredFaction;
    [Tooltip("Minimum required reputation with the selected faction.")]
    [SerializeField] int requiredReputation;
    [Tooltip("Optional world event whose active state gates this payment rule.")]
    [SerializeField] WorldEventDefinition requiredWorldEvent;
    [Tooltip("Expected active state for Required World Event.")]
    [SerializeField] bool requiredWorldEventActive = true;
    [Tooltip("How extra requirements are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode requirementMatchMode = ConsequenceRequirementMatchMode.All;
    [Tooltip("Extra activity-style requirements checked before this payment rule can be used.")]
    [SerializeField] List<ActivityRequirement> requirements = new List<ActivityRequirement>();
    [Tooltip("Message/debug reason used when this payment rule is locked.")]
    [TextArea]
    [SerializeField] string lockedMessage = "This checkout option is not available.";

    [Header("Events")]
    [Tooltip("Optional event published when checkout succeeds through this payment rule.")]
    [SerializeField] GameEventDefinition paidEvent;
    [Tooltip("Optional event published when checkout is blocked by this payment rule.")]
    [SerializeField] GameEventDefinition blockedEvent;
    [Tooltip("If enabled, checkout events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed = true;
    [Tooltip("If enabled, checkout events are written to GameDebugLogger.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public ShopCheckoutTerminalKind TerminalKind => terminalKind;
    public Sprite Icon => icon;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public ShopCatalogDefinition RequiredCatalog => requiredCatalog;
    public IReadOnlyList<ShopCatalogType> AllowedCatalogTypes => allowedCatalogTypes != null ? (IReadOnlyList<ShopCatalogType>)allowedCatalogTypes : Array.Empty<ShopCatalogType>();
    public IReadOnlyList<string> RequiredCatalogTags => requiredCatalogTags != null ? (IReadOnlyList<string>)requiredCatalogTags : Array.Empty<string>();
    public int MaxLineCount => Mathf.Max(0, maxLineCount);
    public int MaxBundleCount => Mathf.Max(0, maxBundleCount);
    public float MinimumSubtotal => Mathf.Max(0f, minimumSubtotal);
    public float MaximumSubtotal => Mathf.Max(0f, maximumSubtotal);
    public ShopPaymentMode PaymentMode => paymentMode;
    public float FlatServiceFee => Mathf.Max(0f, flatServiceFee);
    public float PercentageServiceFee => Mathf.Max(0f, percentageServiceFee);
    public float DiscountPercent => Mathf.Max(0f, discountPercent);
    public bool RoundAmountUp => roundAmountUp;
    public bool RequireWalletFunds => requireWalletFunds;
    public TitleDefinition RequiredTitle => requiredTitle;
    public MilestoneDefinition RequiredMilestone => requiredMilestone;
    public ReputationFactionDefinition RequiredFaction => requiredFaction;
    public int RequiredReputation => requiredReputation;
    public WorldEventDefinition RequiredWorldEvent => requiredWorldEvent;
    public bool RequiredWorldEventActive => requiredWorldEventActive;
    public IReadOnlyList<ActivityRequirement> Requirements => requirements != null ? (IReadOnlyList<ActivityRequirement>)requirements : Array.Empty<ActivityRequirement>();

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    public bool TryBuildPaymentQuote(PlayerController player, ShopCatalog shop, PlayerShopBasketLog basketLog, string sourceId, out ShopCheckoutPaymentQuote quote, out string failureMessage) {
        quote = null;
        if(!CanUse(player, shop, basketLog, out failureMessage)) {
            PublishPaymentEvent(blockedEvent, "blocked", null, player, shop, sourceId, GameEventImportance.Warning, failureMessage);
            return false;
        }

        float itemSubtotal = basketLog.GetCurrentTotal(shop, out failureMessage);
        if(!string.IsNullOrWhiteSpace(failureMessage)) {
            PublishPaymentEvent(blockedEvent, "blocked", null, player, shop, sourceId, GameEventImportance.Warning, failureMessage);
            return false;
        }

        float serviceFee = FlatServiceFee + itemSubtotal * PercentageServiceFee;
        float discount = Mathf.Max(0f, (itemSubtotal + serviceFee) * DiscountPercent);
        float amountDue = Mathf.Max(0f, itemSubtotal + serviceFee - discount);
        if(paymentMode == ShopPaymentMode.Free) {
            discount = itemSubtotal + serviceFee;
            amountDue = 0f;
        }

        if(roundAmountUp) {
            serviceFee = Mathf.Ceil(serviceFee);
            discount = Mathf.Ceil(discount);
            amountDue = Mathf.Ceil(amountDue);
        }

        quote = new ShopCheckoutPaymentQuote {
            paymentRuleId = Id,
            paymentRuleName = DisplayName,
            terminalKind = terminalKind,
            paymentMode = paymentMode,
            itemSubtotal = Mathf.Max(0f, itemSubtotal),
            serviceFee = Mathf.Max(0f, serviceFee),
            discountTotal = Mathf.Min(Mathf.Max(0f, discount), Mathf.Max(0f, itemSubtotal + serviceFee)),
            amountDue = Mathf.Max(0f, amountDue),
            sourceId = string.IsNullOrWhiteSpace(sourceId) ? Id : sourceId,
            message = $"{DisplayName}: {amountDue:0} due."
        };

        if(paymentMode == ShopPaymentMode.Money && requireWalletFunds && quote.AmountToCharge > 0f && (Wallet.i == null || !Wallet.i.HasMoney(quote.AmountToCharge))) {
            failureMessage = $"You need {quote.AmountToCharge:0} money for {DisplayName}.";
            PublishPaymentEvent(blockedEvent, "blocked", quote, player, shop, sourceId, GameEventImportance.Warning, failureMessage);
            return false;
        }

        failureMessage = null;
        return true;
    }

    public bool CanUse(PlayerController player, ShopCatalog shop, PlayerShopBasketLog basketLog, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required for checkout.";
            return false;
        }

        if(shop == null || shop.Catalog == null) {
            failureMessage = "A shop catalog is required for checkout.";
            return false;
        }

        if(basketLog == null || !basketLog.HasActiveBasket || basketLog.GetLineCount() == 0) {
            failureMessage = "The basket is empty.";
            return false;
        }

        if(basketLog.ActiveBasket.shopId != shop.ShopId) {
            failureMessage = $"Basket belongs to {basketLog.ActiveBasket.shopName}, not {shop.Catalog.DisplayName}.";
            return false;
        }

        if(requiredCatalog != null && shop.Catalog != requiredCatalog) {
            failureMessage = $"{DisplayName} cannot be used at this shop.";
            return false;
        }

        if(allowedCatalogTypes != null && allowedCatalogTypes.Count > 0 && !allowedCatalogTypes.Contains(shop.Catalog.CatalogType)) {
            failureMessage = $"{DisplayName} cannot be used for this shop type.";
            return false;
        }

        if(!MatchesTags(requiredCatalogTags, catalogTagMatchMode, tag => shop.Catalog.HasTag(tag))) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{DisplayName} is not available for this shop." : lockedMessage;
            return false;
        }

        if(requiredTitle != null && !(player.GetComponent<PlayerTitles>()?.HasTitle(requiredTitle) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need {requiredTitle.DisplayName}." : lockedMessage;
            return false;
        }

        if(requiredMilestone != null && !(player.GetComponent<PlayerMilestones>()?.HasMilestone(requiredMilestone) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need {requiredMilestone.DisplayName} first." : lockedMessage;
            return false;
        }

        if(requiredFaction != null) {
            int reputation = player.GetComponent<PlayerReputation>()?.GetReputation(requiredFaction) ?? 0;
            if(reputation < requiredReputation) {
                failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need more reputation with {requiredFaction.DisplayName}." : lockedMessage;
                return false;
            }
        }

        if(requiredWorldEvent != null) {
            bool active = WorldEventManager.i != null && WorldEventManager.i.IsEventActive(requiredWorldEvent);
            if(active != requiredWorldEventActive) {
                failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{DisplayName} is not available right now." : lockedMessage;
                return false;
            }
        }

        if(!ConsequenceChainDefinition.RequirementsMet(player, requirements, requirementMatchMode, out failureMessage)) {
            return false;
        }

        int lineCount = basketLog.GetLineCount();
        int bundleCount = basketLog.GetBundleCount();
        if(MaxLineCount > 0 && lineCount > MaxLineCount) {
            failureMessage = $"{DisplayName} accepts at most {MaxLineCount} basket line(s).";
            return false;
        }

        if(MaxBundleCount > 0 && bundleCount > MaxBundleCount) {
            failureMessage = $"{DisplayName} accepts at most {MaxBundleCount} bundle(s).";
            return false;
        }

        float itemSubtotal = basketLog.GetCurrentTotal(shop, out failureMessage);
        if(!string.IsNullOrWhiteSpace(failureMessage)) {
            return false;
        }

        if(MinimumSubtotal > 0f && itemSubtotal < MinimumSubtotal) {
            failureMessage = $"{DisplayName} requires at least {MinimumSubtotal:0} money in the basket.";
            return false;
        }

        if(MaximumSubtotal > 0f && itemSubtotal > MaximumSubtotal) {
            failureMessage = $"{DisplayName} accepts baskets up to {MaximumSubtotal:0} money.";
            return false;
        }

        failureMessage = null;
        return true;
    }

    public void PublishPaid(ShopCheckoutPaymentQuote quote, PlayerController player, ShopCatalog shop, string sourceId) {
        PublishPaymentEvent(paidEvent, "paid", quote, player, shop, sourceId, GameEventImportance.Success, null);
    }

    public void PublishBlocked(ShopCheckoutPaymentQuote quote, PlayerController player, ShopCatalog shop, string sourceId, string failureMessage) {
        PublishPaymentEvent(blockedEvent, "blocked", quote, player, shop, sourceId, GameEventImportance.Warning, failureMessage);
    }

    bool MatchesTags(List<string> requiredTags, ConsequenceRequirementMatchMode matchMode, Func<string, bool> hasTag) {
        var activeTags = requiredTags?.Where(tag => !string.IsNullOrWhiteSpace(tag)).ToList() ?? new List<string>();
        if(activeTags.Count == 0) {
            return true;
        }

        if(matchMode == ConsequenceRequirementMatchMode.Any) {
            return activeTags.Any(hasTag);
        }

        return activeTags.All(hasTag);
    }

    void PublishPaymentEvent(GameEventDefinition eventDefinition, string phase, ShopCheckoutPaymentQuote quote, PlayerController player, ShopCatalog shop, string sourceId, GameEventImportance importance, string failureMessage) {
        GameEventPublishing.PublishOptional(
            eventDefinition,
            $"shop.checkout.{phase}.{Id}.{shop?.ShopId ?? "shop"}",
            !string.IsNullOrWhiteSpace(failureMessage) ? failureMessage : $"{DisplayName} checkout {phase}.",
            GameEventCategory.Shop,
            importance,
            player != null ? player : this,
            "ShopPaymentRuleDefinition",
            GameEventScope.Player,
            showInFeed: showEventsInFeed,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("paymentRuleId", Id),
            GameEventPublishing.Value("paymentRuleName", DisplayName),
            GameEventPublishing.Value("terminalKind", terminalKind),
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("shopId", shop != null ? shop.ShopId : string.Empty),
            GameEventPublishing.Value("sourceId", sourceId),
            GameEventPublishing.Value("itemSubtotal", quote != null ? quote.itemSubtotal : 0f),
            GameEventPublishing.Value("amountDue", quote != null ? quote.amountDue : 0f),
            GameEventPublishing.Value("amountToCharge", quote != null ? quote.AmountToCharge : 0f));
    }
}

public class ShopCheckoutTerminal : MonoBehaviour, IPlayerTriggerable, Interactable {
    [Header("Identity")]
    [Tooltip("Stable source id written into checkout records. Empty uses payment rule id or this GameObject name.")]
    [SerializeField] string sourceId = string.Empty;
    [Tooltip("Readable terminal name for debug/future UI. Empty uses payment rule display name or this GameObject name.")]
    [SerializeField] string displayName = string.Empty;

    [Header("Checkout")]
    [Tooltip("Shop whose active basket can be checked out from this terminal.")]
    [SerializeField] ShopCatalog shop;
    [Tooltip("Payment rule used by this terminal. Empty uses normal money checkout with no extra fee/rules.")]
    [SerializeField] ShopPaymentRuleDefinition paymentRule;
    [Tooltip("Optional explicit player. Empty uses the triggering/interacting player or PlayerController.i.")]
    [SerializeField] PlayerController playerOverride;
    [Tooltip("Action applied when this terminal is triggered.")]
    [SerializeField] ShopCheckoutTerminalAction triggerAction = ShopCheckoutTerminalAction.CheckoutBasket;
    [Tooltip("Action applied when an Interactable flow calls Interact.")]
    [SerializeField] ShopCheckoutTerminalAction interactAction = ShopCheckoutTerminalAction.CheckoutBasket;
    [Tooltip("If enabled, player trigger applies Trigger Action.")]
    [SerializeField] bool applyOnPlayerTrigger = true;
    [Tooltip("If enabled, this trigger can be called repeatedly by the player.")]
    [SerializeField] bool triggerRepeatedly = true;

    [Header("Feedback")]
    [Tooltip("If enabled, result text is shown through the existing DialogManager when available.")]
    [SerializeField] bool showDialogFeedback;
    [Tooltip("If enabled, blocked terminal actions are written to GameDebug.")]
    [SerializeField] bool logBlockedAttempts = true;
    [Tooltip("If enabled, successful terminal actions are written to GameDebug.")]
    [SerializeField] bool logSuccessfulAttempts;

    public bool TriggerRepeatedly => triggerRepeatedly;
    public string SourceId => ResolveSourceId();
    public string DisplayName => !string.IsNullOrWhiteSpace(displayName) ? displayName : paymentRule != null ? paymentRule.DisplayName : gameObject.name;
    public ShopCatalog Shop => shop;
    public ShopPaymentRuleDefinition PaymentRule => paymentRule;
    public ShopCheckoutTerminalAction TriggerAction => triggerAction;
    public ShopCheckoutTerminalAction InteractAction => interactAction;

    public void OnPlayerTriggered(PlayerController player) {
        if(!applyOnPlayerTrigger) {
            return;
        }

        ApplyAction(triggerAction, ResolvePlayer(player), out _);
    }

    public IEnumerator Interact(Transform initiator) {
        var player = ResolvePlayer(initiator != null ? initiator.GetComponent<PlayerController>() : null);
        ApplyAction(interactAction, player, out var feedback);
        if(showDialogFeedback && DialogManager.i != null && !string.IsNullOrWhiteSpace(feedback)) {
            yield return DialogManager.i.ShowDialogText(feedback);
        }
    }

    public bool TryPreviewQuote(PlayerController player, out ShopCheckoutPaymentQuote quote, out string failureMessage) {
        player = ResolvePlayer(player);
        quote = null;
        var log = player != null ? player.GetComponent<PlayerShopBasketLog>() : null;
        if(!ValidateTerminal(player, log, out failureMessage)) {
            RecordBlocked(player, failureMessage, null);
            return false;
        }

        if(paymentRule != null) {
            if(!paymentRule.TryBuildPaymentQuote(player, shop, log, ResolveSourceId(), out quote, out failureMessage)) {
                RecordBlocked(player, failureMessage, quote);
                return false;
            }
        } else {
            float subtotal = log.GetCurrentTotal(shop, out failureMessage);
            if(!string.IsNullOrWhiteSpace(failureMessage)) {
                RecordBlocked(player, failureMessage, null);
                return false;
            }

            quote = ShopCheckoutPaymentQuote.CreateDefault(subtotal, ResolveSourceId());
        }

        failureMessage = null;
        return true;
    }

    public bool TryCheckout(PlayerController player, out ShopBasketCheckoutRecord record, out string failureMessage) {
        player = ResolvePlayer(player);
        record = null;
        var log = player != null ? player.GetComponent<PlayerShopBasketLog>() : null;
        if(!ValidateTerminal(player, log, out failureMessage)) {
            RecordBlocked(player, failureMessage, null);
            return false;
        }

        if(!log.TryCheckout(shop, ResolveSourceId(), paymentRule, out record, out failureMessage)) {
            RecordBlocked(player, failureMessage, null);
            return false;
        }

        if(logSuccessfulAttempts) {
            GameDebug.Success($"{DisplayName} checked out basket for {record.amountPaid:0}.", GameDebugCategory.Shop, this, "ShopCheckoutTerminal");
        }

        return true;
    }

    bool ApplyAction(ShopCheckoutTerminalAction action, PlayerController player, out string feedback) {
        feedback = null;
        switch(action) {
            case ShopCheckoutTerminalAction.PreviewQuote:
                if(TryPreviewQuote(player, out var quote, out feedback)) {
                    feedback = quote != null ? quote.BuildSummary() : "Checkout quote ready.";
                    return true;
                }
                return false;
            case ShopCheckoutTerminalAction.ClearBasket:
                var log = player != null ? player.GetComponent<PlayerShopBasketLog>() : null;
                if(log != null && log.ClearBasket(ResolveSourceId())) {
                    feedback = "Basket cleared.";
                    return true;
                }
                feedback = "No basket was cleared.";
                RecordBlocked(player, feedback, null);
                return false;
            default:
                if(TryCheckout(player, out var record, out feedback)) {
                    feedback = record != null ? $"Checkout completed for {record.amountPaid:0}." : "Checkout completed.";
                    return true;
                }
                return false;
        }
    }

    bool ValidateTerminal(PlayerController player, PlayerShopBasketLog log, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required for checkout terminals.";
            return false;
        }

        if(shop == null) {
            failureMessage = "Checkout terminal has no shop assigned.";
            return false;
        }

        if(log == null) {
            failureMessage = "Player has no shop basket log.";
            return false;
        }

        failureMessage = null;
        return true;
    }

    void RecordBlocked(PlayerController player, string failureMessage, ShopCheckoutPaymentQuote quote) {
        paymentRule?.PublishBlocked(quote, player, shop, ResolveSourceId(), failureMessage);
        if(logBlockedAttempts && !string.IsNullOrWhiteSpace(failureMessage)) {
            GameDebug.Warning(failureMessage, GameDebugCategory.Shop, player != null ? player : this, "ShopCheckoutTerminal");
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

        if(paymentRule != null) {
            return $"checkout:{paymentRule.Id}";
        }

        return gameObject.name;
    }
}

[Serializable]
public class ShopCheckoutPaymentQuote {
    [Tooltip("Payment rule id that produced this quote.")]
    public string paymentRuleId;
    [Tooltip("Payment rule display name that produced this quote.")]
    public string paymentRuleName;
    [Tooltip("Terminal kind that produced this quote.")]
    public ShopCheckoutTerminalKind terminalKind;
    [Tooltip("Payment mode used by this quote.")]
    public ShopPaymentMode paymentMode;
    [Tooltip("Basket item subtotal before terminal fees or discounts.")]
    public float itemSubtotal;
    [Tooltip("Flat plus percentage checkout fee.")]
    public float serviceFee;
    [Tooltip("Discount/waiver applied by the payment rule.")]
    public float discountTotal;
    [Tooltip("Total amount due after fees and discounts.")]
    public float amountDue;
    [Tooltip("Source id that requested the quote.")]
    public string sourceId;
    [Tooltip("Short readable quote message.")]
    public string message;

    public float AmountToCharge => paymentMode == ShopPaymentMode.Money ? Mathf.Max(0f, amountDue) : 0f;
    public bool ChargesWallet => paymentMode == ShopPaymentMode.Money;

    public static ShopCheckoutPaymentQuote CreateDefault(float itemSubtotal, string sourceId) {
        float total = Mathf.Max(0f, Mathf.Ceil(itemSubtotal));
        return new ShopCheckoutPaymentQuote {
            paymentRuleId = string.Empty,
            paymentRuleName = "Default Checkout",
            terminalKind = ShopCheckoutTerminalKind.Cashier,
            paymentMode = ShopPaymentMode.Money,
            itemSubtotal = total,
            serviceFee = 0f,
            discountTotal = 0f,
            amountDue = total,
            sourceId = sourceId,
            message = $"Default checkout: {total:0} due."
        };
    }

    public bool MatchesSubtotal(float subtotal) {
        return Mathf.Abs(Mathf.Max(0f, itemSubtotal) - Mathf.Max(0f, subtotal)) < 0.01f;
    }

    public string BuildSummary() {
        return $"{paymentRuleName}: subtotal {itemSubtotal:0}, fee {serviceFee:0}, discount {discountTotal:0}, due {amountDue:0}.";
    }
}

[Serializable]
public class ShopCheckoutCartResult {
    [Tooltip("Basket item subtotal before terminal fees or discounts.")]
    public float itemSubtotal;
    [Tooltip("Flat plus percentage checkout fee.")]
    public float serviceFee;
    [Tooltip("Discount/waiver applied by the payment rule.")]
    public float discountTotal;
    [Tooltip("Amount charged to Wallet. Free/manual approval can be 0.")]
    public float amountPaid;
    [Tooltip("Total amount due after fees and discounts.")]
    public float amountDue;
    [Tooltip("Payment rule id used by this checkout.")]
    public string paymentRuleId;
    [Tooltip("Payment rule name used by this checkout.")]
    public string paymentRuleName;
    [Tooltip("Terminal kind used by this checkout.")]
    public ShopCheckoutTerminalKind terminalKind;
    [Tooltip("Payment mode used by this checkout.")]
    public ShopPaymentMode paymentMode;
    [Tooltip("Short readable payment message.")]
    public string paymentMessage;
    [Tooltip("Current checkout line snapshots.")]
    public List<ShopBasketLineState> lines = new List<ShopBasketLineState>();
}
