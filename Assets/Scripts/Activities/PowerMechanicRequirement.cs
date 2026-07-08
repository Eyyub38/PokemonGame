using UnityEngine;

public enum PowerMechanicRequirementMode {
    MechanicUnlocked,
    MechanicNotUnlocked,
    MechanicUsed,
    MechanicUseCountAtLeast,
    KindUseCountAtLeast,
    ChargeReady,
    PokemonCanUseMechanic,
    BattleRuleAllowsMechanic
}

[CreateAssetMenu(menuName = "Activities/Requirements/Power Mechanic Requirement")]
public class PowerMechanicRequirement : ActivityRequirement {
    [Header("Target")]
    [Tooltip("How the player's power mechanic state should be checked.")]
    [SerializeField] PowerMechanicRequirementMode mode = PowerMechanicRequirementMode.MechanicUnlocked;
    [Tooltip("Specific mechanic used by mechanic-based checks.")]
    [SerializeField] PowerMechanicDefinition mechanic;
    [Tooltip("Mechanic kind used by kind-based checks.")]
    [SerializeField] PowerMechanicKind kind = PowerMechanicKind.MegaEvolution;
    [Tooltip("Optional battle rule set used by Battle Rule Allows Mechanic.")]
    [SerializeField] BattleRuleSetDefinition ruleSet;

    [Header("Threshold")]
    [Tooltip("Required count for count-based checks.")]
    [Min(0)]
    [SerializeField] int requiredCount = 1;
    [Tooltip("If enabled, blocked mechanic attempts also count.")]
    [SerializeField] bool includeBlockedAttempts;
    [Tooltip("If enabled, the final result is inverted.")]
    [SerializeField] bool invertResult;

    public override bool IsMet(PlayerController player) {
        bool met = Evaluate(player);
        return invertResult ? !met : met;
    }

    bool Evaluate(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerPowerMechanicLog>() : null;
        switch(mode) {
            case PowerMechanicRequirementMode.MechanicUnlocked:
                return log != null && log.HasUnlocked(mechanic);
            case PowerMechanicRequirementMode.MechanicNotUnlocked:
                return log == null || !log.HasUnlocked(mechanic);
            case PowerMechanicRequirementMode.MechanicUsed:
                return log != null && log.GetUseCount(mechanic, includeBlockedAttempts) > 0;
            case PowerMechanicRequirementMode.MechanicUseCountAtLeast:
                return log != null && log.GetUseCount(mechanic, includeBlockedAttempts) >= Mathf.Max(0, requiredCount);
            case PowerMechanicRequirementMode.KindUseCountAtLeast:
                return log != null && log.GetKindUseCount(kind, includeBlockedAttempts) >= Mathf.Max(0, requiredCount);
            case PowerMechanicRequirementMode.ChargeReady:
                return mechanic == null || log != null && log.CanSpendCharge(mechanic, out _);
            case PowerMechanicRequirementMode.PokemonCanUseMechanic:
                return CanAnyPartyPokemonUse(player);
            case PowerMechanicRequirementMode.BattleRuleAllowsMechanic:
                return ruleSet != null && mechanic != null && ruleSet.CanUsePowerMechanic(true, mechanic, 0, 0, out _);
            default:
                return false;
        }
    }

    bool CanAnyPartyPokemonUse(PlayerController player) {
        if(player == null || mechanic == null) {
            return false;
        }

        var party = player.GetComponent<PokemonParty>();
        if(party == null || party.Pokemons == null) {
            return false;
        }

        foreach(var pokemon in party.Pokemons) {
            if(pokemon != null && mechanic.CanUsePokemon(pokemon, out _)) {
                return true;
            }
        }

        return false;
    }
}
