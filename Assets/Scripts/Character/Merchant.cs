using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Merchant : MonoBehaviour{
    [SerializeField] List<ItemBase> availableItems;

    public List<ItemBase> AvailableItems => availableItems;


    public IEnumerator Trade(){
        ShopMenuState.i.AvailableItems = availableItems;
        yield return GameController.i.StateMachine.PushAndWait(ShopMenuState.i);
    }
}
