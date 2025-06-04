using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Items/Create new recovery item")]
public class RecoveryItem : ItemBase{
    [Header("HP")]
    [SerializeField] int hpAmount;
    [SerializeField] bool restoreMaxHp;

    [Header("PP")]
    [SerializeField] int ppAmount;
    [SerializeField] bool restoreMaxPp;

    [Header("Status")]
    [SerializeField] ConditionID status;
    [SerializeField] bool recoverAllStatus;

    [Header("Revive")]
    [SerializeField] bool revive;
    [SerializeField] bool maxRevive;

    public override bool Use(Pokemon pokemon){

        if(revive || maxRevive){
            if(pokemon.HP > 0){
                return false;
            }
            if(revive){
                pokemon.IncreaseHP(pokemon.MaxHp / 2);
            } else if(maxRevive){
                pokemon.IncreaseHP(pokemon.MaxHp);
            }

            pokemon.CureStatus();

            return true;
        }

        if(pokemon.HP <= 0){
            return false;
        }

        if(restoreMaxHp || hpAmount > 0){
            if(pokemon.HP == pokemon.MaxHp){
                return false;
            }
            if(restoreMaxHp){
                pokemon.IncreaseHP(pokemon.MaxHp);
            } else {
                pokemon.IncreaseHP(hpAmount);
            }
        }

        if(recoverAllStatus || status != ConditionID.non){
            if(pokemon.Status == null && pokemon.VolatileStatus == null){
                return false;
            }

            if(recoverAllStatus){
                pokemon.CureStatus();
                pokemon.CureVolatileStatus();
            } else {
                if(pokemon.Status.Id == status ){
                    pokemon.CureStatus();
                } else if(pokemon.VolatileStatus.Id == status){
                    pokemon.CureVolatileStatus();
                } else {
                    return false;
                }
            }

            if(restoreMaxPp){
                pokemon.Moves.ForEach(m => m.IncreasePP(m.Base.PP));
            } else if(ppAmount > 0){
                pokemon.Moves.ForEach(m => m.IncreasePP(ppAmount));
            }
        }
        return true;
    }
}