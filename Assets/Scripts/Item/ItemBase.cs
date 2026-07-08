using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum ItemType{ HealHP, HealStatus, TempBoost, PermBoost, Evolution, Pokeball, RestoreMove, Revive, TeachMove}

public class ItemBase : ScriptableObject{
    [Header("Item Details")]
    [Tooltip("Item name shown in inventory and messages.")]
    [SerializeField] string _name;
    [Tooltip("Item description shown in inventory.")]
    [SerializeField] string description;
    [Tooltip("Item icon used by inventory UI.")]
    [SerializeField] Sprite icon;
    [Tooltip("Inventory category/type for this item.")]
    [SerializeField] ItemType itemType;
    [Tooltip("Shop buy/sell reference price.")]
    [Min(0f)]
    [SerializeField] float price;
    [Tooltip("If enabled, shops may allow selling this item.")]
    [SerializeField] bool isSellable;

    public virtual string Name => _name;
    public string Description => description;
    public Sprite Icon => icon;
    public ItemType ItemType => itemType;
    public float Price => price;
    public bool IsSellable => isSellable;
    public virtual bool CanUseInBattle => true;
    public virtual bool CanUseInOutsideBattle => true;
    public virtual bool IsConsumable => true;

    public virtual bool Use(Pokemon pokemon){
        return false;
    }

}
