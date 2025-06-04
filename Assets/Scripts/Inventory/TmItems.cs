using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Items/Create new TM or HM")]
public class TmItems : ItemBase{
    [SerializeField] MoveBase move;

    public MoveBase Move => move;

    public override bool Use(Pokemon pokemon){
        return pokemon.HasMove(move);
    }
} 