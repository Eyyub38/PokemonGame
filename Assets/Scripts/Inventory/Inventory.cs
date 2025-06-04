using System;
using System.Linq;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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

    public ItemBase UseItem(int itemIndex, Pokemon selected){
        var item = recoverySlots[itemIndex].Item;
        bool itemUsed = item.Use(selected);
        if(itemUsed){
            RemoveItem(item);
            return item;
        }

        return null;
    }

    public void RemoveItem(ItemBase item){
        var itemSlot = recoverySlots.First( slot => slot.Item == item);
        itemSlot.Count--;
        if(itemSlot.Count == 0){
            recoverySlots.Remove(itemSlot);
        }

        OnUpdated?.Invoke();
    }
}

[System.Serializable]
public class ItemSlot{
    [Header("Item Slot")]
    [SerializeField] ItemBase item;
    [SerializeField] int count;

    public ItemBase Item => item;
    public int Count {get => count; set => count = value; }
}