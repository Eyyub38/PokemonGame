using System.Collections;
using UnityEngine;

public class RuntimeHealthMonitor : MonoBehaviour {
    [Tooltip("If enabled, runs a health check once when the scene starts.")]
    [SerializeField] bool runOnStart = true;
    [Tooltip("If enabled, health checks repeat while this component is active.")]
    [SerializeField] bool repeatChecks = true;
    [Tooltip("Seconds between repeated health checks.")]
    [Min(1f)]
    [SerializeField] float checkIntervalSeconds = 10f;
    [Tooltip("If enabled, checks for duplicate singleton/manager components.")]
    [SerializeField] bool checkDuplicateManagers = true;
    [Tooltip("If enabled, checks critical scene references such as GameController and TimeSystem.")]
    [SerializeField] bool checkCriticalReferences = true;
    [Tooltip("If enabled, checks expected player backend components.")]
    [SerializeField] bool checkPlayerSystems = true;
    [Tooltip("If enabled, successful checks are written to the debug log.")]
    [SerializeField] bool logSuccessfulChecks;

    Coroutine monitorRoutine;

    void Start() {
        if(runOnStart) {
            RunHealthCheck();
        }

        if(repeatChecks) {
            monitorRoutine = StartCoroutine(MonitorRoutine());
        }
    }

    void OnDisable() {
        if(monitorRoutine != null) {
            StopCoroutine(monitorRoutine);
            monitorRoutine = null;
        }
    }

    [ContextMenu("Run Runtime Health Check")]
    public void RunHealthCheck() {
        int issues = 0;

        if(checkDuplicateManagers) {
            issues += CheckDuplicateManagers();
        }

        if(checkCriticalReferences) {
            issues += CheckCriticalReferences();
        }

        if(checkPlayerSystems) {
            issues += CheckPlayerSystems();
        }

        if(issues == 0 && logSuccessfulChecks) {
            GameDebug.Success("Runtime health check passed.", GameDebugCategory.Validation, this, "RuntimeHealthMonitor");
        } else if(issues > 0) {
            GameDebug.Warning($"Runtime health check found {issues} issue(s).", GameDebugCategory.Validation, this, "RuntimeHealthMonitor");
        }
    }

    IEnumerator MonitorRoutine() {
        var wait = new WaitForSeconds(Mathf.Max(1f, checkIntervalSeconds));
        while(enabled) {
            yield return wait;
            RunHealthCheck();
        }
    }

    int CheckDuplicateManagers() {
        int issues = 0;
        issues += WarnIfDuplicate<GameController>("GameController");
        issues += WarnIfDuplicate<PlayerController>("PlayerController");
        issues += WarnIfDuplicate<BattleSystem>("BattleSystem");
        issues += WarnIfDuplicate<DialogManager>("DialogManager");
        issues += WarnIfDuplicate<AudioManager>("AudioManager");
        issues += WarnIfDuplicate<TimeSystem>("TimeSystem");
        issues += WarnIfDuplicate<SavingSystem>("SavingSystem");
        issues += WarnIfDuplicate<GameDebugLogger>("GameDebugLogger");
        issues += WarnIfDuplicate<GameEventBus>("GameEventBus");
        issues += WarnIfDuplicate<NotificationFeed>("NotificationFeed");
        issues += WarnIfDuplicate<SpeechBubbleDialogManager>("SpeechBubbleDialogManager");
        issues += WarnIfDuplicate<WorldEventManager>("WorldEventManager");
        issues += WarnIfDuplicate<BattleRuleManager>("BattleRuleManager");
        return issues;
    }

    int CheckCriticalReferences() {
        int issues = 0;

        if(GameController.i == null) {
            GameDebug.Error("GameController.i is null.", GameDebugCategory.Validation, this, "RuntimeHealthMonitor");
            issues++;
        } else {
            issues += WarnIfNull(GameController.i.PlayerController, "GameController.PlayerController", GameController.i);
            issues += WarnIfNull(GameController.i.WorldCamera, "GameController.WorldCamera", GameController.i);
            issues += WarnIfNull(GameController.i.PartyScreen, "GameController.PartyScreen", GameController.i);
            issues += WarnIfNull(GameController.i.InputMaps, "GameController.InputMaps", GameController.i);

            if(GameController.i.StateMachine == null) {
                GameDebug.Error("GameController.StateMachine is null.", GameDebugCategory.Validation, GameController.i, "RuntimeHealthMonitor");
                issues++;
            } else if(GameController.i.StateMachine.CurrentState == null) {
                GameDebug.Warning("GameController.StateMachine.CurrentState is null.", GameDebugCategory.Validation, GameController.i, "RuntimeHealthMonitor");
                issues++;
            }
        }

        issues += WarnIfNull(DialogManager.i, "DialogManager.i", this);
        issues += WarnIfNull(GameLayers.i, "GameLayers.i", this);
        issues += WarnIfNull(GlobalSettings.i, "GlobalSettings.i", this);
        issues += WarnIfNull(TimeSystem.i, "TimeSystem.i", this);

        return issues;
    }

    int CheckPlayerSystems() {
        var player = PlayerController.i;
        if(player == null) {
            GameDebug.Error("PlayerController.i is null.", GameDebugCategory.Validation, this, "RuntimeHealthMonitor");
            return 1;
        }

        int issues = 0;
        issues += WarnIfMissingComponent<Character>(player);
        issues += WarnIfMissingComponent<PokemonParty>(player);
        issues += WarnIfMissingComponent<Inventory>(player);
        issues += WarnIfMissingComponent<QuestList>(player);
        issues += WarnIfMissingComponent<SurvivalNeedsController>(player);
        issues += WarnIfMissingComponent<PokemonCareNeedsController>(player);
        issues += WarnIfMissingComponent<PlayerProgression>(player);
        issues += WarnIfMissingComponent<PlayerResearchLog>(player);
        issues += WarnIfMissingComponent<PlayerToolInventory>(player);
        issues += WarnIfMissingComponent<PlayerRecipeBook>(player);
        issues += WarnIfMissingComponent<PlayerShopLedger>(player);
        issues += WarnIfMissingComponent<PlayerShopBasketLog>(player);
        issues += WarnIfMissingComponent<PlayerShopShelfLog>(player);
        issues += WarnIfMissingComponent<PlayerLearnableOfferLog>(player);
        issues += WarnIfMissingComponent<PlayerLoyaltyLog>(player);
        issues += WarnIfMissingComponent<PlayerServicePackageLog>(player);
        issues += WarnIfMissingComponent<PlayerServiceLog>(player);
        issues += WarnIfMissingComponent<PlayerEncounterLog>(player);
        issues += WarnIfMissingComponent<PlayerJobLog>(player);
        issues += WarnIfMissingComponent<PlayerTransitLog>(player);
        issues += WarnIfMissingComponent<PlayerCustomization>(player);
        issues += WarnIfMissingComponent<PlayerOriginLog>(player);
        issues += WarnIfMissingComponent<PlayerNewGameSetupLog>(player);
        issues += WarnIfMissingComponent<PlayerPokeNavLog>(player);
        issues += WarnIfMissingComponent<PlayerPokeNavFeedLog>(player);
        issues += WarnIfMissingComponent<PlayerPokeNavGuideLog>(player);
        issues += WarnIfMissingComponent<PlayerRoleActivityBoardLog>(player);
        issues += WarnIfMissingComponent<PlayerCampStationLog>(player);
        issues += WarnIfMissingComponent<PlayerMapLog>(player);
        issues += WarnIfMissingComponent<PlayerMapNavigationLog>(player);
        issues += WarnIfMissingComponent<PlayerWorldRegionLog>(player);
        issues += WarnIfMissingComponent<PlayerRideLog>(player);
        issues += WarnIfMissingComponent<PlayerRideController>(player);
        issues += WarnIfMissingComponent<PlayerRumorLog>(player);
        issues += WarnIfMissingComponent<PlayerRumorLifecycleLog>(player);
        issues += WarnIfMissingComponent<PlayerWorldConditionLog>(player);
        issues += WarnIfMissingComponent<PlayerJourneyEnvironmentLog>(player);
        issues += WarnIfMissingComponent<PlayerJourneyIncidentLog>(player);
        issues += WarnIfMissingComponent<PlayerRiskLog>(player);
        issues += WarnIfMissingComponent<PlayerConsequenceChainLog>(player);
        issues += WarnIfMissingComponent<PlayerWorldTriggerLog>(player);
        issues += WarnIfMissingComponent<PlayerSituationEventLog>(player);
        issues += WarnIfMissingComponent<PlayerSituationEventSignalLog>(player);
        issues += WarnIfMissingComponent<PlayerSceneObjectLog>(player);
        issues += WarnIfMissingComponent<PlayerSceneSpawnLog>(player);
        issues += WarnIfMissingComponent<PlayerWorldDiscoveryLog>(player);
        issues += WarnIfMissingComponent<PlayerLocationVisitLog>(player);
        issues += WarnIfMissingComponent<PlayerChronicleLog>(player);
        issues += WarnIfMissingComponent<PlayerNavigationHintLog>(player);
        issues += WarnIfMissingComponent<PlayerAreaProfileLog>(player);
        issues += WarnIfMissingComponent<PlayerCalendarLog>(player);
        issues += WarnIfMissingComponent<PlayerBattleModeSettings>(player);
        issues += WarnIfMissingComponent<PlayerBattleRuleLog>(player);
        issues += WarnIfMissingComponent<PlayerCompetitionLog>(player);
        issues += WarnIfMissingComponent<PlayerCompetitionRankingLog>(player);
        issues += WarnIfMissingComponent<PlayerCompetitionHonorLog>(player);
        issues += WarnIfMissingComponent<PlayerCompetitionSeasonLog>(player);
        issues += WarnIfMissingComponent<PlayerCompetitionBracketLog>(player);
        issues += WarnIfMissingComponent<PlayerCompetitionPrizeLog>(player);
        issues += WarnIfMissingComponent<PlayerCompetitionRegistrationLog>(player);
        issues += WarnIfMissingComponent<PlayerCompetitionInvitationLog>(player);
        issues += WarnIfMissingComponent<PlayerCompetitionVenueLog>(player);
        issues += WarnIfMissingComponent<PlayerSponsorLog>(player);
        issues += WarnIfMissingComponent<PlayerPowerMechanicLog>(player);
        issues += WarnIfMissingComponent<PlayerContestLog>(player);
        issues += WarnIfMissingComponent<PlayerCareerLog>(player);
        issues += WarnIfMissingComponent<PlayerOrganizationLog>(player);
        issues += WarnIfMissingComponent<PlayerAssignmentLog>(player);
        issues += WarnIfMissingComponent<PlayerAccessLog>(player);
        issues += WarnIfMissingComponent<PlayerLawLog>(player);
        issues += WarnIfMissingComponent<PlayerInvestigationLog>(player);
        issues += WarnIfMissingComponent<PlayerNPCMemoryLog>(player);
        issues += WarnIfMissingComponent<PlayerNPCReactionLog>(player);
        issues += WarnIfMissingComponent<PlayerWitnessReportLog>(player);
        issues += WarnIfMissingComponent<PlayerReportPropagationLog>(player);
        issues += WarnIfMissingComponent<PlayerPokemonCareFacilityLog>(player);
        issues += WarnIfMissingComponent<PlayerPokemonAssignmentLog>(player);
        issues += WarnIfMissingComponent<PlayerPokemonHeldItemLog>(player);
        issues += WarnIfMissingComponent<PlayerCompanionExpeditionLog>(player);
        issues += WarnIfMissingComponent<PlayerCompanionExpeditionRouteLog>(player);
        issues += WarnIfMissingComponent<PlayerReputation>(player);
        issues += WarnIfMissingComponent<PlayerRelationships>(player);
        issues += WarnIfMissingComponent<PlayerActivityJournal>(player);
        issues += WarnIfMissingComponent<PlayerLifestyleLog>(player);
        issues += WarnIfMissingComponent<PlayerLifePathLog>(player);
        issues += WarnIfMissingComponent<PlayerMilestones>(player);
        issues += WarnIfMissingComponent<PlayerTitles>(player);
        return issues;
    }

    int WarnIfDuplicate<T>(string label) where T : Object {
        int count = FindObjectsByType<T>(FindObjectsInactive.Include).Length;
        if(count <= 1) {
            return 0;
        }

        GameDebug.Warning($"{label} has {count} instances in loaded scenes.", GameDebugCategory.Validation, this, "RuntimeHealthMonitor");
        return 1;
    }

    int WarnIfNull(Object value, string label, Object context) {
        if(value != null) {
            return 0;
        }

        GameDebug.Warning($"{label} is missing.", GameDebugCategory.Validation, context, "RuntimeHealthMonitor");
        return 1;
    }

    int WarnIfMissingComponent<T>(Component owner) where T : Component {
        if(owner.GetComponent<T>() != null) {
            return 0;
        }

        GameDebug.Warning($"{owner.name} is missing component {typeof(T).Name}.", GameDebugCategory.Validation, owner, "RuntimeHealthMonitor");
        return 1;
    }
}
