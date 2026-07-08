using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum ShopSecurityIncidentKind {
    UnpaidBasketExit,
    BasketInspection,
    SuspiciousCheckoutFailure,
    ManualInspection,
    Custom
}

public enum ShopSecurityConsequenceMode {
    SecurityLogOnly,
    RiskIncident,
    LawViolation,
    RiskIncidentAndLawViolation
}

public enum ShopSecuritySourceAction {
    PreviewEvaluation,
    EvaluateSecurity,
    ClearBasket
}

[CreateAssetMenu(menuName = "Shops/Security Policy Definition")]
public class ShopSecurityPolicyDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this security policy. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in debug/future shop security UI. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note or player-facing explanation of what this security policy checks.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Optional icon used by future security, shop or warning UI.")]
    [SerializeField] Sprite icon;
    [Tooltip("Free-form tags such as mart, mall, theft, guard, camera, self-checkout or region name.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Catalog Filters")]
    [Tooltip("If assigned, this security policy only evaluates this exact shop catalog.")]
    [SerializeField] ShopCatalogDefinition requiredCatalog;
    [Tooltip("If not empty, this security policy only evaluates these catalog types.")]
    [SerializeField] List<ShopCatalogType> allowedCatalogTypes = new List<ShopCatalogType>();
    [Tooltip("Catalog tags required before this policy can evaluate. Empty means no catalog tag filter.")]
    [SerializeField] List<string> requiredCatalogTags = new List<string>();
    [Tooltip("How required catalog tags are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode catalogTagMatchMode = ConsequenceRequirementMatchMode.All;

    [Header("Basket Detection")]
    [Tooltip("If enabled, an active basket must belong to the evaluated shop id before this policy can trigger.")]
    [SerializeField] bool requireMatchingShop = true;
    [Tooltip("If enabled, the policy can only trigger when the player has an active basket.")]
    [SerializeField] bool requireActiveBasket = true;
    [Tooltip("If enabled, the policy can only trigger when the active basket has at least one line.")]
    [SerializeField] bool requireBasketLines = true;
    [Tooltip("Minimum unpaid basket value required to trigger. 0 means any value can trigger.")]
    [Min(0f)]
    [SerializeField] float minimumUnpaidValue = 1f;
    [Tooltip("Minimum basket line count required to trigger. 0 means no line-count threshold.")]
    [Min(0)]
    [SerializeField] int minimumLineCount = 1;
    [Tooltip("Minimum basket bundle count required to trigger. 0 means no bundle-count threshold.")]
    [Min(0)]
    [SerializeField] int minimumBundleCount = 1;
    [Tooltip("If enabled, the policy only triggers when the unpaid basket value is higher than the player's current Wallet funds.")]
    [SerializeField] bool requireBasketValueExceedsWallet;

    [Header("Response")]
    [Tooltip("If enabled, triggered evaluations mark the source as blocking exit. Portal/door code can read the result and stop movement.")]
    [SerializeField] bool blockExitWhenTriggered = true;
    [Tooltip("If enabled, the active basket is cleared after a triggered evaluation is recorded.")]
    [SerializeField] bool clearBasketWhenTriggered;
    [Tooltip("How this policy connects to Risk and Law systems when triggered.")]
    [SerializeField] ShopSecurityConsequenceMode consequenceMode = ShopSecurityConsequenceMode.RiskIncident;
    [Tooltip("Risk incident applied when Consequence Mode includes Risk Incident.")]
    [SerializeField] RiskIncidentDefinition riskIncident;
    [Tooltip("Law violation applied when Consequence Mode includes Law Violation.")]
    [SerializeField] LawViolationDefinition lawViolation;
    [Tooltip("If enabled, the assigned risk incident applies its configured reputation, milestone, title and linked-law consequences.")]
    [SerializeField] bool applyRiskConsequences = true;
    [Tooltip("If enabled, the assigned law violation applies its configured reputation, milestone and title consequences.")]
    [SerializeField] bool applyLawConsequences = true;
    [Tooltip("Reporter id written into Risk and Law logs. Empty uses the current shop id or shop-security.")]
    [SerializeField] string reporterId = string.Empty;
    [Tooltip("Optional region override written into Risk logs. Empty uses the risk incident's default region.")]
    [SerializeField] RegionInfoDefinition riskRegionOverride;
    [Tooltip("Message used when this policy triggers. Empty generates a default message.")]
    [TextArea]
    [SerializeField] string triggeredMessage = "Security noticed unpaid shop items.";
    [Tooltip("Message used when this policy evaluates cleanly. Empty generates a default message.")]
    [TextArea]
    [SerializeField] string cleanMessage = "No unpaid shop items were detected.";

    [Header("Access")]
    [Tooltip("Optional title, badge, permit or license required before this security policy can evaluate.")]
    [SerializeField] TitleDefinition requiredTitle;
    [Tooltip("Optional milestone required before this security policy can evaluate.")]
    [SerializeField] MilestoneDefinition requiredMilestone;
    [Tooltip("Optional faction whose reputation gates this security policy.")]
    [SerializeField] ReputationFactionDefinition requiredFaction;
    [Tooltip("Minimum required reputation with the selected faction.")]
    [SerializeField] int requiredReputation;
    [Tooltip("Optional world event whose active state gates this security policy.")]
    [SerializeField] WorldEventDefinition requiredWorldEvent;
    [Tooltip("Expected active state for Required World Event.")]
    [SerializeField] bool requiredWorldEventActive = true;
    [Tooltip("How extra requirements are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode requirementMatchMode = ConsequenceRequirementMatchMode.All;
    [Tooltip("Extra activity-style requirements checked before this security policy can evaluate.")]
    [SerializeField] List<ActivityRequirement> requirements = new List<ActivityRequirement>();
    [Tooltip("Message/debug reason used when this security policy is locked.")]
    [TextArea]
    [SerializeField] string lockedMessage = "This shop security policy is not available.";

    [Header("Events")]
    [Tooltip("Optional event published when this policy triggers.")]
    [SerializeField] GameEventDefinition triggeredEvent;
    [Tooltip("Optional event published when this policy evaluates cleanly and Publish Clean Evaluations is enabled.")]
    [SerializeField] GameEventDefinition cleanEvent;
    [Tooltip("Optional event published when this policy cannot evaluate because setup or requirements are blocked.")]
    [SerializeField] GameEventDefinition blockedEvent;
    [Tooltip("If enabled, clean evaluations also publish events. Triggered and blocked evaluations always publish through their configured options.")]
    [SerializeField] bool publishCleanEvaluations;
    [Tooltip("If enabled, security events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed = true;
    [Tooltip("If enabled, security events are written to GameDebugLogger.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public ShopCatalogDefinition RequiredCatalog => requiredCatalog;
    public IReadOnlyList<ShopCatalogType> AllowedCatalogTypes => allowedCatalogTypes != null ? (IReadOnlyList<ShopCatalogType>)allowedCatalogTypes : Array.Empty<ShopCatalogType>();
    public IReadOnlyList<string> RequiredCatalogTags => requiredCatalogTags != null ? (IReadOnlyList<string>)requiredCatalogTags : Array.Empty<string>();
    public bool RequireMatchingShop => requireMatchingShop;
    public bool RequireActiveBasket => requireActiveBasket;
    public bool RequireBasketLines => requireBasketLines;
    public float MinimumUnpaidValue => Mathf.Max(0f, minimumUnpaidValue);
    public int MinimumLineCount => Mathf.Max(0, minimumLineCount);
    public int MinimumBundleCount => Mathf.Max(0, minimumBundleCount);
    public bool RequireBasketValueExceedsWallet => requireBasketValueExceedsWallet;
    public bool BlockExitWhenTriggered => blockExitWhenTriggered;
    public bool ClearBasketWhenTriggered => clearBasketWhenTriggered;
    public ShopSecurityConsequenceMode ConsequenceMode => consequenceMode;
    public RiskIncidentDefinition RiskIncident => riskIncident;
    public LawViolationDefinition LawViolation => lawViolation;
    public bool ApplyRiskConsequences => applyRiskConsequences;
    public bool ApplyLawConsequences => applyLawConsequences;
    public IReadOnlyList<ActivityRequirement> Requirements => requirements != null ? (IReadOnlyList<ActivityRequirement>)requirements : Array.Empty<ActivityRequirement>();

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    public bool TryEvaluate(
        PlayerController player,
        ShopCatalog shop,
        PlayerShopBasketLog basketLog,
        string sourceId,
        ShopSecurityIncidentKind incidentKind,
        bool applyConsequences,
        out ShopSecurityEvaluationResult result,
        out string failureMessage
    ) {
        result = CreateBaseResult(shop, sourceId, incidentKind);
        if(!CanEvaluate(player, shop, basketLog, out failureMessage)) {
            result.message = failureMessage;
            PublishSecurityEvent(blockedEvent, "blocked", result, player, shop, sourceId, GameEventImportance.Warning, failureMessage);
            return false;
        }

        PopulateBasketSnapshot(result, shop, basketLog, out failureMessage);
        if(!string.IsNullOrWhiteSpace(failureMessage)) {
            result.message = failureMessage;
            PublishSecurityEvent(blockedEvent, "blocked", result, player, shop, sourceId, GameEventImportance.Warning, failureMessage);
            return false;
        }

        if(!ShouldTrigger(result, basketLog, out string cleanReason)) {
            result.triggered = false;
            result.message = cleanReason;
            if(publishCleanEvaluations) {
                PublishSecurityEvent(cleanEvent, "clean", result, player, shop, sourceId, GameEventImportance.Trace, cleanReason);
            }
            return true;
        }

        result.triggered = true;
        result.blockedExit = blockExitWhenTriggered;
        result.consequenceMode = consequenceMode;
        result.message = string.IsNullOrWhiteSpace(triggeredMessage) ? $"{DisplayName} triggered." : triggeredMessage;

        if(applyConsequences) {
            ApplyConsequences(player, shop, basketLog, sourceId, result);
        }

        PublishSecurityEvent(triggeredEvent, "triggered", result, player, shop, sourceId, GameEventImportance.Warning, null);
        return true;
    }

    public bool CanEvaluate(PlayerController player, ShopCatalog shop, PlayerShopBasketLog basketLog, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required for shop security evaluation.";
            return false;
        }

        if(shop == null && (requiredCatalog != null || allowedCatalogTypes.Count > 0 || requiredCatalogTags.Count > 0 || requireMatchingShop)) {
            failureMessage = "A shop is required for this shop security policy.";
            return false;
        }

        if(shop != null && shop.Catalog == null) {
            failureMessage = "The assigned shop has no catalog.";
            return false;
        }

        if(requiredCatalog != null && shop != null && shop.Catalog != requiredCatalog) {
            failureMessage = $"{DisplayName} cannot evaluate this shop.";
            return false;
        }

        if(allowedCatalogTypes.Count > 0 && shop != null && !allowedCatalogTypes.Contains(shop.Catalog.CatalogType)) {
            failureMessage = $"{DisplayName} cannot evaluate this shop type.";
            return false;
        }

        if(shop != null && !MatchesTags(requiredCatalogTags, catalogTagMatchMode, tag => shop.Catalog.HasTag(tag))) {
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

        if(requireActiveBasket && (basketLog == null || !basketLog.HasActiveBasket)) {
            failureMessage = null;
            return true;
        }

        failureMessage = null;
        return true;
    }

    ShopSecurityEvaluationResult CreateBaseResult(ShopCatalog shop, string sourceId, ShopSecurityIncidentKind incidentKind) {
        return new ShopSecurityEvaluationResult {
            policyId = Id,
            policyName = DisplayName,
            shopId = shop != null ? shop.ShopId : string.Empty,
            catalogId = shop != null && shop.Catalog != null ? shop.Catalog.Id : string.Empty,
            shopName = shop != null && shop.Catalog != null ? shop.Catalog.DisplayName : string.Empty,
            sourceId = string.IsNullOrWhiteSpace(sourceId) ? Id : sourceId,
            incidentKind = incidentKind,
            consequenceMode = consequenceMode,
            day = GetCurrentDay(),
            absoluteHour = GetCurrentAbsoluteHour()
        };
    }

    void PopulateBasketSnapshot(ShopSecurityEvaluationResult result, ShopCatalog shop, PlayerShopBasketLog basketLog, out string failureMessage) {
        failureMessage = null;
        if(result == null || basketLog == null || !basketLog.HasActiveBasket) {
            return;
        }

        var basket = basketLog.ActiveBasket;
        result.basketShopId = basket.shopId;
        result.basketShopName = basket.shopName;
        result.lineCount = basketLog.GetLineCount();
        result.bundleCount = basketLog.GetBundleCount();

        if(requireMatchingShop && shop != null && basket.shopId != shop.ShopId) {
            result.message = $"Basket belongs to {basket.shopName}, not {shop.Catalog.DisplayName}.";
            return;
        }

        result.unpaidValue = shop != null && (!requireMatchingShop || basket.shopId == shop.ShopId)
            ? basketLog.GetCurrentTotal(shop, out failureMessage)
            : basketLog.GetSnapshotTotal();
    }

    bool ShouldTrigger(ShopSecurityEvaluationResult result, PlayerShopBasketLog basketLog, out string cleanReason) {
        if(result == null) {
            cleanReason = "No security result was created.";
            return false;
        }

        if(requireActiveBasket && (basketLog == null || !basketLog.HasActiveBasket)) {
            cleanReason = string.IsNullOrWhiteSpace(cleanMessage) ? "No active shop basket." : cleanMessage;
            return false;
        }

        if(requireBasketLines && result.lineCount <= 0) {
            cleanReason = string.IsNullOrWhiteSpace(cleanMessage) ? "The active basket is empty." : cleanMessage;
            return false;
        }

        if(MinimumLineCount > 0 && result.lineCount < MinimumLineCount) {
            cleanReason = string.IsNullOrWhiteSpace(cleanMessage) ? $"Basket has fewer than {MinimumLineCount} line(s)." : cleanMessage;
            return false;
        }

        if(MinimumBundleCount > 0 && result.bundleCount < MinimumBundleCount) {
            cleanReason = string.IsNullOrWhiteSpace(cleanMessage) ? $"Basket has fewer than {MinimumBundleCount} bundle(s)." : cleanMessage;
            return false;
        }

        if(MinimumUnpaidValue > 0f && result.unpaidValue < MinimumUnpaidValue) {
            cleanReason = string.IsNullOrWhiteSpace(cleanMessage) ? $"Unpaid value is below {MinimumUnpaidValue:0}." : cleanMessage;
            return false;
        }

        if(requireBasketValueExceedsWallet && Wallet.i != null && Wallet.i.HasMoney(result.unpaidValue)) {
            cleanReason = string.IsNullOrWhiteSpace(cleanMessage) ? "Player can afford the basket value." : cleanMessage;
            return false;
        }

        cleanReason = null;
        return true;
    }

    void ApplyConsequences(PlayerController player, ShopCatalog shop, PlayerShopBasketLog basketLog, string sourceId, ShopSecurityEvaluationResult result) {
        if(player == null || result == null) {
            return;
        }

        string resolvedSourceId = string.IsNullOrWhiteSpace(sourceId) ? Id : sourceId;
        string resolvedReporterId = ResolveReporterId(shop);

        if(ConsequenceModeIncludesRisk() && riskIncident != null) {
            var riskRecord = riskIncident.Apply(
                player,
                resolvedSourceId,
                resolvedReporterId,
                riskRegionOverride,
                null,
                null,
                applyRiskConsequences,
                this);
            result.riskIncidentId = riskIncident.Id;
            result.riskIncidentName = riskIncident.DisplayName;
            result.riskRecordId = riskRecord != null ? riskRecord.recordId : string.Empty;
        }

        if(ConsequenceModeIncludesLaw() && lawViolation != null) {
            var lawLog = player.GetComponent<PlayerLawLog>() ?? player.gameObject.AddComponent<PlayerLawLog>();
            var lawIncident = lawLog.RecordViolation(lawViolation, resolvedSourceId, resolvedReporterId, applyLawConsequences, this);
            result.lawViolationId = lawViolation.Id;
            result.lawViolationName = lawViolation.DisplayName;
            result.lawIncidentId = lawIncident != null ? lawIncident.incidentId : string.Empty;
        }

        if(clearBasketWhenTriggered && basketLog != null && basketLog.ClearBasket(resolvedSourceId)) {
            result.basketCleared = true;
        }

        var securityLog = player.GetComponent<PlayerShopSecurityLog>() ?? player.gameObject.AddComponent<PlayerShopSecurityLog>();
        var record = securityLog.RecordIncident(this, shop, result, resolvedSourceId);
        result.securityRecordId = record != null ? record.recordId : string.Empty;
    }

    bool ConsequenceModeIncludesRisk() {
        return consequenceMode == ShopSecurityConsequenceMode.RiskIncident
            || consequenceMode == ShopSecurityConsequenceMode.RiskIncidentAndLawViolation;
    }

    bool ConsequenceModeIncludesLaw() {
        return consequenceMode == ShopSecurityConsequenceMode.LawViolation
            || consequenceMode == ShopSecurityConsequenceMode.RiskIncidentAndLawViolation;
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

    string ResolveReporterId(ShopCatalog shop) {
        if(!string.IsNullOrWhiteSpace(reporterId)) {
            return reporterId;
        }

        return shop != null ? shop.ShopId : "shop-security";
    }

    void PublishSecurityEvent(GameEventDefinition eventDefinition, string phase, ShopSecurityEvaluationResult result, PlayerController player, ShopCatalog shop, string sourceId, GameEventImportance importance, string failureMessage) {
        GameEventPublishing.PublishOptional(
            eventDefinition,
            $"shop.security.{phase}.{Id}.{shop?.ShopId ?? "shop"}",
            !string.IsNullOrWhiteSpace(failureMessage) ? failureMessage : result != null && !string.IsNullOrWhiteSpace(result.message) ? result.message : $"{DisplayName} security {phase}.",
            GameEventCategory.Shop,
            importance,
            player != null ? player : this,
            "ShopSecurityPolicyDefinition",
            GameEventScope.Player,
            showInFeed: showEventsInFeed,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("securityPolicyId", Id),
            GameEventPublishing.Value("securityPolicyName", DisplayName),
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("shopId", shop != null ? shop.ShopId : string.Empty),
            GameEventPublishing.Value("sourceId", sourceId),
            GameEventPublishing.Value("incidentKind", result != null ? result.incidentKind : ShopSecurityIncidentKind.Custom),
            GameEventPublishing.Value("triggered", result != null && result.triggered),
            GameEventPublishing.Value("blockedExit", result != null && result.blockedExit),
            GameEventPublishing.Value("unpaidValue", result != null ? result.unpaidValue : 0f),
            GameEventPublishing.Value("lineCount", result != null ? result.lineCount : 0),
            GameEventPublishing.Value("bundleCount", result != null ? result.bundleCount : 0),
            GameEventPublishing.Value("riskIncidentId", result != null ? result.riskIncidentId : string.Empty),
            GameEventPublishing.Value("lawViolationId", result != null ? result.lawViolationId : string.Empty));
    }

    int GetCurrentDay() {
        return TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }
}

public class ShopSecuritySource : MonoBehaviour, IPlayerTriggerable, Interactable {
    [Header("Identity")]
    [Tooltip("Stable source id written into security records. Empty uses policy id or this GameObject name.")]
    [SerializeField] string sourceId = string.Empty;
    [Tooltip("Readable source name for debug/future UI. Empty uses policy display name or this GameObject name.")]
    [SerializeField] string displayName = string.Empty;

    [Header("Security")]
    [Tooltip("Shop whose active basket is checked by this security source.")]
    [SerializeField] ShopCatalog shop;
    [Tooltip("Security policy that evaluates unpaid basket behavior.")]
    [SerializeField] ShopSecurityPolicyDefinition securityPolicy;
    [Tooltip("Optional explicit player. Empty uses the triggering/interacting player or PlayerController.i.")]
    [SerializeField] PlayerController playerOverride;
    [Tooltip("Incident kind written into security records when this source evaluates.")]
    [SerializeField] ShopSecurityIncidentKind incidentKind = ShopSecurityIncidentKind.UnpaidBasketExit;
    [Tooltip("Action applied when this source is triggered.")]
    [SerializeField] ShopSecuritySourceAction triggerAction = ShopSecuritySourceAction.EvaluateSecurity;
    [Tooltip("Action applied when an Interactable flow calls Interact.")]
    [SerializeField] ShopSecuritySourceAction interactAction = ShopSecuritySourceAction.EvaluateSecurity;
    [Tooltip("If enabled, player trigger applies Trigger Action.")]
    [SerializeField] bool applyOnPlayerTrigger = true;
    [Tooltip("If enabled, this trigger can be called repeatedly by the player.")]
    [SerializeField] bool triggerRepeatedly = true;

    [Header("Feedback")]
    [Tooltip("If enabled, result text is shown through the existing DialogManager when available.")]
    [SerializeField] bool showDialogFeedback;
    [Tooltip("If enabled, blocked security actions are written to GameDebug.")]
    [SerializeField] bool logBlockedAttempts = true;
    [Tooltip("If enabled, successful security actions are written to GameDebug.")]
    [SerializeField] bool logSuccessfulAttempts;

    public bool TriggerRepeatedly => triggerRepeatedly;
    public string SourceId => ResolveSourceId();
    public string DisplayName => !string.IsNullOrWhiteSpace(displayName) ? displayName : securityPolicy != null ? securityPolicy.DisplayName : gameObject.name;
    public ShopCatalog Shop => shop;
    public ShopSecurityPolicyDefinition SecurityPolicy => securityPolicy;
    public ShopSecuritySourceAction TriggerAction => triggerAction;
    public ShopSecuritySourceAction InteractAction => interactAction;
    public ShopSecurityEvaluationResult LastResult { get; private set; }
    public bool LastEvaluationBlockedExit => LastResult != null && LastResult.blockedExit;

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

    public bool TryPreview(PlayerController player, out ShopSecurityEvaluationResult result, out string failureMessage) {
        return TryEvaluateInternal(player, applyConsequences: false, out result, out failureMessage);
    }

    public bool TryEvaluate(PlayerController player, out ShopSecurityEvaluationResult result, out string failureMessage) {
        return TryEvaluateInternal(player, applyConsequences: true, out result, out failureMessage);
    }

    public bool TryEvaluateAndShouldBlockExit(PlayerController player, out bool shouldBlockExit, out ShopSecurityEvaluationResult result, out string failureMessage) {
        bool success = TryEvaluate(player, out result, out failureMessage);
        shouldBlockExit = success && result != null && result.blockedExit;
        return success;
    }

    bool TryEvaluateInternal(PlayerController player, bool applyConsequences, out ShopSecurityEvaluationResult result, out string failureMessage) {
        player = ResolvePlayer(player);
        result = null;
        if(player == null) {
            failureMessage = "A player is required for shop security.";
            RecordBlocked(player, failureMessage);
            return false;
        }

        if(securityPolicy == null) {
            failureMessage = "Shop security source has no policy assigned.";
            RecordBlocked(player, failureMessage);
            return false;
        }

        var basketLog = player.GetComponent<PlayerShopBasketLog>();
        if(!securityPolicy.TryEvaluate(player, shop, basketLog, ResolveSourceId(), incidentKind, applyConsequences, out result, out failureMessage)) {
            LastResult = result;
            RecordBlocked(player, failureMessage);
            return false;
        }

        LastResult = result;
        if(logSuccessfulAttempts) {
            string state = result != null && result.triggered ? "triggered" : "clean";
            GameDebug.Success($"{DisplayName} security evaluation {state}.", GameDebugCategory.Shop, this, "ShopSecuritySource");
        }

        return true;
    }

    bool ApplyAction(ShopSecuritySourceAction action, PlayerController player, out string feedback) {
        feedback = null;
        switch(action) {
            case ShopSecuritySourceAction.PreviewEvaluation:
                if(TryPreview(player, out var preview, out feedback)) {
                    feedback = preview != null ? preview.BuildSummary() : "Security preview ready.";
                    return true;
                }
                return false;
            case ShopSecuritySourceAction.ClearBasket:
                var log = player != null ? player.GetComponent<PlayerShopBasketLog>() : null;
                if(log != null && log.ClearBasket(ResolveSourceId())) {
                    feedback = "Basket cleared.";
                    return true;
                }
                feedback = "No basket was cleared.";
                RecordBlocked(player, feedback);
                return false;
            default:
                if(TryEvaluate(player, out var result, out feedback)) {
                    feedback = result != null ? result.BuildSummary() : "Security evaluation completed.";
                    return true;
                }
                return false;
        }
    }

    void RecordBlocked(PlayerController player, string failureMessage) {
        if(logBlockedAttempts && !string.IsNullOrWhiteSpace(failureMessage)) {
            GameDebug.Warning(failureMessage, GameDebugCategory.Shop, player != null ? player : this, "ShopSecuritySource");
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

        if(securityPolicy != null) {
            return $"security:{securityPolicy.Id}";
        }

        return gameObject.name;
    }
}

public class PlayerShopSecurityLog : MonoBehaviour, ISavable {
    [Tooltip("Maximum security records kept in memory/save data. Older records are trimmed first. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxRecords = 200;
    [Tooltip("Runtime/save history of shop security incidents.")]
    [SerializeField] List<ShopSecurityIncidentRecord> records = new List<ShopSecurityIncidentRecord>();

    public IReadOnlyList<ShopSecurityIncidentRecord> Records => records;
    public event Action<ShopSecurityIncidentRecord> OnSecurityIncidentRecorded;

    public ShopSecurityIncidentRecord RecordIncident(ShopSecurityPolicyDefinition policy, ShopCatalog shop, ShopSecurityEvaluationResult result, string sourceId) {
        if(policy == null || result == null) {
            return null;
        }

        var record = new ShopSecurityIncidentRecord(result) {
            recordId = Guid.NewGuid().ToString("N"),
            policyId = policy.Id,
            policyName = policy.DisplayName,
            shopId = shop != null ? shop.ShopId : result.shopId,
            catalogId = shop != null && shop.Catalog != null ? shop.Catalog.Id : result.catalogId,
            shopName = shop != null && shop.Catalog != null ? shop.Catalog.DisplayName : result.shopName,
            sourceId = string.IsNullOrWhiteSpace(sourceId) ? result.sourceId : sourceId
        };

        records.Add(record);
        TrimHistory();
        OnSecurityIncidentRecorded?.Invoke(record);
        return record;
    }

    public int GetIncidentCount(ShopSecurityPolicyDefinition policy = null, string shopId = null, ShopSecurityIncidentKind? incidentKind = null) {
        return records.Count(record => record != null
            && (policy == null || record.policyId == policy.Id)
            && (string.IsNullOrWhiteSpace(shopId) || record.shopId == shopId)
            && (!incidentKind.HasValue || record.incidentKind == incidentKind.Value));
    }

    public ShopSecurityIncidentRecord GetLatestRecord(string shopId = null) {
        return records
            .Where(record => record != null && (string.IsNullOrWhiteSpace(shopId) || record.shopId == shopId))
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
        return new PlayerShopSecurityLogSaveData {
            records = records.Where(record => record != null).Select(record => record.Clone()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerShopSecurityLogSaveData;
        records = saveData?.records?.Where(record => record != null).Select(record => record.Clone()).ToList()
            ?? new List<ShopSecurityIncidentRecord>();
        TrimHistory();
    }
}

[Serializable]
public class ShopSecurityEvaluationResult {
    [Tooltip("Security record id created when this evaluation is saved to PlayerShopSecurityLog.")]
    public string securityRecordId;
    [Tooltip("Security policy id that produced this result.")]
    public string policyId;
    [Tooltip("Security policy display name that produced this result.")]
    public string policyName;
    [Tooltip("Shop instance id evaluated by this result.")]
    public string shopId;
    [Tooltip("Catalog definition id evaluated by this result.")]
    public string catalogId;
    [Tooltip("Shop display name copied for fallback/debug output.")]
    public string shopName;
    [Tooltip("Source id that requested this security evaluation.")]
    public string sourceId;
    [Tooltip("Kind of security incident represented by this result.")]
    public ShopSecurityIncidentKind incidentKind;
    [Tooltip("Consequence mode used by this result.")]
    public ShopSecurityConsequenceMode consequenceMode;
    [Tooltip("If enabled, the policy detected an incident.")]
    public bool triggered;
    [Tooltip("If enabled, the source/caller should block exit or movement.")]
    public bool blockedExit;
    [Tooltip("If enabled, the active basket was cleared by this evaluation.")]
    public bool basketCleared;
    [Tooltip("Shop id copied from the active basket.")]
    public string basketShopId;
    [Tooltip("Shop name copied from the active basket.")]
    public string basketShopName;
    [Tooltip("Current or snapshot unpaid basket value.")]
    public float unpaidValue;
    [Tooltip("Basket line count at evaluation time.")]
    public int lineCount;
    [Tooltip("Basket bundle count at evaluation time.")]
    public int bundleCount;
    [Tooltip("Risk incident id applied by this result.")]
    public string riskIncidentId;
    [Tooltip("Risk incident display name applied by this result.")]
    public string riskIncidentName;
    [Tooltip("Runtime PlayerRiskLog record id created by this result.")]
    public string riskRecordId;
    [Tooltip("Law violation id applied by this result.")]
    public string lawViolationId;
    [Tooltip("Law violation display name applied by this result.")]
    public string lawViolationName;
    [Tooltip("Runtime PlayerLawLog incident id created by this result.")]
    public string lawIncidentId;
    [Tooltip("Readable evaluation message.")]
    public string message;
    [Tooltip("In-game day when this evaluation happened.")]
    public int day;
    [Tooltip("Absolute in-game hour when this evaluation happened.")]
    public int absoluteHour;

    public string BuildSummary() {
        string state = triggered ? blockedExit ? "triggered and blocked exit" : "triggered" : "clean";
        return $"{policyName}: {state}, value {unpaidValue:0}, lines {lineCount}, bundles {bundleCount}.";
    }
}

[Serializable]
public class ShopSecurityIncidentRecord {
    [Tooltip("Unique runtime/save id for this security incident.")]
    public string recordId;
    [Tooltip("Security policy id that produced this record.")]
    public string policyId;
    [Tooltip("Security policy display name copied for fallback/debug output.")]
    public string policyName;
    [Tooltip("Shop instance id evaluated by this record.")]
    public string shopId;
    [Tooltip("Catalog definition id evaluated by this record.")]
    public string catalogId;
    [Tooltip("Shop display name copied for fallback/debug output.")]
    public string shopName;
    [Tooltip("Source id that requested this security evaluation.")]
    public string sourceId;
    [Tooltip("Kind of security incident represented by this record.")]
    public ShopSecurityIncidentKind incidentKind;
    [Tooltip("Consequence mode used by this record.")]
    public ShopSecurityConsequenceMode consequenceMode;
    [Tooltip("If enabled, this record came from a triggered evaluation.")]
    public bool triggered;
    [Tooltip("If enabled, the related source/caller should block exit or movement.")]
    public bool blockedExit;
    [Tooltip("If enabled, the active basket was cleared by this evaluation.")]
    public bool basketCleared;
    [Tooltip("Shop id copied from the active basket.")]
    public string basketShopId;
    [Tooltip("Shop name copied from the active basket.")]
    public string basketShopName;
    [Tooltip("Current or snapshot unpaid basket value.")]
    public float unpaidValue;
    [Tooltip("Basket line count at evaluation time.")]
    public int lineCount;
    [Tooltip("Basket bundle count at evaluation time.")]
    public int bundleCount;
    [Tooltip("Risk incident id applied by this record.")]
    public string riskIncidentId;
    [Tooltip("Risk incident display name applied by this record.")]
    public string riskIncidentName;
    [Tooltip("Runtime PlayerRiskLog record id created by this record.")]
    public string riskRecordId;
    [Tooltip("Law violation id applied by this record.")]
    public string lawViolationId;
    [Tooltip("Law violation display name applied by this record.")]
    public string lawViolationName;
    [Tooltip("Runtime PlayerLawLog incident id created by this record.")]
    public string lawIncidentId;
    [Tooltip("Readable evaluation message.")]
    public string message;
    [Tooltip("In-game day when this record was created.")]
    public int day;
    [Tooltip("Absolute in-game hour when this record was created.")]
    public int absoluteHour;

    public ShopSecurityIncidentRecord() {
    }

    public ShopSecurityIncidentRecord(ShopSecurityEvaluationResult result) {
        if(result == null) {
            return;
        }

        CopyFrom(result);
    }

    void CopyFrom(ShopSecurityEvaluationResult result) {
        policyId = result.policyId;
        policyName = result.policyName;
        shopId = result.shopId;
        catalogId = result.catalogId;
        shopName = result.shopName;
        sourceId = result.sourceId;
        incidentKind = result.incidentKind;
        consequenceMode = result.consequenceMode;
        triggered = result.triggered;
        blockedExit = result.blockedExit;
        basketCleared = result.basketCleared;
        basketShopId = result.basketShopId;
        basketShopName = result.basketShopName;
        unpaidValue = result.unpaidValue;
        lineCount = result.lineCount;
        bundleCount = result.bundleCount;
        riskIncidentId = result.riskIncidentId;
        riskIncidentName = result.riskIncidentName;
        riskRecordId = result.riskRecordId;
        lawViolationId = result.lawViolationId;
        lawViolationName = result.lawViolationName;
        lawIncidentId = result.lawIncidentId;
        message = result.message;
        day = result.day;
        absoluteHour = result.absoluteHour;
    }

    public ShopSecurityIncidentRecord Clone() {
        return new ShopSecurityIncidentRecord {
            recordId = recordId,
            policyId = policyId,
            policyName = policyName,
            shopId = shopId,
            catalogId = catalogId,
            shopName = shopName,
            sourceId = sourceId,
            incidentKind = incidentKind,
            consequenceMode = consequenceMode,
            triggered = triggered,
            blockedExit = blockedExit,
            basketCleared = basketCleared,
            basketShopId = basketShopId,
            basketShopName = basketShopName,
            unpaidValue = unpaidValue,
            lineCount = lineCount,
            bundleCount = bundleCount,
            riskIncidentId = riskIncidentId,
            riskIncidentName = riskIncidentName,
            riskRecordId = riskRecordId,
            lawViolationId = lawViolationId,
            lawViolationName = lawViolationName,
            lawIncidentId = lawIncidentId,
            message = message,
            day = day,
            absoluteHour = absoluteHour
        };
    }
}

[Serializable]
public class PlayerShopSecurityLogSaveData {
    [Tooltip("Saved shop security records.")]
    public List<ShopSecurityIncidentRecord> records;
}
