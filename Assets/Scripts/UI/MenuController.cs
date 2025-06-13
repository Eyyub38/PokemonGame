using System;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using GDEUtills.GenerciSelectionUI;

public class MenuController : SelectionUI<TextSlot>{
    void Start(){
        SetItems(GetComponentsInChildren<TextSlot>().ToList());
    }
}
