using System;
using System.Linq;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Inventory : MonoBehaviour{
    [Header("Slots")]
    [SerializeField] List<ItemSlot> slots;

    public List<ItemSlot> Slots => slots;

    public event Action OnUpdated;

    public static Inventory GetInventory(){
        return FindObjectOfType<PlayerController>().GetComponent<Inventory>();
    }

    public ItemBase UseItem(int itemIndex, Pokemon selected){
        var item = slots[itemIndex].Item;
        bool itemUsed = item.Use(selected);
        if(itemUsed){
            RemoveItem(item);
            return item;
        }

        return null;
    }

    public void RemoveItem(ItemBase item){
        var itemSlot = slots.First( slot => slot.Item == item);
        itemSlot.Count--;
        if(itemSlot.Count == 0){
            slots.Remove(itemSlot);
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