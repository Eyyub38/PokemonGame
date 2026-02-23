using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class TextSlot : MonoBehaviour, ISelectableItem{
    [SerializeField] Text text;

    Color originalColor;
    bool initialized = false;

    public void OnSelectionChanged(bool selected){
        text.color = selected ? GlobalSettings.i.HighlightedTextColor : originalColor;
    }

    public void Init(){
        if(!initialized) {
            originalColor = text.color;
            initialized = true;
        }
    }

    public void SetText(string s){
        text.text = s;
    }

    public void Clear(){
        text.color = originalColor;
    }
}
