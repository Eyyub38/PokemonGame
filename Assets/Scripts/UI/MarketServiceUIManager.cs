using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum MarketServiceUIActionResultKind {
    None,
    Refreshed,
    BasketUpdated,
    CheckoutPreviewed,
    CheckedOut,
    RefundPreviewed,
    Refunded,
    DeliveryPreviewed,
    DeliveryOrdered,
    DeliveryClaimed,
    DeliveryCancelled,
    LoyaltyJoined,
    ServicePackageUsed,
    AppointmentPreviewed,
    AppointmentBooked,
    AppointmentCompleted,
    AppointmentCancelled,
    Blocked
}

public class MarketServiceUIManager : MonoBehaviour {
    [Header("Player")]
    [Tooltip("Player whose shop/service state is shown. Empty uses PlayerController.i or the first PlayerController in the scene.")]
    [SerializeField] PlayerController playerOverride;
    [Tooltip("If enabled, missing player log components are created when actions need them.")]
    [SerializeField] bool createMissingLogsForActions = true;

    [Header("Shop Sources")]
    [Tooltip("Shelf source used for visible offer rows and add-to-basket actions.")]
    [SerializeField] ShopShelfSource shelfSource;
    [Tooltip("Optional basket source for begin/add preset/checkout/clear actions configured on that source.")]
    [SerializeField] ShopBasketSource basketSource;
    [Tooltip("Checkout terminal used for preview and checkout actions.")]
    [SerializeField] ShopCheckoutTerminal checkoutTerminal;
    [Tooltip("Refund source used for refund preview and refund actions.")]
    [SerializeField] ShopRefundSource refundSource;
    [Tooltip("Delivery source used for delivery quote/order/claim/cancel actions.")]
    [SerializeField] ShopDeliverySource deliverySource;

    [Header("Service Sources")]
    [Tooltip("Loyalty program source shown by this UI manager.")]
    [SerializeField] LoyaltyProgramSource loyaltySource;
    [Tooltip("Service package source shown by this UI manager.")]
    [SerializeField] ServicePackageSource servicePackageSource;
    [Tooltip("Service appointment source shown by this UI manager.")]
    [SerializeField] ServiceAppointmentSource appointmentSource;

    [Header("Snapshot")]
    [Tooltip("If enabled, Refresh is called when this component starts.")]
    [SerializeField] bool refreshOnStart = true;
    [Tooltip("If enabled, Refresh is called after every successful or blocked action.")]
    [SerializeField] bool refreshAfterActions = true;
    [Tooltip("If enabled, checkout/delivery/refund/appointment preview actions refresh the stored preview rows.")]
    [SerializeField] bool storeLastPreviewRows = true;
    [Tooltip("Maximum history rows copied into the UI snapshot. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxHistoryRows = 8;
    [Tooltip("Default bundle count used by AddSelectedOffer when UI does not provide a value.")]
    [Min(1)]
    [SerializeField] int defaultBundles = 1;

    [Header("Debug")]
    [Tooltip("If enabled, successful UI backend actions are written to GameDebug.")]
    [SerializeField] bool logSuccessfulActions;
    [Tooltip("If enabled, blocked UI backend actions are written to GameDebug.")]
    [SerializeField] bool logBlockedActions = true;

    MarketServiceUIScreenSnapshot currentSnapshot = new MarketServiceUIScreenSnapshot();
    MarketServiceUIActionResult lastResult = new MarketServiceUIActionResult();
    ShopCheckoutPaymentQuote lastCheckoutQuote;
    ShopRefundQuote lastRefundQuote;
    ShopDeliveryQuote lastDeliveryQuote;
    ServiceAppointmentQuote lastAppointmentQuote;

    public MarketServiceUIScreenSnapshot CurrentSnapshot => currentSnapshot;
    public MarketServiceUIActionResult LastResult => lastResult;
    public ShopShelfSource ShelfSource => shelfSource;
    public ShopBasketSource BasketSource => basketSource;
    public ShopCheckoutTerminal CheckoutTerminal => checkoutTerminal;
    public ShopRefundSource RefundSource => refundSource;
    public ShopDeliverySource DeliverySource => deliverySource;
    public LoyaltyProgramSource LoyaltySource => loyaltySource;
    public ServicePackageSource ServicePackageSource => servicePackageSource;
    public ServiceAppointmentSource AppointmentSource => appointmentSource;
    public int DefaultBundles => Mathf.Max(1, defaultBundles);
    public event Action<MarketServiceUIScreenSnapshot> OnSnapshotChanged;
    public event Action<MarketServiceUIActionResult> OnActionResult;

    void Start() {
        if(refreshOnStart) {
            Refresh();
        }
    }

    [ContextMenu("Refresh UI Snapshot")]
    public MarketServiceUIScreenSnapshot RefreshFromContextMenu() {
        return Refresh();
    }

    public MarketServiceUIScreenSnapshot Refresh() {
        var player = ResolvePlayer();
        var shop = ResolvePrimaryShop();
        var basketLog = player != null ? player.GetComponent<PlayerShopBasketLog>() : null;

        currentSnapshot = new MarketServiceUIScreenSnapshot {
            hasPlayer = player != null,
            playerName = player != null ? player.name : string.Empty,
            walletMoney = Wallet.i != null ? Wallet.i.Money : 0f,
            day = TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1,
            hour = TimeSystem.i != null ? Mathf.Clamp(TimeSystem.i.Hour, 0, 23) : 0,
            absoluteHour = GetCurrentAbsoluteHour(),
            hasShop = shop != null,
            shopId = shop != null ? shop.ShopId : string.Empty,
            shopName = shop != null && shop.Catalog != null ? shop.Catalog.DisplayName : shop != null ? shop.name : string.Empty,
            shelfName = shelfSource != null ? shelfSource.DisplayName : string.Empty,
            checkoutTerminalName = checkoutTerminal != null ? checkoutTerminal.DisplayName : string.Empty,
            refundSourceName = refundSource != null ? refundSource.DisplayName : string.Empty,
            deliverySourceName = deliverySource != null ? deliverySource.DisplayName : string.Empty,
            loyaltySourceName = loyaltySource != null ? loyaltySource.DisplayName : string.Empty,
            servicePackageSourceName = servicePackageSource != null ? servicePackageSource.DisplayName : string.Empty,
            appointmentSourceName = appointmentSource != null ? appointmentSource.DisplayName : string.Empty,
            offers = BuildOfferRows(player, basketLog),
            basket = BuildBasketSnapshot(shop, basketLog),
            checkoutHistory = BuildCheckoutRows(basketLog),
            pendingDeliveries = BuildDeliveryRows(player),
            loyaltyPrograms = BuildLoyaltyRows(player),
            servicePackages = BuildServicePackageRows(player),
            appointmentRecords = BuildAppointmentRows(player),
            checkoutQuote = storeLastPreviewRows ? MarketServiceUICheckoutQuoteRow.FromQuote(lastCheckoutQuote) : null,
            refundQuote = storeLastPreviewRows ? MarketServiceUIRefundQuoteRow.FromQuote(lastRefundQuote) : null,
            deliveryQuote = storeLastPreviewRows ? MarketServiceUIDeliveryQuoteRow.FromQuote(lastDeliveryQuote) : null,
            appointmentQuote = storeLastPreviewRows ? MarketServiceUIAppointmentQuoteRow.FromQuote(lastAppointmentQuote) : null,
            lastResult = lastResult
        };

        OnSnapshotChanged?.Invoke(currentSnapshot);
        return currentSnapshot;
    }

    public bool TryBeginBasket(out string feedback) {
        var player = ResolvePlayer();
        if(player == null) {
            return Block(MarketServiceUIActionResultKind.Blocked, "A player is required to begin a basket.", out feedback);
        }

        if(shelfSource != null) {
            if(shelfSource.TryBeginBasket(player, out feedback)) {
                return Succeed(MarketServiceUIActionResultKind.BasketUpdated, "Basket started.", out feedback);
            }

            return Block(MarketServiceUIActionResultKind.Blocked, feedback, out feedback);
        }

        if(basketSource != null) {
            if(basketSource.TryApply(player, out feedback)) {
                return Succeed(MarketServiceUIActionResultKind.BasketUpdated, "Basket source action applied.", out feedback);
            }

            return Block(MarketServiceUIActionResultKind.Blocked, feedback, out feedback);
        }

        var shop = ResolvePrimaryShop();
        if(shop == null) {
            return Block(MarketServiceUIActionResultKind.Blocked, "No shop source is assigned.", out feedback);
        }

        var log = GetBasketLog(player, createMissingLogsForActions);
        if(log != null && log.BeginBasket(shop, clearExisting: false, sourceId: "ui:market-service")) {
            return Succeed(MarketServiceUIActionResultKind.BasketUpdated, "Basket started.", out feedback);
        }

        return Block(MarketServiceUIActionResultKind.Blocked, "Could not begin basket.", out feedback);
    }

    public bool TryAddSelectedOffer(string offerId, int bundles, out string feedback) {
        bundles = Mathf.Max(1, bundles <= 0 ? DefaultBundles : bundles);
        var player = ResolvePlayer();
        if(player == null) {
            return Block(MarketServiceUIActionResultKind.Blocked, "A player is required to add basket offers.", out feedback);
        }

        if(shelfSource != null) {
            if(shelfSource.TryAddOffer(player, offerId, bundles, out feedback)) {
                return Succeed(MarketServiceUIActionResultKind.BasketUpdated, "Offer added to basket.", out feedback);
            }

            return Block(MarketServiceUIActionResultKind.Blocked, feedback, out feedback);
        }

        var shop = ResolvePrimaryShop();
        var entry = shop != null ? shop.FindOffer(offerId) : null;
        var log = GetBasketLog(player, createMissingLogsForActions);
        feedback = null;
        if(shop != null && entry != null && log != null && log.AddOffer(shop, entry, bundles, "ui:market-service", out feedback)) {
            return Succeed(MarketServiceUIActionResultKind.BasketUpdated, "Offer added to basket.", out feedback);
        }

        return Block(MarketServiceUIActionResultKind.Blocked, string.IsNullOrWhiteSpace(feedback) ? $"Offer '{offerId}' could not be added." : feedback, out feedback);
    }

    public bool TrySetBasketOfferBundles(string offerId, int bundles, out string feedback) {
        var player = ResolvePlayer();
        var log = player != null ? player.GetComponent<PlayerShopBasketLog>() : null;
        var shop = ResolvePrimaryShop();
        feedback = null;
        if(log != null && log.SetOfferBundles(shop, offerId, bundles, out feedback)) {
            return Succeed(MarketServiceUIActionResultKind.BasketUpdated, "Basket line updated.", out feedback);
        }

        return Block(MarketServiceUIActionResultKind.Blocked, string.IsNullOrWhiteSpace(feedback) ? "Basket line could not be updated." : feedback, out feedback);
    }

    public bool TryRemoveBasketOffer(string offerId, int bundles, out string feedback) {
        var player = ResolvePlayer();
        var log = player != null ? player.GetComponent<PlayerShopBasketLog>() : null;
        if(log != null && log.RemoveOffer(offerId, Mathf.Max(1, bundles <= 0 ? 1 : bundles))) {
            return Succeed(MarketServiceUIActionResultKind.BasketUpdated, "Basket line removed.", out feedback);
        }

        return Block(MarketServiceUIActionResultKind.Blocked, "Basket line could not be removed.", out feedback);
    }

    public bool TryClearBasket(out string feedback) {
        var player = ResolvePlayer();
        var log = player != null ? player.GetComponent<PlayerShopBasketLog>() : null;
        if(log != null && log.ClearBasket("ui:market-service")) {
            ClearPreviewRows();
            return Succeed(MarketServiceUIActionResultKind.BasketUpdated, "Basket cleared.", out feedback);
        }

        return Block(MarketServiceUIActionResultKind.Blocked, "No basket was cleared.", out feedback);
    }

    public bool TryPreviewCheckout(out ShopCheckoutPaymentQuote quote, out string feedback) {
        quote = null;
        if(checkoutTerminal == null) {
            return Block(MarketServiceUIActionResultKind.Blocked, "No checkout terminal is assigned.", out feedback);
        }

        if(checkoutTerminal.TryPreviewQuote(ResolvePlayer(), out quote, out feedback)) {
            lastCheckoutQuote = quote;
            return Succeed(MarketServiceUIActionResultKind.CheckoutPreviewed, quote != null ? quote.BuildSummary() : "Checkout quote ready.", out feedback);
        }

        return Block(MarketServiceUIActionResultKind.Blocked, feedback, out feedback);
    }

    public bool TryCheckout(out ShopBasketCheckoutRecord record, out string feedback) {
        record = null;
        if(checkoutTerminal == null) {
            return Block(MarketServiceUIActionResultKind.Blocked, "No checkout terminal is assigned.", out feedback);
        }

        if(checkoutTerminal.TryCheckout(ResolvePlayer(), out record, out feedback)) {
            lastCheckoutQuote = null;
            return Succeed(MarketServiceUIActionResultKind.CheckedOut, record != null ? $"Checkout completed for {record.amountPaid:0}." : "Checkout completed.", out feedback);
        }

        return Block(MarketServiceUIActionResultKind.Blocked, feedback, out feedback);
    }

    public bool TryPreviewRefund(out ShopRefundQuote quote, out string feedback) {
        quote = null;
        if(refundSource == null) {
            return Block(MarketServiceUIActionResultKind.Blocked, "No refund source is assigned.", out feedback);
        }

        if(refundSource.TryPreviewRefund(ResolvePlayer(), out quote, out feedback)) {
            lastRefundQuote = quote;
            return Succeed(MarketServiceUIActionResultKind.RefundPreviewed, quote != null ? quote.BuildSummary() : "Refund quote ready.", out feedback);
        }

        return Block(MarketServiceUIActionResultKind.Blocked, feedback, out feedback);
    }

    public bool TryRefund(out ShopRefundRecord record, out string feedback) {
        record = null;
        if(refundSource == null) {
            return Block(MarketServiceUIActionResultKind.Blocked, "No refund source is assigned.", out feedback);
        }

        if(refundSource.TryRefund(ResolvePlayer(), out record, out feedback)) {
            lastRefundQuote = null;
            return Succeed(MarketServiceUIActionResultKind.Refunded, record != null ? $"Refund completed for {record.amountRefunded:0}." : "Refund completed.", out feedback);
        }

        return Block(MarketServiceUIActionResultKind.Blocked, feedback, out feedback);
    }

    public bool TryPreviewDelivery(out ShopDeliveryQuote quote, out string feedback) {
        quote = null;
        if(deliverySource == null) {
            return Block(MarketServiceUIActionResultKind.Blocked, "No delivery source is assigned.", out feedback);
        }

        if(deliverySource.TryPreviewOrder(ResolvePlayer(), out quote, out feedback)) {
            lastDeliveryQuote = quote;
            return Succeed(MarketServiceUIActionResultKind.DeliveryPreviewed, quote != null ? quote.BuildSummary() : "Delivery quote ready.", out feedback);
        }

        return Block(MarketServiceUIActionResultKind.Blocked, feedback, out feedback);
    }

    public bool TryPlaceDeliveryOrder(out ShopDeliveryOrderRecord record, out string feedback) {
        record = null;
        if(deliverySource == null) {
            return Block(MarketServiceUIActionResultKind.Blocked, "No delivery source is assigned.", out feedback);
        }

        if(deliverySource.TryPlaceOrder(ResolvePlayer(), out record, out feedback)) {
            lastDeliveryQuote = null;
            return Succeed(MarketServiceUIActionResultKind.DeliveryOrdered, record != null ? $"Delivery order placed. Due in {record.deliveryHours} hour(s)." : "Delivery order placed.", out feedback);
        }

        return Block(MarketServiceUIActionResultKind.Blocked, feedback, out feedback);
    }

    public bool TryClaimDueDeliveries(out List<ShopDeliveryOrderRecord> delivered, out string feedback) {
        delivered = new List<ShopDeliveryOrderRecord>();
        if(deliverySource == null) {
            return Block(MarketServiceUIActionResultKind.Blocked, "No delivery source is assigned.", out feedback);
        }

        if(deliverySource.TryClaimDueDeliveries(ResolvePlayer(), out delivered, out feedback)) {
            return Succeed(MarketServiceUIActionResultKind.DeliveryClaimed, $"Claimed {delivered.Count} delivery order(s).", out feedback);
        }

        return Block(MarketServiceUIActionResultKind.Blocked, feedback, out feedback);
    }

    public bool TryCancelLatestDelivery(out ShopDeliveryOrderRecord cancelled, out string feedback) {
        cancelled = null;
        if(deliverySource == null) {
            return Block(MarketServiceUIActionResultKind.Blocked, "No delivery source is assigned.", out feedback);
        }

        if(deliverySource.TryCancelLatestPending(ResolvePlayer(), out cancelled, out feedback)) {
            return Succeed(MarketServiceUIActionResultKind.DeliveryCancelled, cancelled != null ? $"Delivery cancelled and refunded {cancelled.refundAmount:0}." : "Delivery cancelled.", out feedback);
        }

        return Block(MarketServiceUIActionResultKind.Blocked, feedback, out feedback);
    }

    public bool TryJoinLoyaltyProgram(string programId, out PlayerLoyaltyRecord record, out string feedback) {
        record = null;
        if(loyaltySource == null) {
            return Block(MarketServiceUIActionResultKind.Blocked, "No loyalty source is assigned.", out feedback);
        }

        var program = loyaltySource.Programs?.FirstOrDefault(candidate => candidate != null && candidate.Id == programId);
        if(loyaltySource.TryJoin(ResolvePlayer(), program, out record, out feedback)) {
            return Succeed(MarketServiceUIActionResultKind.LoyaltyJoined, record != null ? $"{record.programName} joined." : "Loyalty program joined.", out feedback);
        }

        return Block(MarketServiceUIActionResultKind.Blocked, feedback, out feedback);
    }

    public bool TryUseServicePackage(string packageId, out ServicePackageUseResult result, out string feedback) {
        result = null;
        if(servicePackageSource == null) {
            return Block(MarketServiceUIActionResultKind.Blocked, "No service package source is assigned.", out feedback);
        }

        var package = servicePackageSource.Packages?.FirstOrDefault(candidate => candidate != null && candidate.Id == packageId);
        if(servicePackageSource.TryUse(ResolvePlayer(), package, out result, out feedback)) {
            return Succeed(MarketServiceUIActionResultKind.ServicePackageUsed, result != null ? $"{result.packageName} completed." : "Service package completed.", out feedback);
        }

        return Block(MarketServiceUIActionResultKind.Blocked, feedback, out feedback);
    }

    public bool TryPreviewAppointment(out ServiceAppointmentQuote quote, out string feedback) {
        quote = null;
        if(appointmentSource == null) {
            return Block(MarketServiceUIActionResultKind.Blocked, "No appointment source is assigned.", out feedback);
        }

        if(appointmentSource.TryPreview(ResolvePlayer(), out quote, out feedback)) {
            lastAppointmentQuote = quote;
            return Succeed(MarketServiceUIActionResultKind.AppointmentPreviewed, quote != null ? quote.BuildSummary() : "Appointment quote ready.", out feedback);
        }

        return Block(MarketServiceUIActionResultKind.Blocked, feedback, out feedback);
    }

    public bool TryBookAppointment(out ServiceAppointmentRecord record, out string feedback) {
        record = null;
        if(appointmentSource == null) {
            return Block(MarketServiceUIActionResultKind.Blocked, "No appointment source is assigned.", out feedback);
        }

        if(appointmentSource.TryBook(ResolvePlayer(), out record, out feedback)) {
            lastAppointmentQuote = null;
            return Succeed(MarketServiceUIActionResultKind.AppointmentBooked, record != null ? $"{record.appointmentName} booked for day {record.scheduledDay}." : "Appointment booked.", out feedback);
        }

        return Block(MarketServiceUIActionResultKind.Blocked, feedback, out feedback);
    }

    public bool TryCompleteDueAppointments(out List<ServiceAppointmentRecord> completed, out string feedback) {
        completed = new List<ServiceAppointmentRecord>();
        if(appointmentSource == null) {
            return Block(MarketServiceUIActionResultKind.Blocked, "No appointment source is assigned.", out feedback);
        }

        if(appointmentSource.TryCompleteDue(ResolvePlayer(), out completed, out feedback)) {
            return Succeed(MarketServiceUIActionResultKind.AppointmentCompleted, $"Completed {completed.Count} appointment(s).", out feedback);
        }

        return Block(MarketServiceUIActionResultKind.Blocked, feedback, out feedback);
    }

    public bool TryCancelLatestAppointment(out ServiceAppointmentRecord cancelled, out string feedback) {
        cancelled = null;
        if(appointmentSource == null) {
            return Block(MarketServiceUIActionResultKind.Blocked, "No appointment source is assigned.", out feedback);
        }

        if(appointmentSource.TryCancelLatestPending(ResolvePlayer(), out cancelled, out feedback)) {
            return Succeed(MarketServiceUIActionResultKind.AppointmentCancelled, cancelled != null ? $"{cancelled.appointmentName} cancelled." : "Appointment cancelled.", out feedback);
        }

        return Block(MarketServiceUIActionResultKind.Blocked, feedback, out feedback);
    }

    List<MarketServiceUIOfferRow> BuildOfferRows(PlayerController player, PlayerShopBasketLog basketLog) {
        if(shelfSource == null) {
            return new List<MarketServiceUIOfferRow>();
        }

        var shop = shelfSource.Shop;
        var ledger = player != null ? player.GetComponent<PlayerShopLedger>() : null;
        return shelfSource.GetVisibleOffers(player)
            .Where(entry => entry != null)
            .Select(entry => MarketServiceUIOfferRow.FromEntry(shop, player, ledger, basketLog, entry))
            .ToList();
    }

    MarketServiceUIBasketSnapshot BuildBasketSnapshot(ShopCatalog shop, PlayerShopBasketLog basketLog) {
        if(basketLog == null || !basketLog.HasActiveBasket) {
            return new MarketServiceUIBasketSnapshot {
                isActive = false,
                displayText = "Basket is empty."
            };
        }

        string failure = null;
        float currentTotal = shop != null ? basketLog.GetCurrentTotal(shop, out failure) : basketLog.GetSnapshotTotal();
        var state = basketLog.ActiveBasket;
        return new MarketServiceUIBasketSnapshot {
            isActive = true,
            shopId = state.shopId,
            shopName = state.shopName,
            sourceId = state.sourceId,
            lineCount = basketLog.GetLineCount(),
            bundleCount = basketLog.GetBundleCount(),
            snapshotTotal = basketLog.GetSnapshotTotal(),
            currentTotal = currentTotal,
            canAfford = currentTotal <= 0f || Wallet.i != null && Wallet.i.HasMoney(currentTotal),
            failureMessage = failure,
            displayText = $"{state.shopName}: {basketLog.GetLineCount()} line(s), {currentTotal:0} due.",
            lines = state.lines != null
                ? state.lines.Where(line => line != null).Select(MarketServiceUIBasketLineRow.FromLine).ToList()
                : new List<MarketServiceUIBasketLineRow>()
        };
    }

    List<MarketServiceUICheckoutRecordRow> BuildCheckoutRows(PlayerShopBasketLog basketLog) {
        if(basketLog == null || basketLog.CheckoutRecords == null) {
            return new List<MarketServiceUICheckoutRecordRow>();
        }

        IEnumerable<ShopBasketCheckoutRecord> query = basketLog.CheckoutRecords.Where(record => record != null).OrderByDescending(record => record.absoluteHour);
        if(maxHistoryRows > 0) {
            query = query.Take(maxHistoryRows);
        }

        return query.Select(MarketServiceUICheckoutRecordRow.FromRecord).ToList();
    }

    List<MarketServiceUIDeliveryOrderRow> BuildDeliveryRows(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerDeliveryLog>() : null;
        if(log == null || log.Orders == null) {
            return new List<MarketServiceUIDeliveryOrderRow>();
        }

        IEnumerable<ShopDeliveryOrderRecord> query = log.Orders.Where(order => order != null && order.IsPending).OrderBy(order => order.dueAbsoluteHour);
        if(maxHistoryRows > 0) {
            query = query.Take(maxHistoryRows);
        }

        return query.Select(order => MarketServiceUIDeliveryOrderRow.FromRecord(order, GetCurrentAbsoluteHour())).ToList();
    }

    List<MarketServiceUILoyaltyProgramRow> BuildLoyaltyRows(PlayerController player) {
        if(loyaltySource == null) {
            return new List<MarketServiceUILoyaltyProgramRow>();
        }

        var log = player != null ? player.GetComponent<PlayerLoyaltyLog>() : null;
        return loyaltySource.Programs
            .Where(program => program != null)
            .Select(program => MarketServiceUILoyaltyProgramRow.FromProgram(program, log, loyaltySource.GetAvailablePrograms(player).Contains(program)))
            .ToList();
    }

    List<MarketServiceUIServicePackageRow> BuildServicePackageRows(PlayerController player) {
        if(servicePackageSource == null) {
            return new List<MarketServiceUIServicePackageRow>();
        }

        var log = player != null ? player.GetComponent<PlayerServicePackageLog>() : null;
        var catalog = servicePackageSource.ShopContext != null ? servicePackageSource.ShopContext.Catalog : null;
        return servicePackageSource.Packages
            .Where(package => package != null)
            .Select(package => MarketServiceUIServicePackageRow.FromPackage(package, player, log, servicePackageSource.SourceId, catalog))
            .ToList();
    }

    List<MarketServiceUIAppointmentRecordRow> BuildAppointmentRows(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerServiceAppointmentLog>() : null;
        if(log == null || log.Records == null) {
            return new List<MarketServiceUIAppointmentRecordRow>();
        }

        var appointmentId = appointmentSource != null && appointmentSource.Appointment != null ? appointmentSource.Appointment.Id : null;
        var sourceId = appointmentSource != null ? appointmentSource.SourceId : null;
        IEnumerable<ServiceAppointmentRecord> query = log.Records
            .Where(record => record != null)
            .Where(record => string.IsNullOrWhiteSpace(appointmentId) || record.appointmentId == appointmentId)
            .Where(record => string.IsNullOrWhiteSpace(sourceId) || record.sourceId == sourceId)
            .OrderByDescending(record => record.scheduledAbsoluteHour);
        if(maxHistoryRows > 0) {
            query = query.Take(maxHistoryRows);
        }

        return query.Select(record => MarketServiceUIAppointmentRecordRow.FromRecord(record, GetCurrentAbsoluteHour())).ToList();
    }

    PlayerController ResolvePlayer() {
        if(playerOverride != null) {
            return playerOverride;
        }

        if(PlayerController.i != null) {
            return PlayerController.i;
        }

        return FindAnyObjectByType<PlayerController>();
    }

    ShopCatalog ResolvePrimaryShop() {
        if(shelfSource != null && shelfSource.Shop != null) return shelfSource.Shop;
        if(checkoutTerminal != null && checkoutTerminal.Shop != null) return checkoutTerminal.Shop;
        if(deliverySource != null && deliverySource.Shop != null) return deliverySource.Shop;
        if(refundSource != null && refundSource.Shop != null) return refundSource.Shop;
        if(basketSource != null && basketSource.Shop != null) return basketSource.Shop;
        if(servicePackageSource != null && servicePackageSource.ShopContext != null) return servicePackageSource.ShopContext;
        if(appointmentSource != null && appointmentSource.ShopContext != null) return appointmentSource.ShopContext;
        return null;
    }

    PlayerShopBasketLog GetBasketLog(PlayerController player, bool createIfMissing) {
        if(player == null) {
            return null;
        }

        var log = player.GetComponent<PlayerShopBasketLog>();
        return log != null || !createIfMissing ? log : player.gameObject.AddComponent<PlayerShopBasketLog>();
    }

    bool Succeed(MarketServiceUIActionResultKind kind, string message, out string feedback) {
        feedback = message;
        RecordResult(kind, true, message);
        return true;
    }

    bool Block(MarketServiceUIActionResultKind kind, string message, out string feedback) {
        feedback = string.IsNullOrWhiteSpace(message) ? "Action was blocked." : message;
        RecordResult(kind, false, feedback);
        return false;
    }

    void RecordResult(MarketServiceUIActionResultKind kind, bool success, string message) {
        lastResult = new MarketServiceUIActionResult {
            kind = success ? kind : MarketServiceUIActionResultKind.Blocked,
            success = success,
            message = message ?? string.Empty,
            day = TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1,
            hour = TimeSystem.i != null ? Mathf.Clamp(TimeSystem.i.Hour, 0, 23) : 0,
            absoluteHour = GetCurrentAbsoluteHour()
        };

        if(success && logSuccessfulActions) {
            GameDebug.Success(lastResult.message, GameDebugCategory.UI, this, "MarketServiceUIManager");
        } else if(!success && logBlockedActions) {
            GameDebug.Warning(lastResult.message, GameDebugCategory.UI, this, "MarketServiceUIManager");
        }

        OnActionResult?.Invoke(lastResult);
        if(refreshAfterActions) {
            Refresh();
        }
    }

    void ClearPreviewRows() {
        lastCheckoutQuote = null;
        lastRefundQuote = null;
        lastDeliveryQuote = null;
        lastAppointmentQuote = null;
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }
}

[Serializable]
public class MarketServiceUIScreenSnapshot {
    [Tooltip("If enabled, a player was resolved for this snapshot.")]
    public bool hasPlayer;
    [Tooltip("Resolved player name.")]
    public string playerName;
    [Tooltip("Current Wallet money when the snapshot was built.")]
    public float walletMoney;
    [Tooltip("Current in-game day.")]
    public int day;
    [Tooltip("Current in-game hour.")]
    public int hour;
    [Tooltip("Current absolute in-game hour.")]
    public int absoluteHour;
    [Tooltip("If enabled, a primary shop was resolved for this snapshot.")]
    public bool hasShop;
    [Tooltip("Primary shop id used by this snapshot.")]
    public string shopId;
    [Tooltip("Primary shop display name used by this snapshot.")]
    public string shopName;
    [Tooltip("Shelf source display name.")]
    public string shelfName;
    [Tooltip("Checkout terminal display name.")]
    public string checkoutTerminalName;
    [Tooltip("Refund source display name.")]
    public string refundSourceName;
    [Tooltip("Delivery source display name.")]
    public string deliverySourceName;
    [Tooltip("Loyalty source display name.")]
    public string loyaltySourceName;
    [Tooltip("Service package source display name.")]
    public string servicePackageSourceName;
    [Tooltip("Appointment source display name.")]
    public string appointmentSourceName;
    [Tooltip("Visible shelf/shop offer rows.")]
    public List<MarketServiceUIOfferRow> offers = new List<MarketServiceUIOfferRow>();
    [Tooltip("Current basket state.")]
    public MarketServiceUIBasketSnapshot basket;
    [Tooltip("Recent checkout records.")]
    public List<MarketServiceUICheckoutRecordRow> checkoutHistory = new List<MarketServiceUICheckoutRecordRow>();
    [Tooltip("Pending delivery order rows.")]
    public List<MarketServiceUIDeliveryOrderRow> pendingDeliveries = new List<MarketServiceUIDeliveryOrderRow>();
    [Tooltip("Loyalty programs shown by this manager.")]
    public List<MarketServiceUILoyaltyProgramRow> loyaltyPrograms = new List<MarketServiceUILoyaltyProgramRow>();
    [Tooltip("Service packages shown by this manager.")]
    public List<MarketServiceUIServicePackageRow> servicePackages = new List<MarketServiceUIServicePackageRow>();
    [Tooltip("Appointment records shown by this manager.")]
    public List<MarketServiceUIAppointmentRecordRow> appointmentRecords = new List<MarketServiceUIAppointmentRecordRow>();
    [Tooltip("Most recent checkout preview row, if one was requested.")]
    public MarketServiceUICheckoutQuoteRow checkoutQuote;
    [Tooltip("Most recent refund preview row, if one was requested.")]
    public MarketServiceUIRefundQuoteRow refundQuote;
    [Tooltip("Most recent delivery preview row, if one was requested.")]
    public MarketServiceUIDeliveryQuoteRow deliveryQuote;
    [Tooltip("Most recent appointment preview row, if one was requested.")]
    public MarketServiceUIAppointmentQuoteRow appointmentQuote;
    [Tooltip("Most recent UI backend action result.")]
    public MarketServiceUIActionResult lastResult;
}

[Serializable]
public class MarketServiceUIActionResult {
    [Tooltip("Kind of UI backend action that produced this result.")]
    public MarketServiceUIActionResultKind kind;
    [Tooltip("If enabled, the action succeeded.")]
    public bool success;
    [Tooltip("Readable result, failure or feedback text.")]
    public string message;
    [Tooltip("In-game day when the result was produced.")]
    public int day;
    [Tooltip("In-game hour when the result was produced.")]
    public int hour;
    [Tooltip("Absolute in-game hour when the result was produced.")]
    public int absoluteHour;
}

[Serializable]
public class MarketServiceUIOfferRow {
    [Tooltip("Offer id used by add-to-basket actions.")]
    public string offerId;
    [Tooltip("Readable offer name.")]
    public string displayName;
    [Tooltip("Inventory item id/name granted by this offer.")]
    public string itemId;
    [Tooltip("Inventory item count granted per purchase bundle.")]
    public int itemsPerBundle;
    [Tooltip("Price for one purchase bundle after current modifiers.")]
    public float unitPrice;
    [Tooltip("Remaining stock. -1 means unlimited.")]
    public int remainingStock;
    [Tooltip("If enabled, this offer is currently addable by the resolved player.")]
    public bool canAdd;
    [Tooltip("If Can Add is false, this explains why when available.")]
    public string unavailableReason;
    [Tooltip("Bundles of this offer currently in the active basket.")]
    public int bundlesInBasket;
    [Tooltip("Short text useful for placeholder UI rows.")]
    public string displayText;

    public static MarketServiceUIOfferRow FromEntry(ShopCatalog shop, PlayerController player, PlayerShopLedger ledger, PlayerShopBasketLog basketLog, ShopCatalogEntry entry) {
        string failure = null;
        bool canAdd = shop != null && shop.CanBuy(player, entry, 1, out failure);
        int remainingStock = entry != null ? entry.GetRemainingStock(shop != null ? shop.ShopId : null, ledger) : 0;
        float unitPrice = shop != null ? shop.GetBuyPrice(player, entry, 1) : 0f;
        int inBasket = basketLog != null ? basketLog.GetBundleCount(entry != null ? entry.OfferId : null) : 0;
        return new MarketServiceUIOfferRow {
            offerId = entry != null ? entry.OfferId : string.Empty,
            displayName = entry != null ? entry.DisplayName : string.Empty,
            itemId = entry != null && entry.GetItem() != null ? entry.GetItem().name : string.Empty,
            itemsPerBundle = entry != null ? entry.GetBundleCount() : 0,
            unitPrice = unitPrice,
            remainingStock = remainingStock,
            canAdd = canAdd,
            unavailableReason = failure,
            bundlesInBasket = inBasket,
            displayText = $"{(entry != null ? entry.DisplayName : "Offer")} - {unitPrice:0}"
        };
    }
}

[Serializable]
public class MarketServiceUIBasketSnapshot {
    [Tooltip("If enabled, the player currently has an active basket.")]
    public bool isActive;
    [Tooltip("Shop id this basket belongs to.")]
    public string shopId;
    [Tooltip("Shop display name this basket belongs to.")]
    public string shopName;
    [Tooltip("Source id that started or last updated the basket.")]
    public string sourceId;
    [Tooltip("Number of lines in the basket.")]
    public int lineCount;
    [Tooltip("Total purchase bundles in the basket.")]
    public int bundleCount;
    [Tooltip("Stored basket total from line snapshots.")]
    public float snapshotTotal;
    [Tooltip("Fresh total using the current shop prices when possible.")]
    public float currentTotal;
    [Tooltip("If enabled, the current Wallet can afford Current Total.")]
    public bool canAfford;
    [Tooltip("Failure produced while refreshing current basket total, if any.")]
    public string failureMessage;
    [Tooltip("Short text useful for placeholder UI.")]
    public string displayText;
    [Tooltip("Basket line rows.")]
    public List<MarketServiceUIBasketLineRow> lines = new List<MarketServiceUIBasketLineRow>();
}

[Serializable]
public class MarketServiceUIBasketLineRow {
    [Tooltip("Offer id selected from the shop catalog.")]
    public string offerId;
    [Tooltip("Inventory item id/name copied from the offer.")]
    public string itemId;
    [Tooltip("Readable item/offer name.")]
    public string itemName;
    [Tooltip("Number of purchase bundles.")]
    public int bundles;
    [Tooltip("Inventory items per bundle.")]
    public int itemsPerBundle;
    [Tooltip("Price for one bundle.")]
    public float unitBundlePrice;
    [Tooltip("Line total price.")]
    public float lineTotal;
    [Tooltip("Short text useful for placeholder UI.")]
    public string displayText;

    public static MarketServiceUIBasketLineRow FromLine(ShopBasketLineState line) {
        return new MarketServiceUIBasketLineRow {
            offerId = line.offerId,
            itemId = line.itemId,
            itemName = line.itemName,
            bundles = Mathf.Max(0, line.bundles),
            itemsPerBundle = Mathf.Max(1, line.itemsPerBundle),
            unitBundlePrice = line.unitBundlePrice,
            lineTotal = line.lineTotal,
            displayText = $"{line.itemName} x{Mathf.Max(1, line.bundles)} - {line.lineTotal:0}"
        };
    }
}

[Serializable]
public class MarketServiceUICheckoutQuoteRow {
    [Tooltip("Payment rule id used by this quote.")]
    public string paymentRuleId;
    [Tooltip("Payment rule display name used by this quote.")]
    public string paymentRuleName;
    [Tooltip("Payment mode used by this quote.")]
    public string paymentMode;
    [Tooltip("Basket subtotal before terminal fees or discounts.")]
    public float itemSubtotal;
    [Tooltip("Checkout service fee.")]
    public float serviceFee;
    [Tooltip("Discount applied by the payment rule.")]
    public float discountTotal;
    [Tooltip("Final amount due.")]
    public float amountDue;
    [Tooltip("Amount that will be charged to Wallet.")]
    public float amountToCharge;
    [Tooltip("Readable quote message.")]
    public string message;

    public static MarketServiceUICheckoutQuoteRow FromQuote(ShopCheckoutPaymentQuote quote) {
        if(quote == null) {
            return null;
        }

        return new MarketServiceUICheckoutQuoteRow {
            paymentRuleId = quote.paymentRuleId,
            paymentRuleName = quote.paymentRuleName,
            paymentMode = quote.paymentMode.ToString(),
            itemSubtotal = quote.itemSubtotal,
            serviceFee = quote.serviceFee,
            discountTotal = quote.discountTotal,
            amountDue = quote.amountDue,
            amountToCharge = quote.AmountToCharge,
            message = quote.BuildSummary()
        };
    }
}

[Serializable]
public class MarketServiceUICheckoutRecordRow {
    [Tooltip("Checkout record id.")]
    public string recordId;
    [Tooltip("Shop display name copied by the checkout record.")]
    public string shopName;
    [Tooltip("Payment rule display name used by the checkout.")]
    public string paymentRuleName;
    [Tooltip("Amount paid at checkout.")]
    public float amountPaid;
    [Tooltip("Basket item subtotal.")]
    public float itemSubtotal;
    [Tooltip("Checkout service fee.")]
    public float serviceFee;
    [Tooltip("Checkout discount total.")]
    public float discountTotal;
    [Tooltip("Number of lines checked out.")]
    public int lineCount;
    [Tooltip("Total purchase bundles checked out.")]
    public int bundleCount;
    [Tooltip("Absolute hour when checkout happened.")]
    public int absoluteHour;
    [Tooltip("Short text useful for placeholder UI.")]
    public string displayText;

    public static MarketServiceUICheckoutRecordRow FromRecord(ShopBasketCheckoutRecord record) {
        return new MarketServiceUICheckoutRecordRow {
            recordId = record.recordId,
            shopName = record.shopName,
            paymentRuleName = record.paymentRuleName,
            amountPaid = record.amountPaid,
            itemSubtotal = record.itemSubtotal,
            serviceFee = record.serviceFee,
            discountTotal = record.discountTotal,
            lineCount = record.lineCount,
            bundleCount = record.bundleCount,
            absoluteHour = record.absoluteHour,
            displayText = $"{record.shopName}: {record.amountPaid:0} paid"
        };
    }
}

[Serializable]
public class MarketServiceUIRefundQuoteRow {
    [Tooltip("Return policy id used by this quote.")]
    public string returnPolicyId;
    [Tooltip("Return policy display name.")]
    public string returnPolicyName;
    [Tooltip("Checkout record id being refunded.")]
    public string checkoutRecordId;
    [Tooltip("Gross refund before policy deductions.")]
    public float grossRefund;
    [Tooltip("Restocking fee subtracted from the refund.")]
    public float restockingFee;
    [Tooltip("Final amount refunded.")]
    public float amountRefunded;
    [Tooltip("Readable refund quote message.")]
    public string message;

    public static MarketServiceUIRefundQuoteRow FromQuote(ShopRefundQuote quote) {
        if(quote == null) {
            return null;
        }

        return new MarketServiceUIRefundQuoteRow {
            returnPolicyId = quote.returnPolicyId,
            returnPolicyName = quote.returnPolicyName,
            checkoutRecordId = quote.checkoutRecordId,
            grossRefund = quote.grossRefund,
            restockingFee = quote.restockingFee,
            amountRefunded = quote.amountRefunded,
            message = quote.BuildSummary()
        };
    }
}

[Serializable]
public class MarketServiceUIDeliveryQuoteRow {
    [Tooltip("Delivery service id used by this quote.")]
    public string serviceId;
    [Tooltip("Delivery service display name.")]
    public string serviceName;
    [Tooltip("Destination display name.")]
    public string destinationName;
    [Tooltip("Basket item subtotal.")]
    public float itemSubtotal;
    [Tooltip("Delivery fee.")]
    public float deliveryFee;
    [Tooltip("Total due for the delivery order.")]
    public float totalDue;
    [Tooltip("In-game hours until delivery is due.")]
    public int deliveryHours;
    [Tooltip("Absolute hour when delivery becomes due.")]
    public int dueAbsoluteHour;
    [Tooltip("Readable delivery quote message.")]
    public string message;

    public static MarketServiceUIDeliveryQuoteRow FromQuote(ShopDeliveryQuote quote) {
        if(quote == null) {
            return null;
        }

        return new MarketServiceUIDeliveryQuoteRow {
            serviceId = quote.serviceId,
            serviceName = quote.serviceName,
            destinationName = quote.destinationName,
            itemSubtotal = quote.itemSubtotal,
            deliveryFee = quote.deliveryFee,
            totalDue = quote.totalDue,
            deliveryHours = quote.deliveryHours,
            dueAbsoluteHour = quote.dueAbsoluteHour,
            message = quote.BuildSummary()
        };
    }
}

[Serializable]
public class MarketServiceUIDeliveryOrderRow {
    [Tooltip("Delivery order id.")]
    public string orderId;
    [Tooltip("Delivery service display name.")]
    public string serviceName;
    [Tooltip("Shop display name.")]
    public string shopName;
    [Tooltip("Destination display name.")]
    public string destinationName;
    [Tooltip("Total paid for this order.")]
    public float totalPaid;
    [Tooltip("Refund amount if cancelled in time.")]
    public float refundAmount;
    [Tooltip("Absolute hour when the order becomes due.")]
    public int dueAbsoluteHour;
    [Tooltip("Hours remaining until due. 0 means due now.")]
    public int hoursRemaining;
    [Tooltip("If enabled, this order can be claimed/delivered now.")]
    public bool isDue;
    [Tooltip("If enabled, the pending order can be cancelled.")]
    public bool canCancel;
    [Tooltip("Short text useful for placeholder UI.")]
    public string displayText;

    public static MarketServiceUIDeliveryOrderRow FromRecord(ShopDeliveryOrderRecord order, int currentAbsoluteHour) {
        int remaining = Mathf.Max(0, order.dueAbsoluteHour - currentAbsoluteHour);
        return new MarketServiceUIDeliveryOrderRow {
            orderId = order.orderId,
            serviceName = order.serviceName,
            shopName = order.shopName,
            destinationName = order.destinationName,
            totalPaid = order.totalPaid,
            refundAmount = order.refundAmount,
            dueAbsoluteHour = order.dueAbsoluteHour,
            hoursRemaining = remaining,
            isDue = order.IsDue(currentAbsoluteHour),
            canCancel = order.allowCancellation && currentAbsoluteHour <= order.cancellationDeadlineAbsoluteHour,
            displayText = $"{order.serviceName} to {order.destinationName}: {(remaining <= 0 ? "due" : remaining + "h")}"
        };
    }
}

[Serializable]
public class MarketServiceUILoyaltyProgramRow {
    [Tooltip("Loyalty program id.")]
    public string programId;
    [Tooltip("Loyalty program display name.")]
    public string displayName;
    [Tooltip("Loyalty program description.")]
    public string description;
    [Tooltip("Loyalty program kind.")]
    public string kind;
    [Tooltip("Money required to join.")]
    public float joinCost;
    [Tooltip("If enabled, source filtering says this program can be joined/refreshed.")]
    public bool isAvailable;
    [Tooltip("If enabled, the player already has this membership.")]
    public bool isOwned;
    [Tooltip("Current points owned by the player.")]
    public int points;
    [Tooltip("Current tier display name.")]
    public string tierName;
    [Tooltip("Short text useful for placeholder UI.")]
    public string displayText;

    public static MarketServiceUILoyaltyProgramRow FromProgram(LoyaltyProgramDefinition program, PlayerLoyaltyLog log, bool isAvailable) {
        var record = log != null ? log.Memberships.FirstOrDefault(item => item != null && item.programId == program.Id) : null;
        return new MarketServiceUILoyaltyProgramRow {
            programId = program.Id,
            displayName = program.DisplayName,
            description = program.Description,
            kind = program.Kind.ToString(),
            joinCost = program.JoinCost,
            isAvailable = isAvailable,
            isOwned = record != null,
            points = record != null ? record.points : 0,
            tierName = record != null ? record.CurrentTierName : string.Empty,
            displayText = $"{program.DisplayName} - {(record != null ? record.points + " pts" : program.JoinCost.ToString("0"))}"
        };
    }
}

[Serializable]
public class MarketServiceUIServicePackageRow {
    [Tooltip("Service package id.")]
    public string packageId;
    [Tooltip("Service package display name.")]
    public string displayName;
    [Tooltip("Service package description.")]
    public string description;
    [Tooltip("Service package category.")]
    public string category;
    [Tooltip("Expected total price for this package.")]
    public float expectedPrice;
    [Tooltip("If enabled, this package can be used now.")]
    public bool isAvailable;
    [Tooltip("If Is Available is false, this explains why when available.")]
    public string unavailableReason;
    [Tooltip("Short text useful for placeholder UI.")]
    public string displayText;

    public static MarketServiceUIServicePackageRow FromPackage(ServicePackageDefinition package, PlayerController player, PlayerServicePackageLog log, string sourceId, ShopCatalogDefinition catalog) {
        bool available = package.CanUse(player, log, sourceId, catalog, out var failure);
        float price = package.GetExpectedTotalPrice(player, catalog);
        return new MarketServiceUIServicePackageRow {
            packageId = package.Id,
            displayName = package.DisplayName,
            description = package.Description,
            category = package.Category.ToString(),
            expectedPrice = price,
            isAvailable = available,
            unavailableReason = failure,
            displayText = $"{package.DisplayName} - {price:0}"
        };
    }
}

[Serializable]
public class MarketServiceUIAppointmentQuoteRow {
    [Tooltip("Appointment id.")]
    public string appointmentId;
    [Tooltip("Appointment display name.")]
    public string appointmentName;
    [Tooltip("Scheduled in-game day.")]
    public int scheduledDay;
    [Tooltip("Scheduled in-game hour.")]
    public int scheduledHour;
    [Tooltip("Booking fee paid immediately.")]
    public float bookingFee;
    [Tooltip("Expected service/package cost due at completion.")]
    public float expectedCompletionCost;
    [Tooltip("How the appointment completes once due.")]
    public string completionMode;
    [Tooltip("Readable appointment quote message.")]
    public string message;

    public static MarketServiceUIAppointmentQuoteRow FromQuote(ServiceAppointmentQuote quote) {
        if(quote == null) {
            return null;
        }

        return new MarketServiceUIAppointmentQuoteRow {
            appointmentId = quote.appointmentId,
            appointmentName = quote.appointmentName,
            scheduledDay = quote.scheduledDay,
            scheduledHour = quote.scheduledHour,
            bookingFee = quote.bookingFee,
            expectedCompletionCost = quote.expectedCompletionCost,
            completionMode = quote.completionMode.ToString(),
            message = quote.BuildSummary()
        };
    }
}

[Serializable]
public class MarketServiceUIAppointmentRecordRow {
    [Tooltip("Appointment record id.")]
    public string recordId;
    [Tooltip("Appointment definition id.")]
    public string appointmentId;
    [Tooltip("Appointment display name.")]
    public string appointmentName;
    [Tooltip("Provider/source display name.")]
    public string sourceName;
    [Tooltip("Scheduled in-game day.")]
    public int scheduledDay;
    [Tooltip("Scheduled in-game hour.")]
    public int scheduledHour;
    [Tooltip("Booking fee paid for this appointment.")]
    public float bookingFee;
    [Tooltip("If enabled, appointment is still pending.")]
    public bool isPending;
    [Tooltip("If enabled, appointment is due now.")]
    public bool isDue;
    [Tooltip("If enabled, appointment was completed.")]
    public bool completed;
    [Tooltip("If enabled, appointment was cancelled.")]
    public bool cancelled;
    [Tooltip("Short text useful for placeholder UI.")]
    public string displayText;

    public static MarketServiceUIAppointmentRecordRow FromRecord(ServiceAppointmentRecord record, int currentAbsoluteHour) {
        return new MarketServiceUIAppointmentRecordRow {
            recordId = record.recordId,
            appointmentId = record.appointmentId,
            appointmentName = record.appointmentName,
            sourceName = record.sourceName,
            scheduledDay = record.scheduledDay,
            scheduledHour = record.scheduledHour,
            bookingFee = record.bookingFee,
            isPending = record.IsPending,
            isDue = record.IsDue(currentAbsoluteHour),
            completed = record.completed,
            cancelled = record.cancelled,
            displayText = $"{record.appointmentName}: day {record.scheduledDay} {record.scheduledHour:00}:00"
        };
    }
}
