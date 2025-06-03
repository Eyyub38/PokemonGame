using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour{
    public void HandleUpdate(Action onBack){
        if(Input.GetKeyDown(KeyCode.Escape)){
            onBack?.Invoke();
        }
    }
}
