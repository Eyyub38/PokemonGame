using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class ImageSlot : MonoBehaviour, ISelectableItem{
    Image image;
    Color originalColor;
    bool initialized = false;

    void Awake(){
        image = GetComponent<Image>();
    }

    public void Clear(){
        image.color = originalColor;
    }

    public void Init(){
        if(!initialized) {
            originalColor = image.color;
            initialized = true;
        }
    }

    public void OnSelectionChanged(bool selected){
        image.color = selected ? GlobalSettings.i.HighlightedImageColor : originalColor;
    }
}
