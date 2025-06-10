using System;
using System.Linq;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum ItemCategory{ Recovery, Pokeball, TMs, Evolution}

public class Inventory : MonoBehaviour, ISavable{
    [Header("Slots")]
    [SerializeField] List<ItemSlot> recoverySlots;
    [SerializeField] List<ItemSlot> pokeballSlots;
    [SerializeField] List<ItemSlot> tmSlots;
    [SerializeField] List<ItemSlot> evolutionSlots;

    List<List<ItemSlot>> allSlots;

    public static List<string> ItemCategories {get; set;} = new List<string>(){"Recovery", "Pokeball", "TM", "Evolution"};
    
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

    public int GetItemCount(ItemBase item){
        int categoryIndex = (int)GetCategoryFromItem(item);
        var currSlots = GetItemSlotsByCategory(categoryIndex);

        var itemSlot = currSlots.FirstOrDefault(slot => slot.Item == item);

        if(itemSlot != null){
            return itemSlot.Count;
        } else {
            return 0;
        }
    }

    public ItemBase UseItem(int itemIndex, Pokemon selected, int categoryIndex){
        var item = GetItem(itemIndex, categoryIndex);
        bool itemUsed = item.Use(selected);
        if(itemUsed){
            if(!item.IsUsable){
                RemoveItem(item);
            }
            return item;
        }
        return null;
    }

    public void RemoveItem(ItemBase item,int count = 1){
        int categoryIndex = (int)GetCategoryFromItem(item);

        var currSlots = GetItemSlotsByCategory(categoryIndex);
        var itemSlot = currSlots.First( slot => slot.Item == item);
        itemSlot.Count -= count;
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

    public ItemCategory GetCategoryFromItem(ItemBase item){
        if(item is RecoveryItem){
            return ItemCategory.Recovery;
        } else if(item is PokeballItem){
            return ItemCategory.Pokeball;
        } else if(item is EvolutionItem){
            return ItemCategory.Evolution;
        } else {
            return ItemCategory.TMs;
        }
    }

    public object CaptureState(){
        var saveData = new InventorySaveData(){
            recovery = recoverySlots.Select(i => i.GetSaveData()).ToList(),
            pokeball = pokeballSlots.Select(i => i.GetSaveData()).ToList(),
            tm = tmSlots.Select(i => i.GetSaveData()).ToList(),
            evolution = evolutionSlots.Select(i => i.GetSaveData()).ToList()
        };
        return saveData;
    }

    public void RestoreState(object state){
        var saveData = (InventorySaveData)state;

        recoverySlots = saveData.recovery.Select(i => new ItemSlot(i)).ToList();
        pokeballSlots = saveData.pokeball.Select(i => new ItemSlot(i)).ToList();
        tmSlots = saveData.tm.Select(i => new ItemSlot(i)).ToList();
        evolutionSlots = saveData.evolution.Select(i => new ItemSlot(i)).ToList();

        allSlots = new List<List<ItemSlot>>(){recoverySlots, pokeballSlots, tmSlots, evolutionSlots};

        OnUpdated?.Invoke();
    }

    public bool HasItemEnough(ItemBase item, int count = 1){
        int categoryIndex = (int)GetCategoryFromItem(item);
        var currSlots = GetItemSlotsByCategory(categoryIndex);

        return currSlots.Exists(slot => slot.Item == item && slot.Count >= count);
    }
}

[Serializable]
public class ItemSlot{
    [Header("Item Slot")]
    [SerializeField] ItemBase item;
    [SerializeField] int count;

    public ItemBase Item {get => item; set => item = value; }
    public int Count {get => count; set => count = value; }

    public ItemSlot(){}

    public ItemSlot(ItemSaveData saveData){
        item = ItemDB.GetObjectByName(saveData.name);
        count = saveData.count;
    }

    public ItemSaveData GetSaveData(){
        var saveData = new ItemSaveData(){
            name = item.name,
            count = count
        };
        return saveData;
    }
}

[Serializable]
public class ItemSaveData{
    public string name;
    public int count;
}

[Serializable]
public class InventorySaveData{
    public List<ItemSaveData> recovery;
    public List<ItemSaveData> pokeball;
    public List<ItemSaveData> tm;
    public List<ItemSaveData> evolution;
}