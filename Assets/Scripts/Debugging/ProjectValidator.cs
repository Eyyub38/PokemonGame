using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ProjectValidator : MonoBehaviour {
    [Tooltip("If enabled, validation runs automatically when this component starts.")]
    [SerializeField] bool runOnStart;
    [Tooltip("If enabled, informational validation issues are also logged.")]
    [SerializeField] bool logInfoIssues;

    public ProjectValidationReport LastReport { get; private set; }

    void Start() {
        if(runOnStart) {
            RunValidation();
        }
    }

    [ContextMenu("Run Project Validation")]
    public void RunValidationFromContextMenu() {
        RunValidation();
    }

    public ProjectValidationReport RunValidation() {
        LastReport = ValidateAll();
        LogReport(LastReport);
        return LastReport;
    }

    public static ProjectValidationReport ValidateAll() {
        var report = new ProjectValidationReport();
        ValidateActivityDefinitions(report);
        ValidateActivityZones(report);
        ValidateActivityPermissions(report);
        ValidateActivityOutcomes(report);
        ValidateRecipes(report);
        ValidateCraftingStations(report);
        ValidateItemBrands(report);
        ValidateItemModels(report);
        ValidateShopCatalogs(report);
        ValidateShopShelves(report);
        ValidateShopPaymentRules(report);
        ValidateShopReturnPolicies(report);
        ValidateShopSecurityPolicies(report);
        ValidateShopRestockSchedules(report);
        ValidateShopDeliveryServices(report);
        ValidateLearnableOffers(report);
        ValidateLoyaltyPrograms(report);
        ValidateSponsors(report);
        ValidateServices(report);
        ValidateServicePackages(report);
        ValidateServiceAppointments(report);
        ValidateEncounterTables(report);
        ValidateEncounterSourceProfiles(report);
        ValidateStealthCaptureProfiles(report);
        ValidateEncounterResolutions(report);
        ValidateEncounterResolutionChoiceSets(report);
        ValidateEncounterSources(report);
        ValidateOverworldEncounterPaths(report);
        ValidateCustomizationParts(report);
        ValidateCustomizationPresets(report);
        ValidatePlayerOrigins(report);
        ValidatePlayerLifestyles(report);
        ValidateNewGameSetups(report);
        ValidateLifePaths(report);
        ValidateQuests(report);
        ValidatePokedexEntries(report);
        ValidatePokemonCoreGrowth(report);
        ValidatePokemonAbilityTrees(report);
        ValidatePokemonEvolutions(report);
        ValidatePokemonTechniqueLearning(report);
        ValidatePokemonHeldItems(report);
        ValidateRegionInfo(report);
        ValidateWorldRegions(report);
        ValidateRideSystem(report);
        ValidatePokeNavEntries(report);
        ValidatePokeNavGuideSections(report);
        ValidatePokeNavFeedItems(report);
        ValidateSocialPosts(report);
        ValidateSocialActivities(report);
        ValidateRoleActivityBoards(report);
        ValidateCampStations(report);
        ValidateMapMarkers(report);
        ValidateMapViewProfiles(report);
        ValidateRumors(report);
        ValidateWorldConditions(report);
        ValidateJourneyEnvironments(report);
        ValidateJourneyIncidents(report);
        ValidateRiskIncidents(report);
        ValidateConsequenceChains(report);
        ValidateWorldTriggers(report);
        ValidateSituationEvents(report);
        ValidateSceneObjects(report);
        ValidateSceneSpawns(report);
        ValidateWorldDiscoveries(report);
        ValidateLocationVisits(report);
        ValidateChronicleEntries(report);
        ValidateNavigationHints(report);
        ValidateAreaProfiles(report);
        ValidateCalendarEvents(report);
        ValidateBattleAIProfiles(report);
        ValidateBattleModes(report);
        ValidateBattleRuleSets(report);
        ValidatePowerMechanics(report);
        ValidateCompetitions(report);
        ValidateCompetitionRankings(report);
        ValidateCompetitionHonors(report);
        ValidateCompetitionSeasons(report);
        ValidateCompetitionEntrants(report);
        ValidateCompetitionRosters(report);
        ValidateCompetitionPrizeTables(report);
        ValidateCompetitionVenues(report);
        ValidateCompetitionInvitations(report);
        ValidateCompetitionRegistrationWindows(report);
        ValidateCompetitionRegistrations(report);
        ValidateCompetitionMatchResolvers(report);
        ValidateCompetitionBracketSources(report);
        ValidateCompetitionRegistrationSources(report);
        ValidateCompetitionInvitationSources(report);
        ValidateCompetitionVenueSources(report);
        ValidateSponsorSources(report);
        ValidateShopBasketSources(report);
        ValidateShopCheckoutTerminals(report);
        ValidateShopRefundSources(report);
        ValidateShopSecuritySources(report);
        ValidateShopRestockSources(report);
        ValidateShopDeliverySources(report);
        ValidateShopShelfSources(report);
        ValidateLearnableOfferSources(report);
        ValidateLoyaltyProgramSources(report);
        ValidateServicePackageSources(report);
        ValidateServiceAppointmentSources(report);
        ValidateMarketServiceUIManagers(report);
        ValidateCampStationUIManagers(report);
        ValidateRoleActivityBoardUIManagers(report);
        ValidatePokeNavMapUIManagers(report);
        ValidatePokeNavKnowledgeDetailUIManagers(report);
        ValidatePokeNavMapFilterUIManagers(report);
        ValidateJourneyIncidentUIManagers(report);
        ValidateNotificationFeedUIManagers(report);
        ValidateBattleModeOptionsUIManagers(report);
        ValidateProgressionAccessUIManagers(report);
        ValidateProgressionFocusedPanelUIManagers(report);
        ValidateRadialMenuUI(report);
        ValidateCompetitionRegistrationUIManagers(report);
        ValidateCompetitionBracketRankingUIManagers(report);
        ValidateTransitJourneyUIManagers(report);
        ValidateEncounterResolutionUIManagers(report);
        ValidateOverworldEncounterDebugUIManagers(report);
        ValidateCompanionNodeFollow(report);
        ValidatePokeNavFeedSources(report);
        ValidateMapDiscoverySources(report);
        ValidateBattleChallenges(report);
        ValidateContests(report);
        ValidateCareers(report);
        ValidateOrganizations(report);
        ValidateAssignments(report);
        ValidateAccessProfiles(report);
        ValidateLawViolations(report);
        ValidateInvestigations(report);
        ValidateNPCMemoryTopics(report);
        ValidateNPCReactions(report);
        ValidateWitnessReports(report);
        ValidateReportPropagations(report);
        ValidateNPCGeneration(report);
        ValidateDialogGraphs(report);
        ValidateCompanions(report);
        ValidateJobs(report);
        ValidateJobBoards(report);
        ValidateTransitRoutes(report);
        ValidateTransitStops(report);
        ValidateTransitJourneys(report);
        ValidateTransitJourneySources(report);
        ValidateTransitRegionHandoffs(report);
        ValidateFarmables(report);
        ValidateResources(report);
        ValidateResearchSubjects(report);
        ValidateSurvivalNeeds(report);
        ValidateCareActions(report);
        ValidateCareFacilities(report);
        ValidatePokemonAssignments(report);
        ValidateCareNeedControllers(report);
        ValidateTitles(report);
        ValidateAssetAuditProfiles(report);
        ValidateContentAuditProfiles(report);
        ValidateDuplicateIds(report);
        return report;
    }

    static void ValidateActivityDefinitions(ProjectValidationReport report) {
        foreach(var activity in ProjectValidatorAssetFinder.FindAssets<ActivityDefinition>()) {
            if(activity == null) continue;

            string context = $"Activity/{activity.name}";
            if(string.IsNullOrWhiteSpace(activity.Id)) {
                report.Error("Activity id is empty.", context);
            }

            foreach(var requirement in activity.Requirements) {
                if(requirement == null) {
                    report.Warning("Activity has a null requirement slot.", context);
                }
            }

            foreach(var outcome in activity.Outcomes) {
                if(outcome == null) {
                    report.Warning("Activity has a null outcome slot.", context);
                }
            }

            ValidateCareerPointGrants(activity.CareerPointRewards, report, context);
            ValidateLifePathRewards(activity.LifePathRewards, report, context);
            ValidateOrganizationMembershipGrants(activity.OrganizationMembershipRewards, report, context);
            ValidateOrganizationPointGrants(activity.OrganizationPointRewards, report, context);

            foreach(var cost in activity.ItemCosts) {
                if(cost != null && cost.item == null && cost.count > 0) {
                    report.Warning("Activity item cost has count but no item.", context);
                }
            }

            foreach(var cost in activity.ToolCosts) {
                if(cost != null && cost.tool == null && cost.durabilityCost > 0) {
                    report.Warning("Activity tool cost has durability cost but no tool.", context);
                }
            }

            foreach(var cost in activity.NeedCosts) {
                if(cost != null && cost.need == null && cost.amount > 0) {
                    report.Warning("Activity need cost has amount but no need.", context);
                }
            }
        }
    }

    static void ValidateActivityZones(ProjectValidationReport report) {
        foreach(var zone in ProjectValidatorAssetFinder.FindAssets<ActivityZoneDefinition>()) {
            if(zone == null) continue;

            string context = $"ActivityZone/{zone.name}";
            if(zone.RuleMode == ActivityZoneRuleMode.AllowListedActivities && (zone.AllowedActivities == null || zone.AllowedActivities.Count == 0)) {
                report.Warning("Activity zone uses allow-list mode but has no allowed activities.", context);
            }

            if(zone.AllowedActivities != null) {
                foreach(var activity in zone.AllowedActivities) {
                    if(activity == null) {
                        report.Warning("Activity zone has a null allowed activity slot.", context);
                    }
                }
            }

            if(zone.BlockedActivities != null) {
                foreach(var activity in zone.BlockedActivities) {
                    if(activity == null) {
                        report.Warning("Activity zone has a null blocked activity slot.", context);
                    }
                }
            }

            if(zone.Permissions != null) {
                foreach(var permission in zone.Permissions) {
                    if(permission == null) {
                        report.Warning("Activity zone has a null permission slot.", context);
                    }
                }
            }

            if(zone.Modifiers != null) {
                foreach(var modifier in zone.Modifiers) {
                    if(modifier == null) {
                        report.Warning("Activity zone has a null modifier slot.", context);
                    }
                }
            }
        }
    }

    static void ValidateActivityPermissions(ProjectValidationReport report) {
        foreach(var permission in ProjectValidatorAssetFinder.FindAssets<ActivityPermissionDefinition>()) {
            if(permission == null) continue;

            string context = $"ActivityPermission/{permission.name}";
            if(string.IsNullOrWhiteSpace(permission.Id)) {
                report.Error("Activity permission id is empty.", context);
            }

            if(permission.Tags != null && permission.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Activity permission has an empty tag slot.", context);
            }

            if(permission.Activities != null) {
                foreach(var activity in permission.Activities) {
                    if(activity == null) {
                        report.Warning("Activity permission has a null activity filter slot.", context);
                    }
                }
            }

            if(permission.ActivityTags != null && permission.ActivityTags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Activity permission has an empty activity tag filter slot.", context);
            }

            if(permission.Zones != null) {
                foreach(var zone in permission.Zones) {
                    if(zone == null) {
                        report.Warning("Activity permission has a null zone filter slot.", context);
                    }
                }
            }

            if(permission.ZoneTags != null && permission.ZoneTags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Activity permission has an empty zone tag filter slot.", context);
            }

            foreach(var requirement in permission.Requirements) {
                if(requirement == null) {
                    report.Warning("Activity permission has a null requirement slot.", context);
                }
            }
        }
    }

    static void ValidateActivityOutcomes(ProjectValidationReport report) {
        foreach(var outcome in ProjectValidatorAssetFinder.FindAssets<ActivityOutcomeDefinition>()) {
            if(outcome == null) continue;

            string context = $"ActivityOutcome/{outcome.name}";
            if(outcome.Chance <= 0f) {
                report.Warning("Outcome chance is 0, so it will never trigger.", context);
            }

            ValidateCareerPointGrants(outcome.CareerPointRewards, report, context);
            ValidateLifePathRewards(outcome.LifePathRewards, report, context);
            ValidateOrganizationMembershipGrants(outcome.OrganizationMembershipRewards, report, context);
            ValidateOrganizationPointGrants(outcome.OrganizationPointRewards, report, context);

            foreach(var chain in outcome.ConsequenceChains) {
                if(chain == null) {
                    report.Warning("Activity outcome has a null consequence chain slot.", context);
                }
            }
        }
    }

    static void ValidateRecipes(ProjectValidationReport report) {
        foreach(var recipe in ProjectValidatorAssetFinder.FindAssets<RecipeDefinition>()) {
            if(recipe == null) continue;

            string context = $"Recipe/{recipe.name}";
            if(string.IsNullOrWhiteSpace(recipe.Id)) {
                report.Error("Recipe id is empty.", context);
            }

            if(recipe.OutputItem == null) {
                report.Warning("Recipe has no output item.", context);
            }

            if(recipe.RequiresCraftingStation && recipe.RequiredStation == null) {
                report.Warning("Recipe requires a crafting station but has no exact required station assigned. Any station that allows the recipe can still craft it.", context);
            }

            foreach(var ingredient in recipe.Ingredients) {
                if(ingredient != null && ingredient.item == null && ingredient.count > 0) {
                    report.Warning("Recipe ingredient has count but no item.", context);
                }
            }

            foreach(var cost in recipe.ToolCosts) {
                if(cost != null && cost.tool == null && cost.durabilityCost > 0) {
                    report.Warning("Recipe tool cost has durability cost but no tool.", context);
                }
            }

            foreach(var cost in recipe.NeedCosts) {
                if(cost != null && cost.need == null && cost.amount > 0) {
                    report.Warning("Recipe need cost has amount but no need.", context);
                }
            }

            foreach(var requirement in recipe.ExtraRequirements) {
                if(requirement == null) {
                    report.Warning("Recipe has a null extra requirement slot.", context);
                }
            }
        }
    }

    static void ValidateCraftingStations(ProjectValidationReport report) {
        foreach(var station in ProjectValidatorAssetFinder.FindAssets<CraftingStationDefinition>()) {
            if(station == null) continue;

            string context = $"CraftingStation/{station.name}";
            if(string.IsNullOrWhiteSpace(station.Id)) {
                report.Error("Crafting station id is empty.", context);
            }

            foreach(var recipe in station.AllowedRecipes) {
                if(recipe == null) {
                    report.Warning("Crafting station has a null allowed recipe slot.", context);
                }
            }

            foreach(var recipe in station.BlockedRecipes) {
                if(recipe == null) {
                    report.Warning("Crafting station has a null blocked recipe slot.", context);
                }
            }
        }
    }

    static void ValidateItemBrands(ProjectValidationReport report) {
        foreach(var brand in ProjectValidatorAssetFinder.FindAssets<ItemBrandDefinition>()) {
            if(brand == null) continue;

            string context = $"ItemBrand/{brand.name}";
            if(string.IsNullOrWhiteSpace(brand.Id)) {
                report.Error("Item brand id is empty.", context);
            }
        }
    }

    static void ValidateItemModels(ProjectValidationReport report) {
        foreach(var model in ProjectValidatorAssetFinder.FindAssets<ItemModelDefinition>()) {
            if(model == null) continue;

            string context = $"ItemModel/{model.name}";
            if(string.IsNullOrWhiteSpace(model.Id)) {
                report.Error("Item model id is empty.", context);
            }

            if(model.Item == null) {
                report.Warning("Item model has no inventory item.", context);
            }

            if(model.EffectivenessMultiplier <= 0f) {
                report.Warning("Item model effectiveness is 0, so systems reading potency may treat it as ineffective.", context);
            }
        }
    }

    static void ValidateShopCatalogs(ProjectValidationReport report) {
        foreach(var catalog in ProjectValidatorAssetFinder.FindAssets<ShopCatalogDefinition>()) {
            if(catalog == null) continue;

            string context = $"ShopCatalog/{catalog.name}";
            if(string.IsNullOrWhiteSpace(catalog.Id)) {
                report.Error("Shop catalog id is empty.", context);
            }

            var entries = catalog.Entries ?? new List<ShopCatalogEntry>();
            var duplicateOffers = entries
                .Where(e => e != null)
                .GroupBy(e => e.OfferId)
                .Where(g => !string.IsNullOrWhiteSpace(g.Key) && g.Count() > 1);

            foreach(var duplicate in duplicateOffers) {
                report.Warning($"Shop catalog has duplicate offer id '{duplicate.Key}'.", context);
            }

            foreach(var entry in entries) {
                if(entry == null) {
                    report.Warning("Shop catalog has a null entry slot.", context);
                    continue;
                }

                if(entry.GetItem() == null) {
                    report.Warning($"Shop offer '{entry.OfferId}' has no item/model item.", context);
                }

                if(entry.stockLimitPeriod != ShopStockLimitPeriod.None && entry.stockLimit <= 0) {
                    report.Warning($"Shop offer '{entry.OfferId}' has a stock limit period but no stock limit.", context);
                }
            }
        }
    }

    static void ValidateShopShelves(ProjectValidationReport report) {
        foreach(var shelf in ProjectValidatorAssetFinder.FindAssets<ShopShelfDefinition>()) {
            if(shelf == null) continue;

            string context = $"ShopShelf/{shelf.name}";
            if(string.IsNullOrWhiteSpace(shelf.Id)) {
                report.Error("Shop shelf id is empty.", context);
            }

            if(shelf.Tags != null && shelf.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Shop shelf has an empty tag slot.", context);
            }

            if(shelf.RequiredCatalogTags != null && shelf.RequiredCatalogTags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Shop shelf has an empty required catalog tag slot.", context);
            }

            if(shelf.ExplicitOfferIds != null && shelf.ExplicitOfferIds.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Shop shelf has an empty explicit offer id slot.", context);
            }

            if(shelf.RequiredOfferTags != null && shelf.RequiredOfferTags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Shop shelf has an empty required offer tag slot.", context);
            }

            if(!shelf.IncludeExplicitOffers && !shelf.IncludeFilteredCatalogOffers) {
                report.Warning("Shop shelf includes neither explicit offers nor filtered catalog offers, so it will always be empty.", context);
            }

            if(shelf.IncludeExplicitOffers && (shelf.ExplicitOfferIds == null || shelf.ExplicitOfferIds.Count == 0) && !shelf.IncludeFilteredCatalogOffers) {
                report.Warning("Shop shelf only includes explicit offers but no offer ids are assigned.", context);
            }

            if(shelf.MinimumQualityTier > 0 && shelf.MaximumQualityTier > 0 && shelf.MinimumQualityTier > shelf.MaximumQualityTier) {
                report.Warning("Shop shelf minimum quality tier is higher than maximum quality tier.", context);
            }

            bool usesModelOnlyFilters = (shelf.AllowedModelCategories != null && shelf.AllowedModelCategories.Count > 0)
                || shelf.RequiredBrand != null
                || shelf.MinimumQualityTier > 0
                || shelf.MaximumQualityTier > 0;
            if(usesModelOnlyFilters) {
                report.Info("Shop shelf uses model-only filters. Direct itemOverride offers can match offer/item tags, but category, brand and quality filters require ItemModelDefinition.", context);
            }

            if(!string.IsNullOrWhiteSpace(shelf.DefaultOfferId)
                && shelf.IncludeExplicitOffers
                && shelf.ExplicitOfferIds != null
                && shelf.ExplicitOfferIds.Count > 0
                && !shelf.ExplicitOfferIds.Contains(shelf.DefaultOfferId)) {
                report.Info("Shop shelf default offer id is not in explicit offer ids. It can still be found through filtered catalog offers.", context);
            }

            ValidateObjectList(shelf.Requirements, report, context, "Shop shelf has a null requirement slot.");
        }
    }

    static void ValidateShopPaymentRules(ProjectValidationReport report) {
        foreach(var rule in ProjectValidatorAssetFinder.FindAssets<ShopPaymentRuleDefinition>()) {
            if(rule == null) continue;

            string context = $"ShopPaymentRule/{rule.name}";
            if(string.IsNullOrWhiteSpace(rule.Id)) {
                report.Error("Shop payment rule id is empty.", context);
            }

            if(rule.Tags != null && rule.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Shop payment rule has an empty tag slot.", context);
            }

            if(rule.RequiredCatalogTags != null && rule.RequiredCatalogTags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Shop payment rule has an empty required catalog tag slot.", context);
            }

            if(rule.MaximumSubtotal > 0f && rule.MinimumSubtotal > rule.MaximumSubtotal) {
                report.Warning("Shop payment rule minimum subtotal is higher than maximum subtotal.", context);
            }

            if(rule.PaymentMode != ShopPaymentMode.Money && rule.RequireWalletFunds) {
                report.Info("Shop payment rule does not charge Wallet, so Require Wallet Funds has no effect.", context);
            }

            if(rule.PaymentMode == ShopPaymentMode.Free && (rule.FlatServiceFee > 0f || rule.PercentageServiceFee > 0f)) {
                report.Info("Free payment rule has fees configured, but final amount due will still be waived.", context);
            }

            if(rule.DiscountPercent > 1f) {
                report.Info("Shop payment rule discount percent is greater than 1.0, so it can fully waive the checkout total.", context);
            }

            ValidateObjectList(rule.Requirements, report, context, "Shop payment rule has a null requirement slot.");
        }
    }

    static void ValidateShopReturnPolicies(ProjectValidationReport report) {
        foreach(var policy in ProjectValidatorAssetFinder.FindAssets<ShopReturnPolicyDefinition>()) {
            if(policy == null) continue;

            string context = $"ShopReturnPolicy/{policy.name}";
            if(string.IsNullOrWhiteSpace(policy.Id)) {
                report.Error("Shop return policy id is empty.", context);
            }

            if(policy.Tags != null && policy.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Shop return policy has an empty tag slot.", context);
            }

            if(policy.RequiredCatalogTags != null && policy.RequiredCatalogTags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Shop return policy has an empty required catalog tag slot.", context);
            }

            if(!policy.AllowFullReceiptRefund && !policy.AllowLineRefund) {
                report.Warning("Shop return policy allows neither full receipt nor line refunds.", context);
            }

            if(policy.MaxRefundPercent <= 0f) {
                report.Info("Shop return policy refund percent is 0, so refunds only record the return unless fees are used for analytics.", context);
            }

            if(!policy.RequireItemsInInventory) {
                report.Info("Shop return policy does not require returned items in inventory. This is useful for manual adjustments, but can duplicate value if used as a normal refund.", context);
            }

            if(policy.RestoreLimitedStockOnRefund && !policy.RequireItemsInInventory) {
                report.Info("Shop return policy restores limited stock without requiring inventory items. Check this is intended.", context);
            }

            ValidateObjectList(policy.Requirements, report, context, "Shop return policy has a null requirement slot.");
        }
    }

    static void ValidateShopSecurityPolicies(ProjectValidationReport report) {
        foreach(var policy in ProjectValidatorAssetFinder.FindAssets<ShopSecurityPolicyDefinition>()) {
            if(policy == null) continue;

            string context = $"ShopSecurityPolicy/{policy.name}";
            if(string.IsNullOrWhiteSpace(policy.Id)) {
                report.Error("Shop security policy id is empty.", context);
            }

            if(policy.Tags != null && policy.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Shop security policy has an empty tag slot.", context);
            }

            if(policy.RequiredCatalogTags != null && policy.RequiredCatalogTags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Shop security policy has an empty required catalog tag slot.", context);
            }

            if(policy.RequireActiveBasket && !policy.RequireBasketLines && policy.MinimumLineCount <= 0 && policy.MinimumBundleCount <= 0 && policy.MinimumUnpaidValue <= 0f) {
                report.Info("Shop security policy can trigger on any active basket, even when the basket is empty or has no value.", context);
            }

            if(policy.ConsequenceMode == ShopSecurityConsequenceMode.RiskIncident && policy.RiskIncident == null) {
                report.Warning("Shop security policy records a risk incident but has no risk incident assigned.", context);
            }

            if(policy.ConsequenceMode == ShopSecurityConsequenceMode.LawViolation && policy.LawViolation == null) {
                report.Warning("Shop security policy records a law violation but has no law violation assigned.", context);
            }

            if(policy.ConsequenceMode == ShopSecurityConsequenceMode.RiskIncidentAndLawViolation) {
                if(policy.RiskIncident == null) {
                    report.Warning("Shop security policy records risk and law but has no risk incident assigned.", context);
                }

                if(policy.LawViolation == null) {
                    report.Warning("Shop security policy records risk and law but has no law violation assigned.", context);
                }

                if(policy.RiskIncident != null && policy.RiskIncident.RecordLawViolation && policy.RiskIncident.LawViolation != null) {
                    report.Warning("Shop security policy records a direct law violation while its risk incident also records law. This can create duplicate law incidents.", context);
                }
            }

            if(!policy.BlockExitWhenTriggered && policy.ConsequenceMode == ShopSecurityConsequenceMode.SecurityLogOnly && !policy.ClearBasketWhenTriggered) {
                report.Info("Shop security policy only records a security log and does not block exit, clear basket, risk or law.", context);
            }

            ValidateObjectList(policy.Requirements, report, context, "Shop security policy has a null requirement slot.");
        }
    }

    static void ValidateShopRestockSchedules(ProjectValidationReport report) {
        foreach(var schedule in ProjectValidatorAssetFinder.FindAssets<ShopRestockScheduleDefinition>()) {
            if(schedule == null) continue;

            string context = $"ShopRestockSchedule/{schedule.name}";
            if(string.IsNullOrWhiteSpace(schedule.Id)) {
                report.Error("Shop restock schedule id is empty.", context);
            }

            if(schedule.Tags != null && schedule.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Shop restock schedule has an empty tag slot.", context);
            }

            if(schedule.RequiredCatalogTags != null && schedule.RequiredCatalogTags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Shop restock schedule has an empty required catalog tag slot.", context);
            }

            if(schedule.TimingMode == ShopRestockTimingMode.ManualOnly) {
                report.Info("Shop restock schedule is manual only. It will not run from normal due checks unless forced or explicitly triggered.", context);
            }

            if(schedule.TimingMode == ShopRestockTimingMode.EveryNDays && schedule.RepeatEveryDays <= 0) {
                report.Warning("Shop restock schedule uses Every N Days but interval is 0.", context);
            }

            if(schedule.TimingMode == ShopRestockTimingMode.CalendarEventActive && schedule.CalendarEvent == null) {
                report.Warning("Shop restock schedule uses Calendar Event Active but has no calendar event assigned.", context);
            }

            if(schedule.AllowedStockPeriods != null && schedule.AllowedStockPeriods.Any(period => period == ShopStockLimitPeriod.None)) {
                report.Warning("Shop restock schedule allowed stock periods includes None, which cannot be restored.", context);
            }

            if(schedule.OnlyLimitedStockOffers && schedule.AllowedStockPeriods != null && schedule.AllowedStockPeriods.Count == 0) {
                report.Info("Shop restock schedule restores any limited stock period because Allowed Stock Periods is empty.", context);
            }

            if(schedule.RestoreMode == ShopRestockRestoreMode.RestoreBundleCount && schedule.RestoreBundleCount <= 0) {
                report.Warning("Shop restock schedule restores a bundle count but the restore count is 0.", context);
            }

            if(schedule.RequiredModelTags != null && schedule.RequiredModelTags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Shop restock schedule has an empty required model tag slot.", context);
            }

            if(schedule.RequiredModelTags != null && schedule.RequiredModelTags.Count > 0) {
                report.Info("Shop restock schedule uses model-tag filters. Direct itemOverride offers without ItemModelDefinition will not match these tags.", context);
            }

            if(schedule.ExplicitOfferIds != null && schedule.ExplicitOfferIds.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Shop restock schedule has an empty explicit offer id slot.", context);
            }

            ValidateObjectList(schedule.Requirements, report, context, "Shop restock schedule has a null requirement slot.");
        }
    }

    static void ValidateShopDeliveryServices(ProjectValidationReport report) {
        foreach(var service in ProjectValidatorAssetFinder.FindAssets<ShopDeliveryServiceDefinition>()) {
            if(service == null) continue;

            string context = $"ShopDeliveryService/{service.name}";
            if(string.IsNullOrWhiteSpace(service.Id)) {
                report.Error("Shop delivery service id is empty.", context);
            }

            if(service.Tags != null && service.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Shop delivery service has an empty tag slot.", context);
            }

            if(service.RequiredCatalogTags != null && service.RequiredCatalogTags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Shop delivery service has an empty required catalog tag slot.", context);
            }

            if(service.RequiredDestinationTags != null && service.RequiredDestinationTags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Shop delivery service has an empty required destination tag slot.", context);
            }

            if(service.FulfillmentMode == ShopDeliveryFulfillmentMode.ClaimAtDestination && !service.RequireDestinationId && service.RequiredDestinationMarker == null && service.RequiredDestinationRegion == null) {
                report.Info("Claim-at-destination delivery has no explicit destination requirement. Any claim source can claim matching empty-destination orders.", context);
            }

            if(service.BaseDeliveryHours <= 0 && service.HoursPerBundle <= 0f && service.MinimumDeliveryHours <= 0) {
                report.Info("Shop delivery service can deliver immediately because all delivery duration values are 0.", context);
            }

            if(service.FlatDeliveryFee <= 0f && service.PercentageDeliveryFee <= 0f) {
                report.Info("Shop delivery service has no delivery fee.", context);
            }

            if(!service.ChargeWalletOnOrder) {
                report.Info("Shop delivery service does not charge Wallet on order. It can be useful for free/event deliveries but bypasses normal payment.", context);
            }

            if(service.AllowCancellation && service.CancellationRefundPercent <= 0f) {
                report.Info("Shop delivery service allows cancellation but refunds no money.", context);
            }

            if(service.MaxLineCount > 0 && service.MaxBundleCount > 0 && service.MaxBundleCount < service.MaxLineCount) {
                report.Info("Delivery max bundle count is lower than max line count, so multi-line orders may be blocked unless every line has one bundle.", context);
            }

            ValidateObjectList(service.Requirements, report, context, "Shop delivery service has a null requirement slot.");
        }
    }

    static void ValidateLearnableOffers(ProjectValidationReport report) {
        foreach(var offer in ProjectValidatorAssetFinder.FindAssets<LearnableOfferDefinition>()) {
            if(offer == null) continue;

            string context = $"LearnableOffer/{offer.name}";
            if(string.IsNullOrWhiteSpace(offer.Id)) {
                report.Error("Learnable offer id is empty.", context);
            }

            if(offer.Tags != null && offer.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Learnable offer has an empty tag slot.", context);
            }

            if(offer.BasePrice <= 0f && offer.RepeatMode == ConsequenceChainRepeatMode.Unlimited) {
                report.Info("Free unlimited learnable offer can be purchased repeatedly unless reward ownership blocks it.", context);
            }

            if(offer.RepeatMode == ConsequenceChainRepeatMode.CooldownHours && offer.CooldownHours <= 0) {
                report.Warning("Learnable offer uses Cooldown Hours repeat mode but cooldown is 0.", context);
            }

            bool hasAnyGrant =
                (offer.RecipeGrants != null && offer.RecipeGrants.Any(grant => grant != null && grant.recipe != null))
                || (offer.PokemonKnowledgeGrants != null && offer.PokemonKnowledgeGrants.Any(grant => grant != null && grant.pokemon != null))
                || (offer.RegionsToDiscover != null && offer.RegionsToDiscover.Any(entry => entry != null))
                || (offer.PokeNavEntriesToDiscover != null && offer.PokeNavEntriesToDiscover.Any(entry => entry != null))
                || (offer.SocialPostsToUnlock != null && offer.SocialPostsToUnlock.Any(entry => entry != null))
                || (offer.MapMarkersToDiscover != null && offer.MapMarkersToDiscover.Any(entry => entry != null))
                || (offer.TitleGrants != null && offer.TitleGrants.Any(entry => entry != null && entry.title != null))
                || (offer.MilestonesToComplete != null && offer.MilestonesToComplete.Any(entry => entry != null))
                || (offer.ResearchProgressGrants != null && offer.ResearchProgressGrants.Any(entry => entry != null && entry.subject != null));

            if(!hasAnyGrant) {
                report.Warning("Learnable offer has no recipe, knowledge, map, title, milestone or research grant.", context);
            }

            ValidateObjectList(offer.RecipeGrants?.Select(grant => grant != null ? grant.recipe : null), report, context, "Learnable offer has a null recipe grant slot.");
            ValidateObjectList(offer.PokemonKnowledgeGrants?.Select(grant => grant != null ? grant.pokemon : null), report, context, "Learnable offer has a null Pokemon knowledge grant slot.");
            ValidateObjectList(offer.RegionsToDiscover, report, context, "Learnable offer has a null region grant slot.");
            ValidateObjectList(offer.PokeNavEntriesToDiscover, report, context, "Learnable offer has a null PokeNav entry grant slot.");
            ValidateObjectList(offer.SocialPostsToUnlock, report, context, "Learnable offer has a null social post grant slot.");
            ValidateObjectList(offer.MapMarkersToDiscover, report, context, "Learnable offer has a null map marker grant slot.");
            ValidateObjectList(offer.TitleGrants?.Select(grant => grant != null ? grant.title : null), report, context, "Learnable offer has a null title grant slot.");
            ValidateObjectList(offer.MilestonesToComplete, report, context, "Learnable offer has a null milestone grant slot.");
            ValidateObjectList(offer.ResearchProgressGrants?.Select(grant => grant != null ? grant.subject : null), report, context, "Learnable offer has a null research grant slot.");
            ValidateObjectList(offer.Requirements, report, context, "Learnable offer has a null requirement slot.");
        }
    }

    static void ValidateLoyaltyPrograms(ProjectValidationReport report) {
        foreach(var program in ProjectValidatorAssetFinder.FindAssets<LoyaltyProgramDefinition>()) {
            if(program == null) continue;

            string context = $"LoyaltyProgram/{program.name}";
            if(string.IsNullOrWhiteSpace(program.Id)) {
                report.Error("Loyalty program id is empty.", context);
            }

            if(program.Tags != null && program.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Loyalty program has an empty tag slot.", context);
            }

            if(program.RequiredCatalogTags != null && program.RequiredCatalogTags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Loyalty program has an empty required catalog tag slot.", context);
            }

            if(program.RequiredItemModelTags != null && program.RequiredItemModelTags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Loyalty program has an empty required item model tag slot.", context);
            }

            if(program.ItemBrandFilter != null || program.FilterItemModelCategory || (program.RequiredItemModelTags != null && program.RequiredItemModelTags.Count > 0)) {
                report.Info("Loyalty item filters are model-backed. Direct itemOverride shop offers without ItemModelDefinition will not match these brand/category/tag rules.", context);
            }

            if(program.Expires && program.DefaultDurationHours <= 0) {
                report.Warning("Loyalty program expires but has no duration.", context);
            }

            if(program.AutoJoinOnFirstPointGain && program.JoinCost > 0f) {
                report.Warning("Loyalty program auto-joins on point gain but has a join cost. Auto-join only works safely for free programs.", context);
            }

            if(program.GrantMode == LoyaltyProgramGrantMode.RefreshExistingOnly) {
                report.Info("Loyalty program can only refresh an already owned membership.", context);
            }

            bool hasPointRule = program.EarnFromShopPurchases
                && (program.PointsPerMoneySpent > 0f || program.FlatPointsPerPurchase > 0 || program.PointsPerPurchasedBundle > 0);
            hasPointRule |= program.EarnFromShopSells
                && (program.PointsPerMoneySold > 0f || program.FlatPointsPerSell > 0);

            if(!hasPointRule && (program.Tiers == null || program.Tiers.Count == 0)) {
                report.Info("Loyalty program has no point earning rules and no tiers. It can still be used as an access flag.", context);
            }

            ValidateObjectList(program.JoinRequirements, report, context, "Loyalty program has a null join requirement slot.");

            var tierIds = new HashSet<string>();
            foreach(var tier in program.Tiers) {
                if(tier == null) {
                    report.Warning("Loyalty program has a null tier slot.", context);
                    continue;
                }

                if(string.IsNullOrWhiteSpace(tier.TierId)) {
                    report.Warning("Loyalty tier id is empty.", context);
                } else if(!tierIds.Add(tier.TierId)) {
                    report.Warning($"Loyalty tier id '{tier.TierId}' is duplicated in this program.", context);
                }

                if(Mathf.Approximately(tier.BuyPriceMultiplier, 1f)
                    && Mathf.Approximately(tier.SellPriceMultiplier, 1f)
                    && Mathf.Approximately(tier.PointEarnMultiplier, 1f)
                    && (tier.TitleGrants == null || !tier.TitleGrants.Any(grant => grant != null && grant.title != null))
                    && (tier.MilestonesToComplete == null || !tier.MilestonesToComplete.Any(milestone => milestone != null))
                    && (tier.ReputationChanges == null || !tier.ReputationChanges.Any(change => change != null && change.faction != null && change.amount != 0))
                    && (tier.LifestylePointGrants == null || !tier.LifestylePointGrants.Any(grant => grant != null && grant.lifestyle != null && grant.points != 0))) {
                    report.Info($"Loyalty tier '{tier.DisplayName}' has no visible benefit yet.", context);
                }

                foreach(var titleGrant in tier.TitleGrants) {
                    if(titleGrant != null && titleGrant.title == null) {
                        report.Warning($"Loyalty tier '{tier.DisplayName}' has a title grant without a title.", context);
                    }
                }

                foreach(var reputationChange in tier.ReputationChanges) {
                    if(reputationChange != null && reputationChange.faction == null && reputationChange.amount != 0) {
                        report.Warning($"Loyalty tier '{tier.DisplayName}' has a reputation change without a faction.", context);
                    }
                }

                foreach(var lifestyleGrant in tier.LifestylePointGrants) {
                    if(lifestyleGrant != null && lifestyleGrant.lifestyle == null && lifestyleGrant.points != 0) {
                        report.Warning($"Loyalty tier '{tier.DisplayName}' has a lifestyle point grant without a lifestyle.", context);
                    }
                }
            }
        }
    }

    static void ValidateSponsors(ProjectValidationReport report) {
        foreach(var sponsor in ProjectValidatorAssetFinder.FindAssets<SponsorDefinition>()) {
            if(sponsor == null) continue;

            string context = $"Sponsor/{sponsor.name}";
            if(string.IsNullOrWhiteSpace(sponsor.Id)) {
                report.Error("Sponsor id is empty.", context);
            }

            if(sponsor.Tags != null && sponsor.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Sponsor has an empty tag slot.", context);
            }

            if(sponsor.RequiredCatalogTags != null && sponsor.RequiredCatalogTags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Sponsor has an empty required catalog tag slot.", context);
            }

            if(sponsor.RequiredItemModelTags != null && sponsor.RequiredItemModelTags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Sponsor has an empty required item model tag slot.", context);
            }

            if(sponsor.Expires && sponsor.DefaultDurationHours <= 0) {
                report.Warning("Sponsor expires but has no duration.", context);
            }

            if(sponsor.GrantMode == SponsorGrantMode.RefreshExistingOnly) {
                report.Info("Sponsor can only refresh an already owned sponsorship.", context);
            }

            if(Mathf.Approximately(sponsor.BuyPriceMultiplier, 1f)
                && Mathf.Approximately(sponsor.SellPriceMultiplier, 1f)
                && Mathf.Approximately(sponsor.PrizeMoneyMultiplier, 1f)
                && sponsor.SponsorPointsOnGrant <= 0) {
                report.Info("Sponsor has no visible benefit yet. It can still be used as an access/requirement flag.", context);
            }

            ValidateObjectList(sponsor.GrantRequirements, report, context, "Sponsor has a null grant requirement slot.");
        }
    }

    static void ValidateServices(ProjectValidationReport report) {
        foreach(var service in ProjectValidatorAssetFinder.FindAssets<ServiceDefinition>()) {
            if(service == null) continue;

            string context = $"Service/{service.name}";
            if(string.IsNullOrWhiteSpace(service.Id)) {
                report.Error("Service id is empty.", context);
            }

            if(service.Tags != null && service.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Service has an empty tag slot.", context);
            }

            if(service.RepeatMode == ConsequenceChainRepeatMode.CooldownHours && service.CooldownHours <= 0) {
                report.Warning("Service uses Cooldown Hours repeat mode but cooldown is 0.", context);
            }

            if(ServiceHasPokemonEffect(service) && service.PokemonTargetMode == ServicePokemonTargetMode.None) {
                report.Warning("Service has Pokemon effects but Pokemon Target Mode is None.", context);
            }

            ValidateObjectList(service.Requirements, report, context, "Service has a null requirement slot.");
            ValidateObjectList(service.MilestonesToComplete, report, context, "Service has a null milestone slot.");
            ValidateObjectList(service.CompletedChains, report, context, "Service has a null completed consequence chain slot.");
            ValidateObjectList(service.BlockedChains, report, context, "Service has a null blocked consequence chain slot.");

            foreach(var needChange in service.NeedChanges) {
                if(needChange != null && needChange.need == null && needChange.amount != 0) {
                    report.Warning("Service need change has amount but no survival need.", context);
                }
            }

            foreach(var titleGrant in service.TitleGrants) {
                if(titleGrant != null && titleGrant.title == null) {
                    report.Warning("Service has a title grant without a title.", context);
                }
            }

            foreach(var reputationChange in service.ReputationChanges) {
                if(reputationChange != null && reputationChange.faction == null && reputationChange.amount != 0) {
                    report.Warning("Service has a reputation change without a faction.", context);
                }
            }

            foreach(var relationshipChange in service.RelationshipChanges) {
                if(relationshipChange != null && relationshipChange.subject == null && relationshipChange.amount != 0) {
                    report.Warning("Service has a relationship change without a subject.", context);
                }
            }

            foreach(var lifestyleGrant in service.LifestylePointGrants) {
                if(lifestyleGrant != null && lifestyleGrant.lifestyle == null && lifestyleGrant.points != 0) {
                    report.Warning("Service has a lifestyle point grant without a lifestyle.", context);
                }
            }

            foreach(var careerGrant in service.CareerPointGrants) {
                if(careerGrant != null && careerGrant.career == null && careerGrant.points > 0) {
                    report.Warning("Service has a career point grant without a career.", context);
                }
            }

            ValidateLifePathRewards(service.LifePathRewards, report, context);

            foreach(var membershipGrant in service.OrganizationMembershipGrants) {
                if(membershipGrant != null && membershipGrant.organization == null) {
                    report.Warning("Service has an organization membership grant without an organization.", context);
                }
            }

            foreach(var pointGrant in service.OrganizationPointGrants) {
                if(pointGrant != null && pointGrant.organization == null && pointGrant.points > 0) {
                    report.Warning("Service has an organization point grant without an organization.", context);
                }
            }
        }

        foreach(var provider in ProjectValidatorAssetFinder.FindAssets<ServiceProvider>()) {
            if(provider == null) continue;

            string context = $"ServiceProvider/{provider.name}";
            if(provider.Service == null) {
                report.Warning("Service provider has no service definition assigned.", context);
            }
        }
    }

    static bool ServiceHasPokemonEffect(ServiceDefinition service) {
        return service != null
            && (service.HealPokemonToFull
                || service.PokemonHpHeal > 0
                || service.CurePokemonStatus
                || service.CurePokemonVolatileStatus
                || service.PokemonExperience > 0
                || service.PokemonCareAction != null);
    }

    static void ValidateServicePackages(ProjectValidationReport report) {
        foreach(var package in ProjectValidatorAssetFinder.FindAssets<ServicePackageDefinition>()) {
            if(package == null) continue;

            string context = $"ServicePackage/{package.name}";
            if(string.IsNullOrWhiteSpace(package.Id)) {
                report.Error("Service package id is empty.", context);
            }

            if(package.Tags != null && package.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Service package has an empty tag slot.", context);
            }

            if(package.BasePrice <= 0f && package.RepeatMode == ConsequenceChainRepeatMode.Unlimited) {
                report.Info("Free unlimited service package can be used repeatedly unless included services/offers block it.", context);
            }

            if(package.RepeatMode == ConsequenceChainRepeatMode.CooldownHours && package.CooldownHours <= 0) {
                report.Warning("Service package uses Cooldown Hours repeat mode but cooldown is 0.", context);
            }

            bool hasAnyEffect =
                (package.Services != null && package.Services.Any(entry => entry != null && entry.Service != null))
                || (package.LearnableOffers != null && package.LearnableOffers.Any(entry => entry != null && entry.Offer != null))
                || (package.TitleGrants != null && package.TitleGrants.Any(entry => entry != null && entry.title != null))
                || (package.MilestonesToComplete != null && package.MilestonesToComplete.Any(entry => entry != null))
                || (package.ReputationChanges != null && package.ReputationChanges.Any(entry => entry != null && entry.faction != null && entry.amount != 0))
                || (package.RelationshipChanges != null && package.RelationshipChanges.Any(entry => entry != null && entry.subject != null && entry.amount != 0))
                || (package.LifestylePointGrants != null && package.LifestylePointGrants.Any(entry => entry != null && entry.lifestyle != null && entry.points != 0))
                || (package.CareerPointGrants != null && package.CareerPointGrants.Any(entry => entry != null && entry.career != null && entry.points > 0))
                || (package.LifePathRewards != null && package.LifePathRewards.Any(entry => entry != null && entry.lifePath != null && entry.HasAnyPayload))
                || (package.OrganizationMembershipGrants != null && package.OrganizationMembershipGrants.Any(entry => entry != null && entry.organization != null))
                || (package.OrganizationPointGrants != null && package.OrganizationPointGrants.Any(entry => entry != null && entry.organization != null && entry.points > 0))
                || (package.CompletedChains != null && package.CompletedChains.Any(entry => entry != null));

            if(!hasAnyEffect) {
                report.Warning("Service package has no services, learnable offers, rewards or consequences.", context);
            }

            bool hasOptionalPaidEntry =
                (package.Services != null && package.Services.Any(entry => entry != null && !entry.Required && entry.Service != null && entry.Service.MoneyCost > 0f))
                || (package.LearnableOffers != null && package.LearnableOffers.Any(entry => entry != null && !entry.Required && entry.Offer != null && entry.Offer.BasePrice > 0f));
            if(hasOptionalPaidEntry && !package.CheckCombinedPriceBeforeApplying) {
                report.Info("Service package has optional paid entries and combined price precheck is disabled. Optional entries may be skipped when the player only affords the required package flow.", context);
            }

            ValidateObjectList(package.Services?.Select(entry => entry != null ? entry.Service : null), report, context, "Service package has a null service entry.");
            ValidateObjectList(package.LearnableOffers?.Select(entry => entry != null ? entry.Offer : null), report, context, "Service package has a null learnable offer entry.");
            ValidateObjectList(package.Requirements, report, context, "Service package has a null requirement slot.");
            ValidateObjectList(package.TitleGrants?.Select(grant => grant != null ? grant.title : null), report, context, "Service package has a null title grant slot.");
            ValidateObjectList(package.MilestonesToComplete, report, context, "Service package has a null milestone slot.");
            ValidateObjectList(package.CompletedChains, report, context, "Service package has a null completed consequence chain slot.");
            ValidateObjectList(package.BlockedChains, report, context, "Service package has a null blocked consequence chain slot.");

            foreach(var entry in package.Services) {
                if(entry == null || entry.Service == null) {
                    continue;
                }

                if(entry.TimesToUse > 1 && entry.Service.RepeatMode != ConsequenceChainRepeatMode.Unlimited) {
                    report.Warning($"Service package runs '{entry.Service.DisplayName}' multiple times, but the service repeat mode is {entry.Service.RepeatMode}.", context);
                }

                if(package.BasePrice > 0f && entry.Service.MoneyCost > 0f) {
                    report.Info($"Service package and included service '{entry.Service.DisplayName}' both have prices. This is valid, but the player will pay both.", context);
                }
            }

            foreach(var titleGrant in package.TitleGrants) {
                if(titleGrant != null && titleGrant.title == null) {
                    report.Warning("Service package has a title grant without a title.", context);
                }
            }

            foreach(var reputationChange in package.ReputationChanges) {
                if(reputationChange != null && reputationChange.faction == null && reputationChange.amount != 0) {
                    report.Warning("Service package has a reputation change without a faction.", context);
                }
            }

            foreach(var relationshipChange in package.RelationshipChanges) {
                if(relationshipChange != null && relationshipChange.subject == null && relationshipChange.amount != 0) {
                    report.Warning("Service package has a relationship change without a subject.", context);
                }
            }

            foreach(var lifestyleGrant in package.LifestylePointGrants) {
                if(lifestyleGrant != null && lifestyleGrant.lifestyle == null && lifestyleGrant.points != 0) {
                    report.Warning("Service package has a lifestyle point grant without a lifestyle.", context);
                }
            }

            foreach(var careerGrant in package.CareerPointGrants) {
                if(careerGrant != null && careerGrant.career == null && careerGrant.points > 0) {
                    report.Warning("Service package has a career point grant without a career.", context);
                }
            }

            ValidateLifePathRewards(package.LifePathRewards, report, context);

            foreach(var membershipGrant in package.OrganizationMembershipGrants) {
                if(membershipGrant != null && membershipGrant.organization == null) {
                    report.Warning("Service package has an organization membership grant without an organization.", context);
                }
            }

            foreach(var pointGrant in package.OrganizationPointGrants) {
                if(pointGrant != null && pointGrant.organization == null && pointGrant.points > 0) {
                    report.Warning("Service package has an organization point grant without an organization.", context);
                }
            }
        }
    }

    static void ValidateServiceAppointments(ProjectValidationReport report) {
        foreach(var appointment in ProjectValidatorAssetFinder.FindAssets<ServiceAppointmentDefinition>()) {
            if(appointment == null) continue;

            string context = $"ServiceAppointment/{appointment.name}";
            if(string.IsNullOrWhiteSpace(appointment.Id)) {
                report.Error("Service appointment id is empty.", context);
            }

            if(appointment.Tags != null && appointment.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Service appointment has an empty tag slot.", context);
            }

            if(appointment.RequiredProviderTags != null && appointment.RequiredProviderTags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Service appointment has an empty required provider tag slot.", context);
            }

            if(appointment.PayloadMode == ServiceAppointmentPayloadMode.Service && appointment.Service == null) {
                report.Warning("Service appointment payload mode is Service but no service is assigned.", context);
            }

            if(appointment.PayloadMode == ServiceAppointmentPayloadMode.ServicePackage && appointment.ServicePackage == null) {
                report.Warning("Service appointment payload mode is Service Package but no service package is assigned.", context);
            }

            if(appointment.PayloadMode == ServiceAppointmentPayloadMode.None && !appointment.AllowEmptyCompletion) {
                report.Info("Service appointment has no completion payload. This is valid for pure reservations/reminders, but it will not apply gameplay effects.", context);
            }

            if(appointment.ScheduleMode == ServiceAppointmentScheduleMode.ManualOnly) {
                report.Info("Service appointment uses Manual Only schedule. Sources should request explicit day/hour values.", context);
            }

            if(appointment.ScheduleMode == ServiceAppointmentScheduleMode.CalendarEventScheduled && appointment.CalendarEvent == null) {
                report.Warning("Service appointment uses Calendar Event Scheduled mode but no calendar event is assigned.", context);
            }

            if(appointment.EarliestHour > appointment.LatestHour) {
                report.Warning("Service appointment earliest hour is later than latest hour; no slot can be resolved.", context);
            }

            if(appointment.MaxLookAheadDays <= 0) {
                report.Info("Service appointment automatic slot search is limited to the current day.", context);
            }

            if(appointment.MaxBookingsPerSlot == 0) {
                report.Info("Service appointment allows unlimited bookings in the same slot.", context);
            }

            if(appointment.MaxPendingPerPlayer == 0) {
                report.Info("Service appointment allows unlimited pending bookings per player.", context);
            }

            if(appointment.CompletionMode == ServiceAppointmentCompletionMode.AutoCompleteWhenDue
                && appointment.PayloadMode != ServiceAppointmentPayloadMode.None
                && appointment.BookingFee <= 0f) {
                report.Info("Auto-complete appointment may still charge service/package cost at completion. Make the payload free or use booking fee if this should be prepaid.", context);
            }

            if(appointment.CompletionMode == ServiceAppointmentCompletionMode.ClaimAtProvider && !appointment.RequireSameProviderForClaim) {
                report.Info("Claim appointment can be completed by any matching source because same-provider claim is disabled.", context);
            }

            if(appointment.BookingFee > 0f && !appointment.ChargeWalletOnBooking) {
                report.Info("Service appointment has a booking fee but does not charge wallet on booking.", context);
            }

            if(appointment.AllowCancellation && appointment.CancellationRefundPercent <= 0f && appointment.BookingFee > 0f) {
                report.Info("Service appointment can be cancelled but refunds none of the booking fee.", context);
            }

            ValidateObjectList(appointment.Requirements, report, context, "Service appointment has a null requirement slot.");
        }
    }

    static void ValidateEncounterTables(ProjectValidationReport report) {
        foreach(var table in ProjectValidatorAssetFinder.FindAssets<EncounterTableDefinition>()) {
            if(table == null) continue;

            string context = $"EncounterTable/{table.name}";
            if(string.IsNullOrWhiteSpace(table.Id)) {
                report.Error("Encounter table id is empty.", context);
            }

            if(table.Entries == null || table.Entries.Count == 0) {
                report.Warning("Encounter table has no entries.", context);
            }

            int totalWeight = 0;
            foreach(var entry in table.Entries) {
                if(entry == null) {
                    report.Warning("Encounter table has a null entry slot.", context);
                    continue;
                }

                if(entry.Pokemon == null && entry.Weight > 0) {
                    report.Warning("Encounter entry has weight but no Pokemon.", context);
                }

                totalWeight += entry.Weight;
            }

            if(totalWeight <= 0) {
                report.Warning("Encounter table has no positive encounter weights.", context);
            }
        }
    }

    static void ValidateEncounterSourceProfiles(ProjectValidationReport report) {
        foreach(var profile in ProjectValidatorAssetFinder.FindAssets<EncounterSourceProfileDefinition>()) {
            if(profile == null) continue;

            string context = $"EncounterSourceProfile/{profile.name}";
            if(string.IsNullOrWhiteSpace(profile.Id)) {
                report.Error("Encounter source profile id is empty.", context);
            }

            if(profile.EncounterTable == null) {
                report.Error("Encounter source profile has no encounter table.", context);
            }

            if(profile.ChanceMultiplier <= 0f) {
                report.Info("Encounter source profile has a zero chance multiplier and will never roll unless changed.", context);
            }

            if((profile.OutcomeMode == EncounterSourceOutcomeMode.TryStealthCapture
                || profile.OutcomeMode == EncounterSourceOutcomeMode.TryStealthCaptureOnly)
                && profile.StealthCaptureProfile == null) {
                report.Warning("Encounter source profile uses a stealth outcome but has no stealth capture profile.", context);
            }

            if(profile.OutcomeMode == EncounterSourceOutcomeMode.TryStealthCaptureOnly
                && profile.StealthCaptureProfile != null
                && profile.StealthCaptureProfile.StartBattleOnFailure) {
                report.Info("Stealth-only source suppresses battle on failure even though the stealth profile normally starts battles.", context);
            }

            if(profile.RequireActiveZoneType && profile.RequiredZoneType == ActivityZoneType.General) {
                report.Info("Encounter source requires the General zone type. Confirm this is intentional and not meant to be Wild.", context);
            }

            ValidateObjectList(profile.Requirements, report, context, "Encounter source profile has a null requirement slot.");
        }
    }

    static void ValidateStealthCaptureProfiles(ProjectValidationReport report) {
        foreach(var profile in ProjectValidatorAssetFinder.FindAssets<StealthCaptureProfileDefinition>()) {
            if(profile == null) continue;

            string context = $"StealthCapture/{profile.name}";
            if(string.IsNullOrWhiteSpace(profile.Id)) {
                report.Error("Stealth capture profile id is empty.", context);
            }

            if(profile.ConsumePokeball && string.IsNullOrWhiteSpace(profile.NoPokeballMessage)) {
                report.Warning("Stealth capture profile consumes Pokeballs but has no missing Pokeball message.", context);
            }
        }
    }

    static void ValidateEncounterResolutions(ProjectValidationReport report) {
        foreach(var resolution in ProjectValidatorAssetFinder.FindAssets<EncounterResolutionDefinition>()) {
            if(resolution == null) continue;

            string context = $"EncounterResolution/{resolution.name}";
            if(string.IsNullOrWhiteSpace(resolution.Id)) {
                report.Error("Encounter resolution id is empty.", context);
            }

            if(resolution.BaseChancePercent <= 0f && resolution.SuccessOutcome != EncounterResolutionOutcome.NoEffect) {
                report.Info("Encounter resolution has zero base chance, so it will need positive modifiers to succeed.", context);
            }

            if(resolution.SuccessOutcome == EncounterResolutionOutcome.CapturePokemon && resolution.Kind != EncounterResolutionKind.Capture) {
                report.Info("Encounter resolution captures Pokemon but its kind is not Capture. This can be intentional for custom bait/calm captures.", context);
            }

            ValidateObjectList(resolution.Requirements, report, context, "Encounter resolution has a null requirement slot.");
            ValidateObjectList(resolution.SuccessOutcomes, report, context, "Encounter resolution has a null success outcome slot.");
            ValidateObjectList(resolution.FailureOutcomes, report, context, "Encounter resolution has a null failure outcome slot.");

            foreach(var cost in resolution.ItemCosts) {
                if(cost != null && cost.item == null && cost.count > 0) {
                    report.Warning("Encounter resolution item cost has count but no item.", context);
                }
            }
        }

        foreach(var source in ProjectValidatorAssetFinder.FindAssets<EncounterResolutionSource>()) {
            if(source == null) continue;

            string context = $"EncounterResolutionSource/{source.name}";
            if(source.Resolution == null) {
                report.Warning("Encounter resolution source has no resolution definition.", context);
            }

            if(source.Pokemon == null && source.EncounterTable == null) {
                report.Warning("Encounter resolution source has neither exact Pokemon nor encounter table.", context);
            }
        }
    }

    static void ValidateEncounterResolutionChoiceSets(ProjectValidationReport report) {
        foreach(var choiceSet in ProjectValidatorAssetFinder.FindAssets<EncounterResolutionChoiceSetDefinition>()) {
            if(choiceSet == null) continue;

            string context = $"EncounterResolutionChoiceSet/{choiceSet.name}";
            if(string.IsNullOrWhiteSpace(choiceSet.Id)) {
                report.Error("Encounter resolution choice set id is empty.", context);
            }

            if(choiceSet.Choices == null || choiceSet.Choices.Count == 0) {
                report.Warning("Encounter resolution choice set has no choices.", context);
            }

            var duplicateChoices = choiceSet.Choices
                .Where(choice => choice != null)
                .GroupBy(choice => choice.ChoiceId)
                .Where(group => !string.IsNullOrWhiteSpace(group.Key) && group.Count() > 1);

            foreach(var duplicate in duplicateChoices) {
                report.Warning($"Encounter resolution choice set has duplicate choice id '{duplicate.Key}'.", context);
            }

            foreach(var choice in choiceSet.Choices) {
                if(choice == null) {
                    report.Warning("Encounter resolution choice set has a null choice slot.", context);
                    continue;
                }

                if(choice.Resolution == null) {
                    report.Warning("Encounter resolution choice has no resolution definition.", context);
                }
            }
        }

        foreach(var source in ProjectValidatorAssetFinder.FindAssets<EncounterResolutionChoiceSource>()) {
            if(source == null) continue;

            string context = $"EncounterResolutionChoiceSource/{source.name}";
            if(source.ChoiceSet == null) {
                report.Warning("Encounter resolution choice source has no choice set.", context);
            }

            if(source.Pokemon == null && source.EncounterTable == null) {
                report.Warning("Encounter resolution choice source has neither exact Pokemon nor encounter table.", context);
            }
        }
    }

    static void ValidateEncounterSources(ProjectValidationReport report) {
        foreach(var source in ProjectValidatorAssetFinder.FindAssets<EncounterSource>()) {
            if(source == null) continue;

            string context = $"EncounterSource/{source.name}";
            if(source.Profile == null) {
                report.Warning("Encounter source has no profile assigned.", context);
            }

            if(!source.TriggerOnTouch && !source.InteractOnUse) {
                report.Warning("Encounter source cannot be activated because both touch and interact activation are disabled.", context);
            }

            if(source.RealTimeCooldownSeconds <= 0f && source.TriggerRepeatedly) {
                report.Info("Repeated encounter source has no cooldown.", context);
            }

            if(source.PreferStealthOnInteract && source.Profile != null && source.Profile.StealthCaptureProfile == null) {
                report.Info("Encounter source prefers stealth on interact, but its profile has no stealth capture profile.", context);
            }
        }
    }

    static void ValidateOverworldEncounterPaths(ProjectValidationReport report) {
        foreach(var node in ProjectValidatorAssetFinder.FindAssets<OverworldEncounterNode>()) {
            if(node == null) continue;

            string context = $"OverworldEncounterNode/{node.name}";
            if(string.IsNullOrWhiteSpace(node.NodeId)) {
                report.Error("Overworld encounter node id is empty.", context);
            }

            if(node.Connections == null || node.Connections.Count == 0) {
                report.Info("Overworld encounter node has no outgoing connections. This is valid for nests/endpoints, but random-connected agents may stop here.", context);
            } else {
                foreach(var connection in node.Connections) {
                    if(connection == null) {
                        report.Warning("Overworld encounter node has a null connection slot.", context);
                        continue;
                    }

                    if(connection.Node == null && connection.Weight > 0) {
                        report.Warning("Overworld encounter node connection has weight but no destination node.", context);
                    }

                    if(connection.Node != null && connection.Weight <= 0 && connection.Enabled) {
                        report.Info("Overworld encounter node connection is enabled but has zero weight.", context);
                    }

                    if(connection.Node == node) {
                        report.Info("Overworld encounter node connection points back to itself.", context);
                    }

                    if(connection.Enabled && (connection.Flags & OverworldEncounterConnectionFlags.BlockedByDefault) != 0) {
                        report.Info("Overworld encounter node connection is enabled but blocked by default.", context);
                    }

                    if((connection.RequiredCapabilities & connection.BlockedCapabilities) != 0) {
                        report.Warning("Overworld encounter node connection requires and blocks the same movement capability.", context);
                    }
                }
            }

            var allActorBlocks = OverworldEncounterNodeFlags.NoPlayer | OverworldEncounterNodeFlags.NoNPC | OverworldEncounterNodeFlags.NoPokemon;
            if((node.NodeFlags & allActorBlocks) == allActorBlocks) {
                report.Warning("Overworld encounter node blocks players, NPCs and Pokemon. It will only be usable by generic/custom logic.", context);
            }

            ValidateObjectList(node.Requirements, report, context, "Overworld encounter node has a null requirement slot.");
        }

        foreach(var group in ProjectValidatorAssetFinder.FindAssets<OverworldEncounterNodeGroup>()) {
            if(group == null) continue;

            string context = $"OverworldEncounterNodeGroup/{group.name}";
            if(group.Nodes == null || group.Nodes.Count == 0) {
                report.Info("Overworld encounter node group has no nodes. It may populate child nodes at runtime if Include Child Nodes is enabled.", context);
            } else {
                ValidateObjectList(group.Nodes, report, context, "Overworld encounter node group has a null node slot.");
            }
        }

        foreach(var agent in ProjectValidatorAssetFinder.FindAssets<OverworldEncounterPathAgent>()) {
            if(agent == null) continue;

            string context = $"OverworldEncounterPathAgent/{agent.name}";
            bool hasGraph = agent.NodeGroup != null || agent.StartNode != null || (agent.PatrolNodes != null && agent.PatrolNodes.Any(node => node != null));
            if(!hasGraph) {
                report.Warning("Overworld encounter path agent has no node group, start node or patrol nodes.", context);
            }

            if((agent.PathMode == OverworldEncounterPathMode.PatrolList || agent.PathMode == OverworldEncounterPathMode.PingPongPatrol)
                && (agent.PatrolNodes == null || agent.PatrolNodes.Count(node => node != null) == 0)) {
                report.Warning("Overworld encounter path agent uses a patrol mode but has no patrol nodes.", context);
            }

            if(agent.PatrolNodes != null && agent.PatrolNodes.Any(node => node == null)) {
                report.Warning("Overworld encounter path agent has a null patrol node slot.", context);
            }

            if(agent.PathMode == OverworldEncounterPathMode.RandomConnectedNode && agent.StartNode == null && agent.NodeGroup == null) {
                report.Info("Random-connected encounter path agent has no start node or node group, so it cannot choose an initial node.", context);
            }

            if(agent.PathMode != OverworldEncounterPathMode.HoldPosition && agent.MovementCapabilities == OverworldMovementCapabilityFlags.None) {
                report.Warning("Moving overworld encounter path agent has no movement capabilities.", context);
            }

            if((agent.RequiredNodeFlags & agent.BlockedNodeFlags) != 0) {
                report.Warning("Overworld encounter path agent requires and blocks the same node flag.", context);
            }
        }

        foreach(var adapter in ProjectValidatorAssetFinder.FindAssets<OverworldNodeMovementAdapter>()) {
            if(adapter == null) continue;

            string context = $"OverworldNodeMovementAdapter/{adapter.name}";
            if(adapter.PathAgent == null) {
                report.Warning("Overworld node movement adapter has no path agent assigned or available on the same GameObject.", context);
            }

            if(adapter.NodeGroup == null) {
                report.Warning("Overworld node movement adapter has no node group. It cannot resolve a current node.", context);
            }

            if(adapter.MinDirectionMagnitude <= 0f) {
                report.Warning("Overworld node movement adapter has an invalid minimum direction magnitude.", context);
            }

            if(adapter.DirectionDotThreshold < 0f) {
                report.Info("Overworld node movement adapter accepts connections behind/sideways from the input direction because Direction Dot Threshold is below 0.", context);
            }
        }

        foreach(var inputBridge in ProjectValidatorAssetFinder.FindAssets<OverworldNodeMovementInputBridge>()) {
            if(inputBridge == null) continue;

            string context = $"OverworldNodeMovementInputBridge/{inputBridge.name}";
            if(inputBridge.MovementAdapter == null) {
                report.Warning("Overworld node movement input bridge has no movement adapter assigned or available on the same GameObject.", context);
            }

            if(inputBridge.Actions == null) {
                report.Warning("Overworld node movement input bridge has no InputActionAsset assigned.", context);
            }

            if(string.IsNullOrWhiteSpace(inputBridge.ActionMapName)) {
                report.Warning("Overworld node movement input bridge has an empty action map name.", context);
            }

            if(string.IsNullOrWhiteSpace(inputBridge.MoveActionName)) {
                report.Warning("Overworld node movement input bridge has an empty move action name.", context);
            }

            if(inputBridge.RepeatWhileHeld && inputBridge.RepeatDelaySeconds <= 0f) {
                report.Warning("Overworld node movement input bridge repeats while held but has an invalid repeat delay.", context);
            }
        }

        foreach(var debugSource in ProjectValidatorAssetFinder.FindAssets<OverworldEncounterDebugSource>()) {
            if(debugSource == null) continue;

            string context = $"OverworldEncounterDebugSource/{debugSource.name}";
            if(debugSource.NodeGroup == null) {
                report.Info("Overworld encounter debug source has no node group. It can still inspect local path agent or movement adapter references if assigned.", context);
            }

            if(debugSource.PathAgent == null && debugSource.MovementAdapter == null) {
                report.Info("Overworld encounter debug source has no path agent or movement adapter assigned.", context);
            }

            if(debugSource.TestGoalNode == null) {
                report.Info("Overworld encounter debug source has no test goal node. Snapshot still works, but Test Path needs a goal.", context);
            }
        }

        foreach(var flee in ProjectValidatorAssetFinder.FindAssets<OverworldEncounterFleeController>()) {
            if(flee == null) continue;

            string context = $"OverworldEncounterFleeController/{flee.name}";
            if(flee.PathAgent == null) {
                report.Warning("Overworld flee controller has no path agent assigned or available on the same GameObject.", context);
            }

            if(flee.NodeGroup == null) {
                report.Warning("Overworld flee controller has no node group. It cannot find escape candidates.", context);
            }

            if((flee.PreferredEscapeFlags & flee.BlockedEscapeFlags) != 0) {
                report.Warning("Overworld flee controller prefers and blocks the same escape flag.", context);
            }

            if(flee.RequiredEscapeTags != null && flee.RequiredEscapeTags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Overworld flee controller has an empty required escape tag slot.", context);
            }

            if(flee.BlockedEscapeTags != null && flee.BlockedEscapeTags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Overworld flee controller has an empty blocked escape tag slot.", context);
            }

            var requiredTags = flee.RequiredEscapeTags?.Where(tag => !string.IsNullOrWhiteSpace(tag)).ToList() ?? new List<string>();
            var blockedTags = flee.BlockedEscapeTags?.Where(tag => !string.IsNullOrWhiteSpace(tag)).ToList() ?? new List<string>();
            if(requiredTags.Any(required => blockedTags.Any(blocked => string.Equals(required, blocked, StringComparison.OrdinalIgnoreCase)))) {
                report.Warning("Overworld flee controller requires and blocks the same escape tag.", context);
            }

            if(string.IsNullOrWhiteSpace(flee.VirtualEntityId)) {
                report.Info("Overworld flee controller has no explicit virtual entity id, so it will use the GameObject name.", context);
            }
        }

        foreach(var fleeLog in ProjectValidatorAssetFinder.FindAssets<PlayerOverworldFleeLog>()) {
            if(fleeLog == null) continue;

            string context = $"PlayerOverworldFleeLog/{fleeLog.name}";
            var duplicateActiveIds = fleeLog.Records
                .Where(record => record != null && record.state == OverworldVirtualFleeState.Active)
                .GroupBy(record => record.entityId)
                .Where(group => !string.IsNullOrWhiteSpace(group.Key) && group.Count() > 1);

            foreach(var duplicate in duplicateActiveIds) {
                report.Warning($"Overworld flee log has multiple active records for entity '{duplicate.Key}'.", context);
            }
        }

        foreach(var recovery in ProjectValidatorAssetFinder.FindAssets<OverworldFleeRecoverySource>()) {
            if(recovery == null) continue;

            string context = $"OverworldFleeRecoverySource/{recovery.name}";
            if(recovery.FleeLog == null) {
                report.Info("Overworld flee recovery source has no flee log in the current scene. This is valid for prefabs, but scene instances need PlayerOverworldFleeLog.", context);
            }

            if(recovery.RecoveryAction == OverworldFleeRecoveryAction.SpawnPrefabAndMarkRecovered && recovery.RecoveredPrefab == null) {
                report.Warning("Overworld flee recovery source spawns a prefab but has no Recovered Prefab assigned.", context);
            }

            if(recovery.RecoveryAction == OverworldFleeRecoveryAction.EnableExistingObjectAndMarkRecovered && recovery.ExistingObject == null) {
                report.Warning("Overworld flee recovery source enables an existing object but has no Existing Object assigned.", context);
            }

            if(string.IsNullOrWhiteSpace(recovery.EscapeNodeIdFilter) && string.IsNullOrWhiteSpace(recovery.EntityIdFilter) && string.IsNullOrWhiteSpace(recovery.SpeciesIdFilter)) {
                report.Info("Overworld flee recovery source has broad filters and may recover the newest active flee record in the scene.", context);
            }
        }
    }

    static void ValidateCustomizationParts(ProjectValidationReport report) {
        foreach(var part in ProjectValidatorAssetFinder.FindAssets<CustomizationPartDefinition>()) {
            if(part == null) continue;

            string context = $"CustomizationPart/{part.name}";
            if(string.IsNullOrWhiteSpace(part.Id)) {
                report.Error("Customization part id is empty.", context);
            }

            if(!part.HasAnySprite()) {
                report.Warning("Customization part has no sprites assigned.", context);
            }
        }
    }

    static void ValidateCustomizationPresets(ProjectValidationReport report) {
        foreach(var preset in ProjectValidatorAssetFinder.FindAssets<CustomizationPresetDefinition>()) {
            if(preset == null) continue;

            string context = $"CustomizationPreset/{preset.name}";
            if(string.IsNullOrWhiteSpace(preset.Id)) {
                report.Error("Customization preset id is empty.", context);
            }

            if(preset.BaseVisualSet == null && (preset.DefaultParts == null || preset.DefaultParts.Count == 0)) {
                report.Warning("Customization preset has no base visual set and no default parts.", context);
            }

            var duplicateSlots = (preset.DefaultParts ?? new List<CustomizationPartDefinition>())
                .Where(part => part != null && part.ExclusiveInSlot)
                .GroupBy(part => part.Slot)
                .Where(group => group.Count() > 1);

            foreach(var slot in duplicateSlots) {
                report.Warning($"Customization preset has multiple exclusive default parts for slot '{slot.Key}'. The last one wins at runtime.", context);
            }

            foreach(var part in preset.DefaultParts) {
                if(part == null) {
                    report.Warning("Customization preset has a null default part slot.", context);
                }
            }
        }
    }

    static void ValidatePlayerOrigins(ProjectValidationReport report) {
        foreach(var origin in ProjectValidatorAssetFinder.FindAssets<PlayerOriginDefinition>()) {
            if(origin == null) continue;

            string context = $"PlayerOrigin/{origin.name}";
            if(string.IsNullOrWhiteSpace(origin.Id)) {
                report.Error("Player origin id is empty.", context);
            }

            ValidateObjectList(origin.Requirements, report, context, "Player origin has a null requirement slot.");
            ValidateObjectList(origin.TitleGrants.Select(grant => grant != null ? grant.title : null), report, context, "Player origin has a null title grant slot.");
            ValidateObjectList(origin.MilestonesToComplete, report, context, "Player origin has a null milestone slot.");
            ValidateObjectList(origin.CareersToUnlock, report, context, "Player origin has a null career unlock slot.");
            ValidateObjectList(origin.CareerPointGrants.Select(grant => grant != null ? grant.career : null), report, context, "Player origin has a null career point grant slot.");
            ValidateObjectList(origin.OrganizationMembershipGrants.Select(grant => grant != null ? grant.organization : null), report, context, "Player origin has a null organization membership grant slot.");
            ValidateObjectList(origin.OrganizationPointGrants.Select(grant => grant != null ? grant.organization : null), report, context, "Player origin has a null organization point grant slot.");
            ValidateObjectList(origin.ReputationChanges.Select(change => change != null ? change.faction : null), report, context, "Player origin has a null reputation change slot.");
            ValidateObjectList(origin.RelationshipChanges.Select(change => change != null ? change.subject : null), report, context, "Player origin has a null relationship change slot.");
            ValidateObjectList(origin.PokeNavEntries, report, context, "Player origin has a null PokeNav entry slot.");
            ValidateObjectList(origin.RegionsToDiscover, report, context, "Player origin has a null region slot.");
            ValidateObjectList(origin.SocialPostsToUnlock, report, context, "Player origin has a null social post slot.");
            ValidateObjectList(origin.MapMarkersToDiscover, report, context, "Player origin has a null map marker slot.");
            ValidateObjectList(origin.WorldDiscoveries, report, context, "Player origin has a null world discovery slot.");
            ValidateObjectList(origin.SelectedChains, report, context, "Player origin has a null selected consequence chain slot.");
            ValidateObjectList(origin.BlockedChains, report, context, "Player origin has a null blocked consequence chain slot.");

            foreach(var grant in origin.ItemGrants) {
                if(grant == null || grant.Item == null) {
                    report.Warning("Player origin has an empty item grant.", context);
                }
            }

            foreach(var grant in origin.PokemonGrants) {
                if(grant == null || grant.Pokemon == null) {
                    report.Warning("Player origin has an empty Pokemon grant.", context);
                }
            }

            foreach(var grant in origin.ToolGrants) {
                if(grant == null || grant.Tool == null) {
                    report.Warning("Player origin has an empty tool grant.", context);
                }
            }

            foreach(var grant in origin.RecipeGrants) {
                if(grant == null || !grant.IsValid) {
                    report.Warning("Player origin has an empty recipe grant.", context);
                }
            }

            foreach(var grant in origin.CareersToJoin) {
                if(grant == null || grant.Career == null) {
                    report.Warning("Player origin has an empty career join grant.", context);
                }
            }

            foreach(var grant in origin.ResearchGrants) {
                if(grant == null || grant.Subject == null) {
                    report.Warning("Player origin has an empty research grant.", context);
                }
            }
        }

        foreach(var source in ProjectValidatorAssetFinder.FindAssets<PlayerOriginSource>()) {
            if(source == null) continue;

            string context = $"PlayerOriginSource/{source.name}";
            if(source.Origin == null) {
                report.Warning("Player origin source has no origin assigned.", context);
            }
        }
    }

    static void ValidatePlayerLifestyles(ProjectValidationReport report) {
        foreach(var lifestyle in ProjectValidatorAssetFinder.FindAssets<PlayerLifestyleDefinition>()) {
            if(lifestyle == null) continue;

            string context = $"PlayerLifestyle/{lifestyle.name}";
            if(string.IsNullOrWhiteSpace(lifestyle.Id)) {
                report.Error("Player lifestyle id is empty.", context);
            }

            if(lifestyle.ActivityRules == null || lifestyle.ActivityRules.Count == 0) {
                report.Info("Player lifestyle has no activity scoring rules yet.", context);
            }

            foreach(var rule in lifestyle.ActivityRules) {
                if(rule == null) {
                    report.Warning("Player lifestyle has a null activity rule slot.", context);
                    continue;
                }

                if(rule.Mode == LifestyleActivityRuleMode.SpecificActivity && rule.Activity == null) {
                    report.Warning("Lifestyle specific activity rule has no activity assigned.", context);
                }

                if(rule.Mode == LifestyleActivityRuleMode.ActivityTag && string.IsNullOrWhiteSpace(rule.ActivityTag)) {
                    report.Warning("Lifestyle activity tag rule has no tag assigned.", context);
                }

                if(rule.Points == 0) {
                    report.Info("Lifestyle activity rule gives 0 points.", context);
                }
            }

            foreach(var rank in lifestyle.Ranks) {
                if(rank == null) {
                    report.Warning("Player lifestyle has a null rank slot.", context);
                }
            }
        }

        foreach(var source in ProjectValidatorAssetFinder.FindAssets<PlayerLifestyleSource>()) {
            if(source == null) continue;

            string context = $"PlayerLifestyleSource/{source.name}";
            if(source.Grants == null || source.Grants.Count == 0) {
                report.Warning("Player lifestyle source has no grants assigned.", context);
                continue;
            }

            foreach(var grant in source.Grants) {
                if(grant == null || grant.lifestyle == null || grant.points == 0) {
                    report.Warning("Player lifestyle source has an empty or zero point grant.", context);
                }
            }
        }
    }

    static void ValidateNewGameSetups(ProjectValidationReport report) {
        foreach(var setup in ProjectValidatorAssetFinder.FindAssets<NewGameSetupDefinition>()) {
            if(setup == null) continue;

            string context = $"NewGameSetup/{setup.name}";
            if(string.IsNullOrWhiteSpace(setup.Id)) {
                report.Error("New game setup id is empty.", context);
            }

            if(setup.Tags != null && setup.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("New game setup has an empty tag slot.", context);
            }

            ValidateObjectList(setup.Requirements, report, context, "New game setup has a null requirement slot.");

            if(setup.Origin == null) {
                report.Info("New game setup has no origin package. This is valid for UI-only presets, but it will not grant starter resources.", context);
            }

            if(setup.CustomizationParts != null && setup.CustomizationParts.Any(part => part == null)) {
                report.Warning("New game setup has a null customization part slot.", context);
            }

            if(setup.UnlockedBattleRuleSets != null && setup.UnlockedBattleRuleSets.Any(rule => rule == null)) {
                report.Warning("New game setup has a null battle rule set slot.", context);
            }

            if(setup.LifestyleGrants != null) {
                foreach(var grant in setup.LifestyleGrants) {
                    if(grant == null || grant.lifestyle == null || grant.points == 0) {
                        report.Warning("New game setup has an empty or zero point lifestyle grant.", context);
                    }
                }
            }

            if(setup.Origin == null && setup.CustomizationPreset == null && setup.BattleMode == null
                && (setup.CustomizationParts == null || setup.CustomizationParts.Count == 0)
                && (setup.UnlockedBattleRuleSets == null || setup.UnlockedBattleRuleSets.Count == 0)
                && (setup.LifestyleGrants == null || setup.LifestyleGrants.Count == 0)) {
                report.Info("New game setup currently has no gameplay/customization payload.", context);
            }
        }

        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<NewGameSetupDefinition>(), report, "NewGameSetup", setup => setup.Id);

        foreach(var source in ProjectValidatorAssetFinder.FindAssets<NewGameSetupSource>()) {
            if(source == null) continue;

            string context = $"NewGameSetupSource/{source.name}";
            if(source.Setup == null) {
                report.Warning("New game setup source has no setup assigned.", context);
            }
        }
    }

    static void ValidateLifePaths(ProjectValidationReport report) {
        var lifePaths = ProjectValidatorAssetFinder.FindAssets<LifePathDefinition>();
        var perks = ProjectValidatorAssetFinder.FindAssets<LifePathPerkDefinition>();

        foreach(var path in lifePaths) {
            if(path == null) continue;

            string context = $"LifePath/{path.name}";
            if(string.IsNullOrWhiteSpace(path.Id)) {
                report.Error("Life path id is empty.", context);
            }

            if(path.Tags != null && path.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Life path has an empty tag slot.", context);
            }

            if(path.ExperiencePerPerkPoint <= 0) {
                report.Warning("Life path XP per perk point must be greater than 0.", context);
            }

            if(path.MaxExperience > 0
                && path.MaxExperience < path.ExperiencePerPerkPoint
                && path.Perks.Any(perk => perk != null && !perk.UnlockedByDefault && perk.PerkPointCost > 0)) {
                report.Info("Life path max XP is lower than XP per perk point, so normal perk point unlocks cannot happen for this path.", context);
            }

            if(path.Branches == null || path.Branches.Count == 0) {
                report.Info("Life path has no branches. It can still track XP and perks.", context);
            } else {
                var duplicateBranches = path.Branches
                    .Where(branch => branch != null && !string.IsNullOrWhiteSpace(branch.BranchId))
                    .GroupBy(branch => branch.BranchId, StringComparer.OrdinalIgnoreCase)
                    .Where(group => group.Count() > 1);
                foreach(var duplicate in duplicateBranches) {
                    report.Warning($"Life path has duplicate branch id '{duplicate.Key}'.", context);
                }

                foreach(var branch in path.Branches) {
                    if(branch == null) {
                        report.Warning("Life path has a null branch slot.", context);
                    } else if(string.IsNullOrWhiteSpace(branch.BranchId)) {
                        report.Warning("Life path branch id is empty.", context);
                    } else if(branch.Tags != null && branch.Tags.Any(string.IsNullOrWhiteSpace)) {
                        report.Warning($"Life path branch '{branch.BranchId}' has an empty tag slot.", context);
                    }
                }
            }

            var duplicatePerkAssets = path.Perks
                .Where(perk => perk != null)
                .GroupBy(perk => perk)
                .Where(group => group.Count() > 1);
            foreach(var duplicate in duplicatePerkAssets) {
                report.Warning($"Life path lists perk '{duplicate.Key.DisplayName}' more than once.", context);
            }

            foreach(var perk in path.Perks) {
                if(perk == null) {
                    report.Warning("Life path has a null perk slot.", context);
                    continue;
                }

                if(perk.LifePath != path) {
                    report.Warning($"Perk '{perk.DisplayName}' is listed under this path but its Life Path reference points elsewhere.", context);
                }
            }
        }

        foreach(var perk in perks) {
            if(perk == null) continue;

            string context = $"LifePathPerk/{perk.name}";
            if(string.IsNullOrWhiteSpace(perk.Id)) {
                report.Error("Life path perk id is empty.", context);
            }

            if(perk.Tags != null && perk.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Life path perk has an empty tag slot.", context);
            }

            if(perk.LifePath == null) {
                report.Warning("Life path perk has no owning life path.", context);
            } else if(!string.IsNullOrWhiteSpace(perk.BranchId) && !perk.LifePath.HasBranch(perk.BranchId)) {
                report.Warning($"Life path perk references branch '{perk.BranchId}', but the owning path does not define that branch.", context);
            }

            if(perk.LifePath != null && perk.LifePath.MaxExperience > 0 && perk.RequiredPathExperience > perk.LifePath.MaxExperience) {
                report.Warning($"Life path perk requires {perk.RequiredPathExperience} XP, but owning path max XP is {perk.LifePath.MaxExperience}.", context);
            }

            if(perk.LifePath != null && perk.LifePath.MaxExperience > 0 && perk.PerkPointCost > perk.LifePath.CalculateEarnedPerkPoints(perk.LifePath.MaxExperience)) {
                report.Warning("Life path perk costs more perk points than the owning path can generate from its max XP.", context);
            }

            if(perk.UnlockedByDefault && perk.PerkPointCost > 0) {
                report.Info("Life path perk is unlocked by default but still has a perk point cost for manual unlock paths.", context);
            }

            if(string.IsNullOrWhiteSpace(perk.RequiredBranchId) && perk.RequiredBranchProgress > 0) {
                report.Warning("Life path perk requires branch progress but has no Required Branch Id.", context);
            }

            if(!string.IsNullOrWhiteSpace(perk.RequiredBranchId) && perk.LifePath != null && !perk.LifePath.HasBranch(perk.RequiredBranchId)) {
                report.Warning($"Life path perk requires branch '{perk.RequiredBranchId}', but the owning path does not define that branch.", context);
            }

            if(!string.IsNullOrWhiteSpace(perk.RequiredBranchId) && perk.RequiredBranchProgress <= 0) {
                report.Info("Life path perk has Required Branch Id set but Required Branch Progress is 0, so that branch requirement has no effect.", context);
            }

            if(string.IsNullOrWhiteSpace(perk.RequiredTag) && perk.RequiredTagCount > 0) {
                report.Warning("Life path perk requires tag progress but has no Required Tag.", context);
            }

            if(!string.IsNullOrWhiteSpace(perk.RequiredTag) && perk.RequiredTagCount <= 0) {
                report.Info("Life path perk has Required Tag set but Required Tag Count is 0, so that tag requirement has no effect.", context);
            }

            ValidateObjectList(perk.PrerequisitePerks, report, context, "Life path perk has a null prerequisite perk slot.");
            foreach(var prerequisite in perk.PrerequisitePerks) {
                if(prerequisite == null) {
                    continue;
                }

                if(prerequisite == perk) {
                    report.Error("Life path perk lists itself as a prerequisite.", context);
                } else if(perk.LifePath != null && prerequisite.LifePath != null && prerequisite.LifePath != perk.LifePath) {
                    report.Info($"Life path perk depends on prerequisite '{prerequisite.DisplayName}' from another life path.", context);
                }
            }

            if(HasLifePathPerkPrerequisiteCycle(perk, perk, new HashSet<LifePathPerkDefinition>())) {
                report.Error("Life path perk prerequisite chain contains a cycle.", context);
            }

            ValidateObjectList(perk.ExtraRequirements, report, context, "Life path perk has a null extra requirement slot.");
            ValidateLifePathPerkEffects(perk.UnlockEffects, report, context);
        }

        foreach(var requirement in ProjectValidatorAssetFinder.FindAssets<LifePathRequirement>()) {
            if(requirement == null) continue;
            ValidateLifePathRequirement(requirement, report, $"LifePathRequirement/{requirement.name}");
        }

        foreach(var source in ProjectValidatorAssetFinder.FindAssets<LifePathRewardSource>()) {
            if(source == null) continue;

            string context = $"LifePathRewardSource/{source.name}";
            if(source.Rewards == null || source.Rewards.Count == 0) {
                report.Warning("Life path reward source has no rewards assigned.", context);
            }

            ValidateLifePathRewards(source.Rewards, report, context);
        }
    }

    static bool HasLifePathPerkPrerequisiteCycle(LifePathPerkDefinition root, LifePathPerkDefinition current, HashSet<LifePathPerkDefinition> visitedPath) {
        if(current == null) {
            return false;
        }

        if(!visitedPath.Add(current)) {
            return true;
        }

        foreach(var prerequisite in current.PrerequisitePerks) {
            if(prerequisite == null) {
                continue;
            }

            if(prerequisite == root || HasLifePathPerkPrerequisiteCycle(root, prerequisite, visitedPath)) {
                return true;
            }
        }

        visitedPath.Remove(current);
        return false;
    }

    static void ValidateLifePathPerkEffects(LifePathPerkEffectDefinition effects, ProjectValidationReport report, string context) {
        if(effects == null) {
            return;
        }

        ValidateTitleGrants(effects.TitleGrants, report, context);
        ValidateObjectList(effects.MilestonesToComplete, report, context, "Life path perk effect has a null milestone slot.");
        ValidateReputationChanges(effects.ReputationChanges, report, context);
        ValidateRelationshipChanges(effects.RelationshipChanges, report, context);
        ValidateLifestylePointGrants(effects.LifestylePointGrants, report, context);
        ValidateCareerPointGrants(effects.CareerPointGrants, report, context);
        ValidateObjectList(effects.RecipeGrants.Select(grant => grant != null ? grant.recipe : null), report, context, "Life path perk effect has a null recipe grant slot.");
        ValidateOrganizationMembershipGrants(effects.OrganizationMembershipGrants, report, context);
        ValidateOrganizationPointGrants(effects.OrganizationPointGrants, report, context);
        ValidateObjectList(effects.BattleRulesToUnlock, report, context, "Life path perk effect has a null battle rule unlock slot.");
        ValidateObjectList(effects.ContestsToUnlock, report, context, "Life path perk effect has a null contest unlock slot.");
        ValidateObjectList(effects.ConsequenceChains, report, context, "Life path perk effect has a null consequence chain slot.");
    }

    static void ValidateLifePathRequirement(LifePathRequirement requirement, ProjectValidationReport report, string context) {
        switch(requirement.Mode) {
            case LifePathRequirementMode.PathExperienceAtLeast:
            case LifePathRequirementMode.AvailablePerkPointsAtLeast:
            case LifePathRequirementMode.SpentPerkPointsAtLeast:
            case LifePathRequirementMode.DominantPath:
                if(requirement.LifePath == null) {
                    report.Warning("Life path requirement mode needs a Life Path reference.", context);
                }
                break;
            case LifePathRequirementMode.BranchProgressAtLeast:
                if(requirement.LifePath == null) {
                    report.Warning("Life path branch requirement has no Life Path reference.", context);
                }

                if(string.IsNullOrWhiteSpace(requirement.BranchId)) {
                    report.Warning("Life path branch requirement has no Branch Id.", context);
                } else if(requirement.LifePath != null && !requirement.LifePath.HasBranch(requirement.BranchId)) {
                    report.Warning($"Life path branch requirement references branch '{requirement.BranchId}', but the selected path does not define that branch.", context);
                }
                break;
            case LifePathRequirementMode.TagProgressAtLeast:
                if(requirement.LifePath == null) {
                    report.Warning("Life path tag requirement has no Life Path reference.", context);
                }

                if(string.IsNullOrWhiteSpace(requirement.Tag)) {
                    report.Warning("Life path tag requirement has no tag.", context);
                }
                break;
            case LifePathRequirementMode.HasUnlockedPerk:
                if(requirement.Perk == null) {
                    report.Warning("Life path perk requirement has no perk assigned.", context);
                }
                break;
            case LifePathRequirementMode.AnyPathWithTagExperienceAtLeast:
            case LifePathRequirementMode.DominantPathHasTag:
                if(string.IsNullOrWhiteSpace(requirement.Tag)) {
                    report.Warning("Life path tag-based requirement has no tag.", context);
                }
                break;
        }

        if(requirement.RequiredValue == 0
            && requirement.Mode != LifePathRequirementMode.HasUnlockedPerk
            && requirement.Mode != LifePathRequirementMode.DominantPath
            && requirement.Mode != LifePathRequirementMode.DominantPathHasTag) {
            report.Info("Life path requirement required value is 0, so the numeric threshold may be trivially true.", context);
        }

        if(!requirement.Expected) {
            report.Info("Life path requirement is inverted. This is valid, but check that it is intentional.", context);
        }
    }

    static void ValidateQuests(ProjectValidationReport report) {
        foreach(var quest in ProjectValidatorAssetFinder.FindAssets<QuestBase>()) {
            if(quest == null) continue;

            string context = $"Quest/{quest.name}";
            if(string.IsNullOrWhiteSpace(quest.Name)) {
                report.Warning("Quest name is empty.", context);
            }

            if(quest.RequiredItem != null && quest.RequiredItemCount <= 0) {
                report.Warning("Quest has a required item but required count is 0.", context);
            }

            if(quest.RewardItem != null && quest.RewardItemCount <= 0) {
                report.Warning("Quest has a reward item but reward count is 0.", context);
            }

            ValidateReputationChanges(quest.ReputationRewards, report, context);
            ValidateRelationshipChanges(quest.RelationshipRewards, report, context);
            ValidateObjectList(quest.MilestonesToComplete, report, context, "Quest has a null milestone reward slot.");
            ValidateTitleGrants(quest.TitleRewards, report, context);
            ValidateObjectList(quest.RecipeRewards.Select(grant => grant != null ? grant.recipe : null), report, context, "Quest has a null recipe grant slot.");
            ValidateLifePathRewards(quest.LifePathRewards, report, context);
        }
    }

    static void ValidatePokedexEntries(ProjectValidationReport report) {
        foreach(var entry in ProjectValidatorAssetFinder.FindAssets<PokedexEntryDefinition>()) {
            if(entry == null) continue;

            string context = $"PokedexEntry/{entry.name}";
            if(string.IsNullOrWhiteSpace(entry.Id)) {
                report.Error("Pokedex entry id is empty.", context);
            }

            if(entry.Pokemon == null) {
                report.Warning("Pokedex entry has no Pokemon assigned.", context);
            }

            foreach(var habitat in entry.Habitats) {
                if(habitat == null) {
                    report.Warning("Pokedex entry has a null habitat slot.", context);
                    continue;
                }

                if(habitat.region == null && habitat.encounterTable == null) {
                    report.Warning("Pokedex habitat has no region or encounter table.", context);
                }
            }
        }
    }

    static void ValidatePokemonCoreGrowth(ProjectValidationReport report) {
        foreach(var profile in ProjectValidatorAssetFinder.FindAssets<PokemonGrowthProfileDefinition>()) {
            if(profile == null) continue;

            string context = $"PokemonGrowthProfile/{profile.name}";
            if(string.IsNullOrWhiteSpace(profile.Id)) {
                report.Error("Pokemon growth profile id is empty.", context);
            }

            if(profile.DefaultPotentialMinMultiplier > profile.DefaultPotentialMaxMultiplier) {
                report.Warning("Pokemon growth profile default potential min is greater than max.", context);
            }

            var duplicatePotentialStats = profile.PotentialRolls
                .Where(roll => roll != null)
                .GroupBy(roll => roll.Stat)
                .Where(group => group.Count() > 1);
            foreach(var duplicate in duplicatePotentialStats) {
                report.Info($"Pokemon growth profile has multiple potential rolls for {duplicate.Key}. The first matching roll is used.", context);
            }

            var duplicateTrainingStats = profile.TrainingRules
                .Where(rule => rule != null)
                .GroupBy(rule => rule.Stat)
                .Where(group => group.Count() > 1);
            foreach(var duplicate in duplicateTrainingStats) {
                report.Info($"Pokemon growth profile has multiple training rules for {duplicate.Key}. The first matching rule is used.", context);
            }

            if(profile.TotalTrainingCap > 0 && profile.TrainingRules.Count == 0 && profile.DefaultTrainingCapPerStat <= 0) {
                report.Warning("Pokemon growth profile has a total training cap but no per-stat training cap/rules.", context);
            }

            foreach(var traitRoll in profile.StartingTraits) {
                if(traitRoll == null) {
                    report.Warning("Pokemon growth profile has a null starting trait roll.", context);
                } else if(traitRoll.Trait == null && traitRoll.Chance > 0f) {
                    report.Warning("Pokemon growth profile has a starting trait roll with chance but no trait.", context);
                }
            }
        }

        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<PokemonGrowthProfileDefinition>(), report, "pokemon growth profile", profile => profile.Id);

        foreach(var trait in ProjectValidatorAssetFinder.FindAssets<PokemonPassiveTraitDefinition>()) {
            if(trait == null) continue;

            string context = $"PokemonPassiveTrait/{trait.name}";
            if(string.IsNullOrWhiteSpace(trait.Id)) {
                report.Error("Pokemon passive trait id is empty.", context);
            }

            if(trait.Tags != null && trait.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Pokemon passive trait has an empty tag slot.", context);
            }

            bool hasStatEffect = trait.StatModifiers != null && trait.StatModifiers.Any(modifier => modifier != null && (modifier.flatBonus != 0 || !Mathf.Approximately(modifier.multiplierBonus, 0f)));
            bool hasGeneralEffect = !Mathf.Approximately(trait.FriendshipGainMultiplier, 1f)
                || !Mathf.Approximately(trait.ExperienceGainMultiplier, 1f)
                || trait.CareBonus != 0
                || trait.AssignmentBonus != 0;
            if(!hasStatEffect && !hasGeneralEffect) {
                report.Info("Pokemon passive trait has no configured effect yet.", context);
            }
        }

        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<PokemonPassiveTraitDefinition>(), report, "pokemon passive trait", trait => trait.Id);

        foreach(var source in ProjectValidatorAssetFinder.FindAssets<PokemonGrowthInitializerSource>()) {
            if(source == null) continue;

            string context = $"PokemonGrowthInitializerSource/{source.name}";
            if(source.GrowthProfile == null) {
                report.Warning("Pokemon growth initializer has no growth profile.", context);
            }

            if(source.Target == PokemonGrowthInitializerTarget.PartySlot && source.PartySlotIndex > 5) {
                report.Info("Pokemon growth initializer targets a party slot above the normal 0-5 party range.", context);
            }
        }
    }

    static void ValidatePokemonAbilityTrees(ProjectValidationReport report) {
        foreach(var tree in ProjectValidatorAssetFinder.FindAssets<PokemonAbilityTreeDefinition>()) {
            if(tree == null) continue;

            string context = $"PokemonAbilityTree/{tree.name}";
            if(string.IsNullOrWhiteSpace(tree.Id)) {
                report.Error("Pokemon ability tree id is empty.", context);
            }

            if(tree.Tags != null && tree.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Pokemon ability tree has an empty tag slot.", context);
            }

            ValidateObjectList(tree.Requirements, report, context, "Pokemon ability tree has a null requirement slot.");

            if(tree.Nodes == null || tree.Nodes.Count == 0) {
                report.Info("Pokemon ability tree has no nodes yet.", context);
                continue;
            }

            var nodeIds = new HashSet<string>();
            foreach(var node in tree.Nodes) {
                if(node == null) {
                    report.Warning("Pokemon ability tree has a null node slot.", context);
                    continue;
                }

                string nodeContext = $"{context}/Node/{node.NodeId}";
                if(string.IsNullOrWhiteSpace(node.NodeId)) {
                    report.Warning("Pokemon ability tree node id is empty.", nodeContext);
                } else if(!nodeIds.Add(node.NodeId)) {
                    report.Warning($"Duplicate Pokemon ability tree node id '{node.NodeId}'.", nodeContext);
                }

                if(node.Tags != null && node.Tags.Any(string.IsNullOrWhiteSpace)) {
                    report.Warning("Pokemon ability tree node has an empty tag slot.", nodeContext);
                }

                foreach(var prerequisiteId in node.PrerequisiteNodeIds.Where(id => !string.IsNullOrWhiteSpace(id))) {
                    if(!tree.Nodes.Any(candidate => candidate != null && string.Equals(candidate.NodeId, prerequisiteId, System.StringComparison.OrdinalIgnoreCase))) {
                        report.Warning($"Pokemon ability tree node references missing prerequisite '{prerequisiteId}'.", nodeContext);
                    }

                    if(string.Equals(prerequisiteId, node.NodeId, System.StringComparison.OrdinalIgnoreCase)) {
                        report.Warning("Pokemon ability tree node references itself as a prerequisite.", nodeContext);
                    }
                }

                if(node.RequiredGrowthTraitIds != null && node.RequiredGrowthTraitIds.Any(string.IsNullOrWhiteSpace)) {
                    report.Warning("Pokemon ability tree node has an empty required growth trait id.", nodeContext);
                }

                if(node.RequiredKnownTechniqueIds != null && node.RequiredKnownTechniqueIds.Any(string.IsNullOrWhiteSpace)) {
                    report.Warning("Pokemon ability tree node has an empty required known technique id.", nodeContext);
                }

                ValidateObjectList(node.Requirements, report, nodeContext, "Pokemon ability tree node has a null requirement slot.");

                if(node.Effects == null || node.Effects.Count == 0) {
                    report.Info("Pokemon ability tree node has no effects. This is valid for routing/placeholder nodes.", nodeContext);
                    continue;
                }

                foreach(var effect in node.Effects) {
                    ValidatePokemonAbilityTreeEffect(effect, report, nodeContext);
                }
            }
        }

        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<PokemonAbilityTreeDefinition>(), report, "pokemon ability tree", tree => tree.Id);

        foreach(var source in ProjectValidatorAssetFinder.FindAssets<PokemonAbilityTreeSource>()) {
            if(source == null) continue;

            string context = $"PokemonAbilityTreeSource/{source.name}";
            if(source.Tree == null) {
                report.Warning("Pokemon ability tree source has no tree assigned.", context);
            }

            if((source.Action == PokemonAbilityTreeSourceAction.UnlockNode || source.Action == PokemonAbilityTreeSourceAction.GrantPointsAndUnlockNode)
                && string.IsNullOrWhiteSpace(source.NodeId)) {
                report.Warning("Pokemon ability tree source unlocks a node but has no node id.", context);
            }

            if(source.Target == PokemonAbilityTreeTarget.PartySlot && source.PartySlotIndex > 5) {
                report.Info("Pokemon ability tree source targets a party slot above the normal 0-5 party range.", context);
            }
        }
    }

    static void ValidatePokemonAbilityTreeEffect(PokemonAbilityTreeEffect effect, ProjectValidationReport report, string context) {
        if(effect == null) {
            report.Warning("Pokemon ability tree node has a null effect slot.", context);
            return;
        }

        switch(effect.Kind) {
            case PokemonAbilityTreeEffectKind.StatModifier:
                if(effect.FlatStatBonus == 0 && Mathf.Approximately(effect.StatMultiplierBonus, 0f)) {
                    report.Info("Pokemon ability tree stat modifier effect has no configured stat change.", context);
                }
                break;
            case PokemonAbilityTreeEffectKind.GrowthTraining:
                if(effect.TrainingPoints <= 0) {
                    report.Warning("Pokemon ability tree growth training effect grants no training points.", context);
                }
                break;
            case PokemonAbilityTreeEffectKind.PassiveTrait:
                if(effect.PassiveTrait == null) {
                    report.Warning("Pokemon ability tree passive trait effect has no trait assigned.", context);
                }
                break;
            case PokemonAbilityTreeEffectKind.Technique:
                if(effect.Technique == null) {
                    report.Warning("Pokemon ability tree technique effect has no move assigned.", context);
                }
                break;
            case PokemonAbilityTreeEffectKind.Friendship:
                if(effect.FriendshipAmount == 0) {
                    report.Info("Pokemon ability tree friendship effect changes friendship by 0.", context);
                }
                break;
        }
    }

    static void ValidatePokemonEvolutions(ProjectValidationReport report) {
        foreach(var evolution in ProjectValidatorAssetFinder.FindAssets<PokemonEvolutionDefinition>()) {
            if(evolution == null) continue;

            string context = $"PokemonEvolution/{evolution.name}";
            if(string.IsNullOrWhiteSpace(evolution.Id)) {
                report.Error("Pokemon evolution id is empty.", context);
            }

            if(evolution.EvolvesFrom == null) {
                report.Warning("Pokemon evolution has no source Pokemon.", context);
            }

            if(evolution.EvolvesInto == null) {
                report.Warning("Pokemon evolution has no target Pokemon.", context);
            }

            if(evolution.EvolvesFrom != null && evolution.EvolvesInto != null && evolution.EvolvesFrom == evolution.EvolvesInto) {
                report.Warning("Pokemon evolution source and target are the same Pokemon.", context);
            }

            if(evolution.TriggerKind == PokemonEvolutionTriggerKind.ItemUse && evolution.RequiredItem == null) {
                report.Warning("Pokemon evolution uses Item Use trigger but has no required item.", context);
            }

            if(evolution.MinimumLevel <= 0 && evolution.RequiredItem == null && evolution.MinimumFriendship <= 0
                && evolution.RequiredTime == GeneralDayPeriod.None && evolution.Requirements.Count == 0
                && string.IsNullOrWhiteSpace(evolution.RequiredRegionId) && string.IsNullOrWhiteSpace(evolution.RequiredZoneId)
                && string.IsNullOrWhiteSpace(evolution.RequiredSceneName) && evolution.RequiredGrowthTraitIds.Count == 0) {
                report.Info("Pokemon evolution has no visible requirements beyond source Pokemon and trigger.", context);
            }

            if(evolution.RequiredGrowthTraitIds != null && evolution.RequiredGrowthTraitIds.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Pokemon evolution has an empty required growth trait id.", context);
            }

            ValidateObjectList(evolution.Requirements, report, context, "Pokemon evolution has a null requirement slot.");
        }

        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<PokemonEvolutionDefinition>(), report, "pokemon evolution", evolution => evolution.Id);

        foreach(var source in ProjectValidatorAssetFinder.FindAssets<PokemonEvolutionSource>()) {
            if(source == null) continue;

            string context = $"PokemonEvolutionSource/{source.name}";
            if(source.Evolution == null) {
                report.Info("Pokemon evolution source has no explicit route and will use the first eligible route from Resources.", context);
            }

            if(source.TriggerKind == PokemonEvolutionTriggerKind.ItemUse && source.Evolution != null && source.Evolution.RequiredItem == null) {
                report.Info("Pokemon evolution source uses item trigger, but its route has no item requirement.", context);
            }
        }
    }

    static void ValidatePokemonTechniqueLearning(ProjectValidationReport report) {
        foreach(var definition in ProjectValidatorAssetFinder.FindAssets<PokemonTechniqueLearningDefinition>()) {
            if(definition == null) continue;

            string context = $"PokemonTechniqueLearning/{definition.name}";
            if(string.IsNullOrWhiteSpace(definition.Id)) {
                report.Error("Pokemon technique learning id is empty.", context);
            }

            if(definition.Move == null) {
                report.Warning("Pokemon technique learning definition has no move assigned.", context);
            }

            if(definition.Tags != null && definition.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Pokemon technique learning definition has an empty tag slot.", context);
            }

            if(definition.MinimumLevel <= 0 && definition.MinimumFriendship <= 0 && definition.Requirements.Count == 0 && !definition.RequireSpeciesCompatibility) {
                report.Info("Pokemon technique learning definition has no level, friendship, species or reusable requirement gate.", context);
            }

            ValidateObjectList(definition.Requirements, report, context, "Pokemon technique learning definition has a null requirement slot.");
        }

        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<PokemonTechniqueLearningDefinition>(), report, "pokemon technique learning", definition => definition.Id);

        foreach(var source in ProjectValidatorAssetFinder.FindAssets<PokemonTechniqueLearningSource>()) {
            if(source == null) continue;

            string context = $"PokemonTechniqueLearningSource/{source.name}";
            if(source.Definition == null) {
                report.Warning("Pokemon technique learning source has no definition.", context);
            }

            if(source.Target == PokemonTechniqueLearnTarget.PartySlot && source.PartySlotIndex > 5) {
                report.Info("Pokemon technique learning source targets a party slot above the normal 0-5 party range.", context);
            }
        }
    }

    static void ValidatePokemonHeldItems(ProjectValidationReport report) {
        foreach(var heldItem in ProjectValidatorAssetFinder.FindAssets<BattleHeldItem>()) {
            if(heldItem == null) continue;

            string context = $"BattleHeldItem/{heldItem.name}";
            if(string.IsNullOrWhiteSpace(heldItem.Name)) {
                report.Warning("Battle held item has no display name.", context);
            }

            if(!heldItem.HasAnyConfiguredEffect) {
                report.Info("Battle held item has no configured effect yet.", context);
            }
        }

        foreach(var source in ProjectValidatorAssetFinder.FindAssets<PokemonHeldItemSource>()) {
            if(source == null) continue;

            string context = $"PokemonHeldItemSource/{source.name}";
            if(string.IsNullOrWhiteSpace(source.SourceId)) {
                report.Info("Held item source has no source id, so history records will be harder to read.", context);
            }

            if((source.Action == PokemonHeldItemAction.Give || source.Action == PokemonHeldItemAction.Swap) && source.Item == null) {
                report.Warning("Held item source gives/swaps an item but has no item assigned.", context);
            }

            if(source.TargetMode == PokemonHeldItemTargetMode.PartySlot && source.PartySlot > 5) {
                report.Info("Held item source targets a party slot above the normal 0-5 party range.", context);
            }
        }
    }

    static void ValidateRegionInfo(ProjectValidationReport report) {
        foreach(var region in ProjectValidatorAssetFinder.FindAssets<RegionInfoDefinition>()) {
            if(region == null) continue;

            string context = $"RegionInfo/{region.name}";
            if(string.IsNullOrWhiteSpace(region.Id)) {
                report.Error("Region info id is empty.", context);
            }

            foreach(var table in region.EncounterTables) {
                if(table == null) {
                    report.Warning("Region has a null encounter table slot.", context);
                }
            }

            foreach(var zone in region.ActivityZones) {
                if(zone == null) {
                    report.Warning("Region has a null activity zone slot.", context);
                }
            }

            foreach(var shop in region.Shops) {
                if(shop == null) {
                    report.Warning("Region has a null shop slot.", context);
                }
            }

            foreach(var stop in region.TransitStops) {
                if(stop == null) {
                    report.Warning("Region has a null transit stop slot.", context);
                }
            }

            foreach(var board in region.JobBoards) {
                if(board == null) {
                    report.Warning("Region has a null job board slot.", context);
                }
            }
        }
    }

    static void ValidateWorldRegions(ProjectValidationReport report) {
        foreach(var region in ProjectValidatorAssetFinder.FindAssets<WorldRegionDefinition>()) {
            if(region == null) continue;

            string context = $"WorldRegion/{region.name}";
            if(string.IsNullOrWhiteSpace(region.Id)) {
                report.Error("World region id is empty.", context);
            }

            if(region.Tags != null && region.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("World region has an empty tag slot.", context);
            }

            if(string.IsNullOrWhiteSpace(region.DefaultSceneName)) {
                report.Info("World region has no default scene name. Region travel can still override it per route.", context);
            }

            ValidateObjectList(region.RegionInfos, report, context, "World region has a null region info slot.");
            ValidateObjectList(region.MapMarkers, report, context, "World region has a null map marker slot.");
            ValidateObjectList(region.EncounterTables, report, context, "World region has a null encounter table slot.");
            ValidateObjectList(region.ActivityZones, report, context, "World region has a null activity zone slot.");
            ValidateObjectList(region.Shops, report, context, "World region has a null shop slot.");
            ValidateObjectList(region.Services, report, context, "World region has a null service slot.");
            ValidateObjectList(region.TransitStops, report, context, "World region has a null transit stop slot.");
            ValidateObjectList(region.CalendarEvents, report, context, "World region has a null calendar event slot.");
            ValidateObjectList(region.BattleRuleSets, report, context, "World region has a null battle rule set slot.");
            ValidateObjectList(region.FeaturedPokemon, report, context, "World region has a null featured Pokemon slot.");
            ValidateObjectList(region.Requirements, report, context, "World region has a null requirement slot.");
        }

        foreach(var policy in ProjectValidatorAssetFinder.FindAssets<RegionTravelPolicyDefinition>()) {
            if(policy == null) continue;

            string context = $"RegionTravelPolicy/{policy.name}";
            if(string.IsNullOrWhiteSpace(policy.Id)) {
                report.Error("Region travel policy id is empty.", context);
            }

            if(policy.Tags != null && policy.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Region travel policy has an empty tag slot.", context);
            }

            if(policy.Options == null || policy.Options.Count == 0) {
                report.Warning("Region travel policy has no options.", context);
            } else {
                int defaultCount = policy.Options.Count(option => option != null && option.IsDefault);
                if(defaultCount > 1) {
                    report.Warning("Region travel policy has more than one default option. The first one will be used.", context);
                }

                var seenOptionIds = new HashSet<string>();
                foreach(var option in policy.Options) {
                    if(option == null) {
                        report.Warning("Region travel policy has a null option slot.", context);
                        continue;
                    }

                    if(string.IsNullOrWhiteSpace(option.Id)) {
                        report.Warning("Region travel policy option id is empty.", context);
                    } else if(!seenOptionIds.Add(option.Id)) {
                        report.Warning($"Region travel policy has duplicate option id '{option.Id}'.", context);
                    }

                    if(option.ChallengeMode == RegionTravelChallengePolicyMode.StartOverrideChallenge && option.ChallengeOverride == null) {
                        report.Warning("Region travel policy option starts an override challenge but has no override challenge assigned.", context);
                    }

                    if(option.ChallengeMode == RegionTravelChallengePolicyMode.RequireOverrideChallenge && option.ChallengeOverride == null) {
                        report.Warning("Region travel policy option requires an override challenge but has no override challenge assigned.", context);
                    }

                    if(option.CompleteActiveChallengeBeforeTravel && option.ClearActiveChallengeBeforeTravel) {
                        report.Warning("Region travel policy option both completes and clears the active challenge before travel.", context);
                    }

                    ValidateObjectList(option.Requirements, report, context, "Region travel policy option has a null requirement slot.");
                }
            }
        }

        foreach(var route in ProjectValidatorAssetFinder.FindAssets<RegionTravelRouteDefinition>()) {
            if(route == null) continue;

            string context = $"RegionTravelRoute/{route.name}";
            if(string.IsNullOrWhiteSpace(route.Id)) {
                report.Error("Region travel route id is empty.", context);
            }

            if(route.DestinationRegion == null) {
                report.Warning("Region travel route has no destination region.", context);
            }

            if(route.OriginRegion != null && route.DestinationRegion != null && route.OriginRegion == route.DestinationRegion) {
                report.Warning("Region travel route origin and destination are the same.", context);
            }

            if(route.RepeatMode == ConsequenceChainRepeatMode.CooldownHours && route.CooldownHours <= 0) {
                report.Warning("Region travel route uses Cooldown Hours repeat mode but cooldown is 0.", context);
            }

            if(route.RequiresRouteUnlock && route.UnlockedByDefault) {
                report.Info("Region travel route requires unlock but is also unlocked by default. It will be usable immediately.", context);
            }

            if(route.TravelPolicy != null && (route.TravelPolicy.Options == null || route.TravelPolicy.Options.Count == 0)) {
                report.Warning("Region travel route has a travel policy with no options.", context);
            }

            foreach(var cost in route.ItemCosts) {
                if(cost != null && cost.item == null && cost.count > 0) {
                    report.Warning("Region travel route item cost has count but no item.", context);
                }
            }

            foreach(var cost in route.NeedCosts) {
                if(cost != null && cost.need == null && cost.amount > 0) {
                    report.Warning("Region travel route need cost has amount but no need.", context);
                }
            }

            ValidateObjectList(route.Requirements, report, context, "Region travel route has a null requirement slot.");
            ValidateObjectList(route.TitleRewards.Select(grant => grant != null ? grant.title : null), report, context, "Region travel route has a null title reward slot.");
            ValidateObjectList(route.MilestonesToComplete, report, context, "Region travel route has a null milestone slot.");
            ValidateObjectList(route.ReputationRewards.Select(change => change != null ? change.faction : null), report, context, "Region travel route has a null reputation reward slot.");
            ValidateObjectList(route.RelationshipRewards.Select(change => change != null ? change.subject : null), report, context, "Region travel route has a null relationship reward slot.");
            ValidateObjectList(route.LifestyleRewards.Select(grant => grant != null ? grant.lifestyle : null), report, context, "Region travel route has a null lifestyle reward slot.");
        }

        foreach(var challenge in ProjectValidatorAssetFinder.FindAssets<RegionChallengeProfileDefinition>()) {
            if(challenge == null) continue;

            string context = $"RegionChallenge/{challenge.name}";
            if(string.IsNullOrWhiteSpace(challenge.Id)) {
                report.Error("Region challenge id is empty.", context);
            }

            if(challenge.Tags != null && challenge.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Region challenge has an empty tag slot.", context);
            }

            if(challenge.PartyTransferMode == RegionPartyTransferMode.OnePokemonOnly && challenge.MaxRosterPokemon > 1) {
                report.Warning("Region challenge is One Pokemon Only but max roster Pokemon is above 1.", context);
            }

            ValidateObjectList(challenge.Requirements, report, context, "Region challenge has a null requirement slot.");
            ValidateObjectList(challenge.CompletionTitleGrants.Select(grant => grant != null ? grant.title : null), report, context, "Region challenge has a null completion title grant slot.");
            ValidateObjectList(challenge.CompletionMilestones, report, context, "Region challenge has a null completion milestone slot.");
            ValidateObjectList(challenge.CompletionReputationChanges.Select(change => change != null ? change.faction : null), report, context, "Region challenge has a null completion reputation slot.");
            ValidateObjectList(challenge.CompletionRelationshipChanges.Select(change => change != null ? change.subject : null), report, context, "Region challenge has a null completion relationship slot.");
        }

        foreach(var point in ProjectValidatorAssetFinder.FindAssets<RegionTravelPoint>()) {
            if(point == null) continue;

            string context = $"RegionTravelPoint/{point.name}";
            if(point.Routes == null || point.Routes.Count == 0) {
                report.Warning("Region travel point has no routes assigned.", context);
            } else {
                ValidateObjectList(point.Routes, report, context, "Region travel point has a null route slot.");
            }
        }
    }

    static void ValidatePokeNavEntries(ProjectValidationReport report) {
        foreach(var entry in ProjectValidatorAssetFinder.FindAssets<PokeNavEntryDefinition>()) {
            if(entry == null) continue;

            string context = $"PokeNavEntry/{entry.name}";
            if(string.IsNullOrWhiteSpace(entry.Id)) {
                report.Error("PokeNav entry id is empty.", context);
            }

            if(string.IsNullOrWhiteSpace(entry.Body)) {
                report.Info("PokeNav entry has no body text.", context);
            }
        }
    }

    static void ValidatePokeNavGuideSections(ProjectValidationReport report) {
        foreach(var section in ProjectValidatorAssetFinder.FindAssets<PokeNavGuideSectionDefinition>()) {
            if(section == null) continue;

            string context = $"PokeNavGuideSection/{section.name}";
            if(string.IsNullOrWhiteSpace(section.Id)) {
                report.Error("PokeNav guide section id is empty.", context);
            }

            if(section.Tags != null && section.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("PokeNav guide section has an empty tag slot.", context);
            }

            if(section.RequiredTags != null && section.RequiredTags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("PokeNav guide section has an empty required tag slot.", context);
            }

            if(section.BlockedTags != null && section.BlockedTags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("PokeNav guide section has an empty blocked tag slot.", context);
            }

            if(section.ContentTypes == null || section.ContentTypes.Count == 0) {
                report.Warning("PokeNav guide section has no content types selected.", context);
            }

            if(section.ContentTypes != null && section.ContentTypes.GroupBy(type => type).Any(group => group.Count() > 1)) {
                report.Info("PokeNav guide section contains duplicate content type entries.", context);
            }

            if(section.ContentTypes != null
                && section.ContentTypes.Contains(PokeNavGuideContentType.MapMarker)
                && section.MapViewProfile == null) {
                report.Info("PokeNav guide section includes map markers without a map view profile. It will scan marker definitions instead of runtime marker records.", context);
            }

            if(section.RequiredTags != null && section.BlockedTags != null) {
                foreach(var tag in section.RequiredTags.Where(tag => !string.IsNullOrWhiteSpace(tag))) {
                    if(section.BlockedTags.Any(blocked => string.Equals(blocked, tag, System.StringComparison.OrdinalIgnoreCase))) {
                        report.Warning($"PokeNav guide section both requires and blocks tag '{tag}'.", context);
                    }
                }
            }
        }
    }

    static void ValidatePokeNavFeedItems(ProjectValidationReport report) {
        foreach(var item in ProjectValidatorAssetFinder.FindAssets<PokeNavFeedItemDefinition>()) {
            if(item == null) continue;

            string context = $"PokeNavFeedItem/{item.name}";
            if(string.IsNullOrWhiteSpace(item.Id)) {
                report.Error("PokeNav feed item id is empty.", context);
            }

            if(string.IsNullOrWhiteSpace(item.Body)) {
                report.Info("PokeNav feed item has no body text.", context);
            }

            if(item.Tags != null && item.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("PokeNav feed item has an empty tag slot.", context);
            }

            if(item.ExpiresAfterUnlock && item.DefaultDurationHours <= 0) {
                report.Warning("PokeNav feed item expires after unlock but has no duration.", context);
            }

            if(item.FeedType == PokeNavFeedItemType.PokemonSighting && item.RelatedPokemon == null) {
                report.Info("Pokemon sighting feed item has no related Pokemon.", context);
            }

            if(item.RequireCalendarEventVisible && item.RelatedCalendarEvent == null) {
                report.Warning("PokeNav feed item requires a visible calendar event but has no related calendar event.", context);
            }

            if(item.RecordPokemonKnowledge && item.RelatedPokemon == null && item.PokemonKnowledgeToGrant > PokemonKnowledgeLevel.Unknown) {
                report.Warning("PokeNav feed item grants Pokemon knowledge but has no related Pokemon.", context);
            }

            if(item.DiscoverRegion && item.RelatedRegion == null) {
                report.Info("PokeNav feed item has Discover Region enabled but no related region.", context);
            }

            if(item.DiscoverPokeNavEntry && item.RelatedPokeNavEntry == null) {
                report.Info("PokeNav feed item has Discover PokeNav Entry enabled but no related entry.", context);
            }

            if(item.UnlockSocialPost && item.RelatedSocialPost == null) {
                report.Info("PokeNav feed item has Unlock Social Post enabled but no related social post.", context);
            }

            if(item.DiscoverMapMarker && item.RelatedMapMarker == null) {
                report.Info("PokeNav feed item has Discover Map Marker enabled but no related map marker.", context);
            }

            if(item.RevealCalendarEvent && item.RelatedCalendarEvent == null) {
                report.Info("PokeNav feed item has Reveal Calendar Event enabled but no related calendar event.", context);
            }

            if(item.ApplyWorldDiscovery && item.RelatedWorldDiscovery == null) {
                report.Warning("PokeNav feed item applies world discovery but has no related world discovery.", context);
            }

            ValidateObjectList(item.Requirements, report, context, "PokeNav feed item has a null requirement slot.");
        }
    }

    static void ValidateRideSystem(ProjectValidationReport report) {
        foreach(var ride in ProjectValidatorAssetFinder.FindAssets<RidePokemonDefinition>()) {
            if(ride == null) continue;

            string context = $"Ride/{ride.name}";
            if(string.IsNullOrWhiteSpace(ride.Id)) {
                report.Error("Ride id is empty.", context);
            }

            if(ride.Tags != null && ride.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Ride has an empty tag slot.", context);
            }

            if(ride.RequirePokemonInParty && !ride.AllowAnyPokemon && !ride.HasPokemonFilter()) {
                report.Warning("Ride requires a party Pokemon but has no Pokemon species/type/move filter and Allow Any Pokemon is disabled.", context);
            }

            if(ride.MoveSpeedMultiplier <= 0f || ride.RunSpeedMultiplier <= 0f) {
                report.Warning("Ride speed multipliers should be above 0.", context);
            }

            if(ride.RideMode == PokemonRideMode.Surf && !ride.SetCharacterSurfingFlag) {
                report.Info("Surf ride does not set the Character surfing flag. Existing water movement may still block it.", context);
            }

            if(ride.VisualMode == RideVisualMode.PrefabOrSprites
                && ride.RideVisualPrefab == null
                && (ride.DirectionalSprites == null || !ride.DirectionalSprites.HasAnySprite)
                && ride.HidePlayerSprite) {
                report.Warning("Ride hides the player sprite but has no prefab or directional sprites.", context);
            }

            ValidateObjectList(ride.AllowedPokemon, report, context, "Ride has a null allowed Pokemon slot.");
            ValidateObjectList(ride.RequiredTitles, report, context, "Ride has a null required title slot.");
            ValidateObjectList(ride.RequiredMilestones, report, context, "Ride has a null required milestone slot.");
            ValidateObjectList(ride.ExtraRequirements, report, context, "Ride has a null requirement slot.");
        }

        foreach(var point in ProjectValidatorAssetFinder.FindAssets<RidePoint>()) {
            if(point == null) continue;

            string context = $"RidePoint/{point.name}";
            if(point.Ride == null) {
                report.Warning("Ride point has no ride definition assigned.", context);
            }
        }
    }

    static void ValidateSocialPosts(ProjectValidationReport report) {
        foreach(var post in ProjectValidatorAssetFinder.FindAssets<SocialPostDefinition>()) {
            if(post == null) continue;

            string context = $"SocialPost/{post.name}";
            if(string.IsNullOrWhiteSpace(post.Id)) {
                report.Error("Social post id is empty.", context);
            }

            if(string.IsNullOrWhiteSpace(post.Body)) {
                report.Warning("Social post has no body text.", context);
            }

            if(post.RelatedPokemon != null && post.RelatedRegion == null && post.PostType == SocialPostType.PokemonSighting) {
                report.Info("Pokemon sighting post has no related region. This is okay if the location is intentionally vague.", context);
            }
        }
    }

    static void ValidateSocialActivities(ProjectValidationReport report) {
        foreach(var activity in ProjectValidatorAssetFinder.FindAssets<SocialActivityDefinition>()) {
            if(activity == null) continue;

            string context = $"SocialActivity/{activity.name}";
            if(string.IsNullOrWhiteSpace(activity.Id)) {
                report.Error("Social activity id is empty.", context);
            }

            if(activity.BaseActivity == null && activity.DailyLimit <= 0 && activity.CooldownHours <= 0) {
                report.Info("Social activity has no base activity or repeat limit. It can be repeated freely by sources unless blocked elsewhere.", context);
            }

            ValidateObjectList(activity.MilestonesToComplete, report, context, "Social activity has a null milestone slot.");
            ValidateObjectList(activity.TitleGrants?.Select(grant => grant != null ? grant.title : null), report, context, "Social activity has a null title grant slot.");
            ValidateLifePathRewards(activity.LifePathRewards, report, context);
        }
    }

    static void ValidateRoleActivityBoards(ProjectValidationReport report) {
        foreach(var board in ProjectValidatorAssetFinder.FindAssets<RoleActivityBoardDefinition>()) {
            if(board == null) continue;

            string context = $"RoleActivityBoard/{board.name}";
            if(string.IsNullOrWhiteSpace(board.Id)) {
                report.Error("Role activity board id is empty.", context);
            }

            if(board.Tags != null && board.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Role activity board has an empty tag slot.", context);
            }

            ValidateObjectList(board.Requirements, report, context, "Role activity board has a null requirement slot.");

            if(board.Entries == null || board.Entries.Count == 0) {
                report.Info("Role activity board has no entries yet.", context);
                continue;
            }

            var duplicateEntryIds = board.Entries
                .Where(entry => entry != null && entry.HasTarget() && !string.IsNullOrWhiteSpace(entry.ResolveEntryId()))
                .GroupBy(entry => entry.ResolveEntryId())
                .Where(group => group.Count() > 1);
            foreach(var duplicate in duplicateEntryIds) {
                report.Warning($"Role activity board has duplicate entry id '{duplicate.Key}'.", context);
            }

            foreach(var entry in board.Entries) {
                if(entry == null) {
                    report.Warning("Role activity board has a null entry slot.", context);
                    continue;
                }

                string entryContext = $"{context}/Entry/{entry.ResolveEntryId()}";
                ValidateRoleActivityBoardEntry(entry, report, entryContext);
            }
        }

        foreach(var source in ProjectValidatorAssetFinder.FindAssets<RoleActivityBoardSource>()) {
            if(source == null) continue;

            string context = $"RoleActivityBoardSource/{source.name}";
            if(source.Board == null) {
                report.Warning("Role activity board source has no board assigned.", context);
            }

            if(source.InteractAction == RoleActivityBoardSourceAction.RunConfiguredEntry && string.IsNullOrWhiteSpace(source.EntryIdToRun)) {
                report.Warning("Role activity board source runs a configured entry but Entry Id To Run is empty.", context);
            }

            if(source.RunOnPlayerTrigger
                && source.TriggerAction == RoleActivityBoardSourceAction.RunConfiguredEntry
                && string.IsNullOrWhiteSpace(source.EntryIdToRun)) {
                report.Warning("Role activity board source trigger runs a configured entry but Entry Id To Run is empty.", context);
            }
        }
    }

    static void ValidateRoleActivityBoardEntry(RoleActivityBoardEntry entry, ProjectValidationReport report, string context) {
        if(!entry.HasTarget()) {
            report.Warning("Role activity board entry has no target assigned for its selected type.", context);
        }

        ValidateObjectList(entry.ExtraRequirements, report, context, "Role activity board entry has a null requirement slot.");
        ValidateLifePathRewards(entry.LifePathRewards, report, context);

        switch(entry.EntryType) {
            case RoleActivityBoardEntryType.Activity:
                if(entry.Activity == null) {
                    report.Warning("Activity entry has no activity assigned.", context);
                }
                if(!entry.PayActivityCosts && !entry.ApplyActivityRewards && !entry.ApplyActivityRelationshipRewards) {
                    report.Info("Activity entry pays no costs and applies no rewards. It will only report completion.", context);
                }
                break;
            case RoleActivityBoardEntryType.Job:
                if(entry.Job == null) {
                    report.Warning("Job entry has no job assigned.", context);
                }
                break;
            case RoleActivityBoardEntryType.PokemonAssignment:
                if(entry.PokemonAssignment == null) {
                    report.Warning("Pokemon assignment entry has no assignment assigned.", context);
                }
                break;
            case RoleActivityBoardEntryType.PokemonAssignmentBoard:
                if(entry.PokemonAssignmentBoard == null) {
                    report.Warning("Pokemon assignment board entry has no assignment board assigned.", context);
                }
                break;
            case RoleActivityBoardEntryType.SocialActivity:
                if(entry.SocialActivity == null) {
                    report.Warning("Social activity entry has no social activity assigned.", context);
                }
                break;
            case RoleActivityBoardEntryType.SituationEvent:
                if(entry.SituationEvent == null) {
                    report.Warning("Situation event entry has no event assigned.", context);
                }
                break;
            case RoleActivityBoardEntryType.SituationEventPool:
                if(entry.SituationEventPool == null) {
                    report.Warning("Situation event pool entry has no pool assigned.", context);
                }
                break;
            case RoleActivityBoardEntryType.LifePathRewards:
                if(entry.LifePathRewards == null || !entry.LifePathRewards.Any(reward => reward != null)) {
                    report.Warning("Life Path reward entry has no rewards assigned.", context);
                }
                break;
        }
    }

    static void ValidateCampStations(ProjectValidationReport report) {
        foreach(var station in ProjectValidatorAssetFinder.FindAssets<CampStationDefinition>()) {
            if(station == null) continue;

            string context = $"CampStation/{station.name}";
            if(string.IsNullOrWhiteSpace(station.Id)) {
                report.Error("Camp station id is empty.", context);
            }

            if(station.Tags != null && station.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Camp station has an empty tag slot.", context);
            }

            if(station.RequireActivityZone
                && station.AllowedZones.Count == 0
                && station.AllowedZoneTypes.Count == 0
                && station.AllowedZoneTags.Count == 0) {
                report.Info("Camp station requires an activity zone but has no zone filters. Any active zone will be accepted.", context);
            }

            ValidateObjectList(station.Requirements, report, context, "Camp station has a null requirement slot.");
            ValidateObjectList(station.AllowedZones, report, context, "Camp station has a null allowed zone slot.");

            if(station.Actions == null || station.Actions.Count == 0) {
                report.Info("Camp station has no actions yet.", context);
                continue;
            }

            var duplicateActionIds = station.Actions
                .Where(action => action != null && action.HasTarget() && !string.IsNullOrWhiteSpace(action.ResolveActionId()))
                .GroupBy(action => action.ResolveActionId())
                .Where(group => group.Count() > 1);
            foreach(var duplicate in duplicateActionIds) {
                report.Warning($"Camp station has duplicate action id '{duplicate.Key}'.", context);
            }

            foreach(var action in station.Actions) {
                if(action == null) {
                    report.Warning("Camp station has a null action slot.", context);
                    continue;
                }

                string actionContext = $"{context}/Action/{action.ResolveActionId()}";
                ValidateCampStationAction(action, report, actionContext);
            }
        }

        foreach(var source in ProjectValidatorAssetFinder.FindAssets<CampStationSource>()) {
            if(source == null) continue;

            string context = $"CampStationSource/{source.name}";
            if(source.Station == null) {
                report.Warning("Camp station source has no station assigned.", context);
            }

            if(source.InteractAction == CampStationSourceAction.RunConfiguredAction && string.IsNullOrWhiteSpace(source.ActionIdToRun)) {
                report.Warning("Camp station source runs a configured action but Action Id To Run is empty.", context);
            }

            if(source.RunOnPlayerTrigger
                && source.TriggerAction == CampStationSourceAction.RunConfiguredAction
                && string.IsNullOrWhiteSpace(source.ActionIdToRun)) {
                report.Warning("Camp station source trigger runs a configured action but Action Id To Run is empty.", context);
            }
        }
    }

    static void ValidateCampStationAction(CampStationAction action, ProjectValidationReport report, string context) {
        if(!action.HasTarget()) {
            report.Warning("Camp station action has no target assigned for its selected type.", context);
        }

        ValidateObjectList(action.ExtraRequirements, report, context, "Camp station action has a null requirement slot.");
        ValidateLifePathRewards(action.LifePathRewards, report, context);

        foreach(var change in action.SurvivalNeedChanges) {
            if(change == null) {
                report.Warning("Camp station action has a null survival need change slot.", context);
            } else if(change.need == null && change.amount != 0) {
                report.Warning("Camp station survival need change has an amount but no need.", context);
            }
        }

        foreach(var change in action.PokemonCareNeedChanges) {
            if(change == null) {
                report.Warning("Camp station action has a null Pokemon care need change slot.", context);
            } else if(change.need == null && change.amount != 0) {
                report.Warning("Camp station Pokemon care need change has an amount but no care need.", context);
            }
        }

        switch(action.ActionType) {
            case CampStationActionType.Activity:
                if(action.Activity == null) {
                    report.Warning("Activity action has no activity assigned.", context);
                }
                break;
            case CampStationActionType.Rest:
                if(!action.AffectPlayerNeeds && !action.AffectPokemonCareNeeds) {
                    report.Warning("Rest action does not affect player needs or Pokemon care needs.", context);
                }
                break;
            case CampStationActionType.Sleep:
                if(!action.AffectPlayerNeeds && !action.AffectPokemonCareNeeds) {
                    report.Warning("Sleep action does not affect player needs or Pokemon care needs.", context);
                }
                break;
            case CampStationActionType.PokemonCareAction:
                if(action.CareAction == null) {
                    report.Warning("Pokemon care action has no care action assigned.", context);
                }
                break;
            case CampStationActionType.SocialActivity:
                if(action.SocialActivity == null) {
                    report.Warning("Social activity action has no social activity assigned.", context);
                }
                break;
            case CampStationActionType.PokemonAssignment:
                if(action.PokemonAssignment == null) {
                    report.Warning("Pokemon assignment action has no assignment assigned.", context);
                }
                break;
            case CampStationActionType.PokemonAssignmentBoard:
                if(action.PokemonAssignmentBoard == null) {
                    report.Warning("Pokemon assignment board action has no board assigned.", context);
                }
                break;
            case CampStationActionType.SituationEvent:
                if(action.SituationEvent == null) {
                    report.Warning("Situation event action has no event assigned.", context);
                }
                break;
            case CampStationActionType.SituationEventPool:
                if(action.SituationEventPool == null) {
                    report.Warning("Situation event pool action has no pool assigned.", context);
                }
                break;
            case CampStationActionType.RoleActivityBoard:
                if(action.RoleActivityBoard == null) {
                    report.Warning("Role activity board action has no board assigned.", context);
                }
                break;
            case CampStationActionType.LifePathRewards:
                if(action.LifePathRewards == null || !action.LifePathRewards.Any(reward => reward != null)) {
                    report.Warning("Life Path reward action has no rewards assigned.", context);
                }
                break;
        }
    }

    static void ValidateMapMarkers(ProjectValidationReport report) {
        foreach(var marker in ProjectValidatorAssetFinder.FindAssets<MapMarkerDefinition>()) {
            if(marker == null) continue;

            string context = $"MapMarker/{marker.name}";
            if(string.IsNullOrWhiteSpace(marker.Id)) {
                report.Error("Map marker id is empty.", context);
            }

            if(!marker.ShowOnMinimap && !marker.ShowOnWorldMap) {
                report.Warning("Map marker is hidden from both minimap and world map.", context);
            }

            if(marker.VisibilityMode == MapMarkerVisibilityMode.HiddenUntilPokeNavUnlock && marker.PokeNavEntry == null) {
                report.Warning("Marker uses PokeNav unlock visibility but has no PokeNav entry.", context);
            }

            if(marker.VisibilityMode == MapMarkerVisibilityMode.HiddenUntilRegionDiscovery && marker.Region == null) {
                report.Warning("Marker uses region discovery visibility but has no region.", context);
            }

            if(marker.RelatedPokemon != null && marker.Category != MapMarkerCategory.Pokemon && marker.Category != MapMarkerCategory.Encounter && marker.Category != MapMarkerCategory.Event) {
                report.Info("Marker has a related Pokemon but is not categorized as Pokemon, Encounter or Event.", context);
            }
        }
    }

    static void ValidateMapViewProfiles(ProjectValidationReport report) {
        foreach(var profile in ProjectValidatorAssetFinder.FindAssets<MapViewProfileDefinition>()) {
            if(profile == null) continue;

            string context = $"MapViewProfile/{profile.name}";
            if(string.IsNullOrWhiteSpace(profile.Id)) {
                report.Error("Map view profile id is empty.", context);
            }

            if(profile.Tags != null && profile.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Map view profile has an empty tag slot.", context);
            }

            if(profile.RequiredTags != null && profile.RequiredTags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Map view profile has an empty required tag slot.", context);
            }

            if(profile.BlockedTags != null && profile.BlockedTags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Map view profile has an empty blocked tag slot.", context);
            }

            if(profile.UseMaxDistance && profile.MaxDistance <= 0f) {
                report.Warning("Map view profile uses max distance but Max Distance is 0.", context);
            }

            if(profile.AllowedCategories != null && profile.BlockedCategories != null) {
                foreach(var category in profile.AllowedCategories) {
                    if(profile.BlockedCategories.Contains(category)) {
                        report.Warning($"Map view profile both allows and blocks category '{category}'.", context);
                    }
                }
            }

            if(profile.Mode == MapViewMode.Custom && !profile.RequireMinimapEligible && !profile.RequireWorldMapEligible) {
                report.Info("Custom map view profile does not require minimap or world map eligibility. This is fine for debug/custom overlays.", context);
            }
        }
    }

    static void ValidateRumors(ProjectValidationReport report) {
        foreach(var rumor in ProjectValidatorAssetFinder.FindAssets<RumorDefinition>()) {
            if(rumor == null) continue;

            string context = $"Rumor/{rumor.name}";
            if(string.IsNullOrWhiteSpace(rumor.Id)) {
                report.Error("Rumor id is empty.", context);
            }

            if(string.IsNullOrWhiteSpace(rumor.Body) && string.IsNullOrWhiteSpace(rumor.Teaser)) {
                report.Warning("Rumor has no teaser or body text.", context);
            }

            if(!rumor.UnlockedByDefault) {
                report.Info("Rumor is not unlocked by default. Make sure a source, title, job or script unlocks it.", context);
            }

            if(rumor.RelatedSocialPost == null && rumor.RelatedMapMarker == null && rumor.RelatedRegion == null && rumor.RelatedPokeNavEntry == null && rumor.RelatedPokemon == null) {
                report.Info("Rumor has no related PokeNav, map, region or Pokemon data. This is fine for flavor rumors.", context);
            }

            if(rumor.SpreadProfile != null && !rumor.SeedLifecycleFromSources && !rumor.UnlockedByDefault) {
                report.Info("Rumor uses lifecycle but cannot be seeded by sources and is not unlocked by default. Make sure a script seeds it.", context);
            }
        }

        foreach(var profile in ProjectValidatorAssetFinder.FindAssets<RumorSpreadProfileDefinition>()) {
            if(profile == null) continue;

            string context = $"RumorSpreadProfile/{profile.name}";
            if(string.IsNullOrWhiteSpace(profile.Id)) {
                report.Error("Rumor spread profile id is empty.", context);
            }

            if(profile.ArchivedAfterHours > 0 && profile.ForgottenAfterHours > 0 && profile.ArchivedAfterHours < profile.ForgottenAfterHours) {
                report.Warning("Rumor spread profile archives before it is forgotten.", context);
            }

            if(profile.SpreadSteps == null || profile.SpreadSteps.Count == 0) {
                report.Info("Rumor spread profile has no spread steps. Rumors will stay near their origin/archive sources.", context);
            }

            foreach(var step in profile.SpreadSteps) {
                if(step == null) {
                    report.Warning("Rumor spread profile has a null spread step slot.", context);
                    continue;
                }

                if(!step.reachesAnyRegion && !step.includeOriginRegion && (step.regions == null || step.regions.Count == 0)) {
                    report.Warning("Rumor spread step cannot reach any region.", context);
                }
            }
        }

        foreach(var source in ProjectValidatorAssetFinder.FindAssets<RumorSource>()) {
            if(source == null) continue;

            string context = $"RumorSource/{source.name}";
            if(source.Rumors.Any(rumor => rumor != null && rumor.SpreadProfile != null) && source.Region == null) {
                report.Info("Rumor source has lifecycle rumors but no region. It can still match type/tag rules, but regional spread will be limited.", context);
            }

            if(source.SourceTags != null && source.SourceTags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Rumor source has an empty source tag slot.", context);
            }
        }
    }

    static void ValidateWorldConditions(ProjectValidationReport report) {
        foreach(var condition in ProjectValidatorAssetFinder.FindAssets<WorldConditionDefinition>()) {
            if(condition == null) continue;

            string context = $"WorldCondition/{condition.name}";
            if(string.IsNullOrWhiteSpace(condition.Id)) {
                report.Error("World condition id is empty.", context);
            }

            if(condition.Tags != null && condition.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("World condition has an empty tag slot.", context);
            }

            if(!condition.GlobalScope
                && condition.Regions.Count == 0
                && condition.RegionTags.Count == 0
                && condition.Zones.Count == 0
                && condition.ZoneTags.Count == 0) {
                report.Warning("World condition is not global but has no region or zone scope targets.", context);
            }

            if(!condition.AffectsAllActivities
                && condition.AffectedActivities.Count == 0
                && condition.AffectedActivityTags.Count == 0) {
                report.Warning("World condition does not affect any activity because no activity or tag targets are configured.", context);
            }

            bool hasAnyEffect = condition.BlocksActivities
                || !Mathf.Approximately(condition.ExperienceMultiplier, 1f)
                || condition.FlatExperienceBonus != 0
                || condition.YieldBonus != 0
                || condition.ResearchPointBonus != 0
                || condition.PokemonCareBonus != 0
                || !Mathf.Approximately(condition.ItemCostMultiplier, 1f)
                || !Mathf.Approximately(condition.ToolDurabilityCostMultiplier, 1f)
                || !Mathf.Approximately(condition.NeedCostMultiplier, 1f)
                || !Mathf.Approximately(condition.EncounterRateMultiplier, 1f)
                || !Mathf.Approximately(condition.ShopPriceMultiplier, 1f)
                || !Mathf.Approximately(condition.RumorSpreadSpeedMultiplier, 1f)
                || !Mathf.Approximately(condition.RumorDecaySpeedMultiplier, 1f);

            if(!hasAnyEffect) {
                report.Info("World condition has no configured effect yet.", context);
            }
        }

        foreach(var source in ProjectValidatorAssetFinder.FindAssets<WorldConditionSource>()) {
            if(source == null) continue;

            string context = $"WorldConditionSource/{source.name}";
            if(source.Condition == null) {
                report.Warning("World condition source has no condition assigned.", context);
                continue;
            }

            if(source.DurationOverrideHours == 0 && source.Condition.DefaultDurationHours == 0) {
                report.Info("World condition source creates a non-expiring condition. This is fine for story/global state.", context);
            }

            if(!source.Condition.GlobalScope && source.Region == null && source.Zone == null) {
                report.Info("World condition source uses the condition's own scope because no runtime region/zone override is assigned.", context);
            }
        }
    }

    static void ValidateJourneyEnvironments(ProjectValidationReport report) {
        foreach(var profile in ProjectValidatorAssetFinder.FindAssets<JourneyEnvironmentProfileDefinition>()) {
            if(profile == null) continue;

            string context = $"JourneyEnvironment/{profile.name}";
            if(string.IsNullOrWhiteSpace(profile.Id)) {
                report.Error("Journey environment profile id is empty.", context);
            }

            if(profile.Tags != null && profile.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Journey environment profile has an empty tag slot.", context);
            }

            if(profile.Rules == null || profile.Rules.Count == 0) {
                report.Info("Journey environment profile has no rules.", context);
                continue;
            }

            var duplicateRules = profile.Rules
                .Where(rule => rule != null && !string.IsNullOrWhiteSpace(rule.RuleId))
                .GroupBy(rule => rule.RuleId, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1);
            foreach(var duplicate in duplicateRules) {
                report.Warning($"Journey environment profile has duplicate rule id '{duplicate.Key}'.", context);
            }

            foreach(var rule in profile.Rules) {
                if(rule == null) {
                    report.Warning("Journey environment profile has a null rule slot.", context);
                    continue;
                }

                ValidateJourneyEnvironmentRule(rule, report, $"{context}/Rule/{rule.RuleId}");
            }
        }

        foreach(var controller in ProjectValidatorAssetFinder.FindAssets<JourneyEnvironmentController>()) {
            if(controller == null) continue;

            string context = $"JourneyEnvironmentController/{controller.name}";
            if(controller.Profile == null) {
                report.Warning("Journey environment controller has no profile assigned.", context);
            }
        }
    }

    static void ValidateJourneyEnvironmentRule(JourneyEnvironmentRule rule, ProjectValidationReport report, string context) {
        if(rule.EvaluateChance <= 0f) {
            report.Warning("Journey environment rule chance is 0, so it cannot apply.", context);
        }

        if(rule.IntervalHours == 0) {
            report.Info("Journey environment rule has no interval/cooldown. Use carefully with frequent triggers.", context);
        }

        if(!rule.GlobalScope
            && rule.Regions.Count == 0
            && rule.RegionTags.Count == 0
            && rule.Zones.Count == 0
            && rule.ZoneTags.Count == 0) {
            report.Warning("Journey environment rule is not global but has no region or zone scope targets.", context);
        }

        if(!rule.HasAnyPayload) {
            report.Warning("Journey environment rule has no survival changes, Pokemon care changes, situation pools or Life Path rewards.", context);
        }

        if(rule.RegionTags != null && rule.RegionTags.Any(string.IsNullOrWhiteSpace)) {
            report.Warning("Journey environment rule has an empty region tag slot.", context);
        }

        if(rule.ZoneTags != null && rule.ZoneTags.Any(string.IsNullOrWhiteSpace)) {
            report.Warning("Journey environment rule has an empty zone tag slot.", context);
        }

        if(rule.RequiredWorldConditionTags != null && rule.RequiredWorldConditionTags.Any(string.IsNullOrWhiteSpace)) {
            report.Warning("Journey environment rule has an empty required world condition tag slot.", context);
        }

        if(rule.BlockedWorldConditionTags != null && rule.BlockedWorldConditionTags.Any(string.IsNullOrWhiteSpace)) {
            report.Warning("Journey environment rule has an empty blocked world condition tag slot.", context);
        }

        ValidateObjectList(rule.Regions, report, context, "Journey environment rule has a null region slot.");
        ValidateObjectList(rule.Zones, report, context, "Journey environment rule has a null activity zone slot.");
        ValidateObjectList(rule.RequiredWorldConditions, report, context, "Journey environment rule has a null required world condition slot.");
        ValidateObjectList(rule.BlockedWorldConditions, report, context, "Journey environment rule has a null blocked world condition slot.");
        ValidateObjectList(rule.ExtraRequirements, report, context, "Journey environment rule has a null requirement slot.");
        ValidateObjectList(rule.SituationEventPools, report, context, "Journey environment rule has a null situation event pool slot.");
        ValidateLifePathRewards(rule.LifePathRewards, report, context);

        foreach(var change in rule.SurvivalNeedChanges) {
            if(change == null) {
                report.Warning("Journey environment rule has a null survival need change slot.", context);
                continue;
            }

            if(change.Need == null && change.AmountPerHour != 0) {
                report.Warning("Journey environment survival change has an amount but no survival need.", context);
            }

            if(change.Need != null && change.AmountPerHour == 0) {
                report.Info($"Journey environment survival change for '{change.Need.DisplayName}' has 0 amount per hour.", context);
            }
        }

        foreach(var change in rule.PokemonCareNeedChanges) {
            if(change == null) {
                report.Warning("Journey environment rule has a null Pokemon care change slot.", context);
                continue;
            }

            if(change.Need == null && change.AmountPerHour != 0) {
                report.Warning("Journey environment Pokemon care change has an amount but no care need.", context);
            }

            if(change.Need != null && change.AmountPerHour == 0) {
                report.Info($"Journey environment Pokemon care change for '{change.Need.DisplayName}' has 0 amount per hour.", context);
            }
        }
    }

    static void ValidateJourneyIncidents(ProjectValidationReport report) {
        foreach(var incident in ProjectValidatorAssetFinder.FindAssets<JourneyIncidentDefinition>()) {
            if(incident == null) continue;

            string context = $"JourneyIncident/{incident.name}";
            if(string.IsNullOrWhiteSpace(incident.Id)) {
                report.Error("Journey incident id is empty.", context);
            }

            if(incident.Tags != null && incident.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Journey incident has an empty tag slot.", context);
            }

            if(!incident.GlobalScope
                && incident.AllowedRegions.Count == 0
                && incident.AllowedRegionTags.Count == 0
                && incident.AllowedZones.Count == 0
                && incident.AllowedZoneTags.Count == 0) {
                report.Warning("Journey incident has Global Scope disabled but no region or zone filters.", context);
            }

            if(incident.ActivationChance <= 0f) {
                report.Warning("Journey incident activation chance is 0, so it cannot activate.", context);
            }

            if(incident.BaseWeight <= 0) {
                report.Info("Journey incident has base weight 0. Board entries must provide their own weight if it should roll.", context);
            }

            if(incident.RepeatMode == ConsequenceChainRepeatMode.CooldownHours && incident.CooldownHours <= 0) {
                report.Info("Journey incident uses Cooldown Hours repeat mode with 0 cooldown.", context);
            }

            if(incident.DurationHours <= 0 && incident.ExpireAutomatically) {
                report.Info("Journey incident has no active duration. Expire Automatically has no effect.", context);
            }

            ValidateObjectList(incident.AllowedRegions, report, context, "Journey incident has a null allowed region slot.");
            ValidateObjectList(incident.AllowedZones, report, context, "Journey incident has a null allowed zone slot.");
            ValidateObjectList(incident.Requirements, report, context, "Journey incident has a null requirement slot.");
            ValidateLifePathRewards(incident.LifePathRewardsOnActivate, report, context);
            ValidateLifePathRewards(incident.LifePathRewardsOnResolve, report, context);
            ValidateLifePathRewards(incident.LifePathRewardsOnExpire, report, context);
            ValidateObjectList(incident.ConsequenceChainsOnActivate, report, context, "Journey incident has a null activate consequence chain slot.");
            ValidateObjectList(incident.ConsequenceChainsOnResolve, report, context, "Journey incident has a null resolve consequence chain slot.");
            ValidateObjectList(incident.ConsequenceChainsOnExpire, report, context, "Journey incident has a null expire consequence chain slot.");

            if(!JourneyIncidentHasEffects(incident)) {
                report.Info("Journey incident has no situation, Life Path or consequence effects. It can still be used as a timed marker.", context);
            }
        }

        foreach(var board in ProjectValidatorAssetFinder.FindAssets<JourneyIncidentBoardDefinition>()) {
            if(board == null) continue;

            string context = $"JourneyIncidentBoard/{board.name}";
            if(string.IsNullOrWhiteSpace(board.Id)) {
                report.Error("Journey incident board id is empty.", context);
            }

            if(board.Tags != null && board.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Journey incident board has an empty tag slot.", context);
            }

            if(!board.GlobalScope
                && board.AllowedRegions.Count == 0
                && board.AllowedRegionTags.Count == 0
                && board.AllowedZones.Count == 0
                && board.AllowedZoneTags.Count == 0) {
                report.Warning("Journey incident board has Global Scope disabled but no region or zone filters.", context);
            }

            if(board.RollChance <= 0f) {
                report.Warning("Journey incident board roll chance is 0, so it cannot activate incidents.", context);
            }

            ValidateObjectList(board.Requirements, report, context, "Journey incident board has a null requirement slot.");
            ValidateObjectList(board.AllowedRegions, report, context, "Journey incident board has a null allowed region slot.");
            ValidateObjectList(board.AllowedZones, report, context, "Journey incident board has a null allowed zone slot.");

            if(board.Entries == null || board.Entries.Count == 0) {
                report.Info("Journey incident board has no entries.", context);
                continue;
            }

            var duplicateEntryIds = board.Entries
                .Where(entry => entry != null && entry.Incident != null && !string.IsNullOrWhiteSpace(entry.ResolveEntryId()))
                .GroupBy(entry => entry.ResolveEntryId(), StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1);
            foreach(var duplicate in duplicateEntryIds) {
                report.Warning($"Journey incident board has duplicate entry id '{duplicate.Key}'.", context);
            }

            foreach(var entry in board.Entries) {
                if(entry == null) {
                    report.Warning("Journey incident board has a null entry slot.", context);
                    continue;
                }

                string entryContext = entry.Incident != null ? $"{context}/Entry/{entry.Incident.Id}" : $"{context}/Entry";
                if(entry.Incident == null) {
                    report.Warning("Journey incident board entry has no incident assigned.", entryContext);
                }

                if(entry.Weight <= 0 && (entry.Incident == null || entry.Incident.BaseWeight <= 0)) {
                    report.Warning("Journey incident board entry has no weight and the incident has no base weight.", entryContext);
                }

                ValidateObjectList(entry.ExtraRequirements, report, entryContext, "Journey incident board entry has a null requirement slot.");
                foreach(var modifier in entry.WeightModifiers) {
                    if(modifier == null) {
                        report.Warning("Journey incident board entry has a null weight modifier slot.", entryContext);
                    } else if(modifier.condition == null) {
                        report.Warning("Journey incident board weight modifier has no world condition.", entryContext);
                    }
                }
            }
        }

        foreach(var source in ProjectValidatorAssetFinder.FindAssets<JourneyIncidentSource>()) {
            if(source == null) continue;

            string context = $"JourneyIncidentSource/{source.name}";
            if(source.Incident == null && source.Board == null) {
                report.Warning("Journey incident source has no incident or board assigned.", context);
            }

            if((source.InteractAction == JourneyIncidentSourceAction.RollBoard || source.TriggerAction == JourneyIncidentSourceAction.RollBoard)
                && source.Board == null) {
                report.Warning("Journey incident source can roll a board but has no board assigned.", context);
            }

            bool usesConfiguredIncident = source.InteractAction == JourneyIncidentSourceAction.ActivateConfiguredIncident
                || source.InteractAction == JourneyIncidentSourceAction.ResolveConfiguredIncident
                || source.InteractAction == JourneyIncidentSourceAction.ExpireConfiguredIncident
                || source.TriggerAction == JourneyIncidentSourceAction.ActivateConfiguredIncident
                || source.TriggerAction == JourneyIncidentSourceAction.ResolveConfiguredIncident
                || source.TriggerAction == JourneyIncidentSourceAction.ExpireConfiguredIncident;

            if(usesConfiguredIncident && source.Incident == null) {
                report.Warning("Journey incident source uses a configured incident action but has no incident assigned.", context);
            }
        }
    }

    static bool JourneyIncidentHasEffects(JourneyIncidentDefinition incident) {
        if(incident == null) {
            return false;
        }

        return incident.SituationEventOnActivate != null
            || incident.SituationPoolOnActivate != null
            || incident.LifePathRewardsOnActivate.Any(entry => entry != null && entry.lifePath != null && entry.HasAnyPayload)
            || incident.LifePathRewardsOnResolve.Any(entry => entry != null && entry.lifePath != null && entry.HasAnyPayload)
            || incident.LifePathRewardsOnExpire.Any(entry => entry != null && entry.lifePath != null && entry.HasAnyPayload)
            || incident.ConsequenceChainsOnActivate.Any(entry => entry != null)
            || incident.ConsequenceChainsOnResolve.Any(entry => entry != null)
            || incident.ConsequenceChainsOnExpire.Any(entry => entry != null);
    }

    static void ValidateRiskIncidents(ProjectValidationReport report) {
        foreach(var incident in ProjectValidatorAssetFinder.FindAssets<RiskIncidentDefinition>()) {
            if(incident == null) continue;

            string context = $"RiskIncident/{incident.name}";
            if(string.IsNullOrWhiteSpace(incident.Id)) {
                report.Error("Risk incident id is empty.", context);
            }

            if(incident.Tags != null && incident.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Risk incident has an empty tag slot.", context);
            }

            bool hasRiskPoints = incident.HeatPoints > 0 || incident.SuspicionPoints > 0 || incident.EvidencePoints > 0;
            bool hasConsequences = incident.RecordLawViolation
                || (incident.ApplyReputationChanges && incident.ReputationChanges.Count > 0)
                || incident.MilestonesToComplete.Count > 0
                || incident.TitleGrants.Count > 0;

            if(!hasRiskPoints && !hasConsequences) {
                report.Info("Risk incident has no points or consequences. This is valid if it is only used as a marker.", context);
            }

            if(incident.RecordLawViolation && incident.LawViolation == null) {
                report.Warning("Risk incident records a law violation but has no law violation assigned.", context);
            }

            if(incident.PermanentUntilCleared && incident.ActiveDurationHours > 0) {
                report.Info("Risk incident is permanent until cleared; duration hours will be ignored.", context);
            }

            ValidateReputationChanges(incident.ReputationChanges, report, context);
            ValidateTitleGrants(incident.TitleGrants, report, context);

            foreach(var milestone in incident.MilestonesToComplete) {
                if(milestone == null) {
                    report.Warning("Risk incident has a null milestone slot.", context);
                }
            }
        }

        foreach(var source in ProjectValidatorAssetFinder.FindAssets<RiskSource>()) {
            if(source == null) continue;

            string context = $"RiskSource/{source.name}";
            if(source.Incident == null) {
                report.Warning("Risk source has no incident assigned.", context);
            }
        }
    }

    static void ValidateConsequenceChains(ProjectValidationReport report) {
        foreach(var chain in ProjectValidatorAssetFinder.FindAssets<ConsequenceChainDefinition>()) {
            if(chain == null) continue;

            string context = $"ConsequenceChain/{chain.name}";
            if(string.IsNullOrWhiteSpace(chain.Id)) {
                report.Error("Consequence chain id is empty.", context);
            }

            if(chain.Tags != null && chain.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Consequence chain has an empty tag slot.", context);
            }

            foreach(var requirement in chain.Requirements) {
                if(requirement == null) {
                    report.Warning("Consequence chain has a null requirement slot.", context);
                }
            }

            if(chain.Steps == null || chain.Steps.Count == 0) {
                report.Info("Consequence chain has no steps yet.", context);
                continue;
            }

            for(int i = 0; i < chain.Steps.Count; i++) {
                ValidateConsequenceStep(chain.Steps[i], report, $"{context}/Step {i}");
            }
        }

        foreach(var source in ProjectValidatorAssetFinder.FindAssets<ConsequenceChainSource>()) {
            if(source == null) continue;

            string context = $"ConsequenceChainSource/{source.name}";
            if(source.Chain == null) {
                report.Warning("Consequence chain source has no chain assigned.", context);
            }

            if(source.TriggerMode == ConsequenceChainSourceTriggerMode.ApplyWhenAccessFails && source.AccessProfile == null) {
                report.Warning("Consequence chain source applies when access fails but has no access profile.", context);
            }
        }
    }

    static void ValidateConsequenceStep(ConsequenceChainStep step, ProjectValidationReport report, string context) {
        if(step == null) {
            report.Warning("Consequence chain has a null step slot.", context);
            return;
        }

        foreach(var requirement in step.Requirements) {
            if(requirement == null) {
                report.Warning("Consequence step has a null requirement slot.", context);
            }
        }

        switch(step.Action) {
            case ConsequenceStepAction.CompleteMilestones:
                if(step.Milestones == null || step.Milestones.Count == 0) {
                    report.Warning("Complete Milestones step has no milestones.", context);
                }

                foreach(var milestone in step.Milestones) {
                    if(milestone == null) {
                        report.Warning("Complete Milestones step has a null milestone slot.", context);
                    }
                }
                break;
            case ConsequenceStepAction.ApplyTitleGrants:
                ValidateTitleGrants(step.TitleGrants, report, context);
                break;
            case ConsequenceStepAction.ApplyReputationChanges:
                ValidateReputationChanges(step.ReputationChanges, report, context);
                break;
            case ConsequenceStepAction.ApplyRelationshipChanges:
                ValidateRelationshipChanges(step.RelationshipChanges, report, context);
                break;
            case ConsequenceStepAction.ApplyLifePathRewards:
                if(step.LifePathRewards == null || step.LifePathRewards.Count == 0) {
                    report.Warning("Apply Life Path Rewards step has no rewards.", context);
                }

                ValidateLifePathRewards(step.LifePathRewards, report, context);
                break;
            case ConsequenceStepAction.RecordRiskIncident:
                if(step.RiskIncident == null) {
                    report.Warning("Record Risk Incident step has no risk incident.", context);
                }
                break;
            case ConsequenceStepAction.RecordLawViolation:
                if(step.LawViolation == null) {
                    report.Warning("Record Law Violation step has no law violation.", context);
                }
                break;
            case ConsequenceStepAction.ActivateWorldCondition:
            case ConsequenceStepAction.DeactivateWorldCondition:
            case ConsequenceStepAction.ToggleWorldCondition:
                if(step.WorldCondition == null) {
                    report.Warning("World condition step has no world condition.", context);
                }
                break;
            case ConsequenceStepAction.UnlockRumor:
            case ConsequenceStepAction.HearRumor:
                if(step.Rumor == null) {
                    report.Warning("Rumor step has no rumor.", context);
                }
                break;
            case ConsequenceStepAction.SeedRumorLifecycle:
                if(step.Rumor == null) {
                    report.Warning("Seed Rumor Lifecycle step has no rumor.", context);
                }

                if(step.RumorSource == null) {
                    report.Info("Seed Rumor Lifecycle step has no rumor source override. It can still use the chain source/context rumor source.", context);
                }
                break;
            case ConsequenceStepAction.SetSceneObjectState:
            case ConsequenceStepAction.ClearSceneObjectState:
            case ConsequenceStepAction.RecordSceneObjectInteraction:
                if(step.SceneObject == null) {
                    report.Warning("Scene object step has no scene object assigned.", context);
                }
                break;
            case ConsequenceStepAction.RecordWorldDiscovery:
                if(step.WorldDiscovery == null) {
                    report.Warning("Record World Discovery step has no world discovery assigned.", context);
                }
                break;
            case ConsequenceStepAction.RecordLocationVisit:
                if(step.LocationVisit == null) {
                    report.Warning("Record Location Visit step has no location visit assigned.", context);
                }
                break;
            case ConsequenceStepAction.RecordChronicleEntry:
                if(step.ChronicleEntry == null) {
                    report.Warning("Record Chronicle Entry step has no chronicle entry assigned.", context);
                }
                break;
            case ConsequenceStepAction.ActivateNavigationHint:
            case ConsequenceStepAction.CompleteNavigationHint:
            case ConsequenceStepAction.ClearNavigationHint:
                if(step.NavigationHint == null) {
                    report.Warning("Navigation Hint step has no navigation hint assigned.", context);
                }
                break;
            case ConsequenceStepAction.EnterAreaProfile:
            case ConsequenceStepAction.ExitAreaProfile:
                if(step.AreaProfile == null) {
                    report.Warning("Area Profile step has no area profile assigned.", context);
                }
                break;
        }
    }

    static void ValidateWorldTriggers(ProjectValidationReport report) {
        foreach(var trigger in ProjectValidatorAssetFinder.FindAssets<WorldTriggerDefinition>()) {
            if(trigger == null) continue;

            string context = $"WorldTrigger/{trigger.name}";
            if(string.IsNullOrWhiteSpace(trigger.Id)) {
                report.Error("World trigger id is empty.", context);
            }

            if(trigger.Tags != null && trigger.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("World trigger has an empty tag slot.", context);
            }

            foreach(var requirement in trigger.Requirements) {
                if(requirement == null) {
                    report.Warning("World trigger has a null requirement slot.", context);
                }
            }

            foreach(var filter in trigger.EventValueFilters) {
                if(filter == null) {
                    report.Warning("World trigger has a null event value filter slot.", context);
                    continue;
                }

                if(string.IsNullOrWhiteSpace(filter.Key)) {
                    report.Warning("World trigger event value filter has no key.", context);
                }
            }

            if(trigger.ConsequenceChains == null || trigger.ConsequenceChains.Count == 0) {
                report.Info("World trigger has no consequence chains. It can still record history, but it will not change game state.", context);
            }

            foreach(var chain in trigger.ConsequenceChains) {
                if(chain == null) {
                    report.Warning("World trigger has a null consequence chain slot.", context);
                }
            }

            if(trigger.TriggerKind == WorldTriggerKind.GameEvent
                && trigger.EventValueFilters.Count == 0
                && trigger.ConsequenceChains.Count > 0) {
                report.Info("Game Event world trigger has no value filters. Make sure its event id/category filters are specific enough.", context);
            }
        }

        foreach(var controller in ProjectValidatorAssetFinder.FindAssets<WorldTriggerController>()) {
            if(controller == null) continue;

            string context = $"WorldTriggerController/{controller.name}";
            if(controller.Triggers == null || controller.Triggers.Count == 0) {
                report.Info("World trigger controller has no triggers assigned.", context);
                continue;
            }

            foreach(var trigger in controller.Triggers) {
                if(trigger == null) {
                    report.Warning("World trigger controller has a null trigger slot.", context);
                }
            }
        }

        foreach(var source in ProjectValidatorAssetFinder.FindAssets<WorldTriggerSource>()) {
            if(source == null) continue;

            string context = $"WorldTriggerSource/{source.name}";
            if(source.Triggers == null || source.Triggers.Count == 0) {
                report.Warning("World trigger source has no triggers assigned.", context);
                continue;
            }

            foreach(var trigger in source.Triggers) {
                if(trigger == null) {
                    report.Warning("World trigger source has a null trigger slot.", context);
                }
            }
        }
    }

    static void ValidateSituationEvents(ProjectValidationReport report) {
        foreach(var situation in ProjectValidatorAssetFinder.FindAssets<SituationEventDefinition>()) {
            if(situation == null) continue;

            string context = $"SituationEvent/{situation.name}";
            if(string.IsNullOrWhiteSpace(situation.Id)) {
                report.Error("Situation event id is empty.", context);
            }

            if(situation.Tags != null && situation.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Situation event has an empty tag slot.", context);
            }

            if(!situation.GlobalScope
                && situation.AllowedRegions.Count == 0
                && situation.AllowedRegionTags.Count == 0
                && situation.AllowedZones.Count == 0
                && situation.AllowedZoneTags.Count == 0) {
                report.Warning("Situation event has Global Scope disabled but no region or zone filters.", context);
            }

            if(situation.StartChance <= 0f) {
                report.Warning("Situation event start chance is 0, so it cannot start from pools.", context);
            }

            if(situation.BaseWeight <= 0) {
                report.Info("Situation event has base weight 0. Pool entries must provide their own weight if it should roll.", context);
            }

            if(situation.RepeatMode == ConsequenceChainRepeatMode.CooldownHours && situation.CooldownHours <= 0) {
                report.Info("Situation event uses Cooldown Hours repeat mode with 0 cooldown.", context);
            }

            if(situation.DurationHours <= 0 && situation.ExpireAutomatically) {
                report.Info("Situation event has no active duration. Expire Automatically has no effect.", context);
            }

            ValidateObjectList(situation.AllowedRegions, report, context, "Situation event has a null allowed region slot.");
            ValidateObjectList(situation.AllowedZones, report, context, "Situation event has a null allowed zone slot.");
            ValidateObjectList(situation.Requirements, report, context, "Situation event has a null requirement slot.");
            ValidateObjectList(situation.RequiredWorldConditions, report, context, "Situation event has a null required world condition slot.");
            ValidateObjectList(situation.BlockedWorldConditions, report, context, "Situation event has a null blocked world condition slot.");
            ValidateSituationWorldConditionActivations(situation.WorldConditionsOnStart, report, context);
            ValidateLifePathRewards(situation.LifePathRewardsOnStart, report, context);
            ValidateLifePathRewards(situation.LifePathRewardsOnResolve, report, context);
            ValidateLifePathRewards(situation.LifePathRewardsOnExpire, report, context);
            ValidateObjectList(situation.ConsequenceChainsOnStart, report, context, "Situation event has a null start consequence chain slot.");
            ValidateObjectList(situation.ConsequenceChainsOnResolve, report, context, "Situation event has a null resolve consequence chain slot.");
            ValidateObjectList(situation.ConsequenceChainsOnExpire, report, context, "Situation event has a null expire consequence chain slot.");

            if(!SituationEventHasEffects(situation)) {
                report.Info("Situation event has no world condition, life path or consequence effects. It can still be used as a timed marker.", context);
            }
        }

        foreach(var pool in ProjectValidatorAssetFinder.FindAssets<SituationEventPoolDefinition>()) {
            if(pool == null) continue;

            string context = $"SituationEventPool/{pool.name}";
            if(string.IsNullOrWhiteSpace(pool.Id)) {
                report.Error("Situation event pool id is empty.", context);
            }

            if(pool.Tags != null && pool.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Situation event pool has an empty tag slot.", context);
            }

            if(!pool.GlobalScope
                && pool.AllowedRegions.Count == 0
                && pool.AllowedRegionTags.Count == 0
                && pool.AllowedZones.Count == 0
                && pool.AllowedZoneTags.Count == 0) {
                report.Warning("Situation event pool has Global Scope disabled but no region or zone filters.", context);
            }

            if(pool.RollChance <= 0f) {
                report.Warning("Situation event pool roll chance is 0, so it cannot start events.", context);
            }

            if(pool.Entries == null || pool.Entries.Count == 0) {
                report.Warning("Situation event pool has no entries.", context);
                continue;
            }

            foreach(var entry in pool.Entries) {
                if(entry == null) {
                    report.Warning("Situation event pool has a null entry slot.", context);
                    continue;
                }

                string entryContext = entry.Event != null ? $"{context}/Entry/{entry.Event.Id}" : $"{context}/Entry";
                if(entry.Event == null) {
                    report.Warning("Situation event pool entry has no event assigned.", entryContext);
                }

                if(entry.Weight <= 0 && (entry.Event == null || entry.Event.BaseWeight <= 0)) {
                    report.Warning("Situation event pool entry has no weight and the event has no base weight.", entryContext);
                }

                ValidateObjectList(entry.ExtraRequirements, report, entryContext, "Situation event pool entry has a null requirement slot.");
                foreach(var modifier in entry.WeightModifiers) {
                    if(modifier == null) {
                        report.Warning("Situation event pool entry has a null weight modifier slot.", entryContext);
                    } else if(modifier.condition == null) {
                        report.Warning("Situation event weight modifier has no world condition.", entryContext);
                    }
                }
            }
        }

        foreach(var controller in ProjectValidatorAssetFinder.FindAssets<SituationEventController>()) {
            if(controller == null) continue;

            string context = $"SituationEventController/{controller.name}";
            if(controller.Pools == null || controller.Pools.Count == 0) {
                report.Info("Situation event controller has no pools assigned.", context);
                continue;
            }

            ValidateObjectList(controller.Pools, report, context, "Situation event controller has a null pool slot.");
        }

        foreach(var source in ProjectValidatorAssetFinder.FindAssets<SituationEventSource>()) {
            if(source == null) continue;

            string context = $"SituationEventSource/{source.name}";
            if(source.EventDefinition == null && source.Pool == null && (source.Pools == null || source.Pools.Count == 0)) {
                report.Warning("Situation event source has no event or pool target assigned.", context);
            }

            ValidateObjectList(source.Pools, report, context, "Situation event source has a null pool slot.");
        }

        foreach(var profile in ProjectValidatorAssetFinder.FindAssets<SituationEventSignalProfileDefinition>()) {
            if(profile == null) continue;

            string context = $"SituationEventSignalProfile/{profile.name}";
            if(string.IsNullOrWhiteSpace(profile.Id)) {
                report.Error("Situation event signal profile id is empty.", context);
            }

            if(profile.Tags != null && profile.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Situation event signal profile has an empty tag slot.", context);
            }

            if(profile.Rules == null || profile.Rules.Count == 0) {
                report.Info("Situation event signal profile has no rules.", context);
                continue;
            }

            var ruleIds = new HashSet<string>();
            foreach(var rule in profile.Rules) {
                if(rule == null) {
                    report.Warning("Situation event signal profile has a null rule slot.", context);
                    continue;
                }

                string ruleContext = $"{context}/Rule/{rule.RuleId}";
                if(!ruleIds.Add(rule.RuleId)) {
                    report.Warning($"Duplicate situation event signal rule id '{rule.RuleId}'.", ruleContext);
                }

                if(rule.EvaluateChance <= 0f) {
                    report.Warning("Situation event signal rule chance is 0, so it cannot evaluate.", ruleContext);
                }

                if(rule.CooldownHours == 0) {
                    report.Info("Situation event signal rule has no cooldown. Use carefully for frequent triggers.", ruleContext);
                }

                if(rule.Pools == null || rule.Pools.Count == 0) {
                    report.Warning("Situation event signal rule has no pools.", ruleContext);
                } else {
                    ValidateObjectList(rule.Pools, report, ruleContext, "Situation event signal rule has a null pool slot.");
                }

                ValidateObjectList(rule.ExtraRequirements, report, ruleContext, "Situation event signal rule has a null extra requirement slot.");
                ValidateSituationEventSignalRule(rule, report, ruleContext);
            }
        }

        foreach(var controller in ProjectValidatorAssetFinder.FindAssets<SituationEventSignalController>()) {
            if(controller == null) continue;

            string context = $"SituationEventSignalController/{controller.name}";
            if(controller.Profile == null) {
                report.Warning("Situation event signal controller has no profile assigned.", context);
            }
        }

        foreach(var manager in ProjectValidatorAssetFinder.FindAssets<SituationEventSignalUIManager>()) {
            if(manager == null) continue;

            string context = $"SituationEventSignalUIManager/{manager.name}";
            if(manager.Controller == null && manager.ProfileOverride == null) {
                report.Info("Situation event signal UI manager has no explicit controller or profile. It will resolve the first runtime signal controller.", context);
            }

            if(manager.MaxRuleRows == 1) {
                report.Info("Situation event signal UI manager only exposes one rule row. This is valid for compact debug widgets, but narrow for profile tuning panels.", context);
            }

            if(manager.MaxHistoryRows == 1) {
                report.Info("Situation event signal UI manager only exposes one history row. This is valid for compact HUDs, but narrow for signal log panels.", context);
            }
        }
    }

    static void ValidateSituationEventSignalRule(SituationEventSignalRule rule, ProjectValidationReport report, string context) {
        if(rule == null) {
            return;
        }

        switch(rule.Mode) {
            case SituationEventSignalMode.SpecificSurvivalNeedStateAtOrBelow:
                if(rule.SurvivalNeed == null) {
                    report.Warning("Specific survival signal rule has no survival need assigned.", context);
                }
                break;
            case SituationEventSignalMode.SpecificPokemonCareNeedStateAtOrBelow:
                if(rule.PokemonCareNeed == null) {
                    report.Warning("Specific Pokemon care signal rule has no care need assigned.", context);
                }
                break;
            case SituationEventSignalMode.RequiredQuestStatus:
                if(rule.RequiredQuest == null) {
                    report.Warning("Quest status signal rule has no quest assigned.", context);
                }
                break;
            case SituationEventSignalMode.ActiveAreaProfile:
                if(rule.RequiredAreaProfile == null && string.IsNullOrWhiteSpace(rule.RequiredAreaProfileTag)) {
                    report.Warning("Area profile signal rule has neither profile nor tag assigned.", context);
                }
                break;
            case SituationEventSignalMode.ActiveActivityZoneTag:
                if(string.IsNullOrWhiteSpace(rule.RequiredActivityZoneTag)) {
                    report.Warning("Activity zone tag signal rule has no tag assigned.", context);
                }
                break;
            case SituationEventSignalMode.WorldConditionState:
                if(rule.RequiredWorldCondition == null) {
                    report.Warning("World condition signal rule has no world condition assigned.", context);
                }
                break;
        }
    }

    static void ValidateSituationWorldConditionActivations(IEnumerable<SituationWorldConditionActivation> activations, ProjectValidationReport report, string context) {
        if(activations == null) {
            return;
        }

        foreach(var activation in activations) {
            if(activation == null) {
                report.Warning("Situation event has a null world condition activation slot.", context);
                continue;
            }

            if(activation.condition == null) {
                report.Warning("Situation event world condition activation has no condition assigned.", context);
            }
        }
    }

    static bool SituationEventHasEffects(SituationEventDefinition situation) {
        if(situation == null) {
            return false;
        }

        return situation.WorldConditionsOnStart.Any(entry => entry != null && entry.condition != null)
            || situation.LifePathRewardsOnStart.Any(entry => entry != null && entry.lifePath != null && entry.HasAnyPayload)
            || situation.LifePathRewardsOnResolve.Any(entry => entry != null && entry.lifePath != null && entry.HasAnyPayload)
            || situation.LifePathRewardsOnExpire.Any(entry => entry != null && entry.lifePath != null && entry.HasAnyPayload)
            || situation.ConsequenceChainsOnStart.Any(entry => entry != null)
            || situation.ConsequenceChainsOnResolve.Any(entry => entry != null)
            || situation.ConsequenceChainsOnExpire.Any(entry => entry != null);
    }

    static void ValidateSceneObjects(ProjectValidationReport report) {
        foreach(var sceneObject in ProjectValidatorAssetFinder.FindAssets<SceneObjectDefinition>()) {
            if(sceneObject == null) continue;

            string context = $"SceneObject/{sceneObject.name}";
            if(string.IsNullOrWhiteSpace(sceneObject.Id)) {
                report.Error("Scene object id is empty.", context);
            }

            if(sceneObject.Tags != null && sceneObject.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Scene object has an empty tag slot.", context);
            }

            if(!sceneObject.IsAvailableState(sceneObject.DefaultState)) {
                report.Info("Scene object default state is unavailable. Make sure a chain, trigger or save state can reveal it.", context);
            }
        }

        foreach(var conditional in ProjectValidatorAssetFinder.FindAssets<ConditionalSceneObject>()) {
            if(conditional == null) continue;

            string context = $"ConditionalSceneObject/{conditional.name}";
            if(conditional.SceneObject == null && (conditional.Requirements == null || conditional.Requirements.Count == 0)) {
                report.Info("Conditional scene object has no scene object definition and no requirements. It will usually remain available.", context);
            }

            foreach(var requirement in conditional.Requirements) {
                if(requirement == null) {
                    report.Warning("Conditional scene object has a null requirement slot.", context);
                }
            }

            foreach(var chain in conditional.BecameAvailableChains) {
                if(chain == null) {
                    report.Warning("Conditional scene object has a null Became Available chain slot.", context);
                }
            }

            foreach(var chain in conditional.BecameUnavailableChains) {
                if(chain == null) {
                    report.Warning("Conditional scene object has a null Became Unavailable chain slot.", context);
                }
            }

            foreach(var chain in conditional.SuccessfulInteractionChains) {
                if(chain == null) {
                    report.Warning("Conditional scene object has a null Successful Interaction chain slot.", context);
                }
            }

            foreach(var chain in conditional.BlockedInteractionChains) {
                if(chain == null) {
                    report.Warning("Conditional scene object has a null Blocked Interaction chain slot.", context);
                }
            }
        }
    }

    static void ValidateSceneSpawns(ProjectValidationReport report) {
        foreach(var profile in ProjectValidatorAssetFinder.FindAssets<SceneSpawnProfileDefinition>()) {
            if(profile == null) continue;

            string context = $"SceneSpawn/{profile.name}";
            if(string.IsNullOrWhiteSpace(profile.Id)) {
                report.Error("Scene spawn profile id is empty.", context);
            }

            if(profile.Tags != null && profile.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Scene spawn profile has an empty tag slot.", context);
            }

            foreach(var requirement in profile.Requirements) {
                if(requirement == null) {
                    report.Warning("Scene spawn profile has a null requirement slot.", context);
                }
            }

            if(profile.Entries == null || profile.Entries.Count == 0) {
                report.Warning("Scene spawn profile has no entries.", context);
                continue;
            }

            int totalWeight = 0;
            foreach(var entry in profile.Entries) {
                if(entry == null) {
                    report.Warning("Scene spawn profile has a null entry slot.", context);
                    continue;
                }

                if(entry.Enabled && entry.Prefab == null) {
                    report.Warning($"Scene spawn entry '{entry.EntryId}' is enabled but has no prefab.", context);
                }

                if(profile.SelectionMode == SceneSpawnSelectionMode.WeightedRandom && entry.Enabled) {
                    totalWeight += entry.Weight;
                    if(entry.Weight <= 0) {
                        report.Info($"Scene spawn entry '{entry.EntryId}' has 0 weight and will be ignored by weighted selection.", context);
                    }
                }

                foreach(var requirement in entry.Requirements) {
                    if(requirement == null) {
                        report.Warning($"Scene spawn entry '{entry.EntryId}' has a null requirement slot.", context);
                    }
                }

                foreach(var chain in entry.SpawnedChains) {
                    if(chain == null) {
                        report.Warning($"Scene spawn entry '{entry.EntryId}' has a null spawned chain slot.", context);
                    }
                }
            }

            if(profile.SelectionMode == SceneSpawnSelectionMode.WeightedRandom && totalWeight <= 0) {
                report.Warning("Weighted scene spawn profile has no positive entry weights.", context);
            }
        }

        foreach(var controller in ProjectValidatorAssetFinder.FindAssets<SceneSpawnController>()) {
            if(controller == null) continue;

            string context = $"SceneSpawnController/{controller.name}";
            if(controller.Profile == null) {
                report.Warning("Scene spawn controller has no profile assigned.", context);
            }

            if(!controller.UseOwnTransformAsFallbackPoint && (controller.SpawnPoints == null || controller.SpawnPoints.Count == 0 || controller.SpawnPoints.All(point => point == null))) {
                report.Warning("Scene spawn controller has no valid spawn points and fallback transform is disabled.", context);
            }

            if(controller.SpawnPoints != null) {
                foreach(var point in controller.SpawnPoints) {
                    if(point == null) {
                        report.Warning("Scene spawn controller has a null spawn point slot.", context);
                    }
                }
            }

            foreach(var chain in controller.BatchSpawnedChains) {
                if(chain == null) {
                    report.Warning("Scene spawn controller has a null Batch Spawned chain slot.", context);
                }
            }

            foreach(var chain in controller.BlockedChains) {
                if(chain == null) {
                    report.Warning("Scene spawn controller has a null Blocked chain slot.", context);
                }
            }
        }
    }

    static void ValidateWorldDiscoveries(ProjectValidationReport report) {
        foreach(var discovery in ProjectValidatorAssetFinder.FindAssets<WorldDiscoveryDefinition>()) {
            if(discovery == null) continue;

            string context = $"WorldDiscovery/{discovery.name}";
            if(string.IsNullOrWhiteSpace(discovery.Id)) {
                report.Error("World discovery id is empty.", context);
            }

            if(discovery.Tags != null && discovery.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("World discovery has an empty tag slot.", context);
            }

            foreach(var requirement in discovery.Requirements) {
                if(requirement == null) {
                    report.Warning("World discovery has a null requirement slot.", context);
                }
            }

            bool hasLinkedKnowledge = discovery.RelatedPokemon != null
                || discovery.RelatedRegion != null
                || discovery.PokeNavEntry != null
                || discovery.SocialPost != null
                || discovery.MapMarker != null;

            if(!hasLinkedKnowledge) {
                report.Info("World discovery has no linked Pokemon, region, PokeNav entry, social post or map marker. It can still record history only.", context);
            }

            if(discovery.RecordPokemonKnowledge && discovery.RelatedPokemon == null) {
                report.Info("World discovery records Pokemon knowledge but has no Related Pokemon assigned.", context);
            }

            if(discovery.DiscoverRegion && discovery.RelatedRegion == null) {
                report.Info("World discovery discovers region but has no Related Region assigned.", context);
            }

            if(discovery.DiscoverPokeNavEntry && discovery.PokeNavEntry == null) {
                report.Info("World discovery discovers PokeNav entry but has no PokeNav Entry assigned.", context);
            }

            if(discovery.UnlockSocialPost && discovery.SocialPost == null) {
                report.Info("World discovery unlocks social post but has no Social Post assigned.", context);
            }

            if(discovery.DiscoverMapMarker && discovery.MapMarker == null) {
                report.Info("World discovery discovers map marker but has no Map Marker assigned.", context);
            }
        }

        foreach(var source in ProjectValidatorAssetFinder.FindAssets<WorldDiscoverySource>()) {
            if(source == null) continue;

            string context = $"WorldDiscoverySource/{source.name}";
            if(source.Discoveries == null || source.Discoveries.Count == 0) {
                report.Warning("World discovery source has no discoveries assigned.", context);
            } else {
                foreach(var discovery in source.Discoveries) {
                    if(discovery == null) {
                        report.Warning("World discovery source has a null discovery slot.", context);
                    }
                }
            }

            foreach(var chain in source.SuccessfulDiscoveryChains) {
                if(chain == null) {
                    report.Warning("World discovery source has a null Successful Discovery chain slot.", context);
                }
            }

            foreach(var chain in source.BlockedDiscoveryChains) {
                if(chain == null) {
                    report.Warning("World discovery source has a null Blocked Discovery chain slot.", context);
                }
            }
        }
    }

    static void ValidateLocationVisits(ProjectValidationReport report) {
        foreach(var visit in ProjectValidatorAssetFinder.FindAssets<LocationVisitDefinition>()) {
            if(visit == null) continue;

            string context = $"LocationVisit/{visit.name}";
            if(string.IsNullOrWhiteSpace(visit.Id)) {
                report.Error("Location visit id is empty.", context);
            }

            if(visit.Tags != null && visit.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Location visit has an empty tag slot.", context);
            }

            foreach(var requirement in visit.Requirements) {
                if(requirement == null) {
                    report.Warning("Location visit has a null requirement slot.", context);
                }
            }

            bool hasLocationLink = visit.Region != null
                || visit.MapMarker != null
                || visit.ActivityZone != null
                || !string.IsNullOrWhiteSpace(visit.SceneName)
                || !string.IsNullOrWhiteSpace(visit.LocationKey)
                || (visit.WorldDiscoveries != null && visit.WorldDiscoveries.Count > 0);

            if(!hasLocationLink) {
                report.Info("Location visit has no region, map marker, activity zone, scene name, location key or linked world discovery. It can still record history only.", context);
            }

            if(visit.DiscoverRegion && visit.Region == null) {
                report.Info("Location visit discovers region but has no Region assigned.", context);
            }

            if(visit.DiscoverMapMarker && visit.MapMarker == null) {
                report.Info("Location visit discovers map marker but has no Map Marker assigned.", context);
            }

            foreach(var discovery in visit.WorldDiscoveries) {
                if(discovery == null) {
                    report.Warning("Location visit has a null world discovery slot.", context);
                }
            }

            foreach(var chain in visit.VisitChains) {
                if(chain == null) {
                    report.Warning("Location visit has a null Visit Chain slot.", context);
                }
            }
        }

        foreach(var source in ProjectValidatorAssetFinder.FindAssets<LocationVisitSource>()) {
            if(source == null) continue;

            string context = $"LocationVisitSource/{source.name}";
            if(source.Visits == null || source.Visits.Count == 0) {
                report.Warning("Location visit source has no visits assigned.", context);
            } else {
                foreach(var visit in source.Visits) {
                    if(visit == null) {
                        report.Warning("Location visit source has a null visit slot.", context);
                    }
                }
            }

            foreach(var chain in source.SuccessfulVisitChains) {
                if(chain == null) {
                    report.Warning("Location visit source has a null Successful Visit chain slot.", context);
                }
            }

            foreach(var chain in source.BlockedVisitChains) {
                if(chain == null) {
                    report.Warning("Location visit source has a null Blocked Visit chain slot.", context);
                }
            }
        }
    }

    static void ValidateChronicleEntries(ProjectValidationReport report) {
        foreach(var entry in ProjectValidatorAssetFinder.FindAssets<ChronicleEntryDefinition>()) {
            if(entry == null) continue;

            string context = $"ChronicleEntry/{entry.name}";
            if(string.IsNullOrWhiteSpace(entry.Id)) {
                report.Error("Chronicle entry id is empty.", context);
            }

            if(string.IsNullOrWhiteSpace(entry.Title) && string.IsNullOrWhiteSpace(entry.Message)) {
                report.Warning("Chronicle entry has no title or message text.", context);
            }

            if(entry.Tags != null && entry.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Chronicle entry has an empty tag slot.", context);
            }

            foreach(var requirement in entry.Requirements) {
                if(requirement == null) {
                    report.Warning("Chronicle entry has a null requirement slot.", context);
                }
            }

            foreach(var chain in entry.EntryChains) {
                if(chain == null) {
                    report.Warning("Chronicle entry has a null Entry Chain slot.", context);
                }
            }
        }

        foreach(var rule in ProjectValidatorAssetFinder.FindAssets<ChronicleCaptureRuleDefinition>()) {
            if(rule == null) continue;

            string context = $"ChronicleCaptureRule/{rule.name}";
            if(string.IsNullOrWhiteSpace(rule.Id)) {
                report.Error("Chronicle capture rule id is empty.", context);
            }

            if(rule.Enabled && rule.EventCategories.Count == 0 && rule.EventScopes.Count == 0) {
                report.Info("Chronicle capture rule has no category or scope filter, so it may match many events.", context);
            }

            if(rule.OutputTags != null && rule.OutputTags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Chronicle capture rule has an empty output tag slot.", context);
            }
        }

        foreach(var source in ProjectValidatorAssetFinder.FindAssets<ChronicleSource>()) {
            if(source == null) continue;

            string context = $"ChronicleSource/{source.name}";
            if(source.Entries == null || source.Entries.Count == 0) {
                report.Warning("Chronicle source has no entries assigned.", context);
            } else {
                foreach(var entry in source.Entries) {
                    if(entry == null) {
                        report.Warning("Chronicle source has a null entry slot.", context);
                    }
                }
            }

            foreach(var chain in source.SuccessfulEntryChains) {
                if(chain == null) {
                    report.Warning("Chronicle source has a null Successful Entry chain slot.", context);
                }
            }

            foreach(var chain in source.BlockedEntryChains) {
                if(chain == null) {
                    report.Warning("Chronicle source has a null Blocked Entry chain slot.", context);
                }
            }
        }
    }

    static void ValidateNavigationHints(ProjectValidationReport report) {
        foreach(var hint in ProjectValidatorAssetFinder.FindAssets<NavigationHintDefinition>()) {
            if(hint == null) continue;

            string context = $"NavigationHint/{hint.name}";
            if(string.IsNullOrWhiteSpace(hint.Id)) {
                report.Error("Navigation hint id is empty.", context);
            }

            if(hint.Tags != null && hint.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Navigation hint has an empty tag slot.", context);
            }

            foreach(var requirement in hint.Requirements) {
                if(requirement == null) {
                    report.Warning("Navigation hint has a null requirement slot.", context);
                }
            }

            bool hasTarget = hint.Region != null
                || hint.MapMarker != null
                || hint.ActivityZone != null
                || hint.PokeNavEntry != null
                || hint.SocialPost != null
                || hint.LocationVisit != null
                || hint.UseStoredWorldPosition
                || !string.IsNullOrWhiteSpace(hint.SceneName)
                || !string.IsNullOrWhiteSpace(hint.LocationKey);

            if(!hasTarget) {
                report.Info("Navigation hint has no target link, stored position, scene name or location key. It can still be activated by a source with runtime position.", context);
            }

            if(hint.DiscoverRegion && hint.Region == null) {
                report.Info("Navigation hint discovers region but has no Region assigned.", context);
            }

            if(hint.DiscoverMapMarker && hint.MapMarker == null) {
                report.Info("Navigation hint discovers map marker but has no Map Marker assigned.", context);
            }

            if(hint.DiscoverPokeNavEntry && hint.PokeNavEntry == null) {
                report.Info("Navigation hint discovers PokeNav entry but has no PokeNav Entry assigned.", context);
            }

            if(hint.UnlockSocialPost && hint.SocialPost == null) {
                report.Info("Navigation hint unlocks social post but has no Social Post assigned.", context);
            }

            foreach(var discovery in hint.WorldDiscoveries) {
                if(discovery == null) {
                    report.Warning("Navigation hint has a null world discovery slot.", context);
                }
            }

            foreach(var chain in hint.ActivatedChains) {
                if(chain == null) {
                    report.Warning("Navigation hint has a null Activated Chain slot.", context);
                }
            }

            foreach(var chain in hint.CompletedChains) {
                if(chain == null) {
                    report.Warning("Navigation hint has a null Completed Chain slot.", context);
                }
            }

            foreach(var chain in hint.ClearedChains) {
                if(chain == null) {
                    report.Warning("Navigation hint has a null Cleared Chain slot.", context);
                }
            }
        }

        foreach(var source in ProjectValidatorAssetFinder.FindAssets<NavigationHintSource>()) {
            if(source == null) continue;

            string context = $"NavigationHintSource/{source.name}";
            if(source.Hints == null || source.Hints.Count == 0) {
                report.Warning("Navigation hint source has no hints assigned.", context);
            } else {
                foreach(var hint in source.Hints) {
                    if(hint == null) {
                        report.Warning("Navigation hint source has a null hint slot.", context);
                    }
                }
            }

            foreach(var chain in source.SuccessfulHintChains) {
                if(chain == null) {
                    report.Warning("Navigation hint source has a null Successful Hint chain slot.", context);
                }
            }

            foreach(var chain in source.BlockedHintChains) {
                if(chain == null) {
                    report.Warning("Navigation hint source has a null Blocked Hint chain slot.", context);
                }
            }
        }
    }

    static void ValidateAreaProfiles(ProjectValidationReport report) {
        foreach(var profile in ProjectValidatorAssetFinder.FindAssets<AreaProfileDefinition>()) {
            if(profile == null) continue;

            string context = $"AreaProfile/{profile.name}";
            if(string.IsNullOrWhiteSpace(profile.Id)) {
                report.Error("Area profile id is empty.", context);
            }

            if(profile.Tags != null && profile.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Area profile has an empty tag slot.", context);
            }

            foreach(var requirement in profile.Requirements) {
                if(requirement == null) {
                    report.Warning("Area profile has a null requirement slot.", context);
                }
            }

            bool hasAreaContext = profile.Region != null
                || profile.ActivityZone != null
                || profile.MapMarker != null
                || !string.IsNullOrWhiteSpace(profile.SceneName)
                || !string.IsNullOrWhiteSpace(profile.AreaKey);

            if(!hasAreaContext) {
                report.Info("Area profile has no region, activity zone, map marker, scene name or area key. It can still use the runtime source position.", context);
            }

            ValidateObjectList(profile.EnterLocationVisits, report, context, "Area profile has a null Enter Location Visit slot.");
            ValidateObjectList(profile.EnterWorldDiscoveries, report, context, "Area profile has a null Enter World Discovery slot.");
            ValidateObjectList(profile.EnterChronicleEntries, report, context, "Area profile has a null Enter Chronicle Entry slot.");
            ValidateObjectList(profile.ExitChronicleEntries, report, context, "Area profile has a null Exit Chronicle Entry slot.");
            ValidateObjectList(profile.EnteredChains, report, context, "Area profile has a null Entered Chain slot.");
            ValidateObjectList(profile.ExitedChains, report, context, "Area profile has a null Exited Chain slot.");

            foreach(var action in profile.EnterNavigationHints) {
                if(action == null || action.Hint == null) {
                    report.Warning("Area profile has an enter navigation hint action without a hint.", context);
                }
            }

            foreach(var action in profile.ExitNavigationHints) {
                if(action == null || action.Hint == null) {
                    report.Warning("Area profile has an exit navigation hint action without a hint.", context);
                }
            }

            foreach(var change in profile.EnterWorldConditions) {
                if(change == null || change.Condition == null) {
                    report.Warning("Area profile has an enter world condition change without a condition.", context);
                }
            }

            foreach(var change in profile.ExitWorldConditions) {
                if(change == null || change.Condition == null) {
                    report.Warning("Area profile has an exit world condition change without a condition.", context);
                }
            }
        }

        foreach(var source in ProjectValidatorAssetFinder.FindAssets<AreaProfileSource>()) {
            if(source == null) continue;

            string context = $"AreaProfileSource/{source.name}";
            if(source.Profile == null) {
                report.Warning("Area profile source has no profile assigned.", context);
            }
        }
    }

    static void ValidateCalendarEvents(ProjectValidationReport report) {
        foreach(var calendarEvent in ProjectValidatorAssetFinder.FindAssets<CalendarEventDefinition>()) {
            if(calendarEvent == null) continue;

            string context = $"CalendarEvent/{calendarEvent.name}";
            if(string.IsNullOrWhiteSpace(calendarEvent.Id)) {
                report.Error("Calendar event id is empty.", context);
            }

            if(string.IsNullOrWhiteSpace(calendarEvent.Summary) && string.IsNullOrWhiteSpace(calendarEvent.Details)) {
                report.Warning("Calendar event has no summary or details text.", context);
            }

            if(calendarEvent.UseEndDay && calendarEvent.EndDay < calendarEvent.StartDay) {
                report.Warning("Calendar event end day is earlier than start day.", context);
            }

            if(calendarEvent.RepeatMode == CalendarRepeatMode.Weekly && calendarEvent.ActiveWeekDays != null && calendarEvent.ActiveWeekDays.Count == 0) {
                report.Info("Weekly calendar event has no weekdays selected, so every weekday is valid.", context);
            }

            if(calendarEvent.RepeatMode == CalendarRepeatMode.SpecificDays && (calendarEvent.SpecificDays == null || calendarEvent.SpecificDays.Count == 0)) {
                report.Warning("Specific Days calendar event has no specific days.", context);
            }

            if(!calendarEvent.UnlockedByDefault) {
                report.Info("Calendar event is not unlocked by default. Make sure a source, title, job or script unlocks it.", context);
            }
        }
    }

    static void ValidateBattleAIProfiles(ProjectValidationReport report) {
        foreach(var profile in ProjectValidatorAssetFinder.FindAssets<BattleAIProfile>()) {
            if(profile == null) continue;

            string context = $"BattleAIProfile/{profile.name}";
            if(string.IsNullOrWhiteSpace(profile.Id)) {
                report.Error("Battle AI profile id is empty.", context);
            }

            if(profile.Randomness >= 0.9f && profile.Tier == BattleAITier.Champion) {
                report.Info("Champion AI profile has very high randomness. Confirm this is intentional.", context);
            }

            if(profile.AllowSwitching && profile.Tier == BattleAITier.Wild) {
                report.Info("Wild AI profile can switch Pokemon. This only works when a party exists and switching is allowed by battle rules.", context);
            }
        }

        foreach(var trainer in ProjectValidatorAssetFinder.FindAssets<TrainerController>()) {
            if(trainer == null) continue;

            string context = $"TrainerAI/{trainer.name}";
            if(trainer.BattleAIProfile == null) {
                report.Info("Trainer has no AI profile override and will use BattleSystem's default trainer AI.", context);
            }
        }
    }

    static void ValidateBattleModes(ProjectValidationReport report) {
        foreach(var mode in ProjectValidatorAssetFinder.FindAssets<BattleModeDefinition>()) {
            if(mode == null) continue;

            string context = $"BattleMode/{mode.name}";
            if(string.IsNullOrWhiteSpace(mode.Id)) {
                report.Error("Battle mode id is empty.", context);
            }

            if(string.IsNullOrWhiteSpace(mode.BattleSystemKey)) {
                report.Warning("Battle mode has an empty battle system key.", context);
            }

            if(mode.Tags != null && mode.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Battle mode has an empty tag slot.", context);
            }

            if(!mode.ImplementedInCurrentBattleSystem && !mode.AllowFallbackToClassic) {
                report.Info("Battle mode is not implemented and cannot fall back to classic. It will block battle start until a backend exists.", context);
            }

            if(mode.Kind == BattleModeKind.CommandPalette && !mode.UsesKnownMovePalette) {
                report.Info("Command Palette mode does not use the known-move palette flag. Confirm the metadata matches the intended UI.", context);
            }

            if(mode.UsesElementModifiers && !mode.UsesActionPoints && !mode.UsesStamina) {
                report.Info("Element modifier mode has no AP or stamina metadata. It can still work, but future UI may not show a resource cost.", context);
            }
        }

        foreach(var negotiator in ProjectValidatorAssetFinder.FindAssets<BattleRuleNegotiator>()) {
            if(negotiator == null || negotiator.ForcedBattleMode == null || negotiator.Challenge == null) continue;

            var allowedModes = negotiator.Challenge.AllowedBattleModes;
            if(allowedModes != null && allowedModes.Count > 0 && !allowedModes.Contains(negotiator.ForcedBattleMode)) {
                report.Warning("Battle negotiator forces a mode that is not listed in its challenge allowed modes.", $"BattleRuleNegotiator/{negotiator.name}");
            }
        }
    }

    static void ValidateBattleRuleSets(ProjectValidationReport report) {
        foreach(var ruleSet in ProjectValidatorAssetFinder.FindAssets<BattleRuleSetDefinition>()) {
            if(ruleSet == null) continue;

            string context = $"BattleRuleSet/{ruleSet.name}";
            if(string.IsNullOrWhiteSpace(ruleSet.Id)) {
                report.Error("Battle rule set id is empty.", context);
            }

            if(ruleSet.ExactPokemon > 0 && (ruleSet.MinPokemon > 0 || ruleSet.MaxPokemon > 0)) {
                report.Info("Exact Pokemon is set, so min/max Pokemon checks are ignored.", context);
            }

            if(ruleSet.MinPokemon > 0 && ruleSet.MaxPokemon > 0 && ruleSet.MinPokemon > ruleSet.MaxPokemon) {
                report.Warning("Minimum Pokemon is higher than maximum Pokemon.", context);
            }

            if(ruleSet.MinLevel > 0 && ruleSet.MaxLevel > 0 && ruleSet.MinLevel > ruleSet.MaxLevel) {
                report.Warning("Minimum level is higher than maximum level.", context);
            }

            if(ruleSet.ItemRule == BattleRuleItemRule.LimitedCount && ruleSet.MaxPlayerItemUses <= 0) {
                report.Warning("Item rule is Limited Count but max player item uses is 0.", context);
            }

            if(ruleSet.SwitchRule == BattleRuleSwitchRule.LimitedCount && ruleSet.MaxPlayerSwitches <= 0) {
                report.Warning("Switch rule is Limited Count but max player switches is 0.", context);
            }

            if(ruleSet.PowerMechanicRule == BattleRulePowerMechanicRule.LimitedCount
                && ruleSet.MaxPlayerPowerMechanicUses <= 0
                && ruleSet.MaxOpponentPowerMechanicUses <= 0) {
                report.Warning("Power mechanic rule is Limited Count but both player and opponent limits are 0.", context);
            }

            ValidateObjectList(ruleSet.AllowedPowerMechanics, report, context, "Battle rule set has a null allowed power mechanic slot.");
            ValidateObjectList(ruleSet.BannedPowerMechanics, report, context, "Battle rule set has a null banned power mechanic slot.");
        }
    }

    static void ValidatePowerMechanics(ProjectValidationReport report) {
        foreach(var mechanic in ProjectValidatorAssetFinder.FindAssets<PowerMechanicDefinition>()) {
            if(mechanic == null) continue;

            string context = $"PowerMechanic/{mechanic.name}";
            if(string.IsNullOrWhiteSpace(mechanic.Id)) {
                report.Error("Power mechanic id is empty.", context);
            }

            if(mechanic.Tags != null && mechanic.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Power mechanic has an empty tag slot.", context);
            }

            if(mechanic.Kind == PowerMechanicKind.MegaEvolution && mechanic.TemporaryPokemonBase == null) {
                report.Warning("Mega Evolution mechanic has no temporary Pokemon base assigned.", context);
            }

            if((mechanic.Kind == PowerMechanicKind.ZMove || mechanic.Kind == PowerMechanicKind.Gigantamax)
                && mechanic.SelectionMode == PowerMechanicSelectionMode.AttachToMove
                && mechanic.ReplacementMove == null) {
                report.Info("Move-attached power mechanic has no replacement move. It will act as a buff on the selected move.", context);
            }

            if(mechanic.RequiresSelectedMove && mechanic.SelectionMode == PowerMechanicSelectionMode.SeparateAction) {
                report.Warning("Mechanic requires a selected move but selection mode is Separate Action.", context);
            }

            if(mechanic.ConsumesTrainerCharge && mechanic.TrainerChargeCost <= 0) {
                report.Warning("Mechanic consumes trainer charge but charge cost is 0.", context);
            }

            if(mechanic.RequirePlayerUnlock && !mechanic.UnlockedByDefault) {
                report.Info("Mechanic requires player unlock. Make sure a reward/source unlocks it.", context);
            }

            ValidateObjectList(mechanic.AllowedPokemon, report, context, "Power mechanic has a null allowed Pokemon slot.");
            ValidateObjectList(mechanic.AllowedRuleSets, report, context, "Power mechanic has a null allowed rule set slot.");
            ValidateObjectList(mechanic.BannedRuleSets, report, context, "Power mechanic has a null banned rule set slot.");
            ValidateObjectList(mechanic.ExtraRequirements, report, context, "Power mechanic has a null requirement slot.");
        }
    }

    static void ValidateCompetitions(ProjectValidationReport report) {
        foreach(var competition in ProjectValidatorAssetFinder.FindAssets<CompetitionDefinition>()) {
            if(competition == null) continue;

            string context = $"Competition/{competition.name}";
            if(string.IsNullOrWhiteSpace(competition.Id)) {
                report.Error("Competition id is empty.", context);
            }

            if(competition.Tags != null && competition.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Competition has an empty tag slot.", context);
            }

            if(competition.RequirePlayerUnlock && !competition.UnlockedByDefault) {
                report.Info("Competition requires player unlock. Make sure a source unlocks it.", context);
            }

            if(competition.Format == CompetitionFormat.FrontierStreak && competition.RequiredWinStreakToComplete <= 0) {
                report.Warning("Frontier Streak competition has no required win streak.", context);
            }

            if(competition.Stages == null || competition.Stages.Count == 0) {
                report.Warning("Competition has no stages.", context);
            } else {
                var duplicateStages = competition.Stages
                    .Where(stage => stage != null)
                    .GroupBy(stage => stage.StageId)
                    .Where(group => !string.IsNullOrWhiteSpace(group.Key) && group.Count() > 1)
                    .Select(group => group.Key)
                    .ToList();

                foreach(var duplicateStageId in duplicateStages) {
                    report.Warning($"Competition has duplicate stage id '{duplicateStageId}'.", context);
                }

                foreach(var stage in competition.Stages) {
                    if(stage == null) {
                        report.Warning("Competition has a null stage slot.", context);
                        continue;
                    }

                    string stageContext = $"{context}/Stage/{stage.StageId}";
                    if(stage.Challenges == null || stage.Challenges.Count == 0) {
                        report.Warning("Competition stage has no battle challenges.", stageContext);
                    }

                    if(stage.AdvanceMode == CompetitionStageAdvanceMode.CompleteRequiredWins && stage.RequiredWins <= 0) {
                        report.Warning("Competition stage requires wins but Required Wins is 0.", stageContext);
                    }

                    if(stage.AdvanceMode == CompetitionStageAdvanceMode.ManualOnly) {
                        report.Info("Competition stage uses Manual Only advancement. Progression must be advanced by code or event.", stageContext);
                    }

                    ValidateObjectList(stage.Challenges, report, stageContext, "Competition stage has a null challenge slot.");
                    ValidateObjectList(stage.CompletionMilestones, report, stageContext, "Competition stage has a null completion milestone slot.");
                    ValidateObjectList(stage.CompletionTitleGrants.Select(grant => grant != null ? grant.title : null), report, stageContext, "Competition stage has a null title grant slot.");
                    ValidateLifePathRewards(stage.CompletionLifePathRewards, report, stageContext);
                    ValidateObjectList(stage.BattleRulesToUnlock, report, stageContext, "Competition stage has a null battle rule unlock slot.");
                    ValidateObjectList(stage.HonorsToAward, report, stageContext, "Competition stage has a null honor award slot.");
                }
            }

            ValidateObjectList(competition.EntryRequirements, report, context, "Competition has a null entry requirement slot.");
            ValidateObjectList(competition.CalendarEventsToUnlock, report, context, "Competition has a null calendar event unlock slot.");
            ValidateObjectList(competition.CompletionMilestones, report, context, "Competition has a null completion milestone slot.");
            ValidateObjectList(competition.CompletionTitleGrants.Select(grant => grant != null ? grant.title : null), report, context, "Competition has a null title grant slot.");
            ValidateLifePathRewards(competition.CompletionLifePathRewards, report, context);
            ValidateObjectList(competition.BattleRulesToUnlock, report, context, "Competition has a null battle rule unlock slot.");
            ValidateObjectList(competition.HonorsToAward, report, context, "Competition has a null honor award slot.");
        }
    }

    static void ValidateCompetitionRankings(ProjectValidationReport report) {
        foreach(var ranking in ProjectValidatorAssetFinder.FindAssets<CompetitionRankingDefinition>()) {
            if(ranking == null) continue;

            string context = $"CompetitionRanking/{ranking.name}";
            if(string.IsNullOrWhiteSpace(ranking.Id)) {
                report.Error("Competition ranking id is empty.", context);
            }

            if(ranking.Tags != null && ranking.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Competition ranking has an empty tag slot.", context);
            }

            if(ranking.RequirePlayerUnlock && !ranking.UnlockedByDefault) {
                report.Info("Competition ranking requires player unlock. Make sure a source unlocks it.", context);
            }

            if(ranking.RequireActiveSeason && ranking.SeasonCalendarEvent == null) {
                report.Warning("Competition ranking requires an active season but has no season calendar event.", context);
            }

            if(ranking.PointRules == null || ranking.PointRules.Count == 0) {
                report.Warning("Competition ranking has no point rules.", context);
            } else {
                for(int i = 0; i < ranking.PointRules.Count; i++) {
                    var rule = ranking.PointRules[i];
                    string ruleContext = $"{context}/PointRule/{i}";
                    if(rule == null) {
                        report.Warning("Competition ranking has a null point rule slot.", context);
                        continue;
                    }

                    if(rule.Enabled && rule.FlatPoints == 0 && rule.WinPoints == 0 && rule.LossPoints == 0) {
                        report.Warning("Competition ranking point rule is enabled but gives 0 points.", ruleContext);
                    }

                    if(rule.Competition == null && string.IsNullOrWhiteSpace(rule.CompetitionTag)) {
                        report.Info("Competition ranking point rule applies to every competition.", ruleContext);
                    }
                }
            }

            if(ranking.RankTiers == null || ranking.RankTiers.Count == 0) {
                report.Info("Competition ranking has no rank tiers. It can still track points, but will not grant rank rewards.", context);
            } else {
                var duplicateTiers = ranking.RankTiers
                    .Where(tier => tier != null)
                    .GroupBy(tier => tier.TierId)
                    .Where(group => !string.IsNullOrWhiteSpace(group.Key) && group.Count() > 1)
                    .Select(group => group.Key)
                    .ToList();

                foreach(var duplicateTierId in duplicateTiers) {
                    report.Warning($"Competition ranking has duplicate tier id '{duplicateTierId}'.", context);
                }

                foreach(var tier in ranking.RankTiers) {
                    if(tier == null) {
                        report.Warning("Competition ranking has a null tier slot.", context);
                        continue;
                    }

                    string tierContext = $"{context}/Tier/{tier.TierId}";
                    ValidateObjectList(tier.TitleGrants.Select(grant => grant != null ? grant.title : null), report, tierContext, "Competition ranking tier has a null title grant slot.");
                    ValidateObjectList(tier.MilestonesToComplete, report, tierContext, "Competition ranking tier has a null milestone slot.");
                    ValidateLifePathRewards(tier.LifePathRewards, report, tierContext);
                    ValidateObjectList(tier.CompetitionsToUnlock, report, tierContext, "Competition ranking tier has a null competition unlock slot.");
                    ValidateObjectList(tier.BattleRulesToUnlock, report, tierContext, "Competition ranking tier has a null battle rule unlock slot.");
                    ValidateObjectList(tier.PowerMechanicsToUnlock, report, tierContext, "Competition ranking tier has a null power mechanic unlock slot.");
                    ValidateObjectList(tier.HonorsToAward, report, tierContext, "Competition ranking tier has a null honor award slot.");
                }
            }

            ValidateObjectList(ranking.Requirements, report, context, "Competition ranking has a null requirement slot.");
        }
    }

    static void ValidateCompetitionHonors(ProjectValidationReport report) {
        foreach(var honor in ProjectValidatorAssetFinder.FindAssets<CompetitionHonorDefinition>()) {
            if(honor == null) continue;

            string context = $"CompetitionHonor/{honor.name}";
            if(string.IsNullOrWhiteSpace(honor.Id)) {
                report.Error("Competition honor id is empty.", context);
            }

            if(honor.Tags != null && honor.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Competition honor has an empty tag slot.", context);
            }

            if(honor.RequiredBestStreak > 0 && honor.RequiredCompletedCompetition == null) {
                report.Warning("Competition honor requires a best streak but has no required competition.", context);
            }

            if(!string.IsNullOrWhiteSpace(honor.RequiredRankingTierId) && honor.RequiredRanking == null) {
                report.Warning("Competition honor requires a ranking tier id but has no required ranking.", context);
            }

            if(!honor.Unique) {
                report.Info("Competition honor is repeatable and may create multiple history records.", context);
            }

            ValidateObjectList(honor.Requirements, report, context, "Competition honor has a null requirement slot.");
            ValidateObjectList(honor.TitleGrants.Select(grant => grant != null ? grant.title : null), report, context, "Competition honor has a null title grant slot.");
            ValidateObjectList(honor.MilestonesToComplete, report, context, "Competition honor has a null milestone slot.");
            ValidateLifePathRewards(honor.LifePathRewards, report, context);
            ValidateObjectList(honor.CompetitionsToUnlock, report, context, "Competition honor has a null competition unlock slot.");
            ValidateObjectList(honor.RankingsToUnlock, report, context, "Competition honor has a null ranking unlock slot.");
            ValidateObjectList(honor.BattleRulesToUnlock, report, context, "Competition honor has a null battle rule unlock slot.");
            ValidateObjectList(honor.PowerMechanicsToUnlock, report, context, "Competition honor has a null power mechanic unlock slot.");
            ValidateObjectList(honor.CalendarEventsToUnlock, report, context, "Competition honor has a null calendar event unlock slot.");
        }
    }

    static void ValidateCompetitionSeasons(ProjectValidationReport report) {
        foreach(var season in ProjectValidatorAssetFinder.FindAssets<CompetitionSeasonDefinition>()) {
            if(season == null) continue;

            string context = $"CompetitionSeason/{season.name}";
            if(string.IsNullOrWhiteSpace(season.Id)) {
                report.Error("Competition season id is empty.", context);
            }

            if(season.Tags != null && season.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Competition season has an empty tag slot.", context);
            }

            if(season.RequirePlayerUnlock && !season.UnlockedByDefault) {
                report.Info("Competition season requires player unlock. Make sure a source unlocks it.", context);
            }

            if(season.RequireActiveCalendarEvent && season.CalendarEvent == null) {
                report.Warning("Competition season requires an active calendar event but no calendar event is assigned.", context);
            }

            if(season.CompetitionsToUnlockOnStart.Count == 0
                && season.RankingsToUnlockOnStart.Count == 0
                && season.BattleRulesToUnlockOnStart.Count == 0
                && season.LifePathRewardsOnStart.Count == 0
                && season.CompetitionsToResetOnStart.Count == 0
                && season.RankingsToResetOnStart.Count == 0) {
                report.Info("Competition season has no start effects. It can still be used as a requirement or calendar marker.", context);
            }

            ValidateObjectList(season.StartRequirements, report, context, "Competition season has a null start requirement slot.");
            ValidateObjectList(season.CompletionRequirements, report, context, "Competition season has a null completion requirement slot.");
            ValidateObjectList(season.CompetitionsToUnlockOnStart, report, context, "Competition season has a null competition start unlock slot.");
            ValidateObjectList(season.RankingsToUnlockOnStart, report, context, "Competition season has a null ranking start unlock slot.");
            ValidateObjectList(season.BattleRulesToUnlockOnStart, report, context, "Competition season has a null battle rule start unlock slot.");
            ValidateLifePathRewards(season.LifePathRewardsOnStart, report, context);
            ValidateObjectList(season.CalendarEventsToUnlockOnStart, report, context, "Competition season has a null calendar start unlock slot.");
            ValidateObjectList(season.CompetitionsToResetOnStart, report, context, "Competition season has a null competition reset slot.");
            ValidateObjectList(season.RankingsToResetOnStart, report, context, "Competition season has a null ranking reset slot.");
            ValidateObjectList(season.MilestonesToComplete, report, context, "Competition season has a null completion milestone slot.");
            ValidateObjectList(season.TitleGrants.Select(grant => grant != null ? grant.title : null), report, context, "Competition season has a null title grant slot.");
            ValidateLifePathRewards(season.LifePathRewardsOnCompletion, report, context);
            ValidateObjectList(season.HonorsToAward, report, context, "Competition season has a null honor award slot.");
            ValidateObjectList(season.CompetitionsToUnlockOnCompletion, report, context, "Competition season has a null competition completion unlock slot.");
            ValidateObjectList(season.RankingsToUnlockOnCompletion, report, context, "Competition season has a null ranking completion unlock slot.");
            ValidateObjectList(season.PowerMechanicsToUnlockOnCompletion, report, context, "Competition season has a null power mechanic completion unlock slot.");
        }
    }

    static void ValidateCompetitionEntrants(ProjectValidationReport report) {
        foreach(var entrant in ProjectValidatorAssetFinder.FindAssets<CompetitionEntrantDefinition>()) {
            if(entrant == null) continue;

            string context = $"CompetitionEntrant/{entrant.name}";
            if(string.IsNullOrWhiteSpace(entrant.Id)) {
                report.Error("Competition entrant id is empty.", context);
            }

            if(entrant.Tags != null && entrant.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Competition entrant has an empty tag slot.", context);
            }

            if(entrant.Selectable && entrant.Challenge == null && entrant.PartyTemplate == null) {
                report.Warning("Selectable entrant has no battle challenge or party template.", context);
            }

            if(entrant.SelectionWeight <= 0 && entrant.Selectable) {
                report.Info("Selectable entrant has 0 selection weight. It will not be chosen by weighted random rosters.", context);
            }

            ValidateObjectList(entrant.Requirements, report, context, "Competition entrant has a null requirement slot.");
        }
    }

    static void ValidateCompetitionRosters(ProjectValidationReport report) {
        foreach(var roster in ProjectValidatorAssetFinder.FindAssets<CompetitionRosterDefinition>()) {
            if(roster == null) continue;

            string context = $"CompetitionRoster/{roster.name}";
            if(string.IsNullOrWhiteSpace(roster.Id)) {
                report.Error("Competition roster id is empty.", context);
            }

            if(roster.Tags != null && roster.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Competition roster has an empty tag slot.", context);
            }

            if(roster.Competition == null) {
                report.Info("Competition roster has no linked competition. It can still be used as a standalone bracket.", context);
            }

            if(roster.MaxOpponentCount > 0 && roster.MinOpponentCount > roster.MaxOpponentCount) {
                report.Warning("Competition roster minimum opponent count is higher than maximum opponent count.", context);
            }

            var entrants = roster.Entrants?.Where(entrant => entrant != null).ToList() ?? new List<CompetitionEntrantDefinition>();
            if(entrants.Count == 0) {
                report.Warning("Competition roster has no entrant candidates.", context);
            }

            if(roster.MinOpponentCount > entrants.Count && !roster.AllowDuplicateEntrants) {
                report.Warning("Competition roster requires more opponents than it has entrant candidates.", context);
            }

            if(!roster.AllowDuplicateEntrants) {
                var duplicateEntrants = entrants
                    .GroupBy(entrant => entrant.Id)
                    .Where(group => !string.IsNullOrWhiteSpace(group.Key) && group.Count() > 1)
                    .Select(group => group.Key)
                    .ToList();

                foreach(var duplicateEntrantId in duplicateEntrants) {
                    report.Warning($"Competition roster has duplicate entrant '{duplicateEntrantId}' while duplicates are disabled.", context);
                }
            }

            if(roster.SelectionMode == CompetitionRosterSelectionMode.WeightedRandom && entrants.Count > 0 && entrants.All(entrant => entrant.SelectionWeight <= 0)) {
                report.Warning("Weighted random roster has no entrant with positive selection weight.", context);
            }

            if(!roster.IncludePlayer) {
                report.Info("Competition roster does not include the player. It may be useful for simulation, but player match recording expects a player entrant.", context);
            }

            ValidateObjectList(roster.Entrants, report, context, "Competition roster has a null entrant slot.");
            ValidateObjectList(roster.Requirements, report, context, "Competition roster has a null requirement slot.");
        }
    }

    static void ValidateCompetitionPrizeTables(ProjectValidationReport report) {
        foreach(var prize in ProjectValidatorAssetFinder.FindAssets<CompetitionPrizeTableDefinition>()) {
            if(prize == null) continue;

            string context = $"CompetitionPrize/{prize.name}";
            if(string.IsNullOrWhiteSpace(prize.Id)) {
                report.Error("Competition prize id is empty.", context);
            }

            if(prize.Tags != null && prize.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Competition prize has an empty tag slot.", context);
            }

            if(prize.Triggers == null || prize.Triggers.Count == 0) {
                report.Info("Competition prize has no trigger filters, so it can match every prize trigger.", context);
            }

            if(prize.RepeatMode == CompetitionPrizeRepeatMode.CooldownHours && prize.CooldownHours <= 0) {
                report.Warning("Competition prize uses Cooldown Hours repeat mode but cooldown is 0.", context);
            }

            if(!CompetitionPrizeHasReward(prize)) {
                report.Warning("Competition prize has no rewards or unlocks.", context);
            }

            foreach(var reward in prize.ItemRewards) {
                if(reward == null) {
                    report.Warning("Competition prize has a null item reward slot.", context);
                    continue;
                }

                if(reward.Item == null && reward.MaxCount > 0) {
                    report.Warning("Competition prize item reward has count but no item.", context);
                }
            }

            ValidateObjectList(prize.Requirements, report, context, "Competition prize has a null requirement slot.");
            ValidateReputationChanges(prize.ReputationChanges, report, context);
            ValidateRelationshipChanges(prize.RelationshipChanges, report, context);
            ValidateCareerPointGrants(prize.CareerPointRewards, report, context);
            ValidateLifePathRewards(prize.LifePathRewards, report, context);
            ValidateOrganizationMembershipGrants(prize.OrganizationMembershipRewards, report, context);
            ValidateOrganizationPointGrants(prize.OrganizationPointRewards, report, context);
            ValidateObjectList(prize.MilestonesToComplete, report, context, "Competition prize has a null milestone slot.");
            ValidateTitleGrants(prize.TitleGrants, report, context);
            ValidateObjectList(prize.RecipeRewards.Select(grant => grant != null ? grant.recipe : null), report, context, "Competition prize has a null recipe grant slot.");
            ValidateObjectList(prize.CompetitionsToUnlock, report, context, "Competition prize has a null competition unlock slot.");
            ValidateObjectList(prize.RankingsToUnlock, report, context, "Competition prize has a null ranking unlock slot.");
            ValidateObjectList(prize.BattleRulesToUnlock, report, context, "Competition prize has a null battle rule unlock slot.");
            ValidateObjectList(prize.PowerMechanicsToUnlock, report, context, "Competition prize has a null power mechanic unlock slot.");
            ValidateObjectList(prize.HonorsToAward, report, context, "Competition prize has a null honor award slot.");
            ValidateObjectList(prize.CalendarEventsToUnlock, report, context, "Competition prize has a null calendar event unlock slot.");
            ValidateObjectList(prize.InvitationsToGrant, report, context, "Competition prize has a null invitation grant slot.");
            ValidateObjectList(prize.SponsorsToGrant, report, context, "Competition prize has a null sponsor grant slot.");
        }
    }

    static bool CompetitionPrizeHasReward(CompetitionPrizeTableDefinition prize) {
        if(prize == null) {
            return false;
        }

        return prize.MoneyReward > 0
            || prize.TrainerExperience > 0
            || prize.ItemRewards.Any(reward => reward != null && reward.Item != null && reward.MaxCount > 0)
            || prize.ReputationChanges.Any(change => change != null && change.faction != null && change.amount != 0)
            || prize.RelationshipChanges.Any(change => change != null && change.subject != null && change.amount != 0)
            || prize.CareerPointRewards.Any(grant => grant != null && grant.career != null && grant.points > 0)
            || prize.LifePathRewards.Any(reward => reward != null && reward.lifePath != null && reward.HasAnyPayload)
            || prize.OrganizationMembershipRewards.Any(grant => grant != null && grant.organization != null)
            || prize.OrganizationPointRewards.Any(grant => grant != null && grant.organization != null && grant.points > 0)
            || prize.MilestonesToComplete.Any(entry => entry != null)
            || prize.TitleGrants.Any(grant => grant != null && grant.title != null)
            || prize.RecipeRewards.Any(grant => grant != null && grant.recipe != null)
            || prize.CompetitionsToUnlock.Any(entry => entry != null)
            || prize.RankingsToUnlock.Any(entry => entry != null)
            || prize.BattleRulesToUnlock.Any(entry => entry != null)
            || prize.PowerMechanicsToUnlock.Any(entry => entry != null)
            || prize.HonorsToAward.Any(entry => entry != null)
            || prize.CalendarEventsToUnlock.Any(entry => entry != null)
            || prize.InvitationsToGrant.Any(entry => entry != null)
            || prize.SponsorsToGrant.Any(entry => entry != null);
    }

    static void ValidateCompetitionVenues(ProjectValidationReport report) {
        foreach(var venue in ProjectValidatorAssetFinder.FindAssets<CompetitionVenueDefinition>()) {
            if(venue == null) continue;

            string context = $"CompetitionVenue/{venue.name}";
            if(string.IsNullOrWhiteSpace(venue.Id)) {
                report.Error("Competition venue id is empty.", context);
            }

            if(venue.Tags != null && venue.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Competition venue has an empty tag slot.", context);
            }

            if(venue.RequiredRegistrationTags != null && venue.RequiredRegistrationTags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Competition venue has an empty required registration tag slot.", context);
            }

            if(venue.RequiredRosterTags != null && venue.RequiredRosterTags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Competition venue has an empty required roster tag slot.", context);
            }

            if(venue.RequiredCompetitionTags != null && venue.RequiredCompetitionTags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Competition venue has an empty required competition tag slot.", context);
            }

            if(venue.RepeatMode == ConsequenceChainRepeatMode.CooldownHours && venue.CooldownHours <= 0) {
                report.Warning("Competition venue uses Cooldown Hours repeat mode but cooldown is 0.", context);
            }

            if(venue.EnterAreaProfileOnUse && venue.AreaProfile == null) {
                report.Warning("Competition venue enters an area profile on use but has no area profile assigned.", context);
            }

            if(venue.RequireRuleSetAccess && venue.RuleSetOverride == null) {
                report.Info("Competition venue requires rule access but has no rule set override.", context);
            }

            if(venue.AllowedRegistrations.Count == 0
                && venue.AllowedRosters.Count == 0
                && venue.AllowedCompetitions.Count == 0
                && venue.RequiredRegistrationTags.Count == 0
                && venue.RequiredRosterTags.Count == 0
                && venue.RequiredCompetitionTags.Count == 0) {
                report.Info("Competition venue has no hosting filters, so it can host any matching content when requirements pass.", context);
            }

            ValidateObjectList(venue.AllowedRegistrations, report, context, "Competition venue has a null allowed registration slot.");
            ValidateObjectList(venue.AllowedRosters, report, context, "Competition venue has a null allowed roster slot.");
            ValidateObjectList(venue.AllowedCompetitions, report, context, "Competition venue has a null allowed competition slot.");
            ValidateObjectList(venue.AllowedSeasons, report, context, "Competition venue has a null allowed season slot.");
            ValidateObjectList(venue.AllowedRankings, report, context, "Competition venue has a null allowed ranking slot.");
            ValidateObjectList(venue.Requirements, report, context, "Competition venue has a null requirement slot.");
        }
    }

    static void ValidateCompetitionInvitations(ProjectValidationReport report) {
        foreach(var invitation in ProjectValidatorAssetFinder.FindAssets<CompetitionInvitationDefinition>()) {
            if(invitation == null) continue;

            string context = $"CompetitionInvitation/{invitation.name}";
            if(string.IsNullOrWhiteSpace(invitation.Id)) {
                report.Error("Competition invitation id is empty.", context);
            }

            if(invitation.Tags != null && invitation.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Competition invitation has an empty tag slot.", context);
            }

            if(invitation.RequiredRegistrationTags != null && invitation.RequiredRegistrationTags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Competition invitation has an empty required registration tag slot.", context);
            }

            if(invitation.RequiredWindowTags != null && invitation.RequiredWindowTags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Competition invitation has an empty required window tag slot.", context);
            }

            if(invitation.Expires && invitation.DefaultDurationHours <= 0) {
                report.Warning("Competition invitation expires but has no duration.", context);
            }

            if(invitation.GrantMode == CompetitionInvitationGrantMode.RefreshExistingOnly) {
                report.Info("Competition invitation can only refresh an already owned invitation.", context);
            }

            ValidateObjectList(invitation.GrantRequirements, report, context, "Competition invitation has a null grant requirement slot.");
        }
    }

    static void ValidateCompetitionRegistrations(ProjectValidationReport report) {
        foreach(var registration in ProjectValidatorAssetFinder.FindAssets<CompetitionRegistrationDefinition>()) {
            if(registration == null) continue;

            string context = $"CompetitionRegistration/{registration.name}";
            if(string.IsNullOrWhiteSpace(registration.Id)) {
                report.Error("Competition registration id is empty.", context);
            }

            if(registration.Tags != null && registration.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Competition registration has an empty tag slot.", context);
            }

            if(registration.Roster == null) {
                report.Warning("Competition registration has no roster assigned.", context);
            }

            if(registration.RepeatMode == CompetitionRegistrationRepeatMode.CooldownHours && registration.CooldownHours <= 0) {
                report.Warning("Competition registration uses Cooldown Hours repeat mode but cooldown is 0.", context);
            }

            if(registration.RepeatMode == CompetitionRegistrationRepeatMode.OncePerWindow && registration.WindowMode == CompetitionRegistrationWindowMode.AlwaysOpen) {
                report.Warning("Competition registration uses Once Per Window repeat mode but Window Mode is Always Open.", context);
            }

            if(registration.WindowMode != CompetitionRegistrationWindowMode.AlwaysOpen
                && (registration.RegistrationWindows == null || registration.RegistrationWindows.Count == 0)) {
                report.Warning("Competition registration requires windows but has no registration windows assigned.", context);
            }

            if(registration.WindowMode == CompetitionRegistrationWindowMode.AlwaysOpen
                && registration.RegistrationWindows != null
                && registration.RegistrationWindows.Count > 0) {
                report.Info("Competition registration has window assets, but Window Mode is Always Open so they are ignored.", context);
            }

            if(registration.GenerateBracketOnRegister && registration.Roster == null) {
                report.Warning("Competition registration is set to generate a bracket but has no roster.", context);
            }

            if(registration.InvitationMode == CompetitionRegistrationInvitationMode.AnyListedInvitation
                && (registration.RequiredInvitations == null || registration.RequiredInvitations.Count == 0)) {
                report.Warning("Competition registration requires a listed invitation but has no required invitations.", context);
            }

            if(registration.InvitationMode == CompetitionRegistrationInvitationMode.NotRequired
                && registration.RequiredInvitations != null
                && registration.RequiredInvitations.Count > 0) {
                report.Info("Competition registration has required invitations, but Invitation Mode is Not Required so they are ignored.", context);
            }

            if(registration.VenueMode == CompetitionRegistrationVenueMode.AnyListedVenue
                && (registration.RequiredVenues == null || registration.RequiredVenues.Count == 0)) {
                report.Warning("Competition registration requires a listed venue but has no required venues.", context);
            }

            if(registration.VenueMode == CompetitionRegistrationVenueMode.NotRequired
                && registration.RequiredVenues != null
                && registration.RequiredVenues.Count > 0) {
                report.Info("Competition registration has required venues, but Venue Mode is Not Required so they are ignored.", context);
            }

            foreach(var cost in registration.ItemCosts) {
                if(cost == null) {
                    report.Warning("Competition registration has a null item cost slot.", context);
                    continue;
                }

                if(cost.item == null && cost.count > 0) {
                    report.Warning("Competition registration item cost has count but no item.", context);
                }
            }

            ValidateObjectList(registration.Requirements, report, context, "Competition registration has a null requirement slot.");
            ValidateObjectList(registration.RegistrationWindows, report, context, "Competition registration has a null registration window slot.");
            ValidateObjectList(registration.RequiredInvitations, report, context, "Competition registration has a null required invitation slot.");
            ValidateObjectList(registration.RequiredVenues, report, context, "Competition registration has a null required venue slot.");
        }
    }

    static void ValidateCompetitionRegistrationWindows(ProjectValidationReport report) {
        foreach(var window in ProjectValidatorAssetFinder.FindAssets<CompetitionRegistrationWindowDefinition>()) {
            if(window == null) continue;

            string context = $"CompetitionRegistrationWindow/{window.name}";
            if(string.IsNullOrWhiteSpace(window.Id)) {
                report.Error("Competition registration window id is empty.", context);
            }

            if(window.Tags != null && window.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Competition registration window has an empty tag slot.", context);
            }

            if(window.CalendarMode != CompetitionRegistrationWindowCalendarMode.Ignore && window.CalendarEvent == null) {
                report.Warning("Competition registration window uses a calendar gate but has no calendar event.", context);
            }

            if(!window.UseManualSchedule && window.CalendarMode == CompetitionRegistrationWindowCalendarMode.Ignore) {
                report.Info("Competition registration window has no manual schedule and no calendar gate, so it is always open when requirements pass.", context);
            }

            if(window.RepeatMode == CalendarRepeatMode.SpecificDays && (window.SpecificDays == null || window.SpecificDays.Count == 0)) {
                report.Warning("Competition registration window uses Specific Days but has no days assigned.", context);
            }

            if(window.RequiredRegistrationTags != null && window.RequiredRegistrationTags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Competition registration window has an empty required registration tag slot.", context);
            }

            ValidateObjectList(window.Requirements, report, context, "Competition registration window has a null requirement slot.");
        }
    }

    static void ValidateCompetitionMatchResolvers(ProjectValidationReport report) {
        foreach(var resolver in ProjectValidatorAssetFinder.FindAssets<CompetitionMatchResolverDefinition>()) {
            if(resolver == null) continue;

            string context = $"CompetitionMatchResolver/{resolver.name}";
            if(string.IsNullOrWhiteSpace(resolver.Id)) {
                report.Error("Competition match resolver id is empty.", context);
            }

            if(resolver.Tags != null && resolver.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Competition match resolver has an empty tag slot.", context);
            }

            if(resolver.KindPowerRules == null || resolver.KindPowerRules.Count == 0) {
                report.Info("Competition match resolver has no kind power rules. Default power will be used for every entrant kind.", context);
            } else {
                var duplicateKinds = resolver.KindPowerRules
                    .Where(rule => rule != null)
                    .GroupBy(rule => rule.Kind)
                    .Where(group => group.Count() > 1)
                    .Select(group => group.Key)
                    .ToList();

                foreach(var duplicateKind in duplicateKinds) {
                    report.Warning($"Competition match resolver has duplicate power rules for kind '{duplicateKind}'.", context);
                }
            }

            foreach(var rule in resolver.TagPowerRules) {
                if(rule == null) {
                    report.Warning("Competition match resolver has a null tag power rule slot.", context);
                    continue;
                }

                if(string.IsNullOrWhiteSpace(rule.Tag) && rule.PowerModifier != 0) {
                    report.Warning("Competition match resolver tag power rule has a modifier but no tag.", context);
                }
            }
        }
    }

    static void ValidateCompetitionBracketSources(ProjectValidationReport report) {
        foreach(var source in ProjectValidatorAssetFinder.FindAssets<CompetitionBracketSource>()) {
            if(source == null) continue;

            string context = $"CompetitionBracketSource/{source.name}";
            if(source.Roster == null) {
                report.Warning("Competition bracket source has no roster assigned.", context);
            }

            if(source.Venue != null && source.Roster != null && !source.Venue.MatchesRoster(source.Roster, out var venueFailure)) {
                report.Warning($"Competition bracket source venue may not host this roster: {venueFailure}", context);
            }

            ValidateObjectList(source.PrizeTables, report, context, "Competition bracket source has a null prize table slot.");
        }
    }

    static void ValidateCompetitionRegistrationSources(ProjectValidationReport report) {
        foreach(var source in ProjectValidatorAssetFinder.FindAssets<CompetitionRegistrationSource>()) {
            if(source == null) continue;

            string context = $"CompetitionRegistrationSource/{source.name}";
            if(source.Registration == null) {
                report.Warning("Competition registration source has no registration assigned.", context);
            }
        }
    }

    static void ValidateCompetitionInvitationSources(ProjectValidationReport report) {
        foreach(var source in ProjectValidatorAssetFinder.FindAssets<CompetitionInvitationSource>()) {
            if(source == null) continue;

            string context = $"CompetitionInvitationSource/{source.name}";
            if(source.Invitation == null) {
                report.Warning("Competition invitation source has no invitation assigned.", context);
            }
        }
    }

    static void ValidateCompetitionVenueSources(ProjectValidationReport report) {
        foreach(var source in ProjectValidatorAssetFinder.FindAssets<CompetitionVenueSource>()) {
            if(source == null) continue;

            string context = $"CompetitionVenueSource/{source.name}";
            if(source.Venue == null) {
                report.Warning("Competition venue source has no venue assigned.", context);
            }

            if(source.Venue != null && source.RegistrationSource != null && source.RegistrationSource.Registration != null
                && !source.Venue.MatchesRegistration(source.RegistrationSource.Registration, out var registrationFailure)) {
                report.Warning($"Competition venue source may not host linked registration: {registrationFailure}", context);
            }

            if(source.Venue != null && source.BracketSource != null && source.BracketSource.Roster != null
                && !source.Venue.MatchesRoster(source.BracketSource.Roster, out var rosterFailure)) {
                report.Warning($"Competition venue source may not host linked bracket roster: {rosterFailure}", context);
            }
        }
    }

    static void ValidateSponsorSources(ProjectValidationReport report) {
        foreach(var source in ProjectValidatorAssetFinder.FindAssets<SponsorSource>()) {
            if(source == null) continue;

            string context = $"SponsorSource/{source.name}";
            if(source.Sponsor == null) {
                report.Warning("Sponsor source has no sponsor assigned.", context);
            }
        }
    }

    static void ValidateShopBasketSources(ProjectValidationReport report) {
        foreach(var source in ProjectValidatorAssetFinder.FindAssets<ShopBasketSource>()) {
            if(source == null) continue;

            string context = $"ShopBasketSource/{source.name}";
            if(source.Shop == null) {
                report.Warning("Shop basket source has no shop assigned.", context);
            }

            if(source.Action == ShopBasketSourceAction.CheckoutBasket && source.CheckoutPaymentRule == null) {
                report.Info("Shop basket checkout uses default money payment because no payment rule is assigned.", context);
            }

            bool needsPresetOffers = source.Action == ShopBasketSourceAction.AddPresetOffers
                || source.Action == ShopBasketSourceAction.BeginAndAddPresetOffers;
            if(needsPresetOffers && (source.PresetOffers == null || source.PresetOffers.Count == 0)) {
                report.Warning("Shop basket source uses preset offer action but has no preset offers.", context);
            }

            if(source.PresetOffers == null) {
                continue;
            }

            foreach(var preset in source.PresetOffers) {
                if(preset == null) {
                    report.Warning("Shop basket source has a null preset offer slot.", context);
                    continue;
                }

                if(string.IsNullOrWhiteSpace(preset.offerId)) {
                    report.Warning("Shop basket preset has an empty offer id.", context);
                    continue;
                }

                if(source.Shop != null && source.Shop.Catalog != null && source.Shop.FindOffer(preset.offerId) == null) {
                    report.Warning($"Shop basket preset references offer '{preset.offerId}' that is not in the assigned shop catalog.", context);
                }
            }
        }
    }

    static void ValidateShopCheckoutTerminals(ProjectValidationReport report) {
        foreach(var terminal in ProjectValidatorAssetFinder.FindAssets<ShopCheckoutTerminal>()) {
            if(terminal == null) continue;

            string context = $"ShopCheckoutTerminal/{terminal.name}";
            if(terminal.Shop == null) {
                report.Warning("Checkout terminal has no shop assigned.", context);
            }

            if(terminal.PaymentRule == null) {
                report.Info("Checkout terminal has no payment rule. It will use default money checkout with no extra terminal restrictions.", context);
            }

            if(terminal.TriggerAction == ShopCheckoutTerminalAction.PreviewQuote && terminal.PaymentRule == null) {
                report.Info("Preview quote action without a payment rule only previews the default basket total.", context);
            }
        }
    }

    static void ValidateShopRefundSources(ProjectValidationReport report) {
        foreach(var source in ProjectValidatorAssetFinder.FindAssets<ShopRefundSource>()) {
            if(source == null) continue;

            string context = $"ShopRefundSource/{source.name}";
            if(source.Shop == null) {
                report.Warning("Shop refund source has no shop assigned.", context);
            }

            if(source.ReturnPolicy == null) {
                report.Warning("Shop refund source has no return policy assigned.", context);
            }

            if(source.TriggerAction == ShopRefundSourceAction.PreviewRefund && source.ReturnPolicy == null) {
                report.Warning("Refund preview action requires a return policy.", context);
            }
        }
    }

    static void ValidateShopSecuritySources(ProjectValidationReport report) {
        foreach(var source in ProjectValidatorAssetFinder.FindAssets<ShopSecuritySource>()) {
            if(source == null) continue;

            string context = $"ShopSecuritySource/{source.name}";
            if(source.SecurityPolicy == null) {
                report.Warning("Shop security source has no policy assigned.", context);
            }

            if(source.Shop == null) {
                report.Warning("Shop security source has no shop assigned. Only policies that do not require a matching shop or catalog filters can evaluate without one.", context);
            }

            if(source.TriggerAction == ShopSecuritySourceAction.PreviewEvaluation && source.SecurityPolicy == null) {
                report.Warning("Security preview action requires a security policy.", context);
            }
        }
    }

    static void ValidateShopRestockSources(ProjectValidationReport report) {
        foreach(var source in ProjectValidatorAssetFinder.FindAssets<ShopRestockSource>()) {
            if(source == null) continue;

            string context = $"ShopRestockSource/{source.name}";
            if(source.Shop == null) {
                report.Warning("Shop restock source has no shop assigned.", context);
            }

            if(source.Schedules == null || source.Schedules.Count == 0) {
                report.Warning("Shop restock source has no schedules assigned.", context);
            } else {
                foreach(var schedule in source.Schedules) {
                    if(schedule == null) {
                        report.Warning("Shop restock source has a null schedule slot.", context);
                    }
                }
            }

            if(source.TriggerAction == ShopRestockSourceAction.ForceRunAll) {
                report.Info("Shop restock source trigger force-runs all schedules, ignoring normal timing and once-per-day checks.", context);
            }
        }
    }

    static void ValidateShopDeliverySources(ProjectValidationReport report) {
        foreach(var source in ProjectValidatorAssetFinder.FindAssets<ShopDeliverySource>()) {
            if(source == null) continue;

            string context = $"ShopDeliverySource/{source.name}";
            if(source.Shop == null) {
                report.Warning("Shop delivery source has no shop assigned.", context);
            }

            if(source.DeliveryService == null) {
                report.Warning("Shop delivery source has no delivery service assigned.", context);
            }

            if(source.TriggerAction == ShopDeliverySourceAction.ClaimDueDeliveries && source.DeliveryService != null && source.DeliveryService.FulfillmentMode == ShopDeliveryFulfillmentMode.AutoToInventoryWhenDue) {
                report.Info("Delivery source trigger claims due deliveries, but the assigned service auto-delivers to inventory. Claim action can still claim other matching claim-at-destination orders in the player's log.", context);
            }

            if(source.DeliveryService != null && source.DeliveryService.RequireDestinationId && source.DestinationMarker == null && source.DestinationRegion == null) {
                report.Info("Delivery service requires a destination id. Source can still provide one through its Destination Id text field.", context);
            }

            if(source.DestinationTags != null && source.DestinationTags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Shop delivery source has an empty destination tag slot.", context);
            }
        }
    }

    static void ValidateShopShelfSources(ProjectValidationReport report) {
        foreach(var source in ProjectValidatorAssetFinder.FindAssets<ShopShelfSource>()) {
            if(source == null) continue;

            string context = $"ShopShelfSource/{source.name}";
            if(source.Shop == null) {
                report.Warning("Shop shelf source has no shop assigned.", context);
            }

            if(source.Shelf == null) {
                report.Warning("Shop shelf source has no shelf definition assigned.", context);
            }

            bool needsVisibleOffers = source.StartAction == ShopShelfSourceAction.AddDefaultOffer
                || source.StartAction == ShopShelfSourceAction.AddFirstVisibleOffer
                || source.StartAction == ShopShelfSourceAction.AddAllVisibleOffers
                || source.TriggerAction == ShopShelfSourceAction.AddDefaultOffer
                || source.TriggerAction == ShopShelfSourceAction.AddFirstVisibleOffer
                || source.TriggerAction == ShopShelfSourceAction.AddAllVisibleOffers
                || source.InteractAction == ShopShelfSourceAction.AddDefaultOffer
                || source.InteractAction == ShopShelfSourceAction.AddFirstVisibleOffer
                || source.InteractAction == ShopShelfSourceAction.AddAllVisibleOffers;

            if(needsVisibleOffers && source.Shelf == null) {
                report.Warning("Shop shelf source has an add action but no shelf definition.", context);
            }

            if(needsVisibleOffers && source.Shop == null) {
                report.Warning("Shop shelf source has an add action but no shop.", context);
            }
        }
    }

    static void ValidateLearnableOfferSources(ProjectValidationReport report) {
        foreach(var source in ProjectValidatorAssetFinder.FindAssets<LearnableOfferSource>()) {
            if(source == null) continue;

            string context = $"LearnableOfferSource/{source.name}";
            if(source.Offers == null || source.Offers.Count == 0) {
                report.Warning("Learnable offer source has no offers assigned.", context);
            } else {
                ValidateObjectList(source.Offers, report, context, "Learnable offer source has a null offer slot.");
            }

            if(source.TriggerAction != LearnableOfferSourceAction.None && (source.Offers == null || source.Offers.Count == 0)) {
                report.Warning("Learnable offer source has a trigger purchase action but no offers.", context);
            }
        }
    }

    static void ValidateLoyaltyProgramSources(ProjectValidationReport report) {
        foreach(var source in ProjectValidatorAssetFinder.FindAssets<LoyaltyProgramSource>()) {
            if(source == null) continue;

            string context = $"LoyaltyProgramSource/{source.name}";
            if(source.Programs == null || source.Programs.Count == 0) {
                report.Warning("Loyalty program source has no programs assigned.", context);
            } else {
                ValidateObjectList(source.Programs, report, context, "Loyalty program source has a null program slot.");
            }

            if(source.ManualPoints <= 0 && (source.StartAction == LoyaltyProgramSourceAction.GrantManualPoints
                || source.TriggerAction == LoyaltyProgramSourceAction.GrantManualPoints
                || source.InteractAction == LoyaltyProgramSourceAction.GrantManualPoints)) {
                report.Warning("Loyalty program source grants manual points but Manual Points is 0.", context);
            }

            if(source.StartAction != LoyaltyProgramSourceAction.None && (source.Programs == null || source.Programs.Count == 0)) {
                report.Warning("Loyalty program source has a start action but no programs.", context);
            }

            if(source.TriggerAction != LoyaltyProgramSourceAction.None && (source.Programs == null || source.Programs.Count == 0)) {
                report.Warning("Loyalty program source has a trigger action but no programs.", context);
            }

            if(source.InteractAction != LoyaltyProgramSourceAction.None && (source.Programs == null || source.Programs.Count == 0)) {
                report.Warning("Loyalty program source has an interact action but no programs.", context);
            }
        }
    }

    static void ValidateServicePackageSources(ProjectValidationReport report) {
        foreach(var source in ProjectValidatorAssetFinder.FindAssets<ServicePackageSource>()) {
            if(source == null) continue;

            string context = $"ServicePackageSource/{source.name}";
            if(source.Packages == null || source.Packages.Count == 0) {
                report.Warning("Service package source has no packages assigned.", context);
            } else {
                ValidateObjectList(source.Packages, report, context, "Service package source has a null package slot.");
            }

            if(source.StartAction != ServicePackageSourceAction.None && (source.Packages == null || source.Packages.Count == 0)) {
                report.Warning("Service package source has a start action but no packages.", context);
            }

            if(source.TriggerAction != ServicePackageSourceAction.None && (source.Packages == null || source.Packages.Count == 0)) {
                report.Warning("Service package source has a trigger action but no packages.", context);
            }

            if(source.InteractAction != ServicePackageSourceAction.None && (source.Packages == null || source.Packages.Count == 0)) {
                report.Warning("Service package source has an interact action but no packages.", context);
            }
        }
    }

    static void ValidateServiceAppointmentSources(ProjectValidationReport report) {
        foreach(var source in ProjectValidatorAssetFinder.FindAssets<ServiceAppointmentSource>()) {
            if(source == null) continue;

            string context = $"ServiceAppointmentSource/{source.name}";
            if(source.Appointment == null) {
                report.Warning("Service appointment source has no appointment assigned.", context);
                continue;
            }

            if(source.Appointment.RequiredProviderTags != null
                && source.Appointment.RequiredProviderTags.Count > 0
                && (source.ProviderTags == null || source.ProviderTags.Count == 0)) {
                report.Info("Service appointment requires provider tags but this source has no provider tags.", context);
            }

            if(source.TriggerAction == ServiceAppointmentSourceAction.CompleteDueAppointments
                && source.Appointment.CompletionMode == ServiceAppointmentCompletionMode.AutoCompleteWhenDue) {
                report.Info("Source trigger completes due appointments, but the assigned appointment is configured for auto-completion.", context);
            }

            if(source.ShopContext == null && source.Appointment.ShopContext == null && source.Appointment.BookingFee > 0f) {
                report.Info("Paid service appointment has no shop context. Base booking fee still works, but catalog/sponsor price modifiers will not apply.", context);
            }
        }
    }

    static void ValidateMarketServiceUIManagers(ProjectValidationReport report) {
        foreach(var manager in ProjectValidatorAssetFinder.FindAssets<MarketServiceUIManager>()) {
            if(manager == null) continue;

            string context = $"MarketServiceUIManager/{manager.name}";
            bool hasAnySource =
                manager.ShelfSource != null
                || manager.BasketSource != null
                || manager.CheckoutTerminal != null
                || manager.RefundSource != null
                || manager.DeliverySource != null
                || manager.LoyaltySource != null
                || manager.ServicePackageSource != null
                || manager.AppointmentSource != null;

            if(!hasAnySource) {
                report.Warning("Market/service UI manager has no shop, service or appointment source assigned.", context);
            }

            if(manager.CheckoutTerminal != null && manager.CheckoutTerminal.Shop == null) {
                report.Info("Market/service UI manager uses a checkout terminal whose shop is not assigned.", context);
            }

            if(manager.DeliverySource != null && manager.DeliverySource.Shop == null) {
                report.Info("Market/service UI manager uses a delivery source whose shop is not assigned.", context);
            }

            if(manager.RefundSource != null && manager.RefundSource.Shop == null) {
                report.Info("Market/service UI manager uses a refund source whose shop is not assigned.", context);
            }

            if(manager.ShelfSource != null && manager.ShelfSource.Shop == null) {
                report.Info("Market/service UI manager uses a shelf source whose shop is not assigned.", context);
            }
        }
    }

    static void ValidateCampStationUIManagers(ProjectValidationReport report) {
        foreach(var manager in ProjectValidatorAssetFinder.FindAssets<CampStationUIManager>()) {
            if(manager == null) continue;

            string context = $"CampStationUIManager/{manager.name}";
            bool hasSource = manager.Source != null;
            bool hasStation = manager.Station != null || (manager.Source != null && manager.Source.Station != null);

            if(!hasSource && !hasStation) {
                report.Warning("Camp station UI manager has no source or station assigned.", context);
            }

            if(manager.Source != null && manager.Source.Station == null) {
                report.Info("Camp station UI manager uses a source with no station assigned.", context);
            }

            if(manager.Source == null && manager.Station != null && manager.Station.RequireActivityZone && manager.ZoneContext == null) {
                report.Info("Camp station UI manager has a station requiring an activity zone but no explicit zone context. It will use PlayerActivityContext.CurrentZone.", context);
            }
        }
    }

    static void ValidateRoleActivityBoardUIManagers(ProjectValidationReport report) {
        foreach(var manager in ProjectValidatorAssetFinder.FindAssets<RoleActivityBoardUIManager>()) {
            if(manager == null) continue;

            string context = $"RoleActivityBoardUIManager/{manager.name}";
            bool hasSource = manager.Source != null;
            bool hasBoard = manager.Board != null || (manager.Source != null && manager.Source.Board != null);

            if(!hasSource && !hasBoard) {
                report.Warning("Role activity board UI manager has no source or board assigned.", context);
            }

            if(manager.Source != null && manager.Source.Board == null) {
                report.Info("Role activity board UI manager uses a source with no board assigned.", context);
            }

            if(manager.Source == null && manager.Board != null && manager.ZoneContext == null) {
                report.Info("Role activity board UI manager has no explicit zone context. It will use PlayerActivityContext.CurrentZone.", context);
            }
        }
    }

    static void ValidatePokeNavMapUIManagers(ProjectValidationReport report) {
        foreach(var manager in ProjectValidatorAssetFinder.FindAssets<PokeNavMapUIManager>()) {
            if(manager == null) continue;

            string context = $"PokeNavMapUIManager/{manager.name}";
            if(manager.MapViewProfile == null) {
                report.Info("PokeNav/map UI manager has no map view profile. It will use runtime MapMarkerRegistry fallback markers.", context);
            }

            if(manager.GuideSections == null || manager.GuideSections.Count == 0) {
                report.Info("PokeNav/map UI manager has no explicit guide sections. It will read guide sections from Resources.", context);
            } else {
                ValidateObjectList(manager.GuideSections, report, context, "PokeNav/map UI manager has a null guide section slot.");
            }

            if(manager.FeedPool != null && manager.FeedPool.Any(item => item == null)) {
                report.Warning("PokeNav/map UI manager has a null feed item slot.", context);
            }

            if(manager.SocialPostPool != null && manager.SocialPostPool.Any(post => post == null)) {
                report.Warning("PokeNav/map UI manager has a null social post slot.", context);
            }
        }
    }

    static void ValidatePokeNavKnowledgeDetailUIManagers(ProjectValidationReport report) {
        foreach(var manager in ProjectValidatorAssetFinder.FindAssets<PokeNavKnowledgeDetailUIManager>()) {
            if(manager == null) continue;

            string context = $"PokeNavKnowledgeDetailUIManager/{manager.name}";
            if(manager.SelectedType == PokeNavKnowledgeDetailType.None) {
                report.Info("PokeNav knowledge detail UI manager has no selected detail type yet. This is fine if another UI selects details at runtime.", context);
            } else if(string.IsNullOrWhiteSpace(manager.SelectedId)) {
                report.Info("PokeNav knowledge detail UI manager has a detail type but no selected id yet.", context);
            }

            if(manager.MaxRowsPerList == 0) {
                report.Info("PokeNav knowledge detail UI manager has unlimited detail rows. This is valid, but large content pools may need paging later.", context);
            }
        }
    }

    static void ValidatePokeNavMapFilterUIManagers(ProjectValidationReport report) {
        foreach(var manager in ProjectValidatorAssetFinder.FindAssets<PokeNavMapFilterUIManager>()) {
            if(manager == null) continue;

            string context = $"PokeNavMapFilterUIManager/{manager.name}";
            bool hasDefault = manager.DefaultProfile != null;
            bool hasPresets = manager.ProfilePresets != null && manager.ProfilePresets.Any(profile => profile != null);
            if(!hasDefault && !hasPresets && !manager.IncludeResourceProfiles) {
                report.Info("PokeNav map filter UI manager has no profile source. It will use runtime MapMarkerRegistry markers only.", context);
            }

            if(manager.ProfilePresets != null && manager.ProfilePresets.Any(profile => profile == null)) {
                report.Warning("PokeNav map filter UI manager has a null profile preset slot.", context);
            }

            if(manager.ActiveCategories != null && manager.ActiveCategories.Count > 0 && manager.ActiveCategories.Distinct().Count() != manager.ActiveCategories.Count) {
                report.Info("PokeNav map filter UI manager has duplicate active category filters.", context);
            }

            if(manager.ActiveTags != null && manager.ActiveTags.Any(string.IsNullOrWhiteSpace)) {
                report.Info("PokeNav map filter UI manager has an empty active tag filter.", context);
            }

            if(manager.MaxMarkerRows == 0) {
                report.Info("PokeNav map filter UI manager has unlimited marker rows. This is valid, but large marker pools may need paging later.", context);
            }
        }
    }

    static void ValidateJourneyIncidentUIManagers(ProjectValidationReport report) {
        foreach(var manager in ProjectValidatorAssetFinder.FindAssets<JourneyIncidentUIManager>()) {
            if(manager == null) continue;

            string context = $"JourneyIncidentUIManager/{manager.name}";
            bool hasSource = manager.Source != null;
            bool hasBoard = manager.Board != null || (manager.Source != null && manager.Source.Board != null);
            bool hasDirectIncident = manager.DirectIncident != null || (manager.DirectIncidents != null && manager.DirectIncidents.Any(incident => incident != null));

            if(!hasSource && !hasBoard && !hasDirectIncident) {
                report.Warning("Journey incident UI manager has no source, board or direct incident assigned.", context);
            }

            if(manager.Source != null && manager.Source.Board == null && manager.Source.Incident == null) {
                report.Info("Journey incident UI manager uses a source with no board or direct incident assigned.", context);
            }

            if(manager.DirectIncidents != null && manager.DirectIncidents.Any(incident => incident == null)) {
                report.Warning("Journey incident UI manager has a null direct incident slot.", context);
            }
        }
    }

    static void ValidateNotificationFeedUIManagers(ProjectValidationReport report) {
        foreach(var manager in ProjectValidatorAssetFinder.FindAssets<NotificationFeedUIManager>()) {
            if(manager == null) continue;

            string context = $"NotificationFeedUIManager/{manager.name}";
            if(manager.FeedOverride == null && !manager.CreateMissingFeed) {
                report.Info("Notification feed UI manager has no feed override and will not create a missing feed. It will only work if a NotificationFeed exists in the scene.", context);
            }

            if(manager.TemplateToPublish == null) {
                report.Info("Notification feed UI manager has no template assigned. Template publish actions need an explicit template argument or inspector assignment.", context);
            }

            if(manager.UseKindFilter && manager.ShowOnlyPinned) {
                report.Info("Notification feed UI manager filters by kind and pinned state together. This is valid, but can produce an empty log if no matching pinned entries exist.", context);
            }

            if(manager.UseChannelFilter && !manager.IncludeRead) {
                report.Info("Notification feed UI manager hides read entries while filtering by channel. This is useful for unread tabs but may make old channel entries disappear.", context);
            }
        }
    }

    static void ValidateBattleModeOptionsUIManagers(ProjectValidationReport report) {
        foreach(var manager in ProjectValidatorAssetFinder.FindAssets<BattleModeOptionsUIManager>()) {
            if(manager == null) continue;

            string context = $"BattleModeOptionsUIManager/{manager.name}";
            bool hasExplicitModes = manager.ModePool != null && manager.ModePool.Any(mode => mode != null);
            bool hasContextModes = manager.Challenge != null || manager.Negotiator != null;

            if(!hasExplicitModes && !manager.IncludeResourceModes && !hasContextModes) {
                report.Warning("Battle mode options UI manager has no explicit modes, no challenge/negotiator context and Include Resource Modes is disabled.", context);
            }

            if(manager.ModePool != null && manager.ModePool.Any(mode => mode == null)) {
                report.Warning("Battle mode options UI manager has a null battle mode slot.", context);
            }

            if(manager.Challenge != null && manager.Negotiator != null && manager.Negotiator.Challenge != null && manager.Negotiator.Challenge != manager.Challenge) {
                report.Info("Battle mode options UI manager has both an explicit challenge and a negotiator with a different challenge. The explicit challenge is used for option rows, while the negotiator can still force a battle mode.", context);
            }

            if(manager.Negotiator != null && manager.Negotiator.ForcedBattleMode != null && !manager.RespectChallengeContextWhenSelecting) {
                report.Info("Battle mode options UI manager has a forced negotiator mode but does not respect challenge context when selecting. Confirm this is intended for a global options screen.", context);
            }
        }
    }

    static void ValidateProgressionAccessUIManagers(ProjectValidationReport report) {
        foreach(var manager in ProjectValidatorAssetFinder.FindAssets<ProgressionAccessUIManager>()) {
            if(manager == null) continue;

            string context = $"ProgressionAccessUIManager/{manager.name}";
            bool hasTitlePool = manager.TitlePool != null && manager.TitlePool.Any(title => title != null);
            bool hasCareerPool = manager.CareerPool != null && manager.CareerPool.Any(career => career != null);
            bool hasMilestonePool = manager.MilestonePool != null && manager.MilestonePool.Any(milestone => milestone != null);
            bool hasFactionPool = manager.FactionPool != null && manager.FactionPool.Any(faction => faction != null);
            bool hasAccessPool = manager.AccessProfilePool != null && manager.AccessProfilePool.Any(profile => profile != null);
            bool readsAnyResources = manager.IncludeResourceTitles
                || manager.IncludeResourceCareers
                || manager.IncludeResourceMilestones
                || manager.IncludeResourceFactions
                || manager.IncludeResourceAccessProfiles;

            if(!hasTitlePool && !hasCareerPool && !hasMilestonePool && !hasFactionPool && !hasAccessPool && !readsAnyResources) {
                report.Warning("Progression/access UI manager has no explicit pools and all resource lookups are disabled.", context);
            }

            if(manager.TitlePool != null && manager.TitlePool.Any(title => title == null)) {
                report.Warning("Progression/access UI manager has a null title slot.", context);
            }

            if(manager.CareerPool != null && manager.CareerPool.Any(career => career == null)) {
                report.Warning("Progression/access UI manager has a null career slot.", context);
            }

            if(manager.MilestonePool != null && manager.MilestonePool.Any(milestone => milestone == null)) {
                report.Warning("Progression/access UI manager has a null milestone slot.", context);
            }

            if(manager.FactionPool != null && manager.FactionPool.Any(faction => faction == null)) {
                report.Warning("Progression/access UI manager has a null reputation faction slot.", context);
            }

            if(manager.AccessProfilePool != null && manager.AccessProfilePool.Any(profile => profile == null)) {
                report.Warning("Progression/access UI manager has a null access profile slot.", context);
            }

            if(!manager.CreateMissingLogsForActions && manager.PlayerOverride == null) {
                report.Info("Progression/access UI manager will not create missing player logs and has no player override. It will rely on a loaded PlayerController with existing logs.", context);
            }

            if(!manager.IncludeInactiveTitles && !manager.IncludeIncompleteMilestones && !manager.IncludeUnusedAccessProfiles) {
                report.Info("Progression/access UI manager only shows active/completed/history rows. This is valid for a status screen but not for a full unlock browser.", context);
            }

            if(!manager.IncludeLockedCareers) {
                report.Info("Progression/access UI manager hides locked careers. This is valid for compact screens but can make unavailable playstyles invisible.", context);
            }
        }
    }

    static void ValidateProgressionFocusedPanelUIManagers(ProjectValidationReport report) {
        foreach(var manager in ProjectValidatorAssetFinder.FindAssets<ProgressionFocusedPanelUIManager>()) {
            if(manager == null) continue;

            string context = $"ProgressionFocusedPanelUIManager/{manager.name}";
            if(manager.Source == null) {
                report.Info("Progression focused panel UI manager has no explicit source. It will search the scene or create a ProgressionAccessUIManager on this GameObject.", context);
            }

            if(manager.ActivePanel == ProgressionFocusedPanelType.Overview && manager.MaxRows == 0) {
                report.Info("Progression focused panel overview has unlimited rows. This is valid, but large content pools may need paging later.", context);
            }

            if(manager.ActiveTags != null && manager.ActiveTags.Any(string.IsNullOrWhiteSpace)) {
                report.Info("Progression focused panel UI manager has an empty active tag filter.", context);
            }
        }
    }

    static void ValidateRadialMenuUI(ProjectValidationReport report) {
        foreach(var controller in ProjectValidatorAssetFinder.FindAssets<RadialMenuController>()) {
            if(controller == null) continue;

            string context = $"RadialMenuController/{controller.name}";
            var view = controller.GetComponentInChildren<RadialMenuView>(true);
            if(view == null) {
                report.Info("Radial menu controller has no child RadialMenuView. This is fine if a view is assigned at runtime, but prefab wiring should include one.", context);
            }
        }

        foreach(var bridge in ProjectValidatorAssetFinder.FindAssets<RadialMenuOpenBridge>()) {
            if(bridge == null) continue;

            string context = $"RadialMenuOpenBridge/{bridge.name}";
            if(bridge.Controller == null) {
                report.Info("Radial menu open bridge has no explicit controller. It will search in hierarchy/scene at runtime.", context);
            }

            if(bridge.Provider == null) {
                report.Warning("Radial menu open bridge has no provider assigned. Assign a Party, Inventory, World/Tool or Encounter radial provider.", context);
            } else if(bridge.Provider is not IRadialMenuProvider) {
                report.Warning("Radial menu open bridge provider does not implement IRadialMenuProvider.", context);
            }

            if(bridge.DefaultIndex < -1) {
                report.Warning("Radial menu open bridge default index should be -1 or greater.", context);
            }
        }

        foreach(var view in ProjectValidatorAssetFinder.FindAssets<RadialMenuView>()) {
            if(view == null) continue;

            string context = $"RadialMenuView/{view.name}";
            if(view.Segments == null) {
                report.Info("Radial menu view has no segment instances yet. They are usually created from the segment prefab at runtime.", context);
            }
        }

        foreach(var layout in ProjectValidatorAssetFinder.FindAssets<RadialMenuLayoutProfile>()) {
            if(layout == null) continue;

            string context = $"RadialMenuLayout/{layout.name}";
            if(layout.Radius <= 0f) {
                report.Warning("Radial menu layout radius is 0, so segments will overlap at the center.", context);
            }

            if(layout.SegmentSize.x <= 0f || layout.SegmentSize.y <= 0f) {
                report.Warning("Radial menu layout segment size has a non-positive axis.", context);
            }
        }

        foreach(var provider in ProjectValidatorAssetFinder.FindAssets<RadialPartyMenuProvider>()) {
            if(provider == null) continue;

            string context = $"RadialPartyMenuProvider/{provider.name}";
            if(provider.PartyScreen == null && provider.PartyOverride == null) {
                report.Info("Radial party menu provider has no explicit PartyScreen or PokemonParty. It will resolve them at runtime.", context);
            }

            if(provider.Actions == null || provider.Actions.Count == 0) {
                report.Warning("Radial party menu provider has no action definitions.", context);
            } else if(provider.Actions.Any(action => action == null)) {
                report.Warning("Radial party menu provider has a null action slot.", context);
            }
        }

        foreach(var provider in ProjectValidatorAssetFinder.FindAssets<RadialInventoryMenuProvider>()) {
            if(provider == null) continue;

            string context = $"RadialInventoryMenuProvider/{provider.name}";
            if(provider.InventoryUI == null && provider.InventoryOverride == null && provider.ItemOverride == null) {
                report.Info("Radial inventory menu provider has no explicit InventoryUI, Inventory or Item override. It will resolve context/item sources at runtime.", context);
            }

            if(provider.CategoryIndex >= Inventory.ItemCategories.Count) {
                report.Warning("Radial inventory menu provider category index is outside the known inventory category range.", context);
            }

            if(provider.Actions == null || provider.Actions.Count == 0) {
                report.Warning("Radial inventory menu provider has no action definitions.", context);
            } else if(provider.Actions.Any(action => action == null)) {
                report.Warning("Radial inventory menu provider has a null action slot.", context);
            }
        }

        foreach(var provider in ProjectValidatorAssetFinder.FindAssets<RadialWorldToolMenuProvider>()) {
            if(provider == null) continue;

            string context = $"RadialWorldToolMenuProvider/{provider.name}";
            if(provider.InteractionSensor == null && provider.InteractableOverride == null && provider.PromptOverride == null) {
                report.Info("Radial world/tool menu provider has no explicit sensor, interactable or prompt source. It will resolve world context at runtime.", context);
            }

            if(provider.InteractableOverride != null && provider.InteractableOverride is not Interactable) {
                report.Warning("Radial world/tool menu provider interactable override does not implement Interactable.", context);
            }

            if(provider.ToolActions != null && provider.ToolActions.Any(action => action == null)) {
                report.Warning("Radial world/tool menu provider has a null tool action slot.", context);
            }

            if(provider.ToolActions != null && provider.ToolActions.Any(action => action != null && action.actionKind == RadialWorldToolActionKind.UseTool && action.tool == null)) {
                report.Info("Radial world/tool menu provider has a UseTool action without a linked ToolDefinition. It will behave as a generic action.", context);
            }
        }

        foreach(var provider in ProjectValidatorAssetFinder.FindAssets<RadialEncounterMenuProvider>()) {
            if(provider == null) continue;

            string context = $"RadialEncounterMenuProvider/{provider.name}";
            if(provider.UIManager == null && provider.ChoiceSource == null) {
                report.Info("Radial encounter menu provider has no explicit UI manager or choice source. It will resolve encounter context at runtime.", context);
            }

            if(provider.MaxOptions == 1) {
                report.Info("Radial encounter menu provider only shows one choice option. This is valid, but unusual for a radial choice menu.", context);
            }

            if(provider.RunChoiceOnSelect && provider.ChoiceSource == null && provider.UIManager == null) {
                report.Info("Radial encounter menu provider can run choices on select but currently relies on runtime source resolution.", context);
            }
        }
    }

    static void ValidateCompetitionRegistrationUIManagers(ProjectValidationReport report) {
        foreach(var manager in ProjectValidatorAssetFinder.FindAssets<CompetitionRegistrationUIManager>()) {
            if(manager == null) continue;

            string context = $"CompetitionRegistrationUIManager/{manager.name}";
            bool hasRegistrationSource = manager.RegistrationSource != null && manager.RegistrationSource.Registration != null;
            bool hasRegistrationContext = manager.RegistrationContext != null;
            bool hasRegistrationPool = manager.RegistrationPool != null && manager.RegistrationPool.Any(registration => registration != null);
            bool hasInvitationPool = manager.InvitationPool != null && manager.InvitationPool.Any(invitation => invitation != null);
            bool hasVenuePool = manager.VenuePool != null && manager.VenuePool.Any(venue => venue != null);
            bool readsAnyResources = manager.IncludeResourceRegistrations || manager.IncludeResourceInvitations || manager.IncludeResourceVenues;

            if(!hasRegistrationSource && !hasRegistrationContext && !hasRegistrationPool && !hasInvitationPool && !hasVenuePool && !readsAnyResources) {
                report.Warning("Competition registration UI manager has no source, context, explicit pools and all resource lookups are disabled.", context);
            }

            if(manager.RegistrationSource != null && manager.RegistrationSource.Registration == null) {
                report.Info("Competition registration UI manager uses a registration source with no registration assigned.", context);
            }

            if(manager.PrepareMatchAfterRegistration && manager.BracketSource == null && (manager.RegistrationSource == null || manager.RegistrationSource.BracketSource == null)) {
                report.Warning("Competition registration UI manager prepares a match after registration but no bracket source is available.", context);
            }

            if(manager.RegistrationPool != null && manager.RegistrationPool.Any(registration => registration == null)) {
                report.Warning("Competition registration UI manager has a null registration slot.", context);
            }

            if(manager.InvitationPool != null && manager.InvitationPool.Any(invitation => invitation == null)) {
                report.Warning("Competition registration UI manager has a null invitation slot.", context);
            }

            if(manager.VenuePool != null && manager.VenuePool.Any(venue => venue == null)) {
                report.Warning("Competition registration UI manager has a null venue slot.", context);
            }

            if(manager.RegistrationContext == null && manager.RegistrationSource == null && hasVenuePool) {
                report.Info("Competition registration UI manager has venue rows but no registration context/source. Venues will only be evaluated for entry, not registration hosting.", context);
            }

            if(!manager.IncludeBlockedRegistrations) {
                report.Info("Competition registration UI manager hides blocked registrations. This is valid for compact kiosks but can hide future tournament options.", context);
            }
        }
    }

    static void ValidateCompetitionBracketRankingUIManagers(ProjectValidationReport report) {
        foreach(var manager in ProjectValidatorAssetFinder.FindAssets<CompetitionBracketRankingUIManager>()) {
            if(manager == null) continue;

            string context = $"CompetitionBracketRankingUIManager/{manager.name}";
            bool hasRankingPool = manager.RankingPool != null && manager.RankingPool.Any(ranking => ranking != null);
            bool hasRosterPool = manager.RosterPool != null && manager.RosterPool.Any(roster => roster != null);
            bool hasSeasonPool = manager.SeasonPool != null && manager.SeasonPool.Any(season => season != null);
            bool readsAnyResources = manager.IncludeResourceRankings || manager.IncludeResourceRosters || manager.IncludeResourceSeasons;

            if(!hasRankingPool && !hasRosterPool && !hasSeasonPool && !readsAnyResources) {
                report.Warning("Competition bracket/ranking UI manager has no explicit pools and all resource lookups are disabled.", context);
            }

            if(manager.RankingPool != null && manager.RankingPool.Any(ranking => ranking == null)) {
                report.Warning("Competition bracket/ranking UI manager has a null ranking slot.", context);
            }

            if(manager.RosterPool != null && manager.RosterPool.Any(roster => roster == null)) {
                report.Warning("Competition bracket/ranking UI manager has a null roster slot.", context);
            }

            if(manager.SeasonPool != null && manager.SeasonPool.Any(season => season == null)) {
                report.Warning("Competition bracket/ranking UI manager has a null season slot.", context);
            }

            if(manager.FilterBySelectedRanking && string.IsNullOrWhiteSpace(manager.SelectedRankingId)) {
                report.Info("Competition bracket/ranking UI manager filters by selected ranking, but no selected ranking id is set yet.", context);
            }

            if(!manager.IncludeInactiveBrackets && manager.ActiveTab == CompetitionBracketRankingTab.MatchHistory && !manager.IncludeMatchHistory) {
                report.Info("Competition bracket/ranking UI manager is on match history tab but hides inactive brackets and history rows. Only current active bracket matches can appear.", context);
            }

            if(!manager.IncludeLockedRankings && !manager.IncludeBlockedRosters && !manager.IncludeBlockedSeasons) {
                report.Info("Competition bracket/ranking UI manager hides all locked/blocked rows. This is valid for compact screens but can hide future league goals.", context);
            }
        }
    }

    static void ValidateTransitJourneyUIManagers(ProjectValidationReport report) {
        foreach(var manager in ProjectValidatorAssetFinder.FindAssets<TransitJourneyUIManager>()) {
            if(manager == null) continue;

            string context = $"TransitJourneyUIManager/{manager.name}";
            bool hasSourceJourney = manager.IncludeSourceJourney && manager.Source != null && manager.Source.Journey != null;
            bool hasJourneyPool = manager.JourneyPool != null && manager.JourneyPool.Any(journey => journey != null);
            bool readsResources = manager.IncludeResourceJourneys;

            if(!hasSourceJourney && !hasJourneyPool && !readsResources) {
                report.Warning("Transit journey UI manager has no source journey, explicit journey pool and Include Resource Journeys is disabled.", context);
            }

            if(manager.Source != null && manager.Source.Journey == null) {
                report.Warning("Transit journey UI manager source has no journey assigned.", context);
            }

            if(manager.JourneyPool != null && manager.JourneyPool.Any(journey => journey == null)) {
                report.Warning("Transit journey UI manager has a null journey slot.", context);
            }

            if(manager.Source == null && manager.Station == null && string.IsNullOrWhiteSpace(manager.OriginStopId)) {
                report.Info("Transit journey UI manager has no source, station or origin override. Journey options will use the first leg origin where available.", context);
            }

            if(!manager.CreateMissingLogsForActions) {
                report.Info("Transit journey UI manager will not create missing player transit logs during actions.", context);
            }

            if(!manager.IncludeBlockedJourneys) {
                report.Info("Transit journey UI manager hides blocked journeys. This is valid for compact station screens but can hide locked future routes.", context);
            }
        }
    }

    static void ValidateEncounterResolutionUIManagers(ProjectValidationReport report) {
        foreach(var manager in ProjectValidatorAssetFinder.FindAssets<EncounterResolutionUIManager>()) {
            if(manager == null) continue;

            string context = $"EncounterResolutionUIManager/{manager.name}";
            bool hasSource = manager.ChoiceSource != null;
            bool hasFallback = manager.FallbackChoiceSet != null && (manager.FallbackPokemon != null || manager.FallbackEncounterTable != null);

            if(!hasSource && !hasFallback) {
                report.Warning("Encounter resolution UI manager has no choice source and no complete fallback choice set context.", context);
            }

            if(manager.ChoiceSource != null && manager.ChoiceSource.ChoiceSet == null) {
                report.Warning("Encounter resolution UI manager source has no choice set.", context);
            }

            if(manager.FallbackChoiceSet != null && manager.FallbackPokemon == null && manager.FallbackEncounterTable == null) {
                report.Warning("Encounter resolution UI manager fallback choice set has no fallback Pokemon or encounter table.", context);
            }
        }
    }

    static void ValidateOverworldEncounterDebugUIManagers(ProjectValidationReport report) {
        foreach(var manager in ProjectValidatorAssetFinder.FindAssets<OverworldEncounterDebugUIManager>()) {
            if(manager == null) continue;

            string context = $"OverworldEncounterDebugUIManager/{manager.name}";
            if(manager.DebugSource == null) {
                report.Warning("Overworld encounter debug UI manager has no debug source assigned.", context);
            }

            if(!manager.IncludeBlockedNodes && !manager.IncludeBlockedConnections) {
                report.Info("Overworld encounter debug UI manager hides blocked nodes and blocked connections. This is fine for compact UI, but can hide the reason a path fails.", context);
            }
        }
    }

    static void ValidatePokeNavFeedSources(ProjectValidationReport report) {
        foreach(var source in ProjectValidatorAssetFinder.FindAssets<PokeNavFeedSource>()) {
            if(source == null) continue;

            string context = $"PokeNavFeedSource/{source.name}";
            if(source.FeedItems == null || source.FeedItems.Count == 0) {
                report.Warning("PokeNav feed source has no feed items assigned.", context);
            } else {
                ValidateObjectList(source.FeedItems, report, context, "PokeNav feed source has a null feed item slot.");
            }
        }
    }

    static void ValidateMapDiscoverySources(ProjectValidationReport report) {
        foreach(var source in ProjectValidatorAssetFinder.FindAssets<MapDiscoverySource>()) {
            if(source == null) continue;

            string context = $"MapDiscoverySource/{source.name}";
            bool hasMarkers = source.MarkersToDiscover != null && source.MarkersToDiscover.Count > 0;
            bool hasProviders = source.MarkerProvidersToDiscover != null && source.MarkerProvidersToDiscover.Count > 0;
            bool hasTarget = source.NavigationTargetProvider != null || source.NavigationTargetMarker != null;

            if(!hasMarkers && !hasProviders && !source.SetNavigationTarget) {
                report.Warning("Map discovery source has no markers, providers or navigation target behavior assigned.", context);
            }

            if(hasMarkers) {
                ValidateObjectList(source.MarkersToDiscover, report, context, "Map discovery source has a null marker slot.");
            }

            if(hasProviders) {
                ValidateObjectList(source.MarkerProvidersToDiscover, report, context, "Map discovery source has a null provider slot.");
            }

            if(source.SetNavigationTarget && !hasTarget && !hasMarkers && !hasProviders) {
                report.Warning("Map discovery source wants to set a navigation target but has no target marker/provider.", context);
            }
        }
    }

    static void ValidateBattleChallenges(ProjectValidationReport report) {
        foreach(var challenge in ProjectValidatorAssetFinder.FindAssets<BattleChallengeDefinition>()) {
            if(challenge == null) continue;

            string context = $"BattleChallenge/{challenge.name}";
            if(string.IsNullOrWhiteSpace(challenge.Id)) {
                report.Error("Battle challenge id is empty.", context);
            }

            if(challenge.DefaultRuleSet == null) {
                report.Warning("Battle challenge has no default rule set.", context);
            }

            if(challenge.AlternativeRuleSets != null) {
                foreach(var ruleSet in challenge.AlternativeRuleSets) {
                    if(ruleSet == null) {
                        report.Warning("Battle challenge has a null alternative rule slot.", context);
                    }
                }
            }

            ValidateObjectList(challenge.AllowedBattleModes, report, context, "Battle challenge has a null allowed battle mode slot.");
            if(challenge.DefaultBattleMode != null
                && challenge.AllowedBattleModes != null
                && challenge.AllowedBattleModes.Count > 0
                && !challenge.AllowedBattleModes.Contains(challenge.DefaultBattleMode)) {
                report.Warning("Battle challenge default battle mode is not listed in allowed battle modes.", context);
            }

            ValidateCareerPointGrants(challenge.CompletionCareerPointRewards, report, context);
            ValidateCareerPointGrants(challenge.WinCareerPointRewards, report, context);
            ValidateLifePathRewards(challenge.CompletionLifePathRewards, report, context);
            ValidateLifePathRewards(challenge.WinLifePathRewards, report, context);
            ValidateOrganizationMembershipGrants(challenge.CompletionOrganizationMembershipRewards, report, context);
            ValidateOrganizationPointGrants(challenge.CompletionOrganizationPointRewards, report, context);
            ValidateOrganizationMembershipGrants(challenge.WinOrganizationMembershipRewards, report, context);
            ValidateOrganizationPointGrants(challenge.WinOrganizationPointRewards, report, context);
        }
    }

    static void ValidateContests(ProjectValidationReport report) {
        foreach(var contest in ProjectValidatorAssetFinder.FindAssets<ContestDefinition>()) {
            if(contest == null) continue;

            string context = $"Contest/{contest.name}";
            if(string.IsNullOrWhiteSpace(contest.Id)) {
                report.Error("Contest id is empty.", context);
            }

            if(contest.EntryMode != ContestEntryMode.WholeParty && contest.MinPokemonLevel > 0 && contest.MaxPokemonLevel > 0 && contest.MinPokemonLevel > contest.MaxPokemonLevel) {
                report.Warning("Minimum Pokemon level is higher than maximum Pokemon level.", context);
            }

            if(contest.ScoreCriteria == null || contest.ScoreCriteria.Count == 0) {
                report.Info("Contest has no score criteria. Only base score will be used.", context);
            }

            if(contest.RankThresholds == null || contest.RankThresholds.Count == 0) {
                report.Warning("Contest has no rank thresholds, so completions cannot produce a rank/win.", context);
            }

            foreach(var cost in contest.EntryCosts) {
                if(cost != null && cost.item == null && cost.count > 0) {
                    report.Warning("Contest entry cost has a count but no item.", context);
                }
            }

            ValidateCareerPointGrants(contest.ParticipationCareerPointRewards, report, context);
            ValidateLifePathRewards(contest.ParticipationLifePathRewards, report, context);
            ValidateOrganizationMembershipGrants(contest.ParticipationOrganizationMembershipRewards, report, context);
            ValidateOrganizationPointGrants(contest.ParticipationOrganizationPointRewards, report, context);

            foreach(var rank in contest.RankThresholds) {
                if(rank == null) {
                    report.Warning("Contest has a null rank threshold slot.", context);
                    continue;
                }

                if(rank.countsAsWin && string.IsNullOrWhiteSpace(rank.displayName) && string.IsNullOrWhiteSpace(rank.id)) {
                    report.Info("Winning rank has no id/display name. It will still work but future UI may look plain.", context);
                }

                ValidateCareerPointGrants(rank.careerPointRewards, report, context);
                ValidateLifePathRewards(rank.lifePathRewards, report, context);
                ValidateOrganizationMembershipGrants(rank.organizationMembershipRewards, report, context);
                ValidateOrganizationPointGrants(rank.organizationPointRewards, report, context);
            }
        }
    }

    static void ValidateCareers(ProjectValidationReport report) {
        foreach(var career in ProjectValidatorAssetFinder.FindAssets<CareerPathDefinition>()) {
            if(career == null) continue;

            string context = $"Career/{career.name}";
            if(string.IsNullOrWhiteSpace(career.Id)) {
                report.Error("Career id is empty.", context);
            }

            if(career.Ranks == null || career.Ranks.Count == 0) {
                report.Info("Career has no ranks. It can still track points, but rank rewards will never trigger.", context);
            }

            if(career.JoinMode == CareerJoinMode.MentorOnly) {
                bool hasMentor = ProjectValidatorAssetFinder.FindAssets<CareerMentor>()
                    .Any(mentor => mentor != null && mentor.Career == career);
                if(!hasMentor) {
                    report.Info("Career is mentor-only. Make sure at least one CareerMentor references it in a loaded scene.", context);
                }
            }

            var duplicateRanks = (career.Ranks ?? new List<CareerRankDefinition>())
                .Where(rank => rank != null)
                .GroupBy(rank => rank.Id)
                .Where(group => !string.IsNullOrWhiteSpace(group.Key) && group.Count() > 1);

            foreach(var duplicate in duplicateRanks) {
                report.Warning($"Career has duplicate rank id '{duplicate.Key}'. Rewards are claimed by rank id, so duplicates may skip rewards.", context);
            }

            foreach(var rank in career.Ranks) {
                if(rank == null) {
                    report.Warning("Career has a null rank slot.", context);
                    continue;
                }

                if(rank.trainerExperience > 0 && rank.overrideExperienceSource && rank.experienceSource == PlayerExperienceSource.Battle) {
                    report.Info("Career rank grants Battle XP source. This is valid, but check that it is intentional.", context);
                }

                ValidateOrganizationMembershipGrants(rank.organizationMembershipRewards, report, context);
                ValidateOrganizationPointGrants(rank.organizationPointRewards, report, context);
            }
        }
    }

    static void ValidateOrganizations(ProjectValidationReport report) {
        foreach(var organization in ProjectValidatorAssetFinder.FindAssets<OrganizationDefinition>()) {
            if(organization == null) continue;

            string context = $"Organization/{organization.name}";
            if(string.IsNullOrWhiteSpace(organization.Id)) {
                report.Error("Organization id is empty.", context);
            }

            if(organization.Ranks == null || organization.Ranks.Count == 0) {
                report.Info("Organization has no ranks. It can still grant membership and points, but rank rewards will never trigger.", context);
            }

            if(!organization.CanRunAlongsideExclusiveGroup && string.IsNullOrWhiteSpace(organization.ExclusiveGroup)) {
                report.Info("Organization blocks exclusive group memberships but has no Exclusive Group id.", context);
            }

            if(!organization.PermanentByDefault && organization.CanBeTemporary && organization.DefaultDurationHours <= 0) {
                report.Warning("Temporary organization membership has no default duration.", context);
            }

            var duplicateRanks = (organization.Ranks ?? new List<OrganizationRankDefinition>())
                .Where(rank => rank != null)
                .GroupBy(rank => rank.Id)
                .Where(group => !string.IsNullOrWhiteSpace(group.Key) && group.Count() > 1);

            foreach(var duplicate in duplicateRanks) {
                report.Warning($"Organization has duplicate rank id '{duplicate.Key}'. Rewards are claimed by rank id, so duplicates may skip rewards.", context);
            }

            foreach(var rank in organization.Ranks) {
                if(rank == null) {
                    report.Warning("Organization has a null rank slot.", context);
                    continue;
                }

                ValidateCareerPointGrants(rank.careerPointRewards, report, context);
                ValidateLifePathRewards(rank.lifePathRewards, report, context);
            }

            ValidateLifePathRewards(organization.JoinLifePathRewards, report, context);

            foreach(var board in organization.LinkedJobBoards) {
                if(board == null) {
                    report.Warning("Organization has a null linked job board slot.", context);
                }
            }

            foreach(var shop in organization.LinkedShops) {
                if(shop == null) {
                    report.Warning("Organization has a null linked shop slot.", context);
                }
            }
        }
    }

    static void ValidateAssignments(ProjectValidationReport report) {
        foreach(var assignment in ProjectValidatorAssetFinder.FindAssets<AssignmentDefinition>()) {
            if(assignment == null) continue;

            string context = $"Assignment/{assignment.name}";
            if(string.IsNullOrWhiteSpace(assignment.Id)) {
                report.Error("Assignment id is empty.", context);
            }

            foreach(var requirement in assignment.AcceptanceRequirements) {
                if(requirement == null) {
                    report.Warning("Assignment has a null acceptance requirement slot.", context);
                }
            }

            foreach(var requirement in assignment.CompletionRequirements) {
                if(requirement == null) {
                    report.Warning("Assignment has a null completion requirement slot.", context);
                }
            }

            foreach(var link in assignment.LinkedJobs) {
                if(link == null) {
                    report.Warning("Assignment has a null linked job slot.", context);
                    continue;
                }

                if(link.job == null) {
                    report.Warning("Assignment linked job has no job definition.", context);
                }
            }

            ValidateCareerPointGrants(assignment.CareerPointRewards, report, context);
            ValidateLifePathRewards(assignment.AcceptanceLifePathRewards, report, context);
            ValidateLifePathRewards(assignment.LifePathRewards, report, context);
            ValidateOrganizationMembershipGrants(assignment.OrganizationMembershipRewards, report, context);
            ValidateOrganizationPointGrants(assignment.OrganizationPointRewards, report, context);
            ValidateResearchProgressRewards(assignment.ResearchRewards, report, context);
            ValidateObjectList(assignment.AcceptanceCompetitionInvitations, report, context, "Assignment has a null acceptance competition invitation slot.");
            ValidateObjectList(assignment.CompetitionInvitationRewards, report, context, "Assignment has a null competition invitation reward slot.");
            ValidateObjectList(assignment.AcceptanceSponsors, report, context, "Assignment has a null acceptance sponsor slot.");
            ValidateObjectList(assignment.SponsorRewards, report, context, "Assignment has a null sponsor reward slot.");
        }
    }

    static void ValidateAccessProfiles(ProjectValidationReport report) {
        foreach(var profile in ProjectValidatorAssetFinder.FindAssets<AccessProfileDefinition>()) {
            if(profile == null) continue;

            string context = $"AccessProfile/{profile.name}";
            if(string.IsNullOrWhiteSpace(profile.Id)) {
                report.Error("Access profile id is empty.", context);
            }

            foreach(var requirement in profile.ExtraRequirements) {
                if(requirement == null) {
                    report.Warning("Access profile has a null extra requirement slot.", context);
                }
            }
        }
    }

    static void ValidateLawViolations(ProjectValidationReport report) {
        foreach(var violation in ProjectValidatorAssetFinder.FindAssets<LawViolationDefinition>()) {
            if(violation == null) continue;

            string context = $"LawViolation/{violation.name}";
            if(string.IsNullOrWhiteSpace(violation.Id)) {
                report.Error("Law violation id is empty.", context);
            }

            if(violation.WantedPoints <= 0 && violation.FineAmount <= 0f && (violation.ReputationChanges == null || violation.ReputationChanges.Count == 0)) {
                report.Info("Law violation has no wanted points, fine or reputation changes. This is valid if it is only used as a marker.", context);
            }

            ValidateReputationChanges(violation.ReputationChanges, report, context);
            ValidateTitleGrants(violation.TitleGrants, report, context);

            foreach(var milestone in violation.MilestonesToComplete) {
                if(milestone == null) {
                    report.Warning("Law violation has a null milestone reward slot.", context);
                }
            }
        }
    }

    static void ValidateInvestigations(ProjectValidationReport report) {
        foreach(var clue in ProjectValidatorAssetFinder.FindAssets<InvestigationClueDefinition>()) {
            if(clue == null) continue;

            string context = $"InvestigationClue/{clue.name}";
            if(string.IsNullOrWhiteSpace(clue.Id)) {
                report.Error("Investigation clue id is empty.", context);
            }

            foreach(var requirement in clue.DiscoveryRequirements) {
                if(requirement == null) {
                    report.Warning("Investigation clue has a null discovery requirement slot.", context);
                }
            }
        }

        foreach(var investigationCase in ProjectValidatorAssetFinder.FindAssets<InvestigationCaseDefinition>()) {
            if(investigationCase == null) continue;

            string context = $"InvestigationCase/{investigationCase.name}";
            if(string.IsNullOrWhiteSpace(investigationCase.Id)) {
                report.Error("Investigation case id is empty.", context);
            }

            foreach(var requirement in investigationCase.StartRequirements) {
                if(requirement == null) {
                    report.Warning("Investigation case has a null start requirement slot.", context);
                }
            }

            foreach(var requirement in investigationCase.CompletionRequirements) {
                if(requirement == null) {
                    report.Warning("Investigation case has a null completion requirement slot.", context);
                }
            }

            foreach(var rule in investigationCase.Clues) {
                if(rule == null) {
                    report.Warning("Investigation case has a null clue rule slot.", context);
                    continue;
                }

                if(rule.clue == null) {
                    report.Warning("Investigation case clue rule has no clue assigned.", context);
                }

                foreach(var requirement in rule.extraRequirements) {
                    if(requirement == null) {
                        report.Warning("Investigation case clue rule has a null extra requirement slot.", context);
                    }
                }
            }

            var duplicateClues = investigationCase.Clues
                .Where(rule => rule != null && rule.clue != null)
                .GroupBy(rule => rule.clue.Id)
                .Where(group => !string.IsNullOrWhiteSpace(group.Key) && group.Count() > 1);

            foreach(var duplicate in duplicateClues) {
                report.Warning($"Investigation case has duplicate clue '{duplicate.Key}'.", context);
            }

            foreach(var stage in investigationCase.Stages) {
                if(stage == null) {
                    report.Warning("Investigation case has a null stage slot.", context);
                }
            }

            foreach(var reward in investigationCase.ItemRewards) {
                if(reward != null && reward.item == null && reward.count > 0) {
                    report.Warning("Investigation item reward has count but no item.", context);
                }
            }

            ValidateReputationChanges(investigationCase.ReputationRewards, report, context);
            ValidateTitleGrants(investigationCase.TitleRewards, report, context);
            ValidateCareerPointGrants(investigationCase.CareerPointRewards, report, context);
            ValidateLifePathRewards(investigationCase.LifePathRewards, report, context);
            ValidateOrganizationMembershipGrants(investigationCase.OrganizationMembershipRewards, report, context);
            ValidateOrganizationPointGrants(investigationCase.OrganizationPointRewards, report, context);
            ValidateResearchProgressRewards(investigationCase.ResearchRewards, report, context);

            foreach(var milestone in investigationCase.MilestonesToComplete) {
                if(milestone == null) {
                    report.Warning("Investigation case has a null milestone reward slot.", context);
                }
            }
        }
    }

    static void ValidateNPCMemoryTopics(ProjectValidationReport report) {
        foreach(var topic in ProjectValidatorAssetFinder.FindAssets<NPCMemoryTopicDefinition>()) {
            if(topic == null) continue;

            string context = $"NPCMemoryTopic/{topic.name}";
            if(string.IsNullOrWhiteSpace(topic.Id)) {
                report.Error("NPC memory topic id is empty.", context);
            }

            if(topic.Tags != null && topic.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("NPC memory topic has an empty tag slot.", context);
            }
        }
    }

    static void ValidateNPCReactions(ProjectValidationReport report) {
        foreach(var reaction in ProjectValidatorAssetFinder.FindAssets<NPCReactionDefinition>()) {
            if(reaction == null) continue;

            string context = $"NPCReaction/{reaction.name}";
            if(string.IsNullOrWhiteSpace(reaction.Id)) {
                report.Error("NPC reaction id is empty.", context);
            }

            if(reaction.Tags != null && reaction.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("NPC reaction has an empty tag slot.", context);
            }

            foreach(var requirement in reaction.PlayerRequirements) {
                if(requirement == null) {
                    report.Warning("NPC reaction has a null player requirement slot.", context);
                }
            }

            ValidateRelationshipChanges(reaction.RelationshipChanges, report, context);
            ValidateReputationChanges(reaction.ReputationChanges, report, context);
            ValidateTitleGrants(reaction.TitleGrants, report, context);

            foreach(var milestone in reaction.MilestonesToComplete) {
                if(milestone == null) {
                    report.Warning("NPC reaction has a null milestone slot.", context);
                }
            }

            if(reaction.RecordLawViolation && reaction.LawViolation == null) {
                report.Warning("NPC reaction records a law violation but has no law violation assigned.", context);
            }
        }
    }

    static void ValidateWitnessReports(ProjectValidationReport report) {
        foreach(var witnessReport in ProjectValidatorAssetFinder.FindAssets<WitnessReportDefinition>()) {
            if(witnessReport == null) continue;

            string context = $"WitnessReport/{witnessReport.name}";
            if(string.IsNullOrWhiteSpace(witnessReport.Id)) {
                report.Error("Witness report id is empty.", context);
            }

            if(witnessReport.Tags != null && witnessReport.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Witness report has an empty tag slot.", context);
            }

            foreach(var requirement in witnessReport.PlayerRequirements) {
                if(requirement == null) {
                    report.Warning("Witness report has a null player requirement slot.", context);
                }
            }

            foreach(var reaction in witnessReport.WitnessReactions) {
                if(reaction == null) {
                    report.Warning("Witness report has a null reaction slot.", context);
                }
            }

            foreach(var propagation in witnessReport.Propagations) {
                if(propagation == null) {
                    report.Warning("Witness report has a null propagation slot.", context);
                }
            }

            ValidateRelationshipChanges(witnessReport.RelationshipChanges, report, context);
            ValidateReputationChanges(witnessReport.ReputationChanges, report, context);
            ValidateTitleGrants(witnessReport.TitleGrants, report, context);

            foreach(var milestone in witnessReport.MilestonesToComplete) {
                if(milestone == null) {
                    report.Warning("Witness report has a null milestone slot.", context);
                }
            }

            if(witnessReport.RecordLawViolation && witnessReport.LawViolation == null) {
                report.Warning("Witness report records a law violation but has no law violation assigned.", context);
            }

            if(witnessReport.RecordRiskIncident && witnessReport.RiskIncident == null) {
                report.Warning("Witness report records a risk incident but has no risk incident assigned.", context);
            }
        }
    }

    static void ValidateReportPropagations(ProjectValidationReport report) {
        foreach(var propagation in ProjectValidatorAssetFinder.FindAssets<ReportPropagationDefinition>()) {
            if(propagation == null) continue;

            string context = $"ReportPropagation/{propagation.name}";
            if(string.IsNullOrWhiteSpace(propagation.Id)) {
                report.Error("Report propagation id is empty.", context);
            }

            if(propagation.Tags != null && propagation.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Report propagation has an empty tag slot.", context);
            }

            foreach(var requirement in propagation.PlayerRequirements) {
                if(requirement == null) {
                    report.Warning("Report propagation has a null player requirement slot.", context);
                }
            }

            foreach(var target in propagation.Targets) {
                ValidateReportPropagationTarget(target, report, context);
            }

            ValidateRelationshipChanges(propagation.RelationshipChanges, report, context);
            ValidateReputationChanges(propagation.ReputationChanges, report, context);
            ValidateTitleGrants(propagation.TitleGrants, report, context);

            foreach(var milestone in propagation.MilestonesToComplete) {
                if(milestone == null) {
                    report.Warning("Report propagation has a null milestone slot.", context);
                }
            }
        }
    }

    static void ValidateReportPropagationTarget(ReportPropagationTarget target, ProjectValidationReport report, string context) {
        if(target == null) {
            report.Warning("Report propagation has a null target slot.", context);
            return;
        }

        switch(target.targetType) {
            case ReportPropagationTargetType.ExplicitFaction:
                if(target.faction == null) {
                    report.Warning("Explicit Faction propagation target has no faction.", context);
                }
                break;
            case ReportPropagationTargetType.RelationshipSubject:
                if(target.relationshipSubject == null) {
                    report.Warning("Relationship Subject propagation target has no relationship subject.", context);
                }
                break;
            case ReportPropagationTargetType.Organization:
                if(target.organization == null) {
                    report.Warning("Organization propagation target has no organization.", context);
                }
                break;
            case ReportPropagationTargetType.Career:
                if(target.career == null) {
                    report.Warning("Career propagation target has no career.", context);
                }
                break;
            case ReportPropagationTargetType.CustomGroup:
                if(string.IsNullOrWhiteSpace(target.customTargetId)) {
                    report.Warning("Custom Group propagation target has no custom target id.", context);
                }
                break;
        }

        ValidateRelationshipChanges(target.relationshipChanges, report, context);
        ValidateReputationChanges(target.reputationChanges, report, context);
        ValidateTitleGrants(target.titleGrants, report, context);

        foreach(var milestone in target.milestonesToComplete) {
            if(milestone == null) {
                report.Warning("Report propagation target has a null milestone slot.", context);
            }
        }
    }

    static void ValidateCareerPointGrants(IEnumerable<CareerPointGrant> grants, ProjectValidationReport report, string context) {
        if(grants == null) {
            return;
        }

        foreach(var grant in grants) {
            if(grant == null) {
                report.Warning("Career point reward has a null grant slot.", context);
                continue;
            }

            if(grant.career == null && grant.points > 0) {
                report.Warning("Career point reward has points but no career path.", context);
            }

            if(grant.career != null && grant.points <= 0) {
                report.Warning($"Career point reward for '{grant.career.DisplayName}' has no points.", context);
            }
        }
    }

    static void ValidateLifestylePointGrants(IEnumerable<LifestylePointGrant> grants, ProjectValidationReport report, string context) {
        if(grants == null) {
            return;
        }

        foreach(var grant in grants) {
            if(grant == null) {
                report.Warning("Lifestyle point reward has a null grant slot.", context);
                continue;
            }

            if(grant.lifestyle == null && grant.points != 0) {
                report.Warning("Lifestyle point reward has points but no lifestyle profile.", context);
            }

            if(grant.lifestyle != null && grant.points == 0) {
                report.Info($"Lifestyle point reward for '{grant.lifestyle.DisplayName}' has 0 points.", context);
            }
        }
    }

    static void ValidateLifePathRewards(IEnumerable<LifePathReward> rewards, ProjectValidationReport report, string context) {
        if(rewards == null) {
            return;
        }

        foreach(var reward in rewards) {
            if(reward == null) {
                report.Warning("Life path reward has a null reward slot.", context);
                continue;
            }

            if(reward.lifePath == null && reward.HasAnyPayload) {
                report.Warning("Life path reward has payload but no life path.", context);
            }

            if(reward.lifePath != null && !reward.HasAnyPayload) {
                report.Info($"Life path reward for '{reward.lifePath.DisplayName}' has no XP, branch progress, tag progress or direct perk unlock.", context);
            }

            if(reward.lifePath != null && reward.lifePath.MaxExperience > 0 && reward.experience > reward.lifePath.MaxExperience) {
                report.Info($"Life path reward grants {reward.experience} XP, but '{reward.lifePath.DisplayName}' max XP is {reward.lifePath.MaxExperience}. Runtime will clamp saved XP.", context);
            }

            if(reward.branchProgress != null) {
                var duplicateBranches = reward.branchProgress
                    .Where(branch => branch != null && !string.IsNullOrWhiteSpace(branch.branchId))
                    .GroupBy(branch => branch.branchId, StringComparer.OrdinalIgnoreCase)
                    .Where(group => group.Count() > 1);
                foreach(var duplicate in duplicateBranches) {
                    report.Info($"Life path reward grants branch '{duplicate.Key}' more than once in the same reward.", context);
                }

                foreach(var branch in reward.branchProgress) {
                    if(branch == null) {
                        report.Warning("Life path reward has a null branch progress slot.", context);
                        continue;
                    }

                    if(string.IsNullOrWhiteSpace(branch.branchId) && branch.progress > 0) {
                        report.Warning("Life path reward has branch progress with no branch id.", context);
                    } else if(reward.lifePath != null && !string.IsNullOrWhiteSpace(branch.branchId) && !reward.lifePath.HasBranch(branch.branchId)) {
                        report.Warning($"Life path reward references branch '{branch.branchId}', but '{reward.lifePath.DisplayName}' does not define that branch.", context);
                    } else if(!string.IsNullOrWhiteSpace(branch.branchId) && branch.progress <= 0) {
                        report.Info($"Life path reward branch '{branch.branchId}' has 0 progress.", context);
                    }
                }
            }

            if(reward.tagProgress != null) {
                var duplicateTags = reward.tagProgress
                    .Where(tag => tag != null && !string.IsNullOrWhiteSpace(tag.tag))
                    .GroupBy(tag => tag.tag, StringComparer.OrdinalIgnoreCase)
                    .Where(group => group.Count() > 1);
                foreach(var duplicate in duplicateTags) {
                    report.Info($"Life path reward grants tag '{duplicate.Key}' more than once in the same reward.", context);
                }

                foreach(var tag in reward.tagProgress) {
                    if(tag == null) {
                        report.Warning("Life path reward has a null tag progress slot.", context);
                        continue;
                    }

                    if(string.IsNullOrWhiteSpace(tag.tag) && tag.count > 0) {
                        report.Warning("Life path reward has tag progress with no tag.", context);
                    } else if(!string.IsNullOrWhiteSpace(tag.tag) && tag.count <= 0) {
                        report.Info($"Life path reward tag '{tag.tag}' has 0 count.", context);
                    }
                }
            }

            if(reward.directPerkUnlocks != null) {
                var duplicatePerks = reward.directPerkUnlocks
                    .Where(perk => perk != null)
                    .GroupBy(perk => perk)
                    .Where(group => group.Count() > 1);
                foreach(var duplicate in duplicatePerks) {
                    report.Info($"Life path reward directly unlocks perk '{duplicate.Key.DisplayName}' more than once.", context);
                }

                foreach(var perk in reward.directPerkUnlocks) {
                    if(perk == null) {
                        report.Warning("Life path reward has a null direct perk unlock slot.", context);
                    } else if(perk.LifePath == null) {
                        report.Warning($"Life path reward directly unlocks perk '{perk.DisplayName}', but that perk has no owning life path.", context);
                    } else if(reward.lifePath != null && perk.LifePath != reward.lifePath) {
                        report.Warning($"Life path reward unlocks perk '{perk.DisplayName}', but that perk belongs to another life path.", context);
                    }
                }
            }
        }
    }

    static void ValidateResearchProgressRewards(IEnumerable<ResearchProgressReward> rewards, ProjectValidationReport report, string context) {
        if(rewards == null) {
            return;
        }

        foreach(var reward in rewards) {
            if(reward == null) {
                report.Warning("Research progress reward has a null reward slot.", context);
                continue;
            }

            if(reward.subject == null && reward.points > 0) {
                report.Warning("Research progress reward has points but no subject.", context);
            }

            if(reward.subject != null && reward.points <= 0) {
                report.Warning($"Research progress reward for '{reward.subject.DisplayName}' has no points.", context);
            }
        }
    }

    static void ValidateReputationChanges(IEnumerable<ReputationChange> changes, ProjectValidationReport report, string context) {
        if(changes == null) {
            return;
        }

        foreach(var change in changes) {
            if(change == null) {
                report.Warning("Reputation change has a null entry.", context);
                continue;
            }

            if(change.faction == null && change.amount != 0) {
                report.Warning("Reputation change has amount but no faction.", context);
            }
        }
    }

    static void ValidateRelationshipChanges(IEnumerable<RelationshipChange> changes, ProjectValidationReport report, string context) {
        if(changes == null) {
            return;
        }

        foreach(var change in changes) {
            if(change == null) {
                report.Warning("Relationship change has a null entry.", context);
                continue;
            }

            if(change.subject == null && change.amount != 0) {
                report.Warning("Relationship change has amount but no relationship subject.", context);
            }
        }
    }

    static void ValidateTitleGrants(IEnumerable<TitleGrant> grants, ProjectValidationReport report, string context) {
        if(grants == null) {
            return;
        }

        foreach(var grant in grants) {
            if(grant == null) {
                report.Warning("Title grant has a null entry.", context);
                continue;
            }

            if(grant.title == null) {
                report.Warning("Title grant has no title.", context);
            }
        }
    }

    static void ValidateOrganizationMembershipGrants(IEnumerable<OrganizationMembershipGrant> grants, ProjectValidationReport report, string context) {
        if(grants == null) {
            return;
        }

        foreach(var grant in grants) {
            if(grant == null) {
                report.Warning("Organization membership reward has a null grant slot.", context);
                continue;
            }

            if(grant.organization == null) {
                report.Warning("Organization membership reward has no organization.", context);
            }

            if(grant.organization != null && !grant.grantPermanently && grant.organization.CanBeTemporary && grant.durationHours <= 0 && grant.organization.DefaultDurationHours <= 0) {
                report.Warning($"Temporary membership reward for '{grant.organization.DisplayName}' has no duration and the organization has no default duration.", context);
            }
        }
    }

    static void ValidateOrganizationPointGrants(IEnumerable<OrganizationPointGrant> grants, ProjectValidationReport report, string context) {
        if(grants == null) {
            return;
        }

        foreach(var grant in grants) {
            if(grant == null) {
                report.Warning("Organization point reward has a null grant slot.", context);
                continue;
            }

            if(grant.organization == null && grant.points > 0) {
                report.Warning("Organization point reward has points but no organization.", context);
            }

            if(grant.organization != null && grant.points <= 0) {
                report.Warning($"Organization point reward for '{grant.organization.DisplayName}' has no points.", context);
            }
        }
    }

    static void ValidateNPCGeneration(ProjectValidationReport report) {
        foreach(var visualSet in ProjectValidatorAssetFinder.FindAssets<NPCVisualSetDefinition>()) {
            if(visualSet == null) continue;

            string context = $"NPCVisualSet/{visualSet.name}";
            if(string.IsNullOrWhiteSpace(visualSet.Id)) {
                report.Error("NPC visual set id is empty.", context);
            }

            if(visualSet.WalkDownSprites == null || visualSet.WalkDownSprites.Count == 0) {
                report.Warning("NPC visual set has no walk-down sprites.", context);
            }
        }

        foreach(var partyTemplate in ProjectValidatorAssetFinder.FindAssets<TrainerPartyTemplateDefinition>()) {
            if(partyTemplate == null) continue;

            string context = $"TrainerPartyTemplate/{partyTemplate.name}";
            if(string.IsNullOrWhiteSpace(partyTemplate.Id)) {
                report.Error("Trainer party template id is empty.", context);
            }

            if(partyTemplate.PartySlots == null || partyTemplate.PartySlots.Count == 0) {
                report.Warning("Trainer party template has no party slots.", context);
            }

            foreach(var slot in partyTemplate.PartySlots) {
                if(slot == null || slot.PokemonPool == null || slot.PokemonPool.Count == 0) {
                    report.Warning("Trainer party template has an empty Pokemon pool slot.", context);
                }
            }
        }

        foreach(var pool in ProjectValidatorAssetFinder.FindAssets<NPCVariantPoolDefinition>()) {
            if(pool == null) continue;

            string context = $"NPCVariantPool/{pool.name}";
            if(string.IsNullOrWhiteSpace(pool.Id)) {
                report.Error("NPC variant pool id is empty.", context);
            }

            if(pool.Variants == null || pool.Variants.Count == 0) {
                report.Warning("NPC variant pool has no variants.", context);
            }

            foreach(var variant in pool.Variants) {
                if(variant == null) {
                    report.Warning("NPC variant pool has a null variant slot.", context);
                    continue;
                }

                if(variant.Weight <= 0) {
                    report.Warning($"NPC variant '{variant.Id}' has zero weight.", context);
                }

                if(variant.VisualSet == null) {
                    report.Warning($"NPC variant '{variant.Id}' has no visual set.", context);
                }
            }
        }

        foreach(var profile in ProjectValidatorAssetFinder.FindAssets<NPCSceneRandomizationProfileDefinition>()) {
            if(profile == null) continue;

            string context = $"NPCSceneRandomizationProfile/{profile.name}";
            if(string.IsNullOrWhiteSpace(profile.Id)) {
                report.Error("NPC scene randomization profile id is empty.", context);
            }

            if(profile.Rules == null || profile.Rules.Count == 0) {
                report.Warning("NPC scene randomization profile has no rules.", context);
            }

            foreach(var rule in profile.Rules) {
                if(rule == null) {
                    report.Warning("NPC scene randomization profile has a null rule slot.", context);
                    continue;
                }

                if(rule.Weight <= 0 && rule.Enabled) {
                    report.Warning($"NPC scene randomization rule '{rule.RuleId}' is enabled but has zero weight.", context);
                }

                if(rule.VariantPool == null) {
                    report.Info($"NPC scene randomization rule '{rule.RuleId}' has no pool and will only work with slot pool overrides.", context);
                }

                ValidateObjectList(rule.Requirements, report, context, "NPC scene randomization rule has a null requirement slot.");
            }

            ValidateObjectList(profile.Requirements, report, context, "NPC scene randomization profile has a null requirement slot.");
        }

        foreach(var slot in ProjectValidatorAssetFinder.FindAssets<NPCSceneRandomizationSlot>()) {
            if(slot == null) continue;

            string context = $"NPCSceneRandomizationSlot/{slot.name}";
            if(slot.RandomizationEnabled && !slot.HasNpcController && !slot.HasTrainerController) {
                report.Warning("NPC randomization slot has no NPCController or TrainerController to receive generated data.", context);
            }

            if(slot.FixedSpecialNpc && slot.RandomizationEnabled) {
                report.Info("NPC randomization slot is enabled but marked fixed/special. Profiles will skip it unless they allow fixed slots.", context);
            }
        }

        foreach(var controller in ProjectValidatorAssetFinder.FindAssets<NPCSceneRandomizationController>()) {
            if(controller == null) continue;

            string context = $"NPCSceneRandomizationController/{controller.name}";
            if(controller.Profile == null) {
                report.Warning("NPC scene randomization controller has no profile assigned.", context);
            }

            if(!controller.RandomizeOnStart && !controller.RandomizeOnEnable) {
                report.Info("NPC scene randomization controller has no automatic signal enabled. It must be triggered manually or by another script.", context);
            }

            if(controller.IncludeChildSlots == false && controller.SearchWholeSceneWhenNoRoot == false && (controller.ManualSlots == null || controller.ManualSlots.Count == 0)) {
                report.Warning("NPC scene randomization controller has no way to discover slots.", context);
            }

            ValidateObjectList(controller.ManualSlots, report, context, "NPC scene randomization controller has a null manual slot.");
        }
    }

    static void ValidateDialogGraphs(ProjectValidationReport report) {
        foreach(var graph in ProjectValidatorAssetFinder.FindAssets<DialogGraphDefinition>()) {
            if(graph == null) continue;

            string context = $"DialogGraph/{graph.name}";
            if(string.IsNullOrWhiteSpace(graph.Id)) {
                report.Error("Dialog graph id is empty.", context);
            }

            if(graph.Nodes == null || graph.Nodes.Count == 0) {
                report.Warning("Dialog graph has no nodes.", context);
                continue;
            }

            var duplicateNodes = graph.Nodes
                .Where(node => node != null)
                .GroupBy(node => node.Id)
                .Where(group => !string.IsNullOrWhiteSpace(group.Key) && group.Count() > 1);
            foreach(var duplicate in duplicateNodes) {
                report.Warning($"Dialog graph has duplicate node id '{duplicate.Key}'.", context);
            }

            foreach(var node in graph.Nodes) {
                if(node == null) {
                    report.Warning("Dialog graph has a null node slot.", context);
                    continue;
                }

                string nodeContext = $"{context}/Node/{node.Id}";
                ValidateDialogGraphEffects(node.OnEnterEffects, report, $"{nodeContext}/OnEnter");
                ValidateDialogGraphEffects(node.OnExitEffects, report, $"{nodeContext}/OnExit");

                foreach(var choice in node.Choices) {
                    if(choice == null) {
                        report.Warning("Dialog node has a null choice slot.", nodeContext);
                        continue;
                    }

                    ValidateDialogGraphEffects(choice.Effects, report, $"{nodeContext}/Choice/{choice.Id}");
                    if(!choice.StayOnSameNode && !string.IsNullOrWhiteSpace(choice.NextNodeId) && !graph.HasNode(choice.NextNodeId)) {
                        report.Warning($"Dialog choice routes to missing node '{choice.NextNodeId}'.", nodeContext);
                    }
                }

                if(!string.IsNullOrWhiteSpace(node.AutoNextNodeId) && !graph.HasNode(node.AutoNextNodeId)) {
                    report.Warning($"Dialog node auto-routes to missing node '{node.AutoNextNodeId}'.", nodeContext);
                }
            }
        }
    }

    static void ValidateDialogGraphEffects(DialogGraphEffects effects, ProjectValidationReport report, string context) {
        if(effects == null) {
            return;
        }

        ValidateReputationChanges(effects.ReputationChanges, report, context);
        ValidateRelationshipChanges(effects.RelationshipChanges, report, context);
        ValidateObjectList(effects.MilestonesToComplete, report, context, "Dialog graph effect has a null milestone slot.");
        ValidateTitleGrants(effects.TitleGrants, report, context);
        ValidateObjectList(effects.RecipeGrants.Select(grant => grant != null ? grant.recipe : null), report, context, "Dialog graph effect has a null recipe grant slot.");
        ValidateLifePathRewards(effects.LifePathRewards, report, context);
    }

    static void ValidateCompanions(ProjectValidationReport report) {
        foreach(var role in ProjectValidatorAssetFinder.FindAssets<CompanionRoleDefinition>()) {
            if(role == null) continue;

            string context = $"CompanionRole/{role.name}";
            if(string.IsNullOrWhiteSpace(role.Id)) {
                report.Error("Companion role id is empty.", context);
            }

            foreach(var perk in role.Perks) {
                if(perk == null) {
                    report.Warning("Companion role has a null perk slot.", context);
                }
            }
        }

        foreach(var perk in ProjectValidatorAssetFinder.FindAssets<CompanionPerkDefinition>()) {
            if(perk == null) continue;

            string context = $"CompanionPerk/{perk.name}";
            if(string.IsNullOrWhiteSpace(perk.Id)) {
                report.Error("Companion perk id is empty.", context);
            }

            if(perk.Tags != null && perk.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Companion perk has an empty tag slot.", context);
            }

            if(!perk.AffectsAllActivities
                && (perk.AffectedActivities == null || perk.AffectedActivities.Count == 0)
                && (perk.AffectedActivityTags == null || perk.AffectedActivityTags.Count == 0)) {
                report.Warning("Companion perk does not affect any activity because no activity or tag targets are configured.", context);
            }

            bool hasAnyEffect = !Mathf.Approximately(perk.ExperienceMultiplier, 1f)
                || perk.FlatExperienceBonus != 0
                || perk.YieldBonus != 0
                || perk.ResearchPointBonus != 0
                || perk.PokemonCareBonus != 0
                || perk.StaminaSupportBonus != 0
                || perk.SurvivalSupportBonus != 0
                || perk.BondGainBonus != 0
                || !Mathf.Approximately(perk.ItemCostMultiplier, 1f)
                || !Mathf.Approximately(perk.ToolDurabilityCostMultiplier, 1f)
                || !Mathf.Approximately(perk.NeedCostMultiplier, 1f);

            if(!hasAnyEffect) {
                report.Info("Companion perk has no configured effect yet.", context);
            }
        }

        foreach(var expedition in ProjectValidatorAssetFinder.FindAssets<CompanionExpeditionDefinition>()) {
            if(expedition == null) continue;

            string context = $"CompanionExpedition/{expedition.name}";
            if(string.IsNullOrWhiteSpace(expedition.Id)) {
                report.Error("Companion expedition id is empty.", context);
            }

            if(expedition.Tags != null && expedition.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Companion expedition has an empty tag slot.", context);
            }

            if(expedition.DurationHours <= 0) {
                report.Info("Companion expedition has zero duration and can be claimed immediately.", context);
            }

            if(expedition.SuccessOutcomes.Count == 0 && expedition.FailureOutcomes.Count == 0 && expedition.ClaimActivity == null) {
                report.Info("Companion expedition has no claim activity or outcomes. It can still track history, but claiming gives no reward.", context);
            }

            foreach(var requirement in expedition.Requirements) {
                if(requirement == null) {
                    report.Warning("Companion expedition has a null requirement slot.", context);
                }
            }

            foreach(var modifier in expedition.SuccessModifiers) {
                if(modifier == null) {
                    report.Warning("Companion expedition has a null success modifier slot.", context);
                    continue;
                }

                if(modifier.role == null && modifier.perk == null && modifier.minimumBondLevel == CompanionBondLevel.Stranger && modifier.minimumBondPoints <= 0) {
                    report.Info("Companion expedition success modifier has no condition and always applies.", context);
                }

                if(Mathf.Approximately(modifier.chanceBonus, 0f)) {
                    report.Warning("Companion expedition success modifier has no chance bonus.", context);
                }
            }
        }

        foreach(var route in ProjectValidatorAssetFinder.FindAssets<CompanionExpeditionRouteDefinition>()) {
            if(route == null) continue;

            string context = $"CompanionExpeditionRoute/{route.name}";
            if(string.IsNullOrWhiteSpace(route.Id)) {
                report.Error("Companion expedition route id is empty.", context);
            }

            if(route.Tags != null && route.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Companion expedition route has an empty tag slot.", context);
            }

            if(route.Stages == null || route.Stages.Count == 0) {
                report.Warning("Companion expedition route has no stages.", context);
            }

            foreach(var requirement in route.Requirements) {
                if(requirement == null) {
                    report.Warning("Companion expedition route has a null requirement slot.", context);
                }
            }

            for(int i = 0; i < route.Stages.Count; i++) {
                var stage = route.Stages[i];
                if(stage == null) {
                    report.Warning($"Companion expedition route stage {i} is null.", context);
                    continue;
                }

                if(stage.Expedition == null) {
                    report.Warning($"Companion expedition route stage {i} has no expedition.", context);
                }

                foreach(var requirement in stage.Requirements) {
                    if(requirement == null) {
                        report.Warning($"Companion expedition route stage {i} has a null requirement slot.", context);
                    }
                }
            }

            var duplicateStageIds = route.Stages
                .Where(stage => stage != null && !string.IsNullOrWhiteSpace(stage.stageId))
                .GroupBy(stage => stage.stageId)
                .Where(group => group.Count() > 1);
            foreach(var duplicate in duplicateStageIds) {
                report.Warning($"Companion expedition route has duplicate stage id '{duplicate.Key}'. Generated source ids may collide.", context);
            }

            if(route.RouteSuccessOutcomes.Count == 0 && route.RouteFailureOutcomes.Count == 0) {
                report.Info("Companion expedition route has no route-level outcomes. Stage expeditions may still grant rewards.", context);
            }
        }
    }

    static void ValidateCompanionNodeFollow(ProjectValidationReport report) {
        foreach(var profile in ProjectValidatorAssetFinder.FindAssets<CompanionNodeFollowProfile>()) {
            if(profile == null) continue;

            string context = $"CompanionNodeFollowProfile/{profile.name}";
            if(string.IsNullOrWhiteSpace(profile.ProfileId)) {
                report.Error("Companion node follow profile id is empty.", context);
            }

            if(profile.ExpectedCapabilities == OverworldMovementCapabilityFlags.None) {
                report.Info("Companion node follow profile has no expected movement capabilities. This may be valid for ghost/custom followers.", context);
            }

            if((profile.RequiredTargetFlags & profile.BlockedTargetFlags) != 0) {
                report.Warning("Companion node follow profile requires and blocks the same target flag.", context);
            }

            if(!profile.AllowCurrentPlayerNode && profile.TrailOffset <= 0) {
                report.Info("Companion node follow profile has trail offset 0 but current player node is disabled. It will look for older trail nodes.", context);
            }

            if(profile.CatchUpDistance <= 0f && profile.CatchUpMode != CompanionNodeCatchUpMode.None) {
                report.Info("Companion node follow profile has a catch-up mode but catch-up distance is 0.", context);
            }
        }

        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<CompanionNodeFollowProfile>(), report, "companion node follow profile", profile => profile.ProfileId);

        foreach(var tracker in ProjectValidatorAssetFinder.FindAssets<PlayerNodeTrailTracker>()) {
            if(tracker == null) continue;

            string context = $"PlayerNodeTrailTracker/{tracker.name}";
            if(tracker.NodeGroup == null) {
                report.Warning("Player node trail tracker has no node group or movement adapter node group. Companion node-follow cannot resolve player trail nodes.", context);
            }

            if(tracker.MaxTrailNodes <= 1) {
                report.Info("Player node trail tracker keeps one or fewer nodes, so followers cannot trail behind the player.", context);
            }
        }

        foreach(var controller in ProjectValidatorAssetFinder.FindAssets<CompanionNodeFollowController>()) {
            if(controller == null) continue;

            string context = $"CompanionNodeFollowController/{controller.name}";
            if(controller.Profile == null) {
                report.Warning("Companion node follow controller has no profile.", context);
            }

            if(controller.PathAgent == null) {
                report.Warning("Companion node follow controller has no path agent.", context);
            }

            if(controller.NodeGroup == null) {
                report.Warning("Companion node follow controller has no node group. It cannot resolve start/target nodes.", context);
            }

            if(controller.Profile != null && controller.PathAgent != null && controller.Profile.ExpectedCapabilities != OverworldMovementCapabilityFlags.None
                && !controller.PathAgent.HasAllCapabilities(controller.Profile.ExpectedCapabilities)) {
                report.Info("Companion node follow controller profile expects movement capabilities that the path agent does not currently have.", context);
            }
        }
    }

    static void ValidateJobs(ProjectValidationReport report) {
        foreach(var job in ProjectValidatorAssetFinder.FindAssets<JobDefinition>()) {
            if(job == null) continue;

            string context = $"Job/{job.name}";
            if(string.IsNullOrWhiteSpace(job.Id)) {
                report.Error("Job id is empty.", context);
            }

            if(job.Objectives == null || job.Objectives.Count == 0) {
                report.Warning("Job has no objectives.", context);
            }

            foreach(var objective in job.Objectives) {
                if(objective == null) {
                    report.Warning("Job has a null objective slot.", context);
                    continue;
                }

                if(objective.requiredCount <= 0) {
                    report.Warning("Job objective has no required count.", context);
                }

                if(objective.type == JobObjectiveType.HaveItem && objective.item == null) {
                    report.Warning("Have Item objective has no item.", context);
                }

                if(objective.type == JobObjectiveType.CompleteActivity && objective.activity == null) {
                    report.Warning("Complete Activity objective has no activity.", context);
                }

                if((objective.type == JobObjectiveType.EncounterSeen
                    || objective.type == JobObjectiveType.EncounterBattleStarted
                    || objective.type == JobObjectiveType.EncounterCaptured
                    || objective.type == JobObjectiveType.EncounterStealthCaptured)
                    && objective.pokemon == null) {
                    report.Warning("Encounter objective has no Pokemon.", context);
                }

                if(objective.type == JobObjectiveType.KnowRecipe && objective.recipe == null) {
                    report.Warning("Know Recipe objective has no recipe.", context);
                }

                if(objective.type == JobObjectiveType.HasTitle && objective.title == null) {
                    report.Warning("Has Title objective has no title.", context);
                }

                if(objective.type == JobObjectiveType.ReputationAtLeast && objective.faction == null) {
                    report.Warning("Reputation objective has no faction.", context);
                }

                if((objective.type == JobObjectiveType.TransitRouteUnlocked
                    || objective.type == JobObjectiveType.TransitRouteTravelCount)
                    && objective.transitRoute == null) {
                    report.Warning("Transit route objective has no route.", context);
                }

                if(objective.type == JobObjectiveType.TransitStopUnlocked && objective.transitStop == null) {
                    report.Warning("Transit stop objective has no stop.", context);
                }

                if(objective.type == JobObjectiveType.TransitRouteTagTravelCount && string.IsNullOrWhiteSpace(objective.transitRouteTag)) {
                    report.Warning("Transit route tag objective has no tag.", context);
                }
            }

            ValidateCareerPointGrants(job.CareerPointRewards, report, context);
            ValidateLifePathRewards(job.LifePathRewards, report, context);
            ValidateOrganizationMembershipGrants(job.OrganizationMembershipRewards, report, context);
            ValidateOrganizationPointGrants(job.OrganizationPointRewards, report, context);
        }
    }

    static void ValidateJobBoards(ProjectValidationReport report) {
        foreach(var board in ProjectValidatorAssetFinder.FindAssets<JobBoardDefinition>()) {
            if(board == null) continue;

            string context = $"JobBoard/{board.name}";
            if(string.IsNullOrWhiteSpace(board.Id)) {
                report.Error("Job board id is empty.", context);
            }

            if(board.Offers == null || board.Offers.Count == 0) {
                report.Warning("Job board has no offers.", context);
            }

            foreach(var offer in board.Offers) {
                if(offer == null) {
                    report.Warning("Job board has a null offer slot.", context);
                    continue;
                }

                if(offer.Job == null) {
                    report.Warning("Job board offer has no job.", context);
                }
            }
        }
    }

    static void ValidateTransitRoutes(ProjectValidationReport report) {
        foreach(var route in ProjectValidatorAssetFinder.FindAssets<TransitRouteDefinition>()) {
            if(route == null) continue;

            string context = $"TransitRoute/{route.name}";
            if(string.IsNullOrWhiteSpace(route.Id)) {
                report.Error("Transit route id is empty.", context);
            }

            if(route.RequiresRouteUnlock && !route.UnlockedByDefault) {
                bool hasUnlocker = ProjectValidatorAssetFinder.FindAssets<TransitStopDefinition>().Any(stop => stop != null && stop.Routes != null && stop.Routes.Contains(route));
                if(!hasUnlocker) {
                    report.Info("Route requires an unlock. Make sure a title, job, event or script unlocks it at runtime.", context);
                }
            }

            foreach(var cost in route.ItemCosts) {
                if(cost != null && cost.item == null && cost.count > 0) {
                    report.Warning("Transit route item cost has count but no item.", context);
                }
            }

            foreach(var cost in route.NeedCosts) {
                if(cost != null && cost.need == null && cost.amount > 0) {
                    report.Warning("Transit route need cost has amount but no need.", context);
                }
            }

            if(!string.IsNullOrWhiteSpace(route.DestinationSceneName) && string.IsNullOrWhiteSpace(route.DestinationPortalId)) {
                report.Info("Transit route has a destination scene but no portal/spawn id. This is fine if future UI resolves the arrival another way.", context);
            }
        }
    }

    static void ValidateTransitStops(ProjectValidationReport report) {
        foreach(var stop in ProjectValidatorAssetFinder.FindAssets<TransitStopDefinition>()) {
            if(stop == null) continue;

            string context = $"TransitStop/{stop.name}";
            if(string.IsNullOrWhiteSpace(stop.Id)) {
                report.Error("Transit stop id is empty.", context);
            }

            if(stop.Routes == null || stop.Routes.Count == 0) {
                report.Warning("Transit stop has no routes.", context);
            }

            foreach(var route in stop.Routes) {
                if(route == null) {
                    report.Warning("Transit stop has a null route slot.", context);
                    continue;
                }

                if(!route.CanDepartFrom(stop.Id)) {
                    report.Warning($"Route '{route.Id}' origin stop does not match this stop id.", context);
                }
            }
        }
    }

    static void ValidateTransitJourneys(ProjectValidationReport report) {
        foreach(var journey in ProjectValidatorAssetFinder.FindAssets<TransitJourneyDefinition>()) {
            if(journey == null) continue;

            string context = $"TransitJourney/{journey.name}";
            if(string.IsNullOrWhiteSpace(journey.Id)) {
                report.Error("Transit journey id is empty.", context);
            }

            if(journey.Legs == null || journey.Legs.Count == 0) {
                report.Warning("Transit journey has no legs.", context);
                continue;
            }

            string expectedOrigin = string.Empty;
            for(int i = 0; i < journey.Legs.Count; i++) {
                var leg = journey.Legs[i];
                if(leg == null) {
                    report.Warning($"Transit journey leg {i} is null.", context);
                    continue;
                }

                if(leg.Route == null) {
                    report.Warning($"Transit journey leg {i} has no route.", context);
                    continue;
                }

                if(i > 0 && !string.IsNullOrWhiteSpace(expectedOrigin) && !leg.CanDepartFrom(expectedOrigin)) {
                    report.Warning($"Transit journey leg {i} does not depart from the previous leg destination '{expectedOrigin}'.", context);
                }

                if(leg.StopRule == TransitJourneyStopRule.PassThrough && leg.DwellHours > 0) {
                    report.Info($"Transit journey leg {i} is pass-through but has dwell hours. The player cannot disembark there unless the stop rule changes.", context);
                }

                if(leg.StopRule == TransitJourneyStopRule.RequiredDisembark && i < journey.Legs.Count - 1) {
                    report.Info($"Transit journey leg {i} requires disembark before later legs. Later legs will only be usable if another system starts/continues them.", context);
                }

                expectedOrigin = leg.DestinationStopId;
            }

            if(journey.UseVehicleInterior && string.IsNullOrWhiteSpace(journey.VehicleInteriorSceneName)) {
                report.Warning("Transit journey uses a vehicle interior but has no vehicle interior scene name.", context);
            }

            if(journey.IncidentHooks != null) {
                for(int i = 0; i < journey.IncidentHooks.Count; i++) {
                    var hook = journey.IncidentHooks[i];
                    if(hook == null) {
                        report.Warning($"Transit journey incident hook {i} is null.", context);
                        continue;
                    }

                    if(!hook.Enabled) {
                        continue;
                    }

                    if(hook.Incident == null && hook.Board == null) {
                        report.Warning($"Transit journey incident hook {i} is enabled but has no incident or board.", context);
                    }

                    if(hook.Chance <= 0f) {
                        report.Info($"Transit journey incident hook {i} has 0 chance and will never run.", context);
                    }

                    if(hook.RequireLegIndex && hook.LegIndex >= journey.Legs.Count) {
                        report.Warning($"Transit journey incident hook {i} requires leg index {hook.LegIndex}, but the journey has only {journey.Legs.Count} leg(s).", context);
                    }
                }
            }
        }
    }

    static void ValidateTransitJourneySources(ProjectValidationReport report) {
        foreach(var source in ProjectValidatorAssetFinder.FindAssets<TransitJourneySource>()) {
            if(source == null) continue;

            string context = $"TransitJourneySource/{source.name}";
            if(source.Journey == null) {
                report.Warning("Transit journey source has no journey assigned.", context);
            }

            if(source.Station == null && string.IsNullOrWhiteSpace(source.OriginStopId)) {
                report.Info("Transit journey source has no station or origin override. It will fall back to the first leg origin or GameObject name.", context);
            }

            if(source.StartOnTrigger && !source.CreateMissingJourneyLog) {
                report.Info("Transit journey source starts on trigger but will not create a missing PlayerTransitJourneyLog.", context);
            }
        }
    }

    static void ValidateTransitRegionHandoffs(ProjectValidationReport report) {
        foreach(var handoff in ProjectValidatorAssetFinder.FindAssets<TransitRegionHandoffDefinition>()) {
            if(handoff == null) continue;

            string context = $"TransitRegionHandoff/{handoff.name}";
            if(string.IsNullOrWhiteSpace(handoff.Id)) {
                report.Error("Transit-region handoff id is empty.", context);
            }

            if(handoff.Entries == null || handoff.Entries.Count == 0) {
                report.Warning("Transit-region handoff has no entries.", context);
                continue;
            }

            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for(int i = 0; i < handoff.Entries.Count; i++) {
                var entry = handoff.Entries[i];
                if(entry == null) {
                    report.Warning($"Transit-region handoff entry {i} is null.", context);
                    continue;
                }

                if(string.IsNullOrWhiteSpace(entry.EntryId)) {
                    report.Warning($"Transit-region handoff entry {i} has no entry id and no regional route fallback.", context);
                } else if(!ids.Add(entry.EntryId)) {
                    report.Warning($"Transit-region handoff entry id '{entry.EntryId}' is duplicated inside this handoff.", context);
                }

                if(entry.RegionRoute == null) {
                    report.Warning($"Transit-region handoff entry '{entry.EntryId}' has no regional route.", context);
                }

                if(entry.Trigger == TransitRegionHandoffTrigger.JourneyCompleted && entry.RequireActiveJourney) {
                    report.Info($"Transit-region handoff entry '{entry.EntryId}' uses JourneyCompleted while requiring an active journey. PlayerTransitJourneyLog may clear completed journeys before this can run.", context);
                }

                if(entry.PokemonSelection == TransitRegionHandoffPokemonSelection.PartySlot && (entry.PartySlot < 0 || entry.PartySlot > 5)) {
                    report.Warning($"Transit-region handoff entry '{entry.EntryId}' has an invalid party slot.", context);
                }
            }
        }

        foreach(var source in ProjectValidatorAssetFinder.FindAssets<TransitRegionHandoffSource>()) {
            if(source == null) continue;

            string context = $"TransitRegionHandoffSource/{source.name}";
            if(source.Handoff == null) {
                report.Warning("Transit-region handoff source has no handoff definition assigned.", context);
            }

            if(!source.RunFirstAvailableOnInteract && !source.RunFirstAvailableOnTrigger) {
                report.Info("Transit-region handoff source does not auto-run on interact or trigger. This is fine if UI calls RunEntry manually.", context);
            }
        }
    }

    static void ValidateFarmables(ProjectValidationReport report) {
        foreach(var farmable in ProjectValidatorAssetFinder.FindAssets<FarmableDefinition>()) {
            if(farmable == null) continue;

            string context = $"Farmable/{farmable.name}";
            if(farmable.Activity == null) {
                report.Warning("Farmable has no activity definition.", context);
            }

            if(farmable.Yields == null || farmable.Yields.Count == 0) {
                report.Warning("Farmable has no yields.", context);
            }

            foreach(var farmYield in farmable.Yields) {
                if(farmYield == null || farmYield.item == null) {
                    report.Warning("Farmable has a yield without item.", context);
                }
            }
        }
    }

    static void ValidateResources(ProjectValidationReport report) {
        foreach(var resource in ProjectValidatorAssetFinder.FindAssets<ResourceNodeDefinition>()) {
            if(resource == null) continue;

            string context = $"Resource/{resource.name}";
            if(resource.Activity == null) {
                report.Warning("Resource has no activity definition.", context);
            }

            if(resource.Yields == null || resource.Yields.Count == 0) {
                report.Warning("Resource has no yields.", context);
            }

            foreach(var resourceYield in resource.Yields) {
                if(resourceYield == null || resourceYield.item == null) {
                    report.Warning("Resource has a yield without item.", context);
                }
            }
        }
    }

    static void ValidateResearchSubjects(ProjectValidationReport report) {
        foreach(var subject in ProjectValidatorAssetFinder.FindAssets<ResearchSubjectDefinition>()) {
            if(subject == null) continue;

            string context = $"Research/{subject.name}";
            if(subject.Activity == null) {
                report.Warning("Research subject has no activity definition.", context);
            }
        }
    }

    static void ValidateSurvivalNeeds(ProjectValidationReport report) {
        foreach(var need in ProjectValidatorAssetFinder.FindAssets<SurvivalNeedDefinition>()) {
            if(need == null) continue;

            string context = $"SurvivalNeed/{need.name}";
            if(string.IsNullOrWhiteSpace(need.Id)) {
                report.Error("Survival need id is empty.", context);
            }

            if(need.HourlyDecay == 0 && need.HourlyRestGain == 0 && need.HourlySleepGain == 0) {
                report.Info("Survival need has no passive hourly decay, rest gain or sleep gain. It will only change from direct calls.", context);
            }

            if(need.CriticalThreshold > need.LowThreshold) {
                report.Warning("Survival need critical threshold is higher than low threshold after clamping.", context);
            }
        }

        foreach(var controller in ProjectValidatorAssetFinder.FindAssets<SurvivalNeedsController>()) {
            if(controller == null) continue;

            string context = $"SurvivalNeedsController/{controller.name}";
            ValidateDefinitionList(controller.NeedDefinitions, report, context, "survival need");
        }

        foreach(var manager in ProjectValidatorAssetFinder.FindAssets<SurvivalNeedsUIManager>()) {
            if(manager == null) continue;

            string context = $"SurvivalNeedsUIManager/{manager.name}";
            if(manager.Controller == null) {
                report.Info("Survival needs UI manager has no explicit controller. It will resolve the player controller at runtime.", context);
            }

            if(manager.MaxRecentRows == 1) {
                report.Info("Survival needs UI manager only exposes one recent change row. This is valid for compact HUDs, but narrow for debug/history panels.", context);
            }
        }
    }

    static void ValidateCareActions(ProjectValidationReport report) {
        foreach(var careAction in ProjectValidatorAssetFinder.FindAssets<PokemonCareActionDefinition>()) {
            if(careAction == null) continue;

            string context = $"PokemonCare/{careAction.name}";
            if(string.IsNullOrWhiteSpace(careAction.Id)) {
                report.Error("Care action id is empty.", context);
            }

            if(careAction.Activity == null) {
                report.Warning("Care action has no activity definition.", context);
            }

            if(careAction.Tags != null && careAction.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Care action has an empty tag slot.", context);
            }

            foreach(var moodChange in careAction.MoodChanges) {
                if(moodChange == null || moodChange.mood == null) {
                    report.Warning("Care action has a mood change without a mood.", context);
                }
            }

            foreach(var needRequirement in careAction.CareNeedRequirements) {
                if(needRequirement == null || needRequirement.need == null) {
                    report.Warning("Care action has a care need requirement without a need.", context);
                }
            }

            foreach(var needChange in careAction.CareNeedChanges) {
                if(needChange == null || needChange.need == null) {
                    report.Warning("Care action has a care need change without a need.", context);
                }
            }

            foreach(var evReward in careAction.EffortValueRewards) {
                if(evReward != null && evReward.amount <= 0) {
                    report.Warning("Care action has an EV reward with no amount.", context);
                }
            }

            if(careAction.GrowthTrainingRewards != null && careAction.GrowthTrainingRewards.Count > 0) {
                if(careAction.GrowthProfile == null && careAction.GrowthTrainingRewards.Any(reward => reward != null && !reward.requireInitializedGrowth)) {
                    report.Info("Care action grants growth training without a growth profile. This works best when the Pokemon already has saved training stat-bonus rules.", context);
                }

                foreach(var reward in careAction.GrowthTrainingRewards) {
                    if(reward == null) {
                        report.Warning("Care action has a null growth training reward slot.", context);
                        continue;
                    }

                    if(reward.points <= 0) {
                        report.Warning($"Care action growth training reward for {reward.stat} grants no points.", context);
                    }
                }
            }
        }

        foreach(var manager in ProjectValidatorAssetFinder.FindAssets<PokemonPartyCareStatusUIManager>()) {
            if(manager == null) continue;

            string context = $"PokemonPartyCareStatusUIManager/{manager.name}";
            if(manager.Party == null) {
                report.Info("Pokemon party care status UI manager has no explicit party. It will resolve the player party at runtime.", context);
            }

            if(manager.CareNeedsController == null) {
                report.Info("Pokemon party care status UI manager has no explicit care needs controller. Care need rows require runtime resolution from the party/player object.", context);
            }

            if(manager.MaxRecentRows == 1) {
                report.Info("Pokemon party care status UI manager only exposes one recent care change row. This is valid for compact HUDs, but narrow for debug/history panels.", context);
            }
        }

        foreach(var need in ProjectValidatorAssetFinder.FindAssets<PokemonCareNeedDefinition>()) {
            if(need == null) continue;

            string context = $"PokemonCareNeed/{need.name}";
            if(string.IsNullOrWhiteSpace(need.Id)) {
                report.Error("Care need id is empty.", context);
            }

            if(need.HourlyActiveChange == 0 && need.HourlyRestChange == 0 && need.HourlySleepChange == 0) {
                report.Info("Care need has no passive hourly changes. It will only change from care actions/facilities.", context);
            }

            if(need.DefaultValue <= need.CriticalThreshold) {
                report.Info("Care need default value starts at or below the critical threshold.", context);
            }
        }
    }

    static void ValidateCareFacilities(ProjectValidationReport report) {
        foreach(var facility in ProjectValidatorAssetFinder.FindAssets<PokemonCareFacilityDefinition>()) {
            if(facility == null) continue;

            string context = $"PokemonCareFacility/{facility.name}";
            if(string.IsNullOrWhiteSpace(facility.Id)) {
                report.Error("Care facility id is empty.", context);
            }

            if(facility.Tags != null && facility.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Care facility has an empty tag slot.", context);
            }

            if(facility.CareRules == null || facility.CareRules.Count == 0) {
                report.Info("Care facility has no care rules. It can still reserve slots, but it will not apply care actions.", context);
                continue;
            }

            foreach(var rule in facility.CareRules) {
                if(rule == null) {
                    report.Warning("Care facility has a null care rule slot.", context);
                    continue;
                }

                if(rule.careAction == null) {
                    report.Warning("Care facility rule has no care action.", context);
                }

                if(!rule.applyOnAdmission && !rule.applyOnTimedTick && !rule.applyOnRelease) {
                    report.Warning("Care facility rule is never applied because all timing toggles are disabled.", context);
                }

                if(rule.maxUsesPerStay > 0 && rule.intervalHours <= 0 && facility.DefaultCareIntervalHours <= 0 && rule.applyOnTimedTick) {
                    report.Info("Care facility timed rule has no interval. It may consume all max uses whenever processing runs.", context);
                }
            }
        }
    }

    static void ValidatePokemonAssignments(ProjectValidationReport report) {
        foreach(var assignment in ProjectValidatorAssetFinder.FindAssets<PokemonAssignmentDefinition>()) {
            if(assignment == null) continue;

            string context = $"PokemonAssignment/{assignment.name}";
            if(string.IsNullOrWhiteSpace(assignment.Id)) {
                report.Error("Pokemon assignment id is empty.", context);
            }

            if(assignment.Tags != null && assignment.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Pokemon assignment has an empty tag slot.", context);
            }

            if(assignment.DurationHours <= 0) {
                report.Info("Pokemon assignment has zero duration and can be claimed immediately.", context);
            }

            if(assignment.RepeatMode == PokemonAssignmentRepeatMode.CooldownHours && assignment.CooldownHours <= 0) {
                report.Info("Pokemon assignment uses Cooldown Hours repeat mode with 0 cooldown.", context);
            }

            if(assignment.RequiresActivityZone
                && assignment.AllowedZones.Count == 0
                && assignment.AllowedZoneTypes.Count == 0
                && assignment.AllowedZoneTags.Count == 0) {
                report.Info("Pokemon assignment requires an activity zone but has no zone filters. Any active zone will be accepted.", context);
            }

            if(assignment.AllowedTypes.Count > 0 && assignment.BannedTypes.Any(type => assignment.AllowedTypes.Contains(type))) {
                report.Warning("Pokemon assignment has the same Pokemon type in both allowed and banned type lists.", context);
            }

            ValidateObjectList(assignment.AllowedZones, report, context, "Pokemon assignment has a null allowed zone slot.");
            ValidateObjectList(assignment.Requirements, report, context, "Pokemon assignment has a null requirement slot.");
            ValidateObjectList(assignment.SuccessOutcomes, report, context, "Pokemon assignment has a null success outcome slot.");
            ValidateObjectList(assignment.FailureOutcomes, report, context, "Pokemon assignment has a null failure outcome slot.");
            ValidatePokemonMoodChanges(assignment.StartMoodChanges, report, context, "start");
            ValidatePokemonMoodChanges(assignment.SuccessMoodChanges, report, context, "success");
            ValidatePokemonMoodChanges(assignment.FailureMoodChanges, report, context, "failure");
            ValidateLifePathRewards(assignment.SuccessLifePathRewards, report, context);
            ValidateLifePathRewards(assignment.FailureLifePathRewards, report, context);
            ValidateCareerPointGrants(assignment.SuccessCareerPointRewards, report, context);
            ValidateCareerPointGrants(assignment.FailureCareerPointRewards, report, context);
            ValidateObjectList(assignment.SuccessConsequenceChains, report, context, "Pokemon assignment has a null success consequence chain slot.");
            ValidateObjectList(assignment.FailureConsequenceChains, report, context, "Pokemon assignment has a null failure consequence chain slot.");

            if(assignment.SuccessOutcomes.Count == 0
                && assignment.SuccessLifePathRewards.Count == 0
                && assignment.SuccessCareerPointRewards.Count == 0
                && assignment.SuccessConsequenceChains.Count == 0
                && assignment.ClaimActivity == null) {
                report.Info("Pokemon assignment has no success rewards yet. It can still track history and Pokemon mood/friendship.", context);
            }
        }

        foreach(var source in ProjectValidatorAssetFinder.FindAssets<PokemonAssignmentSource>()) {
            if(source == null) continue;

            string context = $"PokemonAssignmentSource/{source.name}";
            if(source.Assignment == null) {
                report.Warning("Pokemon assignment source has no assignment assigned.", context);
            }

            if(source.AccessProfile == null && source.Assignment == null) {
                report.Info("Pokemon assignment source currently has no access profile or assignment target.", context);
            }
        }

        foreach(var board in ProjectValidatorAssetFinder.FindAssets<PokemonAssignmentBoardDefinition>()) {
            if(board == null) continue;

            string context = $"PokemonAssignmentBoard/{board.name}";
            if(string.IsNullOrWhiteSpace(board.Id)) {
                report.Error("Pokemon assignment board id is empty.", context);
            }

            if(board.Tags != null && board.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Pokemon assignment board has an empty tag slot.", context);
            }

            if(board.Entries == null || board.Entries.Count == 0) {
                report.Info("Pokemon assignment board has no offers yet.", context);
                continue;
            }

            var duplicateOfferIds = board.Entries
                .Where(entry => entry != null && !string.IsNullOrWhiteSpace(entry.OfferId))
                .GroupBy(entry => entry.OfferId)
                .Where(group => group.Count() > 1);
            foreach(var duplicate in duplicateOfferIds) {
                report.Warning($"Pokemon assignment board has duplicate offer id '{duplicate.Key}'.", context);
            }

            foreach(var entry in board.Entries) {
                if(entry == null) {
                    report.Warning("Pokemon assignment board has a null offer slot.", context);
                    continue;
                }

                string entryContext = entry.Assignment != null ? $"{context}/Offer/{entry.Assignment.Id}" : $"{context}/Offer";
                if(entry.Assignment == null) {
                    report.Warning("Pokemon assignment board offer has no assignment.", entryContext);
                }

                ValidateObjectList(entry.ExtraRequirements, report, entryContext, "Pokemon assignment board offer has a null requirement slot.");
            }
        }

        foreach(var manager in ProjectValidatorAssetFinder.FindAssets<PokemonAssignmentUIManager>()) {
            if(manager == null) continue;

            string context = $"PokemonAssignmentUIManager/{manager.name}";
            if(manager.Board == null && (manager.DirectAssignments == null || manager.DirectAssignments.Count == 0)) {
                report.Warning("Pokemon assignment UI manager has no board or direct assignments.", context);
            }

            ValidateObjectList(manager.DirectAssignments, report, context, "Pokemon assignment UI manager has a null direct assignment slot.");
        }
    }

    static void ValidatePokemonMoodChanges(IEnumerable<PokemonMoodChange> changes, ProjectValidationReport report, string context, string phase) {
        if(changes == null) {
            return;
        }

        foreach(var change in changes) {
            if(change == null) {
                report.Warning($"Pokemon assignment has a null {phase} mood change slot.", context);
                continue;
            }

            if(change.mood == null) {
                report.Warning($"Pokemon assignment {phase} mood change has no mood assigned.", context);
            }
        }
    }

    static void ValidateCareNeedControllers(ProjectValidationReport report) {
        foreach(var controller in ProjectValidatorAssetFinder.FindAssets<PokemonCareNeedsController>()) {
            if(controller == null) continue;

            string context = $"PokemonCareNeedsController/{controller.name}";
            ValidateDefinitionList(controller.NeedDefinitions, report, context, "Pokemon care need");
        }
    }

    static void ValidateDefinitionList<T>(IReadOnlyList<T> definitions, ProjectValidationReport report, string context, string label) where T : ScriptableObject {
        if(definitions == null || definitions.Count == 0) {
            report.Info($"Controller has no {label} definitions assigned.", context);
            return;
        }

        var seen = new HashSet<string>();
        foreach(var definition in definitions) {
            if(definition == null) {
                report.Warning($"Controller has a null {label} slot.", context);
                continue;
            }

            string id = definition.name;
            switch(definition) {
                case SurvivalNeedDefinition survivalNeed:
                    id = survivalNeed.Id;
                    break;
                case PokemonCareNeedDefinition careNeed:
                    id = careNeed.Id;
                    break;
            }

            if(!string.IsNullOrWhiteSpace(id) && !seen.Add(id)) {
                report.Warning($"Controller has duplicate {label} id '{id}'.", context);
            }
        }
    }

    static void ValidateTitles(ProjectValidationReport report) {
        foreach(var title in ProjectValidatorAssetFinder.FindAssets<TitleDefinition>()) {
            if(title == null) continue;

            string context = $"Title/{title.name}";
            if(string.IsNullOrWhiteSpace(title.Id)) {
                report.Error("Title id is empty.", context);
            }

            if(!title.PermanentByDefault && title.CanBeTemporary && title.DefaultDurationHours <= 0) {
                report.Warning("Temporary title has no default duration.", context);
            }
        }
    }

    static void ValidateContentAuditProfiles(ProjectValidationReport report) {
        foreach(var profile in ProjectValidatorAssetFinder.FindAssets<ContentAuditProfileDefinition>()) {
            if(profile == null) continue;

            string context = $"ContentAudit/{profile.name}";
            if(string.IsNullOrWhiteSpace(profile.Id)) {
                report.Error("Content audit profile id is empty.", context);
            }

            if(profile.Rules == null || profile.Rules.Count == 0) {
                report.Info("Content audit profile has no rules yet.", context);
                continue;
            }

            foreach(var rule in profile.Rules) {
                if(rule == null) {
                    report.Warning("Content audit profile has a null rule slot.", context);
                    continue;
                }

                if(!rule.Enabled) {
                    continue;
                }

                string ruleContext = $"{context}/{rule.RuleId}";
                if(string.IsNullOrWhiteSpace(rule.RuleId)) {
                    report.Warning("Content audit rule id is empty.", ruleContext);
                }

                if(rule.TargetType == ContentAuditAssetType.CustomUnityObjectType && string.IsNullOrWhiteSpace(rule.CustomTypeName)) {
                    report.Error("Content audit custom type rule has no type name.", ruleContext);
                }

                if(rule.ScanScope == ContentAuditScanScope.LoadedSceneComponents
                    && rule.TargetType != ContentAuditAssetType.CustomUnityObjectType) {
                    report.Warning("Loaded Scene Components scope works best with Custom Unity Object Type pointing to a Component script.", ruleContext);
                }
            }
        }
    }

    static void ValidateAssetAuditProfiles(ProjectValidationReport report) {
        foreach(var profile in ProjectValidatorAssetFinder.FindAssets<AssetAuditProfileDefinition>()) {
            if(profile == null) continue;

            string context = $"AssetAuditProfile/{profile.name}";
            if(string.IsNullOrWhiteSpace(profile.Id)) {
                report.Error("Asset audit profile id is empty.", context);
            }

            if(profile.Tags != null && profile.Tags.Any(string.IsNullOrWhiteSpace)) {
                report.Warning("Asset audit profile has an empty tag slot.", context);
            }

            if(profile.Rules == null || profile.Rules.Count == 0) {
                report.Info("Asset audit profile has no rules.", context);
                continue;
            }

            foreach(var rule in profile.Rules) {
                if(rule == null) {
                    report.Warning("Asset audit profile has a null rule slot.", context);
                    continue;
                }

                string ruleContext = $"{context}/{rule.RuleId}";
                if(rule.TargetType == AssetAuditTargetType.CustomTypeName && string.IsNullOrWhiteSpace(rule.CustomTypeName)) {
                    report.Error("Asset audit custom type rule has no type name.", ruleContext);
                }

                if((rule.Kind == AssetAuditRuleKind.CountAssetsAtLeast
                    || rule.Kind == AssetAuditRuleKind.CountAssetsExactly
                    || rule.Kind == AssetAuditRuleKind.AssetExists)
                    && rule.Threshold <= 0) {
                    report.Info("Asset audit count/exists rule has a threshold of 0.", ruleContext);
                }

                foreach(var folder in rule.SearchFolders) {
                    if(string.IsNullOrWhiteSpace(folder)) {
                        report.Warning("Asset audit rule has an empty search folder slot.", ruleContext);
                    } else if(!folder.Replace('\\', '/').StartsWith("Assets")) {
                        report.Info("Asset audit rule searches outside Assets. Editor AssetDatabase-backed rules will ignore that folder.", ruleContext);
                    }
                }
            }
        }
    }

    static void ValidateDuplicateIds(ProjectValidationReport report) {
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<ActivityDefinition>(), report, "Activity", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<ActivityZoneDefinition>(), report, "ActivityZone", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<ActivityPermissionDefinition>(), report, "ActivityPermission", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<ActivityZoneModifierDefinition>(), report, "ActivityZoneModifier", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<ActivityOutcomeDefinition>(), report, "ActivityOutcome", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<RecipeDefinition>(), report, "Recipe", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<CraftingStationDefinition>(), report, "CraftingStation", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<ItemBrandDefinition>(), report, "ItemBrand", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<ItemModelDefinition>(), report, "ItemModel", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<ShopCatalogDefinition>(), report, "ShopCatalog", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<ShopShelfDefinition>(), report, "ShopShelf", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<ShopPaymentRuleDefinition>(), report, "ShopPaymentRule", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<ShopReturnPolicyDefinition>(), report, "ShopReturnPolicy", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<ShopSecurityPolicyDefinition>(), report, "ShopSecurityPolicy", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<ShopRestockScheduleDefinition>(), report, "ShopRestockSchedule", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<ShopDeliveryServiceDefinition>(), report, "ShopDeliveryService", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<LearnableOfferDefinition>(), report, "LearnableOffer", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<LoyaltyProgramDefinition>(), report, "LoyaltyProgram", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<SponsorDefinition>(), report, "Sponsor", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<ServiceDefinition>(), report, "Service", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<ServicePackageDefinition>(), report, "ServicePackage", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<ServiceAppointmentDefinition>(), report, "ServiceAppointment", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<EncounterTableDefinition>(), report, "EncounterTable", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<EncounterSourceProfileDefinition>(), report, "EncounterSourceProfile", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<StealthCaptureProfileDefinition>(), report, "StealthCapture", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<EncounterResolutionDefinition>(), report, "EncounterResolution", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<EncounterResolutionChoiceSetDefinition>(), report, "EncounterResolutionChoiceSet", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<CustomizationPartDefinition>(), report, "CustomizationPart", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<CustomizationPresetDefinition>(), report, "CustomizationPreset", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<PlayerOriginDefinition>(), report, "PlayerOrigin", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<PlayerLifestyleDefinition>(), report, "PlayerLifestyle", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<LifePathDefinition>(), report, "LifePath", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<LifePathPerkDefinition>(), report, "LifePathPerk", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<PokedexEntryDefinition>(), report, "PokedexEntry", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<RegionInfoDefinition>(), report, "RegionInfo", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<WorldRegionDefinition>(), report, "WorldRegion", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<RegionTravelRouteDefinition>(), report, "RegionTravelRoute", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<RegionTravelPolicyDefinition>(), report, "RegionTravelPolicy", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<RegionChallengeProfileDefinition>(), report, "RegionChallenge", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<RidePokemonDefinition>(), report, "Ride", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<PokeNavEntryDefinition>(), report, "PokeNavEntry", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<PokeNavGuideSectionDefinition>(), report, "PokeNavGuideSection", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<PokeNavFeedItemDefinition>(), report, "PokeNavFeedItem", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<SocialPostDefinition>(), report, "SocialPost", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<RoleActivityBoardDefinition>(), report, "RoleActivityBoard", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<CampStationDefinition>(), report, "CampStation", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<MapMarkerDefinition>(), report, "MapMarker", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<MapViewProfileDefinition>(), report, "MapViewProfile", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<RumorDefinition>(), report, "Rumor", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<RumorSpreadProfileDefinition>(), report, "RumorSpreadProfile", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<WorldConditionDefinition>(), report, "WorldCondition", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<JourneyEnvironmentProfileDefinition>(), report, "JourneyEnvironment", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<JourneyIncidentDefinition>(), report, "JourneyIncident", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<JourneyIncidentBoardDefinition>(), report, "JourneyIncidentBoard", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<RiskIncidentDefinition>(), report, "RiskIncident", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<ConsequenceChainDefinition>(), report, "ConsequenceChain", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<WorldTriggerDefinition>(), report, "WorldTrigger", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<SituationEventDefinition>(), report, "SituationEvent", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<SituationEventPoolDefinition>(), report, "SituationEventPool", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<SituationEventSignalProfileDefinition>(), report, "SituationEventSignalProfile", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<SceneObjectDefinition>(), report, "SceneObject", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<SceneSpawnProfileDefinition>(), report, "SceneSpawn", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<WorldDiscoveryDefinition>(), report, "WorldDiscovery", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<LocationVisitDefinition>(), report, "LocationVisit", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<ChronicleEntryDefinition>(), report, "ChronicleEntry", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<ChronicleCaptureRuleDefinition>(), report, "ChronicleCaptureRule", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<NavigationHintDefinition>(), report, "NavigationHint", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<AreaProfileDefinition>(), report, "AreaProfile", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<CalendarEventDefinition>(), report, "CalendarEvent", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<BattleAIProfile>(), report, "BattleAIProfile", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<BattleModeDefinition>(), report, "BattleMode", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<BattleRuleSetDefinition>(), report, "BattleRuleSet", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<PowerMechanicDefinition>(), report, "PowerMechanic", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<BattleChallengeDefinition>(), report, "BattleChallenge", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<CompetitionDefinition>(), report, "Competition", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<CompetitionRankingDefinition>(), report, "CompetitionRanking", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<CompetitionHonorDefinition>(), report, "CompetitionHonor", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<CompetitionSeasonDefinition>(), report, "CompetitionSeason", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<CompetitionEntrantDefinition>(), report, "CompetitionEntrant", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<CompetitionRosterDefinition>(), report, "CompetitionRoster", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<CompetitionPrizeTableDefinition>(), report, "CompetitionPrize", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<CompetitionVenueDefinition>(), report, "CompetitionVenue", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<CompetitionInvitationDefinition>(), report, "CompetitionInvitation", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<CompetitionRegistrationWindowDefinition>(), report, "CompetitionRegistrationWindow", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<CompetitionRegistrationDefinition>(), report, "CompetitionRegistration", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<CompetitionMatchResolverDefinition>(), report, "CompetitionMatchResolver", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<ContestDefinition>(), report, "Contest", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<CareerPathDefinition>(), report, "Career", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<OrganizationDefinition>(), report, "Organization", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<AssignmentDefinition>(), report, "Assignment", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<AccessProfileDefinition>(), report, "AccessProfile", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<LawViolationDefinition>(), report, "LawViolation", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<InvestigationCaseDefinition>(), report, "InvestigationCase", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<InvestigationClueDefinition>(), report, "InvestigationClue", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<NPCMemoryTopicDefinition>(), report, "NPCMemoryTopic", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<NPCReactionDefinition>(), report, "NPCReaction", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<WitnessReportDefinition>(), report, "WitnessReport", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<ReportPropagationDefinition>(), report, "ReportPropagation", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<CompanionRoleDefinition>(), report, "CompanionRole", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<CompanionPerkDefinition>(), report, "CompanionPerk", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<CompanionExpeditionDefinition>(), report, "CompanionExpedition", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<CompanionExpeditionRouteDefinition>(), report, "CompanionExpeditionRoute", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<NPCVisualSetDefinition>(), report, "NPCVisualSet", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<TrainerPartyTemplateDefinition>(), report, "TrainerPartyTemplate", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<NPCVariantPoolDefinition>(), report, "NPCVariantPool", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<NPCSceneRandomizationProfileDefinition>(), report, "NPCSceneRandomizationProfile", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<JobDefinition>(), report, "Job", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<JobBoardDefinition>(), report, "JobBoard", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<TransitRouteDefinition>(), report, "TransitRoute", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<TransitStopDefinition>(), report, "TransitStop", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<TransitJourneyDefinition>(), report, "TransitJourney", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<TransitRegionHandoffDefinition>(), report, "TransitRegionHandoff", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<FarmableDefinition>(), report, "Farmable", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<ResourceNodeDefinition>(), report, "Resource", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<ResearchSubjectDefinition>(), report, "Research", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<PokemonCareActionDefinition>(), report, "PokemonCare", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<PokemonCareNeedDefinition>(), report, "PokemonCareNeed", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<PokemonCareFacilityDefinition>(), report, "PokemonCareFacility", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<PokemonAssignmentDefinition>(), report, "PokemonAssignment", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<PokemonAssignmentBoardDefinition>(), report, "PokemonAssignmentBoard", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<ToolDefinition>(), report, "Tool", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<SurvivalNeedDefinition>(), report, "SurvivalNeed", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<MilestoneDefinition>(), report, "Milestone", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<TitleDefinition>(), report, "Title", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<RelationshipSubjectDefinition>(), report, "Relationship", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<ReputationFactionDefinition>(), report, "Reputation", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<PokemonMoodDefinition>(), report, "PokemonMood", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<NPCScheduleDefinition>(), report, "NPCSchedule", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<GameEventDefinition>(), report, "GameEvent", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<NotificationDefinition>(), report, "Notification", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<AssetAuditProfileDefinition>(), report, "AssetAuditProfile", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<ConditionalDialogDefinition>(), report, "ConditionalDialog", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<SpeechBubbleStyleDefinition>(), report, "SpeechBubbleStyle", x => x.Id);
        CheckDuplicateIds(ProjectValidatorAssetFinder.FindAssets<ContentAuditProfileDefinition>(), report, "ContentAuditProfile", x => x.Id);
    }

    static void ValidateObjectList<T>(IEnumerable<T> entries, ProjectValidationReport report, string context, string nullMessage) where T : UnityEngine.Object {
        if(entries == null) {
            return;
        }

        foreach(var entry in entries) {
            if(entry == null) {
                report.Warning(nullMessage, context);
            }
        }
    }

    static void CheckDuplicateIds<T>(IEnumerable<T> assets, ProjectValidationReport report, string label, System.Func<T, string> getId) where T : UnityEngine.Object {
        var duplicates = assets
            .Where(a => a != null)
            .GroupBy(getId)
            .Where(g => !string.IsNullOrWhiteSpace(g.Key) && g.Count() > 1);

        foreach(var duplicate in duplicates) {
            string names = string.Join(", ", duplicate.Select(a => a.name));
            report.Error($"Duplicate {label} id '{duplicate.Key}' used by: {names}", label);
        }
    }

    void LogReport(ProjectValidationReport report) {
        if(report == null) {
            return;
        }

        var severity = report.HasErrors ? GameDebugSeverity.Error : report.warningCount > 0 ? GameDebugSeverity.Warning : GameDebugSeverity.Success;
        GameDebugLogger.Ensure().Record(severity, GameDebugCategory.Validation, report.BuildSummary(), this, "ProjectValidator");

        foreach(var issue in report.issues) {
            if(issue.severity == ProjectValidationSeverity.Info && !logInfoIssues) {
                continue;
            }

            var debugSeverity = issue.severity switch {
                ProjectValidationSeverity.Error => GameDebugSeverity.Error,
                ProjectValidationSeverity.Warning => GameDebugSeverity.Warning,
                _ => GameDebugSeverity.Info
            };
            GameDebugLogger.Ensure().Record(debugSeverity, GameDebugCategory.Validation, $"{issue.context}: {issue.message}", this, "ProjectValidator");
        }
    }
}

static class ProjectValidatorAssetFinder {
    public static IReadOnlyList<T> FindAssets<T>() where T : UnityEngine.Object {
        var results = new List<T>();
        var seen = new HashSet<EntityId>();

        AddRange(Resources.LoadAll<T>(""), results, seen);

#if UNITY_EDITOR
        AddEditorAssets(results, seen);
#endif

        if(typeof(Component).IsAssignableFrom(typeof(T))) {
            AddRange(UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include), results, seen);
        }

        return results;
    }

    static void AddRange<T>(IEnumerable<T> assets, List<T> results, HashSet<EntityId> seen) where T : UnityEngine.Object {
        if(assets == null) {
            return;
        }

        foreach(var asset in assets) {
            if(asset == null) {
                continue;
            }

            var id = asset.GetEntityId();
            if(seen.Add(id)) {
                results.Add(asset);
            }
        }
    }

#if UNITY_EDITOR
    static void AddEditorAssets<T>(List<T> results, HashSet<EntityId> seen) where T : UnityEngine.Object {
        if(typeof(Component).IsAssignableFrom(typeof(T))) {
            AddComponentsFromPrefabs(results, seen);
            return;
        }

        string filter = typeof(T) == typeof(ScriptableObject) ? "t:ScriptableObject" : $"t:{typeof(T).Name}";
        foreach(var guid in AssetDatabase.FindAssets(filter, new[] { "Assets" })) {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if(string.IsNullOrWhiteSpace(path)) {
                continue;
            }

            foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(path)) {
                if(asset is T typed) {
                    AddRange(new[] { typed }, results, seen);
                }
            }
        }
    }

    static void AddComponentsFromPrefabs<T>(List<T> results, HashSet<EntityId> seen) where T : UnityEngine.Object {
        foreach(var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" })) {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(prefab == null) {
                continue;
            }

            AddRange(prefab.GetComponentsInChildren(typeof(T), true).OfType<T>(), results, seen);
        }
    }
#endif
}
