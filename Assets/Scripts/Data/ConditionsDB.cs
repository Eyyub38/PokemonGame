using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class ConditionsDB{
    public static void Init(){
        foreach(var kvp in Conditions){
            var conditionId = kvp.Key;
            var condition = kvp.Value;

            condition.Id = conditionId;
        }
    }
    public static Dictionary<ConditionID, Condition> Conditions { get; set; } = new Dictionary<ConditionID, Condition>(){
        { ConditionID.psn,
            new Condition{
                Name = "Poison",
                StartMessage = "has been poisoned!",
                OnAfterTurn = (Pokemon pokemon) =>{
                    pokemon.UpdateHP(pokemon.MaxHp / 8);
                    pokemon.StatusChanges.Enqueue($"{pokemon.Base.Name} is hurt by poison!");
                }
            }
        },
        { ConditionID.brn,
            new Condition{
                Name = "Burn",
                StartMessage = "has been burned!",
                OnAfterTurn = (Pokemon pokemon) =>{
                    pokemon.UpdateHP(pokemon.MaxHp / 16);
                    pokemon.StatusChanges.Enqueue($"{pokemon.Base.Name} is hurt by burn!");
                }
            }
        },
        { ConditionID.tox,
            new Condition{
                Name = "Toxic",
                StartMessage = "has been badly poisoned!",
                OnAfterTurn = (Pokemon pokemon) =>{
                    int damage = pokemon.MaxHp / 16;
                    if (pokemon.StatusTime > 0){
                        damage += pokemon.StatusTime * pokemon.MaxHp / 16;
                    }
                    pokemon.UpdateHP(damage);
                    pokemon.StatusChanges.Enqueue($"{pokemon.Base.Name} is hurt by poison badly!");
                    pokemon.StatusTime++;
                }
            }
        },
        { ConditionID.par,
            new Condition{
                Name = "Paralysis",
                StartMessage = "has been paralyzed!",
                OnBeforeMove = (Pokemon pokemon) => {
                    if (Random.Range(1, 5) == 1){
                        pokemon.StatusChanges.Enqueue($"{pokemon.Base.Name} is fully paralyzed. It can't move!");
                        return false;
                    }
                    return true;
                }
            }
        },
        { ConditionID.frz,
            new Condition{
                Name = "Freeze",
                StartMessage = "has been frozen solid!",
                OnBeforeMove = (Pokemon pokemon) => {
                    if (Random.Range(1, 5) == 1){
                        pokemon.StatusChanges.Enqueue($"{pokemon.Base.Name} thawed out!");
                        pokemon.CureStatus();
                        return true;
                    }
                    pokemon.StatusChanges.Enqueue($"{pokemon.Base.Name} is frozen solid. It can't move!");
                    return false;
                }
            }
        },
        { ConditionID.fro,
            new Condition{
                Name = "Frostbite",
                StartMessage = "has been frostbitten!",
                OnAfterTurn = (Pokemon pokemon) =>{
                    pokemon.UpdateHP(pokemon.MaxHp / 16);
                    pokemon.StatusChanges.Enqueue($"{pokemon.Base.Name} is hurt by frostbite!");
                }
            }
        },
        { ConditionID.slp,
            new Condition{
                Name = "Sleep",
                StartMessage = "has fallen asleep!",
                OnStart = (Pokemon pokemon) => {
                    pokemon.StatusChanges.Enqueue($"{pokemon.Base.Name} fell asleep!");
                    pokemon.StatusTime = Random.Range(1, 4);
                },
                OnBeforeMove = (Pokemon pokemon) => {
                    if (pokemon.StatusTime <= 0){
                        pokemon.CureStatus();
                        pokemon.StatusChanges.Enqueue($"{pokemon.Base.Name} woke up!");
                        return true;
                    }
                    pokemon.StatusTime--;
                    pokemon.StatusChanges.Enqueue($"{pokemon.Base.Name} is fast asleep. It can't move!");
                    return false;
                }
            }
        },
        { ConditionID.confusion,
            new Condition{
                Name = "Confusion",
                StartMessage = "has been confused!",
                OnStart = (Pokemon pokemon) => {
                    pokemon.VolatileStatusTime = Random.Range(1, 5);
                },
                OnBeforeMove = (Pokemon pokemon) => {
                    if (pokemon.VolatileStatusTime <= 0){
                        pokemon.CureVolatileStatus();
                        pokemon.StatusChanges.Enqueue($"{pokemon.Base.Name} kickesd off its confusion!");
                        return true;
                    }
                    pokemon.VolatileStatusTime--;

                    if(Random.Range(1, 3) == 1){
                        return true;
                    }
                    pokemon.StatusChanges.Enqueue($"{pokemon.Base.Name} is confued.");
                    pokemon.UpdateHP(pokemon.MaxHp / 8);
                    pokemon.StatusChanges.Enqueue($"{pokemon.Base.Name} hurt itself in its confusion!");
                    return false;
                }
            }
        },
    };

    public static float GetStatusBonus(Condition condition){
        if(condition == null){
            return 1f;
        } else if(condition.Id == ConditionID.slp || condition.Id == ConditionID.frz){
            return 2f;
        } else if(condition.Id == ConditionID.psn || condition.Id == ConditionID.par || condition.Id == ConditionID.brn){
            return 1.5f;
        }
        
        return 1f;
    }
}

public enum ConditionID{ non, psn, brn, slp, par, frz, fro, tox, confusion}