using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum ContestHostTriggerMode {
    RevealOnly,
    AutoRunFirstAvailable
}

public class ContestHost : MonoBehaviour, IPlayerTriggerable {
    [Header("Host")]
    [Tooltip("Stable source id used by contest logs. Empty uses GameObject name.")]
    [SerializeField] string hostId;
    [Tooltip("Name shown in debug/future UI. Empty uses GameObject name.")]
    [SerializeField] string displayName;
    [Tooltip("Contests available from this host.")]
    [SerializeField] List<ContestDefinition> contests = new List<ContestDefinition>();

    [Header("Trigger")]
    [Tooltip("What this host does when the player triggers it without a UI.")]
    [SerializeField] ContestHostTriggerMode triggerMode = ContestHostTriggerMode.RevealOnly;
    [Tooltip("If enabled, triggering this host unlocks its listed contests in PlayerContestLog.")]
    [SerializeField] bool unlockContestsOnTrigger = true;
    [Tooltip("If enabled, repeated player triggers can call OnPlayerTriggered more than once.")]
    [SerializeField] bool triggerRepeatedly = true;

    [Header("Access")]
    [Tooltip("Optional title, badge, permit or license required before this host can be used.")]
    [SerializeField] TitleDefinition requiredTitle;
    [Tooltip("Optional faction whose reputation gates this host.")]
    [SerializeField] ReputationFactionDefinition requiredFaction;
    [Tooltip("Minimum reputation required with the selected faction.")]
    [SerializeField] int requiredReputation;
    [Tooltip("Message shown when host access is blocked.")]
    [SerializeField] string lockedMessage = "This contest host is not available right now.";

    [Header("Debug")]
    [Tooltip("If enabled, reveal/run attempts are written to GameDebug.")]
    [SerializeField] bool logAttempts;

    public string HostId => string.IsNullOrWhiteSpace(hostId) ? name : hostId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public IReadOnlyList<ContestDefinition> Contests => contests;
    public bool TriggerRepeatedly => triggerRepeatedly;

    public void OnPlayerTriggered(PlayerController player) {
        if(player == null) {
            return;
        }

        if(!CanUse(player, out var failureMessage)) {
            PublishHostEvent(player, "blocked", failureMessage, GameEventImportance.Warning);
            return;
        }

        var log = player.GetComponent<PlayerContestLog>() ?? player.gameObject.AddComponent<PlayerContestLog>();
        if(unlockContestsOnTrigger) {
            foreach(var contest in contests) {
                log.UnlockContest(contest, HostId);
            }
        }

        if(triggerMode == ContestHostTriggerMode.AutoRunFirstAvailable) {
            TryRunFirstAvailable(player, out _, out _);
        } else {
            PublishHostEvent(player, "revealed", $"{DisplayName} has {GetAvailableContests(player).Count} available contest(s).", GameEventImportance.Info);
        }
    }

    public bool CanUse(PlayerController player, out string failureMessage) {
        if(requiredTitle != null && !(player?.GetComponent<PlayerTitles>()?.HasTitle(requiredTitle) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need {requiredTitle.DisplayName}." : lockedMessage;
            return false;
        }

        if(requiredFaction != null) {
            int reputation = player?.GetComponent<PlayerReputation>()?.GetReputation(requiredFaction) ?? 0;
            if(reputation < requiredReputation) {
                failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need more reputation with {requiredFaction.DisplayName}." : lockedMessage;
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    public List<ContestDefinition> GetAvailableContests(PlayerController player) {
        if(player == null || !CanUse(player, out _)) {
            return new List<ContestDefinition>();
        }

        var pokemon = player.GetComponent<PokemonParty>()?.GetHealthyPokemon();
        return (contests ?? new List<ContestDefinition>())
            .Where(contest => contest != null && contest.CanEnter(player, pokemon, out _))
            .OrderBy(contest => contest.Difficulty)
            .ThenBy(contest => contest.DisplayName)
            .ToList();
    }

    public bool TryRunContest(PlayerController player, ContestDefinition contest, Pokemon selectedPokemon, out ContestRunResult result, out string failureMessage) {
        result = null;
        if(player == null) {
            failureMessage = "A player is required to run a contest.";
            return false;
        }

        if(!CanUse(player, out failureMessage)) {
            PublishHostEvent(player, "blocked", failureMessage, GameEventImportance.Warning);
            return false;
        }

        if(contest == null || !contests.Contains(contest)) {
            failureMessage = "This contest is not available from this host.";
            PublishHostEvent(player, "blocked", failureMessage, GameEventImportance.Warning);
            return false;
        }

        bool success = contest.TryRunContest(player, selectedPokemon, out result, out failureMessage);
        PublishHostEvent(player, success ? "completed" : "blocked", success ? $"{contest.DisplayName} completed." : failureMessage, success && result != null && result.won ? GameEventImportance.Success : GameEventImportance.Info);
        return success;
    }

    public bool TryRunFirstAvailable(PlayerController player, out ContestRunResult result, out string failureMessage) {
        result = null;
        var contest = GetAvailableContests(player).FirstOrDefault();
        if(contest == null) {
            failureMessage = "No contest is available right now.";
            PublishHostEvent(player, "empty", failureMessage, GameEventImportance.Trace);
            return false;
        }

        var pokemon = player.GetComponent<PokemonParty>()?.GetHealthyPokemon();
        return TryRunContest(player, contest, pokemon, out result, out failureMessage);
    }

    void PublishHostEvent(PlayerController player, string phase, string message, GameEventImportance importance) {
        if(logAttempts) {
            GameDebug.Step(message, GameDebugCategory.Contest, player != null ? player : this, "ContestHost");
        }

        GameEventPublishing.PublishOptional(
            null,
            $"contest-host.{phase}.{HostId}",
            message,
            GameEventCategory.Contest,
            importance,
            player != null ? player : this,
            "ContestHost",
            GameEventScope.Player,
            showInFeed: false,
            writeToDebugLog: logAttempts,
            GameEventPublishing.Value("hostId", HostId),
            GameEventPublishing.Value("hostName", DisplayName),
            GameEventPublishing.Value("phase", phase));
    }
}
