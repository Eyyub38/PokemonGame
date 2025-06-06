using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PickUp : MonoBehaviour, Interactable{
    [SerializeField] ItemBase item;

    public bool Used { get; set; } = false; 

    public IEnumerator Interact(Transform initiator){
        if(!Used){
            initiator.GetComponent<Inventory>().AddItem(item);
            Used = true;
            GetComponent<SpriteRenderer>().enabled = false;
            GetComponent<BoxCollider2D>().enabled = false;

            yield return DialogManager.i.ShowDialogText($"You picked up {item.Name}!");
        }
    }
}
