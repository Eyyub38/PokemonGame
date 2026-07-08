using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ItemGiver : MonoBehaviour, ISavable{
    [Tooltip("Item given to the player.")]
    [SerializeField] ItemBase item;
    [Tooltip("Amount of the item given.")]
    [Min(1)]
    [SerializeField] int amount = 1;
    [Tooltip("Dialog shown before the item is given.")]
    [SerializeField] Dialog dialog;

    bool used = false;

    public IEnumerator GiveItem(PlayerController player){
        yield return DialogManager.i.ShowDialog(dialog);
        player.GetComponent<Inventory>().AddItem(item, amount);
        used = true;
        AudioManager.i.PlaySfx(AudioId.ItemObtained, pauseMusic: true);
        PublishItemReceivedEvent(player);
        yield return DialogManager.i.ShowDialogText($"{player.Name} received {amount} {item.name}{(amount > 1 ? "s" : "")}.");
    } 

    void PublishItemReceivedEvent(PlayerController player) {
        if(item == null || player == null) {
            return;
        }

        GameEventBus.Publish(
            $"inventory.item-received.{item.Name}",
            $"{player.Name} received {amount} {item.Name}{(amount > 1 ? "s" : "")}.",
            GameEventCategory.Inventory,
            GameEventImportance.Success,
            this,
            "ItemGiver",
            GameEventScope.Player,
            showInFeed: true,
            writeToDebugLog: false,
            values: new [] {
                GameEventPublishing.Value("itemName", item.Name),
                GameEventPublishing.Value("count", amount),
                GameEventPublishing.Value("method", "received")
            });
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
