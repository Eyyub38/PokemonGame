using System.Collections;
using System.Linq;
using UnityEngine;

public enum PokemonAssignmentSourceAction {
    StartWithFirstEligiblePokemon,
    StartWithPartyIndex,
    ClaimFirstReady,
    StartOrClaimReady
}

public class PokemonAssignmentSource : MonoBehaviour, IPlayerTriggerable, Interactable {
    [Header("Source")]
    [Tooltip("Stable source id used by repeat/cooldown records. Empty uses GameObject name.")]
    [SerializeField] string sourceId = string.Empty;
    [Tooltip("Name saved in assignment history/debug output. Empty uses GameObject name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Action performed when this source is triggered.")]
    [SerializeField] PokemonAssignmentSourceAction action = PokemonAssignmentSourceAction.StartWithFirstEligiblePokemon;

    [Header("Assignment")]
    [Tooltip("Pokemon assignment started or claimed by this source.")]
    [SerializeField] PokemonAssignmentDefinition assignment = null;
    [Tooltip("Party index used by Start With Party Index. 0 is the first party Pokemon.")]
    [Min(0)]
    [SerializeField] int partyIndex;

    [Header("Context")]
    [Tooltip("Optional activity zone context. Empty falls back to PlayerActivityContext.CurrentZone.")]
    [SerializeField] ActivityZoneDefinition zoneContext = null;

    [Header("Access")]
    [Tooltip("Optional access profile checked before this source runs.")]
    [SerializeField] AccessProfileDefinition accessProfile = null;
    [Tooltip("If enabled, access checks are published to access logs/events.")]
    [SerializeField] bool publishAccessChecks = true;

    [Header("Debug")]
    [Tooltip("If enabled, repeated player triggers can call this source more than once.")]
    [SerializeField] bool triggerRepeatedly = true;
    [Tooltip("If enabled, source attempts are written to GameDebugLogger.")]
    [SerializeField] bool logAttempts;

    public string SourceId => string.IsNullOrWhiteSpace(sourceId) ? name : sourceId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public PokemonAssignmentDefinition Assignment => assignment;
    public ActivityZoneDefinition ZoneContext => zoneContext;
    public AccessProfileDefinition AccessProfile => accessProfile;
    public bool TriggerRepeatedly => triggerRepeatedly;

    public IEnumerator Interact(Transform initiator) {
        var player = initiator != null ? initiator.GetComponent<PlayerController>() : PlayerController.i;
        Apply(player);
        yield break;
    }

    public void OnPlayerTriggered(PlayerController player) {
        Apply(player);
    }

    [ContextMenu("Apply Pokemon Assignment Source")]
    public void ApplyFromContextMenu() {
        Apply(PlayerController.i);
    }

    public bool Apply(PlayerController player) {
        if(player == null) {
            Log("A player is required to run Pokemon assignment source.", GameDebugSeverity.Warning);
            return false;
        }

        if(assignment == null) {
            Log("Pokemon assignment source has no assignment assigned.", GameDebugSeverity.Warning);
            return false;
        }

        if(accessProfile != null && !accessProfile.CanAccess(player, out var accessFailure)) {
            if(publishAccessChecks) {
                accessProfile.PublishChecked(player, false, SourceId, accessFailure, this);
            }
            Log(accessFailure, GameDebugSeverity.Warning);
            return false;
        }

        if(accessProfile != null && publishAccessChecks) {
            accessProfile.PublishChecked(player, true, SourceId, accessProfile.PassedMessage, this);
        }

        var log = player.GetComponent<PlayerPokemonAssignmentLog>() ?? player.gameObject.AddComponent<PlayerPokemonAssignmentLog>();
        bool result;
        switch(action) {
            case PokemonAssignmentSourceAction.ClaimFirstReady:
                result = log.TryClaimFirstReady(player, assignment, SourceId, out var claimFailure);
                if(!result) {
                    Log(claimFailure, GameDebugSeverity.Warning);
                }
                return result;
            case PokemonAssignmentSourceAction.StartOrClaimReady:
                var readyState = log.GetReadyAssignments(assignment, SourceId).FirstOrDefault();
                if(readyState != null) {
                    bool claimed = log.TryClaim(player, assignment, readyState, out var readyClaimFailure);
                    if(claimed) {
                        Log($"{DisplayName} claimed a ready Pokemon assignment.", GameDebugSeverity.Info);
                    } else {
                        Log(readyClaimFailure, GameDebugSeverity.Warning);
                    }
                    return claimed;
                }
                return TryStart(player, log, ResolveFirstEligiblePokemon(player, log), out _);
            case PokemonAssignmentSourceAction.StartWithPartyIndex:
                return TryStart(player, log, ResolvePokemonByIndex(player, partyIndex), out _);
            default:
                return TryStart(player, log, ResolveFirstEligiblePokemon(player, log), out _);
        }
    }

    bool TryStart(PlayerController player, PlayerPokemonAssignmentLog log, Pokemon pokemon, out string failureMessage) {
        var zone = zoneContext != null ? zoneContext : PlayerActivityContext.CurrentZone;
        bool started = log.TryStart(player, assignment, pokemon, zone, SourceId, DisplayName, out failureMessage);
        if(started) {
            Log($"{pokemon?.NickName ?? "Pokemon"} started {assignment.DisplayName}.", GameDebugSeverity.Info);
        } else {
            Log(failureMessage, GameDebugSeverity.Warning);
            PublishBlocked(player, pokemon, failureMessage);
        }
        return started;
    }

    Pokemon ResolveFirstEligiblePokemon(PlayerController player, PlayerPokemonAssignmentLog log) {
        var party = player != null ? player.GetComponent<PokemonParty>() : null;
        if(party?.Pokemons == null) {
            return null;
        }

        var zone = zoneContext != null ? zoneContext : PlayerActivityContext.CurrentZone;
        return party.Pokemons.FirstOrDefault(pokemon => pokemon != null
            && assignment.CanStart(player, pokemon, log, zone, SourceId, out _));
    }

    Pokemon ResolvePokemonByIndex(PlayerController player, int index) {
        var party = player != null ? player.GetComponent<PokemonParty>() : null;
        if(party?.Pokemons == null || index < 0 || index >= party.Pokemons.Count) {
            return null;
        }

        return party.Pokemons[index];
    }

    void PublishBlocked(PlayerController player, Pokemon pokemon, string failureMessage) {
        GameEventPublishing.PublishOptional(
            assignment.BlockedEvent,
            $"pokemon-assignment.blocked.{assignment.Id}.{SourceId}",
            string.IsNullOrWhiteSpace(failureMessage) ? $"{assignment.DisplayName} was blocked." : failureMessage,
            GameEventCategory.PokemonCare,
            GameEventImportance.Warning,
            this,
            "PokemonAssignmentSource",
            GameEventScope.Player,
            showInFeed: true,
            writeToDebugLog: logAttempts,
            GameEventPublishing.Value("assignmentId", assignment.Id),
            GameEventPublishing.Value("assignmentName", assignment.DisplayName),
            GameEventPublishing.Value("sourceId", SourceId),
            GameEventPublishing.Value("pokemonName", pokemon != null ? pokemon.NickName : string.Empty),
            GameEventPublishing.Value("failureMessage", failureMessage));
    }

    void Log(string message, GameDebugSeverity severity) {
        if(!logAttempts && severity < GameDebugSeverity.Warning) {
            return;
        }

        GameDebugLogger.Ensure().Record(severity, GameDebugCategory.PokemonCare, message, this, "PokemonAssignmentSource");
    }
}
