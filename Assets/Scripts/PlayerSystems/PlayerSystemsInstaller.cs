using UnityEngine;

public class PlayerSystemsInstaller : MonoBehaviour {
    [Tooltip("If enabled, player backend systems are installed during Awake.")]
    [SerializeField] bool installOnAwake = true;
    [Tooltip("If enabled, installs SurvivalNeedsController.")]
    [SerializeField] bool installSurvivalNeeds = true;
    [Tooltip("If enabled, installs PokemonCareNeedsController for passive party Pokemon care needs.")]
    [SerializeField] bool installPokemonCareNeeds = true;
    [Tooltip("If enabled, installs PlayerProgression.")]
    [SerializeField] bool installProgression = true;
    [Tooltip("If enabled, installs PlayerResearchLog.")]
    [SerializeField] bool installResearchLog = true;
    [Tooltip("If enabled, installs PlayerToolInventory.")]
    [SerializeField] bool installToolInventory = true;
    [Tooltip("If enabled, installs PlayerRecipeBook for learned crafting recipes.")]
    [SerializeField] bool installRecipeBook = true;
    [Tooltip("If enabled, installs PlayerShopLedger for limited shop stock and purchase history.")]
    [SerializeField] bool installShopLedger = true;
    [Tooltip("If enabled, installs PlayerShopBasketLog for market basket and self-checkout state.")]
    [SerializeField] bool installShopBasketLog = true;
    [Tooltip("If enabled, installs PlayerShopShelfLog for market shelf browsing and add-to-basket history.")]
    [SerializeField] bool installShopShelfLog = true;
    [Tooltip("If enabled, installs PlayerLearnableOfferLog for recipe, permit and information purchase history.")]
    [SerializeField] bool installLearnableOfferLog = true;
    [Tooltip("If enabled, installs PlayerLoyaltyLog for shop, clinic, inn and special membership points/tier history.")]
    [SerializeField] bool installLoyaltyLog = true;
    [Tooltip("If enabled, installs PlayerServicePackageLog for bundled service, membership and appointment-like package history.")]
    [SerializeField] bool installServicePackageLog = true;
    [Tooltip("If enabled, installs PlayerServiceLog for paid/free service history and repeat rules.")]
    [SerializeField] bool installServiceLog = true;
    [Tooltip("If enabled, installs PlayerEncounterLog for seen/captured encounter history.")]
    [SerializeField] bool installEncounterLog = true;
    [Tooltip("If enabled, installs PlayerOverworldFleeLog for virtual/unloaded-map flee state.")]
    [SerializeField] bool installOverworldFleeLog = true;
    [Tooltip("If enabled, installs PlayerJobLog for job board and repeatable task history.")]
    [SerializeField] bool installJobLog = true;
    [Tooltip("If enabled, installs PlayerTransitLog for route unlocks and travel history.")]
    [SerializeField] bool installTransitLog = true;
    [Tooltip("If enabled, installs PlayerTransitJourneyLog for active vehicle journeys, stop windows and onboard activity history.")]
    [SerializeField] bool installTransitJourneyLog = true;
    [Tooltip("If enabled, installs PlayerCustomization for outfit/preset state.")]
    [SerializeField] bool installCustomization = true;
    [Tooltip("If enabled, installs PlayerOriginLog for selected New Game origin/background state.")]
    [SerializeField] bool installOriginLog = true;
    [Tooltip("If enabled, installs PlayerNewGameSetupLog for selected New Game setup package state.")]
    [SerializeField] bool installNewGameSetupLog = true;
    [Tooltip("If enabled, installs PlayerPokeNavLog for Pokedex, region and social feed knowledge.")]
    [SerializeField] bool installPokeNavLog = true;
    [Tooltip("If enabled, installs PlayerPokeNavFeedLog for PokeNav news, sightings and bulletin history.")]
    [SerializeField] bool installPokeNavFeedLog = true;
    [Tooltip("If enabled, installs PlayerPokeNavGuideLog for generic guide read, pin and dismiss state.")]
    [SerializeField] bool installPokeNavGuideLog = true;
    [Tooltip("If enabled, installs PlayerPhoneLog for known contacts, phone call history and storage-call permissions.")]
    [SerializeField] bool installPhoneLog = true;
    [Tooltip("If enabled, installs PlayerDialogGraphLog for interactive dialog node and response history.")]
    [SerializeField] bool installDialogGraphLog = true;
    [Tooltip("If enabled, installs PlayerSocialActivityLog for hangout, date, camp and companion/Pokemon social activity history.")]
    [SerializeField] bool installSocialActivityLog = true;
    [Tooltip("If enabled, installs PlayerRoleActivityBoardLog for police/professor/camp/ranger board view and action history.")]
    [SerializeField] bool installRoleActivityBoardLog = true;
    [Tooltip("If enabled, installs PlayerCampStationLog for camp station view and action history.")]
    [SerializeField] bool installCampStationLog = true;
    [Tooltip("If enabled, installs PlayerMapLog for discovered minimap/world map markers.")]
    [SerializeField] bool installMapLog = true;
    [Tooltip("If enabled, installs PlayerMapNavigationLog for active minimap/world map targets.")]
    [SerializeField] bool installMapNavigationLog = true;
    [Tooltip("If enabled, installs PlayerWorldRegionLog for multi-region travel and challenge state.")]
    [SerializeField] bool installWorldRegionLog = true;
    [Tooltip("If enabled, installs PlayerRideLog for Pokemon ride mount/dismount history.")]
    [SerializeField] bool installRideLog = true;
    [Tooltip("If enabled, installs PlayerRideController for Pokemon ride runtime behavior.")]
    [SerializeField] bool installRideController = true;
    [Tooltip("If enabled, installs PlayerRideCompanionLog for ride capacity and companion catch-up history.")]
    [SerializeField] bool installRideCompanionLog = true;
    [Tooltip("If enabled, installs PlayerRideCompanionCoordinator for ride companion capacity handling.")]
    [SerializeField] bool installRideCompanionCoordinator = true;
    [Tooltip("If enabled, installs PlayerPokemonFollowerLog for selected Pokemon follower state and history.")]
    [SerializeField] bool installPokemonFollowerLog = true;
    [Tooltip("If enabled, installs PlayerPokemonFollowerController for active party Pokemon following the player.")]
    [SerializeField] bool installPokemonFollowerController = true;
    [Tooltip("If enabled, installs PlayerNodeTrailTracker so companion and follower node-follow systems can follow player node history.")]
    [SerializeField] bool installPlayerNodeTrailTracker = true;
    [Tooltip("If enabled, installs PlayerRumorLog for heard/unlocked rumor history.")]
    [SerializeField] bool installRumorLog = true;
    [Tooltip("If enabled, installs PlayerRumorLifecycleLog for rumor spread/decay state.")]
    [SerializeField] bool installRumorLifecycleLog = true;
    [Tooltip("If enabled, installs PlayerWorldConditionLog for active world condition state.")]
    [SerializeField] bool installWorldConditionLog = true;
    [Tooltip("If enabled, installs PlayerJourneyEnvironmentLog for hourly region/weather/environment effect history.")]
    [SerializeField] bool installJourneyEnvironmentLog = true;
    [Tooltip("If enabled, installs PlayerJourneyIncidentLog for route/camp/travel incident active state and history.")]
    [SerializeField] bool installJourneyIncidentLog = true;
    [Tooltip("If enabled, installs PlayerRiskLog for heat, suspicion and evidence tracking.")]
    [SerializeField] bool installRiskLog = true;
    [Tooltip("If enabled, installs PlayerConsequenceChainLog for consequence chain run history and repeat rules.")]
    [SerializeField] bool installConsequenceChainLog = true;
    [Tooltip("If enabled, installs PlayerWorldTriggerLog for world trigger run history and repeat rules.")]
    [SerializeField] bool installWorldTriggerLog = true;
    [Tooltip("If enabled, installs PlayerSituationEventLog for regional/situational event history and active event state.")]
    [SerializeField] bool installSituationEventLog = true;
    [Tooltip("If enabled, installs PlayerSituationEventSignalLog for situation event signal cooldown/evaluation history.")]
    [SerializeField] bool installSituationEventSignalLog = true;
    [Tooltip("If enabled, installs PlayerSceneObjectLog for logical scene object state and interaction history.")]
    [SerializeField] bool installSceneObjectLog = true;
    [Tooltip("If enabled, installs PlayerSceneSpawnLog for scene spawn and conditional prefab history.")]
    [SerializeField] bool installSceneSpawnLog = true;
    [Tooltip("If enabled, installs PlayerWorldDiscoveryLog for PokeNav/map sightings and discovery history.")]
    [SerializeField] bool installWorldDiscoveryLog = true;
    [Tooltip("If enabled, installs PlayerLocationVisitLog for region, scene and location visit history.")]
    [SerializeField] bool installLocationVisitLog = true;
    [Tooltip("If enabled, installs PlayerChronicleLog for long-term event and journal history.")]
    [SerializeField] bool installChronicleLog = true;
    [Tooltip("If enabled, installs PlayerNavigationHintLog for active map, minimap and PokeNav guidance targets.")]
    [SerializeField] bool installNavigationHintLog = true;
    [Tooltip("If enabled, installs PlayerAreaProfileLog for active area profile and area transition history.")]
    [SerializeField] bool installAreaProfileLog = true;
    [Tooltip("If enabled, installs PlayerCalendarLog for scheduled events and calendar history.")]
    [SerializeField] bool installCalendarLog = true;
    [Tooltip("If enabled, installs PlayerBattleModeSettings for selected classic/future battle mode preferences.")]
    [SerializeField] bool installBattleModeSettings = true;
    [Tooltip("If enabled, installs PlayerBattleRuleLog for challenge, rule and battle format history.")]
    [SerializeField] bool installBattleRuleLog = true;
    [Tooltip("If enabled, installs PlayerCompetitionLog for league, frontier and championship progression.")]
    [SerializeField] bool installCompetitionLog = true;
    [Tooltip("If enabled, installs PlayerCompetitionRankingLog for league points, ranks and qualification.")]
    [SerializeField] bool installCompetitionRankingLog = true;
    [Tooltip("If enabled, installs PlayerCompetitionHonorLog for medals, champion titles and Hall of Fame records.")]
    [SerializeField] bool installCompetitionHonorLog = true;
    [Tooltip("If enabled, installs PlayerCompetitionSeasonLog for league seasons, qualifiers and championship cycles.")]
    [SerializeField] bool installCompetitionSeasonLog = true;
    [Tooltip("If enabled, installs PlayerCompetitionBracketLog for generated tournament rosters and bracket runs.")]
    [SerializeField] bool installCompetitionBracketLog = true;
    [Tooltip("If enabled, installs PlayerCompetitionPrizeLog for tournament prize history and repeat limits.")]
    [SerializeField] bool installCompetitionPrizeLog = true;
    [Tooltip("If enabled, installs PlayerCompetitionRegistrationLog for tournament registration history and repeat limits.")]
    [SerializeField] bool installCompetitionRegistrationLog = true;
    [Tooltip("If enabled, installs PlayerCompetitionInvitationLog for qualifier passes, invitations and wildcards.")]
    [SerializeField] bool installCompetitionInvitationLog = true;
    [Tooltip("If enabled, installs PlayerCompetitionVenueLog for arena, gym and stadium history.")]
    [SerializeField] bool installCompetitionVenueLog = true;
    [Tooltip("If enabled, installs PlayerSponsorLog for sponsor agreements and brand/shop benefits.")]
    [SerializeField] bool installSponsorLog = true;
    [Tooltip("If enabled, installs PlayerPowerMechanicLog for Mega/Z/Gigantamax unlocks, charges and usage history.")]
    [SerializeField] bool installPowerMechanicLog = true;
    [Tooltip("If enabled, installs PlayerContestLog for contest unlocks, attempts and best scores.")]
    [SerializeField] bool installContestLog = true;
    [Tooltip("If enabled, installs PlayerCareerLog for career path unlocks, ranks and points.")]
    [SerializeField] bool installCareerLog = true;
    [Tooltip("If enabled, installs PlayerOrganizationLog for organization memberships, ranks and permits.")]
    [SerializeField] bool installOrganizationLog = true;
    [Tooltip("If enabled, installs PlayerAssignmentLog for police/professor/special assignments.")]
    [SerializeField] bool installAssignmentLog = true;
    [Tooltip("If enabled, installs PlayerAccessLog for reusable access profile history.")]
    [SerializeField] bool installAccessLog = true;
    [Tooltip("If enabled, installs PlayerLawLog for law, incident and wanted state history.")]
    [SerializeField] bool installLawLog = true;
    [Tooltip("If enabled, installs PlayerInvestigationLog for case, clue and evidence history.")]
    [SerializeField] bool installInvestigationLog = true;
    [Tooltip("If enabled, installs PlayerNPCMemoryLog for NPC interaction and memory history.")]
    [SerializeField] bool installNPCMemoryLog = true;
    [Tooltip("If enabled, installs PlayerNPCReactionLog for NPC reaction and witness history.")]
    [SerializeField] bool installNPCReactionLog = true;
    [Tooltip("If enabled, installs PlayerWitnessReportLog for witness/report history.")]
    [SerializeField] bool installWitnessReportLog = true;
    [Tooltip("If enabled, installs PlayerReportPropagationLog for report spread/broadcast history.")]
    [SerializeField] bool installReportPropagationLog = true;
    [Tooltip("If enabled, installs PlayerPokemonCareFacilityLog for daycare/ranch/facility stay history.")]
    [SerializeField] bool installPokemonCareFacilityLog = true;
    [Tooltip("If enabled, installs PlayerPokemonAssignmentLog for Pokemon task/assignment history.")]
    [SerializeField] bool installPokemonAssignmentLog = true;
    [Tooltip("If enabled, installs PlayerPokemonHeldItemLog for Pokemon held item equip, unequip and swap history.")]
    [SerializeField] bool installPokemonHeldItemLog = true;
    [Tooltip("If enabled, installs PlayerCompanionExpeditionLog for companion task/expedition history.")]
    [SerializeField] bool installCompanionExpeditionLog = true;
    [Tooltip("If enabled, installs PlayerCompanionExpeditionRouteLog for multi-stage companion expedition history.")]
    [SerializeField] bool installCompanionExpeditionRouteLog = true;
    [Tooltip("If enabled, installs PlayerReputation.")]
    [SerializeField] bool installReputation = true;
    [Tooltip("If enabled, installs PlayerActivityJournal.")]
    [SerializeField] bool installActivityJournal = true;
    [Tooltip("If enabled, installs PlayerLifestyleLog for playstyle/lifestyle point tracking.")]
    [SerializeField] bool installLifestyleLog = true;
    [Tooltip("If enabled, installs PlayerLifePathLog for vocation/life path XP, branches and perks.")]
    [SerializeField] bool installLifePathLog = true;
    [Tooltip("If enabled, installs PlayerRelationships.")]
    [SerializeField] bool installRelationships = true;
    [Tooltip("If enabled, installs PlayerMilestones.")]
    [SerializeField] bool installMilestones = true;
    [Tooltip("If enabled, installs PlayerTitles for title, badge, permit and license access.")]
    [SerializeField] bool installTitles = true;

    void Awake() {
        if(installOnAwake) {
            Install();
        }
    }

    public void Install() {
        if(installSurvivalNeeds) {
            EnsureComponent<SurvivalNeedsController>();
        }

        if(installPokemonCareNeeds) {
            EnsureComponent<PokemonCareNeedsController>();
        }

        if(installProgression) {
            EnsureComponent<PlayerProgression>();
        }

        if(installResearchLog) {
            EnsureComponent<PlayerResearchLog>();
        }

        if(installToolInventory) {
            EnsureComponent<PlayerToolInventory>();
        }

        if(installRecipeBook) {
            EnsureComponent<PlayerRecipeBook>();
        }

        if(installShopLedger) {
            EnsureComponent<PlayerShopLedger>();
        }

        if(installShopBasketLog) {
            EnsureComponent<PlayerShopBasketLog>();
        }

        if(installShopShelfLog) {
            EnsureComponent<PlayerShopShelfLog>();
        }

        if(installLearnableOfferLog) {
            EnsureComponent<PlayerLearnableOfferLog>();
        }

        if(installLoyaltyLog) {
            EnsureComponent<PlayerLoyaltyLog>();
        }

        if(installServicePackageLog) {
            EnsureComponent<PlayerServicePackageLog>();
        }

        if(installServiceLog) {
            EnsureComponent<PlayerServiceLog>();
        }

        if(installEncounterLog) {
            EnsureComponent<PlayerEncounterLog>();
        }

        if(installOverworldFleeLog) {
            EnsureComponent<PlayerOverworldFleeLog>();
        }

        if(installJobLog) {
            EnsureComponent<PlayerJobLog>();
        }

        if(installTransitLog) {
            EnsureComponent<PlayerTransitLog>();
        }

        if(installTransitJourneyLog) {
            EnsureComponent<PlayerTransitJourneyLog>();
        }

        if(installCustomization) {
            EnsureComponent<PlayerCustomization>();
        }

        if(installOriginLog) {
            EnsureComponent<PlayerOriginLog>();
        }

        if(installNewGameSetupLog) {
            EnsureComponent<PlayerNewGameSetupLog>();
        }

        if(installPokeNavLog) {
            EnsureComponent<PlayerPokeNavLog>();
        }

        if(installPokeNavFeedLog) {
            EnsureComponent<PlayerPokeNavFeedLog>();
        }

        if(installPokeNavGuideLog) {
            EnsureComponent<PlayerPokeNavGuideLog>();
        }

        if(installPhoneLog) {
            EnsureComponent<PlayerPhoneLog>();
        }

        if(installDialogGraphLog) {
            EnsureComponent<PlayerDialogGraphLog>();
        }

        if(installSocialActivityLog) {
            EnsureComponent<PlayerSocialActivityLog>();
        }

        if(installRoleActivityBoardLog) {
            EnsureComponent<PlayerRoleActivityBoardLog>();
        }

        if(installCampStationLog) {
            EnsureComponent<PlayerCampStationLog>();
        }

        if(installMapLog) {
            EnsureComponent<PlayerMapLog>();
        }

        if(installMapNavigationLog) {
            EnsureComponent<PlayerMapNavigationLog>();
        }

        if(installWorldRegionLog) {
            EnsureComponent<PlayerWorldRegionLog>();
        }

        if(installRideLog) {
            EnsureComponent<PlayerRideLog>();
        }

        if(installRideController) {
            EnsureComponent<PlayerRideController>();
        }

        if(installRideCompanionLog) {
            EnsureComponent<PlayerRideCompanionLog>();
        }

        if(installRideCompanionCoordinator) {
            EnsureComponent<PlayerRideCompanionCoordinator>();
        }

        if(installPokemonFollowerLog) {
            EnsureComponent<PlayerPokemonFollowerLog>();
        }

        if(installPokemonFollowerController) {
            EnsureComponent<PlayerPokemonFollowerController>();
        }

        if(installPlayerNodeTrailTracker) {
            EnsureComponent<PlayerNodeTrailTracker>();
        }

        if(installRumorLog) {
            EnsureComponent<PlayerRumorLog>();
        }

        if(installRumorLifecycleLog) {
            EnsureComponent<PlayerRumorLifecycleLog>();
        }

        if(installWorldConditionLog) {
            EnsureComponent<PlayerWorldConditionLog>();
        }

        if(installJourneyEnvironmentLog) {
            EnsureComponent<PlayerJourneyEnvironmentLog>();
        }

        if(installJourneyIncidentLog) {
            EnsureComponent<PlayerJourneyIncidentLog>();
        }

        if(installRiskLog) {
            EnsureComponent<PlayerRiskLog>();
        }

        if(installConsequenceChainLog) {
            EnsureComponent<PlayerConsequenceChainLog>();
        }

        if(installWorldTriggerLog) {
            EnsureComponent<PlayerWorldTriggerLog>();
        }

        if(installSituationEventLog) {
            EnsureComponent<PlayerSituationEventLog>();
        }

        if(installSituationEventSignalLog) {
            EnsureComponent<PlayerSituationEventSignalLog>();
        }

        if(installSceneObjectLog) {
            EnsureComponent<PlayerSceneObjectLog>();
        }

        if(installSceneSpawnLog) {
            EnsureComponent<PlayerSceneSpawnLog>();
        }

        if(installWorldDiscoveryLog) {
            EnsureComponent<PlayerWorldDiscoveryLog>();
        }

        if(installLocationVisitLog) {
            EnsureComponent<PlayerLocationVisitLog>();
        }

        if(installChronicleLog) {
            EnsureComponent<PlayerChronicleLog>();
        }

        if(installNavigationHintLog) {
            EnsureComponent<PlayerNavigationHintLog>();
        }

        if(installAreaProfileLog) {
            EnsureComponent<PlayerAreaProfileLog>();
        }

        if(installCalendarLog) {
            EnsureComponent<PlayerCalendarLog>();
        }

        if(installBattleModeSettings) {
            EnsureComponent<PlayerBattleModeSettings>();
        }

        if(installBattleRuleLog) {
            EnsureComponent<PlayerBattleRuleLog>();
        }

        if(installCompetitionLog) {
            EnsureComponent<PlayerCompetitionLog>();
        }

        if(installCompetitionRankingLog) {
            EnsureComponent<PlayerCompetitionRankingLog>();
        }

        if(installCompetitionHonorLog) {
            EnsureComponent<PlayerCompetitionHonorLog>();
        }

        if(installCompetitionSeasonLog) {
            EnsureComponent<PlayerCompetitionSeasonLog>();
        }

        if(installCompetitionBracketLog) {
            EnsureComponent<PlayerCompetitionBracketLog>();
        }

        if(installCompetitionPrizeLog) {
            EnsureComponent<PlayerCompetitionPrizeLog>();
        }

        if(installCompetitionRegistrationLog) {
            EnsureComponent<PlayerCompetitionRegistrationLog>();
        }

        if(installCompetitionInvitationLog) {
            EnsureComponent<PlayerCompetitionInvitationLog>();
        }

        if(installCompetitionVenueLog) {
            EnsureComponent<PlayerCompetitionVenueLog>();
        }

        if(installSponsorLog) {
            EnsureComponent<PlayerSponsorLog>();
        }

        if(installPowerMechanicLog) {
            EnsureComponent<PlayerPowerMechanicLog>();
        }

        if(installContestLog) {
            EnsureComponent<PlayerContestLog>();
        }

        if(installCareerLog) {
            EnsureComponent<PlayerCareerLog>();
        }

        if(installOrganizationLog) {
            EnsureComponent<PlayerOrganizationLog>();
        }

        if(installAssignmentLog) {
            EnsureComponent<PlayerAssignmentLog>();
        }

        if(installAccessLog) {
            EnsureComponent<PlayerAccessLog>();
        }

        if(installLawLog) {
            EnsureComponent<PlayerLawLog>();
        }

        if(installInvestigationLog) {
            EnsureComponent<PlayerInvestigationLog>();
        }

        if(installNPCMemoryLog) {
            EnsureComponent<PlayerNPCMemoryLog>();
        }

        if(installNPCReactionLog) {
            EnsureComponent<PlayerNPCReactionLog>();
        }

        if(installWitnessReportLog) {
            EnsureComponent<PlayerWitnessReportLog>();
        }

        if(installReportPropagationLog) {
            EnsureComponent<PlayerReportPropagationLog>();
        }

        if(installPokemonCareFacilityLog) {
            EnsureComponent<PlayerPokemonCareFacilityLog>();
        }

        if(installPokemonAssignmentLog) {
            EnsureComponent<PlayerPokemonAssignmentLog>();
        }

        if(installPokemonHeldItemLog) {
            EnsureComponent<PlayerPokemonHeldItemLog>();
        }

        if(installCompanionExpeditionLog) {
            EnsureComponent<PlayerCompanionExpeditionLog>();
        }

        if(installCompanionExpeditionRouteLog) {
            EnsureComponent<PlayerCompanionExpeditionRouteLog>();
        }

        if(installReputation) {
            EnsureComponent<PlayerReputation>();
        }

        if(installActivityJournal) {
            EnsureComponent<PlayerActivityJournal>();
        }

        if(installLifestyleLog) {
            EnsureComponent<PlayerLifestyleLog>();
        }

        if(installLifePathLog) {
            EnsureComponent<PlayerLifePathLog>();
        }

        if(installRelationships) {
            EnsureComponent<PlayerRelationships>();
        }

        if(installMilestones) {
            EnsureComponent<PlayerMilestones>();
        }

        if(installTitles) {
            EnsureComponent<PlayerTitles>();
        }
    }

    T EnsureComponent<T>() where T : Component {
        var component = GetComponent<T>();
        if(component == null) {
            component = gameObject.AddComponent<T>();
        }
        return component;
    }
}
