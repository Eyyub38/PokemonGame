using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Items/Create new TM")]
public class TmItem : ItemBase{
    [Tooltip("Move taught by this TM item.")]
    [SerializeField] MoveBase move;

    public MoveBase Move => move;
    public override bool CanUseInBattle => false;
    public override string Name => base.Name + " (" + move.Name + ")";

    public override bool Use(Pokemon pokemon){
        return pokemon.HasMove(move);
    }

    public bool CanBeTaught(Pokemon pokemon){
        return pokemon.Base.LearnableMovesByTm.Contains(move);
    }
}
