using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum PokemonTechniqueLearnSource {
    Unknown,
    LevelUp,
    TM,
    Tutor,
    Item,
    Quest,
    Care,
    Training,
    Evolution,
    Event,
    Manual
}

public enum PokemonTechniqueLearnTarget {
    PartySlot,
    FirstHealthyPokemon,
    AllPartyPokemon
}

[CreateAssetMenu(menuName = "Pokemon/Technique Learning/Learn Definition")]
public class PokemonTechniqueLearningDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable id saved in technique memory history. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in debug output or future UI. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer notes explaining this learning source.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Free-form tags such as tutor, fire, beginner, ranger, contest or training.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Technique")]
    [Tooltip("Move/technique learned by this definition.")]
    [SerializeField] MoveBase move;
    [Tooltip("Broad source category recorded in Pokemon technique memory.")]
    [SerializeField] PokemonTechniqueLearnSource source = PokemonTechniqueLearnSource.Tutor;
    [Tooltip("If enabled, the Pokemon must be able to learn this move through its TM list or level-up list.")]
    [SerializeField] bool requireSpeciesCompatibility = true;
    [Tooltip("If enabled, already known techniques are treated as success instead of blocked.")]
    [SerializeField] bool allowAlreadyKnownAsSuccess = true;

    [Header("Active Move Set")]
    [Tooltip("If enabled, the learned technique is added to the active 4-move set when there is room.")]
    [SerializeField] bool addToActiveMoveSetWhenPossible = true;
    [Tooltip("If enabled, this definition can replace an active move by index when called from code/future UI.")]
    [SerializeField] bool allowActiveMoveReplacement = true;

    [Header("Requirements")]
    [Tooltip("Minimum Pokemon level required. 0 ignores level.")]
    [Min(0)]
    [SerializeField] int minimumLevel;
    [Tooltip("Minimum friendship required. 0 ignores friendship.")]
    [Range(0, 255)]
    [SerializeField] int minimumFriendship;
    [Tooltip("Reusable player requirements such as title, license, quest, region, research, shop or reputation gates.")]
    [SerializeField] List<ActivityRequirement> requirements = new List<ActivityRequirement>();
    [Tooltip("Message shown when this learn definition is blocked.")]
    [TextArea]
    [SerializeField] string blockedMessage = "This technique cannot be learned right now.";

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public IReadOnlyList<string> Tags => tags != null ? tags : Array.Empty<string>();
    public MoveBase Move => move;
    public PokemonTechniqueLearnSource Source => source;
    public bool RequireSpeciesCompatibility => requireSpeciesCompatibility;
    public bool AllowAlreadyKnownAsSuccess => allowAlreadyKnownAsSuccess;
    public bool AddToActiveMoveSetWhenPossible => addToActiveMoveSetWhenPossible;
    public bool AllowActiveMoveReplacement => allowActiveMoveReplacement;
    public int MinimumLevel => Mathf.Max(0, minimumLevel);
    public int MinimumFriendship => Mathf.Clamp(minimumFriendship, 0, 255);
    public IReadOnlyList<ActivityRequirement> Requirements => requirements != null ? requirements : Array.Empty<ActivityRequirement>();

    public bool CanLearn(Pokemon pokemon, PlayerController player, out string failureMessage) {
        failureMessage = null;
        if(pokemon == null) {
            failureMessage = "No Pokemon selected.";
            return false;
        }

        if(move == null) {
            failureMessage = "No technique is assigned.";
            return false;
        }

        if(pokemon.HasKnownTechnique(move)) {
            if(allowAlreadyKnownAsSuccess) {
                return true;
            }

            failureMessage = $"{pokemon.NickName} already knows {move.Name}.";
            return false;
        }

        if(minimumLevel > 0 && pokemon.Level < minimumLevel) {
            failureMessage = $"{pokemon.NickName} must reach level {minimumLevel}.";
            return false;
        }

        if(minimumFriendship > 0 && pokemon.Friendship < minimumFriendship) {
            failureMessage = $"{pokemon.NickName} needs more friendship.";
            return false;
        }

        if(requireSpeciesCompatibility && !PokemonCanLearnMove(pokemon, move)) {
            failureMessage = $"{pokemon.NickName} cannot learn {move.Name}.";
            return false;
        }

        foreach(var requirement in Requirements) {
            if(requirement != null && !requirement.IsMet(player)) {
                failureMessage = string.IsNullOrWhiteSpace(requirement.FailureMessage) ? blockedMessage : requirement.FailureMessage;
                return false;
            }
        }

        return true;
    }

    public bool TryApply(Pokemon pokemon, PlayerController player, int replaceActiveMoveIndex, string sourceId, out string failureMessage) {
        if(!CanLearn(pokemon, player, out failureMessage)) {
            return false;
        }

        bool remembered = pokemon.RememberTechnique(move, source, string.IsNullOrWhiteSpace(sourceId) ? Id : sourceId, DisplayName);
        if(addToActiveMoveSetWhenPossible && !pokemon.HasMove(move) && pokemon.Moves.Count < PokemonBase.MaxNumberOfMoves) {
            pokemon.LearnMove(move, source, Id, DisplayName);
        } else if(allowActiveMoveReplacement && replaceActiveMoveIndex >= 0) {
            pokemon.SetActiveMove(replaceActiveMoveIndex, move, source, Id, DisplayName);
        }

        failureMessage = remembered ? null : $"{pokemon.NickName} already knows {move.Name}.";
        return allowAlreadyKnownAsSuccess || remembered;
    }

    bool PokemonCanLearnMove(Pokemon pokemon, MoveBase candidate) {
        if(pokemon?.Base == null || candidate == null) {
            return false;
        }

        bool levelMove = pokemon.Base.LearnableMoves != null && pokemon.Base.LearnableMoves.Any(entry => entry != null && entry.Base == candidate);
        bool tmMove = pokemon.Base.LearnableMovesByTm != null && pokemon.Base.LearnableMovesByTm.Contains(candidate);
        return levelMove || tmMove;
    }
}

[Serializable]
public class PokemonTechniqueMemoryState {
    [Tooltip("Known techniques remembered by this Pokemon.")]
    public List<PokemonTechniqueMemoryRecord> knownTechniques = new List<PokemonTechniqueMemoryRecord>();
    [Tooltip("Recent learn/forget/equip history for future UI/debugging.")]
    public List<PokemonTechniqueMemoryHistoryRecord> history = new List<PokemonTechniqueMemoryHistoryRecord>();

    public bool HasMove(MoveBase move) {
        string id = PokemonTechniqueMemoryUtility.GetMoveId(move);
        return !string.IsNullOrWhiteSpace(id) && HasMoveId(id);
    }

    public bool HasMoveId(string moveId) {
        return !string.IsNullOrWhiteSpace(moveId)
            && knownTechniques != null
            && knownTechniques.Any(record => record != null && string.Equals(record.moveId, moveId, StringComparison.OrdinalIgnoreCase));
    }

    public bool Remember(MoveBase move, PokemonTechniqueLearnSource source, string sourceId, string sourceName) {
        if(move == null) {
            return false;
        }

        knownTechniques ??= new List<PokemonTechniqueMemoryRecord>();
        string moveId = PokemonTechniqueMemoryUtility.GetMoveId(move);
        var existing = knownTechniques.FirstOrDefault(record => record != null && string.Equals(record.moveId, moveId, StringComparison.OrdinalIgnoreCase));
        if(existing != null) {
            existing.lastSource = source;
            existing.lastSourceId = sourceId;
            existing.lastLearnedAbsoluteHour = GetCurrentAbsoluteHour();
            AddHistory(move, "refreshed", source, sourceId, sourceName);
            return false;
        }

        knownTechniques.Add(new PokemonTechniqueMemoryRecord {
            moveId = moveId,
            moveName = move.Name,
            firstSource = source,
            lastSource = source,
            firstSourceId = sourceId,
            lastSourceId = sourceId,
            learnedDay = GetCurrentDay(),
            learnedAbsoluteHour = GetCurrentAbsoluteHour(),
            lastLearnedAbsoluteHour = GetCurrentAbsoluteHour()
        });
        AddHistory(move, "learned", source, sourceId, sourceName);
        return true;
    }

    public void MarkActive(MoveBase move, bool active, PokemonTechniqueLearnSource source, string sourceId, string sourceName) {
        if(move == null) {
            return;
        }

        Remember(move, source, sourceId, sourceName);
        string moveId = PokemonTechniqueMemoryUtility.GetMoveId(move);
        var record = knownTechniques.FirstOrDefault(entry => entry != null && string.Equals(entry.moveId, moveId, StringComparison.OrdinalIgnoreCase));
        if(record != null) {
            record.activeMoveSet = active;
        }

        AddHistory(move, active ? "activated" : "deactivated", source, sourceId, sourceName);
    }

    public void SetFavorite(MoveBase move, bool favorite) {
        if(move == null || knownTechniques == null) {
            return;
        }

        string moveId = PokemonTechniqueMemoryUtility.GetMoveId(move);
        var record = knownTechniques.FirstOrDefault(entry => entry != null && string.Equals(entry.moveId, moveId, StringComparison.OrdinalIgnoreCase));
        if(record != null) {
            record.favorite = favorite;
        }
    }

    public IEnumerable<MoveBase> ResolveKnownMoves() {
        return knownTechniques != null
            ? knownTechniques.Select(record => record != null ? MoveDB.GetObjectByName(record.moveId) : null).Where(move => move != null)
            : Enumerable.Empty<MoveBase>();
    }

    public void SyncActiveMoves(IEnumerable<Move> activeMoves) {
        if(knownTechniques == null) {
            knownTechniques = new List<PokemonTechniqueMemoryRecord>();
        }

        foreach(var record in knownTechniques.Where(record => record != null)) {
            record.activeMoveSet = false;
        }

        foreach(var move in activeMoves ?? Enumerable.Empty<Move>()) {
            if(move?.Base == null) {
                continue;
            }

            Remember(move.Base, PokemonTechniqueLearnSource.Unknown, "active-sync", "Active Move Sync");
            string moveId = PokemonTechniqueMemoryUtility.GetMoveId(move.Base);
            var record = knownTechniques.FirstOrDefault(entry => entry != null && string.Equals(entry.moveId, moveId, StringComparison.OrdinalIgnoreCase));
            if(record != null) {
                record.activeMoveSet = true;
            }
        }
    }

    void AddHistory(MoveBase move, string operation, PokemonTechniqueLearnSource source, string sourceId, string sourceName) {
        if(move == null) {
            return;
        }

        history ??= new List<PokemonTechniqueMemoryHistoryRecord>();
        history.Add(new PokemonTechniqueMemoryHistoryRecord {
            moveId = PokemonTechniqueMemoryUtility.GetMoveId(move),
            moveName = move.Name,
            operation = operation,
            source = source,
            sourceId = sourceId,
            sourceName = sourceName,
            day = GetCurrentDay(),
            absoluteHour = GetCurrentAbsoluteHour()
        });

        if(history.Count > 80) {
            history.RemoveAt(0);
        }
    }

    int GetCurrentDay() {
        return TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }
}

[Serializable]
public class PokemonTechniqueMemoryRecord {
    [Tooltip("Move asset id/name.")]
    public string moveId;
    [Tooltip("Move display name saved for fallback/debug output.")]
    public string moveName;
    [Tooltip("If enabled, this technique is currently in the classic active move set.")]
    public bool activeMoveSet;
    [Tooltip("If enabled, future UI can pin this technique.")]
    public bool favorite;
    [Tooltip("First source that taught this technique.")]
    public PokemonTechniqueLearnSource firstSource;
    [Tooltip("Most recent source that refreshed/taught this technique.")]
    public PokemonTechniqueLearnSource lastSource;
    [Tooltip("First source id that taught this technique.")]
    public string firstSourceId;
    [Tooltip("Most recent source id for this technique.")]
    public string lastSourceId;
    [Tooltip("In-game day when this technique was first learned.")]
    public int learnedDay;
    [Tooltip("Absolute hour when this technique was first learned.")]
    public int learnedAbsoluteHour;
    [Tooltip("Absolute hour when this technique was last refreshed/taught.")]
    public int lastLearnedAbsoluteHour;
}

[Serializable]
public class PokemonTechniqueMemoryHistoryRecord {
    [Tooltip("Move asset id/name.")]
    public string moveId;
    [Tooltip("Move display name saved for fallback/debug output.")]
    public string moveName;
    [Tooltip("Operation such as learned, refreshed, activated or deactivated.")]
    public string operation;
    [Tooltip("Source category for this operation.")]
    public PokemonTechniqueLearnSource source;
    [Tooltip("Specific source id.")]
    public string sourceId;
    [Tooltip("Specific source display name.")]
    public string sourceName;
    [Tooltip("In-game day of this history entry.")]
    public int day;
    [Tooltip("Absolute in-game hour of this history entry.")]
    public int absoluteHour;
}

public class PokemonTechniqueLearningSource : MonoBehaviour, Interactable, IPlayerTriggerable {
    [Header("Definition")]
    [Tooltip("Learning definition applied by this source.")]
    [SerializeField] PokemonTechniqueLearningDefinition definition;
    [Tooltip("Player used by context-menu/start actions. Empty uses PlayerController.i.")]
    [SerializeField] PlayerController playerOverride;

    [Header("Targeting")]
    [Tooltip("Which Pokemon receive this technique.")]
    [SerializeField] PokemonTechniqueLearnTarget target = PokemonTechniqueLearnTarget.PartySlot;
    [Tooltip("Party slot used when Target is Party Slot.")]
    [Min(0)]
    [SerializeField] int partySlotIndex;
    [Tooltip("Active move index to replace. -1 means no forced replacement.")]
    [SerializeField] int replaceActiveMoveIndex = -1;

    [Header("Triggering")]
    [Tooltip("If enabled, trigger volumes may run this source repeatedly.")]
    [SerializeField] bool triggerRepeatedly;
    [Tooltip("If enabled, learning results are written to GameDebug.")]
    [SerializeField] bool writeDebugLog;

    public PokemonTechniqueLearningDefinition Definition => definition;
    public PokemonTechniqueLearnTarget Target => target;
    public int PartySlotIndex => Mathf.Max(0, partySlotIndex);
    public bool TriggerRepeatedly => triggerRepeatedly;

    public IEnumerator Interact(Transform initiator) {
        TryApply(initiator != null ? initiator.GetComponent<PlayerController>() : ResolvePlayer(), out _);
        yield break;
    }

    public void OnPlayerTriggered(PlayerController player) {
        TryApply(player != null ? player : ResolvePlayer(), out _);
    }

    [ContextMenu("Try Teach Technique")]
    public void TryFromContextMenu() {
        TryApply(ResolvePlayer(), out _);
    }

    public bool TryApply(PlayerController player, out string feedback) {
        feedback = null;
        if(definition == null) {
            feedback = "Technique learning definition is missing.";
            WriteDebug(feedback, warning: true);
            return false;
        }

        var party = player != null ? player.GetComponent<PokemonParty>() : null;
        if(party == null || party.Pokemons == null) {
            feedback = "Pokemon party is missing.";
            WriteDebug(feedback, warning: true);
            return false;
        }

        int successCount = 0;
        string lastMessage = null;
        foreach(var pokemon in ResolveTargets(party)) {
            if(pokemon == null) {
                continue;
            }

            if(definition.TryApply(pokemon, player, replaceActiveMoveIndex, Definition.Id, out var failure)) {
                successCount++;
            } else {
                lastMessage = failure;
            }
        }

        if(successCount > 0) {
            party.PartyUpdated();
            feedback = $"{definition.DisplayName} taught to {successCount} Pokemon.";
            WriteDebug(feedback, warning: false);
            return true;
        }

        feedback = string.IsNullOrWhiteSpace(lastMessage) ? "No Pokemon learned the technique." : lastMessage;
        WriteDebug(feedback, warning: true);
        return false;
    }

    IEnumerable<Pokemon> ResolveTargets(PokemonParty party) {
        switch(target) {
            case PokemonTechniqueLearnTarget.AllPartyPokemon:
                return party.Pokemons.Where(pokemon => pokemon != null);
            case PokemonTechniqueLearnTarget.FirstHealthyPokemon:
                var healthy = party.GetHealthyPokemon();
                return healthy != null ? new[] { healthy } : Enumerable.Empty<Pokemon>();
            default:
                return partySlotIndex >= 0 && partySlotIndex < party.Pokemons.Count && party.Pokemons[partySlotIndex] != null
                    ? new[] { party.Pokemons[partySlotIndex] }
                    : Enumerable.Empty<Pokemon>();
        }
    }

    PlayerController ResolvePlayer() {
        if(playerOverride != null) {
            return playerOverride;
        }

        return PlayerController.i != null ? PlayerController.i : FindAnyObjectByType<PlayerController>();
    }

    void WriteDebug(string message, bool warning) {
        if(!writeDebugLog || string.IsNullOrWhiteSpace(message)) {
            return;
        }

        if(warning) {
            GameDebug.Warning(message, GameDebugCategory.General, this, "PokemonTechniqueLearningSource");
        } else {
            GameDebug.Success(message, GameDebugCategory.General, this, "PokemonTechniqueLearningSource");
        }
    }
}

public static class PokemonTechniqueMemoryUtility {
    public static string GetMoveId(MoveBase move) {
        return move != null ? move.name : string.Empty;
    }
}
