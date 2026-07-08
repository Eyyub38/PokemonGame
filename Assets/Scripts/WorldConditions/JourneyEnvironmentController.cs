using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class JourneyEnvironmentController : MonoBehaviour {
    [Header("Profile")]
    [Tooltip("Journey environment profile evaluated by this controller.")]
    [SerializeField] JourneyEnvironmentProfileDefinition profile = null;
    [Tooltip("Optional player override. Empty uses PlayerController.i or the first loaded PlayerController.")]
    [SerializeField] PlayerController playerOverride = null;

    [Header("Context")]
    [Tooltip("Default region context used when a rule does not override it. Empty can fall back to the active area profile.")]
    [SerializeField] RegionInfoDefinition regionContext = null;
    [Tooltip("Default activity zone context used when a rule does not override it. Empty can fall back to PlayerActivityContext.CurrentZone.")]
    [SerializeField] ActivityZoneDefinition zoneContext = null;

    [Header("Subscriptions")]
    [Tooltip("If enabled, rules that accept Start are evaluated once at Start.")]
    [SerializeField] bool evaluateOnStart = true;
    [Tooltip("If enabled, rules that accept Time Changed are evaluated once per in-game hour.")]
    [SerializeField] bool evaluateOnTimeChanged = true;
    [Tooltip("If enabled, rules that accept Area Profile Changed are evaluated when active area profiles change.")]
    [SerializeField] bool evaluateOnAreaProfileChanged = true;
    [Tooltip("If enabled, rules that accept World Condition Changed are evaluated when world conditions change.")]
    [SerializeField] bool evaluateOnWorldConditionChanged = true;

    [Header("Logging")]
    [Tooltip("If enabled, creates PlayerJourneyEnvironmentLog when missing.")]
    [SerializeField] bool createMissingEnvironmentLog = true;
    [Tooltip("If enabled, successful evaluations are written to GameDebug.")]
    [SerializeField] bool logSuccessfulEvaluations;
    [Tooltip("If enabled, blocked evaluations are written to GameDebug.")]
    [SerializeField] bool logBlockedEvaluations;

    int minuteBuffer;
    bool timeSubscribed;
    PlayerController subscribedPlayer;
    PlayerAreaProfileLog areaProfileLog;
    PlayerWorldConditionLog worldConditionLog;

    public JourneyEnvironmentProfileDefinition Profile => profile;
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
            Evaluate(JourneyEnvironmentEvaluationTrigger.Start);
        }
    }

    void OnDisable() {
        UnsubscribeTime();
        UnsubscribePlayerSignals();
    }

    [ContextMenu("Evaluate Journey Environment")]
    public void EvaluateFromContextMenu() {
        Evaluate(JourneyEnvironmentEvaluationTrigger.Manual);
    }

    public List<JourneyEnvironmentEvaluationResult> Evaluate(JourneyEnvironmentEvaluationTrigger trigger, int hours = 1) {
        var results = new List<JourneyEnvironmentEvaluationResult>();
        var player = ResolvePlayer();
        if(player == null || profile == null) {
            return results;
        }

        SubscribePlayerSignals();
        var log = GetEnvironmentLog(player);
        var fallbackRegion = ResolveRegion(player);
        var fallbackZone = ResolveZone();

        foreach(var rule in profile.Rules) {
            if(rule == null) {
                continue;
            }

            results.Add(EvaluateRule(player, log, rule, trigger, Mathf.Max(1, hours), fallbackRegion, fallbackZone));
        }

        return results;
    }

    JourneyEnvironmentEvaluationResult EvaluateRule(
        PlayerController player,
        PlayerJourneyEnvironmentLog log,
        JourneyEnvironmentRule rule,
        JourneyEnvironmentEvaluationTrigger trigger,
        int hours,
        RegionInfoDefinition fallbackRegion,
        ActivityZoneDefinition fallbackZone) {
        var region = rule.ResolveRegion(fallbackRegion);
        var zone = rule.ResolveZone(fallbackZone);
        string sourceId = rule.ResolveSourceId(profile);
        string sourceName = rule.ResolveSourceName(profile);

        if(!rule.CanEvaluate(player, profile, log, trigger, region, zone, out var failureMessage)) {
            if(rule.RecordBlockedEvaluations) {
                log?.RecordEvaluation(profile, rule, trigger, sourceId, region, zone, 0, 0, 0, 0, 0, true, failureMessage);
            }

            LogEvaluation(rule, trigger, false, failureMessage, 0, 0, 0, 0);
            return new JourneyEnvironmentEvaluationResult(profile, rule, trigger, sourceId, true, failureMessage);
        }

        int survivalChanges = ApplySurvivalChanges(player, rule, hours, sourceId);
        int pokemonCareChanges = ApplyPokemonCareChanges(player, rule, hours, sourceId);
        int rolledPools = 0;
        int startedEvents = 0;
        int lifePathRewardsApplied = ApplyLifePathRewards(player, rule, sourceId, sourceName);
        var messages = new List<string>();

        foreach(var pool in rule.SituationEventPools) {
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

        string message = BuildSuccessMessage(rule, survivalChanges, pokemonCareChanges, rolledPools, startedEvents, lifePathRewardsApplied, messages);
        log?.RecordEvaluation(profile, rule, trigger, sourceId, region, zone, survivalChanges, pokemonCareChanges, rolledPools, startedEvents, lifePathRewardsApplied, false, message);
        LogEvaluation(rule, trigger, true, message, survivalChanges, pokemonCareChanges, rolledPools, startedEvents);

        return new JourneyEnvironmentEvaluationResult(profile, rule, trigger, sourceId, false, message) {
            survivalChanges = survivalChanges,
            pokemonCareChanges = pokemonCareChanges,
            rolledPools = rolledPools,
            startedEvents = startedEvents,
            lifePathRewardsApplied = lifePathRewardsApplied
        };
    }

    int ApplySurvivalChanges(PlayerController player, JourneyEnvironmentRule rule, int hours, string sourceId) {
        var survival = player != null ? player.GetComponent<SurvivalNeedsController>() : null;
        if(survival == null) {
            return 0;
        }

        int applied = 0;
        foreach(var change in rule.SurvivalNeedChanges) {
            if(change?.Need == null || change.AmountPerHour == 0) {
                continue;
            }

            if(survival.TryChangeNeed(change.Need, change.AmountPerHour * hours, sourceId, out _)) {
                applied++;
            }
        }

        return applied;
    }

    int ApplyPokemonCareChanges(PlayerController player, JourneyEnvironmentRule rule, int hours, string sourceId) {
        var care = player != null ? player.GetComponent<PokemonCareNeedsController>() : null;
        if(care == null || rule.PokemonCareNeedChanges.Count == 0) {
            return 0;
        }

        int applied = 0;
        foreach(var pokemon in ResolvePokemonTargets(player, rule.PokemonTargetMode)) {
            foreach(var change in rule.PokemonCareNeedChanges) {
                if(change?.Need == null || change.AmountPerHour == 0) {
                    continue;
                }

                if(care.TryChangeNeed(pokemon, change.Need, change.AmountPerHour * hours, sourceId, change.Context, out _)) {
                    applied++;
                }
            }
        }

        return applied;
    }

    IEnumerable<Pokemon> ResolvePokemonTargets(PlayerController player, JourneyEnvironmentPokemonTargetMode targetMode) {
        var party = player != null ? player.GetComponent<PokemonParty>() : null;
        if(party?.Pokemons == null) {
            return Enumerable.Empty<Pokemon>();
        }

        return targetMode switch {
            JourneyEnvironmentPokemonTargetMode.LeadPokemon => party.Pokemons.Where(pokemon => pokemon != null).Take(1),
            JourneyEnvironmentPokemonTargetMode.HealthyParty => party.Pokemons.Where(pokemon => pokemon != null && pokemon.HP > 0),
            JourneyEnvironmentPokemonTargetMode.FaintedParty => party.Pokemons.Where(pokemon => pokemon != null && pokemon.HP <= 0),
            _ => party.Pokemons.Where(pokemon => pokemon != null)
        };
    }

    int ApplyLifePathRewards(PlayerController player, JourneyEnvironmentRule rule, string sourceId, string sourceName) {
        if(player == null || rule.LifePathRewards.Count == 0) {
            return 0;
        }

        int payloadCount = rule.LifePathRewards.Count(reward => reward != null && reward.lifePath != null && reward.HasAnyPayload);
        if(payloadCount > 0) {
            player.GetComponent<PlayerLifePathLog>()?.ApplyRewards(rule.LifePathRewards, sourceId, sourceName, this);
        }

        return payloadCount;
    }

    string BuildSuccessMessage(
        JourneyEnvironmentRule rule,
        int survivalChanges,
        int pokemonCareChanges,
        int rolledPools,
        int startedEvents,
        int lifePathRewardsApplied,
        List<string> poolMessages) {
        if(survivalChanges + pokemonCareChanges + startedEvents + lifePathRewardsApplied > 0) {
            return $"{rule.DisplayName} applied survival {survivalChanges}, care {pokemonCareChanges}, events {startedEvents}, rewards {lifePathRewardsApplied}.";
        }

        return poolMessages.FirstOrDefault() ?? $"{rule.DisplayName} evaluated with {rolledPools} pool(s).";
    }

    void SubscribeTime() {
        if(timeSubscribed || TimeSystem.i == null || !evaluateOnTimeChanged) {
            return;
        }

        TimeSystem.i.OnTimeChanged += HandleTimeChanged;
        timeSubscribed = true;
    }

    void UnsubscribeTime() {
        if(!timeSubscribed || TimeSystem.i == null) {
            timeSubscribed = false;
            return;
        }

        TimeSystem.i.OnTimeChanged -= HandleTimeChanged;
        timeSubscribed = false;
    }

    void SubscribePlayerSignals() {
        var player = ResolvePlayer();
        if(player == null || player == subscribedPlayer) {
            return;
        }

        UnsubscribePlayerSignals();
        subscribedPlayer = player;

        areaProfileLog = player.GetComponent<PlayerAreaProfileLog>();
        if(evaluateOnAreaProfileChanged && areaProfileLog != null) {
            areaProfileLog.OnAreaEntered += HandleAreaProfileChanged;
            areaProfileLog.OnAreaExited += HandleAreaProfileExited;
        }

        worldConditionLog = player.GetComponent<PlayerWorldConditionLog>();
        if(evaluateOnWorldConditionChanged && worldConditionLog != null) {
            worldConditionLog.OnWorldConditionsChanged += HandleWorldConditionsChanged;
        }
    }

    void UnsubscribePlayerSignals() {
        if(areaProfileLog != null) {
            areaProfileLog.OnAreaEntered -= HandleAreaProfileChanged;
            areaProfileLog.OnAreaExited -= HandleAreaProfileExited;
        }

        if(worldConditionLog != null) {
            worldConditionLog.OnWorldConditionsChanged -= HandleWorldConditionsChanged;
        }

        areaProfileLog = null;
        worldConditionLog = null;
        subscribedPlayer = null;
    }

    void HandleTimeChanged() {
        minuteBuffer++;
        if(minuteBuffer < 60) {
            return;
        }

        int hours = minuteBuffer / 60;
        minuteBuffer %= 60;
        Evaluate(JourneyEnvironmentEvaluationTrigger.TimeChanged, hours);
    }

    void HandleAreaProfileChanged(PlayerAreaProfileState state) {
        Evaluate(JourneyEnvironmentEvaluationTrigger.AreaProfileChanged);
    }

    void HandleAreaProfileExited(PlayerAreaProfileRecord record) {
        Evaluate(JourneyEnvironmentEvaluationTrigger.AreaProfileChanged);
    }

    void HandleWorldConditionsChanged() {
        Evaluate(JourneyEnvironmentEvaluationTrigger.WorldConditionChanged);
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

    RegionInfoDefinition ResolveRegion(PlayerController player) {
        if(regionContext != null) {
            return regionContext;
        }

        var areaLog = player != null ? player.GetComponent<PlayerAreaProfileLog>() : null;
        var activeArea = areaLog?.ActiveAreas?.LastOrDefault(state => state != null && !string.IsNullOrWhiteSpace(state.regionId));
        return activeArea?.ResolveDefinition()?.Region;
    }

    ActivityZoneDefinition ResolveZone() {
        return zoneContext != null ? zoneContext : PlayerActivityContext.CurrentZone;
    }

    PlayerJourneyEnvironmentLog GetEnvironmentLog(PlayerController player) {
        if(player == null) {
            return null;
        }

        var log = player.GetComponent<PlayerJourneyEnvironmentLog>();
        if(log == null && createMissingEnvironmentLog) {
            log = player.gameObject.AddComponent<PlayerJourneyEnvironmentLog>();
        }

        return log;
    }

    void LogEvaluation(
        JourneyEnvironmentRule rule,
        JourneyEnvironmentEvaluationTrigger trigger,
        bool success,
        string message,
        int survivalChanges,
        int pokemonCareChanges,
        int rolledPools,
        int startedEvents) {
        if(success && !logSuccessfulEvaluations) {
            return;
        }

        if(!success && !logBlockedEvaluations) {
            return;
        }

        var severity = success ? GameDebugSeverity.Trace : GameDebugSeverity.Trace;
        GameDebugLogger.Ensure().Record(
            severity,
            GameDebugCategory.WorldTrigger,
            $"{rule.DisplayName} via {trigger}: {message} (survival {survivalChanges}, care {pokemonCareChanges}, pools {rolledPools}, events {startedEvents})",
            this,
            "JourneyEnvironmentController");
    }
}

public class JourneyEnvironmentEvaluationResult {
    public readonly string profileId;
    public readonly string ruleId;
    public readonly string sourceId;
    public readonly JourneyEnvironmentEvaluationTrigger trigger;
    public bool blocked;
    public string message;
    public int survivalChanges;
    public int pokemonCareChanges;
    public int rolledPools;
    public int startedEvents;
    public int lifePathRewardsApplied;

    public JourneyEnvironmentEvaluationResult(JourneyEnvironmentProfileDefinition profile, JourneyEnvironmentRule rule, JourneyEnvironmentEvaluationTrigger trigger, string sourceId, bool blocked, string message) {
        profileId = profile != null ? profile.Id : string.Empty;
        ruleId = rule != null ? rule.RuleId : string.Empty;
        this.trigger = trigger;
        this.sourceId = sourceId;
        this.blocked = blocked;
        this.message = message;
    }
}
