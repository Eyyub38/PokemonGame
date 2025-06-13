using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class TextSlot : MonoBehaviour, ISelectableItem
{
    [SerializeField] Text text;

    Color originalColor;

    public void OnSelectionChanged(bool selected){
        text.color = selected ? GlobalSettings.i.HighlightedColor : originalColor;
    }

    public void Init(){
        originalColor = text.color;
    }
}
