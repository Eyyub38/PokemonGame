using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public enum ContentAuditScanScope {
    Resources,
    EditorProjectAssets,
    LoadedSceneComponents
}

public enum ContentAuditAssetType {
    AnyScriptableObject,
    CustomUnityObjectType,
    Activity,
    ActivityZone,
    ActivityPermission,
    ActivityOutcome,
    Recipe,
    CraftingStation,
    ItemBrand,
    ItemModel,
    ShopCatalog,
    ShopBasketSource,
    ShopShelf,
    ShopShelfSource,
    ShopPaymentRule,
    ShopCheckoutTerminal,
    ShopReturnPolicy,
    ShopRefundSource,
    PlayerShopReceiptLog,
    ShopSecurityPolicy,
    ShopSecuritySource,
    PlayerShopSecurityLog,
    ShopRestockSchedule,
    ShopRestockSource,
    PlayerShopRestockLog,
    ShopDeliveryService,
    ShopDeliverySource,
    PlayerDeliveryLog,
    LearnableOffer,
    LearnableOfferSource,
    LoyaltyProgram,
    LoyaltyProgramSource,
    EncounterTable,
    EncounterSourceProfile,
    EncounterSource,
    StealthCaptureProfile,
    CustomizationPart,
    CustomizationPreset,
    PlayerOrigin,
    PlayerLifestyle,
    NewGameSetup,
    NewGameSetupSource,
    PlayerNewGameSetupLog,
    PokedexEntry,
    RegionInfo,
    PokeNavEntry,
    PokeNavGuideSection,
    PokeNavFeedItem,
    PokeNavFeedSource,
    SocialPost,
    MapMarker,
    MapViewProfile,
    MapDiscoverySource,
    Rumor,
    RumorSpreadProfile,
    WorldCondition,
    RiskIncident,
    ConsequenceChain,
    WorldTrigger,
    SceneObject,
    SceneSpawnProfile,
    WorldDiscovery,
    LocationVisit,
    ChronicleEntry,
    ChronicleCaptureRule,
    NavigationHint,
    AreaProfile,
    CalendarEvent,
    BattleAIProfile,
    BattleRuleSet,
    BattleChallenge,
    Contest,
    CareerPath,
    Organization,
    Assignment,
    AccessProfile,
    LawViolation,
    InvestigationCase,
    InvestigationClue,
    NPCMemoryTopic,
    NPCReaction,
    WitnessReport,
    ReportPropagation,
    CompanionRole,
    CompanionPerk,
    CompanionExpedition,
    CompanionExpeditionRoute,
    NPCVisualSet,
    TrainerPartyTemplate,
    NPCVariantPool,
    NPCSceneRandomizationProfile,
    NPCSceneRandomizationSlot,
    NPCSceneRandomizationController,
    Job,
    JobBoard,
    TransitRoute,
    TransitStop,
    Farmable,
    ResourceNode,
    ResearchSubject,
    PokemonCareAction,
    PokemonCareNeed,
    PokemonCareFacility,
    PokemonAbilityTree,
    PokemonAbilityTreeSource,
    Tool,
    SurvivalNeed,
    Milestone,
    Title,
    RelationshipSubject,
    ReputationFaction,
    PokemonMood,
    NPCSchedule,
    GameEvent,
    Notification,
    AssetAuditProfile,
    AssetAuditRunner,
    MarketServiceUIManager,
    PokeNavMapUIManager,
    ConditionalDialog,
    SpeechBubbleStyle,
    Service,
    ServicePackage,
    ServicePackageSource,
    ServiceAppointment,
    ServiceAppointmentSource,
    PlayerServiceAppointmentLog,
    WorldRegion,
    RegionTravelRoute,
    RegionChallengeProfile,
    RegionTravelPoint,
    RidePokemon,
    RidePoint,
    PowerMechanic,
    Competition,
    CompetitionRanking,
    CompetitionHonor,
    CompetitionSeason,
    CompetitionEntrant,
    CompetitionRoster,
    CompetitionBracketSource,
    CompetitionPrizeTable,
    CompetitionVenue,
    CompetitionVenueSource,
    CompetitionInvitation,
    CompetitionInvitationSource,
    Sponsor,
    SponsorSource,
    CompetitionRegistrationWindow,
    CompetitionRegistration,
    CompetitionRegistrationSource,
    CompetitionMatchResolver,
    PokemonCareNeedsController,
    RegionTravelPolicy,
    BattleMode
}

public enum ContentAuditRuleMode {
    CountAtLeast,
    CountAtMost,
    CountExactly,
    Exists,
    None
}

public enum ContentAuditSeverity {
    Info,
    Warning,
    Error
}

[CreateAssetMenu(menuName = "Debugging/Content Audit/Profile Definition")]
public class ContentAuditProfileDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable id for this audit profile. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in debug logs. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note explaining what this profile is meant to verify.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Free-form tags such as core, map, demo, release, city, route or optional.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Rules")]
    [Tooltip("Editable audit rules. Each rule scans a content type and checks whether the expected count/filter condition is met.")]
    [SerializeField] List<ContentAuditRule> rules = new List<ContentAuditRule>();
    [Tooltip("If enabled, successful rules are included in the generated report results.")]
    [SerializeField] bool includePassedRulesInReport = true;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public IReadOnlyList<ContentAuditRule> Rules => rules != null ? (IReadOnlyList<ContentAuditRule>)rules : Array.Empty<ContentAuditRule>();
    public bool IncludePassedRulesInReport => includePassedRulesInReport;

    public ContentAuditReport Run(UnityEngine.Object context = null) {
        var report = new ContentAuditReport(Id, DisplayName, context != null ? context.name : null);

        if(rules == null || rules.Count == 0) {
            report.AddIssue(ContentAuditSeverity.Info, "No audit rules are defined.", "ContentAudit/Profile", 0, null);
            return report;
        }

        foreach(var rule in rules) {
            if(rule == null) {
                report.AddIssue(ContentAuditSeverity.Warning, "Profile contains a null rule slot.", DisplayName, 0, null);
                continue;
            }

            if(!rule.Enabled) {
                continue;
            }

            var result = rule.Evaluate();
            if(result.Passed && !includePassedRulesInReport) {
                report.passedRuleCount++;
                report.totalRuleCount++;
                continue;
            }

            report.AddResult(result);
        }

        return report;
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag) && Tags.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase));
    }
}

[Serializable]
public class ContentAuditRule {
    [Tooltip("If disabled, this rule is skipped.")]
    [SerializeField] bool enabled = true;
    [Tooltip("Stable id for this rule inside the profile. Empty uses the rule description or target type.")]
    [SerializeField] string ruleId = string.Empty;
    [Tooltip("Short note explaining what this rule protects against.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Severity used when the rule fails.")]
    [SerializeField] ContentAuditSeverity severity = ContentAuditSeverity.Warning;
    [Tooltip("Where this rule searches for content. Editor Project Assets scans the whole Assets folder only inside Unity Editor.")]
    [SerializeField] ContentAuditScanScope scanScope = ContentAuditScanScope.Resources;
    [Tooltip("Content type to scan. Use Custom Unity Object Type when the target script is not listed here.")]
    [SerializeField] ContentAuditAssetType targetType = ContentAuditAssetType.AnyScriptableObject;
    [Tooltip("Type name used by Custom Unity Object Type or Loaded Scene Components. Accepts class name or full namespace-qualified name.")]
    [SerializeField] string customTypeName = string.Empty;
    [Tooltip("How the matched asset/component count should be evaluated.")]
    [SerializeField] ContentAuditRuleMode mode = ContentAuditRuleMode.CountAtLeast;
    [Tooltip("Expected count used by Count At Least, Count At Most and Count Exactly.")]
    [Min(0)]
    [SerializeField] int threshold = 1;
    [Tooltip("Optional asset/component name filter. Empty means any name is accepted.")]
    [SerializeField] string nameContains = string.Empty;
    [Tooltip("Optional id prefix filter. Works on assets/components with a public Id property.")]
    [SerializeField] string idStartsWith = string.Empty;
    [Tooltip("Optional tag filter. Works on definitions exposing HasTag(string), Tags, or tags.")]
    [SerializeField] string requiredTag = string.Empty;
    [Tooltip("If scanning loaded scene components, inactive objects are included when this is enabled.")]
    [SerializeField] bool includeInactiveSceneObjects = true;
    [Tooltip("Optional custom failure text. Empty generates a standard message.")]
    [TextArea]
    [SerializeField] string failureMessage = string.Empty;

    public bool Enabled => enabled;
    public string RuleId => string.IsNullOrWhiteSpace(ruleId)
        ? !string.IsNullOrWhiteSpace(description) ? description : $"{targetType}/{mode}"
        : ruleId;
    public string Description => description;
    public ContentAuditSeverity Severity => severity;
    public ContentAuditScanScope ScanScope => scanScope;
    public ContentAuditAssetType TargetType => targetType;
    public string CustomTypeName => customTypeName;
    public ContentAuditRuleMode Mode => mode;
    public int Threshold => Mathf.Max(0, threshold);
    public string NameContains => nameContains;
    public string IdStartsWith => idStartsWith;
    public string RequiredTag => requiredTag;
    public bool IncludeInactiveSceneObjects => includeInactiveSceneObjects;

    public ContentAuditRuleResult Evaluate() {
        var candidates = ContentAuditAssetResolver.FindCandidates(scanScope, targetType, customTypeName, includeInactiveSceneObjects, out string scanMessage);
        var matches = candidates.Where(MatchesFilters).ToList();
        bool passed = EvaluateCount(matches.Count);
        string expected = BuildExpectedText();
        string message = passed
            ? $"{RuleId} passed. Matched {matches.Count}; expected {expected}."
            : string.IsNullOrWhiteSpace(failureMessage)
                ? $"{RuleId} failed. Matched {matches.Count}; expected {expected}."
                : failureMessage;

        if(!string.IsNullOrWhiteSpace(scanMessage)) {
            message = $"{message} {scanMessage}";
        }

        return new ContentAuditRuleResult(
            RuleId,
            description,
            severity,
            scanScope,
            targetType,
            customTypeName,
            mode,
            Threshold,
            matches.Count,
            passed,
            message,
            matches.Select(ContentAuditAssetResolver.GetReadableName).Take(8).ToList()
        );
    }

    bool MatchesFilters(UnityEngine.Object candidate) {
        if(candidate == null) {
            return false;
        }

        if(!string.IsNullOrWhiteSpace(nameContains)
            && ContentAuditAssetResolver.GetReadableName(candidate).IndexOf(nameContains, StringComparison.OrdinalIgnoreCase) < 0) {
            return false;
        }

        if(!string.IsNullOrWhiteSpace(idStartsWith)) {
            string id = ContentAuditAssetResolver.GetId(candidate);
            if(string.IsNullOrWhiteSpace(id) || !id.StartsWith(idStartsWith, StringComparison.OrdinalIgnoreCase)) {
                return false;
            }
        }

        if(!string.IsNullOrWhiteSpace(requiredTag) && !ContentAuditAssetResolver.HasTag(candidate, requiredTag)) {
            return false;
        }

        return true;
    }

    bool EvaluateCount(int count) {
        return mode switch {
            ContentAuditRuleMode.CountAtLeast => count >= Threshold,
            ContentAuditRuleMode.CountAtMost => count <= Threshold,
            ContentAuditRuleMode.CountExactly => count == Threshold,
            ContentAuditRuleMode.Exists => count > 0,
            ContentAuditRuleMode.None => count == 0,
            _ => false
        };
    }

    string BuildExpectedText() {
        return mode switch {
            ContentAuditRuleMode.CountAtLeast => $"at least {Threshold}",
            ContentAuditRuleMode.CountAtMost => $"at most {Threshold}",
            ContentAuditRuleMode.CountExactly => $"exactly {Threshold}",
            ContentAuditRuleMode.Exists => "one or more",
            ContentAuditRuleMode.None => "none",
            _ => "unknown"
        };
    }
}

[Serializable]
public class ContentAuditReport {
    [Tooltip("Id of the audit profile that produced this report.")]
    public string profileId;
    [Tooltip("Display name of the audit profile that produced this report.")]
    public string profileName;
    [Tooltip("Optional Unity object or runner that requested the audit.")]
    public string contextName;
    [Tooltip("Local timestamp when this report was generated.")]
    public string generatedAt;
    [Tooltip("Unity frame count when this report was generated.")]
    public int frame;
    [Tooltip("Number of enabled rules evaluated.")]
    public int totalRuleCount;
    [Tooltip("Number of rules that passed.")]
    public int passedRuleCount;
    [Tooltip("Number of failed info-level rules.")]
    public int infoCount;
    [Tooltip("Number of failed warning-level rules.")]
    public int warningCount;
    [Tooltip("Number of failed error-level rules.")]
    public int errorCount;
    [Tooltip("Detailed rule results and issues.")]
    public List<ContentAuditRuleResult> results = new List<ContentAuditRuleResult>();

    public bool HasErrors => errorCount > 0;
    public bool HasWarnings => warningCount > 0;

    public ContentAuditReport(string profileId, string profileName, string contextName) {
        this.profileId = profileId;
        this.profileName = profileName;
        this.contextName = contextName;
        generatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        frame = Time.frameCount;
    }

    public void AddResult(ContentAuditRuleResult result) {
        if(result == null) {
            return;
        }

        totalRuleCount++;
        if(result.Passed) {
            passedRuleCount++;
        } else {
            if(result.Severity == ContentAuditSeverity.Error) errorCount++;
            else if(result.Severity == ContentAuditSeverity.Warning) warningCount++;
            else infoCount++;
        }

        results.Add(result);
    }

    public void AddIssue(ContentAuditSeverity severity, string message, string context, int matchedCount, List<string> sampleMatches) {
        AddResult(new ContentAuditRuleResult(
            context,
            string.Empty,
            severity,
            ContentAuditScanScope.Resources,
            ContentAuditAssetType.AnyScriptableObject,
            string.Empty,
            ContentAuditRuleMode.Exists,
            0,
            matchedCount,
            false,
            message,
            sampleMatches ?? new List<string>()
        ));
    }

    public string BuildSummary() {
        return $"Content audit '{profileName}' finished. Errors={errorCount}, Warnings={warningCount}, Info={infoCount}, Passed={passedRuleCount}/{totalRuleCount}";
    }
}

[Serializable]
public class ContentAuditRuleResult {
    [Tooltip("Rule id that produced this result.")]
    public string ruleId;
    [Tooltip("Designer note copied from the rule.")]
    public string description;
    [Tooltip("Severity used when this result fails.")]
    public ContentAuditSeverity severity;
    [Tooltip("Scan source used by this rule.")]
    public ContentAuditScanScope scanScope;
    [Tooltip("Target content type used by this rule.")]
    public ContentAuditAssetType targetType;
    [Tooltip("Custom type name used by this rule, if any.")]
    public string customTypeName;
    [Tooltip("Count mode used by this rule.")]
    public ContentAuditRuleMode mode;
    [Tooltip("Configured threshold for this rule.")]
    public int threshold;
    [Tooltip("Number of assets/components matching the rule filters.")]
    public int matchedCount;
    [Tooltip("Whether this rule passed.")]
    public bool passed;
    [Tooltip("Human-readable result message.")]
    public string message;
    [Tooltip("Small sample of matching asset/component names for quick inspection.")]
    public List<string> sampleMatches;

    public bool Passed => passed;
    public ContentAuditSeverity Severity => severity;
    public string Message => message;

    public ContentAuditRuleResult(
        string ruleId,
        string description,
        ContentAuditSeverity severity,
        ContentAuditScanScope scanScope,
        ContentAuditAssetType targetType,
        string customTypeName,
        ContentAuditRuleMode mode,
        int threshold,
        int matchedCount,
        bool passed,
        string message,
        List<string> sampleMatches
    ) {
        this.ruleId = ruleId;
        this.description = description;
        this.severity = severity;
        this.scanScope = scanScope;
        this.targetType = targetType;
        this.customTypeName = customTypeName;
        this.mode = mode;
        this.threshold = threshold;
        this.matchedCount = matchedCount;
        this.passed = passed;
        this.message = message;
        this.sampleMatches = sampleMatches ?? new List<string>();
    }
}

static class ContentAuditAssetResolver {
    static readonly Dictionary<ContentAuditAssetType, Type> typeMap = new Dictionary<ContentAuditAssetType, Type>() {
        { ContentAuditAssetType.AnyScriptableObject, typeof(ScriptableObject) },
        { ContentAuditAssetType.Activity, typeof(ActivityDefinition) },
        { ContentAuditAssetType.ActivityZone, typeof(ActivityZoneDefinition) },
        { ContentAuditAssetType.ActivityPermission, typeof(ActivityPermissionDefinition) },
        { ContentAuditAssetType.ActivityOutcome, typeof(ActivityOutcomeDefinition) },
        { ContentAuditAssetType.Recipe, typeof(RecipeDefinition) },
        { ContentAuditAssetType.CraftingStation, typeof(CraftingStationDefinition) },
        { ContentAuditAssetType.ItemBrand, typeof(ItemBrandDefinition) },
        { ContentAuditAssetType.ItemModel, typeof(ItemModelDefinition) },
        { ContentAuditAssetType.ShopCatalog, typeof(ShopCatalogDefinition) },
        { ContentAuditAssetType.ShopBasketSource, typeof(ShopBasketSource) },
        { ContentAuditAssetType.ShopShelf, typeof(ShopShelfDefinition) },
        { ContentAuditAssetType.ShopShelfSource, typeof(ShopShelfSource) },
        { ContentAuditAssetType.ShopPaymentRule, typeof(ShopPaymentRuleDefinition) },
        { ContentAuditAssetType.ShopCheckoutTerminal, typeof(ShopCheckoutTerminal) },
        { ContentAuditAssetType.ShopReturnPolicy, typeof(ShopReturnPolicyDefinition) },
        { ContentAuditAssetType.ShopRefundSource, typeof(ShopRefundSource) },
        { ContentAuditAssetType.PlayerShopReceiptLog, typeof(PlayerShopReceiptLog) },
        { ContentAuditAssetType.ShopSecurityPolicy, typeof(ShopSecurityPolicyDefinition) },
        { ContentAuditAssetType.ShopSecuritySource, typeof(ShopSecuritySource) },
        { ContentAuditAssetType.PlayerShopSecurityLog, typeof(PlayerShopSecurityLog) },
        { ContentAuditAssetType.ShopRestockSchedule, typeof(ShopRestockScheduleDefinition) },
        { ContentAuditAssetType.ShopRestockSource, typeof(ShopRestockSource) },
        { ContentAuditAssetType.PlayerShopRestockLog, typeof(PlayerShopRestockLog) },
        { ContentAuditAssetType.ShopDeliveryService, typeof(ShopDeliveryServiceDefinition) },
        { ContentAuditAssetType.ShopDeliverySource, typeof(ShopDeliverySource) },
        { ContentAuditAssetType.PlayerDeliveryLog, typeof(PlayerDeliveryLog) },
        { ContentAuditAssetType.LearnableOffer, typeof(LearnableOfferDefinition) },
        { ContentAuditAssetType.LearnableOfferSource, typeof(LearnableOfferSource) },
        { ContentAuditAssetType.LoyaltyProgram, typeof(LoyaltyProgramDefinition) },
        { ContentAuditAssetType.LoyaltyProgramSource, typeof(LoyaltyProgramSource) },
        { ContentAuditAssetType.Service, typeof(ServiceDefinition) },
        { ContentAuditAssetType.ServicePackage, typeof(ServicePackageDefinition) },
        { ContentAuditAssetType.ServicePackageSource, typeof(ServicePackageSource) },
        { ContentAuditAssetType.ServiceAppointment, typeof(ServiceAppointmentDefinition) },
        { ContentAuditAssetType.ServiceAppointmentSource, typeof(ServiceAppointmentSource) },
        { ContentAuditAssetType.PlayerServiceAppointmentLog, typeof(PlayerServiceAppointmentLog) },
        { ContentAuditAssetType.EncounterTable, typeof(EncounterTableDefinition) },
        { ContentAuditAssetType.EncounterSourceProfile, typeof(EncounterSourceProfileDefinition) },
        { ContentAuditAssetType.EncounterSource, typeof(EncounterSource) },
        { ContentAuditAssetType.StealthCaptureProfile, typeof(StealthCaptureProfileDefinition) },
        { ContentAuditAssetType.CustomizationPart, typeof(CustomizationPartDefinition) },
        { ContentAuditAssetType.CustomizationPreset, typeof(CustomizationPresetDefinition) },
        { ContentAuditAssetType.PlayerOrigin, typeof(PlayerOriginDefinition) },
        { ContentAuditAssetType.PlayerLifestyle, typeof(PlayerLifestyleDefinition) },
        { ContentAuditAssetType.NewGameSetup, typeof(NewGameSetupDefinition) },
        { ContentAuditAssetType.NewGameSetupSource, typeof(NewGameSetupSource) },
        { ContentAuditAssetType.PlayerNewGameSetupLog, typeof(PlayerNewGameSetupLog) },
        { ContentAuditAssetType.PokedexEntry, typeof(PokedexEntryDefinition) },
        { ContentAuditAssetType.RegionInfo, typeof(RegionInfoDefinition) },
        { ContentAuditAssetType.WorldRegion, typeof(WorldRegionDefinition) },
        { ContentAuditAssetType.RegionTravelRoute, typeof(RegionTravelRouteDefinition) },
        { ContentAuditAssetType.RegionTravelPolicy, typeof(RegionTravelPolicyDefinition) },
        { ContentAuditAssetType.RegionChallengeProfile, typeof(RegionChallengeProfileDefinition) },
        { ContentAuditAssetType.RegionTravelPoint, typeof(RegionTravelPoint) },
        { ContentAuditAssetType.PokeNavEntry, typeof(PokeNavEntryDefinition) },
        { ContentAuditAssetType.PokeNavGuideSection, typeof(PokeNavGuideSectionDefinition) },
        { ContentAuditAssetType.PokeNavFeedItem, typeof(PokeNavFeedItemDefinition) },
        { ContentAuditAssetType.PokeNavFeedSource, typeof(PokeNavFeedSource) },
        { ContentAuditAssetType.SocialPost, typeof(SocialPostDefinition) },
        { ContentAuditAssetType.MapMarker, typeof(MapMarkerDefinition) },
        { ContentAuditAssetType.MapViewProfile, typeof(MapViewProfileDefinition) },
        { ContentAuditAssetType.MapDiscoverySource, typeof(MapDiscoverySource) },
        { ContentAuditAssetType.Rumor, typeof(RumorDefinition) },
        { ContentAuditAssetType.RumorSpreadProfile, typeof(RumorSpreadProfileDefinition) },
        { ContentAuditAssetType.WorldCondition, typeof(WorldConditionDefinition) },
        { ContentAuditAssetType.RiskIncident, typeof(RiskIncidentDefinition) },
        { ContentAuditAssetType.ConsequenceChain, typeof(ConsequenceChainDefinition) },
        { ContentAuditAssetType.WorldTrigger, typeof(WorldTriggerDefinition) },
        { ContentAuditAssetType.SceneObject, typeof(SceneObjectDefinition) },
        { ContentAuditAssetType.SceneSpawnProfile, typeof(SceneSpawnProfileDefinition) },
        { ContentAuditAssetType.WorldDiscovery, typeof(WorldDiscoveryDefinition) },
        { ContentAuditAssetType.LocationVisit, typeof(LocationVisitDefinition) },
        { ContentAuditAssetType.ChronicleEntry, typeof(ChronicleEntryDefinition) },
        { ContentAuditAssetType.ChronicleCaptureRule, typeof(ChronicleCaptureRuleDefinition) },
        { ContentAuditAssetType.NavigationHint, typeof(NavigationHintDefinition) },
        { ContentAuditAssetType.AreaProfile, typeof(AreaProfileDefinition) },
        { ContentAuditAssetType.CalendarEvent, typeof(CalendarEventDefinition) },
        { ContentAuditAssetType.BattleAIProfile, typeof(BattleAIProfile) },
        { ContentAuditAssetType.BattleMode, typeof(BattleModeDefinition) },
        { ContentAuditAssetType.BattleRuleSet, typeof(BattleRuleSetDefinition) },
        { ContentAuditAssetType.BattleChallenge, typeof(BattleChallengeDefinition) },
        { ContentAuditAssetType.Contest, typeof(ContestDefinition) },
        { ContentAuditAssetType.CareerPath, typeof(CareerPathDefinition) },
        { ContentAuditAssetType.Organization, typeof(OrganizationDefinition) },
        { ContentAuditAssetType.Assignment, typeof(AssignmentDefinition) },
        { ContentAuditAssetType.AccessProfile, typeof(AccessProfileDefinition) },
        { ContentAuditAssetType.LawViolation, typeof(LawViolationDefinition) },
        { ContentAuditAssetType.InvestigationCase, typeof(InvestigationCaseDefinition) },
        { ContentAuditAssetType.InvestigationClue, typeof(InvestigationClueDefinition) },
        { ContentAuditAssetType.NPCMemoryTopic, typeof(NPCMemoryTopicDefinition) },
        { ContentAuditAssetType.NPCReaction, typeof(NPCReactionDefinition) },
        { ContentAuditAssetType.WitnessReport, typeof(WitnessReportDefinition) },
        { ContentAuditAssetType.ReportPropagation, typeof(ReportPropagationDefinition) },
        { ContentAuditAssetType.CompanionRole, typeof(CompanionRoleDefinition) },
        { ContentAuditAssetType.CompanionPerk, typeof(CompanionPerkDefinition) },
        { ContentAuditAssetType.CompanionExpedition, typeof(CompanionExpeditionDefinition) },
        { ContentAuditAssetType.CompanionExpeditionRoute, typeof(CompanionExpeditionRouteDefinition) },
        { ContentAuditAssetType.NPCVisualSet, typeof(NPCVisualSetDefinition) },
        { ContentAuditAssetType.TrainerPartyTemplate, typeof(TrainerPartyTemplateDefinition) },
        { ContentAuditAssetType.NPCVariantPool, typeof(NPCVariantPoolDefinition) },
        { ContentAuditAssetType.NPCSceneRandomizationProfile, typeof(NPCSceneRandomizationProfileDefinition) },
        { ContentAuditAssetType.NPCSceneRandomizationSlot, typeof(NPCSceneRandomizationSlot) },
        { ContentAuditAssetType.NPCSceneRandomizationController, typeof(NPCSceneRandomizationController) },
        { ContentAuditAssetType.Job, typeof(JobDefinition) },
        { ContentAuditAssetType.JobBoard, typeof(JobBoardDefinition) },
        { ContentAuditAssetType.TransitRoute, typeof(TransitRouteDefinition) },
        { ContentAuditAssetType.TransitStop, typeof(TransitStopDefinition) },
        { ContentAuditAssetType.Farmable, typeof(FarmableDefinition) },
        { ContentAuditAssetType.ResourceNode, typeof(ResourceNodeDefinition) },
        { ContentAuditAssetType.ResearchSubject, typeof(ResearchSubjectDefinition) },
        { ContentAuditAssetType.PokemonCareAction, typeof(PokemonCareActionDefinition) },
        { ContentAuditAssetType.PokemonCareNeed, typeof(PokemonCareNeedDefinition) },
        { ContentAuditAssetType.PokemonCareFacility, typeof(PokemonCareFacilityDefinition) },
        { ContentAuditAssetType.PokemonAbilityTree, typeof(PokemonAbilityTreeDefinition) },
        { ContentAuditAssetType.PokemonAbilityTreeSource, typeof(PokemonAbilityTreeSource) },
        { ContentAuditAssetType.PokemonCareNeedsController, typeof(PokemonCareNeedsController) },
        { ContentAuditAssetType.Tool, typeof(ToolDefinition) },
        { ContentAuditAssetType.SurvivalNeed, typeof(SurvivalNeedDefinition) },
        { ContentAuditAssetType.Milestone, typeof(MilestoneDefinition) },
        { ContentAuditAssetType.Title, typeof(TitleDefinition) },
        { ContentAuditAssetType.RelationshipSubject, typeof(RelationshipSubjectDefinition) },
        { ContentAuditAssetType.ReputationFaction, typeof(ReputationFactionDefinition) },
        { ContentAuditAssetType.PokemonMood, typeof(PokemonMoodDefinition) },
        { ContentAuditAssetType.NPCSchedule, typeof(NPCScheduleDefinition) },
        { ContentAuditAssetType.GameEvent, typeof(GameEventDefinition) },
        { ContentAuditAssetType.Notification, typeof(NotificationDefinition) },
        { ContentAuditAssetType.AssetAuditProfile, typeof(AssetAuditProfileDefinition) },
        { ContentAuditAssetType.AssetAuditRunner, typeof(AssetAuditRunner) },
        { ContentAuditAssetType.MarketServiceUIManager, typeof(MarketServiceUIManager) },
        { ContentAuditAssetType.PokeNavMapUIManager, typeof(PokeNavMapUIManager) },
        { ContentAuditAssetType.ConditionalDialog, typeof(ConditionalDialogDefinition) },
        { ContentAuditAssetType.SpeechBubbleStyle, typeof(SpeechBubbleStyleDefinition) },
        { ContentAuditAssetType.RidePokemon, typeof(RidePokemonDefinition) },
        { ContentAuditAssetType.RidePoint, typeof(RidePoint) },
        { ContentAuditAssetType.PowerMechanic, typeof(PowerMechanicDefinition) },
        { ContentAuditAssetType.Competition, typeof(CompetitionDefinition) },
        { ContentAuditAssetType.CompetitionRanking, typeof(CompetitionRankingDefinition) },
        { ContentAuditAssetType.CompetitionHonor, typeof(CompetitionHonorDefinition) },
        { ContentAuditAssetType.CompetitionSeason, typeof(CompetitionSeasonDefinition) },
        { ContentAuditAssetType.CompetitionEntrant, typeof(CompetitionEntrantDefinition) },
        { ContentAuditAssetType.CompetitionRoster, typeof(CompetitionRosterDefinition) },
        { ContentAuditAssetType.CompetitionBracketSource, typeof(CompetitionBracketSource) },
        { ContentAuditAssetType.CompetitionPrizeTable, typeof(CompetitionPrizeTableDefinition) },
        { ContentAuditAssetType.CompetitionVenue, typeof(CompetitionVenueDefinition) },
        { ContentAuditAssetType.CompetitionVenueSource, typeof(CompetitionVenueSource) },
        { ContentAuditAssetType.CompetitionInvitation, typeof(CompetitionInvitationDefinition) },
        { ContentAuditAssetType.CompetitionInvitationSource, typeof(CompetitionInvitationSource) },
        { ContentAuditAssetType.Sponsor, typeof(SponsorDefinition) },
        { ContentAuditAssetType.SponsorSource, typeof(SponsorSource) },
        { ContentAuditAssetType.CompetitionRegistrationWindow, typeof(CompetitionRegistrationWindowDefinition) },
        { ContentAuditAssetType.CompetitionRegistration, typeof(CompetitionRegistrationDefinition) },
        { ContentAuditAssetType.CompetitionRegistrationSource, typeof(CompetitionRegistrationSource) },
        { ContentAuditAssetType.CompetitionMatchResolver, typeof(CompetitionMatchResolverDefinition) }
    };

    public static IReadOnlyList<UnityEngine.Object> FindCandidates(
        ContentAuditScanScope scanScope,
        ContentAuditAssetType targetType,
        string customTypeName,
        bool includeInactiveSceneObjects,
        out string scanMessage
    ) {
        scanMessage = string.Empty;
        Type type = ResolveType(targetType, customTypeName);
        if(type == null) {
            scanMessage = $"Could not resolve type '{customTypeName}'.";
            return Array.Empty<UnityEngine.Object>();
        }

        if(scanScope == ContentAuditScanScope.LoadedSceneComponents) {
            return FindLoadedSceneComponents(type, includeInactiveSceneObjects);
        }

        if(scanScope == ContentAuditScanScope.EditorProjectAssets) {
#if UNITY_EDITOR
            return FindEditorAssets(type);
#else
            scanMessage = "Editor Project Assets scope is only available inside Unity Editor; Resources scope was used instead.";
            return FindResources(type);
#endif
        }

        return FindResources(type);
    }

    public static string GetReadableName(UnityEngine.Object candidate) {
        if(candidate == null) {
            return "(null)";
        }

        string displayName = GetStringMember(candidate, "DisplayName");
        if(!string.IsNullOrWhiteSpace(displayName)) {
            return displayName;
        }

        return candidate.name;
    }

    public static string GetId(UnityEngine.Object candidate) {
        return candidate == null ? string.Empty : GetStringMember(candidate, "Id");
    }

    public static bool HasTag(UnityEngine.Object candidate, string tag) {
        if(candidate == null || string.IsNullOrWhiteSpace(tag)) {
            return false;
        }

        var type = candidate.GetType();
        var hasTag = type.GetMethod("HasTag", BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(string) }, null);
        if(hasTag != null && hasTag.ReturnType == typeof(bool)) {
            try {
                return (bool)hasTag.Invoke(candidate, new object[] { tag });
            } catch(TargetInvocationException) {
                return false;
            }
        }

        object tags = GetMemberValue(candidate, "Tags") ?? GetMemberValue(candidate, "tags");
        if(tags is IEnumerable<string> stringTags) {
            return stringTags.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase));
        }

        if(tags is IEnumerable enumerableTags) {
            foreach(var item in enumerableTags) {
                if(item is string value && string.Equals(value, tag, StringComparison.OrdinalIgnoreCase)) {
                    return true;
                }
            }
        }

        return false;
    }

    static Type ResolveType(ContentAuditAssetType targetType, string customTypeName) {
        if(targetType != ContentAuditAssetType.CustomUnityObjectType) {
            return typeMap.TryGetValue(targetType, out var mappedType) ? mappedType : null;
        }

        if(string.IsNullOrWhiteSpace(customTypeName)) {
            return null;
        }

        return AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType(customTypeName)
                ?? assembly.GetTypes().FirstOrDefault(type => string.Equals(type.Name, customTypeName, StringComparison.OrdinalIgnoreCase)))
            .FirstOrDefault(type => type != null && typeof(UnityEngine.Object).IsAssignableFrom(type));
    }

    static IReadOnlyList<UnityEngine.Object> FindResources(Type type) {
        return Resources.LoadAll(string.Empty, type).Where(asset => asset != null).ToList();
    }

    static IReadOnlyList<UnityEngine.Object> FindLoadedSceneComponents(Type type, bool includeInactiveSceneObjects) {
        if(!typeof(Component).IsAssignableFrom(type)) {
            return Array.Empty<UnityEngine.Object>();
        }

        var inactiveMode = includeInactiveSceneObjects ? FindObjectsInactive.Include : FindObjectsInactive.Exclude;
        return UnityEngine.Object.FindObjectsByType(type, inactiveMode)
            .Where(component => component != null)
            .ToList();
    }

#if UNITY_EDITOR
    static IReadOnlyList<UnityEngine.Object> FindEditorAssets(Type type) {
        if(typeof(Component).IsAssignableFrom(type)) {
            return FindEditorPrefabComponents(type);
        }

        string filter = type == typeof(ScriptableObject) ? "t:ScriptableObject" : $"t:{type.Name}";
        return AssetDatabase.FindAssets(filter)
            .Select(AssetDatabase.GUIDToAssetPath)
            .SelectMany(AssetDatabase.LoadAllAssetsAtPath)
            .Where(asset => asset != null && type.IsAssignableFrom(asset.GetType()))
            .Distinct()
            .ToList();
    }

    static IReadOnlyList<UnityEngine.Object> FindEditorPrefabComponents(Type type) {
        return AssetDatabase.FindAssets("t:Prefab")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<GameObject>)
            .Where(prefab => prefab != null)
            .SelectMany(prefab => prefab.GetComponentsInChildren(type, true).Cast<UnityEngine.Object>())
            .Where(component => component != null)
            .Distinct()
            .ToList();
    }
#endif

    static string GetStringMember(UnityEngine.Object candidate, string memberName) {
        object value = GetMemberValue(candidate, memberName);
        return value as string ?? string.Empty;
    }

    static object GetMemberValue(UnityEngine.Object candidate, string memberName) {
        var type = candidate.GetType();
        var property = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if(property != null) {
            return property.GetValue(candidate);
        }

        var field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return field != null ? field.GetValue(candidate) : null;
    }
}
