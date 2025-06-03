using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Inventory : MonoBehaviour{
    [Header("Slots")]
    [SerializeField] List<ItemSlot> slots;

    public List<ItemSlot> Slots => slots;

    public static Inventory GetInventory(){
        return FindObjectOfType<PlayerController>().GetComponent<Inventory>();
    }
}

[System.Serializable]
public class ItemSlot{
    [Header("Item Slot")]
    [SerializeField] ItemBase item;
    [SerializeField] int count;

    public ItemBase Item => item;
    public int Count => count;
}