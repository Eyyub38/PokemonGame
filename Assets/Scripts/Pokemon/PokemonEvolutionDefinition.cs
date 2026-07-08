using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum PokemonEvolutionTriggerKind {
    Any,
    LevelUp,
    ItemUse,
    Interaction,
    Care,
    Assignment,
    RegionTravel,
    Manual
}

public enum PokemonEvolutionRequirementMatchMode {
    All,
    Any
}

[CreateAssetMenu(menuName = "Pokemon/Evolution/Evolution Definition")]
public class PokemonEvolutionDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable id saved in Pokemon evolution history. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in debug output or future UI. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer notes explaining this evolution route.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Free-form tags such as starter, region, friendship, stone, quest, care or special.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Pokemon")]
    [Tooltip("Pokemon species/base that can use this evolution route.")]
    [SerializeField] PokemonBase evolvesFrom;
    [Tooltip("Pokemon species/base that this route evolves into.")]
    [SerializeField] PokemonBase evolvesInto;
    [Tooltip("If enabled, this route can be used while the Pokemon has a temporary battle base override.")]
    [SerializeField] bool allowTemporaryBattleBase;

    [Header("Trigger")]
    [Tooltip("Trigger type this route expects. Any allows every trigger kind.")]
    [SerializeField] PokemonEvolutionTriggerKind triggerKind = PokemonEvolutionTriggerKind.Any;
    [Tooltip("Item required when trigger kind is Item Use, or optional item gate for other trigger kinds.")]
    [SerializeField] ItemBase requiredItem;
    [Tooltip("If enabled, the item must be consumed by the caller/source after a successful evolution.")]
    [SerializeField] bool consumeRequiredItem = true;

    [Header("Core Conditions")]
    [Tooltip("Minimum Pokemon level required. 0 ignores level.")]
    [Min(0)]
    [SerializeField] int minimumLevel;
    [Tooltip("Minimum friendship required. 0 ignores friendship.")]
    [Range(0, 255)]
    [SerializeField] int minimumFriendship;
    [Tooltip("Required gender. None ignores gender.")]
    [SerializeField] Gender requiredGender = Gender.None;
    [Tooltip("Required classic nature. Empty list ignores nature.")]
    [SerializeField] List<NatureID> allowedNatures = new List<NatureID>();
    [Tooltip("Required personality. Empty list ignores personality.")]
    [SerializeField] List<PersonalityID> allowedPersonalities = new List<PersonalityID>();
    [Tooltip("Required passive growth trait ids. Empty list ignores traits.")]
    [SerializeField] List<string> requiredGrowthTraitIds = new List<string>();

    [Header("World Conditions")]
    [Tooltip("Required day period for evolution. None ignores time.")]
    [SerializeField] GeneralDayPeriod requiredTime = GeneralDayPeriod.None;
    [Tooltip("Required region id. Empty ignores region.")]
    [SerializeField] string requiredRegionId = string.Empty;
    [Tooltip("Required zone id. Empty ignores zone.")]
    [SerializeField] string requiredZoneId = string.Empty;
    [Tooltip("Required scene name. Empty ignores scene.")]
    [SerializeField] string requiredSceneName = string.Empty;

    [Header("Extra Requirements")]
    [Tooltip("How extra ActivityRequirement assets are evaluated.")]
    [SerializeField] PokemonEvolutionRequirementMatchMode requirementMatchMode = PokemonEvolutionRequirementMatchMode.All;
    [Tooltip("Reusable player requirements for quest, title, license, reputation, region, research or other gates.")]
    [SerializeField] List<ActivityRequirement> requirements = new List<ActivityRequirement>();
    [Tooltip("Message shown when this route is blocked.")]
    [TextArea]
    [SerializeField] string blockedMessage = "Evolution requirements are not met.";

    [Header("Player Choice")]
    [Tooltip("If enabled, future UI should ask the player before applying this evolution.")]
    [SerializeField] bool requiresPlayerConfirmation = true;
    [Tooltip("If enabled, the player can defer/cancel this evolution and try again later.")]
    [SerializeField] bool allowDeferral = true;
    [Tooltip("If enabled, this route is hidden from debug/future UI until requirements are met.")]
    [SerializeField] bool hideUntilEligible;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public IReadOnlyList<string> Tags => tags != null ? tags : Array.Empty<string>();
    public PokemonBase EvolvesFrom => evolvesFrom;
    public PokemonBase EvolvesInto => evolvesInto;
    public bool AllowTemporaryBattleBase => allowTemporaryBattleBase;
    public PokemonEvolutionTriggerKind TriggerKind => triggerKind;
    public ItemBase RequiredItem => requiredItem;
    public bool ConsumeRequiredItem => consumeRequiredItem;
    public int MinimumLevel => Mathf.Max(0, minimumLevel);
    public int MinimumFriendship => Mathf.Clamp(minimumFriendship, 0, 255);
    public Gender RequiredGender => requiredGender;
    public IReadOnlyList<NatureID> AllowedNatures => allowedNatures != null ? allowedNatures : Array.Empty<NatureID>();
    public IReadOnlyList<PersonalityID> AllowedPersonalities => allowedPersonalities != null ? allowedPersonalities : Array.Empty<PersonalityID>();
    public IReadOnlyList<string> RequiredGrowthTraitIds => requiredGrowthTraitIds != null ? requiredGrowthTraitIds : Array.Empty<string>();
    public GeneralDayPeriod RequiredTime => requiredTime;
    public string RequiredRegionId => requiredRegionId;
    public string RequiredZoneId => requiredZoneId;
    public string RequiredSceneName => requiredSceneName;
    public IReadOnlyList<ActivityRequirement> Requirements => requirements != null ? requirements : Array.Empty<ActivityRequirement>();
    public bool RequiresPlayerConfirmation => requiresPlayerConfirmation;
    public bool AllowDeferral => allowDeferral;
    public bool HideUntilEligible => hideUntilEligible;

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    public bool CanEvolve(Pokemon pokemon, PlayerController player, PokemonEvolutionTriggerKind trigger, ItemBase item, PokemonEvolutionContext context, out string failureMessage) {
        failureMessage = null;
        if(pokemon == null) {
            failureMessage = "No Pokemon selected.";
            return false;
        }

        if(evolvesFrom == null || evolvesInto == null) {
            failureMessage = "Evolution route is missing source or target Pokemon.";
            return false;
        }

        var sourceBase = allowTemporaryBattleBase ? pokemon.Base : pokemon.OriginalBase;
        if(sourceBase != evolvesFrom) {
            failureMessage = $"{pokemon.NickName} does not match this evolution route.";
            return false;
        }

        if(triggerKind != PokemonEvolutionTriggerKind.Any && triggerKind != trigger) {
            failureMessage = "Evolution trigger does not match this route.";
            return false;
        }

        if(requiredItem != null && item != requiredItem) {
            failureMessage = $"{requiredItem.Name} is required.";
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

        if(requiredGender != Gender.None && pokemon.Gender != requiredGender) {
            failureMessage = $"{pokemon.NickName} does not match the required gender.";
            return false;
        }

        if(AllowedNatures.Count > 0 && !AllowedNatures.Contains(GetNatureId(pokemon))) {
            failureMessage = $"{pokemon.NickName}'s nature does not match this evolution.";
            return false;
        }

        if(AllowedPersonalities.Count > 0 && !AllowedPersonalities.Contains(pokemon.PersonalityID)) {
            failureMessage = $"{pokemon.NickName}'s personality does not match this evolution.";
            return false;
        }

        foreach(var traitId in RequiredGrowthTraitIds.Where(id => !string.IsNullOrWhiteSpace(id))) {
            if(!pokemon.HasPassiveTrait(traitId)) {
                failureMessage = $"{pokemon.NickName} lacks a required growth trait.";
                return false;
            }
        }

        if(requiredTime != GeneralDayPeriod.None && (TimeSystem.i == null || TimeSystem.i.EvolutionTime != requiredTime)) {
            failureMessage = $"Evolution requires {requiredTime}.";
            return false;
        }

        context ??= PokemonEvolutionContext.FromPlayer(player);
        if(!string.IsNullOrWhiteSpace(requiredRegionId) && !string.Equals(requiredRegionId, context.regionId, StringComparison.OrdinalIgnoreCase)) {
            failureMessage = "Evolution requires a different region.";
            return false;
        }

        if(!string.IsNullOrWhiteSpace(requiredZoneId) && !string.Equals(requiredZoneId, context.zoneId, StringComparison.OrdinalIgnoreCase)) {
            failureMessage = "Evolution requires a different zone.";
            return false;
        }

        if(!string.IsNullOrWhiteSpace(requiredSceneName) && !string.Equals(requiredSceneName, context.sceneName, StringComparison.OrdinalIgnoreCase)) {
            failureMessage = "Evolution requires a different scene.";
            return false;
        }

        if(!PassesExtraRequirements(player, out failureMessage)) {
            failureMessage = string.IsNullOrWhiteSpace(failureMessage) ? blockedMessage : failureMessage;
            return false;
        }

        return true;
    }

    bool PassesExtraRequirements(PlayerController player, out string failureMessage) {
        failureMessage = null;
        var active = Requirements.Where(requirement => requirement != null).ToList();
        if(active.Count == 0) {
            return true;
        }

        if(requirementMatchMode == PokemonEvolutionRequirementMatchMode.Any) {
            foreach(var requirement in active) {
                if(requirement.IsMet(player)) {
                    return true;
                }
            }

            failureMessage = active.FirstOrDefault()?.FailureMessage;
            return false;
        }

        foreach(var requirement in active) {
            if(!requirement.IsMet(player)) {
                failureMessage = requirement.FailureMessage;
                return false;
            }
        }

        return true;
    }

    NatureID GetNatureId(Pokemon pokemon) {
        if(pokemon?.Nature == null || string.IsNullOrWhiteSpace(pokemon.Nature.Name)) {
            return NatureID.Hardy;
        }

        return Enum.TryParse(pokemon.Nature.Name, out NatureID natureId) ? natureId : NatureID.Hardy;
    }
}

[Serializable]
public class PokemonEvolutionRuntimeState {
    [Tooltip("Evolution route ids the player deferred/cancelled for now.")]
    public List<string> deferredEvolutionIds = new List<string>();
    [Tooltip("Evolution history records for this Pokemon.")]
    public List<PokemonEvolutionHistoryRecord> history = new List<PokemonEvolutionHistoryRecord>();

    public bool IsDeferred(string evolutionId) {
        return !string.IsNullOrWhiteSpace(evolutionId)
            && deferredEvolutionIds != null
            && deferredEvolutionIds.Any(id => string.Equals(id, evolutionId, StringComparison.OrdinalIgnoreCase));
    }

    public void Defer(string evolutionId) {
        if(string.IsNullOrWhiteSpace(evolutionId)) {
            return;
        }

        deferredEvolutionIds ??= new List<string>();
        if(!IsDeferred(evolutionId)) {
            deferredEvolutionIds.Add(evolutionId);
        }
    }

    public void ClearDeferred(string evolutionId) {
        if(string.IsNullOrWhiteSpace(evolutionId) || deferredEvolutionIds == null) {
            return;
        }

        deferredEvolutionIds.RemoveAll(id => string.Equals(id, evolutionId, StringComparison.OrdinalIgnoreCase));
    }

    public void Record(PokemonEvolutionDefinition definition, PokemonBase fromBase, PokemonBase toBase, PokemonEvolutionTriggerKind trigger, string sourceId) {
        history ??= new List<PokemonEvolutionHistoryRecord>();
        history.Add(new PokemonEvolutionHistoryRecord {
            evolutionId = definition != null ? definition.Id : string.Empty,
            evolutionName = definition != null ? definition.DisplayName : string.Empty,
            fromPokemonName = fromBase != null ? fromBase.Name : string.Empty,
            toPokemonName = toBase != null ? toBase.Name : string.Empty,
            triggerKind = trigger,
            sourceId = sourceId,
            day = TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1,
            absoluteHour = TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0
        });

        if(history.Count > 30) {
            history.RemoveAt(0);
        }
    }
}

[Serializable]
public class PokemonEvolutionHistoryRecord {
    [Tooltip("Evolution definition id.")]
    public string evolutionId;
    [Tooltip("Evolution definition display name.")]
    public string evolutionName;
    [Tooltip("Previous Pokemon name.")]
    public string fromPokemonName;
    [Tooltip("New Pokemon name.")]
    public string toPokemonName;
    [Tooltip("Trigger that caused this evolution.")]
    public PokemonEvolutionTriggerKind triggerKind;
    [Tooltip("Source id that triggered this evolution.")]
    public string sourceId;
    [Tooltip("In-game day when this evolution happened.")]
    public int day;
    [Tooltip("Absolute in-game hour when this evolution happened.")]
    public int absoluteHour;
}

[Serializable]
public class PokemonEvolutionContext {
    [Tooltip("Region id used by evolution requirements.")]
    public string regionId;
    [Tooltip("Zone id used by evolution requirements.")]
    public string zoneId;
    [Tooltip("Scene name used by evolution requirements.")]
    public string sceneName;

    public static PokemonEvolutionContext FromPlayer(PlayerController player) {
        return new PokemonEvolutionContext {
            regionId = string.Empty,
            zoneId = string.Empty,
            sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        };
    }
}

public static class PokemonEvolutionService {
    public static IReadOnlyList<PokemonEvolutionDefinition> FindRoutes(Pokemon pokemon, PlayerController player, PokemonEvolutionTriggerKind trigger, ItemBase item, PokemonEvolutionContext context, bool includeDeferred = false) {
        if(pokemon == null) {
            return Array.Empty<PokemonEvolutionDefinition>();
        }

        return Resources.LoadAll<PokemonEvolutionDefinition>("")
            .Where(route => route != null)
            .Where(route => includeDeferred || pokemon.EvolutionState == null || !pokemon.EvolutionState.IsDeferred(route.Id))
            .Where(route => route.CanEvolve(pokemon, player, trigger, item, context, out _))
            .ToList();
    }

    public static PokemonEvolutionDefinition FindFirstRoute(Pokemon pokemon, PlayerController player, PokemonEvolutionTriggerKind trigger, ItemBase item, PokemonEvolutionContext context, bool includeDeferred = false) {
        return FindRoutes(pokemon, player, trigger, item, context, includeDeferred).FirstOrDefault();
    }
}

public class PokemonEvolutionSource : MonoBehaviour, Interactable, IPlayerTriggerable {
    [Header("References")]
    [Tooltip("Specific evolution route to try. Empty uses the first eligible route found from Resources.")]
    [SerializeField] PokemonEvolutionDefinition evolution;
    [Tooltip("Player affected by context-menu/start triggers. Empty uses PlayerController.i.")]
    [SerializeField] PlayerController playerOverride;

    [Header("Targeting")]
    [Tooltip("Party slot used by this source.")]
    [Min(0)]
    [SerializeField] int partySlotIndex;
    [Tooltip("If enabled, the first healthy Pokemon is used instead of Party Slot.")]
    [SerializeField] bool useFirstHealthyPokemon = true;

    [Header("Trigger")]
    [Tooltip("Trigger type sent to the evolution route.")]
    [SerializeField] PokemonEvolutionTriggerKind triggerKind = PokemonEvolutionTriggerKind.Interaction;
    [Tooltip("Optional item context sent to item-gated routes.")]
    [SerializeField] ItemBase itemContext;
    [Tooltip("If enabled, trigger volumes may run this source repeatedly.")]
    [SerializeField] bool triggerRepeatedly;
    [Tooltip("If enabled, successful routes are applied immediately. Disable when future UI should ask first.")]
    [SerializeField] bool applyImmediately = true;
    [Tooltip("If enabled, result messages are written to GameDebug.")]
    [SerializeField] bool writeDebugLog;

    public bool TriggerRepeatedly => triggerRepeatedly;
    public PokemonEvolutionDefinition Evolution => evolution;
    public PokemonEvolutionTriggerKind TriggerKind => triggerKind;

    public IEnumerator Interact(Transform initiator) {
        TryRun(initiator != null ? initiator.GetComponent<PlayerController>() : ResolvePlayer(), out _);
        yield break;
    }

    public void OnPlayerTriggered(PlayerController player) {
        TryRun(player != null ? player : ResolvePlayer(), out _);
    }

    [ContextMenu("Try Evolution Source")]
    public void TryFromContextMenu() {
        TryRun(ResolvePlayer(), out _);
    }

    public bool TryRun(PlayerController player, out string message) {
        message = null;
        var pokemon = ResolvePokemon(player);
        if(pokemon == null) {
            message = "No Pokemon found for evolution source.";
            WriteDebug(message, warning: true);
            return false;
        }

        var context = PokemonEvolutionContext.FromPlayer(player);
        var route = evolution != null ? evolution : PokemonEvolutionService.FindFirstRoute(pokemon, player, triggerKind, itemContext, context);
        if(route == null) {
            message = "No eligible evolution route found.";
            WriteDebug(message, warning: true);
            return false;
        }

        if(!route.CanEvolve(pokemon, player, triggerKind, itemContext, context, out message)) {
            WriteDebug(message, warning: true);
            return false;
        }

        if(applyImmediately) {
            pokemon.Evolve(route, triggerKind, "evolution-source");
            message = $"{pokemon.NickName} evolved through {route.DisplayName}.";
        } else {
            message = $"{route.DisplayName} is eligible.";
        }

        WriteDebug(message, warning: false);
        return true;
    }

    Pokemon ResolvePokemon(PlayerController player) {
        var party = player != null ? player.GetComponent<PokemonParty>() : null;
        if(party == null || party.Pokemons == null) {
            return null;
        }

        if(useFirstHealthyPokemon) {
            return party.GetHealthyPokemon();
        }

        return partySlotIndex >= 0 && partySlotIndex < party.Pokemons.Count ? party.Pokemons[partySlotIndex] : null;
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
            GameDebug.Warning(message, GameDebugCategory.General, this, "PokemonEvolutionSource");
        } else {
            GameDebug.Success(message, GameDebugCategory.General, this, "PokemonEvolutionSource");
        }
    }
}
