using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Merchant : MonoBehaviour{
    public IEnumerator Trade(){
        yield return ShopController.i.StartTrading(this);
    }
}
