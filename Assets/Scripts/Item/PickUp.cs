using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PickUp : MonoBehaviour, Interactable, ISavable{
    [Tooltip("Item added to the player's inventory when picked up.")]
    [SerializeField] ItemBase item;
    [Tooltip("Amount of the item added to the inventory.")]
    [Min(1)]
    [SerializeField] int count = 1;

    public bool Used { get; set; } = false;

    public IEnumerator Interact(Transform initiator){
        if(!Used){
            initiator.GetComponent<Inventory>().AddItem(item, count);
            Used = true;
            GetComponent<SpriteRenderer>().enabled = false;
            GetComponent<BoxCollider2D>().enabled = false;

            string playerName = initiator.GetComponent<PlayerController>().Name;
            AudioManager.i.PlaySfx(AudioId.ItemObtained, pauseMusic: true);
            PublishItemObtainedEvent(playerName, "picked up");
            yield return DialogManager.i.ShowDialogText($"{playerName} picked up {item.Name}{(count > 1 ? "s" : "")}!");
        }
    }

    void PublishItemObtainedEvent(string playerName, string verb) {
        if(item == null) {
            return;
        }

        GameEventBus.Publish(
            $"inventory.item-obtained.{item.Name}",
            $"{playerName} {verb} {count} {item.Name}{(count > 1 ? "s" : "")}.",
            GameEventCategory.Inventory,
            GameEventImportance.Success,
            this,
            "PickUp",
            GameEventScope.Player,
            showInFeed: true,
            writeToDebugLog: false,
            values: new [] {
                GameEventPublishing.Value("itemName", item.Name),
                GameEventPublishing.Value("count", count),
                GameEventPublishing.Value("method", verb)
            });
    }
    
    public object CaptureState(){
        return Used;
    }

    public void RestoreState(object state){
        Used = (bool) state;

        if(Used){
            GetComponent<SpriteRenderer>().enabled = false;
            GetComponent<BoxCollider2D>().enabled = false;
        }
    }
}
