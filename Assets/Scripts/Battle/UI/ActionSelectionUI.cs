using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using GDEUtills.GenerciSelectionUI;

public class ActionSelectionUI : SelectionUI<TextSlot>{
    protected override void Start(){
        base.Start();
        SetSelectionSettings(SelectionType.Grid, 2);
        SetItems(GetComponentsInChildren<TextSlot>().ToList());
    }
}
