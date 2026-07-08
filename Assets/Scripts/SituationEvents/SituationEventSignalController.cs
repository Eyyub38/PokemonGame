using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SituationEventSignalController : MonoBehaviour {
    [Header("Profile")]
    [Tooltip("Signal profile evaluated by this controller.")]
    [SerializeField] SituationEventSignalProfileDefinition profile = null;
    [Tooltip("Optional player override. Empty uses PlayerController.i or the first loaded PlayerController.")]
    [SerializeField] PlayerController playerOverride = null;

    [Header("Context")]
    [Tooltip("Default region context passed into pool/event filters when rules do not override it.")]
    [SerializeField] RegionInfoDefinition regionContext = null;
    [Tooltip("Default activity zone context passed into pool/event filters. Empty can fall back to PlayerActivityContext.CurrentZone.")]
    [SerializeField] ActivityZoneDefinition zoneContext = null;

    [Header("Subscriptions")]
    [Tooltip("If enabled, rules that accept Start are evaluated once at Start.")]
    [SerializeField] bool evaluateOnStart = true;
    [Tooltip("If enabled, rules that accept Time Changed are evaluated on TimeSystem.OnTimeChanged.")]
    [SerializeField] bool evaluateOnTimeChanged = true;
    [Tooltip("If enabled, rules that accept Day Changed are evaluated on TimeSystem.OnDayChanged.")]
    [SerializeField] bool evaluateOnDayChanged = true;
    [Tooltip("If enabled, rules that accept Survival Need Changed are evaluated when player survival needs change.")]
    [SerializeField] bool evaluateOnSurvivalNeedChanged = true;
    [Tooltip("If enabled, rules that accept Pokemon Care Need Changed are evaluated when party Pokemon care needs change.")]
    [SerializeField] bool evaluateOnPokemonCareNeedChanged = true;
    [Tooltip("If enabled, rules that accept Area Profile Changed are evaluated when area profile enter/exit records change.")]
    [SerializeField] bool evaluateOnAreaProfileChanged = true;

    [Header("Logging")]
    [Tooltip("If enabled, creates PlayerSituationEventSignalLog when missing.")]
    [SerializeField] bool createMissingSignalLog = true;
    [Tooltip("If enabled, successful signal evaluations are written to GameDebug.")]
    [SerializeField] bool logSuccessfulEvaluations;
    [Tooltip("If enabled, blocked signal evaluations are written to GameDebug.")]
    [SerializeField] bool logBlockedEvaluations;

    bool timeSubscribed;
    PlayerController subscribedPlayer;
    SurvivalNeedsController survivalNeeds;
    PokemonCareNeedsController pokemonCareNeeds;
    PlayerAreaProfileLog areaProfileLog;

    public SituationEventSignalProfileDefinition Profile => profile;
    public RegionInfoDefinition RegionContext => regionContext;
    public ActivityZoneDefinition ZoneContext => zoneContext;

    void OnEnable() {
        SubscribeTime();
        SubscribePlayerSignals();
    }

    void Start() {
        SubscribeTime();
        SubscribePlayerSignals();
        if(evaluateOnStart) {
            Evaluate(SituationEventSignalTrigger.Start);
        }
    }

    void OnDisable() {
        UnsubscribeTime();
        UnsubscribePlayerSignals();
    }

    [ContextMenu("Evaluate Situation Event Signals")]
    public void EvaluateFromContextMenu() {
        Evaluate(SituationEventSignalTrigger.Manual);
    }

    public List<SituationEventSignalEvaluationResult> Evaluate(SituationEventSignalTrigger trigger) {
        var results = new List<SituationEventSignalEvaluationResult>();
        var player = ResolvePlayer();
        if(player == null || profile == null) {
            return results;
        }

        SubscribePlayerSignals();
        player.GetComponent<PlayerSituationEventLog>()?.PruneExpired();
        var log = GetSignalLog(player);

        foreach(var rule in profile.Rules) {
            if(rule == null) {
                continue;
            }

            var result = EvaluateRule(player, log, rule, trigger);
            results.Add(result);
        }

        return results;
    }

    SituationEventSignalEvaluationResult EvaluateRule(PlayerController player, PlayerSituationEventSignalLog log, SituationEventSignalRule rule, SituationEventSignalTrigger trigger) {
        string sourceId = rule.ResolveSourceId(profile);
        if(!rule.CanEvaluate(player, profile, log, trigger, out var failureMessage)) {
            if(rule.RecordBlockedEvaluations) {
                log?.RecordEvaluation(profile, rule, trigger, sourceId, 0, 0, true, failureMessage);
            }
            LogEvaluation(rule, trigger, false, failureMessage, 0, 0);
            return new SituationEventSignalEvaluationResult(profile, rule, trigger, sourceId, true, failureMessage);
        }

        var region = rule.ResolveRegion(regionContext);
        var zone = rule.ResolveZone(zoneContext != null ? zoneContext : PlayerActivityContext.CurrentZone);
        string sourceName = rule.ResolveSourceName(profile);
        int rolledPools = 0;
        int startedEvents = 0;
        var messages = new List<string>();

        foreach(var pool in rule.Pools) {
            if(pool == null) {
                continue;
            }

            rolledPools++;
            var result = pool.Roll(player, region, zone, sourceId, sourceName, this);
            if(result != null) {
                startedEvents += result.startedEvents;
                if(!string.IsNullOrWhiteSpace(result.failureMessage)) {
                    messages.Add(result.failureMessage);
                }
            }
        }

        string message = startedEvents > 0
            ? $"{rule.DisplayName} started {startedEvents} situation event(s)."
            : messages.FirstOrDefault() ?? $"{rule.DisplayName} rolled {rolledPools} pool(s).";

        log?.RecordEvaluation(profile, rule, trigger, sourceId, rolledPools, startedEvents, false, message);
        LogEvaluation(rule, trigger, true, message, rolledPools, startedEvents);
        return new SituationEventSignalEvaluationResult(profile, rule, trigger, sourceId, false, message) {
            rolledPools = rolledPools,
            startedEvents = startedEvents
        };
    }

    void SubscribeTime() {
        if(timeSubscribed || TimeSystem.i == null) {
            return;
        }

        if(evaluateOnTimeChanged) {
            TimeSystem.i.OnTimeChanged += HandleTimeChanged;
        }

        if(evaluateOnDayChanged) {
            TimeSystem.i.OnDayChanged += HandleDayChanged;
        }

        timeSubscribed = evaluateOnTimeChanged || evaluateOnDayChanged;
    }

    void UnsubscribeTime() {
        if(!timeSubscribed || TimeSystem.i == null) {
            timeSubscribed = false;
            return;
        }

        TimeSystem.i.OnTimeChanged -= HandleTimeChanged;
        TimeSystem.i.OnDayChanged -= HandleDayChanged;
        timeSubscribed = false;
    }

    void SubscribePlayerSignals() {
        var player = ResolvePlayer();
        if(player == null || player == subscribedPlayer) {
            return;
        }

        UnsubscribePlayerSignals();
        subscribedPlayer = player;

        survivalNeeds = player.GetComponent<SurvivalNeedsController>();
        if(evaluateOnSurvivalNeedChanged && survivalNeeds != null) {
            survivalNeeds.OnNeedChanged += HandleSurvivalNeedChanged;
        }

        pokemonCareNeeds = player.GetComponent<PokemonCareNeedsController>();
        if(evaluateOnPokemonCareNeedChanged && pokemonCareNeeds != null) {
            pokemonCareNeeds.OnCareNeedChanged += HandlePokemonCareNeedChanged;
        }

        areaProfileLog = player.GetComponent<PlayerAreaProfileLog>();
        if(evaluateOnAreaProfileChanged && areaProfileLog != null) {
            areaProfileLog.OnAreaEntered += HandleAreaProfileChanged;
            areaProfileLog.OnAreaExited += HandleAreaProfileExited;
        }
    }

    void UnsubscribePlayerSignals() {
        if(survivalNeeds != null) {
            survivalNeeds.OnNeedChanged -= HandleSurvivalNeedChanged;
        }

        if(pokemonCareNeeds != null) {
            pokemonCareNeeds.OnCareNeedChanged -= HandlePokemonCareNeedChanged;
        }

        if(areaProfileLog != null) {
            areaProfileLog.OnAreaEntered -= HandleAreaProfileChanged;
            areaProfileLog.OnAreaExited -= HandleAreaProfileExited;
        }

        survivalNeeds = null;
        pokemonCareNeeds = null;
        areaProfileLog = null;
        subscribedPlayer = null;
    }

    void HandleTimeChanged() {
        Evaluate(SituationEventSignalTrigger.TimeChanged);
    }

    void HandleDayChanged() {
        Evaluate(SituationEventSignalTrigger.DayChanged);
    }

    void HandleSurvivalNeedChanged(SurvivalNeed need) {
        Evaluate(SituationEventSignalTrigger.SurvivalNeedChanged);
    }

    void HandlePokemonCareNeedChanged(Pokemon pokemon, PokemonCareNeedDefinition need, PokemonCareNeedChangeRecord record) {
        Evaluate(SituationEventSignalTrigger.PokemonCareNeedChanged);
    }

    void HandleAreaProfileChanged(PlayerAreaProfileState state) {
        Evaluate(SituationEventSignalTrigger.AreaProfileChanged);
    }

    void HandleAreaProfileExited(PlayerAreaProfileRecord record) {
        Evaluate(SituationEventSignalTrigger.AreaProfileChanged);
    }

    PlayerController ResolvePlayer() {
        if(playerOverride != null) {
            return playerOverride;
        }

        if(PlayerController.i != null) {
            return PlayerController.i;
        }

        return FindAnyObjectByType<PlayerController>(FindObjectsInactive.Include);
    }

    PlayerSituationEventSignalLog GetSignalLog(PlayerController player) {
        if(player == null) {
            return null;
        }

        var log = player.GetComponent<PlayerSituationEventSignalLog>();
        if(log == null && createMissingSignalLog) {
            log = player.gameObject.AddComponent<PlayerSituationEventSignalLog>();
        }
        return log;
    }

    void LogEvaluation(SituationEventSignalRule rule, SituationEventSignalTrigger trigger, bool success, string message, int rolledPools, int startedEvents) {
        if(success && !logSuccessfulEvaluations) {
            return;
        }

        if(!success && !logBlockedEvaluations) {
            return;
        }

        var severity = success ? startedEvents > 0 ? GameDebugSeverity.Info : GameDebugSeverity.Trace : GameDebugSeverity.Trace;
        GameDebugLogger.Ensure().Record(
            severity,
            GameDebugCategory.WorldTrigger,
            $"{rule.DisplayName} via {trigger}: {message} (pools {rolledPools}, events {startedEvents})",
            this,
            "SituationEventSignalController");
    }
}

public class SituationEventSignalEvaluationResult {
    public readonly string profileId;
    public readonly string ruleId;
    public readonly string sourceId;
    public readonly SituationEventSignalTrigger trigger;
    public bool blocked;
    public string message;
    public int rolledPools;
    public int startedEvents;

    public SituationEventSignalEvaluationResult(SituationEventSignalProfileDefinition profile, SituationEventSignalRule rule, SituationEventSignalTrigger trigger, string sourceId, bool blocked, string message) {
        profileId = profile != null ? profile.Id : string.Empty;
        ruleId = rule != null ? rule.RuleId : string.Empty;
        this.trigger = trigger;
        this.sourceId = sourceId;
        this.blocked = blocked;
        this.message = message;
    }
}
