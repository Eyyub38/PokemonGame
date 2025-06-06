using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ItemDB{
    static Dictionary<string, ItemBase> items;

    public static void Init(){
        items = new Dictionary<string, ItemBase>();

        var itemArray = Resources.LoadAll<ItemBase>("");
        foreach(var item in itemArray){
            if(items.ContainsKey(item.Name)){
                continue;
            }
            items[item.Name] = item;
        }
    }

    public static ItemBase GetItemByName(string name){
        if(!items.ContainsKey(name)){
            return null;
        }

        return items[name];
    }
}