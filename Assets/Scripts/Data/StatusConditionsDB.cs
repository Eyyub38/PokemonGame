using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public enum StatusConditionID{ non, psn, brn, slp, par, frz, fro, tox, confusion}

public class StatusConditionsDB{
    public static void Init(){
        foreach(var kvp in Conditions){
            var conditionId = kvp.Key;
            var condition = kvp.Value;

            condition.Id = conditionId;
        }
    }
    
    public static Dictionary<StatusConditionID, StatusCondition> Conditions { get; set; } = new Dictionary<StatusConditionID, StatusCondition>(){
        { StatusConditionID.psn,
            new StatusCondition{
                Name = "Poison",
                StartMessage = "has been poisoned!",
                OnAfterTurn = (Pokemon pokemon) =>{
                    pokemon.DecreaseHP(pokemon.MaxHp / 8);
                    pokemon.AddStatusEvet( StatusEventType.Damage, $"{pokemon.Base.Name} is hurt by poison!");
                }
            }
        },
        { StatusConditionID.brn,
            new StatusCondition{
                Name = "Burn",
                StartMessage = "has been burned!",
                OnAfterTurn = (Pokemon pokemon) =>{
                    pokemon.DecreaseHP(pokemon.MaxHp / 16);
                    pokemon.AddStatusEvet( StatusEventType.Damage, $"{pokemon.Base.Name} is hurt by burn!");
                }
            }
        },
        { StatusConditionID.tox,
            new StatusCondition{
                Name = "Toxic",
                StartMessage = "has been badly poisoned!",
                OnAfterTurn = (Pokemon pokemon) =>{
                    int damage = pokemon.MaxHp / 16;
                    if (pokemon.StatusTime > 0){
                        damage += pokemon.StatusTime * pokemon.MaxHp / 16;
                    }
                    pokemon.DecreaseHP(damage);
                    pokemon.AddStatusEvet( StatusEventType.Damage, $"{pokemon.Base.Name} is hurt by poison badly!");
                    pokemon.StatusTime++;
                }
            }
        },
        { StatusConditionID.par,
            new StatusCondition{
                Name = "Paralysis",
                StartMessage = "has been paralyzed!",
                OnBeforeMove = (Pokemon pokemon) => {
                    if (Random.Range(1, 5) == 1){
                        pokemon.AddStatusEvet($"{pokemon.Base.Name} is fully paralyzed. It can't move!");
                        return false;
                    }
                    return true;
                }
            }
        },
        { StatusConditionID.frz,
            new StatusCondition{
                Name = "Freeze",
                StartMessage = "has been frozen solid!",
                OnBeforeMove = (Pokemon pokemon) => {
                    if (Random.Range(1, 5) == 1){
                        pokemon.AddStatusEvet($"{pokemon.Base.Name} thawed out!");
                        pokemon.CureStatus();
                        return true;
                    }
                    pokemon.AddStatusEvet($"{pokemon.Base.Name} is frozen solid. It can't move!");
                    return false;
                }
            }
        },
        { StatusConditionID.fro,
            new StatusCondition{
                Name = "Frostbite",
                StartMessage = "has been frostbitten!",
                OnAfterTurn = (Pokemon pokemon) =>{
                    pokemon.DecreaseHP(pokemon.MaxHp / 16);
                    pokemon.AddStatusEvet( StatusEventType.Damage, $"{pokemon.Base.Name} is hurt by frostbite!");
                }
            }
        },
        { StatusConditionID.slp,
            new StatusCondition{
                Name = "Sleep",
                StartMessage = "has fallen asleep!",
                OnStart = (Pokemon pokemon) => {
                    pokemon.AddStatusEvet($"{pokemon.Base.Name} fell asleep!");
                    pokemon.StatusTime = Random.Range(1, 4);
                },
                OnBeforeMove = (Pokemon pokemon) => {
                    if (pokemon.StatusTime <= 0){
                        pokemon.CureStatus();
                        pokemon.AddStatusEvet($"{pokemon.Base.Name} woke up!");
                        return true;
                    }
                    pokemon.StatusTime--;
                    pokemon.AddStatusEvet($"{pokemon.Base.Name} is fast asleep. It can't move!");
                    return false;
                }
            }
        },
        { StatusConditionID.confusion,
            new StatusCondition{
                Name = "Confusion",
                StartMessage = "has been confused!",
                OnStart = (Pokemon pokemon) => {
                    pokemon.VolatileStatusTime = Random.Range(1, 5);
                },
                OnBeforeMove = (Pokemon pokemon) => {
                    if (pokemon.VolatileStatusTime <= 0){
                        pokemon.CureVolatileStatus();
                        pokemon.AddStatusEvet($"{pokemon.Base.Name} kickesd off its confusion!");
                        return true;
                    }
                    pokemon.VolatileStatusTime--;

                    if(Random.Range(1, 3) == 1){
                        return true;
                    }
                    pokemon.AddStatusEvet($"{pokemon.Base.Name} is confused.");
                    pokemon.DecreaseHP(pokemon.MaxHp / 8);
                    pokemon.AddStatusEvet( StatusEventType.Damage, $"{pokemon.Base.Name} hurt itself in its confusion!");
                    return false;
                }
            }
        },
    };
    public static float GetStatusBonus(StatusCondition condition){
        if(condition == null){
            return 1f;
        } else if(condition.Id == StatusConditionID.slp || condition.Id == StatusConditionID.frz){
            return 2f;
        } else if(condition.Id == StatusConditionID.psn || condition.Id == StatusConditionID.par || condition.Id == StatusConditionID.brn){
            return 1.5f;
        }
        
        return 1f;
    }
}