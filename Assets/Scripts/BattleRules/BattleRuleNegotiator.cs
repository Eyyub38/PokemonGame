using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum BattleRuleNegotiationMode {
    DefaultOnly,
    PlayerCanChooseListedRules,
    RequireUnlockedListedRule
}

public class BattleRuleNegotiator : MonoBehaviour {
    [Header("Challenge")]
    [Tooltip("Challenge data used when this NPC/trainer starts a rule-based battle.")]
    [SerializeField] BattleChallengeDefinition challenge;
    [Tooltip("Rule set forced by this negotiator. Empty uses the challenge default or selected rule.")]
    [SerializeField] BattleRuleSetDefinition forcedRuleSet;
    [Tooltip("Battle mode forced by this negotiator. Empty uses challenge/player preference.")]
    [SerializeField] BattleModeDefinition forcedBattleMode;
    [Tooltip("How this negotiator chooses and accepts battle rules.")]
    [SerializeField] BattleRuleNegotiationMode negotiationMode = BattleRuleNegotiationMode.DefaultOnly;
    [Tooltip("Optional extra rule sets this specific NPC can negotiate.")]
    [SerializeField] List<BattleRuleSetDefinition> localRuleOptions = new List<BattleRuleSetDefinition>();

    [Header("Messages")]
    [Tooltip("Message used when no challenge is assigned.")]
    [SerializeField] string missingChallengeMessage = "This trainer has no challenge assigned.";
    [Tooltip("Message used when the selected rule is not accepted by this trainer.")]
    [SerializeField] string ruleNotAcceptedMessage = "That rule is not accepted here.";

    [Header("Debug")]
    [Tooltip("If enabled, blocked rule attempts are written to GameDebug.")]
    [SerializeField] bool logBlockedAttempts;

    public BattleChallengeDefinition Challenge => challenge;
    public BattleRuleSetDefinition ForcedRuleSet => forcedRuleSet;
    public BattleModeDefinition ForcedBattleMode => forcedBattleMode;
    public BattleRuleNegotiationMode NegotiationMode => negotiationMode;
    public IReadOnlyList<BattleRuleSetDefinition> LocalRuleOptions => localRuleOptions;

    public List<BattleRuleSetDefinition> GetAcceptedRuleSets(PlayerController player) {
        if(challenge == null) {
            return new List<BattleRuleSetDefinition>();
        }

        if(negotiationMode == BattleRuleNegotiationMode.DefaultOnly) {
            var rule = forcedRuleSet != null ? forcedRuleSet : challenge.DefaultRuleSet;
            return rule != null && rule.CanAccess(player, out _) ? new List<BattleRuleSetDefinition> { rule } : new List<BattleRuleSetDefinition>();
        }

        var rules = challenge.GetAvailableRuleSets(player);
        if(localRuleOptions != null) {
            rules.AddRange(localRuleOptions.Where(rule => rule != null && rule.CanAccess(player, out _)));
        }

        if(forcedRuleSet != null) {
            rules = rules.Where(rule => rule == forcedRuleSet).ToList();
        }

        rules = rules.Distinct().ToList();

        if(negotiationMode == BattleRuleNegotiationMode.RequireUnlockedListedRule) {
            var log = player != null ? player.GetComponent<PlayerBattleRuleLog>() : null;
            rules = rules.Where(rule => log != null && log.HasUnlockedRuleSet(rule)).ToList();
        }

        return rules;
    }

    public List<BattleModeDefinition> GetAcceptedBattleModes(PlayerController player) {
        if(challenge == null) {
            return new List<BattleModeDefinition>();
        }

        var modes = challenge.GetAvailableBattleModes(player);
        var preferred = player != null ? player.GetComponent<PlayerBattleModeSettings>()?.ResolvePreferredMode(player, challenge) : null;
        if(preferred != null && challenge.IsBattleModeAllowed(player, preferred)) {
            modes.Add(preferred);
        }

        if(forcedBattleMode != null) {
            modes = modes.Where(mode => mode == forcedBattleMode).ToList();
            if(modes.Count == 0 && challenge.IsBattleModeAllowed(player, forcedBattleMode)) {
                modes.Add(forcedBattleMode);
            }
        }

        return modes.Distinct().ToList();
    }

    public bool TryPrepareBattle(PlayerController player, out string failureMessage) {
        return TryPrepareBattle(player, forcedRuleSet, forcedBattleMode, out failureMessage);
    }

    public bool TryPrepareBattle(PlayerController player, BattleRuleSetDefinition selectedRuleSet, out string failureMessage) {
        return TryPrepareBattle(player, selectedRuleSet, forcedBattleMode, out failureMessage);
    }

    public bool TryPrepareBattle(PlayerController player, BattleRuleSetDefinition selectedRuleSet, BattleModeDefinition selectedBattleMode, out string failureMessage) {
        if(challenge == null) {
            failureMessage = missingChallengeMessage;
            LogBlocked(player, failureMessage);
            return false;
        }

        var ruleSet = ResolveSelectedRule(player, selectedRuleSet);
        if(ruleSet == null) {
            failureMessage = ruleNotAcceptedMessage;
            LogBlocked(player, failureMessage);
            return false;
        }

        var battleMode = forcedBattleMode != null ? forcedBattleMode : selectedBattleMode;
        if(!BattleRuleManager.Ensure().PrepareChallenge(player, challenge, ruleSet, battleMode, SourceId, out failureMessage)) {
            LogBlocked(player, failureMessage);
            return false;
        }

        return true;
    }

    BattleRuleSetDefinition ResolveSelectedRule(PlayerController player, BattleRuleSetDefinition selectedRuleSet) {
        if(forcedRuleSet != null) {
            return forcedRuleSet;
        }

        if(selectedRuleSet == null && negotiationMode == BattleRuleNegotiationMode.DefaultOnly) {
            selectedRuleSet = challenge != null ? challenge.DefaultRuleSet : null;
        }

        var accepted = GetAcceptedRuleSets(player);
        if(selectedRuleSet != null && accepted.Contains(selectedRuleSet)) {
            return selectedRuleSet;
        }

        return accepted.FirstOrDefault();
    }

    string SourceId => $"trainer:{gameObject.name}";

    void LogBlocked(PlayerController player, string failureMessage) {
        if(!logBlockedAttempts) {
            return;
        }

        GameDebug.Warning(failureMessage, GameDebugCategory.BattleRule, player != null ? player : this, "BattleRuleNegotiator");
    }
}
