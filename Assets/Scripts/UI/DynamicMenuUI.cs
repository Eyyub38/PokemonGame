using System;
using GDEUtills.GenerciSelectionUI;
using UnityEngine;

public class DynamicMenuUI : SelectionUI<TextSlot>{
    private void Start() {
        InputSource = InputRouter.i.UI;
    }
}
