using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum ItemType{ HealHP, HealStatus, TempBoost, PermBoost, Evolution, Pokeball, RestoreMove, Revive, TeachMove}

public class ItemBase : ScriptableObject{
    [SerializeField] string _name;
    [SerializeField] string description;
    [SerializeField] Sprite icon;
    [SerializeField] ItemType itemType;

    public virtual string Name => _name;
    public string Description => description;
    public Sprite Icon => icon;
    public ItemType ItemType => itemType;

    public virtual bool CanUseInBattle => true;
    public virtual bool CanUseInOffsideBattle => true;
    public virtual bool IsUsable => true;

    public virtual bool Use(Pokemon pokemon){
        return false;
    }

}