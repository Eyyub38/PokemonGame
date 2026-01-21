using System;
using System.Linq;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum AbilityID {
    None,
    Blaze, Overgrow, Torrent, Swarm, Guts, MarvelScale, QuickFeet, CompoundEyes, //Abilities that boost stats
    KeenEye, HyperCutter, BigPecks, ClearBody, WhiteSmoke,                                   //Abilities that prevent stat reduction
    Insomnia, Immunity, Limber, WaterVeil, VitalSpirit, OwnTempo,                            //Abilities that prevent status conditions
    Static, PoisonPoint, FlameBody,                                                                             //Abilities that inflict status conditions on contact
    ToughClaws, StrongJaw, IronFist                                                                         //Abilities that power up moves
}

public class AbilityDB{
    public static Dictionary<AbilityID, Ability> Abilities = new Dictionary<AbilityID, Ability>(){
        { AbilityID.Blaze,
            new Ability(){
                Name = "Blaze",
                Description = "Powers up Fire-type moves when the Pokémon is in trouble.",
                OnModifyAttack = (float attack, Pokemon attacker, Pokemon defender, Move move) => {
                    if(move.Base.Type == PokemonType.Fire && attacker.HP <= attacker.MaxHp / 3){
                        attack *= 1.5f; 
                    }
                    
                    return attack;
                },
                OnModifySpAttack = (float spAttack, Pokemon attacker, Pokemon defender, Move move) => {
                    if(move.Base.Type == PokemonType.Fire && attacker.HP <= attacker.MaxHp / 3){
                        spAttack *= 1.5f; 
                    }
                    
                    return spAttack;
                }
            }
        },
        { AbilityID.Overgrow,
            new Ability(){
                Name = "Overgrow",
                Description = "Powers up Grass-type moves when the Pokémon is in trouble.",
                OnModifyAttack = (float attack, Pokemon attacker, Pokemon defender, Move move) => {
                    if(move.Base.Type == PokemonType.Grass && attacker.HP <= attacker.MaxHp / 3){
                        attack *= 1.5f; 
                    }
                    
                    return attack;
                },
                OnModifySpAttack = (float spAttack, Pokemon attacker, Pokemon defender, Move move) => {
                    if(move.Base.Type == PokemonType.Grass && attacker.HP <= attacker.MaxHp / 3){
                        spAttack *= 1.5f; 
                    }
                    
                    return spAttack;
                }
            }
        },
        { AbilityID.Torrent,
            new Ability(){
                Name = "Torrent",
                Description = "Powers up Water-type moves when the Pokémon is in trouble.",
                OnModifyAttack = (float attack, Pokemon attacker, Pokemon defender, Move move) => {
                    if(move.Base.Type == PokemonType.Water && attacker.HP <= attacker.MaxHp / 3){
                        attack *= 1.5f; 
                    }
                    
                    return attack;
                },
                OnModifySpAttack = (float spAttack, Pokemon attacker, Pokemon defender, Move move) => {
                    if(move.Base.Type == PokemonType.Water && attacker.HP <= attacker.MaxHp / 3){
                        spAttack *= 1.5f; 
                    }
                    
                    return spAttack;
                }
            }
        },
        { AbilityID.Swarm,
            new Ability(){
                Name = "Swarm",
                Description = "Powers up Bug-type moves when the Pokémon is in trouble.",
                OnModifyAttack = (float attack, Pokemon attacker, Pokemon defender, Move move) => {
                    if(move.Base.Type == PokemonType.Bug && attacker.HP <= attacker.MaxHp / 3){
                        attack *= 1.5f; 
                    }
                    
                    return attack;
                },
                OnModifySpAttack = (float spAttack, Pokemon attacker, Pokemon defender, Move move) => {
                    if(move.Base.Type == PokemonType.Bug && attacker.HP <= attacker.MaxHp / 3){
                        spAttack *= 1.5f; 
                    }
                    
                    return spAttack;
                }
            }
        },
        { AbilityID.Guts,
            new Ability(){
                Name = "Guts",
                Description = "Boosts the Pokémon's Attack stat if it has a status condition.",
                OnModifyAttack = (float attack, Pokemon attacker, Pokemon defender, Move move) => {
                    if(attacker.Status != null){
                        attack *= 1.5f; 
                    }
                    
                    return attack;
                },
            }
        },
        {AbilityID.MarvelScale,
            new Ability(){
                Name = "Marvel Scale",
                Description = "Boosts the Pokémon's Defense stat if it has a status condition.",
                OnModifyDefense = (float defense, Pokemon attacker, Pokemon defender, Move move) => {
                    if(defender.Status != null){
                        defense *= 1.5f; 
                    }
                    
                    return defense;
                },
            }
        },
        {AbilityID.QuickFeet,
            new Ability(){
                Name = "Quick Feet",
                Description = "Boosts the Pokémon's Speed stat if it has a status condition.",
                OnModifySpeed = (float speed, Pokemon attacker, Pokemon defender, Move move) => {
                    if(defender.Status != null){
                        speed *= 1.5f; 
                    }
                    
                    return speed;
                },
            }
        },
        {AbilityID.CompoundEyes,
            new Ability(){
                Name = "Compound Eyes",
                Description = "Boosts the Pokémon's accuracy stat.",
                OnModifyAccuracy = (float accuracy, Pokemon attacker, Pokemon defender, Move move) => {
                    return accuracy *= 1.3f;
                }
            }
        },
        {AbilityID.KeenEye,
            new Ability(){
                Name = "Keen Eye",
                Description = "Prevents other Pokémon from lowering this Pokémon's accuracy stat.",
                OnBoost = (Dictionary<Stat, int> boosts, Pokemon source, Pokemon target) => {
                    if(source != null && source == target){
                        return;
                    }

                    if(boosts.ContainsKey(Stat.Accuracy) && boosts[Stat.Accuracy] < 0){
                        boosts.Remove(Stat.Accuracy);

                        target.AddStatusEvet(StatusEventType.Text, $"{target.Base.Name}'s accuracy won't go down because of its Keen Eye!");
                    }
                }
            }
        },
        {AbilityID.HyperCutter,
            new Ability(){
                Name = "Hyper Cutter",
                Description = "Prevents other Pokémon from lowering this Pokémon's Attack stat.",
                OnBoost = (Dictionary<Stat, int> boosts, Pokemon source, Pokemon target) => {
                    if(source != null && source == target){
                        return;
                    }

                    if(boosts.ContainsKey(Stat.Attack) && boosts[Stat.Attack] < 0){
                        boosts.Remove(Stat.Attack);

                        target.AddStatusEvet(StatusEventType.Text, $"{target.Base.Name}'s attack won't go down because of its Hyper Cutter!");
                    }
                }
            }
        },
        {AbilityID.BigPecks,
            new Ability(){
                Name = "Big Pecks",
                Description = "Prevents other Pokémon from lowering this Pokémon's Defense stat.",
                OnBoost = (Dictionary<Stat, int> boosts, Pokemon source, Pokemon target) => {
                    if(source != null && source == target){
                        return;
                    }

                    if(boosts.ContainsKey(Stat.Defense) && boosts[Stat.Defense] < 0){
                        boosts.Remove(Stat.Defense);

                        target.AddStatusEvet(StatusEventType.Text, $"{target.Base.Name}'s defense won't go down because of its Big Pecks!");
                    }
                }
            }
        },
        {AbilityID.ClearBody,
            new Ability(){
                Name = "Clear Body",
                Description = "Prevents other Pokémon from lowering this Pokémon's any stat.",
                OnBoost = (Dictionary<Stat, int> boosts, Pokemon source, Pokemon target) => {
                    if(source != null && source == target){
                        return;
                    }

                    bool boostRemoved = false;
                    foreach(var stat in boosts.Keys.ToList()){
                        if(boosts[stat] < 0){
                            boosts.Remove(stat);
                            boostRemoved = true;
                        }
                    }

                    if(boostRemoved){
                        target.AddStatusEvet(StatusEventType.Text, $"{target.Base.Name}'s stats won't go down because of its Clear Body!");
                    }
                }
            }
        },
        {AbilityID.WhiteSmoke,
            new Ability(){
                Name = "White Smoke",
                Description = "Prevents other Pokémon from lowering this Pokémon's any stat.",
                OnBoost = (Dictionary<Stat, int> boosts, Pokemon source, Pokemon target) => {
                    if(source != null && source == target){
                        return;
                    }

                    bool boostRemoved = false;
                    foreach(var stat in boosts.Keys.ToList()){
                        if(boosts[stat] < 0){
                            boosts.Remove(stat);
                            boostRemoved = true;
                        }
                    }

                    if(boostRemoved){
                        target.AddStatusEvet(StatusEventType.Text, $"{target.Base.Name}'s stats won't go down because of its White Smoke!");
                    }
                }
            }
        },
        {AbilityID.Insomnia,
            new Ability(){
                Name = "Insomnia",
                Description = "Prevents the Pokémon from falling asleep.",
                OnTrySetStatus = (StatusConditionID statusID, Pokemon pokemon, EffectSource source) => {
                    if(statusID == StatusConditionID.Sleep){
                        if(source == EffectSource.Move){
                            pokemon.AddStatusEvet(StatusEventType.Text, $"{pokemon.Base.Name}'s Insomnia prevents it from falling asleep!");
                        }

                        return false;
                    }

                    return true;
                }
            }
        },
        {AbilityID.Immunity,
            new Ability(){
                Name = "Immunity",
                Description = "Prevents the Pokémon from getting poisoned.",
                OnTrySetStatus = (StatusConditionID statusID, Pokemon pokemon, EffectSource source) => {
                    if(statusID == StatusConditionID.Poison){
                        if(source == EffectSource.Move){
                            pokemon.AddStatusEvet(StatusEventType.Text, $"{pokemon.Base.Name}'s Immunity prevents it from getting poisoned!");
                        }

                        return false;
                    }

                    return true;
                }
            }
        },
        {AbilityID.Limber,
            new Ability(){
                Name = "Limber",
                Description = "Prevents the Pokémon from getting paralyzed.",
                OnTrySetStatus = (StatusConditionID statusID, Pokemon pokemon, EffectSource source) => {
                    if(statusID == StatusConditionID.Paralyze){
                        if(source == EffectSource.Move){
                            pokemon.AddStatusEvet(StatusEventType.Text, $"{pokemon.Base.Name}'s Limber prevents it from getting paralyzed!");
                        }

                        return false;
                    }

                    return true;
                }
            }
        },
        {AbilityID.WaterVeil,
            new Ability(){
                Name = "Water Veil",
                Description = "Prevents the Pokémon from getting burned.",
                OnTrySetStatus = (StatusConditionID statusID, Pokemon pokemon, EffectSource source) => {
                    if(statusID == StatusConditionID.Burn){
                        if(source == EffectSource.Move){
                            pokemon.AddStatusEvet(StatusEventType.Text, $"{pokemon.Base.Name}'s water veil prevents it from getting burned!");
                        }

                        return false;
                    }

                    return true;
                }
            }
        },
        {AbilityID.VitalSpirit,
            new Ability(){
                Name = "Vital Spirit",
                Description = "Prevents the Pokémon from falling asleep.",
                OnTrySetStatus = (StatusConditionID statusID, Pokemon pokemon, EffectSource source) => {
                    if(statusID == StatusConditionID.Sleep){
                        if(source == EffectSource.Move){
                            pokemon.AddStatusEvet(StatusEventType.Text, $"{pokemon.Base.Name}'s Vital Spirit prevents it from falling asleep!");
                        }

                        return false;
                    }

                    return true;
                }
            }
        },
        {AbilityID.OwnTempo,
            new Ability(){
                Name = "Own Tempo",
                Description = "Prevents the Pokémon from getting confused.",
                OnTrySetVolatileStatus = (StatusConditionID statusID, Pokemon pokemon, EffectSource source) => {
                    if(statusID == StatusConditionID.Confusion){
                        if(source == EffectSource.Move){
                            pokemon.AddStatusEvet(StatusEventType.Text, $"{pokemon.Base.Name}'s Own Tempo prevents it from getting confused!");
                        }

                        return false;
                    }

                    return true;
                }
            }
        },
        {AbilityID.Static,
            new Ability(){
                Name = "Static",
                Description = "Has a chance to paralyze attackers that use physical moves on the Pokémon.",
                OnDamagingHit = (float damage, Pokemon attacker, Pokemon defender, Move move) => {
                    if(move.Base.HasFlag(MoveFlag.Contact) && (UnityEngine.Random.Range(1, 101) <= 30)){
                        attacker.SetStatus(StatusConditionID.Paralyze, EffectSource.Ability);
                    }
                }
            }
        },
        {AbilityID.PoisonPoint,
            new Ability(){
                Name = "Poison Point",
                Description = "Has a chance to poison attackers that use physical moves on the Pokémon.",
                OnDamagingHit = (float damage, Pokemon attacker, Pokemon defender, Move move) => {
                    if(move.Base.HasFlag(MoveFlag.Contact) && (UnityEngine.Random.Range(1, 101) <= 30)){
                        attacker.SetStatus(StatusConditionID.Poison, EffectSource.Ability);
                    }
                }
            }
        },
        {AbilityID.FlameBody,
            new Ability(){
                Name = "Flame Body",
                Description = "Has a chance to burn attackers that use physical moves on the Pokémon.",
                OnDamagingHit = (float damage, Pokemon attacker, Pokemon defender, Move move) => {
                    if(move.Base.HasFlag(MoveFlag.Contact) && (UnityEngine.Random.Range(1, 101) <= 30)){
                        attacker.SetStatus(StatusConditionID.Burn, EffectSource.Ability);
                    }
                }
            }
        },
        {AbilityID.ToughClaws,
            new Ability(){
                Name = "Tough Claws",
                Description = "Powers up moves that make direct contact.",
                OnModifyMoveBasePower = (float basePower, Pokemon attacker, Pokemon defender, Move move) => {
                    if(move.Base.HasFlag(MoveFlag.Contact)){
                        basePower *= 1.3f;
                    }
                    return basePower;
                }
            }
        },
        {AbilityID.StrongJaw,
            new Ability(){
                Name = "Strong Jaw",
                Description = "Powers up biting moves.",
                OnModifyMoveBasePower = (float basePower, Pokemon attacker, Pokemon defender, Move move) => {
                    if(move.Base.HasFlag(MoveFlag.Bite)){
                        basePower *= 1.3f;
                    }
                    return basePower;
                }
            }
        },
        {AbilityID.IronFist,
            new Ability(){
                Name = "Iron Fist",
                Description = "Powers up punching moves.",
                OnModifyMoveBasePower = (float basePower, Pokemon attacker, Pokemon defender, Move move) => {
                    if(move.Base.HasFlag(MoveFlag.Punch)){
                        basePower *= 1.3f;
                    }
                    return basePower;
                }
            }
        }
        
    };
}
