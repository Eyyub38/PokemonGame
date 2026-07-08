using UnityEngine;

public enum BattleRuleRequirementMode {
    RuleSetUnlocked,
    ChallengeStarted,
    ChallengeCompleted,
    ChallengeWon,
    ChallengeLost,
    ActiveRuleSet,
    ActiveChallenge,
    RuleTagUnlocked
}

[CreateAssetMenu(menuName = "Activities/Requirements/Battle Rule Requirement")]
public class BattleRuleRequirement : ActivityRequirement {
    [Tooltip("Which battle rule or challenge value this requirement checks.")]
    [SerializeField] BattleRuleRequirementMode mode = BattleRuleRequirementMode.ChallengeCompleted;
    [Tooltip("Rule set checked by rule-specific modes.")]
    [SerializeField] BattleRuleSetDefinition ruleSet;
    [Tooltip("Challenge checked by challenge-specific modes.")]
    [SerializeField] BattleChallengeDefinition challenge;
    [Tooltip("Tag checked by Rule Tag Unlocked mode.")]
    [SerializeField] string tag;
    [Tooltip("Minimum count required by count-based challenge modes.")]
    [Min(0)]
    [SerializeField] int requiredCount = 1;
    [Tooltip("If enabled, the selected battle rule condition must be true. If disabled, it must be false.")]
    [SerializeField] bool mustBeMet = true;

    public override bool IsMet(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerBattleRuleLog>() : null;
        var context = BattleRuleManager.i != null ? BattleRuleManager.i.CurrentContext : null;
        bool result = mode switch {
            BattleRuleRequirementMode.RuleSetUnlocked => log != null && log.HasUnlockedRuleSet(ruleSet),
            BattleRuleRequirementMode.ChallengeStarted => log != null && log.GetStartedCount(challenge, ruleSet) >= Mathf.Max(0, requiredCount),
            BattleRuleRequirementMode.ChallengeWon => log != null && log.GetWinCount(challenge, ruleSet) >= Mathf.Max(0, requiredCount),
            BattleRuleRequirementMode.ChallengeLost => log != null && log.GetLossCount(challenge, ruleSet) >= Mathf.Max(0, requiredCount),
            BattleRuleRequirementMode.ActiveRuleSet => context != null && context.IsActive && context.RuleSet == ruleSet,
            BattleRuleRequirementMode.ActiveChallenge => context != null && context.IsActive && context.Challenge == challenge,
            BattleRuleRequirementMode.RuleTagUnlocked => HasUnlockedRuleWithTag(log),
            _ => log != null && log.GetCompletedCount(challenge, ruleSet) >= Mathf.Max(0, requiredCount)
        };

        return mustBeMet ? result : !result;
    }

    bool HasUnlockedRuleWithTag(PlayerBattleRuleLog log) {
        if(log == null || string.IsNullOrWhiteSpace(tag)) {
            return false;
        }

        foreach(var candidate in Resources.LoadAll<BattleRuleSetDefinition>("")) {
            if(candidate != null && candidate.HasTag(tag) && log.HasUnlockedRuleSet(candidate)) {
                return true;
            }
        }

        return false;
    }
}
