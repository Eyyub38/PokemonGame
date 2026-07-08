using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum ShopRestockTimingMode {
    ManualOnly,
    Daily,
    EveryNDays,
    Weekly,
    CalendarEventActive
}

public enum ShopRestockRestoreMode {
    FullRestock,
    RestoreBundleCount
}

public enum ShopRestockSourceAction {
    PreviewDueRestocks,
    RunDueRestocks,
    ForceRunAll,
    CleanupOldStockHistory
}

[CreateAssetMenu(menuName = "Shops/Restock Schedule Definition")]
public class ShopRestockScheduleDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this restock schedule. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in debug/future market UI. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note or player-facing explanation of when this restock happens.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Optional icon used by future market, PokeNav or notification UI.")]
    [SerializeField] Sprite icon;
    [Tooltip("Free-form tags such as daily, weekly, rare, event, mart, apothecary or region name.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Catalog Filters")]
    [Tooltip("If assigned, this schedule only applies to this exact shop catalog.")]
    [SerializeField] ShopCatalogDefinition requiredCatalog;
    [Tooltip("If not empty, this schedule only applies to these catalog types.")]
    [SerializeField] List<ShopCatalogType> allowedCatalogTypes = new List<ShopCatalogType>();
    [Tooltip("Catalog tags required before this schedule can apply. Empty means no catalog tag filter.")]
    [SerializeField] List<string> requiredCatalogTags = new List<string>();
    [Tooltip("How required catalog tags are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode catalogTagMatchMode = ConsequenceRequirementMatchMode.All;

    [Header("Timing")]
    [Tooltip("How this schedule decides whether it is due.")]
    [SerializeField] ShopRestockTimingMode timingMode = ShopRestockTimingMode.Daily;
    [Tooltip("First in-game day this schedule can run.")]
    [Min(1)]
    [SerializeField] int startDay = 1;
    [Tooltip("Interval in days when Timing Mode is Every N Days.")]
    [Min(1)]
    [SerializeField] int repeatEveryDays = 1;
    [Tooltip("Weekdays used by Weekly timing. Empty means any weekday is valid.")]
    [SerializeField] List<WeekDay> activeWeekDays = new List<WeekDay>();
    [Tooltip("Optional calendar event that must be active when Timing Mode is Calendar Event Active.")]
    [SerializeField] CalendarEventDefinition calendarEvent;
    [Tooltip("Earliest in-game hour this schedule can run on a due day.")]
    [Range(0, 23)]
    [SerializeField] int earliestHour = 6;
    [Tooltip("Allowed day periods. Empty means any period after Earliest Hour.")]
    [SerializeField] List<DayPeriod> activePeriods = new List<DayPeriod>();
    [Tooltip("If enabled, this schedule can only run once per shop per in-game day unless forced.")]
    [SerializeField] bool restockOncePerDay = true;

    [Header("Offer Filters")]
    [Tooltip("If enabled, only offers with stock limits are considered.")]
    [SerializeField] bool onlyLimitedStockOffers = true;
    [Tooltip("Stock period types this schedule can restore. Empty means any limited stock period except None.")]
    [SerializeField] List<ShopStockLimitPeriod> allowedStockPeriods = new List<ShopStockLimitPeriod> { ShopStockLimitPeriod.Total };
    [Tooltip("Optional offer ids to restock. Empty means all offers that pass the other filters.")]
    [SerializeField] List<string> explicitOfferIds = new List<string>();
    [Tooltip("Optional item model categories to restock. Empty means all categories.")]
    [SerializeField] List<ItemModelCategory> allowedModelCategories = new List<ItemModelCategory>();
    [Tooltip("Item model tags required before an offer can be restocked. Direct itemOverride offers without a model cannot match model tags.")]
    [SerializeField] List<string> requiredModelTags = new List<string>();
    [Tooltip("How required model tags are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode modelTagMatchMode = ConsequenceRequirementMatchMode.All;
    [Tooltip("If enabled, locked offers are ignored for this player when restocking.")]
    [SerializeField] bool requireOfferUnlockedForPlayer;
    [Tooltip("If enabled, preview/result lines include matching offers that did not need stock restored.")]
    [SerializeField] bool includeUnchangedOffersInResult;

    [Header("Restore")]
    [Tooltip("How much purchased stock is removed from the player ledger.")]
    [SerializeField] ShopRestockRestoreMode restoreMode = ShopRestockRestoreMode.FullRestock;
    [Tooltip("Number of bundles restored when Restore Mode is Restore Bundle Count.")]
    [Min(1)]
    [SerializeField] int restoreBundleCount = 1;
    [Tooltip("If enabled, old daily stock purchase records before the current day are pruned after a successful run.")]
    [SerializeField] bool clearOldDailyHistory = true;
    [Tooltip("If enabled, old weekly stock purchase records before the current week are pruned after a successful run.")]
    [SerializeField] bool clearOldWeeklyHistory = true;
    [Tooltip("Message used when this schedule restores stock. Empty generates a default message.")]
    [TextArea]
    [SerializeField] string restockedMessage = string.Empty;
    [Tooltip("Message used when this schedule is not due. Empty generates a default message.")]
    [TextArea]
    [SerializeField] string notDueMessage = string.Empty;

    [Header("Access")]
    [Tooltip("Optional title, badge, permit or license required before this schedule can run.")]
    [SerializeField] TitleDefinition requiredTitle;
    [Tooltip("Optional milestone required before this schedule can run.")]
    [SerializeField] MilestoneDefinition requiredMilestone;
    [Tooltip("Optional faction whose reputation gates this schedule.")]
    [SerializeField] ReputationFactionDefinition requiredFaction;
    [Tooltip("Minimum required reputation with the selected faction.")]
    [SerializeField] int requiredReputation;
    [Tooltip("Optional world event whose active state gates this schedule.")]
    [SerializeField] WorldEventDefinition requiredWorldEvent;
    [Tooltip("Expected active state for Required World Event.")]
    [SerializeField] bool requiredWorldEventActive = true;
    [Tooltip("How extra requirements are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode requirementMatchMode = ConsequenceRequirementMatchMode.All;
    [Tooltip("Extra activity-style requirements checked before this schedule can run.")]
    [SerializeField] List<ActivityRequirement> requirements = new List<ActivityRequirement>();
    [Tooltip("Message/debug reason used when this schedule is locked.")]
    [TextArea]
    [SerializeField] string lockedMessage = "This restock schedule is not available.";

    [Header("Events")]
    [Tooltip("Optional event published when this schedule restores stock.")]
    [SerializeField] GameEventDefinition restockedEvent;
    [Tooltip("Optional event published when this schedule is checked but not due and Publish Skipped Events is enabled.")]
    [SerializeField] GameEventDefinition skippedEvent;
    [Tooltip("Optional event published when this schedule cannot run because setup or requirements are blocked.")]
    [SerializeField] GameEventDefinition blockedEvent;
    [Tooltip("If enabled, skipped/not-due evaluations publish events.")]
    [SerializeField] bool publishSkippedEvents;
    [Tooltip("If enabled, restock events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed = true;
    [Tooltip("If enabled, restock events are written to GameDebugLogger.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public ShopCatalogDefinition RequiredCatalog => requiredCatalog;
    public IReadOnlyList<ShopCatalogType> AllowedCatalogTypes => allowedCatalogTypes != null ? (IReadOnlyList<ShopCatalogType>)allowedCatalogTypes : Array.Empty<ShopCatalogType>();
    public IReadOnlyList<string> RequiredCatalogTags => requiredCatalogTags != null ? (IReadOnlyList<string>)requiredCatalogTags : Array.Empty<string>();
    public ShopRestockTimingMode TimingMode => timingMode;
    public int StartDay => Mathf.Max(1, startDay);
    public int RepeatEveryDays => Mathf.Max(1, repeatEveryDays);
    public IReadOnlyList<WeekDay> ActiveWeekDays => activeWeekDays != null ? (IReadOnlyList<WeekDay>)activeWeekDays : Array.Empty<WeekDay>();
    public CalendarEventDefinition CalendarEvent => calendarEvent;
    public int EarliestHour => Mathf.Clamp(earliestHour, 0, 23);
    public IReadOnlyList<DayPeriod> ActivePeriods => activePeriods != null ? (IReadOnlyList<DayPeriod>)activePeriods : Array.Empty<DayPeriod>();
    public bool RestockOncePerDay => restockOncePerDay;
    public bool OnlyLimitedStockOffers => onlyLimitedStockOffers;
    public IReadOnlyList<ShopStockLimitPeriod> AllowedStockPeriods => allowedStockPeriods != null ? (IReadOnlyList<ShopStockLimitPeriod>)allowedStockPeriods : Array.Empty<ShopStockLimitPeriod>();
    public IReadOnlyList<string> ExplicitOfferIds => explicitOfferIds != null ? (IReadOnlyList<string>)explicitOfferIds : Array.Empty<string>();
    public IReadOnlyList<ItemModelCategory> AllowedModelCategories => allowedModelCategories != null ? (IReadOnlyList<ItemModelCategory>)allowedModelCategories : Array.Empty<ItemModelCategory>();
    public IReadOnlyList<string> RequiredModelTags => requiredModelTags != null ? (IReadOnlyList<string>)requiredModelTags : Array.Empty<string>();
    public bool RequireOfferUnlockedForPlayer => requireOfferUnlockedForPlayer;
    public bool IncludeUnchangedOffersInResult => includeUnchangedOffersInResult;
    public ShopRestockRestoreMode RestoreMode => restoreMode;
    public int RestoreBundleCount => Mathf.Max(1, restoreBundleCount);
    public bool ClearOldDailyHistory => clearOldDailyHistory;
    public bool ClearOldWeeklyHistory => clearOldWeeklyHistory;
    public IReadOnlyList<ActivityRequirement> Requirements => requirements != null ? (IReadOnlyList<ActivityRequirement>)requirements : Array.Empty<ActivityRequirement>();

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    public bool TryPreview(PlayerController player, ShopCatalog shop, PlayerShopLedger ledger, PlayerShopRestockLog restockLog, string sourceId, out ShopRestockRunResult result, out string failureMessage) {
        return Evaluate(player, shop, ledger, restockLog, sourceId, applyRestock: false, force: false, out result, out failureMessage);
    }

    public bool TryRun(PlayerController player, ShopCatalog shop, PlayerShopLedger ledger, PlayerShopRestockLog restockLog, string sourceId, bool force, out ShopRestockRunResult result, out string failureMessage) {
        return Evaluate(player, shop, ledger, restockLog, sourceId, applyRestock: true, force: force, out result, out failureMessage);
    }

    bool Evaluate(
        PlayerController player,
        ShopCatalog shop,
        PlayerShopLedger ledger,
        PlayerShopRestockLog restockLog,
        string sourceId,
        bool applyRestock,
        bool force,
        out ShopRestockRunResult result,
        out string failureMessage
    ) {
        result = CreateBaseResult(shop, sourceId, force);
        if(!CanRun(player, shop, out failureMessage)) {
            result.blocked = true;
            result.message = failureMessage;
            PublishRestockEvent(blockedEvent, "blocked", result, player, shop, sourceId, GameEventImportance.Warning, failureMessage);
            return false;
        }

        if(!force && !IsDueNow(out var notDueReason)) {
            result.skipped = true;
            result.message = notDueReason;
            if(publishSkippedEvents) {
                PublishRestockEvent(skippedEvent, "skipped", result, player, shop, sourceId, GameEventImportance.Trace, notDueReason);
            }
            failureMessage = null;
            return true;
        }

        if(!force && restockOncePerDay && restockLog != null && restockLog.HasRestockedOnDay(this, shop.ShopId, GetCurrentDay())) {
            result.skipped = true;
            result.message = $"{DisplayName} already restocked today.";
            if(publishSkippedEvents) {
                PublishRestockEvent(skippedEvent, "skipped", result, player, shop, sourceId, GameEventImportance.Trace, result.message);
            }
            failureMessage = null;
            return true;
        }

        var offers = GetMatchingOffers(player, shop);
        foreach(var entry in offers) {
            AddOfferResult(result, ledger, shop, entry, applyRestock);
        }

        if(applyRestock && ledger != null) {
            if(clearOldDailyHistory) {
                ledger.ClearDailyHistoryBefore(GetCurrentDay());
            }

            if(clearOldWeeklyHistory) {
                ledger.ClearWeeklyHistoryBefore(GetCurrentWeekIndex());
            }
        }

        result.applied = applyRestock;
        result.message = BuildResultMessage(result, offers.Count);

        if(applyRestock && restockLog != null) {
            var record = restockLog.RecordRestock(this, shop, result, sourceId);
            result.recordId = record != null ? record.recordId : string.Empty;
        }

        if(applyRestock) {
            PublishRestockEvent(restockedEvent, "restocked", result, player, shop, sourceId, GameEventImportance.Success, null);
        }

        failureMessage = null;
        return true;
    }

    public bool CanRun(PlayerController player, ShopCatalog shop, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required for shop restock schedules.";
            return false;
        }

        if(shop == null || shop.Catalog == null) {
            failureMessage = "A shop catalog is required for restock schedules.";
            return false;
        }

        if(requiredCatalog != null && shop.Catalog != requiredCatalog) {
            failureMessage = $"{DisplayName} cannot restock this shop.";
            return false;
        }

        if(allowedCatalogTypes.Count > 0 && !allowedCatalogTypes.Contains(shop.Catalog.CatalogType)) {
            failureMessage = $"{DisplayName} cannot restock this shop type.";
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

        failureMessage = null;
        return true;
    }

    public bool IsDueNow(out string reason) {
        int day = GetCurrentDay();
        int hour = GetCurrentHour();
        if(timingMode == ShopRestockTimingMode.ManualOnly) {
            reason = string.IsNullOrWhiteSpace(notDueMessage) ? $"{DisplayName} is manual only." : notDueMessage;
            return false;
        }

        if(day < StartDay) {
            reason = string.IsNullOrWhiteSpace(notDueMessage) ? $"{DisplayName} starts on day {StartDay}." : notDueMessage;
            return false;
        }

        if(hour < EarliestHour) {
            reason = string.IsNullOrWhiteSpace(notDueMessage) ? $"{DisplayName} can restock after {EarliestHour:00}:00." : notDueMessage;
            return false;
        }

        if(activePeriods.Count > 0) {
            DayPeriod currentPeriod = TimeSystem.i != null ? TimeSystem.i.CurrentPeriod : DayPeriod.None;
            if(!activePeriods.Contains(currentPeriod)) {
                reason = string.IsNullOrWhiteSpace(notDueMessage) ? $"{DisplayName} is not active during this period." : notDueMessage;
                return false;
            }
        }

        bool due = timingMode switch {
            ShopRestockTimingMode.Daily => true,
            ShopRestockTimingMode.EveryNDays => (day - StartDay) % RepeatEveryDays == 0,
            ShopRestockTimingMode.Weekly => activeWeekDays == null || activeWeekDays.Count == 0 || activeWeekDays.Contains(GetWeekDay(day)),
            ShopRestockTimingMode.CalendarEventActive => calendarEvent != null && calendarEvent.IsActiveNow(),
            _ => false
        };

        reason = due ? null : string.IsNullOrWhiteSpace(notDueMessage) ? $"{DisplayName} is not due now." : notDueMessage;
        return due;
    }

    List<ShopCatalogEntry> GetMatchingOffers(PlayerController player, ShopCatalog shop) {
        if(shop == null || shop.Catalog == null || shop.Catalog.Entries == null) {
            return new List<ShopCatalogEntry>();
        }

        return shop.Catalog.Entries
            .Where(entry => entry != null)
            .Where(entry => OfferMatches(player, entry))
            .OrderBy(entry => entry.SortOrder)
            .ThenBy(entry => entry.DisplayName)
            .ToList();
    }

    bool OfferMatches(PlayerController player, ShopCatalogEntry entry) {
        if(entry == null) {
            return false;
        }

        if(onlyLimitedStockOffers && !entry.HasStockLimit) {
            return false;
        }

        if(allowedStockPeriods != null && allowedStockPeriods.Count > 0 && !allowedStockPeriods.Contains(entry.stockLimitPeriod)) {
            return false;
        }

        var activeOfferIds = explicitOfferIds?.Where(offerId => !string.IsNullOrWhiteSpace(offerId)).ToList() ?? new List<string>();
        if(activeOfferIds.Count > 0 && !activeOfferIds.Any(offerId => string.Equals(offerId, entry.OfferId, StringComparison.OrdinalIgnoreCase))) {
            return false;
        }

        if(allowedModelCategories != null && allowedModelCategories.Count > 0) {
            if(entry.model == null || !allowedModelCategories.Contains(entry.model.Category)) {
                return false;
            }
        }

        if(!MatchesTags(requiredModelTags, modelTagMatchMode, tag => entry.model != null && entry.model.HasTag(tag))) {
            return false;
        }

        if(requireOfferUnlockedForPlayer && !entry.IsUnlocked(player, out _)) {
            return false;
        }

        return true;
    }

    void AddOfferResult(ShopRestockRunResult result, PlayerShopLedger ledger, ShopCatalog shop, ShopCatalogEntry entry, bool applyRestock) {
        if(result == null || shop == null || entry == null) {
            return;
        }

        int before = ledger != null ? ledger.GetPurchasedCount(shop.ShopId, entry.OfferId, entry.stockLimitPeriod) : 0;
        int restoreAmount = restoreMode == ShopRestockRestoreMode.FullRestock ? before : Mathf.Min(before, RestoreBundleCount);
        int restored = applyRestock && ledger != null
            ? ledger.RestoreStock(shop.ShopId, entry.OfferId, entry.stockLimitPeriod, restoreMode == ShopRestockRestoreMode.FullRestock ? 0 : RestoreBundleCount)
            : restoreAmount;
        int after = Mathf.Max(0, before - restored);

        if(restored <= 0 && !includeUnchangedOffersInResult) {
            return;
        }

        result.lines.Add(new ShopRestockOfferResult {
            offerId = entry.OfferId,
            offerName = entry.DisplayName,
            stockPeriod = entry.stockLimitPeriod,
            stockLimit = Mathf.Max(0, entry.stockLimit),
            purchasedBefore = before,
            restoredBundles = restored,
            purchasedAfter = after,
            remainingBefore = entry.HasStockLimit ? Mathf.Max(0, entry.stockLimit - before) : -1,
            remainingAfter = entry.HasStockLimit ? Mathf.Max(0, entry.stockLimit - after) : -1
        });
        result.matchedOfferCount++;
        result.restoredBundleCount += Mathf.Max(0, restored);
    }

    ShopRestockRunResult CreateBaseResult(ShopCatalog shop, string sourceId, bool forced) {
        return new ShopRestockRunResult {
            scheduleId = Id,
            scheduleName = DisplayName,
            timingMode = timingMode,
            restoreMode = restoreMode,
            shopId = shop != null ? shop.ShopId : string.Empty,
            catalogId = shop != null && shop.Catalog != null ? shop.Catalog.Id : string.Empty,
            shopName = shop != null && shop.Catalog != null ? shop.Catalog.DisplayName : string.Empty,
            sourceId = string.IsNullOrWhiteSpace(sourceId) ? Id : sourceId,
            forced = forced,
            day = GetCurrentDay(),
            hour = GetCurrentHour(),
            absoluteHour = GetCurrentAbsoluteHour()
        };
    }

    string BuildResultMessage(ShopRestockRunResult result, int matchingOfferCount) {
        if(result == null) {
            return string.Empty;
        }

        if(!string.IsNullOrWhiteSpace(restockedMessage) && result.restoredBundleCount > 0) {
            return restockedMessage;
        }

        if(matchingOfferCount == 0) {
            return $"{DisplayName} found no matching offers to restock.";
        }

        if(result.restoredBundleCount <= 0) {
            return $"{DisplayName} ran, but no depleted stock needed restoring.";
        }

        return $"{DisplayName} restored {result.restoredBundleCount} bundle(s).";
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

    WeekDay GetWeekDay(int day) {
        int index = Mathf.Abs(day - 1) % 7;
        return (WeekDay)index;
    }

    void PublishRestockEvent(GameEventDefinition eventDefinition, string phase, ShopRestockRunResult result, PlayerController player, ShopCatalog shop, string sourceId, GameEventImportance importance, string failureMessage) {
        GameEventPublishing.PublishOptional(
            eventDefinition,
            $"shop.restock.{phase}.{Id}.{shop?.ShopId ?? "shop"}",
            !string.IsNullOrWhiteSpace(failureMessage) ? failureMessage : result != null && !string.IsNullOrWhiteSpace(result.message) ? result.message : $"{DisplayName} restock {phase}.",
            GameEventCategory.Shop,
            importance,
            player != null ? player : this,
            "ShopRestockScheduleDefinition",
            GameEventScope.Player,
            showInFeed: showEventsInFeed,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("restockScheduleId", Id),
            GameEventPublishing.Value("restockScheduleName", DisplayName),
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("shopId", shop != null ? shop.ShopId : string.Empty),
            GameEventPublishing.Value("sourceId", sourceId),
            GameEventPublishing.Value("timingMode", timingMode),
            GameEventPublishing.Value("restoreMode", restoreMode),
            GameEventPublishing.Value("forced", result != null && result.forced),
            GameEventPublishing.Value("applied", result != null && result.applied),
            GameEventPublishing.Value("skipped", result != null && result.skipped),
            GameEventPublishing.Value("restoredBundleCount", result != null ? result.restoredBundleCount : 0),
            GameEventPublishing.Value("matchedOfferCount", result != null ? result.matchedOfferCount : 0));
    }

    int GetCurrentDay() {
        return TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
    }

    int GetCurrentHour() {
        return TimeSystem.i != null ? Mathf.Clamp(TimeSystem.i.Hour, 0, 23) : 0;
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    int GetCurrentWeekIndex() {
        return Mathf.Max(0, (GetCurrentDay() - 1) / 7);
    }
}

public class ShopRestockSource : MonoBehaviour, IPlayerTriggerable, Interactable {
    [Header("Identity")]
    [Tooltip("Stable source id written into restock records. Empty uses this GameObject name.")]
    [SerializeField] string sourceId = string.Empty;
    [Tooltip("Readable source name for debug/future UI. Empty uses this GameObject name.")]
    [SerializeField] string displayName = string.Empty;

    [Header("Restock")]
    [Tooltip("Shop whose catalog and ledger stock this source refreshes.")]
    [SerializeField] ShopCatalog shop;
    [Tooltip("Restock schedules evaluated by this source.")]
    [SerializeField] List<ShopRestockScheduleDefinition> schedules = new List<ShopRestockScheduleDefinition>();
    [Tooltip("Optional explicit player. Empty uses the triggering/interacting player or PlayerController.i.")]
    [SerializeField] PlayerController playerOverride;
    [Tooltip("Action applied when this source is triggered.")]
    [SerializeField] ShopRestockSourceAction triggerAction = ShopRestockSourceAction.RunDueRestocks;
    [Tooltip("Action applied when an Interactable flow calls Interact.")]
    [SerializeField] ShopRestockSourceAction interactAction = ShopRestockSourceAction.RunDueRestocks;
    [Tooltip("If enabled, player trigger applies Trigger Action.")]
    [SerializeField] bool applyOnPlayerTrigger = true;
    [Tooltip("If enabled, this trigger can be called repeatedly by the player.")]
    [SerializeField] bool triggerRepeatedly = true;
    [Tooltip("If enabled, due schedules are evaluated when this component starts.")]
    [SerializeField] bool runOnStart;
    [Tooltip("If enabled, due schedules are evaluated when TimeSystem reports a new day.")]
    [SerializeField] bool runOnDayChanged = true;
    [Tooltip("If enabled, due schedules are evaluated when TimeSystem reports a time change.")]
    [SerializeField] bool runOnTimeChanged;

    [Header("Maintenance")]
    [Tooltip("If enabled, Cleanup Old Stock History clears daily history before the current day.")]
    [SerializeField] bool cleanupDailyHistory = true;
    [Tooltip("If enabled, Cleanup Old Stock History clears weekly history before the current week.")]
    [SerializeField] bool cleanupWeeklyHistory = true;

    [Header("Feedback")]
    [Tooltip("If enabled, result text is shown through the existing DialogManager when available.")]
    [SerializeField] bool showDialogFeedback;
    [Tooltip("If enabled, blocked restock actions are written to GameDebug.")]
    [SerializeField] bool logBlockedAttempts = true;
    [Tooltip("If enabled, successful restock actions are written to GameDebug.")]
    [SerializeField] bool logSuccessfulAttempts;

    bool subscribedToTime;

    public bool TriggerRepeatedly => triggerRepeatedly;
    public string SourceId => ResolveSourceId();
    public string DisplayName => !string.IsNullOrWhiteSpace(displayName) ? displayName : gameObject.name;
    public ShopCatalog Shop => shop;
    public IReadOnlyList<ShopRestockScheduleDefinition> Schedules => schedules != null ? (IReadOnlyList<ShopRestockScheduleDefinition>)schedules : Array.Empty<ShopRestockScheduleDefinition>();
    public ShopRestockSourceAction TriggerAction => triggerAction;
    public ShopRestockSourceAction InteractAction => interactAction;
    public IReadOnlyList<ShopRestockRunResult> LastResults => lastResults;

    readonly List<ShopRestockRunResult> lastResults = new List<ShopRestockRunResult>();

    void OnEnable() {
        SubscribeToTime();
    }

    void Start() {
        SubscribeToTime();
        if(runOnStart) {
            RunSchedules(ResolvePlayer(null), force: false, applyRestock: true, out _);
        }
    }

    void OnDisable() {
        UnsubscribeFromTime();
    }

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

    public bool PreviewDueRestocks(PlayerController player, out List<ShopRestockRunResult> results, out string failureMessage) {
        return RunSchedules(ResolvePlayer(player), force: false, applyRestock: false, out results, out failureMessage);
    }

    public bool RunDueRestocks(PlayerController player, out List<ShopRestockRunResult> results, out string failureMessage) {
        return RunSchedules(ResolvePlayer(player), force: false, applyRestock: true, out results, out failureMessage);
    }

    public bool ForceRunAll(PlayerController player, out List<ShopRestockRunResult> results, out string failureMessage) {
        return RunSchedules(ResolvePlayer(player), force: true, applyRestock: true, out results, out failureMessage);
    }

    bool ApplyAction(ShopRestockSourceAction action, PlayerController player, out string feedback) {
        feedback = null;
        switch(action) {
            case ShopRestockSourceAction.PreviewDueRestocks:
                if(PreviewDueRestocks(player, out var previewResults, out feedback)) {
                    feedback = BuildFeedback(previewResults, "Restock preview ready.");
                    return true;
                }
                return false;
            case ShopRestockSourceAction.ForceRunAll:
                if(ForceRunAll(player, out var forcedResults, out feedback)) {
                    feedback = BuildFeedback(forcedResults, "Forced restock completed.");
                    return true;
                }
                return false;
            case ShopRestockSourceAction.CleanupOldStockHistory:
                return CleanupOldStockHistory(player, out feedback);
            default:
                if(RunDueRestocks(player, out var runResults, out feedback)) {
                    feedback = BuildFeedback(runResults, "Restock check completed.");
                    return true;
                }
                return false;
        }
    }

    bool RunSchedules(PlayerController player, bool force, bool applyRestock, out List<ShopRestockRunResult> results) {
        return RunSchedules(player, force, applyRestock, out results, out _);
    }

    bool RunSchedules(PlayerController player, bool force, bool applyRestock, out List<ShopRestockRunResult> results, out string failureMessage) {
        results = new List<ShopRestockRunResult>();
        lastResults.Clear();

        if(player == null) {
            failureMessage = "A player is required for shop restock sources.";
            RecordBlocked(player, failureMessage);
            return false;
        }

        if(shop == null) {
            failureMessage = "Shop restock source has no shop assigned.";
            RecordBlocked(player, failureMessage);
            return false;
        }

        if(schedules == null || schedules.Count == 0) {
            failureMessage = "Shop restock source has no schedules assigned.";
            RecordBlocked(player, failureMessage);
            return false;
        }

        var ledger = player.GetComponent<PlayerShopLedger>() ?? player.gameObject.AddComponent<PlayerShopLedger>();
        var restockLog = player.GetComponent<PlayerShopRestockLog>() ?? player.gameObject.AddComponent<PlayerShopRestockLog>();
        bool anySuccess = false;
        foreach(var schedule in schedules) {
            if(schedule == null) {
                continue;
            }

            bool success = applyRestock
                ? schedule.TryRun(player, shop, ledger, restockLog, ResolveSourceId(), force, out var result, out var scheduleFailure)
                : schedule.TryPreview(player, shop, ledger, restockLog, ResolveSourceId(), out result, out scheduleFailure);

            if(result != null) {
                results.Add(result);
                lastResults.Add(result);
            }

            if(success) {
                anySuccess = true;
            } else if(logBlockedAttempts && !string.IsNullOrWhiteSpace(scheduleFailure)) {
                GameDebug.Warning(scheduleFailure, GameDebugCategory.Shop, this, "ShopRestockSource");
            }
        }

        failureMessage = anySuccess ? null : "No restock schedules could run.";
        if(anySuccess && logSuccessfulAttempts) {
            int restored = results.Sum(result => result != null ? result.restoredBundleCount : 0);
            GameDebug.Success($"{DisplayName} evaluated {results.Count} restock schedule(s), restored {restored} bundle(s).", GameDebugCategory.Shop, this, "ShopRestockSource");
        }

        return anySuccess;
    }

    bool CleanupOldStockHistory(PlayerController player, out string feedback) {
        player = ResolvePlayer(player);
        if(player == null) {
            feedback = "A player is required to clean shop stock history.";
            RecordBlocked(player, feedback);
            return false;
        }

        var ledger = player.GetComponent<PlayerShopLedger>();
        if(ledger == null) {
            feedback = "Player has no shop ledger to clean.";
            RecordBlocked(player, feedback);
            return false;
        }

        if(cleanupDailyHistory) {
            ledger.ClearDailyHistoryBefore(GetCurrentDay());
        }

        if(cleanupWeeklyHistory) {
            ledger.ClearWeeklyHistoryBefore(GetCurrentWeekIndex());
        }

        feedback = "Old shop stock history cleaned.";
        return true;
    }

    string BuildFeedback(List<ShopRestockRunResult> results, string fallback) {
        if(results == null || results.Count == 0) {
            return fallback;
        }

        int restored = results.Sum(result => result != null ? result.restoredBundleCount : 0);
        int skipped = results.Count(result => result != null && result.skipped);
        return $"{DisplayName}: {restored} bundle(s) restored, {skipped} schedule(s) skipped.";
    }

    void HandleTimeChanged() {
        if(runOnTimeChanged) {
            RunSchedules(ResolvePlayer(null), force: false, applyRestock: true, out _);
        }
    }

    void HandleDayChanged() {
        if(runOnDayChanged) {
            RunSchedules(ResolvePlayer(null), force: false, applyRestock: true, out _);
        }
    }

    void SubscribeToTime() {
        if(subscribedToTime || TimeSystem.i == null) {
            return;
        }

        TimeSystem.i.OnTimeChanged += HandleTimeChanged;
        TimeSystem.i.OnDayChanged += HandleDayChanged;
        subscribedToTime = true;
    }

    void UnsubscribeFromTime() {
        if(!subscribedToTime || TimeSystem.i == null) {
            return;
        }

        TimeSystem.i.OnTimeChanged -= HandleTimeChanged;
        TimeSystem.i.OnDayChanged -= HandleDayChanged;
        subscribedToTime = false;
    }

    void RecordBlocked(PlayerController player, string failureMessage) {
        if(logBlockedAttempts && !string.IsNullOrWhiteSpace(failureMessage)) {
            GameDebug.Warning(failureMessage, GameDebugCategory.Shop, player != null ? player : this, "ShopRestockSource");
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
        return string.IsNullOrWhiteSpace(sourceId) ? gameObject.name : sourceId;
    }

    int GetCurrentDay() {
        return TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
    }

    int GetCurrentWeekIndex() {
        return Mathf.Max(0, (GetCurrentDay() - 1) / 7);
    }
}

public class PlayerShopRestockLog : MonoBehaviour, ISavable {
    [Tooltip("Maximum restock records kept in memory/save data. Older records are trimmed first. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxRecords = 200;
    [Tooltip("Runtime/save history of shop restock runs.")]
    [SerializeField] List<ShopRestockRecord> records = new List<ShopRestockRecord>();

    public IReadOnlyList<ShopRestockRecord> Records => records;
    public event Action<ShopRestockRecord> OnRestockRecorded;

    public ShopRestockRecord RecordRestock(ShopRestockScheduleDefinition schedule, ShopCatalog shop, ShopRestockRunResult result, string sourceId) {
        if(schedule == null || result == null) {
            return null;
        }

        var record = new ShopRestockRecord(result) {
            recordId = Guid.NewGuid().ToString("N"),
            scheduleId = schedule.Id,
            scheduleName = schedule.DisplayName,
            shopId = shop != null ? shop.ShopId : result.shopId,
            catalogId = shop != null && shop.Catalog != null ? shop.Catalog.Id : result.catalogId,
            shopName = shop != null && shop.Catalog != null ? shop.Catalog.DisplayName : result.shopName,
            sourceId = string.IsNullOrWhiteSpace(sourceId) ? result.sourceId : sourceId
        };

        records.Add(record);
        TrimHistory();
        OnRestockRecorded?.Invoke(record);
        return record;
    }

    public bool HasRestockedOnDay(ShopRestockScheduleDefinition schedule, string shopId, int day) {
        if(schedule == null) {
            return false;
        }

        return records.Any(record => record != null
            && record.scheduleId == schedule.Id
            && record.shopId == shopId
            && record.day == day
            && record.applied
            && !record.skipped
            && !record.blocked);
    }

    public ShopRestockRecord GetLatestRecord(string shopId = null, string scheduleId = null) {
        return records
            .Where(record => record != null
                && (string.IsNullOrWhiteSpace(shopId) || record.shopId == shopId)
                && (string.IsNullOrWhiteSpace(scheduleId) || record.scheduleId == scheduleId))
            .OrderByDescending(record => record.absoluteHour)
            .FirstOrDefault();
    }

    void TrimHistory() {
        if(maxRecords <= 0 || records.Count <= maxRecords) {
            return;
        }

        records = records
            .Where(record => record != null)
            .OrderByDescending(record => record.absoluteHour)
            .Take(maxRecords)
            .OrderBy(record => record.absoluteHour)
            .ToList();
    }

    public object CaptureState() {
        TrimHistory();
        return new PlayerShopRestockLogSaveData {
            records = records.Where(record => record != null).Select(record => record.Clone()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerShopRestockLogSaveData;
        records = saveData?.records?.Where(record => record != null).Select(record => record.Clone()).ToList()
            ?? new List<ShopRestockRecord>();
        TrimHistory();
    }
}

[Serializable]
public class ShopRestockRunResult {
    [Tooltip("Restock record id created when this run is saved to PlayerShopRestockLog.")]
    public string recordId;
    [Tooltip("Restock schedule id that produced this result.")]
    public string scheduleId;
    [Tooltip("Restock schedule display name copied for fallback/debug output.")]
    public string scheduleName;
    [Tooltip("Shop instance id evaluated by this result.")]
    public string shopId;
    [Tooltip("Catalog definition id evaluated by this result.")]
    public string catalogId;
    [Tooltip("Shop display name copied for fallback/debug output.")]
    public string shopName;
    [Tooltip("Source id that requested this restock.")]
    public string sourceId;
    [Tooltip("Timing mode used by this result.")]
    public ShopRestockTimingMode timingMode;
    [Tooltip("Restore mode used by this result.")]
    public ShopRestockRestoreMode restoreMode;
    [Tooltip("If enabled, this run ignored normal timing/once-per-day checks.")]
    public bool forced;
    [Tooltip("If enabled, this result applied ledger changes.")]
    public bool applied;
    [Tooltip("If enabled, this result was skipped because it was not due or already ran.")]
    public bool skipped;
    [Tooltip("If enabled, this result was blocked by setup or requirements.")]
    public bool blocked;
    [Tooltip("Number of matching offer lines included in this result.")]
    [Min(0)]
    public int matchedOfferCount;
    [Tooltip("Total bundle count restored by this result.")]
    [Min(0)]
    public int restoredBundleCount;
    [Tooltip("Readable result message.")]
    public string message;
    [Tooltip("In-game day when this result was evaluated.")]
    public int day;
    [Tooltip("In-game hour when this result was evaluated.")]
    public int hour;
    [Tooltip("Absolute in-game hour when this result was evaluated.")]
    public int absoluteHour;
    [Tooltip("Per-offer restock result lines.")]
    public List<ShopRestockOfferResult> lines = new List<ShopRestockOfferResult>();

    public string BuildSummary() {
        if(skipped) {
            return $"{scheduleName}: skipped.";
        }

        if(blocked) {
            return $"{scheduleName}: blocked.";
        }

        return $"{scheduleName}: restored {restoredBundleCount} bundle(s) across {matchedOfferCount} offer(s).";
    }
}

[Serializable]
public class ShopRestockOfferResult {
    [Tooltip("Offer id restored by this line.")]
    public string offerId;
    [Tooltip("Offer display name copied for fallback/debug output.")]
    public string offerName;
    [Tooltip("Stock period restored by this line.")]
    public ShopStockLimitPeriod stockPeriod;
    [Tooltip("Configured stock limit for this offer.")]
    [Min(0)]
    public int stockLimit;
    [Tooltip("Purchased bundle count before restock.")]
    [Min(0)]
    public int purchasedBefore;
    [Tooltip("Bundle count restored by this line.")]
    [Min(0)]
    public int restoredBundles;
    [Tooltip("Purchased bundle count after restock.")]
    [Min(0)]
    public int purchasedAfter;
    [Tooltip("Remaining stock before restock. -1 means unlimited stock.")]
    public int remainingBefore;
    [Tooltip("Remaining stock after restock. -1 means unlimited stock.")]
    public int remainingAfter;

    public ShopRestockOfferResult Clone() {
        return new ShopRestockOfferResult {
            offerId = offerId,
            offerName = offerName,
            stockPeriod = stockPeriod,
            stockLimit = stockLimit,
            purchasedBefore = purchasedBefore,
            restoredBundles = restoredBundles,
            purchasedAfter = purchasedAfter,
            remainingBefore = remainingBefore,
            remainingAfter = remainingAfter
        };
    }
}

[Serializable]
public class ShopRestockRecord {
    [Tooltip("Unique runtime/save id for this restock record.")]
    public string recordId;
    [Tooltip("Restock schedule id that produced this record.")]
    public string scheduleId;
    [Tooltip("Restock schedule display name copied for fallback/debug output.")]
    public string scheduleName;
    [Tooltip("Shop instance id evaluated by this record.")]
    public string shopId;
    [Tooltip("Catalog definition id evaluated by this record.")]
    public string catalogId;
    [Tooltip("Shop display name copied for fallback/debug output.")]
    public string shopName;
    [Tooltip("Source id that requested this restock.")]
    public string sourceId;
    [Tooltip("Timing mode used by this record.")]
    public ShopRestockTimingMode timingMode;
    [Tooltip("Restore mode used by this record.")]
    public ShopRestockRestoreMode restoreMode;
    [Tooltip("If enabled, this run ignored normal timing/once-per-day checks.")]
    public bool forced;
    [Tooltip("If enabled, this record applied ledger changes.")]
    public bool applied;
    [Tooltip("If enabled, this record was skipped because it was not due or already ran.")]
    public bool skipped;
    [Tooltip("If enabled, this record was blocked by setup or requirements.")]
    public bool blocked;
    [Tooltip("Number of matching offer lines included in this record.")]
    [Min(0)]
    public int matchedOfferCount;
    [Tooltip("Total bundle count restored by this record.")]
    [Min(0)]
    public int restoredBundleCount;
    [Tooltip("Readable result message.")]
    public string message;
    [Tooltip("In-game day when this record was created.")]
    public int day;
    [Tooltip("In-game hour when this record was created.")]
    public int hour;
    [Tooltip("Absolute in-game hour when this record was created.")]
    public int absoluteHour;
    [Tooltip("Per-offer restock record lines.")]
    public List<ShopRestockOfferResult> lines = new List<ShopRestockOfferResult>();

    public ShopRestockRecord() {
    }

    public ShopRestockRecord(ShopRestockRunResult result) {
        if(result == null) {
            return;
        }

        scheduleId = result.scheduleId;
        scheduleName = result.scheduleName;
        shopId = result.shopId;
        catalogId = result.catalogId;
        shopName = result.shopName;
        sourceId = result.sourceId;
        timingMode = result.timingMode;
        restoreMode = result.restoreMode;
        forced = result.forced;
        applied = result.applied;
        skipped = result.skipped;
        blocked = result.blocked;
        matchedOfferCount = result.matchedOfferCount;
        restoredBundleCount = result.restoredBundleCount;
        message = result.message;
        day = result.day;
        hour = result.hour;
        absoluteHour = result.absoluteHour;
        lines = result.lines != null ? result.lines.Where(line => line != null).Select(line => line.Clone()).ToList() : new List<ShopRestockOfferResult>();
    }

    public ShopRestockRecord Clone() {
        return new ShopRestockRecord {
            recordId = recordId,
            scheduleId = scheduleId,
            scheduleName = scheduleName,
            shopId = shopId,
            catalogId = catalogId,
            shopName = shopName,
            sourceId = sourceId,
            timingMode = timingMode,
            restoreMode = restoreMode,
            forced = forced,
            applied = applied,
            skipped = skipped,
            blocked = blocked,
            matchedOfferCount = matchedOfferCount,
            restoredBundleCount = restoredBundleCount,
            message = message,
            day = day,
            hour = hour,
            absoluteHour = absoluteHour,
            lines = lines != null ? lines.Where(line => line != null).Select(line => line.Clone()).ToList() : new List<ShopRestockOfferResult>()
        };
    }
}

[Serializable]
public class PlayerShopRestockLogSaveData {
    [Tooltip("Saved shop restock records.")]
    public List<ShopRestockRecord> records;
}
