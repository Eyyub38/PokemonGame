using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Items/Create new recovery item")]
public class RecoveryItem : ItemBase{
    [Header("HP")]
    [Tooltip("HP restored. 0 means this item does not restore fixed HP.")]
    [Min(0)]
    [SerializeField] int hpAmount;
    [Tooltip("Percent of Max HP restored instantly. 0 means no percentage HP restore.")]
    [Range(0f, 1f)]
    [SerializeField] float hpPercentAmount;
    [Tooltip("If enabled, restores HP to maximum.")]
    [SerializeField] bool restoreMaxHp;

    [Header("Vital Resources")]
    [Tooltip("Vital profile used for core health/stamina calculations. Empty uses default formulas.")]
    [SerializeField] PokemonVitalProfileDefinition vitalProfile;
    [Tooltip("If enabled, vital effects can be applied to a fainted Pokemon.")]
    [SerializeField] bool allowFaintedPokemonForVitalEffects;
    [Tooltip("If enabled, restores core health, core stamina and battle stamina to full.")]
    [SerializeField] bool restoreAllVitalsToFull;
    [Tooltip("If enabled, restores only core health and core stamina to full.")]
    [SerializeField] bool restoreCoreVitalsToFull;
    [Tooltip("If enabled, restores only battle stamina to full.")]
    [SerializeField] bool restoreBattleVitalsToFull;
    [Tooltip("Fine-grained vital changes. Positive restores, negative drains/damages.")]
    [SerializeField] List<PokemonVitalChange> vitalChanges = new List<PokemonVitalChange>();
    [Tooltip("Percent of max core health restored instantly.")]
    [Range(0f, 1f)]
    [SerializeField] float coreHealthPercentAmount;
    [Tooltip("Percent of max core physical stamina restored instantly.")]
    [Range(0f, 1f)]
    [SerializeField] float corePhysicalStaminaPercentAmount;
    [Tooltip("Percent of max core elemental stamina restored instantly.")]
    [Range(0f, 1f)]
    [SerializeField] float coreElementalStaminaPercentAmount;
    [Tooltip("Percent of max battle physical stamina restored instantly.")]
    [Range(0f, 1f)]
    [SerializeField] float battlePhysicalStaminaPercentAmount;
    [Tooltip("Percent of max battle elemental stamina restored instantly.")]
    [Range(0f, 1f)]
    [SerializeField] float battleElementalStaminaPercentAmount;

    [Header("Timed Recovery And Stat Effects")]
    [Tooltip("How many Pokemon turns the timed recovery/stat effect lasts. 0 means no turn duration.")]
    [Min(0)]
    [SerializeField] int timedEffectTurns;
    [Tooltip("How many completed battles the timed recovery/stat effect lasts. 0 means no battle duration.")]
    [Min(0)]
    [SerializeField] int timedEffectBattles;
    [Tooltip("Multiplier applied to future positive HP healing while the timed effect is active. 1 means unchanged.")]
    [Min(0f)]
    [SerializeField] float healingReceivedMultiplier = 1f;
    [Tooltip("Multiplier applied to future positive vital recovery while the timed effect is active. 1 means unchanged.")]
    [Min(0f)]
    [SerializeField] float vitalRecoveryMultiplier = 1f;
    [Tooltip("Percent of Max HP restored at the end of each Pokemon turn while the timed effect is active.")]
    [Range(0f, 1f)]
    [SerializeField] float hpPercentPerTurn;
    [Tooltip("Percent of max core health restored at the end of each Pokemon turn.")]
    [Range(0f, 1f)]
    [SerializeField] float coreHealthPercentPerTurn;
    [Tooltip("Percent of max core physical stamina restored at the end of each Pokemon turn.")]
    [Range(0f, 1f)]
    [SerializeField] float corePhysicalStaminaPercentPerTurn;
    [Tooltip("Percent of max core elemental stamina restored at the end of each Pokemon turn.")]
    [Range(0f, 1f)]
    [SerializeField] float coreElementalStaminaPercentPerTurn;
    [Tooltip("Percent of max battle physical stamina restored at the end of each Pokemon turn.")]
    [Range(0f, 1f)]
    [SerializeField] float battlePhysicalStaminaPercentPerTurn;
    [Tooltip("Percent of max battle elemental stamina restored at the end of each Pokemon turn.")]
    [Range(0f, 1f)]
    [SerializeField] float battleElementalStaminaPercentPerTurn;
    [Tooltip("Temporary stat modifiers active while this item's timed effect remains.")]
    [SerializeField] List<PowerMechanicStatModifier> timedStatModifiers = new List<PowerMechanicStatModifier>();

    [Header("PP")]
    [Tooltip("PP restored to each move. 0 means no fixed PP restore.")]
    [Min(0)]
    [SerializeField] int ppAmount;
    [Tooltip("If enabled, restores all move PP to maximum.")]
    [SerializeField] bool restoreMaxPp;

    [Header("Status")]
    [Tooltip("Specific status condition this item cures.")]
    [SerializeField] StatusConditionID status;
    [Tooltip("If enabled, cures all regular and volatile status conditions.")]
    [SerializeField] bool recoverAllStatus;

    [Header("Revive")]
    [Tooltip("If enabled, revives a fainted Pokemon to half HP.")]
    [SerializeField] bool revive;
    [Tooltip("If enabled, revives a fainted Pokemon to full HP.")]
    [SerializeField] bool maxRevive;

    public override bool Use(Pokemon pokemon){
        if(pokemon == null) {
            return false;
        }

        bool used = false;
        used |= ApplyVitalEffects(pokemon);

        if(revive || maxRevive){
            if(pokemon.HP <= 0){
                if(revive){
                    used |= pokemon.IncreaseHPWithResult(pokemon.MaxHp / 2);
                } else if(maxRevive){
                    used |= pokemon.IncreaseHPWithResult(pokemon.MaxHp);
                }

                pokemon.CureStatus();
                used = true;
            }

            if(!CanApplyToFaintedPokemon() && pokemon.HP <= 0){
                return used;
            }
        }

        if(pokemon.HP <= 0 && !CanApplyToFaintedPokemon()){
            return false;
        }

        if(restoreMaxHp || hpAmount > 0 || hpPercentAmount > 0f){
            if(restoreMaxHp){
                used |= pokemon.IncreaseHPWithResult(pokemon.MaxHp);
            } else {
                int percentHeal = hpPercentAmount > 0f ? Mathf.Max(1, Mathf.RoundToInt(pokemon.MaxHp * hpPercentAmount)) : 0;
                used |= pokemon.IncreaseHPWithResult(hpAmount + percentHeal);
            }
        }

        if(recoverAllStatus || status != StatusConditionID.None) {
            if(pokemon.Status == null && pokemon.VolatileStatus == null){
                // Other effects may still apply.
            } else if(recoverAllStatus){
                pokemon.CureStatus();
                pokemon.CureVolatileStatus();
                used = true;
            } else {
                if(pokemon.Status != null && pokemon.Status.Id == status ){
                    pokemon.CureStatus();
                    used = true;
                } else if(pokemon.VolatileStatus != null && pokemon.VolatileStatus.Id == status){
                    pokemon.CureVolatileStatus();
                    used = true;
                }
            }
        }

        if(restoreMaxPp){
            used |= RestoreMovePP(pokemon, int.MaxValue);
        } else if(ppAmount > 0){
            used |= RestoreMovePP(pokemon, ppAmount);
        }

        used |= ApplyTimedEffect(pokemon);

        return used;
    }

    bool RestoreMovePP(Pokemon pokemon, int amount) {
        if(pokemon?.Moves == null || pokemon.Moves.Count == 0) {
            return false;
        }

        bool changed = false;
        foreach(var move in pokemon.Moves) {
            if(move == null || move.PP >= move.Base.PP) {
                continue;
            }

            move.IncreasePP(amount);
            changed = true;
        }

        return changed;
    }

    bool ApplyVitalEffects(Pokemon pokemon) {
        if(pokemon == null) {
            return false;
        }

        bool changed = false;
        if(restoreAllVitalsToFull) {
            pokemon.RestoreVitalsToFull(vitalProfile);
            return true;
        }

        if(restoreCoreVitalsToFull) {
            pokemon.RestoreCoreVitalsToFull(vitalProfile);
            changed = true;
        }

        if(restoreBattleVitalsToFull) {
            pokemon.RestoreBattleVitalsToFull(vitalProfile);
            changed = true;
        }

        foreach(var change in vitalChanges) {
            if(change != null && change.Apply(pokemon, vitalProfile)) {
                changed = true;
            }
        }

        changed |= RestorePercent(pokemon, PokemonVitalResourceKind.CoreHealth, coreHealthPercentAmount);
        changed |= RestorePercent(pokemon, PokemonVitalResourceKind.CorePhysicalStamina, corePhysicalStaminaPercentAmount);
        changed |= RestorePercent(pokemon, PokemonVitalResourceKind.CoreElementalStamina, coreElementalStaminaPercentAmount);
        changed |= RestorePercent(pokemon, PokemonVitalResourceKind.BattlePhysicalStamina, battlePhysicalStaminaPercentAmount);
        changed |= RestorePercent(pokemon, PokemonVitalResourceKind.BattleElementalStamina, battleElementalStaminaPercentAmount);

        return changed;
    }

    bool RestorePercent(Pokemon pokemon, PokemonVitalResourceKind resource, float percent) {
        if(pokemon == null || percent <= 0f) {
            return false;
        }

        int amount = Mathf.Max(1, Mathf.RoundToInt(pokemon.GetVitalMax(resource, vitalProfile) * percent));
        return pokemon.ChangeVitalResource(resource, amount, vitalProfile) != 0;
    }

    bool ApplyTimedEffect(Pokemon pokemon) {
        if(pokemon == null || !HasTimedEffect()) {
            return false;
        }

        pokemon.AddTimedRecoveryEffect(new PokemonTimedRecoveryEffect(
            Name,
            Name,
            timedEffectTurns,
            timedEffectBattles,
            healingReceivedMultiplier,
            vitalRecoveryMultiplier,
            hpPercentPerTurn,
            coreHealthPercentPerTurn,
            corePhysicalStaminaPercentPerTurn,
            coreElementalStaminaPercentPerTurn,
            battlePhysicalStaminaPercentPerTurn,
            battleElementalStaminaPercentPerTurn,
            timedStatModifiers));
        return true;
    }

    bool HasTimedEffect() {
        bool hasDuration = timedEffectTurns > 0 || timedEffectBattles > 0;
        if(!hasDuration) {
            return false;
        }

        return !Mathf.Approximately(healingReceivedMultiplier, 1f)
            || !Mathf.Approximately(vitalRecoveryMultiplier, 1f)
            || hpPercentPerTurn > 0f
            || coreHealthPercentPerTurn > 0f
            || corePhysicalStaminaPercentPerTurn > 0f
            || coreElementalStaminaPercentPerTurn > 0f
            || battlePhysicalStaminaPercentPerTurn > 0f
            || battleElementalStaminaPercentPerTurn > 0f
            || (timedStatModifiers != null && timedStatModifiers.Count > 0);
    }

    bool CanApplyToFaintedPokemon() {
        return allowFaintedPokemonForVitalEffects
            || revive
            || maxRevive
            || restoreAllVitalsToFull
            || restoreCoreVitalsToFull
            || coreHealthPercentAmount > 0f
            || (vitalChanges != null && vitalChanges.Exists(change => change != null && change.resource == PokemonVitalResourceKind.CoreHealth && change.amount > 0));
    }
}
