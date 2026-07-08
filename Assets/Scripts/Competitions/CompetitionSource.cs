using System.Collections.Generic;
using UnityEngine;

public class CompetitionSource : MonoBehaviour, IPlayerTriggerable {
    [Header("Competition")]
    [Tooltip("Competition represented by this NPC, counter, gate, terminal or region object.")]
    [SerializeField] CompetitionDefinition competition;
    [Tooltip("Optional default challenge used by helper methods and future UI selection.")]
    [SerializeField] BattleChallengeDefinition defaultChallenge;
    [Tooltip("Optional default rule set used by helper methods and future UI selection.")]
    [SerializeField] BattleRuleSetDefinition defaultRuleSet;
    [Tooltip("Short source id written into logs when this component starts or records competition progress.")]
    [SerializeField] string sourceId = "competition-source";

    [Header("Trigger")]
    [Tooltip("If enabled, player trigger attempts to enter/start the competition.")]
    [SerializeField] bool beginOnPlayerTrigger = true;
    [Tooltip("If enabled, this trigger can be called repeatedly by the player.")]
    [SerializeField] bool triggerRepeatedly = true;
    [Tooltip("If enabled, blocked trigger attempts are written to GameDebug.")]
    [SerializeField] bool logBlockedAttempts = true;
    [Tooltip("If enabled, successful trigger attempts are written to GameDebug.")]
    [SerializeField] bool logSuccessfulAttempts;

    public bool TriggerRepeatedly => triggerRepeatedly;
    public CompetitionDefinition Competition => competition;
    public BattleChallengeDefinition DefaultChallenge => defaultChallenge;
    public BattleRuleSetDefinition DefaultRuleSet => defaultRuleSet;

    public void OnPlayerTriggered(PlayerController player) {
        if(!beginOnPlayerTrigger) {
            return;
        }

        TryBegin(player, out _);
    }

    public bool CanBegin(PlayerController player, out string failureMessage) {
        if(competition == null) {
            failureMessage = "No competition is assigned.";
            return false;
        }

        return competition.CanEnter(player, out failureMessage);
    }

    public bool TryBegin(PlayerController player, out string failureMessage) {
        if(competition == null) {
            failureMessage = "No competition is assigned.";
            LogBlocked(failureMessage);
            return false;
        }

        if(!competition.TryBegin(player, ResolveSourceId(), out failureMessage)) {
            LogBlocked(failureMessage);
            return false;
        }

        if(logSuccessfulAttempts) {
            GameDebug.Success($"{competition.DisplayName} entered.", GameDebugCategory.Battle, this, "CompetitionSource");
        }

        return true;
    }

    public List<BattleChallengeDefinition> GetAvailableChallenges(PlayerController player) {
        return competition != null ? competition.GetAvailableChallenges(player) : new List<BattleChallengeDefinition>();
    }

    public bool CanStartDefaultChallenge(PlayerController player, out string failureMessage) {
        if(competition == null) {
            failureMessage = "No competition is assigned.";
            return false;
        }

        if(defaultChallenge == null) {
            failureMessage = "No default challenge is assigned.";
            return false;
        }

        return competition.CanStartChallenge(player, defaultChallenge, defaultRuleSet, out failureMessage);
    }

    public bool RecordDefaultChallengeResult(PlayerController player, bool won, out string failureMessage) {
        if(competition == null) {
            failureMessage = "No competition is assigned.";
            return false;
        }

        if(defaultChallenge == null) {
            failureMessage = "No default challenge is assigned.";
            return false;
        }

        return competition.TryRecordChallengeResult(player, defaultChallenge, defaultRuleSet, won, ResolveSourceId(), out failureMessage);
    }

    string ResolveSourceId() {
        return string.IsNullOrWhiteSpace(sourceId) ? name : sourceId;
    }

    void LogBlocked(string failureMessage) {
        if(logBlockedAttempts) {
            GameDebug.Warning(failureMessage, GameDebugCategory.Battle, this, "CompetitionSource");
        }
    }
}
