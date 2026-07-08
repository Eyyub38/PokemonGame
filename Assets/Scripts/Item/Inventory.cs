using System;
using System.Linq;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum ItemCategory{ Recovery, Pokeball, TMs, Evolution, HeldItems}

public class Inventory : MonoBehaviour, ISavable{
    [Header("Slots")]
    [Tooltip("Saved inventory slots for recovery items.")]
    [SerializeField] List<ItemSlot> recoverySlots;
    [Tooltip("Saved inventory slots for pokeballs.")]
    [SerializeField] List<ItemSlot> pokeballSlots;
    [Tooltip("Saved inventory slots for TM and move teaching items.")]
    [SerializeField] List<ItemSlot> tmSlots;
    [Tooltip("Saved inventory slots for evolution items.")]
    [SerializeField] List<ItemSlot> evolutionSlots;
    [Tooltip("Saved inventory slots for Pokemon held/equipment items.")]
    [SerializeField] List<ItemSlot> heldItemSlots;

    List<List<ItemSlot>> allSlots;

    public static List<string> ItemCategories {get; set;} = new List<string>(){"Recovery", "Pokeball", "TMs", "Evolution", "Held Items"};
    
    public event Action OnUpdated;

    void Awake(){
        EnsureSlotLists();
    }

    public static Inventory GetInventory(){
        return FindAnyObjectByType<PlayerController>().GetComponent<Inventory>();
    }

    public List<ItemSlot> GetItemSlotsByCategory(int categoryIndex){
        EnsureSlotLists();
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
        return UseItem(item, selected);
    }
    
    public ItemBase UseItem(ItemBase item, Pokemon selected){
        bool itemUsed = item.Use(selected);
        if(itemUsed){
            if(item.IsConsumable){
                RemoveItem(item);
            }
            return item;
        }
        return null;
    }

    public void RemoveItem(ItemBase item,int count = 1){
        if(item == null || count <= 0) {
            return;
        }

        int categoryIndex = (int)GetCategoryFromItem(item);

        var currSlots = GetItemSlotsByCategory(categoryIndex);
        var itemSlot = currSlots.FirstOrDefault( slot => slot.Item == item);
        
        if (itemSlot == null) return;
        
        itemSlot.Count -= count;
        if(itemSlot.Count <= 0){
            currSlots.Remove(itemSlot);
        }

        OnUpdated?.Invoke();
    }

    public void AddItem(ItemBase item, int count = 1){
        if (item == null || count <= 0) return;
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
        if(item is BattleHeldItem){
            return ItemCategory.HeldItems;
        } else if(item is RecoveryItem){
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
        EnsureSlotLists();
        var saveData = new InventorySaveData(){
            recovery = recoverySlots.Select(i => i.GetSaveData()).ToList(),
            pokeball = pokeballSlots.Select(i => i.GetSaveData()).ToList(),
            tm = tmSlots.Select(i => i.GetSaveData()).ToList(),
            evolution = evolutionSlots.Select(i => i.GetSaveData()).ToList(),
            heldItems = heldItemSlots.Select(i => i.GetSaveData()).ToList()
        };
        return saveData;
    }

    public void RestoreState(object state){
        var saveData = (InventorySaveData)state;

        recoverySlots = saveData.recovery.Select(i => new ItemSlot(i)).ToList();
        pokeballSlots = saveData.pokeball.Select(i => new ItemSlot(i)).ToList();
        tmSlots = saveData.tm.Select(i => new ItemSlot(i)).ToList();
        evolutionSlots = saveData.evolution.Select(i => new ItemSlot(i)).ToList();
        heldItemSlots = saveData.heldItems != null
            ? saveData.heldItems.Select(i => new ItemSlot(i)).ToList()
            : new List<ItemSlot>();

        EnsureSlotLists();

        OnUpdated?.Invoke();
    }

    public bool HasItemEnough(ItemBase item, int count = 1){
        if(item == null) {
            return false;
        }

        int categoryIndex = (int)GetCategoryFromItem(item);
        var currSlots = GetItemSlotsByCategory(categoryIndex);

        return currSlots.Exists(slot => slot.Item == item && slot.Count >= count);
    }

    void EnsureSlotLists() {
        recoverySlots ??= new List<ItemSlot>();
        pokeballSlots ??= new List<ItemSlot>();
        tmSlots ??= new List<ItemSlot>();
        evolutionSlots ??= new List<ItemSlot>();
        heldItemSlots ??= new List<ItemSlot>();
        allSlots = new List<List<ItemSlot>>(){recoverySlots, pokeballSlots, tmSlots, evolutionSlots, heldItemSlots};
    }
}

[Serializable]
public class ItemSlot{
    [Header("Item Slot")]
    [Tooltip("Inventory item stored in this slot.")]
    [SerializeField] ItemBase item;
    [Tooltip("Stack count for this item.")]
    [Min(0)]
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
            name = item != null ? item.name : string.Empty,
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
    public List<ItemSaveData> heldItems;
}
