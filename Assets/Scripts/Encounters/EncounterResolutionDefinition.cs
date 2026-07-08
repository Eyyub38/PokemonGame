using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum EncounterResolutionKind {
    Capture,
    Calm,
    Feed,
    Observe,
    Distract,
    Treat,
    Custom
}

public enum EncounterResolutionOutcome {
    EndEncounter,
    CapturePokemon,
    StartBattle,
    RecordSeenOnly,
    Flee,
    NoEffect
}

[CreateAssetMenu(menuName = "Encounters/Encounter Resolution Definition")]
public class EncounterResolutionDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this encounter resolution. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in UI/debug messages. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note explaining how this resolution should be used.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Free-form tags such as ranger, bait, stealth, research, care, rare or non-battle.")]
    [SerializeField] List<string> tags = new List<string>();
    [Tooltip("High-level kind of non-battle encounter action.")]
    [SerializeField] EncounterResolutionKind kind = EncounterResolutionKind.Calm;

    [Header("Chance")]
    [Tooltip("Base success chance before modifiers.")]
    [Range(0f, 100f)]
    [SerializeField] float baseChancePercent = 50f;
    [Tooltip("Minimum final success chance.")]
    [Range(0f, 100f)]
    [SerializeField] float minimumChancePercent = 0f;
    [Tooltip("Maximum final success chance.")]
    [Range(0f, 100f)]
    [SerializeField] float maximumChancePercent = 95f;
    [Tooltip("Chance bonus added when the Pokemon is already low HP.")]
    [SerializeField] float lowHpBonusPercent = 0f;
    [Tooltip("Chance penalty applied per Pokemon level.")]
    [SerializeField] float levelPenaltyPerLevel = 0f;
    [Tooltip("Chance modifiers based on Pokemon type.")]
    [SerializeField] List<EncounterResolutionTypeModifier> typeModifiers = new List<EncounterResolutionTypeModifier>();
    [Tooltip("Chance modifiers based on encounter source type.")]
    [SerializeField] List<EncounterResolutionSourceModifier> sourceModifiers = new List<EncounterResolutionSourceModifier>();

    [Header("Requirements")]
    [Tooltip("Reusable requirements that must pass before this resolution can be attempted.")]
    [SerializeField] List<ActivityRequirement> requirements = new List<ActivityRequirement>();
    [Tooltip("Items consumed before the attempt, such as bait, medicine, food or special tools.")]
    [SerializeField] List<ActivityItemCost> itemCosts = new List<ActivityItemCost>();

    [Header("Outcome")]
    [Tooltip("Outcome applied when the attempt succeeds.")]
    [SerializeField] EncounterResolutionOutcome successOutcome = EncounterResolutionOutcome.EndEncounter;
    [Tooltip("Outcome applied when the attempt fails.")]
    [SerializeField] EncounterResolutionOutcome failureOutcome = EncounterResolutionOutcome.StartBattle;
    [Tooltip("If enabled and success outcome captures Pokemon, the Pokemon is added to party/storage.")]
    [SerializeField] bool addPokemonOnCapture = true;
    [Tooltip("Optional Pokeball assigned to the Pokemon if Success Outcome is Capture Pokemon.")]
    [SerializeField] PokeballItem capturePokeball;
    [Tooltip("If enabled, a successful result can disable the source object.")]
    [SerializeField] bool disableSourceOnSuccess = true;

    [Header("Rewards")]
    [Tooltip("Activity outcomes applied after success.")]
    [SerializeField] List<ActivityOutcomeDefinition> successOutcomes = new List<ActivityOutcomeDefinition>();
    [Tooltip("Activity outcomes applied after failure.")]
    [SerializeField] List<ActivityOutcomeDefinition> failureOutcomes = new List<ActivityOutcomeDefinition>();

    [Header("Messages")]
    [Tooltip("Message shown if requirements or item costs block the attempt.")]
    [TextArea]
    [SerializeField] string blockedMessage = "This approach will not work right now.";
    [Tooltip("Message shown after success. {pokemon} is replaced with the Pokemon name.")]
    [TextArea]
    [SerializeField] string successMessage = "{pokemon} accepted your approach.";
    [Tooltip("Message shown after failure. {pokemon} is replaced with the Pokemon name.")]
    [TextArea]
    [SerializeField] string failureMessage = "{pokemon} became wary.";

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public EncounterResolutionKind Kind => kind;
    public float BaseChancePercent => baseChancePercent;
    public IReadOnlyList<ActivityRequirement> Requirements => requirements != null ? (IReadOnlyList<ActivityRequirement>)requirements : Array.Empty<ActivityRequirement>();
    public IReadOnlyList<ActivityItemCost> ItemCosts => itemCosts != null ? (IReadOnlyList<ActivityItemCost>)itemCosts : Array.Empty<ActivityItemCost>();
    public EncounterResolutionOutcome SuccessOutcome => successOutcome;
    public EncounterResolutionOutcome FailureOutcome => failureOutcome;
    public IReadOnlyList<ActivityOutcomeDefinition> SuccessOutcomes => successOutcomes != null ? (IReadOnlyList<ActivityOutcomeDefinition>)successOutcomes : Array.Empty<ActivityOutcomeDefinition>();
    public IReadOnlyList<ActivityOutcomeDefinition> FailureOutcomes => failureOutcomes != null ? (IReadOnlyList<ActivityOutcomeDefinition>)failureOutcomes : Array.Empty<ActivityOutcomeDefinition>();

    public bool CanAttempt(PlayerController player, out string failureMessage) {
        if(player == null) {
            failureMessage = "No player was provided for this encounter resolution.";
            return false;
        }

        foreach(var requirement in Requirements) {
            if(requirement == null) {
                continue;
            }

            if(!requirement.IsMet(player)) {
                failureMessage = string.IsNullOrWhiteSpace(requirement.FailureMessage) ? blockedMessage : requirement.FailureMessage;
                return false;
            }
        }

        var inventory = player.GetComponent<Inventory>();
        foreach(var cost in ItemCosts) {
            if(cost == null || cost.item == null || cost.count <= 0) {
                continue;
            }

            if(inventory == null || !inventory.HasItemEnough(cost.item, cost.count)) {
                failureMessage = $"You need {cost.count}x {cost.item.Name}.";
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    public float PreviewChance(Pokemon pokemon, EncounterSourceType sourceType) {
        return CalculateChance(pokemon, sourceType);
    }

    public EncounterResolutionResult TryResolve(PlayerController player, Pokemon pokemon, EncounterSourceType sourceType, EncounterTableDefinition table, BattleTrigger battleTrigger, UnityEngine.Object context) {
        var result = new EncounterResolutionResult {
            definition = this,
            resolutionId = Id,
            resolutionName = DisplayName,
            kind = kind,
            pokemon = pokemon,
            sourceType = sourceType,
            table = table,
            battleTrigger = battleTrigger,
            successOutcome = successOutcome,
            failureOutcome = failureOutcome
        };

        if(player == null || pokemon == null) {
            result.blocked = true;
            result.message = "Resolution attempt has no player or Pokemon.";
            return result;
        }

        if(!CanAttempt(player, out var blockReason)) {
            result.blocked = true;
            result.message = string.IsNullOrWhiteSpace(blockReason) ? blockedMessage : blockReason;
            return result;
        }

        ConsumeCosts(player);
        result.chancePercent = CalculateChance(pokemon, sourceType);
        result.success = UnityEngine.Random.value * 100f <= result.chancePercent;
        result.message = FormatMessage(result.success ? successMessage : failureMessage, pokemon);
        ApplyOutcome(player, result.success ? successOutcome : failureOutcome, result, context);
        ApplyActivityOutcomes(player, result.success ? SuccessOutcomes : FailureOutcomes);
        player.GetComponent<PlayerEncounterLog>()?.RecordResolutionAttempt(pokemon, sourceType, table, Id, DisplayName, result.success, result.chancePercent, result.message);
        PublishResult(player, result, context);
        return result;
    }

    float CalculateChance(Pokemon pokemon, EncounterSourceType sourceType) {
        float chance = baseChancePercent;

        if(pokemon != null) {
            chance -= pokemon.Level * levelPenaltyPerLevel;
            if(pokemon.MaxHp > 0 && pokemon.HP <= pokemon.MaxHp / 3) {
                chance += lowHpBonusPercent;
            }

            foreach(var typeModifier in typeModifiers) {
                if(typeModifier != null && typeModifier.Matches(pokemon)) {
                    chance += typeModifier.chanceModifierPercent;
                }
            }
        }

        foreach(var sourceModifier in sourceModifiers) {
            if(sourceModifier != null && sourceModifier.sourceType == sourceType) {
                chance += sourceModifier.chanceModifierPercent;
            }
        }

        return Mathf.Clamp(chance, minimumChancePercent, maximumChancePercent);
    }

    void ConsumeCosts(PlayerController player) {
        var inventory = player != null ? player.GetComponent<Inventory>() : null;
        if(inventory == null) {
            return;
        }

        foreach(var cost in ItemCosts) {
            if(cost != null && cost.item != null && cost.count > 0) {
                inventory.RemoveItem(cost.item, cost.count);
            }
        }
    }

    void ApplyOutcome(PlayerController player, EncounterResolutionOutcome outcome, EncounterResolutionResult result, UnityEngine.Object context) {
        switch(outcome) {
            case EncounterResolutionOutcome.CapturePokemon:
                if(result.pokemon != null) {
                    result.pokemon.Pokeball = capturePokeball;
                    if(addPokemonOnCapture) {
                        player.GetComponent<PokemonParty>()?.AddPokemon(result.pokemon);
                    }
                    player.GetComponent<PlayerEncounterLog>()?.RecordCaptured(result.pokemon, result.sourceType, result.table, stealth: true);
                }
                result.startResult = EncounterStartResult.Captured;
                result.disableSource = disableSourceOnSuccess;
                break;
            case EncounterResolutionOutcome.StartBattle:
                result.startResult = EncounterSystem.StartBattle(player, result.pokemon, result.sourceType, result.table, result.battleTrigger, context);
                break;
            case EncounterResolutionOutcome.RecordSeenOnly:
                player.GetComponent<PlayerEncounterLog>()?.RecordSeen(result.pokemon, result.sourceType, result.table);
                result.startResult = EncounterStartResult.NoEncounter;
                break;
            case EncounterResolutionOutcome.Flee:
                player.GetComponent<PlayerEncounterLog>()?.RecordSeen(result.pokemon, result.sourceType, result.table);
                result.startResult = EncounterStartResult.NoEncounter;
                result.disableSource = result.success && disableSourceOnSuccess;
                break;
            case EncounterResolutionOutcome.EndEncounter:
                player.GetComponent<PlayerEncounterLog>()?.RecordSeen(result.pokemon, result.sourceType, result.table);
                result.startResult = EncounterStartResult.NoEncounter;
                result.disableSource = result.success && disableSourceOnSuccess;
                break;
            default:
                result.startResult = EncounterStartResult.NoEncounter;
                break;
        }
    }

    void ApplyActivityOutcomes(PlayerController player, IReadOnlyList<ActivityOutcomeDefinition> outcomes) {
        if(player == null || outcomes == null) {
            return;
        }

        foreach(var outcome in outcomes) {
            outcome?.TryApply(player);
        }
    }

    void PublishResult(PlayerController player, EncounterResolutionResult result, UnityEngine.Object context) {
        GameEventPublishing.PublishOptional(
            null,
            $"encounter-resolution.{(result.success ? "success" : "failed")}.{Id}",
            result.message,
            GameEventCategory.Encounter,
            result.success ? GameEventImportance.Success : GameEventImportance.Info,
            context != null ? context : player,
            "EncounterResolution",
            GameEventScope.Scene,
            showInFeed: true,
            writeToDebugLog: true,
            GameEventPublishing.Value("resolutionId", Id),
            GameEventPublishing.Value("resolutionName", DisplayName),
            GameEventPublishing.Value("kind", kind),
            GameEventPublishing.Value("sourceType", result.sourceType),
            GameEventPublishing.Value("pokemon", result.pokemon != null && result.pokemon.Base != null ? result.pokemon.Base.Name : string.Empty),
            GameEventPublishing.Value("chance", result.chancePercent));
    }

    string FormatMessage(string template, Pokemon pokemon) {
        string pokemonName = pokemon != null && pokemon.Base != null ? pokemon.Base.Name : "Pokemon";
        return string.IsNullOrWhiteSpace(template) ? pokemonName : template.Replace("{pokemon}", pokemonName);
    }
}

[Serializable]
public class EncounterResolutionTypeModifier {
    [Tooltip("Pokemon type that receives this chance modifier.")]
    public PokemonType pokemonType = PokemonType.None;
    [Tooltip("Chance added when the Pokemon has this type.")]
    public float chanceModifierPercent;

    public bool Matches(Pokemon pokemon) {
        return pokemon != null && pokemon.Base != null && pokemonType != PokemonType.None && (pokemon.Base.Type1 == pokemonType || pokemon.Base.Type2 == pokemonType);
    }
}

[Serializable]
public class EncounterResolutionSourceModifier {
    [Tooltip("Encounter source type that receives this chance modifier.")]
    public EncounterSourceType sourceType = EncounterSourceType.Any;
    [Tooltip("Chance added when the encounter uses this source type.")]
    public float chanceModifierPercent;
}

public class EncounterResolutionResult {
    public EncounterResolutionDefinition definition;
    public string resolutionId;
    public string resolutionName;
    public EncounterResolutionKind kind;
    public Pokemon pokemon;
    public EncounterSourceType sourceType;
    public EncounterTableDefinition table;
    public BattleTrigger battleTrigger;
    public EncounterResolutionOutcome successOutcome;
    public EncounterResolutionOutcome failureOutcome;
    public EncounterStartResult startResult;
    public bool blocked;
    public bool success;
    public bool disableSource;
    public float chancePercent;
    public string message;
}

public class EncounterResolutionSource : MonoBehaviour, Interactable, IPlayerTriggerable {
    [Header("Resolution")]
    [Tooltip("Resolution definition used by this source.")]
    [SerializeField] EncounterResolutionDefinition resolution;
    [Tooltip("Exact Pokemon species used by this source. If empty, Encounter Table is rolled.")]
    [SerializeField] PokemonBase pokemon;
    [Tooltip("Minimum level used when Exact Pokemon is assigned.")]
    [Min(1)]
    [SerializeField] int minLevel = 2;
    [Tooltip("Maximum level used when Exact Pokemon is assigned.")]
    [Min(1)]
    [SerializeField] int maxLevel = 4;
    [Tooltip("Encounter table rolled when Exact Pokemon is empty.")]
    [SerializeField] EncounterTableDefinition encounterTable;
    [Tooltip("Optional source override. Any uses the table source type.")]
    [SerializeField] EncounterSourceType sourceOverride = EncounterSourceType.Any;
    [Tooltip("Battle trigger used if the resolution starts battle.")]
    [SerializeField] BattleTrigger battleTrigger = BattleTrigger.LongGrass;

    [Header("Activation")]
    [Tooltip("If enabled, touching this object can run the resolution.")]
    [SerializeField] bool triggerOnTouch;
    [Tooltip("If enabled, interacting with this object can run the resolution.")]
    [SerializeField] bool interactOnUse = true;
    [Tooltip("If enabled, trigger activation can repeat while the player remains in the trigger.")]
    [SerializeField] bool triggerRepeatedly;
    [Tooltip("If enabled, result messages are shown through DialogManager when available.")]
    [SerializeField] bool showResultMessages = true;

    public EncounterResolutionDefinition Resolution => resolution;
    public PokemonBase Pokemon => pokemon;
    public EncounterTableDefinition EncounterTable => encounterTable;
    public EncounterSourceType SourceOverride => sourceOverride;
    public bool TriggerRepeatedly => triggerRepeatedly;

    public void OnPlayerTriggered(PlayerController player) {
        if(triggerOnTouch) {
            Execute(player);
        }
    }

    public IEnumerator Interact(Transform initiator) {
        if(!interactOnUse) {
            yield break;
        }

        var player = initiator != null ? initiator.GetComponent<PlayerController>() : PlayerController.i;
        var result = Execute(player);
        if(showResultMessages && result != null && !string.IsNullOrWhiteSpace(result.message) && DialogManager.i != null) {
            yield return DialogManager.i.ShowDialogText(result.message);
        }
    }

    public EncounterResolutionResult Execute(PlayerController player) {
        if(resolution == null) {
            return new EncounterResolutionResult {
                blocked = true,
                message = "Encounter resolution source has no resolution definition."
            };
        }

        var encounterPokemon = CreatePokemon(player);
        if(encounterPokemon == null) {
            return new EncounterResolutionResult {
                blocked = true,
                message = "No Pokemon could be resolved."
            };
        }

        var sourceType = ResolveSourceType();
        var trigger = encounterTable != null ? encounterTable.BattleTrigger : battleTrigger;
        var result = resolution.TryResolve(player, encounterPokemon, sourceType, encounterTable, trigger, this);
        if(result.disableSource) {
            gameObject.SetActive(false);
        }
        return result;
    }

    Pokemon CreatePokemon(PlayerController player) {
        if(pokemon != null) {
            return new Pokemon(pokemon, UnityEngine.Random.Range(Mathf.Max(1, minLevel), Mathf.Max(minLevel, maxLevel) + 1));
        }

        if(encounterTable != null && encounterTable.RollPokemon(player, out var rolledPokemon, out _)) {
            return rolledPokemon;
        }

        return null;
    }

    EncounterSourceType ResolveSourceType() {
        if(sourceOverride != EncounterSourceType.Any) {
            return sourceOverride;
        }

        return encounterTable != null ? encounterTable.SourceType : EncounterSourceType.Special;
    }
}
