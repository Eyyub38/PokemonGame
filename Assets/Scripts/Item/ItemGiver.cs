using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ItemGiver : MonoBehaviour, ISavable{
    [SerializeField] ItemBase item;
    [SerializeField] int amount = 1;
    [SerializeField] Dialog dialog;

    bool used = false;

    public IEnumerator GiveItem(PlayerController player){
        yield return DialogManager.i.ShowDialog(dialog);
        player.GetComponent<Inventory>().AddItem(item, amount);
        used = true;
        yield return DialogManager.i.ShowDialogText($"{player.name} received {amount} {item.name}{(amount > 1 ? "s" : "")}.");
    } 

    public bool CanBeGiven(){
        return item != null && amount > 0 && !used;
    }

    public object CaptureState(){
        return used;
    }

    public void RestoreState(object state){
        used = (bool) state;
    }
}
