using System;
using GDEUtills.GenerciSelectionUI;
using UnityEngine;

public class DynamicMenuUI : SelectionUI<TextSlot>{
    protected override void Start() {
        base.Start();
        InputSource = InputRouter.i.UI;
    }
}
