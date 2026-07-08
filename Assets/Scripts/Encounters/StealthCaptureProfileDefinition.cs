using UnityEngine;

[CreateAssetMenu(menuName = "Encounters/Stealth Capture Profile")]
public class StealthCaptureProfileDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this stealth capture profile. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in UI/debug messages. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer note for how this stealth capture profile should be used.")]
    [TextArea]
    [SerializeField] string description;

    [Header("Capture")]
    [Tooltip("Base success chance before Pokemon catch rate, level and Pokeball modifiers.")]
    [Range(0f, 100f)]
    [SerializeField] float baseSuccessChancePercent = 25f;
    [Tooltip("If enabled, one Pokeball is consumed when attempting a stealth capture.")]
    [SerializeField] bool consumePokeball = true;
    [Tooltip("Optional Pokeball used when the inventory lookup cannot find a ball or when Consume Pokeball is disabled.")]
    [SerializeField] PokeballItem defaultPokeball;
    [Tooltip("If enabled, a failed stealth capture can immediately start a normal wild battle.")]
    [SerializeField] bool startBattleOnFailure = true;
    [Tooltip("If enabled, successful stealth capture adds the Pokemon to the player's party/storage.")]
    [SerializeField] bool addPokemonOnSuccess = true;
    [Tooltip("Minimum final chance after all modifiers.")]
    [Range(0f, 100f)]
    [SerializeField] float minimumChancePercent = 1f;
    [Tooltip("Maximum final chance after all modifiers.")]
    [Range(0f, 100f)]
    [SerializeField] float maximumChancePercent = 95f;

    [Header("Messages")]
    [Tooltip("Message shown when no Pokeball is available.")]
    [SerializeField] string noPokeballMessage = "You need a Pokeball.";
    [Tooltip("Message shown after a successful stealth capture. {pokemon} is replaced with the Pokemon name.")]
    [SerializeField] string successMessage = "{pokemon} was caught quietly.";
    [Tooltip("Message shown after a failed stealth capture. {pokemon} is replaced with the Pokemon name.")]
    [SerializeField] string failureMessage = "{pokemon} noticed you.";

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public bool ConsumePokeball => consumePokeball;
    public bool StartBattleOnFailure => startBattleOnFailure;
    public bool AddPokemonOnSuccess => addPokemonOnSuccess;
    public string NoPokeballMessage => noPokeballMessage;

    public EncounterCaptureResult TryCapture(PlayerController player, Pokemon pokemon, EncounterSourceType sourceType) {
        var result = new EncounterCaptureResult {
            pokemon = pokemon,
            sourceType = sourceType,
            shouldStartBattle = startBattleOnFailure
        };

        if(player == null || pokemon == null) {
            result.message = "Capture attempt has no player or Pokemon.";
            return result;
        }

        var inventory = player.GetComponent<Inventory>();
        var pokeball = FindPokeball(inventory);
        if(consumePokeball && pokeball == null) {
            result.message = noPokeballMessage;
            result.shouldStartBattle = false;
            return result;
        }

        float chance = CalculateChance(pokemon, pokeball);
        result.chancePercent = chance;
        result.usedPokeball = pokeball;
        result.success = Random.value * 100f <= chance;

        if(consumePokeball && pokeball != null) {
            inventory.RemoveItem(pokeball);
        }

        if(result.success) {
            pokemon.Pokeball = pokeball;
            if(addPokemonOnSuccess) {
                player.GetComponent<PokemonParty>()?.AddPokemon(pokemon);
            }
            result.shouldStartBattle = false;
            result.message = FormatMessage(successMessage, pokemon);
        } else {
            result.message = FormatMessage(failureMessage, pokemon);
        }

        return result;
    }

    float CalculateChance(Pokemon pokemon, PokeballItem pokeball) {
        if(pokemon == null || pokemon.Base == null) {
            return 0f;
        }

        float catchRateFactor = Mathf.Clamp(pokemon.Base.CatchRate / 255f, 0.05f, 2f);
        float levelFactor = Mathf.Clamp01(1f - (pokemon.Level - 1) * 0.015f);
        float ballModifier = pokeball != null ? pokeball.CatchRateModifier : defaultPokeball != null ? defaultPokeball.CatchRateModifier : 1f;
        float chance = baseSuccessChancePercent * catchRateFactor * Mathf.Max(0.1f, levelFactor) * Mathf.Max(0.1f, ballModifier);
        return Mathf.Clamp(chance, minimumChancePercent, maximumChancePercent);
    }

    PokeballItem FindPokeball(Inventory inventory) {
        if(!consumePokeball) {
            return defaultPokeball;
        }

        if(inventory == null) {
            return defaultPokeball;
        }

        var slots = inventory.GetItemSlotsByCategory((int)ItemCategory.Pokeball);
        foreach(var slot in slots) {
            if(slot != null && slot.Count > 0 && slot.Item is PokeballItem pokeball) {
                return pokeball;
            }
        }

        return defaultPokeball != null && inventory.HasItemEnough(defaultPokeball) ? defaultPokeball : null;
    }

    string FormatMessage(string template, Pokemon pokemon) {
        string pokemonName = pokemon != null && pokemon.Base != null ? pokemon.Base.Name : "Pokemon";
        return string.IsNullOrWhiteSpace(template) ? pokemonName : template.Replace("{pokemon}", pokemonName);
    }
}

public class EncounterCaptureResult {
    public Pokemon pokemon;
    public EncounterSourceType sourceType;
    public PokeballItem usedPokeball;
    public bool success;
    public bool shouldStartBattle;
    public float chancePercent;
    public string message;
}
