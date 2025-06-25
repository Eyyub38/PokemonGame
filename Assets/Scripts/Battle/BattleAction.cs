using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum BattleActionType { Move, SwitchPokemon, UseItem, Run}

public class BattleAction{
    public BattleActionType Type { get; set;}
    public BattleUnit User{ get; set;}
    public BattleUnit Target{ get; set;}

    public Move SelectedMove { get; set;}
    public Pokemon SelectedPokemon { get; set;}
    public ItemBase SelectedItem { get; set;}

    public bool IsInvalid { get; set;}

    public int Priority => (Type == BattleActionType.Move) ? SelectedMove.Base.Priority : 99;
}
