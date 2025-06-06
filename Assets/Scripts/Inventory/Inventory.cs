using System;
using System.Linq;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum ItemCategory{ Recovery, Pokeball, TMs, Evolution}

public class Inventory : MonoBehaviour{
    [Header("Slots")]
    [SerializeField] List<ItemSlot> recoverySlots;
    [SerializeField] List<ItemSlot> pokeballSlots;
    [SerializeField] List<ItemSlot> tmSlots;
    [SerializeField] List<ItemSlot> evolutionSlots;

    List<List<ItemSlot>> allSlots;

    public static List<string> ItemCategories {get; set;} = new List<string>(){"Recovery", "Pokeball", "TMs & HMs", "Evolution"};
    
    public event Action OnUpdated;

    void Awake(){
        allSlots = new List<List<ItemSlot>>(){recoverySlots, pokeballSlots, tmSlots, evolutionSlots};
    }

    public static Inventory GetInventory(){
        return FindObjectOfType<PlayerController>().GetComponent<Inventory>();
    }

    public List<ItemSlot> GetItemSlotsByCategory(int categoryIndex){
        return allSlots[categoryIndex];
    }

    public ItemBase GetItem(int itemIndex, int categoryIndex){
        var currSlots = GetItemSlotsByCategory(categoryIndex);
        return currSlots[itemIndex].Item;
    }

    public ItemBase UseItem(int itemIndex, Pokemon selected, int categoryIndex){
        var item = GetItem(itemIndex, categoryIndex);
        bool itemUsed = item.Use(selected);
        if(itemUsed){
            if(!item.IsUsable){
                RemoveItem(item, categoryIndex);
            }
            return item;
        }
        return null;
    }

    public void RemoveItem(ItemBase item, int categoryIndex){
        var currSlots = GetItemSlotsByCategory(categoryIndex);
        var itemSlot = currSlots.First( slot => slot.Item == item);
        itemSlot.Count--;
        if(itemSlot.Count == 0){
            currSlots.Remove(itemSlot);
        }

        OnUpdated?.Invoke();
    }

    public void AddItem(ItemBase item, int count = 1){
        int category = (int)GetCategoryFromItem(item);
        var currSlots = GetItemSlotsByCategory(category);

        var itemSlot = currSlots.FirstOrDefault(slot => slot.Item == item);
        if(itemSlot != null){
            itemSlot.Count += count;
        } else {
            currSlots.Add(new ItemSlot(){
                Item = item, 
                Count = count
            });
        }
        OnUpdated?.Invoke();
    }

    ItemCategory GetCategoryFromItem(ItemBase item){
        if(item is RecoveryItem){
            return ItemCategory.Recovery;
        } else if(item is PokeballItem){
            return ItemCategory.Pokeball;
        } else {
            return ItemCategory.TMs;
        }
    }
}

[System.Serializable]
public class ItemSlot{
    [Header("Item Slot")]
    [SerializeField] ItemBase item;
    [SerializeField] int count;

    public ItemBase Item {get => item; set => item = value; }
    public int Count {get => count; set => count = value; }
}