using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BattleRuleManager : MonoBehaviour {
    [Tooltip("If enabled, this manager survives scene loads.")]
    [SerializeField] bool dontDestroyOnLoad = true;
    [Tooltip("If enabled, rule start/end actions are written to GameDebug.")]
    [SerializeField] bool writeDebugLogs;

    public static BattleRuleManager i { get; private set; }
    public BattleRuleContext CurrentContext { get; private set; }
    public bool HasActiveRule => CurrentContext != null && CurrentContext.IsActive;

    public event Action<BattleRuleContext> OnRuleContextStarted;
    public event Action<BattleRuleContext, bool> OnRuleContextCompleted;
    public event Action OnRuleContextCleared;

    void Awake() {
        if(i != null && i != this) {
            Destroy(gameObject);
            return;
        }

        i = this;
        if(dontDestroyOnLoad) {
            DontDestroyOnLoad(gameObject);
        }
    }

    public static BattleRuleManager Ensure() {
        if(i != null) {
            return i;
        }

        var existing = FindAnyObjectByType<BattleRuleManager>();
        if(existing != null) {
            i = existing;
            return i;
        }

        var go = new GameObject("BattleRuleManager");
        return go.AddComponent<BattleRuleManager>();
    }

    public bool PrepareChallenge(PlayerController player, BattleChallengeDefinition challenge, BattleRuleSetDefinition selectedRuleSet, string sourceId, out string failureMessage) {
        return PrepareChallenge(player, challenge, selectedRuleSet, null, sourceId, out failureMessage);
    }

    public bool PrepareChallenge(PlayerController player, BattleChallengeDefinition challenge, BattleRuleSetDefinition selectedRuleSet, BattleModeDefinition selectedBattleMode, string sourceId, out string failureMessage) {
        if(challenge == null) {
            failureMessage = "No battle challenge is assigned.";
            return false;
        }

        var ruleSet = challenge.ResolveRuleSet(selectedRuleSet);
        if(!challenge.CanStart(player, ruleSet, out failureMessage)) {
            ruleSet?.PublishRejected(player, failureMessage, sourceId);
            return false;
        }

        var battleMode = challenge.ResolveBattleMode(player, selectedBattleMode);
        string fallbackMessage = null;
        if(battleMode != null && !battleMode.CanRunWithCurrentBattleSystem(out failureMessage, out fallbackMessage)) {
            return false;
        }

        CurrentContext = new BattleRuleContext(player, challenge, ruleSet, battleMode, sourceId);
        CurrentContext.Start();
        player?.GetComponent<PlayerBattleRuleLog>()?.RecordChallengeStarted(challenge, ruleSet, battleMode, sourceId);
        ruleSet.PublishAccepted(player, sourceId);
        challenge.PublishStarted(player, ruleSet, sourceId, battleMode);
        OnRuleContextStarted?.Invoke(CurrentContext);

        if(writeDebugLogs) {
            GameDebug.Step($"Battle rule context started: {challenge.DisplayName} / {ruleSet.DisplayName}.", GameDebugCategory.BattleRule, this, "BattleRuleManager");
            if(!string.IsNullOrWhiteSpace(fallbackMessage)) {
                GameDebug.Warning(fallbackMessage, GameDebugCategory.BattleRule, this, "BattleRuleManager");
            }
        }

        failureMessage = null;
        return true;
    }

    public void CompleteCurrent(bool won) {
        if(CurrentContext == null) {
            return;
        }

        var context = CurrentContext;
        context.Complete(won);
        context.Challenge?.ApplyCompletionRewards(context.Player, won);
        context.Player?.GetComponent<PlayerBattleRuleLog>()?.RecordChallengeCompleted(context.Challenge, context.RuleSet, context.BattleMode, won, context.SourceId);
        context.RuleSet?.PublishCompleted(context.Player, won, context.SourceId);
        context.Challenge?.PublishCompleted(context.Player, context.RuleSet, won, context.SourceId, context.BattleMode);
        OnRuleContextCompleted?.Invoke(context, won);

        if(writeDebugLogs) {
            GameDebug.Step($"Battle rule context completed: won={won}.", GameDebugCategory.BattleRule, this, "BattleRuleManager");
        }

        CurrentContext = null;
        OnRuleContextCleared?.Invoke();
    }

    public void ClearCurrent() {
        CurrentContext = null;
        OnRuleContextCleared?.Invoke();
    }
}

[Serializable]
public class BattleRuleContext {
    [Tooltip("Player who accepted this rule context.")]
    [SerializeField] PlayerController player;
    [Tooltip("Challenge represented by this battle context.")]
    [SerializeField] BattleChallengeDefinition challenge;
    [Tooltip("Rule set enforced by this battle context.")]
    [SerializeField] BattleRuleSetDefinition ruleSet;
    [Tooltip("Battle mode selected for this battle context. Empty means classic current behavior.")]
    [SerializeField] BattleModeDefinition battleMode;
    [Tooltip("Source id that started this rule context, such as trainer or board id.")]
    [SerializeField] string sourceId;
    [Tooltip("Whether this rule context is currently active.")]
    [SerializeField] bool isActive;
    [Tooltip("Number of completed turns tracked by this context.")]
    [Min(0)]
    [SerializeField] int completedTurns;
    [Tooltip("Number of player item uses tracked by this context.")]
    [Min(0)]
    [SerializeField] int playerItemUses;
    [Tooltip("Number of opponent item uses tracked by this context.")]
    [Min(0)]
    [SerializeField] int opponentItemUses;
    [Tooltip("Number of voluntary player switches tracked by this context.")]
    [Min(0)]
    [SerializeField] int playerSwitches;
    [Tooltip("Number of voluntary opponent switches tracked by this context.")]
    [Min(0)]
    [SerializeField] int opponentSwitches;
    [Tooltip("Power mechanic uses tracked during this battle context.")]
    [SerializeField] List<BattleRulePowerMechanicUseState> powerMechanicUses = new List<BattleRulePowerMechanicUseState>();

    public PlayerController Player => player;
    public BattleChallengeDefinition Challenge => challenge;
    public BattleRuleSetDefinition RuleSet => ruleSet;
    public BattleModeDefinition BattleMode => battleMode;
    public PokemonVitalProfileDefinition VitalProfile => ruleSet != null ? ruleSet.VitalProfile : null;
    public bool SpendCoreStaminaOnBattleEntry => ruleSet == null || ruleSet.SpendCoreStaminaOnBattleEntry;
    public bool CapBattleHpByCoreHealth => ruleSet == null || ruleSet.CapBattleHpByCoreHealth;
    public string SourceId => sourceId;
    public bool IsActive => isActive;
    public int CompletedTurns => completedTurns;
    public int PlayerItemUses => playerItemUses;
    public int OpponentItemUses => opponentItemUses;
    public int PlayerSwitches => playerSwitches;
    public int OpponentSwitches => opponentSwitches;
    public IReadOnlyList<BattleRulePowerMechanicUseState> PowerMechanicUses => powerMechanicUses;

    public BattleRuleContext(PlayerController player, BattleChallengeDefinition challenge, BattleRuleSetDefinition ruleSet, BattleModeDefinition battleMode, string sourceId) {
        this.player = player;
        this.challenge = challenge;
        this.ruleSet = ruleSet;
        this.battleMode = battleMode;
        this.sourceId = sourceId;
    }

    public void Start() {
        isActive = true;
        completedTurns = 0;
        playerItemUses = 0;
        opponentItemUses = 0;
        playerSwitches = 0;
        opponentSwitches = 0;
        powerMechanicUses = new List<BattleRulePowerMechanicUseState>();
    }

    public void Complete(bool won) {
        isActive = false;
    }

    public bool CanUseItem(bool isPlayer, ItemBase item, out string failureMessage) {
        failureMessage = null;
        int usedCount = isPlayer ? playerItemUses : opponentItemUses;
        return ruleSet == null || ruleSet.CanUseItem(isPlayer, usedCount, item, out failureMessage);
    }

    public bool CanSwitch(bool isPlayer, out string failureMessage) {
        failureMessage = null;
        int switchCount = isPlayer ? playerSwitches : opponentSwitches;
        return ruleSet == null || ruleSet.CanSwitch(isPlayer, switchCount, out failureMessage);
    }

    public bool CanRunAway(out string failureMessage) {
        failureMessage = null;
        return ruleSet == null || ruleSet.CanRunAway(out failureMessage);
    }

    public bool CanUsePowerMechanic(bool isPlayer, PowerMechanicDefinition mechanic, out string failureMessage) {
        failureMessage = null;
        if(ruleSet == null) {
            return true;
        }

        int totalUsed = GetPowerMechanicUseCount(isPlayer);
        int kindUsed = GetPowerMechanicUseCount(isPlayer, mechanic != null ? mechanic.Kind : PowerMechanicKind.Custom);
        return ruleSet.CanUsePowerMechanic(isPlayer, mechanic, totalUsed, kindUsed, out failureMessage);
    }

    public void RecordItemUse(bool isPlayer) {
        if(isPlayer) {
            playerItemUses++;
        } else {
            opponentItemUses++;
        }
    }

    public void RecordSwitch(bool isPlayer) {
        if(isPlayer) {
            playerSwitches++;
        } else {
            opponentSwitches++;
        }
    }

    public void RecordPowerMechanicUse(bool isPlayer, PowerMechanicDefinition mechanic) {
        if(mechanic == null) {
            return;
        }

        powerMechanicUses.Add(new BattleRulePowerMechanicUseState {
            isPlayer = isPlayer,
            mechanicId = mechanic.Id,
            mechanicName = mechanic.DisplayName,
            kind = mechanic.Kind,
            turnNumber = completedTurns + 1
        });
    }

    public int GetPowerMechanicUseCount(bool isPlayer) {
        return powerMechanicUses.Count(use => use != null && use.isPlayer == isPlayer);
    }

    public int GetPowerMechanicUseCount(bool isPlayer, PowerMechanicKind kind) {
        return powerMechanicUses.Count(use => use != null && use.isPlayer == isPlayer && use.kind == kind);
    }

    public bool RecordTurnCompleted(out string ruleMessage) {
        completedTurns++;
        if(ruleSet != null && ruleSet.IsTurnLimitReached(completedTurns)) {
            ruleMessage = $"Turn limit reached: {ruleSet.TurnLimit}.";
            return true;
        }

        ruleMessage = null;
        return false;
    }
}

[Serializable]
public class BattleRulePowerMechanicUseState {
    [Tooltip("If enabled, this use belongs to the player side. If disabled, opponent side.")]
    public bool isPlayer;
    [Tooltip("Saved mechanic id.")]
    public string mechanicId;
    [Tooltip("Saved mechanic display name for fallback/debug output.")]
    public string mechanicName;
    [Tooltip("Saved mechanic kind.")]
    public PowerMechanicKind kind;
    [Tooltip("Battle turn number where this mechanic was used.")]
    public int turnNumber;
}
