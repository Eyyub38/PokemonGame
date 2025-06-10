using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PickUp : MonoBehaviour, Interactable, ISavable{
    [SerializeField] ItemBase item;
    [SerializeField] int count = 1;

    public bool Used { get; set; } = false;


    public IEnumerator Interact(Transform initiator){
        if(!Used){
            initiator.GetComponent<Inventory>().AddItem(item, count);
            Used = true;
            GetComponent<SpriteRenderer>().enabled = false;
            GetComponent<BoxCollider2D>().enabled = false;

            string playerName = initiator.GetComponent<PlayerController>().Name;

            yield return DialogManager.i.ShowDialogText($"{playerName} picked up {item.Name}{(count > 1 ? "s" : "")}!");
        }
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
